using UnityEngine;
using UnityEngine.SceneManagement;

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
            // S-041: 대차는 몸으로 민다 — Player×CartWall 충돌 허용(무시 규칙 폐지).
            // 밀림 폭주는 대차가 자가 이동을 안 하므로(플레이어 푸시가 유일 동력) 재발하지 않는다.
            Physics.IgnoreLayerCollision(8, 9, false);

            ResetSession();

            if (WorldSceneFlowManager.Instance == null)
            {
                Debug.LogError("[CoreBootstrap] WorldSceneFlowManager가 Core 씬에 없다.");
                return;
            }

            // 씬 단독 Play(S-013): 콘텐츠 씬이 이미 떠 있는 상태로 Core가 사후 로드된 경우 —
            // Main으로 끌고 가지 않고, 현재 씬 도착만 통지해 매니저들을 동기화한다.
            if (SceneManager.sceneCount > 1)
            {
                if (System.Enum.TryParse(SceneManager.GetActiveScene().name, out GameScene current))
                {
                    WorldSceneFlowManager.Instance.AdoptCurrent(current); // 다음 전이에서 이 씬이 정상 언로드되게 (S-015)
                    WorldEvents.RaiseSceneTransitionCompleted(current);
                }
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
            _gameState.scannedOrderIds.Clear();
            _gameState.placedDeliveries.Clear();
            _gameState.completedCount = 0;
            _gameState.lateCount = 0;
            _gameState.currentDistrict = string.Empty;
            _gameState.deliveryHistory.Clear();
            _gameState.totalEarned = 0;
            _gameState.ownedFurnitureIds.Clear();
            _gameState.placedFurniture.Clear();
            _gameState.bedSeeded = false;
            _gameState.apartmentGatePassword = Random.Range(0, 10000).ToString("0000"); // S-038 세션 비번
            _gameState.wallpaperIndex = 0;
            _gameState.floorIndex = 0;
            _gameState.coinUnits = 0f;
            _gameState.coinCostBasis = 0f;
            _gameState.nextOrderSerial = 200;
        }
    }
}
