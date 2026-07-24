using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 아파트 공동현관 비번 게이트 (S-038 · D-067). E = 키패드 요청 → 뷰가 입력을 KeypadEntered로
    /// 돌려주면 여기서 GameState 비번과 대조한다(뷰는 표시만 — 판정은 월드 몫).
    /// 성공 시 플레이어 + 근처 대차 + 도크 존의 낱개 상자를 로비로 통째 이동.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ApartmentPasswordGate : MonoBehaviour, IInteractable
    {
        private const float CART_RADIUS = 6f;  // 이 반경의 대차가 함께 들어간다
        private const float DOCK_RADIUS = 4f;  // 도크 존 낱개 상자 동반 반경

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;
        [Tooltip("짐 전용 비콘(도크) 위치 — 낱개 상자 동반 판정 중심.")]
        [SerializeField] private Transform _dockPoint;
        [Tooltip("성공 시 플레이어가 서는 로비 지점.")]
        [SerializeField] private Transform _lobbySpawn;

        private Transform _player;

        private void OnEnable()
        {
            WorldEvents.KeypadEntered += OnKeypadEntered;
        }

        private void OnDisable()
        {
            WorldEvents.KeypadEntered -= OnKeypadEntered;
        }

        public void Interact(PlayerContext ctx)
        {
            _player = ctx.Player.transform;
            WorldEvents.RaiseKeypadRequested();
        }

        public void SetHighlight(bool on)
        {
            if (_renderer == null) return;
            Material material = on && _highlightMaterial != null ? _highlightMaterial : _normalMaterial;
            if (material != null) _renderer.sharedMaterial = material;
        }

        private void OnKeypadEntered(string code)
        {
            if (_player == null) return; // 이 게이트가 요청한 입력이 아님

            if (_gameState == null || code != _gameState.apartmentGatePassword)
            {
                WorldEvents.RaiseKeypadRejected();
                return;
            }

            WorldEvents.RaiseGateOpened();
            TeleportInside();
            _player = null;
        }

        // 플레이어 + 근처 대차 + 도크 존 낱개 상자를 로비로.
        private void TeleportInside()
        {
            if (_lobbySpawn == null || _player == null) return;
            Vector3 lobby = _lobbySpawn.position;

            var controller = _player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false; // CC는 transform 순간이동을 씹는다
            _player.position = lobby;
            if (controller != null) controller.enabled = true;

            foreach (DeliveryCart cart in Object.FindObjectsByType<DeliveryCart>())
                if ((cart.transform.position - transform.position).magnitude <= CART_RADIUS)
                    cart.MoveTo(lobby + Vector3.right * 1.4f);

            Vector3 dock = _dockPoint != null ? _dockPoint.position : transform.position;
            int loose = 0;
            foreach (PickupBox box in Object.FindObjectsByType<PickupBox>())
            {
                if (box.transform.parent != null) continue; // 대차 적재분은 대차가 옮긴다
                if ((box.transform.position - dock).magnitude > DOCK_RADIUS) continue;
                box.transform.position = lobby + new Vector3(2.4f + loose * 0.9f, 0.4f, 0f);
                loose++;
            }
            Debug.Log("[공동현관] 개방 — 로비 진입 (낱개 상자 " + loose + "개 동반).");
        }
    }
}
