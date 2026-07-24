using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 카메라 X 팔로우 — S-016 ⑤. 타겟(플레이어)의 X만 SmoothDamp로 따라간다.
    /// Y·Z·각도는 고정(ARCHITECTURE §2 — 줌·각도 변경 금지, 픽셀 밀도 보호). 데드존 안에서는 정지.
    /// S-048: 수직 적층 씬(아파트·언덕)용 Y 팔로우 옵션 — 층 오르내림을 따라간다(각도·줌은 여전히 고정).
    /// </summary>
    public class CameraFollowX : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [Tooltip("타겟이 이 반경(X)을 벗어나야 따라간다.")]
        [SerializeField] private float _deadZone = 1.5f;
        [Tooltip("클수록 무겁게 따라온다.")]
        [SerializeField] private float _smoothTime = 0.25f;
        [Tooltip("수직 적층 씬용 — 켜면 Y도 따라간다 (기준 높이 = 시작 y - 타겟 시작 y 유지).")]
        [SerializeField] private bool _followY;
        [SerializeField] private float _deadZoneY = 1.2f;

        private float _velocity;
        private float _velocityY;
        private float _heightOffset;

        private void Start()
        {
            if (_target != null) _heightOffset = transform.position.y - _target.position.y;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 position = transform.position;

            float delta = _target.position.x - position.x;
            if (Mathf.Abs(delta) > _deadZone)
            {
                float goal = _target.position.x - Mathf.Sign(delta) * _deadZone;
                position.x = Mathf.SmoothDamp(position.x, goal, ref _velocity, _smoothTime);
            }

            if (_followY)
            {
                float goalY = _target.position.y + _heightOffset;
                if (Mathf.Abs(goalY - position.y) > _deadZoneY)
                    position.y = Mathf.SmoothDamp(position.y, goalY, ref _velocityY, _smoothTime * 1.4f);
            }

            transform.position = position;
        }
    }
}
