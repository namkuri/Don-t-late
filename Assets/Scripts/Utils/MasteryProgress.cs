using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 숙련도 가감·레벨업 판정 단일 창구 (S-063). 배송 성공 +12 · 실패 -6 · 주행 50m +1.
    /// 만충(레벨별 상한) 시 레벨업하고 초과분은 이월. 수치는 추후 밸런스 조정 대상.
    /// </summary>
    public static class MasteryProgress
    {
        // S-165 ④ — 경험치 게이지는 **5칸**. 레벨과 무관하게 상한을 고정해 "몇 건 더 하면
        // 오르는지"가 화면만 보고 세어진다 — 상한이 레벨마다 늘면 그 감각이 깨진다.
        // S-174 ② — 남규님 지시로 **배송 1건 = 2칸**(종전 1칸). 한 칸을 1점으로 잡아
        // 점수와 칸 수가 같아졌다 — 수치를 읽을 때 나눗셈이 사라진다.
        public const int SEGMENTS = 5;             // 게이지 칸 수
        public const float PER_SEGMENT = 1f;       // 한 칸 = 1점
        public const float SUCCESS_GAIN = 2f;      // 배송 성공 1건 = 2칸
        public const float FAIL_LOSS = SUCCESS_GAIN; // 실패는 얻는 만큼만 잃는다

        // ⚠ 주행 경험치는 **0으로 껐다**. 상한이 15로 낮아져 종전 비율(50m당 +1)이면 걷기만 해도
        // 레벨이 오른다 — "상자 1개에 3씩"이라는 기준이 무의미해진다. 되살리려면 값을 올려 잡아야 한다.
        // static readonly인 이유: const 0이면 호출부 가드가 `if(false)`로 접혀 CS0162(도달 불가)
        // 워닝이 뜬다. 워닝 0건이 납품 조건(§8)이라 상수 접기만 막는다 — 값·의미는 동일.
        public static readonly float RUN_METERS_PER_POINT = 0f;

        public static float MaxFor(int level) => SEGMENTS * PER_SEGMENT;

        public static void Add(GameStateSO gameState, float amount)
        {
            if (gameState == null) return;
            gameState.mastery = Mathf.Max(0f, gameState.mastery + amount);
            while (gameState.mastery >= MaxFor(gameState.playerLevel))
            {
                gameState.mastery -= MaxFor(gameState.playerLevel);
                gameState.playerLevel++;
                WorldEvents.RaisePlayerLeveledUp(gameState.playerLevel); // S-174 ③ — SFX·연출
            }
            // S-174 ④ — 게이지 연출(칸 순차 펀치)의 신호. 여기가 숙련도 변동의 단일 창구이므로
            // 발행도 여기 하나면 된다 — 호출부마다 알리면 빠뜨리는 곳이 생긴다.
            WorldEvents.RaiseMasteryChanged(gameState.mastery, gameState.playerLevel);
        }
    }
}
