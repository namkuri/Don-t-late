using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 아파트 공동현관 비번 게이트 (S-038 → S-048 ② 개편). E = 키패드 요청 → 뷰가 KeypadEntered로
    /// 돌려주면 GameState 비번과 대조(뷰는 표시만 — 판정은 월드 몫).
    /// 성공 = **자동 슬라이드문 해제**(ApartmentSlidingDoor.Unlock) — 이후엔 걸어서 드나든다(텔레포트 폐지).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ApartmentPasswordGate : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;
        [Tooltip("성공 시 해제되는 자동문.")]
        [SerializeField] private ApartmentSlidingDoor _door;

        private bool _awaitingEntry;

        private void OnEnable() => WorldEvents.KeypadEntered += OnKeypadEntered;
        private void OnDisable() => WorldEvents.KeypadEntered -= OnKeypadEntered;

        public void Interact(PlayerContext ctx)
        {
            if (_door != null && _door.Unlocked)
            {
                Debug.Log("[공동현관] 이미 해제됨 — 문 앞에 서면 열린다.");
                return;
            }
            _awaitingEntry = true;
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
            if (!_awaitingEntry) return; // 이 게이트가 요청한 입력이 아님

            if (_gameState == null || code != _gameState.apartmentGatePassword)
            {
                WorldEvents.RaiseKeypadRejected();
                return;
            }

            _awaitingEntry = false;
            WorldEvents.RaiseGateOpened();
            if (_door != null) _door.Unlock();
        }
    }
}
