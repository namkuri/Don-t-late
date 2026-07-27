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
        // S-035(D-064): 빌라촌={OO빌라·반지하·원룸·연립} / 먹자골목={식당·호프·분식·포장마차} 컨셉 정합.
        private static readonly (string address, string district, int floor)[] Destinations =
        {
            ("초록빌라 202호", DeliveryOrderSO.DISTRICT_VILLATOWN, 2),
            ("골목연립 반지하", DeliveryOrderSO.DISTRICT_VILLATOWN, -1),
            ("햇살원룸 3호", DeliveryOrderSO.DISTRICT_VILLATOWN, 1),
            ("왕만두분식", DeliveryOrderSO.DISTRICT_FOODALLEY, 1),
            ("달빛호프 2층", DeliveryOrderSO.DISTRICT_FOODALLEY, 2),
            ("끝집포장마차", DeliveryOrderSO.DISTRICT_FOODALLEY, 1),
            ("늦지마아파트 202호", DeliveryOrderSO.DISTRICT_APARTMENT, 2), // S-038
            ("늦지마아파트 303호", DeliveryOrderSO.DISTRICT_APARTMENT, 3),
            ("늦지마아파트 404호", DeliveryOrderSO.DISTRICT_APARTMENT, 4),
            ("언덕 계단집", DeliveryOrderSO.DISTRICT_HILLSIDE, 2),      // S-049 — floor=테라스 단
            ("중턱 빨간지붕", DeliveryOrderSO.DISTRICT_HILLSIDE, 3),
            ("꼭대기 파란대문", DeliveryOrderSO.DISTRICT_HILLSIDE, 4),
        };

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private PickupBox[] _boxes;

        private void Start()
        {
            if (_gameState == null || _boxes == null) return;

            // S-068 ③ — 하루 배송건 고정: 첫 진입 때 확정된 주문은 정산 전엔 바뀌지 않는다.
            // 정산(daySettled) 후 재방문에만 전체 리롤. (구 IsConsumed 시간·이력 교체 폐지 — 재방문마다
            // "완전 새 주문"으로 보이던 원흉. 마감 지난 건은 그대로 남아 지각 실패로 정산된다.)
            bool reroll = _gameState.daySettled;
            if (reroll) _gameState.daySettled = false;

            foreach (PickupBox box in _boxes)
            {
                if (box == null || box.Order == null) continue;
                if (reroll)
                {
                    DeliveryOrderSO fresh = GenerateOrder();
                    box.SetOrder(fresh);
                    Debug.Log("[주문판] 하루 마감 — 새 주문 → #" + fresh.orderId + " " + fresh.address
                            + " (" + fresh.district + ")");
                }

                // 픽업(=적재 등록)해 들고 나간 건의 상자만 치운다 — 스캔만 한 건 그대로.
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
            // S-054 — 개척 진행: 해금된 구역의 주소만 발주 (unlockedDistricts 비면 전체 허용 — 테스트 호환).
            if (_gameState.unlockedDistricts.Count > 0)
                for (int hop = 0; hop < Destinations.Length && !_gameState.unlockedDistricts.Contains(pick.district); hop++)
                    pick = Destinations[(serial + hop + 1) % Destinations.Length];

            DeliveryOrderSO order = ScriptableObject.CreateInstance<DeliveryOrderSO>();
            order.name = "RuntimeOrder_" + serial;
            order.orderId = serial;
            order.address = pick.address;
            order.district = pick.district;
            order.floor = pick.floor;
            order.reward = 900 + (serial % 4) * 400;
            order.weight = 2f + serial % 5;
            float deadline = Mathf.Min(1435f, _gameState.minuteOfDay + 300f + (serial % 3) * 90f); // S-031 ⑦ 최소 여유 240→300분
            // S-035(D-064): 먹자골목은 저녁~밤 마감으로 몰아 "밤 배송량↑" 설정을 신규 시스템 없이 표현.
            if (order.district == DeliveryOrderSO.DISTRICT_FOODALLEY)
                deadline = Mathf.Min(1435f, Mathf.Max(deadline, 19f * 60f));
            order.deadlineMinuteOfDay = deadline;
            return order;
        }
    }
}
