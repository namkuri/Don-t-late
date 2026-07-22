using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 캠프 주문 갱신 (S-021 ③). Camp 씬이 열릴 때(=복귀 포함) 각 상자의 주문을 점검해,
    /// 소진된 건(배송 완료 또는 마감 경과·미적재)은 **새 목적지의 런타임 주문**으로 교체한다.
    /// 새 주문은 세션 전용 SO 인스턴스 — 일련번호는 GameState.nextOrderSerial이 단일 소유.
    /// </summary>
    public class CampOrderBoard : MonoBehaviour
    {
        // 신규 목적지 풀 — district는 이동맵 노드 라벨과 정확히 일치해야 스폰이 맞물린다.
        private static readonly (string address, string district, int floor)[] Destinations =
        {
            ("초록아파트 102동", "행복빌라 구역", 1),
            ("골목슈퍼", "행복빌라 구역", 1),
            ("은하빌라 202호", "달빛맨션 구역", 2),
            ("반달오피스텔 707호", "달빛맨션 구역", 7),
            ("청운상가 지하1층", "행복빌라 구역", -1),
            ("달동네 꼭대기집", "달빛맨션 구역", 4),
        };

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private PickupBox[] _boxes;

        private void Start()
        {
            if (_gameState == null || _boxes == null) return;

            foreach (PickupBox box in _boxes)
            {
                if (box == null || box.Order == null) continue;
                if (!IsConsumed(box.Order)) continue;

                DeliveryOrderSO fresh = GenerateOrder();
                box.SetOrder(fresh);
                Debug.Log("[주문판] 소진 건 교체 → #" + fresh.orderId + " " + fresh.address
                        + " (" + fresh.district + " · 마감 " + (fresh.deadlineMinuteOfDay / 60f).ToString("0.0") + "시)");
            }
        }

        /// <summary>완료됐거나(히스토리), 마감이 지났는데 적재도 안 된 주문 = 소진.</summary>
        private bool IsConsumed(DeliveryOrderSO order)
        {
            foreach (DeliveryRecord record in _gameState.deliveryHistory)
                if (record.orderId == order.orderId) return true;
            if (_gameState.minuteOfDay > order.deadlineMinuteOfDay && !_gameState.cargo.Contains(order))
                return _gameState.scannedOrderIds.Contains(order.orderId); // 손도 안 댄 건은 그대로 둔다
            return false;
        }

        private DeliveryOrderSO GenerateOrder()
        {
            int serial = _gameState.nextOrderSerial++;
            var pick = Destinations[serial % Destinations.Length];

            DeliveryOrderSO order = ScriptableObject.CreateInstance<DeliveryOrderSO>();
            order.name = "RuntimeOrder_" + serial;
            order.orderId = serial;
            order.address = pick.address;
            order.district = pick.district;
            order.floor = pick.floor;
            order.reward = 900 + (serial % 4) * 400;
            order.weight = 2f + serial % 5;
            order.deadlineMinuteOfDay = Mathf.Min(1435f, _gameState.minuteOfDay + 240f + (serial % 3) * 90f);
            return order;
        }
    }
}
