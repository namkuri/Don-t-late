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
}
