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
        [Tooltip("S-124 — 힌트 줄(꽃 선물·호감도) 표시용. 비면 이름만 뜬다.")]
        [SerializeField] private GameStateSO _gameState;
        [Tooltip("호감도 조회용 NPC id (비면 수치 생략).")]
        [SerializeField] private string _npcId;

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
            _label.text = _displayName + HintLine();
        }

        // S-124 — 조작 가시성: "되는데 모르겠다"는 안 된 것과 같다(남규님 판정).
        // 이름 아래 한 줄로 지금 가능한 상호작용을 알린다. 가방에 꽃이 있으면 선물 안내가 우선.
        private string HintLine()
        {
            // 꽃 선물 분기는 PedestrianNpc.Interact에만 있다 — 할머니·사장님에 거짓 힌트를 띄우지 않게
            // "npcId 유무"가 아니라 "선물 받을 수 있는 컴포넌트인가"로 가른다.
            if (_giftTarget != null && _gameState != null && _gameState.bagItems.Exists(b => b.id == GIFT_ITEM_ID))
                return "\n<size=70%><color=#ffd45e>E — 꽃 선물 (호감도 +25)</color></size>";
            if (_gameState != null && !string.IsNullOrEmpty(_npcId))
            {
                int affinity = NpcAffinityLedger.Get(_gameState, _npcId);
                return "\n<size=70%><color=#8fe3d5>E — 인사   호감도 " + affinity + "/100</color></size>";
            }
            return "\n<size=70%><color=#8fe3d5>E — 인사</color></size>";
        }

        private const string GIFT_ITEM_ID = "flower";
        private PedestrianNpc _giftTarget;

        private void Awake() => _giftTarget = GetComponent<PedestrianNpc>();

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
