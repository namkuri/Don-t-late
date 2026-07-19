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
        // ── 시계 ──────────────────────────────────────────────
        /// <summary>분 경계를 넘을 때만 발행. 매 프레임 발행 금지.</summary>
        public static event Action<GameClock> ClockTicked;
        public static event Action<DayPhase> DayPhaseChanged;

        public static void RaiseClockTicked(GameClock clock) => ClockTicked?.Invoke(clock);
        public static void RaiseDayPhaseChanged(DayPhase phase) => DayPhaseChanged?.Invoke(phase);

        // ── 씬 전이 ───────────────────────────────────────────
        public static event Action<GameScene> SceneTransitionStarted;
        public static event Action<GameScene> SceneTransitionCompleted;

        public static void RaiseSceneTransitionStarted(GameScene scene) => SceneTransitionStarted?.Invoke(scene);
        public static void RaiseSceneTransitionCompleted(GameScene scene) => SceneTransitionCompleted?.Invoke(scene);

        // ── 배송 ──────────────────────────────────────────────
        public static event Action<DeliveryData> OrderAccepted;
        public static event Action<DeliveryData> PackagePickedUp;
        public static event Action<DeliveryData> DeliveryCompleted;
        /// <summary>지각 판정. Deadline이 발행하고 Delivery·Debt가 정리한다.</summary>
        public static event Action<DeliveryData> DeliveryFailed;
        /// <summary>마감 임박 경고. 건당 1회.</summary>
        public static event Action<DeliveryData> DeadlineWarned;

        public static void RaiseOrderAccepted(DeliveryData data) => OrderAccepted?.Invoke(data);
        public static void RaisePackagePickedUp(DeliveryData data) => PackagePickedUp?.Invoke(data);
        public static void RaiseDeliveryCompleted(DeliveryData data) => DeliveryCompleted?.Invoke(data);
        public static void RaiseDeliveryFailed(DeliveryData data) => DeliveryFailed?.Invoke(data);
        public static void RaiseDeadlineWarned(DeliveryData data) => DeadlineWarned?.Invoke(data);

        // ── Player → World ────────────────────────────────────
        public static event Action<bool> CarryStateChanged;
        public static event Action<float> StaminaChanged;

        public static void RaiseCarryStateChanged(bool isCarrying) => CarryStateChanged?.Invoke(isCarrying);
        public static void RaiseStaminaChanged(float normalized) => StaminaChanged?.Invoke(normalized);
    }
}
