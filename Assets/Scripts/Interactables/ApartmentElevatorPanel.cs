using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 엘리베이터 패널 (S-048 ③) — 층 호출용(각 층 샤프트 옆)과 캐빈 내부용(층 선택) 두 역할.
    /// 실동작은 ApartmentElevator(캐빈 두뇌)에 위임 — 같은 씬 빌더 배선 참조.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ApartmentElevatorPanel : MonoBehaviour, IInteractable
    {
        [SerializeField] private ApartmentElevator _elevator;
        [Tooltip("호출 패널이 있는 층 (캐빈 내부 패널이면 무시).")]
        [SerializeField] private int _floor = 1;
        [Tooltip("켜면 캐빈 내부 패널 — E = 층 선택 UI.")]
        [SerializeField] private bool _isCabinPanel;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;

        public void Interact(PlayerContext ctx)
        {
            if (_elevator == null) return;
            if (_isCabinPanel) _elevator.RequestFloorSelect();
            else _elevator.CallToFloor(_floor);
        }

        public void SetHighlight(bool on)
        {
            if (_renderer == null) return;
            Material material = on && _highlightMaterial != null ? _highlightMaterial : _normalMaterial;
            if (material != null) _renderer.sharedMaterial = material;
        }
    }
}
