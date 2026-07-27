using UnityEngine;

namespace DontLate
{
    /// <summary>가방 수납 규칙 단일 창구 (S-064). 기본 5칸 — 겹침 허용 아이템은 count로 쌓인다.</summary>
    public static class BagStorage
    {
        public const int CAPACITY = 5;

        /// <summary>수납 시도 — 겹침 가능하면 기존 칸에, 아니면 빈 칸에. 가득이면 false.</summary>
        public static bool TryAdd(GameStateSO gameState, string id, string label, bool stackable, bool holdable)
        {
            if (gameState == null) return false;

            if (stackable)
            {
                for (int i = 0; i < gameState.bagItems.Count; i++)
                {
                    if (gameState.bagItems[i].id != id) continue;
                    BagItem stacked = gameState.bagItems[i];
                    stacked.count++;
                    gameState.bagItems[i] = stacked;
                    Debug.Log("[가방] " + label + " +1 (총 " + stacked.count + ")");
                    return true;
                }
            }

            if (gameState.bagItems.Count >= CAPACITY) return false;
            gameState.bagItems.Add(new BagItem { id = id, label = label, count = 1, stackable = stackable, holdable = holdable });
            Debug.Log("[가방] " + label + " 수납");
            return true;
        }

        /// <summary>해당 칸에서 1개 차감 — 0이 되면 칸 제거.</summary>
        public static void RemoveOne(GameStateSO gameState, int index)
        {
            if (gameState == null || index < 0 || index >= gameState.bagItems.Count) return;
            BagItem item = gameState.bagItems[index];
            item.count--;
            if (item.count <= 0) gameState.bagItems.RemoveAt(index);
            else gameState.bagItems[index] = item;
        }
    }
}
