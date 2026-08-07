using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 아파트 UI(View — S-038): 공동현관 키패드 + 엘리베이터 층 선택. 이벤트 구독·표시만 —
    /// 비번 판정은 ApartmentPasswordGate, 층 이동은 ApartmentElevator 몫.
    /// 위젯은 런타임 생성(빌더는 캔버스+뷰만).
    /// </summary>
    public class ApartmentUIView : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset _font;

        private GameObject _keypadPanel;
        private TMP_Text _keypadDisplay;
        private GameObject _floorPanel;
        private Transform _floorButtonRoot;
        private string _entry = string.Empty;

        private void OnEnable()
        {
            WorldEvents.KeypadRequested += OnKeypadRequested;
            WorldEvents.GateOpened += OnGateOpened;
            WorldEvents.KeypadRejected += OnKeypadRejected;
            WorldEvents.FloorSelectRequested += OnFloorSelectRequested;
            WorldEvents.FloorChosen += OnFloorChosen;
        }

        private void OnDisable()
        {
            WorldEvents.KeypadRequested -= OnKeypadRequested;
            WorldEvents.GateOpened -= OnGateOpened;
            WorldEvents.KeypadRejected -= OnKeypadRejected;
            WorldEvents.FloorSelectRequested -= OnFloorSelectRequested;
            WorldEvents.FloorChosen -= OnFloorChosen;
        }

        private void Start()
        {
            BuildKeypad();
            BuildFloorPanel();
        }

        // ── 키패드 ───────────────────────────────────────────
        private void OnKeypadRequested()
        {
            _entry = string.Empty;
            RefreshDisplay(string.Empty);
            _keypadPanel.SetActive(true);
        }

        private void OnGateOpened() => _keypadPanel.SetActive(false);

        private void OnKeypadRejected()
        {
            _entry = string.Empty;
            RefreshDisplay("<color=#ff7359>비번 오류</color>");
        }

        private void PressDigit(int digit)
        {
            if (_entry.Length >= 4) return;
            _entry += digit.ToString();
            RefreshDisplay(string.Empty);
            WorldAudioManager.Instance?.PlayUiTickSfx();
            if (_entry.Length == 4) WorldEvents.RaiseKeypadEntered(_entry);
        }

        private void RefreshDisplay(string message)
        {
            if (_keypadDisplay == null) return;
            string dots = string.Empty;
            for (int i = 0; i < 4; i++) dots += i < _entry.Length ? "●" : "○";
            _keypadDisplay.text = string.IsNullOrEmpty(message)
                ? "공동현관  " + dots + "\n<size=60%><color=#8a93a8>비번은 폰 배송 앱에</color></size>"
                : message;
        }

        private void BuildKeypad()
        {
            _keypadPanel = MakePanel("KeypadPanel", new Vector2(380f, 500f));
            _keypadDisplay = MakeText(_keypadPanel.transform, "Display", 30f);
            Anchor(_keypadDisplay.rectTransform, new Vector2(0f, -20f), new Vector2(340f, 90f), new Vector2(0.5f, 1f));
            RefreshDisplay(string.Empty);

            for (int d = 0; d <= 9; d++)
            {
                int digit = d;
                Button key = MakeButton(_keypadPanel.transform, "Key" + d, d.ToString(), () => PressDigit(digit));
                RectTransform rect = (RectTransform)key.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(96f, 72f);
                int row = d == 0 ? 3 : (d - 1) / 3;
                int col = d == 0 ? 1 : (d - 1) % 3;
                rect.anchoredPosition = new Vector2(-108f + col * 108f, -130f - row * 82f);
            }

            Button close = MakeButton(_keypadPanel.transform, "Close", "닫기", () => _keypadPanel.SetActive(false));
            RectTransform closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.sizeDelta = new Vector2(140f, 52f);
            closeRect.anchoredPosition = new Vector2(0f, 12f);

            _keypadPanel.SetActive(false);
        }

        // ── 층 선택 ──────────────────────────────────────────
        private void OnFloorSelectRequested(int[] floors)
        {
            for (int i = _floorButtonRoot.childCount - 1; i >= 0; i--)
                Destroy(_floorButtonRoot.GetChild(i).gameObject);

            for (int i = 0; i < floors.Length; i++)
            {
                int floor = floors[i];
                Button b = MakeButton(_floorButtonRoot, "Floor" + floor,
                    floor == 1 ? "1층 (로비)" : floor + "층", () => WorldEvents.RaiseFloorChosen(floor));
                RectTransform rect = (RectTransform)b.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(300f, 62f);
                rect.anchoredPosition = new Vector2(0f, -84f - i * 72f);
            }
            _floorPanel.SetActive(true);
        }

        private void OnFloorChosen(int _) => _floorPanel.SetActive(false);

        private void BuildFloorPanel()
        {
            _floorPanel = MakePanel("FloorPanel", new Vector2(360f, 420f));
            TMP_Text title = MakeText(_floorPanel.transform, "Title", 32f);
            title.text = "엘리베이터 — 층 선택";
            Anchor(title.rectTransform, new Vector2(0f, -18f), new Vector2(330f, 50f), new Vector2(0.5f, 1f));

            GameObject root = new GameObject("Buttons", typeof(RectTransform));
            root.transform.SetParent(_floorPanel.transform, false);
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            _floorButtonRoot = root.transform;

            _floorPanel.SetActive(false);
        }

        // ── 위젯 헬퍼 (그레이박스 톤 — 시안 테두리 + 네이비) ──
        private GameObject MakePanel(string name, Vector2 size)
        {
            GameObject border = new GameObject(name, typeof(RectTransform));
            border.transform.SetParent(transform, false);
            Image borderImage = border.AddComponent<Image>();
            borderImage.color = new Color(0.208f, 0.878f, 0.784f, 1f);
            RectTransform rect = (RectTransform)border.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            GameObject inner = new GameObject("Inner", typeof(RectTransform));
            inner.transform.SetParent(border.transform, false);
            Image innerImage = inner.AddComponent<Image>();
            innerImage.color = new Color(0.039f, 0.051f, 0.086f, 0.96f);
            RectTransform innerRect = (RectTransform)inner.transform;
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(3f, 3f);
            innerRect.offsetMax = new Vector2(-3f, -3f);
            return border;
        }

        private TMP_Text MakeText(Transform parent, string name, float size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) label.font = _font;
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        private void Anchor(RectTransform rect, Vector2 pos, Vector2 size, Vector2 anchor)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
        }

        private Button MakeButton(Transform parent, string name, string label, System.Action onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.13f, 0.18f, 0.26f, 0.95f);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() => onClick());
            TMP_Text text = MakeText(go.transform, "Label", 28f);
            text.text = label;
            text.color = new Color(0.208f, 0.878f, 0.784f);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            return button;
        }
    }
}
