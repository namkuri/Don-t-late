using System.Collections.Generic;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 전역 상태 보관소. 계산·판정 로직은 넣지 않는다 — 매니저의 몫.
    /// 저장 없음(세션제)이므로 CoreBootstrap이 기동 시 초기화한다.
    /// </summary>
    [CreateAssetMenu(menuName = "DontLate/GameState")]
    public class GameStateSO : ScriptableObject
    {
        [Header("시계 — WorldDayNightManager가 쓰고 나머지는 읽는다")]
        public int day;
        // 하루 기준 분. 0~1440.
        public float minuteOfDay;

        [Header("경제")]
        public int money;
        public int debt;

        [Header("배송")]
        // 적재 목록의 단일 소유처. Player 쪽에 Inventory를 두지 않는다.
        public List<DeliveryOrderSO> cargo = new List<DeliveryOrderSO>();
        /// <summary>바코드 스캔된 주문 id (S-011 — 스캔한 짐만 픽업 가능. 스캔 순서 유지).</summary>
        public List<int> scannedOrderIds = new List<int>();
        /// <summary>이동맵에서 고른 목적 구역 (S-015) — District 도착 시 이 구역의 짐·비콘만 스폰.</summary>
        public string currentDistrict;

        // ── S-019: 폰 앱·하우징·투자 ──
        /// <summary>완료 배송 기록 (택배앱 히스토리).</summary>
        public List<DeliveryRecord> deliveryHistory = new List<DeliveryRecord>();
        /// <summary>누적 수익 (택배앱 수익 탭 — money와 달리 지출로 줄지 않는다).</summary>
        public int totalEarned;
        /// <summary>보유 가구 id (구매 후 미배치 인벤토리).</summary>
        public List<string> ownedFurnitureIds = new List<string>();
        /// <summary>배치된 가구 (Home 씬 재생성용 — 세션제).</summary>
        public List<PlacedFurniture> placedFurniture = new List<PlacedFurniture>();
        public bool bedSeeded;      // S-031 ③ — 세션당 1회 침대를 placedFurniture로 시드
        public int wallpaperIndex;  // S-031 ④ — 벽지 팔레트 (HomeDecorator)
        public int floorIndex;      // S-031 ④ — 바닥 팔레트
        /// <summary>보유 코인 수량 (금융앱).</summary>
        public float coinUnits;
        public float coinCostBasis; // S-032 ⑤ — 현재 보유분의 총 매수금액 (차익 = 평가액 − 이것)
        /// <summary>런타임 생성 주문의 다음 일련번호 (S-021 ③ — 캠프 주문 갱신).</summary>
        public int nextOrderSerial = 200;

        [Header("통계")]
        public int completedCount;
        public int lateCount;

        [Header("진행")]
        public GameScene currentScene;

        [Header("세션 초기값")]
        public int startDay = 1;
        public float startMinuteOfDay = 8f * 60f;
        public int startMoney;
        public int startDebt = 10000;
    }

    [System.Serializable]
    public struct DeliveryRecord
    {
        public int orderId;
        public string address;
        public int reward;
        public int day;
        public int minuteOfDay;
    }

    [System.Serializable]
    public struct PlacedFurniture
    {
        public string furnitureId;
        public Vector3 position;
        public float rotationY; // S-030 ③ — R 회전 각도 보존
    }
}
