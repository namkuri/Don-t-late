using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [Tooltip("NPC 정보 표시용 말풍선 배경.")]
        [SerializeField] private Sprite _backgroundSprite;
        [Tooltip("NPC 상호작용 안내 문구용 Ramche 폰트.")]
        [SerializeField] private TMP_FontAsset _hintFont;

        private GameObject _canvasGo;
        private RectTransform _bubbleRect;
        private TMP_Text _nameLabel;
        private TMP_Text _affinityLabel;
        private TMP_Text _interactionLabel;

        public void Show(bool on)
        {
            if (!on)
            {
                if (_canvasGo != null)
                {
                    Destroy(_canvasGo);
                    _canvasGo = null;
                    _bubbleRect = null;
                    _nameLabel = null;
                    _affinityLabel = null;
                    _interactionLabel = null;
                }
                return;
            }
            if (_canvasGo != null || string.IsNullOrEmpty(_displayName)) return;

            _canvasGo = new GameObject("NameCanvas");
            Canvas canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6;

            if (_backgroundSprite != null)
            {
                Image bubble = new GameObject("NpcInfo", typeof(RectTransform)).AddComponent<Image>();
                bubble.transform.SetParent(_canvasGo.transform, false);
                bubble.sprite = _backgroundSprite;
                bubble.preserveAspect = true;
                bubble.raycastTarget = false;
                bubble.color = new Color(1f, 1f, 1f, 0.5f);
                _bubbleRect = bubble.rectTransform;
                _bubbleRect.sizeDelta = new Vector2(220f, 146.5f);
            }

            Color darkBrown = new Color(0.23f, 0.20f, 0.16f, 1f);

            _nameLabel = new GameObject("Name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _nameLabel.transform.SetParent(_canvasGo.transform, false);
            if (UiOverlayFont.Korean != null) _nameLabel.font = UiOverlayFont.Korean;
            _nameLabel.fontSize = 19.2f;
            _nameLabel.fontStyle = FontStyles.Bold;
            _nameLabel.color = darkBrown;
            _nameLabel.alignment = TextAlignmentOptions.Center;
            _nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            _nameLabel.raycastTarget = false;
            _nameLabel.rectTransform.sizeDelta = new Vector2(220f, 34f);
            _nameLabel.text = _displayName;

            _affinityLabel = new GameObject("Affinity", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _affinityLabel.transform.SetParent(_canvasGo.transform, false);
            _affinityLabel.font = _hintFont != null ? _hintFont : UiOverlayFont.Korean;
            _affinityLabel.fontSize = 14.4f;
            _affinityLabel.fontStyle = FontStyles.Normal;
            _affinityLabel.color = darkBrown;
            _affinityLabel.alignment = TextAlignmentOptions.Center;
            _affinityLabel.textWrappingMode = TextWrappingModes.NoWrap;
            _affinityLabel.raycastTarget = false;
            _affinityLabel.rectTransform.sizeDelta = new Vector2(220f, 28f);
            _affinityLabel.text = AffinityLine();

            _interactionLabel = new GameObject("Interaction", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _interactionLabel.transform.SetParent(_canvasGo.transform, false);
            _interactionLabel.font = _hintFont != null ? _hintFont : UiOverlayFont.Korean;
            _interactionLabel.fontSize = 19f;
            _interactionLabel.fontStyle = FontStyles.Bold;
            _interactionLabel.color = new Color(0.56f, 0.89f, 0.84f, 1f);
            _interactionLabel.alignment = TextAlignmentOptions.Center;
            _interactionLabel.textWrappingMode = TextWrappingModes.NoWrap;
            _interactionLabel.raycastTarget = false;
            _interactionLabel.rectTransform.sizeDelta = new Vector2(140f, 30f);
            _interactionLabel.text = InteractionLine();
        }

        private string AffinityLine()
        {
            if (_gameState == null || string.IsNullOrEmpty(_npcId)) return string.Empty;
            return "호감도 " + NpcAffinityLedger.Get(_gameState, _npcId) + "/100";
        }

        // S-124 — 말풍선 밖에서 지금 가능한 상호작용을 알린다. 꽃이 있으면 선물 안내가 우선.
        private string InteractionLine()
        {
            if (_giftTarget != null && _gameState != null && _gameState.bagItems.Exists(b => b.id == GIFT_ITEM_ID))
                return "E — 꽃 선물 (호감도 +25)";
            return "E — 인사";
        }

        private const string GIFT_ITEM_ID = "flower";
        private PedestrianNpc _giftTarget;

        private void Awake() => _giftTarget = GetComponent<PedestrianNpc>();

        private void LateUpdate()
        {
            if (_nameLabel == null || _affinityLabel == null || _interactionLabel == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;
            Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * _headHeight);
            if (screen.z <= 0f) return;
            if (_bubbleRect != null) _bubbleRect.position = new Vector3(screen.x, screen.y, 0f);
            _nameLabel.rectTransform.position = new Vector3(screen.x, screen.y + 15f, 0f);
            _affinityLabel.rectTransform.position = new Vector3(screen.x, screen.y - 7f, 0f);
            _interactionLabel.rectTransform.position = new Vector3(screen.x + 110f, screen.y - 67f, 0f);
        }

        private void OnDestroy()
        {
            if (_canvasGo != null) Destroy(_canvasGo);
        }
    }
}
