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

        /// <summary>S-124 — 런타임 생성물(거리 자판기)용 배선. 빌더 주입이 불가한 경로에서 쓴다.</summary>
        public void Configure(TuningConfigSO tuning, Material drinkMaterial, Material highlightMaterial, Renderer renderer)
        {
            _tuning = tuning;
            _drinkMaterial = drinkMaterial;
            _highlightMaterial = highlightMaterial;
            _renderer = renderer;
            if (_renderer != null) _normalMaterial = _renderer.sharedMaterial;
        }

        // S-125 ② — E는 즉시 결제가 아니라 **구매창**을 연다(남규님 요청). 상자 투척 배출(아래)은
        // 그대로 — 자판기의 재미 요소라 유지한다.
        public void Interact(PlayerContext ctx)
        {
            int price = _tuning != null ? _tuning.vendingPrice : 1000;
            WorldEvents.RaiseKioskRequested(new KioskOffer
            {
                Title = "자판기",
                Items = new[]
                {
                    new KioskItem { id = "drink", label = "에너지드링크", price = price },
                    new KioskItem { id = "water", label = "생수 (더위↓)", price = 800 },
                    new KioskItem { id = "hot_drink", label = "코코아 (추위↓)", price = 1200 },
                },
            });
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
            WorldAudioManager.Instance?.PlayVendingSfx(); // AU-008 — 결제·명중 배출 공용
            // S-031 ⑩: 물리로 굴러떨어진다 → E로 잡고 → 좌클릭으로 마신다 (EnergyDrinkPickup).
            GameObject drink = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            drink.name = "VendedDrink";
            drink.transform.position = transform.position + new Vector3(0f, 0.7f, -0.7f);
            drink.transform.localScale = new Vector3(0.22f, 0.25f, 0.22f);
            if (_drinkMaterial != null) drink.GetComponent<Renderer>().sharedMaterial = _drinkMaterial;

            Rigidbody body = drink.AddComponent<Rigidbody>();
            body.mass = 0.3f;
            body.linearVelocity = new Vector3(Random.Range(-0.4f, 0.4f), 0.5f, -1.6f); // 배출구에서 톡 굴러나옴

            drink.AddComponent<EnergyDrinkPickup>();
        }
    }
}
