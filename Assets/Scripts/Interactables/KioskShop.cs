using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 노점·자판기 (S-125 ② — 남규님 요청 "별도 구매 UI"). E로 말을 걸면 구매창을 연다.
    /// 판매 목록은 가게마다 다르고, 실제 결제·가방 적재는 <see cref="KioskView"/>가 처리한다.
    /// 이 컴포넌트는 "무엇을 파는 가게인가"만 들고 있는다(표시·결제는 UI 층 몫 — 뷰 규칙).
    /// </summary>
    public class KioskShop : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _title = "자판기";
        [SerializeField] private string[] _itemIds;
        [SerializeField] private string[] _itemLabels;
        [SerializeField] private int[] _itemPrices;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _highlightMaterial;
        private Material _normalMaterial;

        private void Awake()
        {
            if (_renderer != null) _normalMaterial = _renderer.sharedMaterial;
        }

        public void Interact(PlayerContext ctx)
        {
            int count = _itemIds != null ? _itemIds.Length : 0;
            if (count == 0) return;

            var items = new KioskItem[count];
            for (int i = 0; i < count; i++)
            {
                items[i] = new KioskItem
                {
                    id = _itemIds[i],
                    label = _itemLabels != null && i < _itemLabels.Length ? _itemLabels[i] : _itemIds[i],
                    price = _itemPrices != null && i < _itemPrices.Length ? _itemPrices[i] : 1000,
                };
            }
            WorldEvents.RaiseKioskRequested(new KioskOffer { Title = _title, Items = items });
        }

        public void SetHighlight(bool on)
        {
            if (_renderer == null) return;
            if (_normalMaterial == null) _normalMaterial = _renderer.sharedMaterial;
            _renderer.sharedMaterial = on && _highlightMaterial != null ? _highlightMaterial : _normalMaterial;
        }
    }
}
