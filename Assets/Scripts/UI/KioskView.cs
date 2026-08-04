using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 노점 구매창 (S-125 ② — 자판기·편의점·포장마차 공용). KioskRequested를 받아 품목을 깔고,
    /// 구매하면 결제(WorldDebtManager) → 가방 적재(BagStorage). ESC·닫기로 닫는다.
    /// 표시·입력만 담당한다 — 무엇을 파는지는 세계 쪽(KioskShop)이 정한다.
    /// </summary>
    public class KioskView : MonoBehaviour
    {
        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _moneyLabel;
        [SerializeField] private RectTransform _listRoot;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Texture2D _drinkIcon;
        [SerializeField] private Texture2D _waterIcon;
        [SerializeField] private Texture2D _cocoaIcon;

        /// <summary>구매창이 떠 있는가 — 다른 클릭 소비자(던지기·철거)가 양보하는 데 쓴다.</summary>
        public static bool IsOpen { get; private set; }

        private KioskOffer _offer;

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnEnable()
        {
            WorldEvents.KioskRequested += OnKioskRequested;
            WorldEvents.SceneTransitionStarted += OnSceneLeaving;
        }

        private void OnDisable()
        {
            WorldEvents.KioskRequested -= OnKioskRequested;
            WorldEvents.SceneTransitionStarted -= OnSceneLeaving;
            IsOpen = false;
        }

        private void OnSceneLeaving(GameScene _) => Close();

        private void Update()
        {
            if (!IsOpen) return;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
        }

        private void OnKioskRequested(KioskOffer offer)
        {
            if (_panel == null || _listRoot == null) return;
            _offer = offer;
            _panel.SetActive(true);
            IsOpen = true;
            if (_titleLabel != null) _titleLabel.text = offer.Title;
            Rebuild();
        }

        private void Close()
        {
            if (_panel != null) _panel.SetActive(false);
            IsOpen = false;
        }

        private void Rebuild()
        {
            for (int i = _listRoot.childCount - 1; i >= 0; i--)
                Destroy(_listRoot.GetChild(i).gameObject);

            RefreshMoney();
            if (_offer.Items == null) return;

            bool illustrated = _panel != null
                && _panel.TryGetComponent(out Image panelImage)
                && panelImage.sprite != null;
            bool showVendingIcons = illustrated && _offer.Title == "자판기";
            float rowHeight = illustrated ? 105f : 74f;
            for (int i = 0; i < _offer.Items.Length; i++)
            {
                KioskItem item = _offer.Items[i];
                GameObject row = new GameObject("Kiosk_" + item.id, typeof(RectTransform));
                row.transform.SetParent(_listRoot, false);
                Image bg = row.AddComponent<Image>();
                bg.color = illustrated ? Color.clear : new Color(0.12f, 0.15f, 0.22f, 0.95f);
                RectTransform rowRect = (RectTransform)row.transform;
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.sizeDelta = new Vector2(-24f, rowHeight - 10f);
                rowRect.anchoredPosition = new Vector2(0f, -8f - i * rowHeight);

                string priceColor = illustrated ? "#437d7c" : "#8fe3d5";
                TMP_Text label = MakeText(row.transform, "Label",
                    item.label + "\n<size=70%><color=" + priceColor + ">₩" +
                    item.price.ToString("N0") + "</color></size>",
                    30f, illustrated ? new Color(0.13f, 0.19f, 0.22f) : Color.white,
                    TextAlignmentOptions.Left);
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                // 일러스트의 왼쪽 정사각형은 상품 아이콘 전용 영역으로 비워 둔다.
                labelRect.offsetMin = new Vector2(illustrated ? 112f : 20f, 4f);
                labelRect.offsetMax = new Vector2(-150f, -4f);

                if (showVendingIcons)
                    MakeVendingIcon(row.transform, item.id);

                KioskItem captured = item;
                Button buy = MakeButton(row.transform, "Buy", "구매", () => Purchase(captured), illustrated);
                RectTransform buyRect = (RectTransform)buy.transform;
                buyRect.anchorMin = buyRect.anchorMax = buyRect.pivot = new Vector2(1f, 0.5f);
                buyRect.sizeDelta = new Vector2(120f, 52f);
                buyRect.anchoredPosition = new Vector2(-14f, 0f);
            }
        }

        private void Purchase(KioskItem item)
        {
            if (WorldDebtManager.Instance == null) return;
            if (!WorldDebtManager.Instance.TrySpend(item.price))
            {
                if (_moneyLabel != null)
                    _moneyLabel.text = "<color=#ff7359>잔액 부족 — ₩" + item.price.ToString("N0") + " 필요</color>";
                WorldAudioManager.Instance?.PlayUiTickSfx();
                return;
            }

            // 소모품은 가방으로 — 쇼핑앱과 같은 경로(중복 구현 금지).
            BagStorage.TryAdd(_gameState, item.id, item.label, true, item.id == "drink");
            BagView.Instance?.Refresh();
            WorldEvents.RaiseItemAcquired(WorldEvents.AcquiredMessage(item.label)); // S-133 ⑤
            WorldAudioManager.Instance?.PlayUiTickSfx();
            RefreshMoney();
        }

        private void RefreshMoney()
        {
            if (_moneyLabel != null && _gameState != null)
                _moneyLabel.text = "소지금 ₩" + _gameState.money.ToString("N0");
        }

        // ── 로컬 UI 헬퍼 (뷰 전용 — 빌더 헬퍼를 런타임에서 못 쓴다) ──

        private TMP_Text MakeText(Transform parent, string name, string text, float size,
            Color color, TextAlignmentOptions alignment)
        {
            var label = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(parent, false);
            if (_font != null) label.font = _font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        private void MakeVendingIcon(Transform parent, string itemId)
        {
            Texture2D texture;
            Rect uv;
            float aspect;
            switch (itemId)
            {
                case "drink":
                    texture = _drinkIcon;
                    uv = new Rect(0.37695f, 0.23438f, 0.24544f, 0.55664f);
                    aspect = 377f / 570f;
                    break;
                case "water":
                    texture = _waterIcon;
                    uv = new Rect(0.42448f, 0.25977f, 0.14974f, 0.52344f);
                    aspect = 230f / 536f;
                    break;
                case "hot_drink":
                    texture = _cocoaIcon;
                    uv = new Rect(0.35872f, 0.26270f, 0.31641f, 0.55176f);
                    aspect = 486f / 565f;
                    break;
                default:
                    return;
            }

            if (texture == null) return;
            GameObject iconGo = new GameObject("Icon_" + itemId, typeof(RectTransform));
            iconGo.transform.SetParent(parent, false);
            RawImage icon = iconGo.AddComponent<RawImage>();
            icon.texture = texture;
            icon.uvRect = uv;
            icon.color = Color.white;
            icon.raycastTarget = false;

            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(72f * aspect, 72f);
            iconRect.anchoredPosition = new Vector2(48f, 0f);
        }

        private Button MakeButton(Transform parent, string name, string text,
            UnityEngine.Events.UnityAction action, bool illustrated)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = illustrated
                ? new Color(1f, 1f, 1f, 0.001f)
                : new Color(0.208f, 0.878f, 0.784f, 1f);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            TMP_Text label = MakeText(go.transform, "Label", text, 26f,
                illustrated ? new Color(0.13f, 0.25f, 0.27f) : new Color(0.06f, 0.09f, 0.14f),
                TextAlignmentOptions.Center);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            return button;
        }
    }
}
