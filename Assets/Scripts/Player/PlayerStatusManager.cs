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
            bool moving = _hub.Locomotion.PlanarVelocity.sqrMagnitude > 0.01f;

            if (moving)
            {
                float drain = tuning.staminaDrainPerSecond;
                if (IsCarrying) drain *= tuning.staminaDrainCarryMultiplier;
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

        /// <summary>든 물건을 마지막 월드 위치·회전 그대로 손에서 놓아 물리로 떨어뜨린다(재픽업 불가).</summary>
        private void DropVisualAsPhysics(Transform visual)
        {
            visual.SetParent(null, worldPositionStays: true);

            if (visual.TryGetComponent(out PickupBox pickup)) Destroy(pickup);

            if (visual.TryGetComponent(out Collider collider))
            {
                collider.enabled = true;
                collider.isTrigger = false;
            }

            if (!visual.TryGetComponent(out Rigidbody _)) visual.gameObject.AddComponent<Rigidbody>();
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
