using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 스태미나와 캐리 상태. 적재 목록 자체는 GameStateSO가 소유하고,
    /// 여기서는 "지금 손에 든 한 건"만 다룬다.
    /// </summary>
    public class PlayerStatusManager : MonoBehaviour
    {
        /// <summary>이 비율 이상 변했을 때만 경계 밖으로 알린다(프레임 데이터 방지).</summary>
        private const float STAMINA_NOTIFY_STEP = 0.05f;

        [Tooltip("든 물건이 붙는 위치. 플레이어 자식 트랜스폼.")]
        [SerializeField] private Transform _carryAnchor;

        private PlayerManager _hub;
        private float _lastNotifiedStamina = -1f;
        private Transform _carriedVisual;

        public float Stamina { get; private set; }
        public float StaminaNormalized => Mathf.Clamp01(Stamina / _hub.Tuning.staminaMax);
        public DeliveryOrderSO CarriedOrder { get; private set; }
        public bool IsCarrying => CarriedOrder != null;

        private void Awake() => _hub = GetComponent<PlayerManager>();

        private void OnEnable() => WorldEvents.DeliveryFailed += OnDeliveryFailed;
        private void OnDisable() => WorldEvents.DeliveryFailed -= OnDeliveryFailed;

        private void Start()
        {
            Stamina = _hub.Tuning.staminaMax;
            NotifyStamina(force: true);
        }

        private void Update()
        {
            TuningConfigSO tuning = _hub.Tuning;

            var mouse = UnityEngine.InputSystem.Mouse.current;
            bool leftClick = mouse != null && mouse.leftButton.wasPressedThisFrame && !PhoneView.IsOpen;
            bool rightClick = mouse != null && mouse.rightButton.wasPressedThisFrame && !PhoneView.IsOpen;

            // S-032 ④: 우클릭 = 드링크 마시기 · 좌클릭 = 던지기(상자 우선, 없으면 드링크 — 택배와 동일 감각).
            if (rightClick && _heldDrink != null)
                ConsumeHeldDrink();
            if (leftClick && IsCarrying)
                ThrowCarryTowardsMouse(tuning.throwSpeed); // 던지기 (S-016 ⑦)
            else if (leftClick && _heldDrink != null)
                ThrowHeldDrink(tuning.throwSpeed);

            bool moving = _hub.Locomotion.PlanarVelocity.sqrMagnitude > 0.01f;

            if (moving)
            {
                // S-019 ③: 걷기 < 달리기, 든 상자는 무게(kg)만큼 가중.
                float drain = _hub.Input.RunHeld ? tuning.staminaDrainRunPerSecond : tuning.staminaDrainPerSecond;
                if (IsCarrying)
                {
                    drain += CarriedOrder.weight > 0f
                        ? CarriedOrder.weight * tuning.staminaDrainPerKg
                        : drain * (tuning.staminaDrainCarryMultiplier - 1f); // 무게 미지정 주문 폴백
                }
                Stamina -= drain * Time.deltaTime;
            }
            else
            {
                Stamina += tuning.staminaRecoverPerSecond * Time.deltaTime;
            }

            Stamina = Mathf.Clamp(Stamina, 0f, tuning.staminaMax);
            NotifyStamina(force: false);
        }

        public bool TryCarry(DeliveryOrderSO order)
        {
            if (IsCarrying) return false;
            CarriedOrder = order;
            WorldEvents.RaiseCarryStateChanged(true);
            return true;
        }

        public DeliveryOrderSO ReleaseCarry(bool dropAsPhysics = false)
        {
            DeliveryOrderSO released = CarriedOrder;
            CarriedOrder = null;

            if (_carriedVisual != null)
            {
                if (dropAsPhysics) DropVisualAsPhysics(_carriedVisual);
                else Destroy(_carriedVisual.gameObject);
                _carriedVisual = null;
            }

            WorldEvents.RaiseCarryStateChanged(false);
            return released;
        }

        /// <summary>
        /// 든 물건을 손에서 놓아 물리로 떨어뜨린다. S-017: PickupBox를 살려 두므로 **다시 주울 수 있고**,
        /// 굴러가 비콘 패드에 닿으면 DeliveryPoint 트리거가 배송으로 인증한다(던져 넣기).
        /// </summary>
        private void DropVisualAsPhysics(Transform visual)
        {
            visual.SetParent(null, worldPositionStays: true);

            if (visual.TryGetComponent(out Collider collider))
            {
                collider.enabled = true;
                collider.isTrigger = false;
            }

            if (visual.TryGetComponent(out Rigidbody body)) body.isKinematic = false;
            else visual.gameObject.AddComponent<Rigidbody>();
        }

        /// <summary>든 상자를 마우스가 가리키는 방향으로 던진다 (S-016 ⑦ — 물리 드롭 + 초기 속도).</summary>
        private void ThrowCarryTowardsMouse(float speed)
        {
            Camera camera = Camera.main;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (camera == null || mouse == null || _carriedVisual == null) return;

            // 마우스 레이를 플레이어 Z평면에 투영해 조준점을 얻는다 (2.5D — 깊이는 유지).
            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            Plane plane = new Plane(Vector3.back, new Vector3(0f, 0f, transform.position.z));
            if (!plane.Raycast(ray, out float enter)) return;
            Vector3 aim = ray.GetPoint(enter);

            Transform visual = _carriedVisual;
            Vector3 direction = (aim - visual.position);
            direction.z = 0f;
            direction = direction.sqrMagnitude < 0.01f ? Vector3.up : direction.normalized;

            ReleaseCarry(dropAsPhysics: true);
            if (visual.TryGetComponent(out Rigidbody body))
                body.linearVelocity = direction * speed + Vector3.up * 1.5f; // 살짝 포물선
            WorldAudioManager.Instance?.PlayThrowSfx(); // AU-008 — Instance 명령 (이벤트 없는 지점)
        }

        // ── 드링크 들기·섭취 (S-031 ⑩) ──────────────────────
        private Transform _heldDrink;

        /// <summary>드링크를 손(캐리 앵커 곁)에 붙인다. 이미 들고 있으면 거절.</summary>
        public bool TryHoldDrink(Transform visual)
        {
            if (_heldDrink != null) return false;
            _heldDrink = visual;
            visual.SetParent(_carryAnchor, false);
            visual.localPosition = new Vector3(0.35f, -0.15f, 0f); // 상자와 공존 — 옆손
            visual.localRotation = Quaternion.identity;
            return true;
        }

        private void ConsumeHeldDrink()
        {
            Destroy(_heldDrink.gameObject);
            _heldDrink = null;
            RecoverStamina(_hub.Tuning.energyDrinkRecover); // 내부에서 힐 이펙트(PlayDrinkEffect)까지 발화
            WorldAudioManager.Instance?.PlayDrinkSfx();     // AU-009
            Debug.Log("[드링크] 섭취 — 스태미나 회복 (우클릭)");
        }

        /// <summary>S-032 ④: 든 드링크를 마우스 방향으로 던진다 — 다시 픽업체가 되어 E로 회수 가능.</summary>
        private void ThrowHeldDrink(float speed)
        {
            Camera camera = Camera.main;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (camera == null || mouse == null) return;

            Transform drink = _heldDrink;
            _heldDrink = null;
            drink.SetParent(null, worldPositionStays: true);

            if (drink.TryGetComponent(out Collider collider)) collider.enabled = true;
            if (!drink.TryGetComponent(out Rigidbody body)) body = drink.gameObject.AddComponent<Rigidbody>();
            body.mass = 0.3f;
            if (drink.GetComponent<EnergyDrinkPickup>() == null) drink.gameObject.AddComponent<EnergyDrinkPickup>();

            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            Plane plane = new Plane(Vector3.back, new Vector3(0f, 0f, transform.position.z));
            Vector3 direction = Vector3.up;
            if (plane.Raycast(ray, out float enter))
            {
                direction = ray.GetPoint(enter) - drink.position;
                direction.z = 0f;
                direction = direction.sqrMagnitude < 0.01f ? Vector3.up : direction.normalized;
            }
            body.linearVelocity = direction * speed + Vector3.up * 1.5f;
            body.angularVelocity = Random.insideUnitSphere * 25f; // S-033 ③ — 캔 팽글팽글
            WorldAudioManager.Instance?.PlayThrowSfx();
            Debug.Log("[드링크] 던짐 (좌클릭) — E로 다시 주울 수 있다");
        }

        /// <summary>든 물건의 겉모습을 캐리 앵커에 붙인다. 내려놓을 때 함께 사라진다.</summary>
        public void AttachCarried(Transform visual)
        {
            _carriedVisual = visual;
            visual.SetParent(_carryAnchor, false);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
        }

        /// <summary>지각으로 실패한 건이 지금 든 것이면 손에서 내려놓는다.</summary>
        private void OnDeliveryFailed(DeliveryData data)
        {
            if (CarriedOrder == null || CarriedOrder.orderId != data.OrderId) return;
            ReleaseCarry();
        }

        public void RecoverStamina(float amount)
        {
            Stamina = Mathf.Clamp(Stamina + amount, 0f, _hub.Tuning.staminaMax);
            NotifyStamina(force: true);
            if (_hub.Effects != null) _hub.Effects.PlayDrinkEffect(); // S-023 드링크 버스트 (재조립 전 씬 대비 가드)
        }

        private void NotifyStamina(bool force)
        {
            float normalized = StaminaNormalized;
            if (!force && Mathf.Abs(normalized - _lastNotifiedStamina) < STAMINA_NOTIFY_STEP) return;
            _lastNotifiedStamina = normalized;
            WorldEvents.RaiseStaminaChanged(normalized);
        }
    }
}
