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
        private float _masteryAccum; // S-063 — 주행 거리 누적
        private bool _wasGrounded = true; // AU-018 ③ — 착지 엣지 감지(공중→접지 전환에 land음)

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

        // S-049 → S-053 ①: 비 오는 날은 어디서든 미끄럼 — 관성 수렴. 언덕 비포장은 더 미끄럽다.
        private bool _inHillside;
        private bool _raining;
        private Vector3 _planarInertia;
        private const float SLIPPERY_ACCEL_RAIN = 7.5f;      // 일반 노면 (낮을수록 미끄럽다)
        private const float SLIPPERY_ACCEL_HILL = 4.5f;      // 언덕 비포장 × 비

        private void OnEnable()
        {
            WorldEvents.SceneTransitionCompleted += OnSceneArrivedLoco;
            WorldEvents.WeatherChanged += OnWeatherChangedLoco;
        }

        private void OnDisable()
        {
            WorldEvents.SceneTransitionCompleted -= OnSceneArrivedLoco;
            WorldEvents.WeatherChanged -= OnWeatherChangedLoco;
        }

        private void OnSceneArrivedLoco(GameScene scene) => _inHillside = scene == GameScene.Hillside;
        private void OnWeatherChangedLoco(WeatherType weather) => _raining = weather == WeatherType.Rain;

        // S-041: CC는 리지드바디를 밀지 않는다 — 히트 시 수평 속도를 실어 대차·상자를 민다.
        private const float PUSH_SPEED = 2.2f;

        // S-066 ③ — 차 충돌 넉백: 수평은 감쇠 속도로, 수직은 점프 속도로 실린다.
        private Vector3 _knockback;

        public void ApplyKnockback(Vector3 impulse)
        {
            _knockback = new Vector3(impulse.x, 0f, impulse.z);
            _verticalVelocity = Mathf.Max(_verticalVelocity, impulse.y);
        }

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

            // S-081 ② — 탈진(스태미나 0): 달리기 불가 (점프도 아래에서 차단).
            bool exhausted = _hub.Status != null && _hub.Status.Stamina <= 0f;
            float speed = _hub.Input.RunHeld && !exhausted ? tuning.runSpeed : tuning.moveSpeed;
            if (_hub.Status.IsCarrying) speed *= tuning.carrySpeedPenalty;
            speed *= _hub.Status.SpeedMultiplier; // S-074 ⑧ — 드링크 버프 (+30%)

            Vector3 targetPlanar = new Vector3(input.x * speed, 0f, input.y * speed * tuning.depthSpeedRatio);
            if (_raining)
            {
                // S-053 ① 미끄럼 — 가감속이 굼떠진다 (멈추려 해도 밀리고, 출발도 굼뜸).
                float accel = _inHillside ? SLIPPERY_ACCEL_HILL : SLIPPERY_ACCEL_RAIN;
                _planarInertia = Vector3.MoveTowards(_planarInertia, targetPlanar, accel * Time.deltaTime);
                PlanarVelocity = _planarInertia;
            }
            else
            {
                _planarInertia = targetPlanar;
                PlanarVelocity = targetPlanar;
            }

            if (_cc.isGrounded)
            {
                _verticalVelocity = -1f; // 접지 유지용 약한 하향
                if (_hub.Input.JumpPressed && !exhausted) // S-081 ②
                {
                    _verticalVelocity = Mathf.Sqrt(-2f * tuning.gravity * tuning.jumpHeight);
                    WorldAudioManager.Instance?.PlayJumpSfx(); // AU-018 ③
                }
            }
            else
            {
                _verticalVelocity += tuning.gravity * Time.deltaTime;
            }

            _knockback = Vector3.MoveTowards(_knockback, Vector3.zero, 18f * Time.deltaTime); // S-066 ③ 감쇠
            Vector3 delta = (PlanarVelocity + _knockback + Vector3.up * _verticalVelocity) * Time.deltaTime;
            delta.z = ResolveDepth(transform.position + delta) - transform.position.z;
            _cc.Move(delta);

            // AU-018 ③ — 공중→접지 전환에 착지음 (스폰·안전망 복귀는 _wasGrounded 초기 true로 억제).
            bool groundedNow = _cc.isGrounded;
            if (groundedNow && !_wasGrounded) WorldAudioManager.Instance?.PlayLandSfx();
            _wasGrounded = groundedNow;

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

            float moved = PlanarVelocity.magnitude * Time.deltaTime;
            _masteryAccum += moved; // S-063 — 주행 50m당 숙련도 +1 (밸런스 추후)
            if (_masteryAccum >= MasteryProgress.RUN_METERS_PER_POINT)
            {
                _masteryAccum -= MasteryProgress.RUN_METERS_PER_POINT;
                MasteryProgress.Add(_hub.GameState, 1f);
            }

            _strideAccum += moved;
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
