using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 가방 슬롯 1칸 (S-064). 좌클릭=사용/들기(S-205), 우클릭=컨텍스트 메뉴(버리기),
    /// 드래그 드랍=칸 이동. 표시만 담당 — 데이터 조작은 BagView가 한다.
    /// </summary>
    public class BagSlot : MonoBehaviour,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private BagView _view;
        [SerializeField] private int _index;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _countLabel;
        [SerializeField] private RawImage _icon;
        [SerializeField] private Texture2D _drinkIcon;
        [SerializeField] private Texture2D _waterIcon;
        [SerializeField] private Texture2D _cocoaIcon;
        [SerializeField] private Texture2D _odengIcon;
        [SerializeField] private Texture2D _flowerIcon;
        [SerializeField] private bool _illustratedStyle;

        private static readonly Color EMPTY = new Color(0.14f, 0.17f, 0.24f, 0.9f);
        private static readonly Color FILLED = new Color(0.22f, 0.27f, 0.36f, 0.95f);
        private static readonly Color SELECTED = new Color(0.21f, 0.55f, 0.50f, 0.95f);
        private static readonly Color ART_EMPTY = Color.white;
        private static readonly Color ART_FILLED = new Color(0.94f, 1f, 0.98f, 1f);
        private static readonly Color ART_SELECTED = new Color(0.72f, 0.94f, 0.90f, 1f);

        public int Index => _index;

        public void Render(BagItem? item, bool selected)
        {
            bool has = item.HasValue;
            bool hasIcon = has && TryRenderIcon(item.Value);
            if (_label != null) _label.text = has && !hasIcon ? item.Value.label : string.Empty;
            if (_countLabel != null) _countLabel.text = has && item.Value.count > 1 ? "×" + item.Value.count : string.Empty;
            if (_icon != null) _icon.enabled = hasIcon;
            if (_background != null)
                _background.color = _illustratedStyle
                    ? selected ? ART_SELECTED : has ? ART_FILLED : ART_EMPTY
                    : selected ? SELECTED : has ? FILLED : EMPTY;
        }

        private bool TryRenderIcon(BagItem item)
        {
            if (_icon == null) return false;

            Texture2D texture;
            Rect uv;
            float aspect;
            switch (item.id)
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
                case "hot_drink" when item.label != null && item.label.Contains("코코아"):
                    texture = _cocoaIcon;
                    uv = new Rect(0.35872f, 0.26270f, 0.31641f, 0.55176f);
                    aspect = 486f / 565f;
                    break;
                case "hot_drink" when item.label != null && item.label.Contains("어묵"):
                    texture = _odengIcon;
                    uv = new Rect(0.36784f, 0.19727f, 0.27214f, 0.68848f);
                    aspect = 418f / 705f;
                    break;
                case "flower":
                    texture = _flowerIcon;
                    uv = new Rect(0.37240f, 0.24707f, 0.28060f, 0.52930f);
                    aspect = 431f / 542f;
                    break;
                default:
                    return false;
            }

            if (texture == null) return false;
            _icon.texture = texture;
            _icon.uvRect = uv;
            _icon.color = Color.white;
            _icon.rectTransform.sizeDelta = new Vector2(84f * aspect, 84f);
            return true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_view == null) return;
            if (eventData.button == PointerEventData.InputButton.Left) _view.OnSlotLeftClick(_index);
            else if (eventData.button == PointerEventData.InputButton.Right) _view.OnSlotRightClick(_index);
        }

        // S-205 — 좌클릭 즉시 사용을 알려 주는 호버 툴팁. 조작이 바뀌었는데 화면에 아무 안내가
        // 없으면 플레이어는 여전히 우클릭을 찾는다. 문구 판단은 BagView가 한다(아이템 종류를 안다).
        public void OnPointerEnter(PointerEventData eventData) => _view?.OnSlotHover(_index, true);
        public void OnPointerExit(PointerEventData eventData) => _view?.OnSlotHover(_index, false);

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_background != null)
                _background.color = (_illustratedStyle ? ART_SELECTED : SELECTED) * 0.8f;
        }

        public void OnDrag(PointerEventData eventData) { } // 고스트 없이 — 드랍 대상 하이라이트로 충분 (그레이박스)

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_view != null) _view.Refresh();
        }

        public void OnDrop(PointerEventData eventData)
        {
            BagSlot source = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<BagSlot>() : null;
            if (source == null || source == this || _view == null) return;
            _view.OnSlotDropped(source.Index, _index);
        }
    }
}
