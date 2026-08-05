namespace DontLate
{
    /// <summary>
    /// 레벨 해금 표 (S-134 ② — 정수님 QA / 남규님 결정). 성장 보상이 **조작 편의로 환원**되는 루프.
    ///
    /// | 레벨 | 해금 |
    /// |---|---|
    /// | 2 | 짐 2개 |
    /// | 3 | 짐 3개 (단 3번째는 흔들려 떨어진다 — S-133 ⑥) |
    /// | 4 | 트럭 |
    /// | 5 | 이동속도 +15% |
    /// | 6 | 스태미나 최대치 +20% |
    ///
    /// 수치를 여기 한 곳에 모은다 — 종전엔 캐리 상한이 `completedCount >= 5`로
    /// PlayerStatusManager에 박혀 있어 레벨과 따로 놀았다.
    /// </summary>
    public static class LevelPerks
    {
        public const int CARRY_2 = 2;
        public const int CARRY_3 = 3;
        public const int TRUCK = 4;
        public const int MOVE_SPEED = 5;
        public const int STAMINA = 6;

        private const float MOVE_SPEED_BONUS = 0.15f;   // Lv5 — 관제 재량(체감되되 밸런스 유지)
        private const float STAMINA_BONUS = 0.20f;      // Lv6

        /// <summary>동시에 들 수 있는 상자 수 (1~3).</summary>
        /// <summary>
        /// S-165 ④ — 두 레벨 사이에 **새로 열린 능력** 이름(없으면 null).
        /// 정산 화면이 "무엇이 좋아졌는지"를 말해주기 위한 표시용 — 레벨 숫자만으론 보상이 안 된다.
        /// 한 정산에서 여러 단계가 올라도 가장 **먼저 열린 것** 하나만 알린다(줄이 길어지면 묻힌다).
        /// </summary>
        public static string PerkGainedBetween(int before, int after)
        {
            if (after <= before) return null;
            for (int level = before + 1; level <= after; level++)
            {
                switch (level)
                {
                    case CARRY_2:     return "택배 상자 2개 들기";
                    case CARRY_3:     return "택배 상자 3개 들기";
                    case TRUCK:       return "회사 트럭 자격";
                    case MOVE_SPEED:  return "이동 속도 상승";
                    case STAMINA:     return "체력 상한 상승";
                }
            }
            return null;
        }

        public static int CarryCapacity(int level)
        {
            if (level >= CARRY_3) return 3;
            if (level >= CARRY_2) return 2;
            return 1;
        }

        /// <summary>트럭을 몰 자격이 되는가 (실제 보유는 GameState.hasTruck — 구매·지급이 따로 있다).</summary>
        public static bool TruckUnlocked(int level) => level >= TRUCK;

        public static float MoveSpeedMultiplier(int level)
            => level >= MOVE_SPEED ? 1f + MOVE_SPEED_BONUS : 1f;

        public static float StaminaMaxMultiplier(int level)
            => level >= STAMINA ? 1f + STAMINA_BONUS : 1f;

        /// <summary>레벨업 시 HUD·토스트에 띄울 한 줄. 해금이 없는 레벨이면 null.</summary>
        public static string UnlockLabel(int level) => level switch
        {
            CARRY_2 => "짐을 2개까지 들 수 있다",
            CARRY_3 => "짐을 3개까지 들 수 있다 (맨 위는 흔들린다)",
            TRUCK => "트럭 해금",
            MOVE_SPEED => "이동속도 +15%",
            STAMINA => "체력이 늘었다 (스태미나 +20%)",
            _ => null,
        };
    }
}
