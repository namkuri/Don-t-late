using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 걷기 가능한 보도 구간. 박스 트리거로 Z(깊이) 이동 범위를 명시한다.
    /// PlayerLocomotionManager가 트리거 진입으로 수집해 목표 위치를 클램프한다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class WalkableVolume : MonoBehaviour
    {
        private BoxCollider _box;

        private void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true;
        }

        public Bounds Bounds => _box.bounds;

        public float ClampZ(float z) => Mathf.Clamp(z, _box.bounds.min.z, _box.bounds.max.z);

        public bool ContainsXZ(Vector3 point)
        {
            Bounds b = _box.bounds;
            return point.x >= b.min.x && point.x <= b.max.x
                && point.z >= b.min.z && point.z <= b.max.z;
        }

        private void OnDrawGizmosSelected()
        {
            BoxCollider box = _box != null ? _box : GetComponent<BoxCollider>();
            if (box == null) return;
            Gizmos.color = new Color(0.21f, 0.88f, 0.78f, 0.25f);
            Gizmos.DrawCube(box.bounds.center, box.bounds.size);
        }
    }
}
