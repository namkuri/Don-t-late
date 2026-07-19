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

        private void Awake() => _hub = GetComponent<PlayerManager>();

        private void Update()
        {
            Vector3 velocity = _hub.Locomotion.PlanarVelocity;
            UpdateFacing(velocity);

            if (_animator == null) return;
            _animator.SetFloat(SpeedHash, velocity.magnitude);
            _animator.SetBool(CarryingHash, _hub.Status.IsCarrying);
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
