using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 근접한 IInteractable 중 가장 가까운 하나를 골라 하이라이트하고, 입력 시 실행한다.
    /// </summary>
    public class InteractionSensor : MonoBehaviour
    {
        // 8이면 District처럼 콜라이더가 많은 씬에서 버퍼가 비상호작용물로 차 비콘이 밀려난다
        // (S-009 실측: "E가 거의 발동 안 됨"의 범인). 후보 수집은 넉넉히.
        private const int MAX_HITS = 32;

        [SerializeField] private LayerMask _interactableMask = ~0;

        private readonly Collider[] _hits = new Collider[MAX_HITS];
        private PlayerManager _hub;
        private PlayerContext _context;
        private IInteractable _current;

        public IInteractable Current => _current;

        private void Awake()
        {
            _hub = GetComponentInParent<PlayerManager>();
            _context = new PlayerContext(_hub);
        }

        private void Update()
        {
            Scan();
            if (_current != null && _hub.Input.InteractPressed) _current.Interact(_context);
        }

        private void Scan()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, _hub.Tuning.interactRadius, _hits, _interactableMask, QueryTriggerInteraction.Collide);

            IInteractable nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (!_hits[i].TryGetComponent(out IInteractable candidate)) continue;
                if (candidate is IFocusGate gate && !gate.AllowsFocus(transform.position)) continue;

                float distance = (_hits[i].transform.position - transform.position).sqrMagnitude;
                if (distance >= nearestDistance) continue;

                nearest = candidate;
                nearestDistance = distance;
            }

            if (ReferenceEquals(nearest, _current)) return;

            _current?.SetHighlight(false);
            _current = nearest;
            _current?.SetHighlight(true);

            WorldEvents.RaiseInteractionFocusChanged(_current != null);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.21f, 0.88f, 0.78f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _hub != null ? _hub.Tuning.interactRadius : 1.6f);
        }
    }
}
