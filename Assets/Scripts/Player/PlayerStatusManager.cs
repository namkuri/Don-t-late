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

        private PlayerManager _hub;
        private float _lastNotifiedStamina = -1f;

        public float Stamina { get; private set; }
        public float StaminaNormalized => Mathf.Clamp01(Stamina / _hub.Tuning.staminaMax);
        public DeliveryOrderSO CarriedOrder { get; private set; }
        public bool IsCarrying => CarriedOrder != null;

        private void Awake() => _hub = GetComponent<PlayerManager>();

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

        public DeliveryOrderSO ReleaseCarry()
        {
            DeliveryOrderSO released = CarriedOrder;
            CarriedOrder = null;
            WorldEvents.RaiseCarryStateChanged(false);
            return released;
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
