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
        [SerializeField] private Image _masteryFill;
        [Tooltip("체력 5칸 (S-134 ④) — 차에 치이면 2칸 꺼진다. 0칸이면 강제 귀가+정산.")]
        [SerializeField] private Image[] _healthPips;

        private const float MASTERY_CELLS = 5f; // S-134 ① — 경험치 5칸
        private static readonly Color HEALTH_ON = new Color(0.90f, 0.35f, 0.32f, 1f);
        private static readonly Color HEALTH_OFF = new Color(0.20f, 0.16f, 0.18f, 1f);
        [SerializeField] private TMP_Text _deliveryCountLabel;

        [Header("상호작용 안내 (하단 중앙)")]
        [SerializeField] private GameObject _ePrompt;

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
                int remaining = Mathf.FloorToInt(_activeDeadline - clock.MinuteOfDay);
                if (remaining != _shownRemaining)
                {
                    _shownRemaining = remaining;
                    _remainingLabel.text = remaining > 0 ? $"마감까지 {remaining}분" : "지각";
                }
            }
        }

        // ── 배송 카드 (캐리 상태) ────────────────────────────
        private void OnCarryStateChanged(bool isCarrying)
        {
            if (_cardRoot != null) _cardRoot.SetActive(isCarrying);
            if (!isCarrying) _hasCard = false;
            // 카드 내용은 PackagePickedUp(실제 든 건의 페이로드)이 채운다 (S-016 ① —
            // 구현이 적재 첫 건을 읽던 결함 수리: 든 것과 다른 주소가 표시됐다).
        }

        private void OnPackagePickedUp(DeliveryData data)
        {
            _activeDeadline = data.DeadlineMinuteOfDay;
            _hasCard = true;
            if (_cardRoot != null) _cardRoot.SetActive(true);
            if (_addressLabel != null)
                _addressLabel.text = data.Address
                    + (string.IsNullOrEmpty(data.District) ? "" : "  <size=70%><color=#8a93a8>" + data.District + "</color></size>");
            if (_cardBackground != null) _cardBackground.color = CardNormal;

            int remaining = Mathf.FloorToInt(_activeDeadline - _gameState.minuteOfDay);
            if (_remainingLabel != null)
                _remainingLabel.text = remaining > 0 ? $"마감까지 {remaining}분" : "지각";
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
            else if (IsHovering(_masteryFill, pointer)) { hovered = _masteryFill; reason = "경험치"; }

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
        }

        // 배송지 포커스면 주소를 [E] 안내에 병기 — 풀해상 오버레이라 픽셀화에 안 뭉개진다 (S-021 ②).
        private void OnFocusAddressChanged(string address)
        {
            if (_ePrompt == null) return;
            TMP_Text label = _ePrompt.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;
            label.text = string.IsNullOrEmpty(address)
                ? "[E] 상호작용"
                : "[E] 배송 인증  <color=#ff9f45>" + address + "</color>";
        }

        // ── 씬별 가시성 ──────────────────────────────────────
        private void OnSceneTransitionCompleted(GameScene scene)
        {
            if (_content != null) _content.SetActive(scene != GameScene.Main);
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
            if (_masteryFill != null)
            {
                float ratio = Mathf.Clamp01(_gameState.mastery / MasteryProgress.MaxFor(_gameState.playerLevel));
                _masteryFill.fillAmount = Mathf.Floor(ratio * MASTERY_CELLS) / MASTERY_CELLS;
            }
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
