using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 카메라 X 팔로우 — S-016 ⑤. 타겟(플레이어)의 X만 SmoothDamp로 따라간다.
    /// Y·Z·각도는 고정(ARCHITECTURE §2 — 줌·각도 변경 금지, 픽셀 밀도 보호). 데드존 안에서는 정지.
    /// </summary>
    public class CameraFollowX : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [Tooltip("타겟이 이 반경(X)을 벗어나야 따라간다.")]
        [SerializeField] private float _deadZone = 1.5f;
        [Tooltip("클수록 무겁게 따라온다.")]
        [SerializeField] private float _smoothTime = 0.25f;

        private float _velocity;

        private void LateUpdate()
        {
            if (_target == null) return;

            float delta = _target.position.x - transform.position.x;
            if (Mathf.Abs(delta) <= _deadZone) return;

            float goal = _target.position.x - Mathf.Sign(delta) * _deadZone;
            float x = Mathf.SmoothDamp(transform.position.x, goal, ref _velocity, _smoothTime);
            transform.position = new Vector3(x, transform.position.y, transform.position.z);
        }
    }
}
