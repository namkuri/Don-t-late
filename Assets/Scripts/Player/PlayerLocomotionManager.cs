using System.Collections.Generic;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// X 자유 이동 + WalkableVolume 안에서만 허용되는 Z(깊이) 이동, 점프, 캐리 속도 페널티.
    /// 회전(facing)은 PlayerAnimationManager가 담당한다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerLocomotionManager : MonoBehaviour
    {
        private PlayerManager _hub;
        private CharacterController _cc;
        private readonly HashSet<WalkableVolume> _volumes = new HashSet<WalkableVolume>();
        private float _verticalVelocity;

        /// <summary>수평 속도(월드). 애니메이션·회전이 읽는다.</summary>
        public Vector3 PlanarVelocity { get; private set; }
        public bool IsGrounded => _cc.isGrounded;

        private void Awake()
        {
            _hub = GetComponent<PlayerManager>();
            _cc = GetComponent<CharacterController>();
        }

        private void Update()
        {
            TuningConfigSO tuning = _hub.Tuning;
            Vector2 input = _hub.Input.MoveVector;

            float speed = _hub.Input.RunHeld ? tuning.runSpeed : tuning.moveSpeed;
            if (_hub.Status.IsCarrying) speed *= tuning.carrySpeedPenalty;

            PlanarVelocity = new Vector3(input.x * speed, 0f, input.y * speed * tuning.depthSpeedRatio);

            if (_cc.isGrounded)
            {
                _verticalVelocity = -1f; // 접지 유지용 약한 하향
                if (_hub.Input.JumpPressed)
                    _verticalVelocity = Mathf.Sqrt(-2f * tuning.gravity * tuning.jumpHeight);
            }
            else
            {
                _verticalVelocity += tuning.gravity * Time.deltaTime;
            }

            Vector3 delta = (PlanarVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime;
            delta.z = ResolveDepth(transform.position + delta) - transform.position.z;
            _cc.Move(delta);
        }

        /// <summary>목표 위치의 Z를 걷기 가능 구간 안으로 되돌린다.</summary>
        private float ResolveDepth(Vector3 target)
        {
            if (_volumes.Count == 0) return target.z; // 볼륨 미배치 구간은 제한하지 않는다

            foreach (WalkableVolume volume in _volumes)
                if (volume.ContainsXZ(target)) return target.z;

            foreach (WalkableVolume volume in _volumes)
                if (volume.ContainsXZ(transform.position)) return volume.ClampZ(target.z);

            return transform.position.z;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out WalkableVolume volume)) _volumes.Add(volume);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out WalkableVolume volume)) _volumes.Remove(volume);
        }
    }
}
