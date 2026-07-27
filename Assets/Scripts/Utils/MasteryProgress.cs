using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 숙련도 가감·레벨업 판정 단일 창구 (S-063). 배송 성공 +12 · 실패 -6 · 주행 50m +1.
    /// 만충(레벨별 상한) 시 레벨업하고 초과분은 이월. 수치는 추후 밸런스 조정 대상.
    /// </summary>
    public static class MasteryProgress
    {
        public const float SUCCESS_GAIN = 12f;
        public const float FAIL_LOSS = 6f;
        public const float RUN_METERS_PER_POINT = 50f;

        public static float MaxFor(int level) => 100f + (level - 1) * 25f;

        public static void Add(GameStateSO gameState, float amount)
        {
            if (gameState == null) return;
            gameState.mastery = Mathf.Max(0f, gameState.mastery + amount);
            while (gameState.mastery >= MaxFor(gameState.playerLevel))
            {
                gameState.mastery -= MaxFor(gameState.playerLevel);
                gameState.playerLevel++;
                Debug.Log("[숙련도] 레벨업! Lv." + gameState.playerLevel);
            }
        }
    }
}
