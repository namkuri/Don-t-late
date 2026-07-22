using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 자판기 (S-019 ②). E = 1,000원 결제 → 드링크 배출. 택배상자를 던져 맞혀도 배출된다(공짜 — 낭만).
    /// 배출된 드링크는 EnergyDrinkPickup — E로 마신다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class VendingMachine : MonoBehaviour, IInteractable
    {
        [SerializeField] private TuningConfigSO _tuning;
        [SerializeField] private Material _drinkMaterial;
        [SerializeField] private Material _highlightMaterial;
        [SerializeField] private Renderer _renderer;
        private Material _normalMaterial;

        private void Awake()
        {
            if (_renderer != null) _normalMaterial = _renderer.sharedMaterial;
        }

        public void Interact(PlayerContext ctx)
        {
            if (WorldDebtManager.Instance == null) return;
            int price = _tuning != null ? _tuning.vendingPrice : 1000;
            if (!WorldDebtManager.Instance.TrySpend(price))
            {
                Debug.Log("[자판기] 잔액 부족 — " + price.ToString("N0") + "원 필요.");
                return;
            }
            Debug.Log("[자판기] " + price.ToString("N0") + "원 결제 — 드링크 배출.");
            DispenseDrink();
        }

        public void SetHighlight(bool on)
        {
            if (_renderer == null) return;
            _renderer.sharedMaterial = on && _highlightMaterial != null ? _highlightMaterial : _normalMaterial;
        }

        /// <summary>상자 투척 명중 → 배출 (물리 상자만 — 충돌 속도 약간 요구).</summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude < 2f) return;
            if (collision.collider.GetComponent<PickupBox>() == null) return;
            Debug.Log("[자판기] 쿵! 상자 명중 — 드링크가 굴러떨어진다.");
            DispenseDrink();
        }

        private void DispenseDrink()
        {
            GameObject drink = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            drink.name = "VendedDrink";
            drink.transform.position = transform.position + new Vector3(0f, 0.3f, -0.8f);
            drink.transform.localScale = new Vector3(0.22f, 0.25f, 0.22f);
            var collider = drink.GetComponent<Collider>();
            collider.isTrigger = true;
            if (_drinkMaterial != null) drink.GetComponent<Renderer>().sharedMaterial = _drinkMaterial;

            var pickup = drink.AddComponent<EnergyDrinkPickup>();
            // 드링크 하이라이트는 머티리얼 스왑 — 배출품이라 리플렉션 배선 대신 그대로 둔다(포커스 시 무변화 허용).
        }
    }
}
