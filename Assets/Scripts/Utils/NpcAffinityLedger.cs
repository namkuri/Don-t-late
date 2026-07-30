using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// NPC 호감도 장부 (S-079 ④) — MasteryProgress 패턴의 정적 헬퍼.
    /// 만남(Meet)이 소셜앱 등재 조건, Add가 증감(0~100 클램프). 상태는 GameStateSO 단일 소유.
    /// </summary>
    public static class NpcAffinityLedger
    {
        public const int MEET_BASE = 20;
        public const int MAX = 100;

        /// <summary>첫 만남 등록 — 이미 만난 NPC면 무시. 소셜앱 리스트 등재 조건.</summary>
        public static void Meet(GameStateSO gameState, string npcId)
        {
            if (gameState == null || string.IsNullOrEmpty(npcId)) return;
            if (gameState.npcAffinities.Exists(n => n.npcId == npcId)) return;
            gameState.npcAffinities.Add(new NpcAffinity { npcId = npcId, affinity = MEET_BASE });
            WorldEvents.RaiseNpcMet(npcId);
        }

        /// <summary>호감도 증감 (만남 미등록이면 등록부터). 0~100 클램프.</summary>
        public static void Add(GameStateSO gameState, string npcId, int delta)
        {
            if (gameState == null || string.IsNullOrEmpty(npcId)) return;
            Meet(gameState, npcId);
            int index = gameState.npcAffinities.FindIndex(n => n.npcId == npcId);
            NpcAffinity entry = gameState.npcAffinities[index];
            entry.affinity = Mathf.Clamp(entry.affinity + delta, 0, MAX);
            gameState.npcAffinities[index] = entry;
            WorldEvents.RaiseNpcAffinityChanged(npcId, entry.affinity);
        }

        /// <summary>
        /// 나쁜 첫인상 (S-124) — 미등록 NPC를 **만남 보너스 없이** 0에서 시작해 감점한다.
        /// Add를 쓰면 Meet가 먼저 20을 얹어 "때려서 친구 추가(20→5)"가 되고, 감점을 건너뛰면
        /// 남규님 관찰처럼 "욕은 하는데 호감도가 안 깎이는" 상태가 된다 — 둘 다 피하는 경로.
        /// </summary>
        public static void Penalize(GameStateSO gameState, string npcId, int penalty)
        {
            if (gameState == null || string.IsNullOrEmpty(npcId) || penalty <= 0) return;
            int index = gameState.npcAffinities.FindIndex(n => n.npcId == npcId);
            if (index < 0)
            {
                gameState.npcAffinities.Add(new NpcAffinity { npcId = npcId, affinity = 0 });
                WorldEvents.RaiseNpcMet(npcId); // 소셜앱에는 등재된다 — "최악의 첫인상"도 관계다
                index = gameState.npcAffinities.Count - 1;
            }
            NpcAffinity entry = gameState.npcAffinities[index];
            entry.affinity = Mathf.Clamp(entry.affinity - penalty, 0, MAX);
            gameState.npcAffinities[index] = entry;
            WorldEvents.RaiseNpcAffinityChanged(npcId, entry.affinity);
        }

        public static int Get(GameStateSO gameState, string npcId)
        {
            int index = gameState.npcAffinities.FindIndex(n => n.npcId == npcId);
            return index >= 0 ? gameState.npcAffinities[index].affinity : 0;
        }
    }
}
