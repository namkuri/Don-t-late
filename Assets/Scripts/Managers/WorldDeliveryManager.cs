using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 배송 수명주기: 수주 → 픽업 → 운반 → 인증 → 완료/실패.
    /// 적재 목록의 원본은 GameStateSO.cargo이며 여기서만 갱신한다.
    /// </summary>
    public class WorldDeliveryManager : MonoBehaviour
    {
        public static WorldDeliveryManager Instance { get; private set; }

        [SerializeField] private GameStateSO _gameState;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable() => WorldEvents.DeliveryFailed += OnDeliveryFailed;
        private void OnDisable() => WorldEvents.DeliveryFailed -= OnDeliveryFailed;

        /// <summary>Camp에서 짐을 받는다.</summary>
        public void AcceptOrder(DeliveryOrderSO order)
        {
            if (_gameState.cargo.Contains(order)) return;

            _gameState.cargo.Add(order);
            WorldEvents.RaiseOrderAccepted(DeliveryData.From(order));
        }

        /// <summary>상자를 실제로 손에 들었을 때 PickupBox가 알린다.</summary>
        public void NotifyPickedUp(DeliveryOrderSO order)
        {
            WorldEvents.RaisePackagePickedUp(DeliveryData.From(order));
        }

        /// <summary>문 앞 인증 성공. 보상을 지급하고 적재에서 제거한다.</summary>
        public void CompleteDelivery(DeliveryOrderSO order)
        {
            if (!_gameState.cargo.Remove(order)) return;

            _gameState.money += order.reward;
            _gameState.completedCount++;
            WorldEvents.RaiseDeliveryCompleted(DeliveryData.From(order));
        }

        public bool IsInCargo(DeliveryOrderSO order) => _gameState.cargo.Contains(order);

        private void OnDeliveryFailed(DeliveryData data)
        {
            int index = _gameState.cargo.FindIndex(o => o != null && o.orderId == data.OrderId);
            if (index < 0) return;

            _gameState.cargo.RemoveAt(index);
            _gameState.lateCount++;
        }
    }
}
