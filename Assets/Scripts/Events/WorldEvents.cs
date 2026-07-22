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
        /// <summary>상호작용 포커스 획득/해제. 대상이 바뀔 때만 발행 — 프레임 데이터 아님.</summary>
        public static event Action<bool> InteractionFocusChanged;
        /// <summary>포커스 대상의 주소(배송지일 때만, 아니면 null) — HUD 풀해상 표시용 (S-021 ②).
        /// InteractionFocusChanged와 같은 빈도로 같이 발행되므로 별도 로그는 달지 않는다.</summary>
        public static event Action<string> FocusAddressChanged;

        public static void RaiseFocusAddressChanged(string address) => FocusAddressChanged?.Invoke(address);

        public static void RaiseCarryStateChanged(bool isCarrying)
        {
            Log("CarryStateChanged → " + (isCarrying ? "들었다" : "내려놓았다"));
            CarryStateChanged?.Invoke(isCarrying);
        }

        public static void RaiseInteractionFocusChanged(bool focused)
        {
            Log("InteractionFocusChanged → 포커스 " + (focused ? "잡힘" : "해제"));
            InteractionFocusChanged?.Invoke(focused);
        }

        public static void RaiseStaminaChanged(float normalized) => StaminaChanged?.Invoke(normalized);

        // ── 파손 ──────────────────────────────────────────────
        /// <summary>취급주의 상자 파손 (AU-008). BoxDurability가 발행 — 상자는 주문을 모르므로 페이로드 없음.</summary>
        public static event Action PackageDestroyed;

        public static void RaisePackageDestroyed()
        {
            Log("PackageDestroyed — 상자 파손");
            PackageDestroyed?.Invoke();
        }

        // ── 상차 (바코드) ─────────────────────────────────────
        /// <summary>폰으로 박스 송장을 스캔했을 때. PhoneView가 목록 갱신에 구독 (S-011).</summary>
        public static event Action<DeliveryData> BarcodeScanned;

        public static void RaiseBarcodeScanned(DeliveryData data)
        {
            Log("BarcodeScanned " + Describe(data));
            BarcodeScanned?.Invoke(data);
        }

        // ── 정산 ──────────────────────────────────────────────
        /// <summary>지각·미니게임 벌금으로 빚이 즉시 늘었을 때 (S-015). HUD 플로팅 표시가 구독.</summary>
        public static event Action<int> DebtIncreased;

        public static void RaiseDebtIncreased(int amount)
        {
            Log("DebtIncreased +" + amount);
            DebtIncreased?.Invoke(amount);
        }

        /// <summary>돈을 실제로 지출했을 때 (S-030 ③ — 자판기·가구 등 TrySpend 공용). HUD 플로팅·코인 SFX가 구독.</summary>
        public static event Action<int> MoneySpent;

        public static void RaiseMoneySpent(int amount)
        {
            Log("MoneySpent -" + amount);
            MoneySpent?.Invoke(amount);
        }

        /// <summary>Camp 복귀 정산 완료. WorldDebtManager가 발행, HUD·연출이 구독.</summary>
        public static event Action<DebtSettlement> DebtSettled;

        public static void RaiseDebtSettled(DebtSettlement s)
        {
            Log("DebtSettled 상환 " + s.Repaid + " · 벌금 " + s.Penalty + " → 잔액 " + s.Money + " / 빚 " + s.Debt);
            DebtSettled?.Invoke(s);
        }

        // ── 진상 전화 미니게임 ────────────────────────────────
        public static event Action<PhoneCall> PhoneRang;
        public static event Action MinigameRequested;
        public static event Action<MinigameResult> MinigameEnded;

        public static void RaisePhoneRang(PhoneCall call)
        {
            Log("PhoneRang ← " + call.CallerName);
            PhoneRang?.Invoke(call);
        }

        public static void RaiseMinigameRequested()
        {
            Log("MinigameRequested");
            MinigameRequested?.Invoke();
        }

        public static void RaiseMinigameEnded(MinigameResult result)
        {
            Log("MinigameEnded " + (result.Success ? "성공" : "실패")
                + " (" + result.HitCount + "/" + result.TotalCount + ")");
            MinigameEnded?.Invoke(result);
        }

        // ── 대화 ──────────────────────────────────────────────
        // 시작/종료만 저빈도 경계 이벤트로 발행한다. 라인 단위 통지는 준-고빈도라
        // WorldDialogueManager의 C# 이벤트(LineChanged)로 UI에만 흘린다 (CODE_RULES §9.5).
        public static event Action<string> DialogueStarted;
        public static event Action<string> DialogueEnded;

        public static void RaiseDialogueStarted(string scenarioName)
        {
            Log("DialogueStarted → " + scenarioName);
            DialogueStarted?.Invoke(scenarioName);
        }

        public static void RaiseDialogueEnded(string scenarioName)
        {
            Log("DialogueEnded → " + scenarioName);
            DialogueEnded?.Invoke(scenarioName);
        }
    }
}
