using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 배달 대차 (S-038 → S-040 물리 재설계). E = 견인 토글 · 상자 든 채 E = 바구니에 투하(실물리).
    /// 상자는 파지 않고 물리로 바구니(사방 벽·위 개방)에 담긴다 — 위로는 튀어나갈 수 있다.
    /// 벽·바닥 콜라이더는 CartWall 레이어(플레이어와 충돌 무시 — S-039 낙사 회귀 방지).
    /// 게이트·엘베는 MoveTo로 대차+바구니 속 상자를 통째 이동.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DeliveryCart : MonoBehaviour, IInteractable
    {
        private const float FOLLOW_DISTANCE = 1.4f;
        private const float FOLLOW_LERP = 8f;
        private const float BASKET_RADIUS = 1.6f; // MoveTo 동반 판정 (수평)
        private const float BASKET_HEIGHT = 2.0f;

        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;
        [SerializeField] private Transform _dropPoint; // 적재 투하 지점 (바구니 위)

        private Transform _towedBy;

        public bool IsTowing => _towedBy != null;

        public void Interact(PlayerContext ctx)
        {
            // 상자를 들고 있으면 = 바구니에 투하 (실물리 — 벽이 가둔다).
            DeliveryOrderSO carried = ctx.Player.Status.CarriedOrder;
            if (carried != null)
            {
                ctx.Player.Status.ReleaseCarry(dropAsPhysics: true);
                PickupBox box = FindNearestBox(carried);
                if (box != null)
                {
                    Vector3 drop = _dropPoint != null ? _dropPoint.position : transform.position + Vector3.up * 1.2f;
                    box.transform.position = drop + new Vector3(Random.Range(-0.15f, 0.15f), 0f, Random.Range(-0.1f, 0.1f));
                    if (box.TryGetComponent(out Rigidbody body)) { body.isKinematic = false; body.linearVelocity = Vector3.zero; }
                }
                Debug.Log("[대차] #" + carried.orderId + " 바구니 투하 — 현재 " + CountInBasket() + "개.");
                return;
            }

            // 빈손 E = 견인 토글.
            _towedBy = _towedBy == null ? ctx.Player.transform : null;
            Debug.Log(_towedBy != null ? "[대차] 견인 시작" : "[대차] 견인 해제");
        }

        public void SetHighlight(bool on)
        {
            if (_renderer == null) return;
            Material material = on && _highlightMaterial != null ? _highlightMaterial : _normalMaterial;
            if (material != null) _renderer.sharedMaterial = material;
        }

        private void Update()
        {
            if (_towedBy == null) return;
            Vector3 target = _towedBy.position - _towedBy.right * FOLLOW_DISTANCE;
            target.y = 0f;
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * FOLLOW_LERP);
        }

        /// <summary>게이트·엘베 이동 — 대차와 바구니 속 상자를 같은 델타로 통째 이동.</summary>
        public void MoveTo(Vector3 position)
        {
            Vector3 delta = position - transform.position;
            foreach (PickupBox box in Object.FindObjectsByType<PickupBox>())
                if (IsInBasket(box.transform.position))
                    box.transform.position += delta;
            transform.position = position;
        }

        private bool IsInBasket(Vector3 point)
        {
            Vector3 local = point - transform.position;
            return Mathf.Abs(local.x) <= BASKET_RADIUS && Mathf.Abs(local.z) <= BASKET_RADIUS
                && local.y >= -0.2f && local.y <= BASKET_HEIGHT;
        }

        private int CountInBasket()
        {
            int count = 0;
            foreach (PickupBox box in Object.FindObjectsByType<PickupBox>())
                if (IsInBasket(box.transform.position)) count++;
            return count;
        }

        private PickupBox FindNearestBox(DeliveryOrderSO order)
        {
            PickupBox nearest = null;
            float best = float.MaxValue;
            foreach (PickupBox box in Object.FindObjectsByType<PickupBox>())
            {
                if (box.Order != order) continue;
                float distance = (box.transform.position - transform.position).sqrMagnitude;
                if (distance < best) { best = distance; nearest = box; }
            }
            return nearest;
        }
    }
}
