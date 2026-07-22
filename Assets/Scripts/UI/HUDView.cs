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

        [Header("경제 (우상 아래)")]
        [SerializeField] private TMP_Text _moneyLabel;
        [SerializeField] private TMP_Text _debtLabel;

        [Header("상호작용 안내 (하단 중앙)")]
        [SerializeField] private GameObject _ePrompt;

        private static readonly Color CardNormal = new Color(0.10f, 0.12f, 0.16f, 0.85f);
        private static readonly Color CardWarn = new Color(1f, 0.624f, 0.271f, 0.92f); // #ff9f45

        // 카드에 걸린 활성 배송의 마감(분). 남은시간 표시 계산에만 쓴다.
        private float _activeDeadline;
        private bool _hasCard;

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
            WorldEvents.StaminaChanged += OnStaminaChanged;
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
            WorldEvents.StaminaChanged -= OnStaminaChanged;
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
                _remainingLabel.text = remaining > 0 ? $"마감까지 {remaining}분" : "지각";
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
        private void OnDebtIncreased(int amount)
        {
            RefreshEconomy();
            SpawnFloatingAmount("+₩" + amount.ToString("N0"), new Color(1f, 0.45f, 0.35f), _debtLabel);
        }

        // ── 플로팅 금액 (S-015) — 라벨 곁에서 떠올랐다 사라진다 ──
        private void SpawnFloatingAmount(string text, Color color, TMP_Text anchorLabel)
        {
            if (anchorLabel == null) return;

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

        // ── 스태미나 ──────────────────────────────────────────
        private void OnStaminaChanged(float normalized)
        {
            if (_staminaFill != null) _staminaFill.fillAmount = Mathf.Clamp01(normalized);
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
            if (_moneyLabel != null) _moneyLabel.text = $"₩{_gameState.money:N0}";
            if (_debtLabel != null) _debtLabel.text = $"빚 ₩{_gameState.debt:N0}";
        }

    }
}
