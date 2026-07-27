using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 씬 가장자리 도보 게이트 (S-054b — 엣지 워크). 밟으면 개척 순서(DISTRICT_PROGRESSION)상
    /// 이전/다음 동네로 걸어서 이동한다(게임분 소모). 미해금 구역 방향은 막히고 안내만 한다.
    /// 캠프의 Next는 첫 구역(빌라촌), 첫 구역의 Prev는 캠프.
    /// </summary>
    public class DistrictEdgeGate : MonoBehaviour
    {
        public enum Direction { Prev, Next }

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private Direction _direction;
        [Tooltip("도보 이동에 소모되는 게임분.")]
        [SerializeField] private float _walkMinutes = 40f;

        private float _denyCooldown;

        private void Update()
        {
            if (_denyCooldown > 0f) _denyCooldown -= Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerManager>() == null) return;
            TryWalk();
        }

        private void TryWalk()
        {
            if (WorldSceneFlowManager.Instance == null || WorldSceneFlowManager.Instance.IsTransitioning) return;

            string[] progression = DeliveryOrderSO.DISTRICT_PROGRESSION;
            GameScene currentScene = _gameState.currentScene;

            // 현재 위치의 개척 인덱스 (-1 = 캠프).
            int index;
            if (currentScene == GameScene.Camp) index = -1;
            else if (currentScene == GameScene.Apartment) index = System.Array.IndexOf(progression, DeliveryOrderSO.DISTRICT_APARTMENT);
            else if (currentScene == GameScene.Hillside) index = System.Array.IndexOf(progression, DeliveryOrderSO.DISTRICT_HILLSIDE);
            else index = Mathf.Max(0, System.Array.IndexOf(progression, _gameState.currentDistrict));

            int targetIndex = index + (_direction == Direction.Next ? 1 : -1);

            if (targetIndex < -1 || targetIndex >= progression.Length)
            {
                Deny("이쪽은 길이 없다.");
                return;
            }

            if (targetIndex == -1)
            {
                // 첫 구역 왼쪽 = 캠프 복귀.
                WorldDayNightManager.Instance?.AdvanceMinutes(_walkMinutes);
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

            WorldDeliveryManager.Instance?.SetDestination(targetDistrict);
            WorldDayNightManager.Instance?.AdvanceMinutes(_walkMinutes);
            Debug.Log("[도보] " + targetDistrict + " 방향으로 걸어간다 (" + _walkMinutes + "게임분).");
            WorldSceneFlowManager.Instance.Request(targetScene);
        }

        private void Deny(string message)
        {
            if (_denyCooldown > 0f) return;
            _denyCooldown = 2.5f;
            Debug.Log("[도보] " + message);
        }
    }
}
