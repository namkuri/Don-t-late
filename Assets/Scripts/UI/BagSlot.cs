using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 가방 슬롯 1칸 (S-064). 좌클릭=선택(들 수 있으면 손에), 우클릭=컨텍스트 메뉴,
    /// 드래그 드랍=칸 이동. 표시만 담당 — 데이터 조작은 BagView가 한다.
    /// </summary>
    public class BagSlot : MonoBehaviour,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private BagView _view;
        [SerializeField] private int _index;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _countLabel;
        [SerializeField] private bool _illustratedStyle;

        private static readonly Color EMPTY = new Color(0.14f, 0.17f, 0.24f, 0.9f);
        private static readonly Color FILLED = new Color(0.22f, 0.27f, 0.36f, 0.95f);
        private static readonly Color SELECTED = new Color(0.21f, 0.55f, 0.50f, 0.95f);
        private static readonly Color ART_EMPTY = new Color(0.50f, 0.78f, 0.75f, 0.04f);
        private static readonly Color ART_FILLED = new Color(0.35f, 0.67f, 0.64f, 0.16f);
        private static readonly Color ART_SELECTED = new Color(0.25f, 0.62f, 0.58f, 0.32f);

        public int Index => _index;

        public void Render(BagItem? item, bool selected)
        {
            bool has = item.HasValue;
            if (_label != null) _label.text = has ? item.Value.label : string.Empty;
            if (_countLabel != null) _countLabel.text = has && item.Value.count > 1 ? "×" + item.Value.count : string.Empty;
            if (_background != null)
                _background.color = _illustratedStyle
                    ? selected ? ART_SELECTED : has ? ART_FILLED : ART_EMPTY
                    : selected ? SELECTED : has ? FILLED : EMPTY;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_view == null) return;
            if (eventData.button == PointerEventData.InputButton.Left) _view.OnSlotLeftClick(_index);
            else if (eventData.button == PointerEventData.InputButton.Right) _view.OnSlotRightClick(_index);
        }

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
