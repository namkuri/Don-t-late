using System.Collections.Generic;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 배달 대차 (S-038 · D-067). E = 견인 토글(플레이어 뒤를 따라온다) ·
    /// 상자를 든 채 E = 대차에 적재(스택). 실린 상자는 PickupBox가 살아 있어 E로 다시 집을 수 있다.
    /// 게이트·엘리베이터가 이동시킬 때는 MoveTo로 통째 순간이동(적재분은 자식이라 동반).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DeliveryCart : MonoBehaviour, IInteractable
    {
        private const int MAX_STACK = 4;
        private const float FOLLOW_DISTANCE = 1.1f;
        private const float FOLLOW_LERP = 8f;

        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;
        [SerializeField] private Transform _stackRoot;

        private Transform _towedBy;
        private readonly List<Transform> _stack = new List<Transform>();

        public bool IsTowing => _towedBy != null;

        public void Interact(PlayerContext ctx)
        {
            // 상자를 들고 있으면 = 적재.
            DeliveryOrderSO carried = ctx.Player.Status.CarriedOrder;
            if (carried != null)
            {
                if (CountStacked() >= MAX_STACK) { Debug.Log("[대차] 만적 — " + MAX_STACK + "개까지."); return; }
                DeliveryOrderSO released = ctx.Player.Status.ReleaseCarry(dropAsPhysics: true);
                // 방금 떨어뜨린 상자(가장 가까운 in-cargo PickupBox)를 스택에 붙인다.
                PickupBox nearest = FindNearestBox(released);
                if (nearest != null) StackBox(nearest.transform);
                Debug.Log("[대차] #" + released.orderId + " 적재 — " + CountStacked() + "/" + MAX_STACK);
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
            PruneStack();
            if (_towedBy == null) return;
            Vector3 target = _towedBy.position - _towedBy.right * FOLLOW_DISTANCE;
            target.y = 0f;
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * FOLLOW_LERP);
        }

        /// <summary>게이트·엘베 이동 — 대차와 적재분을 통째로.</summary>
        public void MoveTo(Vector3 position)
        {
            transform.position = position;
        }

        private void StackBox(Transform box)
        {
            if (box.TryGetComponent(out Rigidbody body)) body.isKinematic = true;
            box.SetParent(_stackRoot != null ? _stackRoot : transform, false);
            box.localPosition = new Vector3(0f, 0.35f + CountStacked() * 0.72f, 0f);
            box.localRotation = Quaternion.identity;
            _stack.Add(box);
        }

        // 스택에서 다시 집어간(부모가 바뀐) 상자는 목록에서 잊는다.
        private void PruneStack()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                Transform box = _stack[i];
                Transform anchor = _stackRoot != null ? _stackRoot : transform;
                if (box == null || box.parent != anchor) _stack.RemoveAt(i);
            }
        }

        private int CountStacked()
        {
            PruneStack();
            return _stack.Count;
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
