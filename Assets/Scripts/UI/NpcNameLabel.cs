using TMPro;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// NPC 근접 이름표 (S-120 — 남규님 발주, 매니페스트 직교 추가). 상호작용 범위에 들어와
    /// 포커스가 잡히면(NPC의 SetHighlight(true)) 머리 위에 이름을 띄우고, 벗어나면 걷는다.
    /// 렌더 패턴 = PedestrianNpc 인사말 오버레이(BoxDurability HP바 초경량판)와 동일.
    /// </summary>
    public class NpcNameLabel : MonoBehaviour
    {
        [SerializeField] private string _displayName;
        [Tooltip("머리 위 오프셋(u) — 인체 1.7u 기준 정수리 위.")]
        [SerializeField] private float _headHeight = 2.0f;

        private GameObject _canvasGo;
        private TMP_Text _label;

        public void Show(bool on)
        {
            if (!on)
            {
                if (_canvasGo != null) { Destroy(_canvasGo); _canvasGo = null; _label = null; }
                return;
            }
            if (_canvasGo != null || string.IsNullOrEmpty(_displayName)) return;

            _canvasGo = new GameObject("NameCanvas");
            Canvas canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6;
            _label = new GameObject("Name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _label.transform.SetParent(_canvasGo.transform, false);
            if (UiOverlayFont.Korean != null) _label.font = UiOverlayFont.Korean;
            _label.fontSize = 24f;
            _label.fontStyle = FontStyles.Bold;
            _label.color = new Color(0.78f, 1f, 0.96f, 1f); // 상호작용 시안 계열 — 하이라이트와 통일
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.raycastTarget = false;
            _label.rectTransform.sizeDelta = new Vector2(300f, 32f);
            _label.text = _displayName;
        }

        private void LateUpdate()
        {
            if (_label == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;
            Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * _headHeight);
            if (screen.z > 0f) _label.rectTransform.position = new Vector3(screen.x, screen.y, 0f);
        }

        private void OnDestroy()
        {
            if (_canvasGo != null) Destroy(_canvasGo);
        }
    }
}
