using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// Animator 파라미터 구동 + 이동 방향 회전. 도트 감성용 45° 스냅 옵션을 갖는다.
    /// </summary>
    public class PlayerAnimationManager : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private bool _snapFacingTo45;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int CarryingHash = Animator.StringToHash("IsCarrying");
        private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");

        private PlayerManager _hub;

        /// <summary>S-116 ⑤ — 촬영 데모: 실주문 없이 운반 자세를 강제한다 (DistrictCaptureDemo).</summary>
        public bool DemoCarrying { get; set; }

        /// <summary>
        /// S-203 ② — 스크립트가 트랜스폼을 직접 옮기는 구간(엔딩 퇴장)의 걷기 속도.
        /// Locomotion을 끄면 PlanarVelocity가 멈춰 Speed 0 = Idle로 미끄러진다. 0이면 실제 이동속도를 쓴다.
        /// </summary>
        public float ScriptedSpeed { get; set; }

        private void Awake() => _hub = GetComponent<PlayerManager>();

        private void Update()
        {
            if (ScriptedSpeed > 0f)
            {
                // 회전은 이동을 지시한 쪽이 소유한다 — 멈춘 PlanarVelocity로 방향을 되돌리지 않는다.
                if (_animator != null) _animator.SetFloat(SpeedHash, ScriptedSpeed);
                return;
            }

            Vector3 velocity = _hub.Locomotion.PlanarVelocity;
            UpdateFacing(velocity);

            if (_animator == null) return;
            _animator.SetFloat(SpeedHash, velocity.magnitude);
            _animator.SetBool(CarryingHash, DemoCarrying || _hub.Status.IsCarrying);
            _animator.SetBool(GroundedHash, _hub.Locomotion.IsGrounded);
        }

        private void UpdateFacing(Vector3 velocity)
        {
            if (velocity.sqrMagnitude < 0.01f) return;

            float yaw = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
            if (_snapFacingTo45) yaw = Mathf.Round(yaw / 45f) * 45f;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(0f, yaw, 0f),
                _hub.Tuning.turnSpeed * Time.deltaTime);
        }
    }
}
