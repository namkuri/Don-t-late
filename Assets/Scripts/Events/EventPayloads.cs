namespace DontLate
{
    /// <summary>콘텐츠 씬 식별자. Core는 상주하므로 목록에 없다.</summary>
    public enum GameScene
    {
        Main,
        Home,
        Camp,
        Travel,
        District,
        Apartment // S-038 (D-067) — 아파트단지 별도 씬 (대차·비번·엘베)
    }

    /// <summary>하루의 날씨 (S-042) — 아침 추첨, 시각·연출·안개가 구독.</summary>
    public enum WeatherType
    {
        Clear,   // 맑음 — 구름 거의 없음
        Cloudy,  // 흐림
        Rain,    // 비 — 먹구름
        Snow,    // 눈
        Fog,     // 안개
        Heat     // 폭염 — 아지랑이
    }

    /// <summary>하루의 시간대. 조명·LUT 전환의 기준.</summary>
    public enum DayPhase
    {
        Morning,
        Day,
        Evening,
        Night
    }

    /// <summary>게임 내 시각 스냅샷. 원본은 GameStateSO가 단일 소유한다.</summary>
    public struct GameClock
    {
        public int Day;
        public int Hour;
        public int Minute;

        public int MinuteOfDay => Hour * 60 + Minute;
    }

    /// <summary>배송 건의 이벤트 페이로드. SO 참조 대신 값으로 흘린다.</summary>
    public struct DeliveryData
    {
        public int OrderId;
        public string Address;
        /// <summary>배송 구역 라벨 (S-015 — 폰 안내·스폰 매칭).</summary>
        public string District;
        public int Floor;
        public int Reward;
        /// <summary>마감 시각 (하루 기준 분, 0~1440).</summary>
        public float DeadlineMinuteOfDay;
        public bool IsLate;

        public static DeliveryData From(DeliveryOrderSO order, bool isLate = false)
        {
            return new DeliveryData
            {
                OrderId = order.orderId,
                Address = order.address,
                District = order.district,
                Floor = order.floor,
                Reward = order.reward,
                DeadlineMinuteOfDay = order.deadlineMinuteOfDay,
                IsLate = isLate
            };
        }
    }

    /// <summary>Camp 복귀 정산 요약. 원본 갱신은 WorldDebtManager가 수행한 뒤 결과만 흘린다.</summary>
    public struct DebtSettlement
    {
        /// <summary>이번 정산에서 빚 상환에 들어간 금액.</summary>
        public int Repaid;
        /// <summary>지각·미니게임 실패 벌금 합계.</summary>
        public int Penalty;
        public int Money;
        public int Debt;
    }

    /// <summary>박말순 전화. 시나리오 본문은 WorldDialogueManager가 id로 찾는다.</summary>
    public struct PhoneCall
    {
        public string CallerName;
        public string ScenarioId;
    }

    /// <summary>리듬 미니게임 결과.</summary>
    public struct MinigameResult
    {
        public bool Success;
        public int HitCount;
        public int TotalCount;
        public float Accuracy;
    }
}
