using System.Collections;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 씬 가장자리 도보 게이트 (S-054b → S-062 개편). 밟으면 개척 순서상 이전/다음 동네로
    /// 걸어서 이동한다(게임분 소모). 캠프 Next=빌라촌, 캠프 Prev=집(S-062 ②), 첫 구역 Prev=캠프.
    /// 미해금 방향은 자식 벽 콜라이더로 물리 차단(S-062 ④). 통과 도착 시 반대편 게이트 앞 스폰(S-062 ①).
    /// </summary>
    public class DistrictEdgeGate : MonoBehaviour
    {
        public enum Direction { Prev, Next }

        /// <summary>도보 도착 시 스폰할 게이트 방향 (S-062 ① — 전이 직전 세팅, 도착 씬 게이트가 소비).</summary>
        private static Direction? _pendingArrival;

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private Direction _direction;
        [Tooltip("미해금일 때 켜지는 물리 벽 (S-062 ④).")]
        [SerializeField] private GameObject _lockWall;

        private float _denyCooldown;

        private void OnEnable()
        {
            WorldEvents.DistrictUnlocked += OnDistrictUnlocked;
        }

        private void OnDisable()
        {
            WorldEvents.DistrictUnlocked -= OnDistrictUnlocked;
        }

        private void Start()
        {
            RefreshLockWall();
            if (_pendingArrival == _direction)
            {
                _pendingArrival = null;
                StartCoroutine(SpawnPlayerHere());
            }
            else if (_pendingArrival != null && _direction == Direction.Prev && FindTargetIndex() == int.MinValue)
            {
                _pendingArrival = null; // 게이트 구성이 다른 씬(집 등) — 힌트 소거만
            }
            // S-082 ④ — 집에서 나와 캠프 도착: 왼쪽(집 방향) 엣지 앞에서 시작.
            else if (_pendingArrival == null && _direction == Direction.Prev
                     && _gameState.currentScene == GameScene.Camp
                     && WorldSceneFlowManager.Instance != null
                     && WorldSceneFlowManager.Instance.PreviousScene == GameScene.Home)
            {
                StartCoroutine(SpawnPlayerHere());
            }
        }

        private void OnDistrictUnlocked(string _) => RefreshLockWall();

        // 도착 스폰 (S-062 ①) — 씬의 플레이어를 이 게이트 앞(안쪽 2.5u)으로 옮긴다.
        private IEnumerator SpawnPlayerHere()
        {
            yield return null; // 씬 오브젝트 Awake/Start 완료 대기
            PlayerManager player = null;
            foreach (Collider hit in Physics.OverlapSphere(Vector3.zero, 200f))
            {
                player = hit.GetComponentInParent<PlayerManager>();
                if (player != null) break;
            }
            if (player == null) yield break;

            float inward = transform.position.x > 0f ? -2.5f : 2.5f;
            Vector3 spawn = new Vector3(transform.position.x + inward, transform.position.y + 0.2f, transform.position.z);
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = spawn;
            if (cc != null) cc.enabled = true;
        }

        private void Update()
        {
            if (_denyCooldown > 0f) _denyCooldown -= Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerManager>() == null) return;
            TryWalk();
        }

        // 현재 씬의 개척 인덱스 (-1=캠프, int.MinValue=판정 불가).
        private int FindTargetIndex()
        {
            string[] progression = DeliveryOrderSO.DISTRICT_PROGRESSION;
            GameScene currentScene = _gameState.currentScene;
            if (currentScene == GameScene.Camp) return -1;
            if (currentScene == GameScene.Apartment) return System.Array.IndexOf(progression, DeliveryOrderSO.DISTRICT_APARTMENT);
            if (currentScene == GameScene.Hillside) return System.Array.IndexOf(progression, DeliveryOrderSO.DISTRICT_HILLSIDE);
            if (currentScene == GameScene.District) return Mathf.Max(0, System.Array.IndexOf(progression, _gameState.currentDistrict));
            return int.MinValue;
        }

        /// <summary>이 게이트가 향하는 목표 구역 — 미해금이면 잠김 (S-062 ④ 벽 판정).</summary>
        private bool IsLocked()
        {
            int index = FindTargetIndex();
            if (index == int.MinValue) return false;
            int target = index + (_direction == Direction.Next ? 1 : -1);
            string[] progression = DeliveryOrderSO.DISTRICT_PROGRESSION;
            if (target < 0 || target >= progression.Length) return false; // 캠프·집 방향은 항상 열림
            return _gameState.unlockedDistricts.Count > 0 && !_gameState.unlockedDistricts.Contains(progression[target]);
        }

        private void RefreshLockWall()
        {
            if (_lockWall != null) _lockWall.SetActive(IsLocked());
        }

        private void TryWalk()
        {
            if (WorldSceneFlowManager.Instance == null || WorldSceneFlowManager.Instance.IsTransitioning) return;

            string[] progression = DeliveryOrderSO.DISTRICT_PROGRESSION;
            int index = FindTargetIndex();
            if (index == int.MinValue) return;
            int targetIndex = index + (_direction == Direction.Next ? 1 : -1);

            if (targetIndex < -1 || targetIndex >= progression.Length)
            {
                // 캠프 왼쪽 = 집으로 귀가 (S-062 ②).
                if (index == -1 && _direction == Direction.Prev)
                {
                    // S-158 — 튜토리얼 중엔 귀가를 막는다. 나가면 `bossIntroPlayed`가 이미 true라
                    // 다시 와도 재개되지 않아, 조작을 다 배우지 못한 채 게임이 시작된다(남규님 실관찰).
                    // 조용히 막으면 고장으로 읽히므로 이유를 말한다.
                    if (_gameState != null && _gameState.tutorialExitLocked)
                    {
                        Deny("사장님 설명이 아직 안 끝났다. 짐부터 챙기자.");
                        return;
                    }

                    _pendingArrival = null;
                    // S-075 3 - 엣지 워크 시간 소모 폐지: 실제 걷는 시간이 곧 페널티 (남규님 R25).
                    Debug.Log("[도보] 집으로 걸어간다.");
                    // S-134 ⑤ — 도보 귀가도 '집으로' 버튼과 **같은 마감**을 탄다.
                    // 종전엔 곧장 Request(Home)이라 하루 정산이 통째로 누락됐다(정수님 QA).
                    if (WorldEvents.HasGoHomeListener && DayHasStarted())
                    {
                        WorldEvents.RaiseGoHomeRequested();
                        return;
                    }
                    // 정산 UI가 없는 씬이거나 하루가 시작도 안 했으면 종전대로 그냥 귀가.
                    WorldSceneFlowManager.Instance.Request(GameScene.Home);
                    return;
                }
                Deny("이쪽은 길이 없다.");
                return;
            }

            if (targetIndex == -1)
            {
                _pendingArrival = Direction.Next; // 캠프 도착 — 오른쪽(빌라촌 방향) 게이트 앞 스폰
                // S-075 3 - 엣지 워크 시간 소모 폐지: 실제 걷는 시간이 곧 페널티 (남규님 R25).
                WorldSceneFlowManager.Instance.Request(GameScene.Camp);
                return;
            }

            string targetDistrict = progression[targetIndex];
            if (_gameState.unlockedDistricts.Count > 0 && !_gameState.unlockedDistricts.Contains(targetDistrict))
            {
                Deny("아직 개척하지 못한 동네다 — 지금 구역 배송을 먼저 성공시키자.");
                return;
            }

            GameScene targetScene = targetDistrict == DeliveryOrderSO.DISTRICT_APARTMENT ? GameScene.Apartment
                                  : targetDistrict == DeliveryOrderSO.DISTRICT_HILLSIDE ? GameScene.Hillside
                                  : GameScene.District;

            // Next로 걸어가면 도착지 왼쪽(Prev 게이트) 앞에서, Prev로 걸어가면 오른쪽(Next 게이트) 앞에서 시작.
            _pendingArrival = _direction == Direction.Next ? Direction.Prev : Direction.Next;
            WorldDeliveryManager.Instance?.SetDestination(targetDistrict);
            // S-075 3 - 엣지 워크 시간 소모 폐지: 실제 걷는 시간이 곧 페널티 (남규님 R25).
            Debug.Log("[도보] " + targetDistrict + " 방향으로 걸어간다.");
            WorldSceneFlowManager.Instance.Request(targetScene);
        }

        private void Deny(string message)
        {
            if (_denyCooldown > 0f) return;
            _denyCooldown = 2.5f;
            Debug.Log("[도보] " + message);
        }

        /// <summary>
        /// 하루가 실제로 시작됐는가 (S-134 ⑤ 동반수리). 아침 캠프 스폰 지점(x −11.5)이 엣지
        /// 트리거 경계(x −13.4)에서 **1.9u밖에 안 떨어져** 있어, 도착 직후 왼쪽으로 몇 걸음만
        /// 걸어도 하루가 통째로 정산돼 버린다(되돌릴 수 없다). 짐도 배치도 파손도 없으면
        /// 정산할 것이 없다는 뜻이므로 그냥 귀가시킨다.
        /// </summary>
        private bool DayHasStarted()
        {
            if (_gameState == null) return false;
            return _gameState.cargo.Count > 0
                || _gameState.placedDeliveries.Count > 0
                || _gameState.destroyedOrderIds.Count > 0
                || _gameState.carriedOrders.Count > 0;
        }
    }
}
