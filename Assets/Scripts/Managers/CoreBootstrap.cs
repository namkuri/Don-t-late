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

        // S-107 ② — 타이틀 복귀 = 새 게임: 엔딩·설정("처음 화면으로") 어느 경유든 Main 도착 시
        // 세션을 리셋한다 (엔딩 후 재시작 시 빚 0 잔존 실사고). 첫 진입 Main도 재리셋 — 멱등이라 무해.
        private void OnEnable()  { WorldEvents.SceneTransitionCompleted += OnSceneArrivedBootstrap; }
        private void OnDisable() { WorldEvents.SceneTransitionCompleted -= OnSceneArrivedBootstrap; }

        private void OnSceneArrivedBootstrap(GameScene scene)
        {
            if (scene == GameScene.Main) ResetSession();

            // S-184 — 첫 인상 구간 종료: **배송을 한 건이라도 마치고 집에 돌아온 순간**.
            // 남규님 문구 그대로 "첫 배송 후 홈 복귀까지". 여기서 푸는 이유는 이 클래스가
            // 세션 수명(시작·리셋)을 이미 소유해서다 — 같은 성격의 상태를 두 곳에 두지 않는다.
            if (scene == GameScene.Home && _gameState != null
                && _gameState.introGraceActive && _gameState.completedCount > 0)
            {
                _gameState.introGraceActive = false;
                Debug.Log("[첫인상] 낮·맑음 고정 해제 — 이제부터 시각·날씨가 자연 진행한다.");
                // 고정 동안 억눌린 날씨를 지금 다시 뽑는다(해제 즉시 자연 상태로).
                WorldWeatherManager.Instance?.RerollNow();
            }
        }

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
            // S-242 — 종잣돈 3,000원 지급. 에셋의 startMoney(0)를 고치는 대신 여기서 하한을 두는 이유는
            // **금액의 근거가 한 곳에 있어야** 하기 때문이다 — 정산에서 보호하는 값과 지급액이 같은 상수다.
            _gameState.money = Mathf.Max(_gameState.startMoney, GameStateSO.PROTECTED_CASH);
            _gameState.debt = _gameState.startDebt;
            _gameState.bossIntroPlayed = false; // S-052 — 사장님 튜토리얼 리셋
            // S-158 — 튜토리얼 상태도 함께 초기화한다. 특히 `tutorialExitLocked`는 남아 있으면
            // **다음 세션에서 귀가가 영구히 막힌다**(SO는 세션 간 값이 남는다).
            _gameState.tutorialDone = false;
            _gameState.tutorialExitLocked = false;
            _gameState.introGraceActive = true; // S-184 — 첫 인상 구간 재개시
            _gameState.unlockedDistricts.Clear(); // S-054 — 빌라촌부터 걸어서 개척
            _gameState.unlockedDistricts.Add(DeliveryOrderSO.DISTRICT_VILLATOWN);
            _gameState.hasTruck = false;
            _gameState.playerLevel = 1;   // S-063
            _gameState.health = GameStateSO.HEALTH_MAX; // S-134 ④ — 세션 시작은 만체력
            _gameState.mastery = 0f;
            _gameState.bagItems.Clear();  // S-064
            // S-155 — 시작 지급: 에너지드링크 1개. 튜토리얼에서 사장님이 "써보라"고 시키므로
            // 가방이 비어 있으면 그 단계를 진행할 수 없다. id·라벨은 `EnergyDrinkPickup`과 같은
            // 값을 쓴다 — 다르면 같은 물건이 가방에서 두 칸으로 갈라진다.
            BagStorage.TryAdd(_gameState, "drink", "에너지드링크", stackable: true, holdable: true);
            _gameState.ownsCart = false;  // S-056
            _gameState.catFriend = false;  // S-059
            _gameState.catRanAway = false;
            _gameState.catFedDay = 0;
            _gameState.carriedOrders.Clear(); // S-066
            _gameState.daySettled = false;    // S-068
            _gameState.dayOrders.Clear();     // S-072 ⑩
            _gameState.destroyedOrderIds.Clear(); // S-074 ③
            _gameState.npcAffinities.Clear();     // S-079 ④
            _gameState.endingMonologuePlayed = false; // S-104
            _gameState.endingPlayed = false;
            _gameState.stamina = -1f;             // S-081 ①
            _gameState.droppedCargo.Clear();
            _gameState.cargo.Clear();
            _gameState.truckCargoIds.Clear(); // S-264
            _gameState.greetedScenes.Clear();  // S-268
            _gameState.truckRemarkDone = false;
            _gameState.scannedOrderIds.Clear();
            _gameState.placedDeliveries.Clear();
            _gameState.completedCount = 0;
            _gameState.lateCount = 0;
            _gameState.currentDistrict = string.Empty;
            _gameState.deliveryHistory.Clear();
            _gameState.totalEarned = 0;
            _gameState.ownedFurnitureIds.Clear();
            _gameState.placedFurniture.Clear();
            _gameState.apartmentGatePassword = Random.Range(0, 10000).ToString("0000"); // S-038 세션 비번
            _gameState.wallpaperIndex = 0;
            _gameState.floorIndex = 0;
            _gameState.coinUnits = 0f;
            _gameState.coinCostBasis = 0f;
            _gameState.nextOrderSerial = 200;
        }
    }
}
