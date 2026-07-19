using UnityEngine;

namespace DontLate
{
    /// <summary>M3 조정용 튜닝값 모음. 코드에 상수를 박지 않는다.</summary>
    [CreateAssetMenu(menuName = "DontLate/TuningConfig")]
    public class TuningConfigSO : ScriptableObject
    {
        [Header("이동 (1u = 1m)")]
        public float moveSpeed = 4f;
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
        public float staminaDrainPerSecond = 2.5f;
        public float staminaDrainCarryMultiplier = 2f;
        public float staminaRecoverPerSecond = 6f;
        public float energyDrinkRecover = 40f;

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
    }
}
