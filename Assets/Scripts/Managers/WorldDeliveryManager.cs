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

        /// <summary>이동맵 노드 선택 시 목적 구역 기록 (S-015). District 스포너가 이 값으로 짐·비콘을 깐다.</summary>
        public void SetDestination(string district)
        {
            _gameState.currentDistrict = district;
        }

        /// <summary>폰 바코드 스캔 등록 (S-011). 이미 등록된 건이면 false — 호출자가 경고 표시.</summary>
        public bool RegisterBarcode(DeliveryOrderSO order)
        {
            if (_gameState.scannedOrderIds.Contains(order.orderId)) return false;
            _gameState.scannedOrderIds.Add(order.orderId);
            WorldEvents.RaiseBarcodeScanned(DeliveryData.From(order));
            return true;
        }

        public bool IsScanned(DeliveryOrderSO order) => _gameState.scannedOrderIds.Contains(order.orderId);

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
            _gameState.totalEarned += order.reward;
            _gameState.deliveryHistory.Add(new DeliveryRecord // 택배앱 히스토리 (S-019)
            {
                orderId = order.orderId,
                address = order.address,
                reward = order.reward,
                day = _gameState.day,
                minuteOfDay = Mathf.FloorToInt(_gameState.minuteOfDay)
            });
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
