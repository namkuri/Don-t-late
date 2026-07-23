using UnityEngine;

namespace DontLate
{
    /// <summary>M3 조정용 튜닝값 모음. 코드에 상수를 박지 않는다.</summary>
    [CreateAssetMenu(menuName = "DontLate/TuningConfig")]
    public class TuningConfigSO : ScriptableObject
    {
        [Header("이동 (1u = 1m)")]
        public float moveSpeed = 4f;
        /// <summary>Shift 달리기 속도. 애니메이션은 Speed 파라미터 임계값으로 걷기/달리기를 가른다.</summary>
        public float runSpeed = 6f;
        /// <summary>Z(깊이) 이동 속도 배율. 대각 감을 위해 X보다 느리다.</summary>
        [Range(0f, 1f)] public float depthSpeedRatio = 0.7f;
        /// <summary>상자를 들었을 때 속도 배율.</summary>
        [Range(0f, 1f)] public float carrySpeedPenalty = 0.75f;
        public float jumpHeight = 1.1f;
        public float gravity = -22f;
        /// <summary>이동 방향 회전 속도 (deg/s).</summary>
        public float turnSpeed = 720f;

        [Header("스태미나")]
        public float staminaMax = 100f;
        /// <summary>걷기 소모/초 (S-019 ③).</summary>
        public float staminaDrainPerSecond = 2f;
        /// <summary>달리기 소모/초 — 걷기보다 크게.</summary>
        public float staminaDrainRunPerSecond = 6f;
        /// <summary>든 상자 1kg당 추가 소모/초.</summary>
        public float staminaDrainPerKg = 0.35f;
        public float staminaDrainCarryMultiplier = 2f; // (구) 무게 미지정 주문 폴백
        public float staminaRecoverPerSecond = 6f;
        public float energyDrinkRecover = 40f;

        [Header("취급주의 상자 (S-019 ①)")]
        public float boxMaxHp = 100f;
        /// <summary>이 속도(m/s) 이하 충돌은 무피해.</summary>
        public float boxSafeImpactSpeed = 5f;
        /// <summary>안전 속도 초과 1m/s당 피해.</summary>
        public float boxDamagePerSpeed = 8f;

        [Header("자판기 (S-019 ②)")]
        public int vendingPrice = 1000;

        [Header("투자 (S-019 ⑥ 금융앱)")]
        /// <summary>코인 가격 진폭(기준가 1,000원 대비).</summary>
        public float coinVolatility = 0.35f;

        [Header("시계")]
        // 실시간 1초당 흐르는 게임 내 분.
        public float gameMinutesPerRealSecond = 1f;
        public int morningStartHour = 6;
        public int dayStartHour = 10;
        public int eveningStartHour = 17;
        public int nightStartHour = 20;

        [Header("마감")]
        // 남은 시간이 이 값(분) 이하가 되면 경고를 1회 발행.
        public float deadlineWarnMinutes = 30f;

        [Header("상호작용")]
        public float interactRadius = 1.6f;
        /// <summary>캐리 중 좌클릭 던지기 속도 (S-016).</summary>
        public float throwSpeed = 7f;

        [Header("정산 (Camp)")]
        /// <summary>지각 1건당 벌금.</summary>
        public int latePenalty = 300;
        /// <summary>미니게임(진상 전화) 실패 벌금.</summary>
        public int minigamePenalty = 200;
        /// <summary>한 번에 실을 수 있는 최대 적재 수.</summary>
        public int maxCargo = 3;

        [Header("이동맵 (Travel)")]
        /// <summary>근거리 노드 이동에 소모되는 게임 분.</summary>
        public float travelNearMinutes = 30f;
        /// <summary>원거리 노드 이동에 소모되는 게임 분.</summary>
        public float travelFarMinutes = 90f;

        [Header("미니게임 (진상 전화)")]
        /// <summary>District 도착 후 전화가 오기까지의 실초.</summary>
        public float phoneCallDelaySeconds = 15f;
        /// <summary>수신 후 받기/거절 없이 방치 시 자동 종료(부재중=실패)까지의 실초 (S-037).</summary>
        public float phoneCallTimeoutSeconds = 15f;
        /// <summary>리듬 시퀀스 키 개수.</summary>
        public int minigameKeyCount = 4;
        /// <summary>키 하나당 입력 제한 실초.</summary>
        public float minigameKeyStepSeconds = 1.2f;
    }
}
