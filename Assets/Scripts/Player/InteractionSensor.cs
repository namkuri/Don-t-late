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

            // S-071 ② — 손이 빈 상태에서 포커스 상자를 좌클릭하면 송장 표시.
            // (들었을 땐 좌클릭=던지기, 음료 들었을 땐 음료 던지기 — 그 경로와 충돌하지 않는 조건만.)
            // S-072 ⑤ — 폰이 열려 있어도 송장은 뜬다 (PhoneView.IsOpen 조건 제거). 대신 포인터가
            // UI 위면 무시(폰·가방 버튼 클릭이 송장으로 새는 것 방지), 송장이 이미 떠 있으면
            // 그 클릭은 닫기 몫이라 재요청하지 않는다 (닫자마자 재열림 방지 — S-072 ④).
            var mouse = UnityEngine.InputSystem.Mouse.current;
            bool overUI = UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !overUI && !InvoiceView.IsOpen
                && !_hub.Status.IsCarrying && !_hub.Status.IsHoldingDrink
                && _current is PickupBox focusedBox && focusedBox.Order != null)
                WorldEvents.RaiseInvoiceRequested(focusedBox.Order);
        }

        private void Scan()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, _hub.Tuning.interactRadius, _hits, _interactableMask, QueryTriggerInteraction.Collide);

            // S-075 ④ — 마우스 기준 포커스: 사거리 내 후보 중 마우스 레이가 맞춘 것을 최우선.
            // 마우스가 아무 후보 위에도 없으면 기존 근접 기준 폴백 (문·게이트 UX 유지).
            Collider mouseHitCollider = null;
            {
                Camera camera = Camera.main;
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (camera != null && mouse != null
                    && Physics.Raycast(camera.ScreenPointToRay(mouse.position.ReadValue()),
                        out RaycastHit mouseHit, 100f, _interactableMask, QueryTriggerInteraction.Collide))
                    mouseHitCollider = mouseHit.collider;
            }

            IInteractable nearest = null;
            float nearestDistance = float.MaxValue;

            bool carrying = _hub.Status.CarryFull; // S-055 — 두 개 들기면 꽉 찼을 때만 제외
            for (int i = 0; i < count; i++)
            {
                if (!_hits[i].TryGetComponent(out IInteractable candidate)) continue;
                if (candidate is IFocusGate gate && !gate.AllowsFocus(transform.position)) continue;
                // S-040: 상자를 든 동안엔 다른 상자를 집을 수 없다 — PickupBox가 포커스를 먹으면
                // 대차·비콘 상호작용이 막힌다 (대차 적재 2개째 불가의 원흉).
                if (carrying && candidate is PickupBox) continue;

                if (_hits[i] == mouseHitCollider) // 마우스가 짚은 후보 — 즉시 확정
                {
                    nearest = candidate;
                    break;
                }

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
            // 배송지 포커스면 주소를 HUD로 (S-021 ② — 월드 텍스트는 픽셀화에 뭉개짐).
            WorldEvents.RaiseFocusAddressChanged(_current is DeliveryPoint point ? point.Address : null);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.21f, 0.88f, 0.78f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _hub != null ? _hub.Tuning.interactRadius : 1.6f);
        }
    }
}
