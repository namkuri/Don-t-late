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
        // AU-009 — 발소리 보폭(m). 이동 거리 누적이 보폭을 넘을 때마다 1발.
        [SerializeField] private float _footstepStride = 1.4f;

        private PlayerManager _hub;
        private CharacterController _cc;
        private readonly HashSet<WalkableVolume> _volumes = new HashSet<WalkableVolume>();
        private float _verticalVelocity;
        private float _strideAccum;

        /// <summary>수평 속도(월드). 애니메이션·회전이 읽는다.</summary>
        public Vector3 PlanarVelocity { get; private set; }
        public bool IsGrounded => _cc.isGrounded;

        private void Awake()
        {
            _hub = GetComponent<PlayerManager>();
            _cc = GetComponent<CharacterController>();
        }

        // S-039 ② 낙사 안전망 — 맵 밖으로 떨어지면 마지막 접지점 위로 복귀.
        private const float FALL_LIMIT_Y = -6f;
        private Vector3 _lastGroundedPosition;

        // S-041: CC는 리지드바디를 밀지 않는다 — 히트 시 수평 속도를 실어 대차·상자를 민다.
        private const float PUSH_SPEED = 2.2f;

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic) return;
            if (hit.moveDirection.y < -0.3f) return; // 밟고 선 것은 밀지 않는다
            Vector3 push = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            body.linearVelocity = new Vector3(push.x * PUSH_SPEED, body.linearVelocity.y, push.z * PUSH_SPEED);
        }

        private void Update()
        {
            if (_cc.isGrounded && transform.position.y > FALL_LIMIT_Y + 2f)
                _lastGroundedPosition = transform.position;
            else if (transform.position.y < FALL_LIMIT_Y)
            {
                _cc.enabled = false; // CC는 켠 채로 transform을 옮기면 씹힌다
                transform.position = _lastGroundedPosition + Vector3.up * 1.5f;
                _cc.enabled = true;
                Debug.Log("[안전망] 낙사 감지 — 마지막 접지점 위로 복귀.");
            }

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

            TickFootstep();
        }

        /// <summary>접지 이동 거리를 누적해 보폭마다 발소리 1발 (AU-009 — 고빈도라 이벤트 금지, Instance 선례).</summary>
        private void TickFootstep()
        {
            if (!_cc.isGrounded || PlanarVelocity.sqrMagnitude < 0.01f)
            {
                _strideAccum = 0f; // 멈추면 리셋 — 재출발은 한 보폭 걸은 뒤 첫발
                return;
            }

            _strideAccum += PlanarVelocity.magnitude * Time.deltaTime;
            if (_strideAccum < _footstepStride) return;

            _strideAccum -= _footstepStride;
            WorldAudioManager.Instance?.PlayFootstepSfx();
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
