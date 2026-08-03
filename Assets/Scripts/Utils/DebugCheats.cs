using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// QA 치트 (S-134 ⑦ — 정수님 QA "다음 지역·트럭 활성화 필요, QA 불편").
    ///
    /// F9  = 다음 구역 해금
    /// F10 = 트럭 지급 (레벨도 Lv4로 끌어올린다 — S-134 ② 게이트 통과용)
    /// F11 = 레벨 +1
    ///
    /// ⚠ **웹 빌드에서도 동작해야 한다** — 정수님 QA 경로가 웹이다. 그래서 에디터 전용이 아니라
    /// `DEVELOPMENT_BUILD`까지 살린다(개발 빌드로 배포해야 QA가 쓸 수 있다는 뜻).
    /// 릴리스 빌드에서는 이 컴포넌트의 Update가 통째로 사라진다.
    /// </summary>
    public class DebugCheats : MonoBehaviour
    {
        [SerializeField] private GameStateSO _gameState;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null || _gameState == null) return;

            if (keyboard.f9Key.wasPressedThisFrame) UnlockNextDistrict();
            if (keyboard.f10Key.wasPressedThisFrame) GrantTruck();
            if (keyboard.f11Key.wasPressedThisFrame) LevelUp();
        }
#endif

        private void UnlockNextDistrict()
        {
            string[] progression = DeliveryOrderSO.DISTRICT_PROGRESSION;
            int frontier = -1;
            for (int i = 0; i < progression.Length; i++)
                if (_gameState.unlockedDistricts.Contains(progression[i])) frontier = i;

            if (frontier + 1 >= progression.Length)
            {
                Debug.Log("[치트] 더 해금할 구역이 없다 (F9)");
                return;
            }
            string next = progression[frontier + 1];
            _gameState.unlockedDistricts.Add(next);
            // 주문판 리롤 신호 — 정상 정산이 쓰는 것과 같은 경로(CampOrderBoard가 소비).
            // ⚠ 기존 주소도 함께 리롤된다. dayOrders를 직접 건드리면 정산 불변식이 깨진다.
            _gameState.daySettled = true;
            WorldEvents.RaiseDistrictUnlocked(next);
            Debug.Log("[치트] 구역 해금 — " + next + " (F9)");
        }

        private void GrantTruck()
        {
            if (_gameState.playerLevel < LevelPerks.TRUCK) _gameState.playerLevel = LevelPerks.TRUCK;
            _gameState.hasTruck = true;
            Debug.Log("[치트] 트럭 지급 + Lv" + LevelPerks.TRUCK + " (F10)");
        }

        private void LevelUp()
        {
            _gameState.playerLevel++;
            _gameState.mastery = 0f;
            string unlock = LevelPerks.UnlockLabel(_gameState.playerLevel);
            Debug.Log("[치트] Lv" + _gameState.playerLevel + (unlock != null ? " — " + unlock : "") + " (F11)");
            if (unlock != null) WorldEvents.RaiseItemAcquired("Lv." + _gameState.playerLevel + " — " + unlock);
        }
    }
}
