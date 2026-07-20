using System;

namespace DontLate
{
    /// <summary>
    /// 도메인 경계 통신 허브. World 매니저 간·Player↔World 통신은 전부 여기를 지난다.
    /// 프레임 단위 데이터(입력·이동)는 올리지 않는다 — 도메인 내부 허브의 몫.
    /// 구독자는 OnEnable/OnDisable 짝을 반드시 맞춘다.
    /// </summary>
    public static class WorldEvents
    {
        // ── 관측 로그 (CODE_RULES §9.5) ────────────────────────
        // 경계 이벤트가 전부 이 클래스를 지나므로 로깅 지점은 여기 하나뿐이다.
        // 릴리스 빌드에서는 Conditional이 호출부째 지운다 — 비용 0.
        private const string LOG_PREFIX = "<color=#35e0c8>[EVENT]</color> ";

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private static void Log(string message) => UnityEngine.Debug.Log(LOG_PREFIX + message);

        private static string Describe(DeliveryData d)
            => "#" + d.OrderId + " " + d.Address + " (마감 " + (d.DeadlineMinuteOfDay / 60f).ToString("0.00") + "시)";

        // ── 시계 ──────────────────────────────────────────────
        /// <summary>분 경계를 넘을 때만 발행. 매 프레임 발행 금지.</summary>
        /// <remarks>고빈도라 로그하지 않는다 (CODE_RULES §9.5).</remarks>
        public static event Action<GameClock> ClockTicked;
        public static event Action<DayPhase> DayPhaseChanged;

        public static void RaiseClockTicked(GameClock clock) => ClockTicked?.Invoke(clock);

        public static void RaiseDayPhaseChanged(DayPhase phase)
        {
            Log("DayPhaseChanged → " + phase);
            DayPhaseChanged?.Invoke(phase);
        }

        // ── 씬 전이 ───────────────────────────────────────────
        public static event Action<GameScene> SceneTransitionStarted;
        public static event Action<GameScene> SceneTransitionCompleted;

        public static void RaiseSceneTransitionStarted(GameScene scene)
        {
            Log("SceneTransitionStarted → " + scene);
            SceneTransitionStarted?.Invoke(scene);
        }

        public static void RaiseSceneTransitionCompleted(GameScene scene)
        {
            Log("SceneTransitionCompleted → " + scene);
            SceneTransitionCompleted?.Invoke(scene);
        }

        // ── 배송 ──────────────────────────────────────────────
        public static event Action<DeliveryData> OrderAccepted;
        public static event Action<DeliveryData> PackagePickedUp;
        public static event Action<DeliveryData> DeliveryCompleted;
        /// <summary>지각 판정. Deadline이 발행하고 Delivery·Debt가 정리한다.</summary>
        public static event Action<DeliveryData> DeliveryFailed;
        /// <summary>마감 임박 경고. 건당 1회.</summary>
        public static event Action<DeliveryData> DeadlineWarned;

        public static void RaiseOrderAccepted(DeliveryData data)
        {
            Log("OrderAccepted " + Describe(data));
            OrderAccepted?.Invoke(data);
        }

        public static void RaisePackagePickedUp(DeliveryData data)
        {
            Log("PackagePickedUp " + Describe(data));
            PackagePickedUp?.Invoke(data);
        }

        public static void RaiseDeliveryCompleted(DeliveryData data)
        {
            Log("DeliveryCompleted " + Describe(data) + " 보상 +" + data.Reward);
            DeliveryCompleted?.Invoke(data);
        }

        public static void RaiseDeliveryFailed(DeliveryData data)
        {
            Log("DeliveryFailed " + Describe(data) + (data.IsLate ? " — 지각" : ""));
            DeliveryFailed?.Invoke(data);
        }

        public static void RaiseDeadlineWarned(DeliveryData data)
        {
            Log("DeadlineWarned " + Describe(data));
            DeadlineWarned?.Invoke(data);
        }

        // ── Player → World ────────────────────────────────────
        public static event Action<bool> CarryStateChanged;
        /// <remarks>연속값이라 로그하지 않는다 (CODE_RULES §9.5).</remarks>
        public static event Action<float> StaminaChanged;

        public static void RaiseCarryStateChanged(bool isCarrying)
        {
            Log("CarryStateChanged → " + (isCarrying ? "들었다" : "내려놓았다"));
            CarryStateChanged?.Invoke(isCarrying);
        }

        public static void RaiseStaminaChanged(float normalized) => StaminaChanged?.Invoke(normalized);
    }
}
