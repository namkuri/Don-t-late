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
        }

        private void Update()
        {
            if (_root == null || !_root.activeSelf) return;
            // 닫기: ESC 또는 아무 곳 좌클릭 (열리게 한 클릭과 같은 프레임은 wasPressed 중복 없음 — 다음 클릭부터).
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                || (mouse != null && mouse.leftButton.wasPressedThisFrame && !_justOpened))
                _root.SetActive(false);
            _justOpened = false;
        }

        private bool _justOpened;

        private void OnSceneLeaving(GameScene _)
        {
            if (_root != null) _root.SetActive(false);
        }

        private void OnInvoiceRequested(DeliveryOrderSO order)
        {
            if (_root == null || order == null) return;
            _root.SetActive(true);
            _justOpened = true;

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
        }

        // orderId 유래 결정적 줄무늬 — 이미지 바 재사용(풀), 폰트 무관.
        private void RebuildBarcode(int orderId)
        {
            if (_barcodeRoot == null) return;
            float width = _barcodeRoot.rect.width;
            int bars = 24;
            uint seed = (uint)(orderId * 2654435761u + 12345u);
            float x = 0f;
            for (int i = 0; i < bars; i++)
            {
                seed = seed * 1664525u + 1013904223u;
                float barWidth = 3f + (seed >> 8) % 7;
                Image bar = i < _barcodeRoot.childCount
                    ? _barcodeRoot.GetChild(i).GetComponent<Image>()
                    : CreateBar();
                var rect = bar.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(x, 0f);
                rect.sizeDelta = new Vector2(barWidth, 0f);
                bar.gameObject.SetActive(x + barWidth <= width);
                x += barWidth + 2f + (seed >> 16) % 4;
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
