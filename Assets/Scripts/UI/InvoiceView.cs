using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 택배 송장 오버레이 (S-071 ② — 남규님 발주, 매니페스트 직교 추가). 손이 빈 상태에서
    /// 상자를 좌클릭하면 InvoiceRequested를 받아 주문 정보를 보여준다: 주문자·주소·구역·
    /// 마감(긴급도 색)·무게·취급주의·바코드(orderId 유래 줄무늬 — 폰트 글리프 리스크 회피, R19 교훈).
    /// 표시만 한다 — 판정·계산 로직 없음 (UI 규칙).
    /// </summary>
    public class InvoiceView : MonoBehaviour
    {
        /// <summary>ESC로 송장을 닫은 프레임 (S-101) — 같은 프레임에 설정창이 또 반응하지 않게 (실행 순서 무관 방어).</summary>
        public static int LastEscCloseFrame { get; private set; } = -1;

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _customerLabel;
        [SerializeField] private TMP_Text _addressLabel;
        [SerializeField] private TMP_Text _deadlineLabel;
        [SerializeField] private TMP_Text _detailLabel;
        [SerializeField] private TMP_Text _barcodeNumberLabel;
        [SerializeField] private RectTransform _barcodeRoot;

        // 주문자명 풀 — orderId로 결정적 선택 (SO에 필드 추가 없이 표시 전용, YAGNI).
        private static readonly string[] Customers =
        {
            "김말순", "박정옥", "이사장", "최반장", "정여사", "한동수", "오미란", "서길동",
            "남주혁", "구본희", "임꺽정", "표창수",
        };

        private static readonly Color URGENT_RED = new Color(1f, 0.45f, 0.35f);
        private static readonly Color WARN_AMBER = new Color(1f, 0.624f, 0.271f);
        private static readonly Color SAFE_MINT = new Color(0.208f, 0.878f, 0.784f);

        private void OnEnable()
        {
            WorldEvents.InvoiceRequested += OnInvoiceRequested;
            WorldEvents.SceneTransitionStarted += OnSceneLeaving;
        }

        private void OnDisable()
        {
            WorldEvents.InvoiceRequested -= OnInvoiceRequested;
            WorldEvents.SceneTransitionStarted -= OnSceneLeaving;
        }

        private void Start()
        {
            if (_root != null) _root.SetActive(false);
            // S-073 — 런타임 오버레이 공유 폰트 등록 (빌더가 주입한 Pretendard — 한글 글리프 보장).
            if (_customerLabel != null && _customerLabel.font != null)
                UiOverlayFont.Korean = _customerLabel.font;
        }

        private void Update()
        {
            if (_root == null || !_root.activeSelf)
            {
                // 다른 경로(폰 닫기 등)로 송장이 꺼졌을 때도 파인더를 남기지 않는다.
                if (_aiming) EndAim();
                return;
            }
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;

            // S-072 ② — 바코드 호버 = 폰 스캔 조준 모드. 마우스 오프셋(-1..1)을 폰 카메라 뷰에 중계한다.
            bool overBarcode = false;
            Vector2 aimOffset = Vector2.zero;
            if (mouse != null && _barcodeRoot != null)
            {
                Vector2 screen = mouse.position.ReadValue();
                if (RectTransformUtility.RectangleContainsScreenPoint(_barcodeRoot, screen))
                {
                    overBarcode = true;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(_barcodeRoot, screen, null, out Vector2 local);
                    Rect rect = _barcodeRoot.rect;
                    aimOffset = new Vector2(
                        Mathf.Clamp((local.x - rect.center.x) / (rect.width * 0.5f), -1f, 1f),
                        Mathf.Clamp((local.y - rect.center.y) / (rect.height * 0.5f), -1f, 1f));
                }
            }
            if (overBarcode && !_aiming) BeginAim();
            else if (!overBarcode && _aiming) EndAim();
            if (_aiming && PhoneView.Instance != null)
            {
                PhoneView.Instance.UpdateBarcodeAim(aimOffset);

                // S-073 ② — 중앙 조준을 잠깐 유지하면 클릭 없이 자동 촬영 (스치는 오발 방지 0.3초).
                if (PhoneView.Instance.IsAimCentered)
                {
                    _aimHoldTime += Time.unscaledDeltaTime;
                    if (_aimHoldTime >= AUTO_SHOOT_HOLD_SECONDS && PhoneView.Instance.TryShootBarcode(_order))
                    {
                        CloseInvoice();
                        _justOpened = false;
                        return;
                    }
                }
                else _aimHoldTime = 0f;
            }

            // 닫기: ESC 또는 아무 곳 좌클릭 — 단 바코드 조준 중 좌클릭은 '스캔 촬영'이다 (S-072 ②·④).
            bool click = mouse != null && mouse.leftButton.wasPressedThisFrame && !_justOpened;
            if (_aiming && click)
            {
                if (PhoneView.Instance != null && PhoneView.Instance.TryShootBarcode(_order))
                    CloseInvoice(); // 스캔 성립 = 송장 접기
            }
            else if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || click)
            {
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                    LastEscCloseFrame = Time.frameCount; // S-101 — 이 프레임 ESC는 송장 닫기가 소비
                CloseInvoice();
            }
            _justOpened = false;
        }

        private bool _justOpened;
        private bool _aiming;
        private float _aimHoldTime; // S-073 ② — 중앙 유지 시간 (자동 촬영)
        private const float AUTO_SHOOT_HOLD_SECONDS = 0.3f;
        private DeliveryOrderSO _order;
        private Image _barcodeBackground; // 호버 하이라이트 대상 (바코드 밴드 배경)

        // S-170 — Begin/End는 이제 **조준 상태만** 토글한다. 파인더를 여닫는 건 송장 열림/닫힘 몫
        // (종전엔 여기서 Open/Close를 불러 파인더 수명이 호버에 묶여 있었다).
        private void BeginAim()
        {
            _aiming = true;
            _aimHoldTime = 0f;
            if (_barcodeBackground == null && _barcodeRoot != null)
                _barcodeBackground = _barcodeRoot.GetComponent<Image>();
            if (_barcodeBackground != null)
                _barcodeBackground.color = new Color(0.78f, 1f, 0.96f, 1f); // 시안 틴트 — 조준 중
            PhoneView.Instance?.SetBarcodeAiming(true);
        }

        private void EndAim()
        {
            _aiming = false;
            if (_barcodeBackground != null) _barcodeBackground.color = Color.white;
            PhoneView.Instance?.SetBarcodeAiming(false);
        }

        private void OnSceneLeaving(GameScene _) => CloseInvoice();

        /// <summary>
        /// S-170 — 송장을 접는 유일한 문. 파인더 수명이 송장에 묶였으므로 닫는 경로가 넷(자동 촬영·
        /// 클릭 촬영·ESC/클릭 닫기·씬 이탈)으로 흩어져 있으면 어느 하나에서 파인더가 남는다.
        /// </summary>
        private void CloseInvoice()
        {
            if (_aiming) EndAim();
            PhoneView.Instance?.CloseBarcodeAim();
            if (_root != null) _root.SetActive(false);
        }

        /// <summary>송장이 떠 있는가 — 센서가 같은 클릭으로 재요청해 '닫자마자 재열림' 되는 것을 막는다 (S-072 ④).</summary>
        public static bool IsOpen { get; private set; }

        private void LateUpdate() => IsOpen = _root != null && _root.activeSelf;

        private void OnInvoiceRequested(DeliveryOrderSO order)
        {
            if (_root == null || order == null) return;
            _root.SetActive(true);
            _justOpened = true;
            _order = order;
            IsOpen = true;

            if (_customerLabel != null)
                _customerLabel.text = "주문자  " + Customers[Mathf.Abs(order.orderId) % Customers.Length];
            if (_addressLabel != null)
                _addressLabel.text = order.address + "\n<size=70%><color=#8a93a8>" + order.district
                    + " · " + (order.floor < 0 ? "지하 " + (-order.floor) + "층" : order.floor + "층") + "</color></size>";

            if (_deadlineLabel != null)
            {
                float remaining = order.deadlineMinuteOfDay - (_gameState != null ? _gameState.minuteOfDay : 0f);
                int hour = Mathf.FloorToInt(order.deadlineMinuteOfDay / 60f);
                int minute = Mathf.FloorToInt(order.deadlineMinuteOfDay % 60f);
                string urgency = remaining < 120f ? "긴급" : remaining < 300f ? "서두를 것" : "여유";
                _deadlineLabel.text = $"마감 {hour:00}:{minute:00}  ·  {urgency}";
                _deadlineLabel.color = remaining < 120f ? URGENT_RED : remaining < 300f ? WARN_AMBER : SAFE_MINT;
            }

            if (_detailLabel != null)
                _detailLabel.text = "무게 " + order.weight.ToString("0.#") + "kg  ·  [취급주의]  ·  보상 ₩"
                    + order.reward.ToString("N0")
                    + (string.IsNullOrEmpty(order.memo) ? "" : "\n<size=80%>" + order.memo + "</size>");

            if (_barcodeNumberLabel != null)
                _barcodeNumberLabel.text = "NO. " + order.orderId.ToString("D6");
            RebuildBarcode(order.orderId);

            // S-170 — 파인더는 **송장이 열려 있는 내내** 떠 있다. 종전엔 바코드 호버에 붙어 있어
            // 마우스를 정확히 올리기 전까진 폰이 비어 있었다(남규님: 무조건 표시).
            PhoneView.Instance?.OpenBarcodeAim(order);
        }

        // orderId 유래 결정적 줄무늬 — 이미지 바 재사용(풀), 폰트 무관.
        // S-072 ① — 2패스 정규화: 랜덤 폭 총합을 밴드 폭에 맞게 스케일 (오버플로 바를 숨기던
        // 구현이 '바코드 짤림'의 원흉 — 활성 직후 rect.width 미계산 프레임도 폴백으로 방어).
        private void RebuildBarcode(int orderId)
        {
            if (_barcodeRoot == null) return;
            float width = _barcodeRoot.rect.width;
            if (width <= 1f) width = 548f; // 활성 첫 프레임 레이아웃 미계산 폴백 (송장지 620 - 여백 72)

            const int bars = 24;
            const float PAD = 10f;
            var widths = new float[bars];
            var gaps = new float[bars];
            float total = 0f;
            uint seed = (uint)(orderId * 2654435761u + 12345u);
            for (int i = 0; i < bars; i++)
            {
                seed = seed * 1664525u + 1013904223u;
                widths[i] = 3f + (seed >> 8) % 7;
                gaps[i] = 2f + (seed >> 16) % 4;
                total += widths[i] + (i < bars - 1 ? gaps[i] : 0f);
            }

            float scale = (width - PAD * 2f) / total;
            float x = PAD;
            for (int i = 0; i < bars; i++)
            {
                Image bar = i < _barcodeRoot.childCount
                    ? _barcodeRoot.GetChild(i).GetComponent<Image>()
                    : CreateBar();
                var rect = bar.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(x, 0f);
                rect.sizeDelta = new Vector2(widths[i] * scale, 0f);
                bar.gameObject.SetActive(true);
                x += (widths[i] + gaps[i]) * scale;
            }
        }

        private Image CreateBar()
        {
            GameObject go = new GameObject("Bar", typeof(RectTransform));
            go.transform.SetParent(_barcodeRoot, false);
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.09f, 0.12f, 1f);
            img.raycastTarget = false;
            return img;
        }
    }
}
