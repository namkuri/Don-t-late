namespace DontLate
{
    /// <summary>콘텐츠 씬 식별자. Core는 상주하므로 목록에 없다.</summary>
    public enum GameScene
    {
        Main,
        Home,
        Camp,
        Travel,
        District
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
                Floor = order.floor,
                Reward = order.reward,
                DeadlineMinuteOfDay = order.deadlineMinuteOfDay,
                IsLate = isLate
            };
        }
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
