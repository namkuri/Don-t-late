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
        // S-074 ④ — 풀 확장 (빌라촌 3→6·먹자 3→5): "초록빌라 202호만 3개" 다양성 붕괴 수리의 일부.
        private static readonly (string address, string district, int floor)[] Destinations =
        {
            ("초록빌라 202호", DeliveryOrderSO.DISTRICT_VILLATOWN, 2),
            ("골목연립 반지하", DeliveryOrderSO.DISTRICT_VILLATOWN, -1),
            ("햇살원룸 3호", DeliveryOrderSO.DISTRICT_VILLATOWN, 1),
            ("행복빌라 301호", DeliveryOrderSO.DISTRICT_VILLATOWN, 3),
            ("모퉁이양옥 1층", DeliveryOrderSO.DISTRICT_VILLATOWN, 1),
            ("파랑대문집 102호", DeliveryOrderSO.DISTRICT_VILLATOWN, 1),
            ("왕만두분식", DeliveryOrderSO.DISTRICT_FOODALLEY, 1),
            ("달빛호프 2층", DeliveryOrderSO.DISTRICT_FOODALLEY, 2),
            ("끝집포장마차", DeliveryOrderSO.DISTRICT_FOODALLEY, 1),
            ("매운족발집", DeliveryOrderSO.DISTRICT_FOODALLEY, 1),
            ("골목치킨 2층", DeliveryOrderSO.DISTRICT_FOODALLEY, 2),
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

            // S-072 ⑩ — 확정 사이클: 첫 진입 시 당일 물량 확정(GameState.dayOrders에 영속) → 스폰 →
            // 배달 → 정산 → 리셋 → 다음 진입 때만 재확정. S-068 고정이 절반만 동작하던 원흉 =
            // S-071 미해금 교체가 씬 오브젝트(비영속)에만 반영돼 재진입(씬 리로드)마다 재추첨되던 것.
            if (_gameState.daySettled)
            {
                _gameState.daySettled = false;
                _gameState.dayOrders.Clear(); // 정산 = 하루 마감 — 물량 리셋
            }

            bool confirm = _gameState.dayOrders.Count == 0; // 첫 진입(또는 정산 후) — 이번에 확정한다
            for (int i = 0; i < _boxes.Length; i++)
            {
                PickupBox box = _boxes[i];
                if (box == null) continue;

                if (confirm)
                {
                    DeliveryOrderSO order = box.Order;
                    // 리롤(주문 소진 이력 有) 또는 미해금 구역(S-071 ① — 물리벽에 막혀 배달 불가)이면 재추첨.
                    if (order == null || IsConsumed(order)
                        || (_gameState.unlockedDistricts.Count > 0
                            && !_gameState.unlockedDistricts.Contains(order.district)))
                    {
                        order = GenerateOrder();
                        Debug.Log("[주문판] 당일 물량 확정 — 새 주문 → #" + order.orderId + " "
                                + order.address + " (" + order.district + ")");
                    }
                    box.SetOrder(order);
                    _gameState.dayOrders.Add(order);
                }
                else
                {
                    // 확정분 재배정 — 씬이 리로드돼도 물량은 GameState의 것 그대로.
                    box.SetOrder(i < _gameState.dayOrders.Count ? _gameState.dayOrders[i] : null);
                }

                // 픽업(=적재 등록)해 들고 나간 건과 파손 건(S-074 ③)의 상자는 치운다 — 스캔만 한 건 그대로.
                box.gameObject.SetActive(box.Order != null
                    && !_gameState.cargo.Contains(box.Order)
                    && !_gameState.destroyedOrderIds.Contains(box.Order.orderId));
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

            // S-074 ④ — 다양성 수리: 구 hop 방식은 미해금 시작점에서 항상 '첫 해금 주소'로 수렴해
            // 연속 serial이 전부 같은 주소가 됐다("초록빌라 202호만 3개"의 원흉). 해금 풀을 먼저
            // 추리고 serial로 고르되, 이번 확정(dayOrders)에서 이미 쓴 주소는 가능하면 피한다.
            var candidates = new System.Collections.Generic.List<(string address, string district, int floor)>();
            foreach (var destination in Destinations)
                if (_gameState.unlockedDistricts.Count == 0
                    || _gameState.unlockedDistricts.Contains(destination.district))
                    candidates.Add(destination);
            if (candidates.Count == 0) candidates.AddRange(Destinations); // 방어 — 전부 미해금이면 전체 풀

            var pick = candidates[serial % candidates.Count];
            for (int hop = 1; hop < candidates.Count
                 && _gameState.dayOrders.Exists(o => o != null && o.address == pick.address); hop++)
                pick = candidates[(serial + hop) % candidates.Count];

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
