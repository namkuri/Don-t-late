using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// Core 씬 기동점. 매니저들이 Awake에서 Instance를 세운 뒤 Start에서 세션을 연다.
    /// 저장이 없으므로 상태 초기화도 여기서 한다(SO에 값이 남는 문제 방지).
    /// </summary>
    public class CoreBootstrap : MonoBehaviour
    {
        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private GameScene _firstScene = GameScene.Main;

        private void Start()
        {
            ResetSession();

            if (WorldSceneFlowManager.Instance == null)
            {
                Debug.LogError("[CoreBootstrap] WorldSceneFlowManager가 Core 씬에 없다.");
                return;
            }

            WorldSceneFlowManager.Instance.Request(_firstScene);
        }

        private void ResetSession()
        {
            _gameState.day = _gameState.startDay;
            _gameState.minuteOfDay = _gameState.startMinuteOfDay;
            _gameState.money = _gameState.startMoney;
            _gameState.debt = _gameState.startDebt;
            _gameState.cargo.Clear();
            _gameState.completedCount = 0;
            _gameState.lateCount = 0;
        }
    }
}
