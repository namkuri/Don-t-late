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
                if (IsConsumed(box.Order))
                {
                    DeliveryOrderSO fresh = GenerateOrder();
                    box.SetOrder(fresh);
                    Debug.Log("[주문판] 소진 건 교체 → #" + fresh.orderId + " " + fresh.address
                            + " (" + fresh.district + " · 마감 " + (fresh.deadlineMinuteOfDay / 60f).ToString("0.0") + "시)");
                }

                // S-034 ①: 이미 트럭에 실은 건의 상자는 캠프에서 치운다 — 안 실은 것만 남는다.
                box.gameObject.SetActive(!_gameState.cargo.Contains(box.Order));
            }
        }

        /// <summary>S-031 ⑦: 캠프 도착 시점에 적재 여유가 없는 미적재 주문도 소진으로 본다 —
        /// "싣는 중에 마감"이 나던 원흉(마감 경과·임박 주문이 상자에 그대로 남던 것).</summary>
        private const float MIN_SLACK_MINUTES = 120f;

        private bool IsConsumed(DeliveryOrderSO order)
        {
            foreach (DeliveryRecord record in _gameState.deliveryHistory)
                if (record.orderId == order.orderId) return true;
            if (!_gameState.cargo.Contains(order)
                && order.deadlineMinuteOfDay - _gameState.minuteOfDay < MIN_SLACK_MINUTES)
                return true;
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
            order.deadlineMinuteOfDay = Mathf.Min(1435f, _gameState.minuteOfDay + 300f + (serial % 3) * 90f); // S-031 ⑦ 최소 여유 240→300분
            return order;
        }
    }
}
