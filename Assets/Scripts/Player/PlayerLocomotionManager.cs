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
        private bool _snowing; // S-084 ③ — 눈은 비의 2배 미끄럽다
        private Vector3 _planarInertia;
        private const float SLIPPERY_ACCEL_RAIN = 7.5f;      // 일반 노면 (낮을수록 미끄럽다)
        private const float SLIPPERY_ACCEL_HILL = 4.5f;      // 언덕 비포장 × 비

        // S-119 ③ — 차 사고 부상: 이동·점프 입력 무시 (넉백 궤적·중력은 유지 — 날아가는 연출).
        // 해제는 "치료 후 집으로" 씬 전이 시 플레이어 재생성으로 자연히 이뤄진다.
        private bool _injured;

        // S-123 ③ — 대화 중 이동 잠금(방해요소): 박말순 전화·행인 대화가 붙으면 발이 묶인다.
        // InputAction을 만지지 않는 이유: 미니게임(진상 전화)이 끝날 때 _move.Enable()이
        // 대화 잠금까지 풀어버린다 — _injured와 같은 플래그 패턴이 그 충돌을 구조적으로 막는다.
        private bool _inDialogue;

        private void OnEnable()
        {
            WorldEvents.SceneTransitionCompleted += OnSceneArrivedLoco;
            WorldEvents.WeatherChanged += OnWeatherChangedLoco;
            WorldEvents.PlayerHitByCar += OnHitByCarLoco;
            WorldEvents.DialogueStarted += OnDialogueStartedLoco;
            WorldEvents.DialogueEnded += OnDialogueEndedLoco;
        }

        private void OnDisable()
        {
            WorldEvents.SceneTransitionCompleted -= OnSceneArrivedLoco;
            WorldEvents.WeatherChanged -= OnWeatherChangedLoco;
            WorldEvents.PlayerHitByCar -= OnHitByCarLoco;
            WorldEvents.DialogueStarted -= OnDialogueStartedLoco;
            WorldEvents.DialogueEnded -= OnDialogueEndedLoco;
        }

        private void OnHitByCarLoco() => _injured = true;
        private void OnDialogueStartedLoco(string _) => _inDialogue = true;
        private void OnDialogueEndedLoco(string _) => _inDialogue = false;

        private void OnSceneArrivedLoco(GameScene scene) => _inHillside = scene == GameScene.Hillside;

        // S-082 ⑤ — 플레이어는 씬마다 새로 태어난다: WeatherChanged 재수신 전까지 비를 몰라
        // "캠프에선 안 미끄러짐"이 나던 구멍 — 현재 날씨를 기동 시 즉시 조회.
        private void Start()
        {
            if (WorldWeatherManager.Instance != null)
            {
                _raining = WorldWeatherManager.Instance.Weather == WeatherType.Rain;
                _snowing = WorldWeatherManager.Instance.Weather == WeatherType.Snow; // S-084 ③
            }
            // S-123 ③ — 대화 도중 태어난 플레이어 보정 (씬 전환 직후 대화가 이어지는 경우).
            if (WorldDialogueManager.Instance != null) _inDialogue = WorldDialogueManager.Instance.IsPlaying;
        }
        private void OnWeatherChangedLoco(WeatherType weather)
        {
            _raining = weather == WeatherType.Rain;
            _snowing = weather == WeatherType.Snow; // S-084 ③
        }

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
            Vector2 input = _injured || _inDialogue ? Vector2.zero : _hub.Input.MoveVector; // S-119 ③ · S-123 ③

            // S-081 ② — 탈진(스태미나 0): 달리기 불가 (점프도 아래에서 차단).
            // S-116 ⑤ — 촬영 데모 중엔 탈진 페널티 면제(왕복 연출이 끊기면 안 된다) + 속도 배수.
            bool exhausted = _hub.Status != null && _hub.Status.Stamina <= 0f && !_hub.Input.DemoActive;
            float speed = _hub.Input.RunHeld && !exhausted ? tuning.runSpeed : tuning.moveSpeed;
            speed *= _hub.Input.DemoSpeedMultiplier;
            if (_hub.Status.IsCarrying) speed *= tuning.carrySpeedPenalty;
            speed *= _hub.Status.SpeedMultiplier; // S-074 ⑧ — 드링크 버프 (+30%)

            // S-088 ⑤ — 태풍 바람: 맞바람 보행 감속·순풍 가속·정지 시 바람 방향으로 밀림.
            float windX = WorldWeatherManager.Instance != null ? WorldWeatherManager.Instance.WindX : 0f;
            if (windX != 0f && Mathf.Abs(input.x) > 0.01f)
            {
                bool tailwind = Mathf.Sign(input.x) == Mathf.Sign(windX);
                speed *= tailwind ? tuning.stormTailwindMultiplier : tuning.stormHeadwindMultiplier;
            }

            Vector3 targetPlanar = new Vector3(input.x * speed, 0f, input.y * speed * tuning.depthSpeedRatio);
            // S-090 ⑤ — 정지 밀림 폐지(남규님 재판정): WASD 미조작 시 바람 무영향.
            Vector3 windPush = Vector3.zero;
            if (_raining || _snowing)
            {
                // S-053 ① 미끄럼 — 가감속이 굼떠진다. S-084 ③: 눈은 비의 2배(accel 절반), 전역 적용.
                float accel = _inHillside ? SLIPPERY_ACCEL_HILL : SLIPPERY_ACCEL_RAIN;
                if (_snowing) accel *= 0.5f;
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
                if (_hub.Input.JumpPressed && !exhausted && !_injured && !_inDialogue) // S-081 ② · S-119 ③ · S-123 ③
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
            Vector3 delta = (PlanarVelocity + windPush + _knockback + Vector3.up * _verticalVelocity) * Time.deltaTime;
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
