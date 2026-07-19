using System.Collections.Generic;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// "늦지마" 압박 담당. 시계는 진행시키지 않고 GameStateSO를 읽어 마감만 판정한다.
    /// 분 경계(ClockTicked)에서만 돌므로 프레임 비용이 없다.
    /// </summary>
    public class WorldDeadlineManager : MonoBehaviour
    {
        public static WorldDeadlineManager Instance { get; private set; }

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private TuningConfigSO _tuning;

        private readonly HashSet<int> _warned = new HashSet<int>();
        private readonly HashSet<int> _failed = new HashSet<int>();
        private readonly List<DeliveryOrderSO> _judgeBuffer = new List<DeliveryOrderSO>();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            WorldEvents.ClockTicked += OnClockTicked;
            WorldEvents.DeliveryCompleted += OnDeliveryCompleted;
        }

        private void OnDisable()
        {
            WorldEvents.ClockTicked -= OnClockTicked;
            WorldEvents.DeliveryCompleted -= OnDeliveryCompleted;
        }

        /// <summary>남은 시간(분). 음수면 이미 지각.</summary>
        public float RemainingMinutes(DeliveryOrderSO order)
            => order.deadlineMinuteOfDay - _gameState.minuteOfDay;

        private void OnClockTicked(GameClock clock)
        {
            _judgeBuffer.Clear();
            _judgeBuffer.AddRange(_gameState.cargo);

            foreach (DeliveryOrderSO order in _judgeBuffer)
            {
                if (order == null) continue;

                float remaining = RemainingMinutes(order);

                if (remaining <= 0f)
                {
                    if (!_failed.Add(order.orderId)) continue;
                    WorldEvents.RaiseDeliveryFailed(DeliveryData.From(order, isLate: true));
                    continue;
                }

                if (remaining > _tuning.deadlineWarnMinutes) continue;
                if (!_warned.Add(order.orderId)) continue;

                WorldEvents.RaiseDeadlineWarned(DeliveryData.From(order));
            }
        }

        private void OnDeliveryCompleted(DeliveryData data)
        {
            _warned.Remove(data.OrderId);
            _failed.Remove(data.OrderId);
        }
    }
}
