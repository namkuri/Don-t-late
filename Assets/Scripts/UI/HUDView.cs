using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// Core 상주 HUD. 이벤트를 구독해 표시만 한다 — 판정·계산 로직은 매니저 몫.
    /// 시각차(남은시간) 같은 '표시 계산'만 여기서 한다. money/debt는 GameStateSO를 읽기만 한다.
    /// 씬별 가시성은 SceneTransitionCompleted로 토글(Main 인트로에선 숨김).
    /// </summary>
    public class HUDView : MonoBehaviour
    {
        [Header("데이터 (읽기 전용)")]
        [SerializeField] private GameStateSO _gameState;

        [Header("가시성 루트")]
        [Tooltip("Main 인트로에선 숨기는 HUD 콘텐츠 컨테이너.")]
        [SerializeField] private GameObject _content;

        [Header("시계 (우상)")]
        [SerializeField] private TMP_Text _clockLabel;

        [Header("배송 카드 (좌상) — 캐리 중에만 표시")]
        [SerializeField] private GameObject _cardRoot;
        [SerializeField] private TMP_Text _addressLabel;
        [SerializeField] private TMP_Text _remainingLabel;
        [Tooltip("DeadlineWarned 시 앰버로 강조되는 배경.")]
        [SerializeField] private Image _cardBackground;

        [Header("스태미나 (좌하)")]
        [SerializeField] private Image _staminaFill;
        [Tooltip("패널티 세그먼트 (S-088 ④) — 오른쪽부터 더움·추움·무거움·강풍 순으로 쌓인다.")]
        [SerializeField] private Image _penaltyHeatFill;
        [SerializeField] private Image _penaltyColdFill;
        [SerializeField] private Image _penaltyCarryFill;
        [SerializeField] private Image _penaltyStormFill;

        [Header("경제 (우상 아래)")]
        [SerializeField] private TMP_Text _moneyLabel;
        [SerializeField] private TMP_Text _debtLabel;

        // S-063 상단 바 — 캐릭터 진행·당일 배송수량.
        [SerializeField] private TMP_Text _levelLabel;
        [Tooltip("경험치 바 5칸 — 배송 1건당 2칸(S-174 ②). 칸 단위로 순차 펀치한다.")]
        [SerializeField] private Image[] _masteryPips;
        [Tooltip("경험치 칸들이 올라앉은 바 배경 — 호버 툴팁 판정용.")]
        [SerializeField] private Image _masteryBar;
        [Tooltip("체력 5칸 (S-134 ④) — 차에 치이면 2칸 꺼진다. 0칸이면 강제 귀가+정산.")]
        [SerializeField] private Image[] _healthPips;

        private const float MASTERY_CELLS = 5f; // S-134 ① — 경험치 5칸
        private static readonly Color HEALTH_ON = new Color(0.90f, 0.35f, 0.32f, 1f);
        private static readonly Color HEALTH_OFF = new Color(0.20f, 0.16f, 0.18f, 1f);
        // S-174 후속 — 경험치는 **노랑**(남규님 지시). 앰버(주황기)는 마감 경고·엣지 화살표가
        // 이미 쓰고 있어, 성장 게이지는 더 밝은 노랑으로 갈라놓는다.
        private static readonly Color MASTERY_ON = new Color(1f, 0.85f, 0.2f, 1f);   // #ffd933
        private static readonly Color MASTERY_OFF = new Color(0.16f, 0.14f, 0.10f, 1f);
        [SerializeField] private TMP_Text _deliveryCountLabel;

        [Header("상호작용 안내 (하단 중앙)")]
        [SerializeField] private GameObject _ePrompt;
        [Tooltip("E 프롬프트 아래 보조 안내 (S-169 — 예: 바코드 스캔). 문구는 센서가 정한다.")]
        [SerializeField] private TMP_Text _focusHintLabel;

        private static readonly Color CardNormal = new Color(0.10f, 0.12f, 0.16f, 0.85f);
        private static readonly Color CardWarn = new Color(1f, 0.624f, 0.271f, 0.92f); // #ff9f45

        // 카드에 걸린 활성 배송의 마감(분). 남은시간 표시 계산에만 쓴다.
        private float _activeDeadline;
        private bool _hasCard;

        // S-069 — 시계 틱(초당 2회)마다 5개 라벨을 무조건 재조립하던 TMP GC 억제:
        // 값이 실제로 바뀐 라벨만 다시 쓴다 (TMP.SetArraySizes가 GC 최상위 기여였다).
        private int _shownMoney = int.MinValue;
        private int _shownDebt = int.MinValue;
        private int _shownLevel = int.MinValue;
        private int _shownDone = int.MinValue;
        private int _shownCargo = int.MinValue;
        private int _shownRemaining = int.MinValue;

        private void OnEnable()
        {
            WorldEvents.ClockTicked += OnClockTicked;
            WorldEvents.CarryStateChanged += OnCarryStateChanged;
            WorldEvents.PackagePickedUp += OnPackagePickedUp;
            WorldEvents.DeadlineWarned += OnDeadlineWarned;
            WorldEvents.DeliveryCompleted += OnDeliveryCompleted;
            WorldEvents.DeliveryFailed += OnDeliveryFailed;
            WorldEvents.DebtSettled += OnDebtSettled;
            WorldEvents.DebtIncreased += OnDebtIncreased;
            WorldEvents.MoneySpent += OnMoneySpent;
            WorldEvents.StaminaChanged += OnStaminaChanged;
            WorldEvents.BuffStaminaChanged += OnBuffStaminaChanged; // S-098 ②
            WorldEvents.StaminaPenaltyChanged += OnStaminaPenaltyChanged; // S-088 ④
            WorldEvents.InteractionFocusChanged += OnInteractionFocusChanged;
            WorldEvents.FocusAddressChanged += OnFocusAddressChanged;
            WorldEvents.FocusPromptChanged += OnFocusPromptChanged;   // S-249
            WorldEvents.FocusHintChanged += OnFocusHintChanged; // S-169
            WorldEvents.MasteryChanged += OnMasteryChanged;     // S-174 ④
            WorldEvents.SceneTransitionCompleted += OnSceneTransitionCompleted;
        }

        private void OnDisable()
        {
            WorldEvents.ClockTicked -= OnClockTicked;
            WorldEvents.CarryStateChanged -= OnCarryStateChanged;
            WorldEvents.PackagePickedUp -= OnPackagePickedUp;
            WorldEvents.DeadlineWarned -= OnDeadlineWarned;
            WorldEvents.DeliveryCompleted -= OnDeliveryCompleted;
            WorldEvents.DeliveryFailed -= OnDeliveryFailed;
            WorldEvents.DebtSettled -= OnDebtSettled;
            WorldEvents.DebtIncreased -= OnDebtIncreased;
            WorldEvents.MoneySpent -= OnMoneySpent;
            WorldEvents.StaminaChanged -= OnStaminaChanged;
            WorldEvents.BuffStaminaChanged -= OnBuffStaminaChanged;
            WorldEvents.StaminaPenaltyChanged -= OnStaminaPenaltyChanged;
            WorldEvents.InteractionFocusChanged -= OnInteractionFocusChanged;
            WorldEvents.FocusAddressChanged -= OnFocusAddressChanged;
            WorldEvents.FocusPromptChanged -= OnFocusPromptChanged;
            WorldEvents.FocusHintChanged -= OnFocusHintChanged;
            WorldEvents.MasteryChanged -= OnMasteryChanged;
            WorldEvents.SceneTransitionCompleted -= OnSceneTransitionCompleted;
        }

        private void Start()
        {
            if (_cardRoot != null) _cardRoot.SetActive(false);
            if (_ePrompt != null) _ePrompt.SetActive(false);
            if (_content != null) _content.SetActive(false); // 첫 씬(Main 인트로)에선 숨김
            if (_staminaFill != null) _staminaFill.fillAmount = 1f;
            RefreshEconomy();
        }

        // ── 시계 ──────────────────────────────────────────────
        private void OnClockTicked(GameClock clock)
        {
            if (_clockLabel != null)
                _clockLabel.text = $"Day {clock.Day} · {clock.Hour:00}:{clock.Minute:00}";

            // S-028 ④: 이벤트 없는 지출·입금(자판기·은행 테스트 버튼)도 시계 틱에서 캐치업.
            RefreshEconomy();

            if (_hasCard && _remainingLabel != null)
            {
                // S-179 — 카드가 여러 건을 담으므로 남은시간도 목록 전체를 다시 쓴다.
                // 분이 바뀔 때만 — 시계 틱은 초당 2회라 매번 재조립하면 TMP GC가 튄다(S-069).
                int remaining = Mathf.FloorToInt(_activeDeadline - clock.MinuteOfDay);
                if (remaining != _shownRemaining)
                {
                    _shownRemaining = remaining;
                    RefreshDeliveryCard();
                }
            }
        }

        // ── 배송 카드 (캐리 상태) ────────────────────────────
        private void OnCarryStateChanged(bool isCarrying)
        {
            if (_cardRoot != null) _cardRoot.SetActive(isCarrying);
            if (!isCarrying) { _hasCard = false; _shownCarryCount = -1; }
            // S-179 — 한 건만 내려놓고 나머지를 계속 들고 있는 경우: 카드는 남고 목록만 줄어든다.
            else RefreshDeliveryCard();
            // 카드 내용은 PackagePickedUp(실제 든 건의 페이로드)이 채운다 (S-016 ① —
            // 구현이 적재 첫 건을 읽던 결함 수리: 든 것과 다른 주소가 표시됐다).
        }

        private void OnPackagePickedUp(DeliveryData data)
        {
            _activeDeadline = data.DeadlineMinuteOfDay;
            _hasCard = true;
            if (_cardRoot != null) _cardRoot.SetActive(true);
            if (_cardBackground != null) _cardBackground.color = CardNormal;
            RefreshDeliveryCard();
        }

        // ── S-179 다중 적재 카드 ───────────────────────────────
        // 종전엔 `PackagePickedUp` **페이로드 한 건**만 그려서, 둘을 들면 나중에 집은 것만 남았다
        // (남규님 지적). Lv2부터 2개·Lv5부터 3개를 드는데 UI가 1건 시절 그대로였다.
        // 이제 **적재 목록(GameStateSO.carriedOrders)을 그린다** — 이벤트는 "갱신 신호"일 뿐이다.
        private const float CARD_LINE_H = 96f;   // 한 건이 차지하는 높이(주소 56 + 마감 48에서 겹침 보정)
        private const float CARD_BASE_H = 150f;  // 1건 카드 높이 — 이 값이 곧 종전 레이아웃이다
        private int _shownCarryCount = -1;

        private void RefreshDeliveryCard()
        {
            if (_gameState == null || _addressLabel == null || _remainingLabel == null) return;
            var carried = _gameState.carriedOrders;
            int count = carried != null ? carried.Count : 0;
            if (count == 0) return; // 내려놓기 처리는 CarryStateChanged가 한다

            var addressText = new System.Text.StringBuilder();
            var remainText = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++)
            {
                DeliveryOrderSO order = carried[i];
                if (order == null) continue;
                if (addressText.Length > 0) { addressText.Append('\n'); remainText.Append('\n'); }

                addressText.Append(order.address);
                if (!string.IsNullOrEmpty(order.district))
                    addressText.Append("  <size=70%><color=#8a93a8>").Append(order.district).Append("</color></size>");

                int left = Mathf.FloorToInt(order.deadlineMinuteOfDay - _gameState.minuteOfDay);
                // 지각한 건도 목록에서 빠지지 않는다 — 들고 있는 건 전부 보여야 어디로 갈지 정한다.
                remainText.Append(left > 0 ? $"마감까지 {left}분" : "<color=#ff7359>지각</color>");
            }
            _addressLabel.text = addressText.ToString();
            _remainingLabel.text = remainText.ToString();

            if (count == _shownCarryCount) return; // 크기 조정은 건수가 바뀔 때만
            _shownCarryCount = count;
            ResizeDeliveryCard(count);
        }

        /// <summary>건수만큼 카드와 두 라벨을 늘린다. 1건이면 빌더가 깐 치수 그대로다.</summary>
        private void ResizeDeliveryCard(int count)
        {
            float extra = CARD_LINE_H * (count - 1);
            if (_cardRoot != null && _cardRoot.transform is RectTransform cardRect)
                cardRect.sizeDelta = new Vector2(cardRect.sizeDelta.x, CARD_BASE_H + extra);
            // 주소는 위에서 아래로, 마감은 아래에서 위로 자란다(각자 앵커 방향).
            GrowLabel(_addressLabel.rectTransform, 56f, count);
            GrowLabel(_remainingLabel.rectTransform, 48f, count);
        }

        private static void GrowLabel(RectTransform rect, float lineHeight, int count)
        {
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, lineHeight * count);
        }

        private void OnDeadlineWarned(DeliveryData data)
        {
            if (_hasCard && _cardBackground != null) _cardBackground.color = CardWarn;
        }

        private void OnDeliveryCompleted(DeliveryData data)
        {
            _hasCard = false;
            if (_cardRoot != null) _cardRoot.SetActive(false);
            RefreshEconomy();
            // 보상 플로팅 (S-015) — 돈 라벨 곁 시안.
            SpawnFloatingAmount("+₩" + data.Reward.ToString("N0"), new Color(0.208f, 0.878f, 0.784f), _moneyLabel);
        }

        private void OnDeliveryFailed(DeliveryData data)
        {
            _hasCard = false;
            if (_cardRoot != null) _cardRoot.SetActive(false);
        }

        // 정산 직후 즉시 반영 — 정산 중엔 시간이 멈춰(ClockTicked 없음) 이 경로가 유일하다 (S-010).
        private void OnDebtSettled(DebtSettlement _) => RefreshEconomy();

        // 벌금 즉시 가산 (S-015) — 빚 라벨 옆에 빨간 플로팅 금액.
        // S-030 ③: 지출 차감 연출 — 잔액 라벨에서 붉은 플로팅.
        private void OnMoneySpent(int amount)
        {
            RefreshEconomy();
            SpawnFloatingAmount("-₩" + amount.ToString("N0"), new Color(1f, 0.45f, 0.35f), _moneyLabel);
        }

        private void OnDebtIncreased(int amount)
        {
            RefreshEconomy();
            SpawnFloatingAmount("+₩" + amount.ToString("N0"), new Color(1f, 0.45f, 0.35f), _debtLabel);
        }

        // ── 플로팅 금액 (S-015) — 라벨 곁에서 떠올랐다 사라진다 ──
        private void SpawnFloatingAmount(string text, Color color, TMP_Text anchorLabel)
        {
            if (anchorLabel == null) return;
            // S-080 ② — 정산 정지 중엔 억제: 일괄 판정이 N건 이벤트를 같은 프레임에 쏘면 플로팅이
            // 겹쳐 마지막 금액만 보였다("+1,700만 뜸"의 원흉). 상세는 정산 패널 리스트가 전담.
            if (Time.timeScale == 0f) return;

            GameObject go = new GameObject("FloatAmount", typeof(RectTransform));
            go.transform.SetParent(anchorLabel.transform.parent, false);
            TMP_Text label = go.AddComponent<TextMeshProUGUI>();
            label.font = anchorLabel.font;
            label.fontSize = anchorLabel.fontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = color;
            label.text = text;
            label.alignment = TextAlignmentOptions.Right;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform rect = (RectTransform)go.transform;
            RectTransform anchorRect = anchorLabel.rectTransform;
            rect.anchorMin = anchorRect.anchorMin;
            rect.anchorMax = anchorRect.anchorMax;
            rect.pivot = anchorRect.pivot;
            rect.sizeDelta = anchorRect.sizeDelta;
            rect.anchoredPosition = anchorRect.anchoredPosition + new Vector2(-30f, -8f);

            StartCoroutine(FloatAndFade(label, rect));
        }

        private System.Collections.IEnumerator FloatAndFade(TMP_Text label, RectTransform rect)
        {
            const float DURATION = 1.4f;
            Vector2 start = rect.anchoredPosition;
            Color baseColor = label.color;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / DURATION;
                rect.anchoredPosition = start + new Vector2(0f, 46f * t);
                label.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t * t);
                yield return null;
            }
            Destroy(label.gameObject);
        }

        // S-090 ③ — 금액 롤링 카운터.
        private Coroutine _moneyRoll;

        private System.Collections.IEnumerator RollMoney(int from, int to)
        {
            const float DURATION = 0.4f;
            float t = 0f;
            while (t < DURATION && _moneyLabel != null)
            {
                t += Time.unscaledDeltaTime;
                int value = Mathf.RoundToInt(Mathf.Lerp(from, to, Mathf.Clamp01(t / DURATION)));
                _moneyLabel.text = "₩" + value.ToString("N0");
                yield return null;
            }
            if (_moneyLabel != null) _moneyLabel.text = "₩" + to.ToString("N0");
            _moneyRoll = null;
        }

        // S-088 ③ — 돈 증가 펀치: 순간 확대+민트 플래시 후 원복.
        // ── S-174 ④ 경험치 칸 순차 펀치 ────────────────────
        // 2칸이 한 번에 올라도 **한 칸씩 차례로** 튄다 — 동시에 튀면 "몇 칸 올랐는지"가 안 세어진다.
        private int _shownLit = -1;      // 마지막으로 표시한 켜진 칸 수 (-1 = 아직 모름)
        private bool _masteryPunching;
        private Coroutine _masteryRoutine;

        private int LitCount()
        {
            if (_gameState == null) return 0;
            float ratio = Mathf.Clamp01(_gameState.mastery / MasteryProgress.MaxFor(_gameState.playerLevel));
            return Mathf.FloorToInt(ratio * MASTERY_CELLS);
        }

        private void PaintMasteryPips()
        {
            if (_masteryPips == null) return;
            int lit = LitCount();
            _shownLit = lit;
            for (int i = 0; i < _masteryPips.Length; i++)
                if (_masteryPips[i] != null)
                    _masteryPips[i].color = i < lit ? MASTERY_ON : MASTERY_OFF;
        }

        private void OnMasteryChanged(float mastery, int level)
        {
            if (_masteryPips == null) return;
            int before = _shownLit;
            int after = LitCount();
            // 첫 통지이거나 줄었으면(실패·레벨업 랩) 연출 없이 그냥 맞춘다.
            if (before < 0 || after <= before) { PaintMasteryPips(); return; }

            if (_masteryRoutine != null) StopCoroutine(_masteryRoutine);
            _masteryRoutine = StartCoroutine(PunchMasteryPips(before, after));
        }

        private System.Collections.IEnumerator PunchMasteryPips(int from, int to)
        {
            _masteryPunching = true;
            for (int i = from; i < to && i < _masteryPips.Length; i++)
            {
                if (_masteryPips[i] == null) continue;
                _masteryPips[i].color = MASTERY_ON; // 켜면서 튄다
                yield return PunchGraphic(_masteryPips[i].rectTransform);
            }
            _masteryPunching = false;
            _masteryRoutine = null;
            PaintMasteryPips(); // 중간에 값이 또 변했을 수 있으니 마지막에 실측으로 맞춘다
        }

        /// <summary>한 칸을 키웠다 되돌린다. 정산창(timeScale=0)에서도 돌도록 unscaled.</summary>
        private System.Collections.IEnumerator PunchGraphic(RectTransform rect)
        {
            const float DURATION = 0.22f;
            const float PEAK = 1.3f; // 바 칸은 넓다 — 1.55면 옆 칸을 덮는다
            float e = 0f;
            while (e < DURATION && rect != null)
            {
                e += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(e / DURATION);
                // 앞부분에서 확 커졌다 감속하며 원복 — 1-(1-k)²로 되돌아온다.
                float scale = Mathf.Lerp(PEAK, 1f, 1f - (1f - k) * (1f - k));
                rect.localScale = Vector3.one * scale;
                yield return null;
            }
            if (rect != null) rect.localScale = Vector3.one;
        }

        private System.Collections.IEnumerator PunchLabel(TMP_Text label)
        {
            Color baseColor = label.color;
            Transform t = label.transform;
            float e = 0f;
            const float DURATION = 0.35f;
            while (e < DURATION && label != null)
            {
                e += Time.unscaledDeltaTime;
                float k = 1f - e / DURATION;
                t.localScale = Vector3.one * (1f + 0.35f * k);
                label.color = Color.Lerp(baseColor, new Color(0.208f, 0.878f, 0.784f), k);
                yield return null;
            }
            if (label != null) { t.localScale = Vector3.one; label.color = baseColor; }
        }

        // ── 스태미나 ──────────────────────────────────────────
        // S-074 ⑦ — 통지값을 목표로 두고 매 프레임 부드럽게 추적: 걷기는 연속으로 흐르고,
        // 뛰기는 드레인 자체가 커서 빠르게 뚝뚝 떨어지는 감각이 남는다.
        private float _staminaTarget = 1f;

        private void OnStaminaChanged(float normalized) => _staminaTarget = Mathf.Clamp01(normalized);

        // S-098 ② — 버프 스태미나 풀(0~0.1): 초록 fill 오른쪽에 파란 세그먼트로 이어 붙는다.
        private float _buffTarget;
        private float _buffShown;

        private void OnBuffStaminaChanged(float normalized) => _buffTarget = Mathf.Clamp(normalized, 0f, 0.2f);

        // S-088 ④ — 패널티 구간: 바 오른쪽 끝에서부터 각자 색으로 잠식 표시. anchorMax 오른쪽 기점.
        private void OnStaminaPenaltyChanged(StaminaPenalties p)
        {
            float right = 1f;
            right = PlacePenalty(_penaltyHeatFill, p.Heat / 100f, right);
            right = PlacePenalty(_penaltyColdFill, p.Cold / 100f, right);
            right = PlacePenalty(_penaltyCarryFill, p.Carry / 100f, right);
            PlacePenalty(_penaltyStormFill, p.Storm / 100f, right);
        }

        private static float PlacePenalty(Image fill, float width, float rightEdge)
        {
            if (fill == null) return rightEdge;
            bool on = width > 0.001f;
            fill.gameObject.SetActive(on);
            if (!on) return rightEdge;
            RectTransform rect = fill.rectTransform;
            rect.anchorMin = new Vector2(rightEdge - width, 0f);
            rect.anchorMax = new Vector2(rightEdge, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rightEdge - width;
        }

        private void Update()
        {
            if (_staminaFill == null) return;
            _staminaFill.fillAmount = Mathf.MoveTowards(_staminaFill.fillAmount, _staminaTarget, Time.deltaTime * 0.6f);
            _buffShown = Mathf.MoveTowards(_buffShown, _buffTarget, Time.deltaTime * 0.6f);
            UpdateBuffFill();
            UpdatePenaltyTooltip();
        }

        // ── S-098 ② — 버프 스태미나 풀: 초록 fill 끝에 이어 붙는 파란 세그먼트 (음용 즉시 표시) ──

        private Image _buffFill;

        private void UpdateBuffFill()
        {
            if (_buffShown < 0.003f)
            {
                if (_buffFill != null) _buffFill.gameObject.SetActive(false);
                return;
            }
            if (_buffFill == null)
            {
                _buffFill = new GameObject("BuffFill", typeof(RectTransform)).AddComponent<Image>();
                _buffFill.transform.SetParent(_staminaFill.rectTransform.parent, false);
                _buffFill.color = new Color(0.31f, 0.58f, 1f, 1f); // 버프 = 파랑 (추움 얼음빛과 구분)
                _buffFill.raycastTarget = false;
            }
            _buffFill.gameObject.SetActive(true);
            // S-099 — 바 안쪽 수납: 합이 바 폭을 넘으면 파랑이 초록 끝을 대체 (배경 박스 밖 돌출 금지 — S-098 반려).
            float left = Mathf.Max(0f, Mathf.Min(_staminaFill.fillAmount, 1f - _buffShown));
            RectTransform rect = _buffFill.rectTransform;
            rect.anchorMin = new Vector2(left, 0f);
            rect.anchorMax = new Vector2(Mathf.Min(left + _buffShown, 1f), 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        // ── S-097 ② — 패널티 세그먼트 호버: 사유 라벨 ──

        private TMP_Text _penaltyTooltip;

        private void UpdatePenaltyTooltip()
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return;
            Vector2 pointer = mouse.position.ReadValue();

            Image hovered = null;
            string reason = null;
            if (IsHovering(_penaltyHeatFill, pointer)) { hovered = _penaltyHeatFill; reason = "더움"; }
            else if (IsHovering(_penaltyColdFill, pointer)) { hovered = _penaltyColdFill; reason = "추움"; }
            else if (IsHovering(_penaltyCarryFill, pointer)) { hovered = _penaltyCarryFill; reason = "무거움"; }
            else if (IsHovering(_penaltyStormFill, pointer)) { hovered = _penaltyStormFill; reason = "강풍"; }
            // S-098 ③ — 세그먼트에 안 걸렸으면 바 전체: 스태미나·경험치 이름표.
            else if (IsHovering(_staminaFill, pointer)) { hovered = _staminaFill; reason = "스태미나"; }
            // S-166 ⑤ — 칸이 낱개로 쪼개져 채움 이미지가 사라졌다. 호버 판정은 바 배경으로 옮긴다.
            else if (IsHovering(_masteryBar, pointer)) { hovered = _masteryBar; reason = "경험치"; }

            if (hovered == null)
            {
                if (_penaltyTooltip != null) _penaltyTooltip.gameObject.SetActive(false);
                return;
            }
            if (_penaltyTooltip == null)
            {
                _penaltyTooltip = new GameObject("PenaltyTooltip", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
                _penaltyTooltip.transform.SetParent(_staminaFill.canvas.transform, false);
                if (UiOverlayFont.Korean != null) _penaltyTooltip.font = UiOverlayFont.Korean;
                _penaltyTooltip.fontSize = 18f;
                _penaltyTooltip.fontStyle = TMPro.FontStyles.Bold;
                _penaltyTooltip.color = new Color(1f, 0.76f, 0.42f, 1f); // 패널티 = 앰버 톤
                _penaltyTooltip.alignment = TMPro.TextAlignmentOptions.Center;
                _penaltyTooltip.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                _penaltyTooltip.raycastTarget = false;
                _penaltyTooltip.rectTransform.sizeDelta = new Vector2(120f, 24f);
            }
            _penaltyTooltip.gameObject.SetActive(true);
            _penaltyTooltip.text = reason;
            _penaltyTooltip.rectTransform.position =
                hovered.rectTransform.position + new Vector3(0f, 26f, 0f);
        }

        private static bool IsHovering(Image segment, Vector2 pointer)
        {
            return segment != null && segment.gameObject.activeSelf
                && RectTransformUtility.RectangleContainsScreenPoint(segment.rectTransform, pointer);
        }

        // ── 상호작용 안내 ────────────────────────────────────
        private void OnInteractionFocusChanged(bool focused)
        {
            if (_ePrompt != null) _ePrompt.SetActive(focused);
            // 포커스가 풀리면 보조 안내도 같이 내린다 — 힌트 이벤트가 뒤늦게 와도 한 프레임 남지 않게.
            if (!focused && _focusHintLabel != null) _focusHintLabel.gameObject.SetActive(false);
        }

        // S-169 — E 프롬프트 아래 한 줄. 뷰는 문구를 만들지 않고 받아 쓴다(판정은 PickupBox 몫).
        private void OnFocusHintChanged(string hint)
        {
            if (_focusHintLabel == null) return;
            bool show = !string.IsNullOrEmpty(hint);
            if (show) _focusHintLabel.text = hint;
            _focusHintLabel.gameObject.SetActive(show);
        }

        // 배송지 포커스면 주소를 [E] 안내에 병기 — 풀해상 오버레이라 픽셀화에 안 뭉개진다 (S-021 ②).
        private void OnFocusAddressChanged(string address)
        {
            _focusAddress = address;
            RefreshEPrompt();
        }

        // S-249 — 대상이 제 문구를 준 경우(트럭 구매/탑승 등). 주소보다 우선한다.
        private void OnFocusPromptChanged(string prompt)
        {
            _focusPrompt = prompt;
            RefreshEPrompt();
        }

        private string _focusAddress;
        private string _focusPrompt;

        private void RefreshEPrompt()
        {
            if (_ePrompt == null) return;
            TMP_Text label = _ePrompt.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;
            label.text = !string.IsNullOrEmpty(_focusPrompt) ? _focusPrompt
                : string.IsNullOrEmpty(_focusAddress) ? "[E] 상호작용"
                : "[E] 배송 인증  <color=#ff9f45>" + _focusAddress + "</color>";
        }

        // ── 씬별 가시성 ──────────────────────────────────────
        private void OnSceneTransitionCompleted(GameScene scene)
        {
            if (_content != null) _content.SetActive(scene != GameScene.Main);

            // S-173 ① — 씬을 넘으면 상호작용 안내를 내린다. HUD는 Core 상주라 살아남는데
            // 포커스를 잡던 오브젝트는 이전 씬과 함께 사라진다. 새 씬의 센서는 **바뀔 때만**
            // 발행하므로 아무것도 안 잡히면 영영 안 쏜다 — 이전 씬의 "[E] 상호작용"이 그대로
            // 남는다(남규님: 정산하고 Home 들어왔는데 EPrompt가 쓸데없이 떠 있음).
            if (_ePrompt != null) _ePrompt.SetActive(false);
            if (_focusHintLabel != null) _focusHintLabel.gameObject.SetActive(false);
        }

        // ── 헬퍼 ─────────────────────────────────────────────
        private void RefreshEconomy()
        {
            if (_gameState == null) return;
            if (_moneyLabel != null && _gameState.money != _shownMoney)
            {
                bool first = _shownMoney == int.MinValue;
                bool increased = !first && _gameState.money > _shownMoney;
                int from = first ? _gameState.money : _shownMoney;
                _shownMoney = _gameState.money;
                // S-090 ③ — 금액 롤링: 이전 표시값에서 새 값으로 촤르르 (가감 공통 0.4s).
                if (_moneyRoll != null) StopCoroutine(_moneyRoll);
                _moneyRoll = StartCoroutine(RollMoney(from, _gameState.money));
                if (increased) StartCoroutine(PunchLabel(_moneyLabel)); // S-088 ③ — 증가 순간 커졌다 원복
            }
            if (_debtLabel != null && _gameState.debt != _shownDebt)
            {
                _shownDebt = _gameState.debt;
                _debtLabel.text = $"빚 ₩{_gameState.debt:N0}";
            }

            // S-063 — 레벨·숙련도·당일 배송수량 (시계 틱과 함께 갱신).
            if (_levelLabel != null && _gameState.playerLevel != _shownLevel)
            {
                _shownLevel = _gameState.playerLevel;
                _levelLabel.text = $"Lv.{_gameState.playerLevel}  {_gameState.nickname}";
            }
            // S-134 ① — 경험치를 **5칸**으로 간략화(정수님 QA). 연속 게이지는 진행이 안 읽혔다.
            // S-166 ⑤ — 칸 나눔을 fillAmount 계단이 아니라 **낱개 이미지**로 바꾼다(HP와 동일 표기).
            // 계단 fill은 잘린 지점에 경계선이 없어 "다섯 칸"이 눈에 안 들어왔다(남규님 지적).
            // S-174 ④ — 칠하기는 여기(주기 갱신), **펀치는 MasteryChanged 이벤트**가 맡는다.
            // 연출 도중 이 루프가 색을 덮어써도 무해하다: 펀치는 크기만 건드린다.
            if (!_masteryPunching) PaintMasteryPips();
            // S-134 ④ — 체력 5칸.
            if (_healthPips != null)
            {
                for (int i = 0; i < _healthPips.Length; i++)
                    if (_healthPips[i] != null)
                        _healthPips[i].color = i < _gameState.health ? HEALTH_ON : HEALTH_OFF;
            }
            if (_deliveryCountLabel != null)
            {
                int done = _gameState.placedDeliveries.Count;
                int cargo = _gameState.cargo.Count;
                if (done != _shownDone || cargo != _shownCargo)
                {
                    _shownDone = done;
                    _shownCargo = cargo;
                    _deliveryCountLabel.text = "박스 " + done + "/" + (done + cargo);
                }
            }
        }

    }
}
