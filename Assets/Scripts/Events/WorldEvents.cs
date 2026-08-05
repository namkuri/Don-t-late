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

        /// <summary>상자 좌클릭 → 송장 표시 요청 (S-071 ② — 센서 발행, InvoiceView 구독). 저빈도.</summary>
        /// <summary>노점·자판기 구매창 열기 (S-125 ② — 남규님 요청: 별도 구매 UI).</summary>
        public static event Action<KioskOffer> KioskRequested;

        public static void RaiseKioskRequested(KioskOffer offer)
        {
            Log("KioskRequested → " + offer.Title);
            KioskRequested?.Invoke(offer);
        }

        /// <summary>
        /// S-146 — 가방/휴대폰을 **열었다**(닫기는 알리지 않는다). 튜토리얼이 "해봤는지"를
        /// 판정하려면 뷰가 상태를 경계 밖으로 알려야 한다(폴링 금지·Find 금지 규약).
        /// 개폐는 사람 조작이라 저빈도 — §9.5 로그 대상이다.
        /// </summary>
        /// <summary>
        /// S-162 — 튜토리얼 단계 시작(제목·설명). 미션 카드 UI가 구독해 표시한다.
        /// 판정은 진행부가 하고 뷰는 **표시만** 한다(UI에 게임 로직 금지 규약).
        /// 단계 전환은 저빈도라 §9.5 로그 대상이다.
        /// </summary>
        public static event Action<string, string> TutorialStepStarted;

        public static void RaiseTutorialStepStarted(string title, string detail)
        {
            Log("TutorialStepStarted " + title);
            TutorialStepStarted?.Invoke(title, detail);
        }

        /// <summary>S-162 — 튜토리얼 단계 완료. 카드가 "완료"로 바뀌고 퇴장한다.</summary>
        public static event Action TutorialStepCleared;

        public static void RaiseTutorialStepCleared()
        {
            Log("TutorialStepCleared");
            TutorialStepCleared?.Invoke();
        }

        public static event Action BagOpened;

        public static void RaiseBagOpened()
        {
            Log("BagOpened");
            BagOpened?.Invoke();
        }

        public static event Action PhoneOpened;

        public static void RaisePhoneOpened()
        {
            Log("PhoneOpened");
            PhoneOpened?.Invoke();
        }

        public static event Action<DeliveryOrderSO> InvoiceRequested;

        public static void RaiseInvoiceRequested(DeliveryOrderSO order)
        {
            Log("InvoiceRequested → #" + (order != null ? order.orderId.ToString() : "?"));
            InvoiceRequested?.Invoke(order);
        }

        public static void RaiseStaminaChanged(float normalized) => StaminaChanged?.Invoke(normalized);

        /// <summary>드링크 버프 스태미나 풀 변화 (S-098 ② — 기본 최대 대비 비율 0~0.1). 연속값 — 로그 금지 목록.</summary>
        public static event Action<float> BuffStaminaChanged;

        public static void RaiseBuffStaminaChanged(float normalized) => BuffStaminaChanged?.Invoke(normalized);

        // ── 파손 ──────────────────────────────────────────────
        /// <summary>취급주의 상자 파손 (AU-008). BoxDurability가 발행 — 상자는 주문을 모르므로 페이로드 없음.</summary>
        public static event Action PackageDestroyed;

        public static void RaisePackageDestroyed()
        {
            Log("PackageDestroyed — 상자 파손");
            PackageDestroyed?.Invoke();
        }

        /// <summary>엔딩 시퀀스 개시 (S-107 ① — WorldEndingManager 발행). 오디오가 엔딩 BGM 전환에 구독. 게임당 1회 저빈도.</summary>
        public static event Action EndingStarted;

        public static void RaiseEndingStarted()
        {
            Log("EndingStarted — 엔딩 시퀀스 개시");
            EndingStarted?.Invoke();
        }

        /// <summary>취급주의 상자가 충격으로 HP가 닳았을 때 — 파손 전 단계 포함 (S-096). BoxDurability가 발행, 사장님 잔소리가 구독.</summary>
        public static event Action PackageDamaged;

        public static void RaisePackageDamaged()
        {
            Log("PackageDamaged — 상자 손상");
            PackageDamaged?.Invoke();
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

        /// <summary>날씨 변경 (S-042 — 하루 1회 추첨). 연출·안개·그레이드가 구독.</summary>
        public static event Action<WeatherType> WeatherChanged;

        public static void RaiseWeatherChanged(WeatherType weather)
        {
            Log("WeatherChanged → " + weather);
            WeatherChanged?.Invoke(weather);
        }

        /// <summary>배송 구역 해금 (S-054 진행 시스템) — 정산에서 개척 판정 시 1회.</summary>
        public static event Action<string> DistrictUnlocked;

        public static void RaiseDistrictUnlocked(string district)
        {
            Log("DistrictUnlocked → " + district);
            DistrictUnlocked?.Invoke(district);
        }

        /// <summary>회사 트럭 수령 (S-054) — 전 구역 개척 완료 보상. 세션당 1회.</summary>
        public static event Action TruckAwarded;

        public static void RaiseTruckAwarded()
        {
            Log("TruckAwarded — 지도앱 즉시 이동 해금");
            TruckAwarded?.Invoke();
        }

        /// <summary>차에 치임 (S-057) — 병원비 청구·미배송 실패 처리는 WorldDeliveryManager 몫.</summary>
        public static event Action PlayerHitByCar;

        public static void RaisePlayerHitByCar()
        {
            Log("PlayerHitByCar — 병원행");
            PlayerHitByCar?.Invoke();
        }

        /// <summary>교통사고 정산 통지 (S-066 ③) — 병원비·실패 건수. AccidentView가 팝업을 연다.</summary>
        /// <summary>교통사고 (S-134 ④ 개정) — 병원비와 **후송 여부**(체력 0칸).
        /// 후송이면 AccidentView가 하루를 정산하고 강제 귀가시킨다. 종전 2번째 인자는
        /// '실패 건수'였으나, 사고가 적재를 전량 실패시키던 규칙 자체가 폐기됐다.</summary>
        public static event Action<int, bool> CarAccident;

        public static void RaiseCarAccident(int hospitalFee, bool hospitalized)
        {
            Log("CarAccident 병원비 " + hospitalFee + (hospitalized ? " · 후송" : ""));
            CarAccident?.Invoke(hospitalFee, hospitalized);
        }

        /// <summary>가방에서 좌클릭 — 손에 들기 요청 (S-064). Player 도메인이 받아 시각물 생성.</summary>
        public static event Action<BagItem> BagHoldRequested;

        /// <summary>플레이어(수신자)가 씬에 있는가 — 없는 씬(집 등)에서 아이템 유실 방지 (S-064).</summary>
        public static bool HasBagHoldListener => BagHoldRequested != null;

        public static void RaiseBagHoldRequested(BagItem item)
        {
            Log("BagHoldRequested → " + item.label);
            BagHoldRequested?.Invoke(item);
        }

        /// <summary>무언가를 획득했다 (S-133 ⑤) — 토스트 한 줄로 알린다.
        /// 페이로드는 **완성된 문장**("에너지드링크를 획득하였습니다") — View는 표시만 한다.</summary>
        public static event Action<string> ItemAcquired;

        public static void RaiseItemAcquired(string message)
        {
            Log("ItemAcquired — " + message);
            ItemAcquired?.Invoke(message);
        }

        /// <summary>한국어 조사(을/를) 자동 선택 — 받침 유무로 고른다.</summary>
        public static string AcquiredMessage(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return "아이템을 획득하였습니다.";
            char last = itemName[itemName.Length - 1];
            bool hasJongseong = last >= 0xAC00 && last <= 0xD7A3 && (last - 0xAC00) % 28 != 0;
            return itemName + (hasJongseong ? "을" : "를") + " 획득하였습니다.";
        }

        /// <summary>상자를 손에서 놓았다 (S-133 ①) — 배치·투척·낙하 공통.
        /// 목적지 패드가 이걸 받아 하이라이트를 끈다. 종전엔 픽업만 있고 놓기가 없어
        /// 상자를 내려놔도 패드가 계속 빛났다.</summary>
        public static event Action<DeliveryData> PackageReleased;

        public static void RaisePackageReleased(DeliveryData data)
        {
            Log("PackageReleased #" + data.OrderId);
            PackageReleased?.Invoke(data);
        }

        /// <summary>귀가 요청 (S-134 ⑤) — '집으로' 버튼과 **엣지워크 도보 귀가**가 공용으로 쓴다.
        /// 정산 UI(SettlementView)가 받아 하루를 마감한다. 도보 귀가가 이 이벤트를 안 타서
        /// 정산이 통째로 누락되던 것이 원래 버그다.</summary>
        public static event Action GoHomeRequested;

        /// <summary>정산 UI가 이 씬에 있는가 — 없으면 호출자가 직접 씬 전이로 폴백한다.</summary>
        public static bool HasGoHomeListener => GoHomeRequested != null;

        public static void RaiseGoHomeRequested()
        {
            Log("GoHomeRequested");
            GoHomeRequested?.Invoke();
        }

        /// <summary>가방 컨텍스트 "사용" (S-064) — 효과는 아이템별 (Player 도메인 몫).</summary>
        public static event Action<BagItem> BagItemConsumed;

        public static bool HasBagConsumeListener => BagItemConsumed != null;

        public static void RaiseBagItemConsumed(BagItem item)
        {
            Log("BagItemConsumed → " + item.label);
            BagItemConsumed?.Invoke(item);
        }

        /// <summary>적설(지면 쌓임) 유무 전환 (AU-018 ③ — 발소리 스노 스왑용). 저빈도: 임계 통과 시 1회.</summary>
        // ── 스태미나 패널티 (S-088 ④) — 구성 변화 시만 통지 (저빈도) ──
        public static event Action<StaminaPenalties> StaminaPenaltyChanged;
        public static void RaiseStaminaPenaltyChanged(StaminaPenalties penalties)
        {
            Log("StaminaPenaltyChanged 합계 " + penalties.Total);
            StaminaPenaltyChanged?.Invoke(penalties);
        }

        // ── NPC 호감도 (S-079 ④) — 저빈도 상태 통지 ──
        public static event Action<string> NpcMet;
        public static void RaiseNpcMet(string npcId)
        {
            Log("NpcMet → " + npcId);
            NpcMet?.Invoke(npcId);
        }

        public static event Action<string, int> NpcAffinityChanged;
        public static void RaiseNpcAffinityChanged(string npcId, int affinity)
        {
            Log("NpcAffinityChanged " + npcId + " → " + affinity);
            NpcAffinityChanged?.Invoke(npcId, affinity);
        }

        public static event Action<bool> SnowCoverChanged;

        public static void RaiseSnowCoverChanged(bool covered)
        {
            Log("SnowCoverChanged → " + covered);
            SnowCoverChanged?.Invoke(covered);
        }

        // ── 아파트 (S-038 · D-067) — 전부 저빈도 상태 통지 ──

        /// <summary>공동현관 비번 패널 앞에서 E — 키패드 UI 열림 요청.</summary>
        public static event Action KeypadRequested;
        public static void RaiseKeypadRequested() { Log("KeypadRequested"); KeypadRequested?.Invoke(); }

        /// <summary>키패드 4자리 입력 완료 — 판정은 게이트 몫(뷰는 표시만).</summary>
        public static event Action<string> KeypadEntered;
        public static void RaiseKeypadEntered(string code) { Log("KeypadEntered ****"); KeypadEntered?.Invoke(code); }

        /// <summary>비번 일치 — 공동현관 개방(게이트가 발행, 키패드 뷰가 닫힘 구독).</summary>
        public static event Action GateOpened;
        public static void RaiseGateOpened() { Log("GateOpened"); GateOpened?.Invoke(); }

        /// <summary>비번 불일치 — 키패드 뷰가 오류 표시.</summary>
        public static event Action KeypadRejected;
        public static void RaiseKeypadRejected() { Log("KeypadRejected"); KeypadRejected?.Invoke(); }

        /// <summary>엘리베이터 문 열림 — 층 선택 UI 요청 (페이로드 = 선택 가능한 층 배열).</summary>
        public static event Action<int[]> FloorSelectRequested;
        public static void RaiseFloorSelectRequested(int[] floors) { Log("FloorSelectRequested"); FloorSelectRequested?.Invoke(floors); }

        /// <summary>층 선택 — 이동은 요청한 엘리베이터가 처리.</summary>
        public static event Action<int> FloorChosen;
        public static void RaiseFloorChosen(int floor) { Log("FloorChosen → " + floor); FloorChosen?.Invoke(floor); }
    }
}
