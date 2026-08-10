using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DontLate
{
    /// <summary>
    /// 행인 NPC (S-052 ② → S-076 ② 확장). 시작 지점 중심 X 왕복 배회.
    /// - 신호 대기: 교차 도로 앞에서 차량 신호가 끝날 때까지(보행 가능=적신호) 멈춘다.
    /// - 회피: 전방에 행인·플레이어·장애물이 있으면 잠깐 멈췄다 계속, 오래 막히면 되돌아간다.
    /// - 반응: 플레이어가 근처를 뛰어가면 잠시 바라보고 다시 걷는다. (호감도 인사말은 소셜 후속.)
    /// - 피격: 차에 치이면 **죽지 않고 날아간다**(S-210) — 플레이어와 같은 규격의 넉백. 감지용 트리거+키네마틱 RB 장착.
    /// </summary>
    public class PedestrianNpc : MonoBehaviour, IInteractable
    {
        [Tooltip("시작 지점 기준 좌우 배회 반경(u).")]
        [SerializeField] private float _patrolHalf = 6f;
        [SerializeField] private float _speed = 1.1f;
        [Tooltip("소셜앱 프로필 id (S-080 ① — 비면 인터랙션은 되지만 등재 없음).")]
        [SerializeField] private string _npcId;
        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private Material _highlightMaterial;
        [SerializeField] private Animator _animator;
        [SerializeField] private AnimationClip _walkClip;
        private Material _normalMaterial;
        [Tooltip("교차 도로 신호등 (S-076 ② — 비면 무신호 씬).")]
        [SerializeField] private TrafficLight _signal;
        [Tooltip("도로 중심 x — 신호 대기 판정용.")]
        [SerializeField] private float _roadX;
        [SerializeField] private float _roadHalfWidth = 2.6f;

        private float _centerX;
        // S-166 ④ — 회피용 옆걸음. 종전엔 막히면 **제자리에 서 있다가 2초 뒤 되돌아갔다**.
        // 자판기·상자 앞에서 서성이거나(또는 콜라이더가 없어 그냥 통과) 보기 흉했다는 게 남규님 지적.
        // 이제 Z로 한 발 비켜서 옆으로 돌아간다 — 2.5D라 깊이 한 뼘이면 충분히 지나간다.
        private float _centerZ;
        private float _sideStep;                     // 현재 회피 오프셋 목표(원 레인 기준 ±u)
        private const float SIDESTEP_MAX = 1.2f;     // 보도를 벗어나지 않을 만큼만
        private const float SIDESTEP_RETURN = 0.55f; // 초당 복귀 속도 — 너무 빠르면 되돌아 부딪힌다
        // 비켜서는 속도는 **보행 속도와 무관**하다. _speed에 묶었더니 느린 행인은 3초 안에
        // 다 못 비켜서 포기하고 되돌아갔다(실측: _speed 0.2 → 반전). 사람도 옆걸음은 빠르게 뗀다.
        private const float SIDESTEP_SPEED = 1.6f;
        private int _direction = 1;
        private float _pauseTimer;
        private float _blockedTime;   // 회피 — 연속 막힘 누적
        private float _watchTimer;    // 뛰는 플레이어 바라보기
        private float _senseTimer;    // 주변 감지 주기
        private Transform _watched;
        private bool _interactionDialogueActive;
        private Quaternion _rotationBeforeInteraction;
        private readonly Collider[] _senseHits = new Collider[8];
        private PlayableGraph _walkGraph;
        private AnimationClipPlayable _walkPlayable;
        private Vector3 _lastAnimationPosition;
        private bool _centerTurnsOnVisuals;
        private bool _movementEnabled = true;
        private bool _movementConfigured = true; // S-217 — 배치상 걸을 수 있는 NPC인가(게이트의 상한)
        private Vector3 _homePosition;           // S-217 ④ — 사고 후 복귀 좌표
        private WalkableVolume _walkableVolume;
        private TextAsset _randomTalkSource;
        private Sprite _npcInfoBackground;
        private NpcNameLabel _npcInfoLabel;
        private RandomTalkPoolData _randomTalkPool;
        private bool _randomTalkLoaded;
        private int _lastRandomTalkIndex = -1;
        private DialogueScenarioSO _interactionScenario;

        [System.Serializable]
        private sealed class RandomTalkPoolData
        {
            public string npcId;
            public string displayName;
            public bool avoidImmediateRepeat;
            public RandomTalkLineData[] lines;
        }

        [System.Serializable]
        private sealed class RandomTalkLineData
        {
            public string speaker;
            public string text;
        }

        internal void CenterTurnsOnVisuals() => _centerTurnsOnVisuals = true;

        /// <summary>
        /// S-217 ②③ — 재생 중인 동작에 따라 걸음을 열고 닫는다(`AlternatingNpcAnimation`이 호출).
        /// 제자리 동작(인사·화내기) 중에 이동하면 발이 붙은 채 미끄러진다.
        /// `Configure(movementEnabled: false)`로 아예 정지인 NPC는 여기서 다시 켜지지 않는다 —
        /// 그건 배치 설정이고 이건 프레임 단위 게이트다.
        /// </summary>
        public void SetMovementAllowed(bool allowed)
        {
            if (!_movementConfigured) return;
            bool opening = allowed && !_movementEnabled;
            _movementEnabled = allowed;
            // S-220 — 걸음이 열리는 순간 몸을 진행 방향으로 맞춘다. 구경·대화로 다른 데를 보다가
            // 그대로 출발하면 옆으로·뒤로 걷는다. "걸어갈 땐 걸어가는 방향을 본다"의 두 번째 관문.
            if (opening && _watchTimer <= 0f && !_interactionDialogueActive) Face();
        }

        internal void Configure(bool movementEnabled, TextAsset randomTalkSource, Sprite npcInfoBackground)
        {
            _movementEnabled = movementEnabled;
            _movementConfigured = movementEnabled; // S-217 — 애초에 정지 배치면 게이트가 못 켠다
            AdoptCrossingSignal();                 // S-217 ④ — 신호등을 보고 건너게
            _randomTalkSource = randomTalkSource;
            _npcInfoBackground = npcInfoBackground;
            EnsureRandomTalkLoaded();
            EnsureInteractionPhysics();
        }

        private void Start()
        {
            if (_movementEnabled)
            {
                _walkableVolume = FindNearestWalkableVolume();
                Vector3 start = transform.position;
                start.z = ClampWalkableZ(start.z);
                transform.position = start;
            }

            _centerX = transform.position.x;
            _centerZ = transform.position.z;
            _homePosition = transform.position; // S-217 ④ — 사고로 밀려나면 돌아올 자리
            if (!_movementEnabled)
            {
                _lastAnimationPosition = transform.position;
                return;
            }

            _direction = Random.value < 0.5f ? 1 : -1;
            // 같은 씬 행인들이 발맞추지 않게 시작 위상 분산.
            transform.position += Vector3.right * Random.Range(-_patrolHalf * 0.5f, _patrolHalf * 0.5f);
            _lastAnimationPosition = transform.position;
            InitializeWalkAnimation();
            Face();
        }

        private void LateUpdate()
        {
            if (!_walkPlayable.IsValid()) return;

            bool moved = (transform.position - _lastAnimationPosition).sqrMagnitude > 0.000001f;
            _walkPlayable.SetSpeed(moved ? 1d : 0d);
            _lastAnimationPosition = transform.position;

            if (moved && _walkClip.length > 0f && _walkPlayable.GetTime() >= _walkClip.length)
                _walkPlayable.SetTime(_walkPlayable.GetTime() % _walkClip.length);
        }

        private void OnDestroy()
        {
            if (_walkGraph.IsValid()) _walkGraph.Destroy();
            if (_interactionScenario != null) Destroy(_interactionScenario);
        }

        private void InitializeWalkAnimation()
        {
            if (_animator == null || _walkClip == null) return;

            _walkGraph = PlayableGraph.Create(name + "_Walk");
            _walkGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _walkPlayable = AnimationClipPlayable.Create(_walkGraph, _walkClip);
            _walkPlayable.SetApplyFootIK(true);
            _walkPlayable.SetSpeed(0d);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_walkGraph, "Walk", _animator);
            output.SetSourcePlayable(_walkPlayable);
            _walkGraph.Play();
        }

        private void Update()
        {
            if (_flying) { FlyStep(); return; } // S-210 — 날아가는 동안엔 배회·감지 전부 정지
            if (_interactionDialogueActive)
            {
                WorldDialogueManager manager = WorldDialogueManager.Instance;
                if (manager != null && manager.IsPlaying)
                {
                    ShowNpcInfo();
                    LookAtWatched();
                    return;
                }

                _interactionDialogueActive = false;
                _watched = null;
                if (_npcInfoLabel != null) _npcInfoLabel.Show(false);
                if (_movementEnabled) Face();
                else transform.rotation = _rotationBeforeInteraction;
            }

            // S-076 ② — 뛰는 플레이어 구경: 걸음을 멈추고 그쪽을 바라본다.
            // S-081 후속(R31b) — 좌우 스냅이 아니라 실제 플레이어 방향으로 (눈을 마주친다).
            if (_watchTimer > 0f)
            {
                _watchTimer -= Time.deltaTime;
                LookAtWatched();
                // S-220 — 구경이 끝나면 **무조건** 진행 방향으로 되돌린다.
                // 종전엔 `&& _movementEnabled`가 붙어 있어, S-217의 클립 게이트가 닫힌 구간
                // (제자리 동작 중)에 구경이 끝나면 이 복귀가 통째로 건너뛰어졌다 — 플레이어를 본 채
                // 굳었다가 다음 걷기 구간에 그 방향 그대로 전진해 **문워크**가 됐다(남규님 관찰).
                // 걷지 않는 NPC라도 몸을 제자리로 돌려놓는 게 맞다.
                if (_watchTimer <= 0f) Face(); // 다시 갈 길 간다
                return;
            }

            _senseTimer -= Time.deltaTime;
            if (_senseTimer <= 0f)
            {
                _senseTimer = 0.3f;
                SenseRunner();
            }

            if (!_movementEnabled) return;

            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.deltaTime;
                return;
            }

            float x = transform.position.x;
            float nextX = x + _direction * _speed * Time.deltaTime;

            // S-076 ② — 신호 대기: 도로 밖에서 진입하려는 순간, 보행 불가(차 주행 중)면 가장자리 대기.
            bool outsideRoad = Mathf.Abs(x - _roadX) > _roadHalfWidth;
            bool entering = outsideRoad && Mathf.Abs(nextX - _roadX) <= _roadHalfWidth;
            if (entering && _signal != null && !_signal.IsWalkable) return; // 제자리 대기 (다음 프레임 재판정)

            // S-076 ② → S-166 ④ — 전방 회피: 막히면 **옆으로 비켜서 돌아간다**.
            bool blocked = FrontBlocked(out float obstacleZ);
            if (blocked)
            {
                // 장애물 반대쪽으로 비킨다. 정면으로 겹쳐 있으면(중심이 같으면) 아무 쪽이나.
                // **이미 비켜서던 쪽이 있으면 그 쪽을 지킨다** — 매번 새로 고르면 좌우로 헤맨다.
                // 막혀 있는 동안엔 목표를 계속 최대로 되돌린다: 복귀 도중 다시 막혔는데 목표가
                // 반쯤 접혀 있으면 그 자리에서 3초를 버티다 포기해 버린다(실측).
                float away = transform.position.z - obstacleZ;
                int side = Mathf.Abs(_sideStep) > 0.01f ? (int)Mathf.Sign(_sideStep)
                    : Mathf.Abs(away) < 0.05f ? (Random.value < 0.5f ? 1 : -1)
                    : away > 0f ? 1 : -1;
                _sideStep = side * SIDESTEP_MAX;
            }

            // 목표 Z로 옆걸음. 막힘이 풀리면 _sideStep이 0으로 잦아들며 원래 레인으로 돌아온다.
            float z = Mathf.MoveTowards(transform.position.z, _centerZ + _sideStep, SIDESTEP_SPEED * Time.deltaTime);
            z = ClampWalkableZ(z);

            if (blocked)
            {
                _blockedTime += Time.deltaTime;
                // 옆걸음으로도 3초를 못 뚫으면(골목 막힘) 포기하고 왔던 길로.
                if (_blockedTime > 3f) { _direction = -_direction; Face(); _blockedTime = 0f; _sideStep = 0f; }
                transform.position = new Vector3(x, transform.position.y, z); // 전진 정지·옆걸음만
                return;
            }
            _blockedTime = 0f;
            _sideStep = Mathf.MoveTowards(_sideStep, 0f, SIDESTEP_RETURN * Time.deltaTime);

            transform.position = new Vector3(nextX, transform.position.y, z);

            float offset = transform.position.x - _centerX;
            if (Mathf.Abs(offset) >= _patrolHalf)
            {
                _direction = offset > 0f ? -1 : 1;
                _pauseTimer = Random.Range(0.8f, 2.4f); // 끝에서 잠깐 두리번
                Face();
            }
        }

        // ── S-210 — 차 사고: 죽는 대신 날아간다 ─────────────────────────────
        // 종전엔 TrafficCar가 행인을 `Destroy` 했다(씬 재입장 전까지 영영 소멸). 남규님 지시로
        // **절대 죽지 않게** 바꾼다 — 사고의 코미디는 남기고 세계는 보존한다.
        // 값은 플레이어 넉백과 같은 규격: 수평은 18u/s²로 감쇠, 수직은 중력에 맡긴다
        // (PlayerLocomotionManager.ApplyKnockback + Update 감쇠와 같은 식).
        // 플레이어는 CharacterController가 밀어 주지만 행인은 트랜스폼으로 걷는 물건이라
        // 같은 식을 여기서 직접 적분한다 — 이 한 마리 때문에 CC를 붙이는 건 과하다.
        private bool _flying;
        private Vector3 _knockback;
        private float _verticalVelocity;
        private float _landingY;
        private const float KNOCK_DECAY = 18f;   // 플레이어와 동일
        private const float GRAVITY = -22f;      // 튜닝 SO를 참조하지 않는다 — 행인은 튜닝 대상이 아니다

        /// <summary>차에 치였다. 이미 날고 있으면 무시 — 같은 차에 매 프레임 재발사되는 걸 막는다.</summary>
        public void ApplyKnockback(Vector3 impulse)
        {
            if (_flying) return;
            _flying = true;
            _landingY = transform.position.y; // 맞은 자리의 지면 높이로 되돌아온다(언덕 대응)
            _knockback = new Vector3(impulse.x, 0f, impulse.z);
            _verticalVelocity = impulse.y;
        }

        private void FlyStep()
        {
            _knockback = Vector3.MoveTowards(_knockback, Vector3.zero, KNOCK_DECAY * Time.deltaTime);
            _verticalVelocity += GRAVITY * Time.deltaTime;

            Vector3 next = transform.position
                + (_knockback + Vector3.up * _verticalVelocity) * Time.deltaTime;

            if (_verticalVelocity < 0f && next.y <= _landingY)
            {
                next.y = _landingY;
                transform.position = next;
                _flying = false;

                // S-217 ④ — **걷지 못하는 NPC는 제자리로 돌아온다.**
                // 넉백(S-210)으로 도로 쪽에 떨어지면 스스로 못 걸어 나오므로 거기 서서 계속 치인다
                // (남규님 관찰: "지혜가 계속 차에 치임"). 걷는 NPC는 순찰로 알아서 복귀하므로 손대지 않는다.
                if (!_movementEnabled && _homePosition != Vector3.zero)
                {
                    transform.position = _homePosition;
                    Face();
                    _pauseTimer = 0.8f;
                    return;
                }

                _knockback = Vector3.zero;
                _verticalVelocity = 0f;
                _sideStep = 0f;      // 원래 레인으로 되돌아가며 걷는다(Update의 옆걸음 복귀가 처리)
                _blockedTime = 0f;
                _pauseTimer = 0.8f;  // 잠깐 정신 차리고
                Face();
                return;
            }

            transform.position = next;
        }

        /// <summary>
        /// 전방 장애물 검사. S-166 ④ — 가는 선(Raycast) 대신 **몸통 굵기의 구**를 던진다.
        /// 종전 레이는 눈높이(y 0.8) 한 줄이라 **바닥의 상자를 그냥 통과**했다 — 남규님이 본
        /// "상자를 안 피한다"의 절반이 이것이었다(나머지 절반은 자판기 콜라이더 부재 = ③).
        /// </summary>
        private bool FrontBlocked(out float obstacleZ)
        {
            obstacleZ = transform.position.z;
            Vector3 origin = transform.position + Vector3.up * 0.55f;
            if (!Physics.SphereCast(origin, 0.32f, Vector3.right * _direction, out RaycastHit hit, 0.9f,
                ~0, QueryTriggerInteraction.Ignore)) return false;
            if (hit.collider.transform.IsChildOf(transform)) return false;
            obstacleZ = hit.collider.bounds.center.z;
            return true;
        }

        // 근처를 뛰어가는 플레이어 감지 — 잠시 바라본다.
        private void SenseRunner()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, 3f, _senseHits,
                ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                PlayerManager player = _senseHits[i].GetComponentInParent<PlayerManager>();
                if (player == null || player.Locomotion == null) continue;
                // S-123 ⑦ — 호감도가 쌓인 사람은 지나가기만 해도 응원한다 (뛰지 않아도 발화).
                if (_gameState != null && !string.IsNullOrEmpty(_npcId) && Time.time >= _cheerReadyAt
                    && NpcAffinityLedger.Get(_gameState, _npcId) >= CHEER_AFFINITY)
                {
                    _cheerReadyAt = Time.time + CHEER_COOLDOWN;
                    _watched = player.transform;
                    _watchTimer = 1.6f;
                    ShowGreeting(Cheers[Random.Range(0, Cheers.Length)]);
                    return;
                }
                bool running = player.Input != null && player.Input.RunHeld
                    && player.Locomotion.PlanarVelocity.sqrMagnitude > 4f;
                if (!running) continue;
                _watched = player.transform;
                _watchTimer = 1.2f;
                return;
            }
        }

        // ── S-123 ④⑦ — 상자 명중(호감도−·욕) / 호감도 응원 ──

        private static readonly string[] Cheers =
        {
            "늦지마맨! 오늘도 파이팅!", "우리 동네 스타 왔네!", "무리하지 말고 쉬어가요!",
        };
        private static readonly string[] Curses =
        {
            "아! 뭐야 이거!", "사람한테 던지면 어떡해!", "눈 뜨고 던져요!",
        };

        private const int CHEER_AFFINITY = 40;   // 만남 20 + 꽃 25 = 45 → 첫 플레이 안에 도달
        /// <summary>S-124 — 소셜앱이 임계를 안내할 수 있게 노출(수치 단일 소유는 여기).</summary>
        public static int CheerAffinity => CHEER_AFFINITY;
        private const float CHEER_COOLDOWN = 20f;
        private const float HIT_COOLDOWN = 1.5f;
        private const float HIT_SPEED_MIN = 2.5f; // 굴러가는 상자·자기 이동으로 오발동하지 않게
        private const int HIT_AFFINITY_PENALTY = -15;

        private float _cheerReadyAt;
        private float _lastHitAt = -10f;

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time - _lastHitAt < HIT_COOLDOWN) return;
            if (other.GetComponent<PickupBox>() == null) return;
            Rigidbody body = other.attachedRigidbody;
            if (body == null || body.isKinematic || body.linearVelocity.magnitude < HIT_SPEED_MIN) return;

            _lastHitAt = Time.time;
            _watched = null;
            _watchTimer = 1.2f;
            // S-124 — 첫 명중도 감점한다(남규님 관찰: "욕은 하는데 호감도가 안 깎임").
            // Penalize는 만남 보너스 20을 얹지 않고 0에서 깎아 "때려서 친구 추가"도 막는다.
            string curse = Curses[Random.Range(0, Curses.Length)];
            if (_gameState != null && !string.IsNullOrEmpty(_npcId))
            {
                NpcAffinityLedger.Penalize(_gameState, _npcId, -HIT_AFFINITY_PENALTY);
                // 감점이 눈에 보여야 한다 — 수치를 말풍선에 붙인다.
                curse += "  <color=#ff7359>호감도 " + HIT_AFFINITY_PENALTY + "</color>";
            }
            ShowGreeting(curse);
        }

        private void Face()
        {
            Quaternion facing = Quaternion.Euler(0f, _direction > 0 ? 90f : -90f, 0f);
            SetRotationAroundVisualCenter(facing);
        }

        private void SetRotationAroundVisualCenter(Quaternion rotation)
        {
            if (!_centerTurnsOnVisuals || !TryGetVisualCenter(out Vector3 centerBefore))
            {
                transform.rotation = rotation;
                return;
            }

            transform.rotation = rotation;
            if (!TryGetVisualCenter(out Vector3 centerAfter)) return;

            Vector3 correction = centerBefore - centerAfter;
            correction.y = 0f;
            transform.position += correction;
            _centerX += correction.x;
            _centerZ += correction.z;
            Vector3 clamped = transform.position;
            clamped.z = ClampWalkableZ(clamped.z);
            transform.position = clamped;
            _centerZ = ClampWalkableZ(_centerZ);
        }

        private bool TryGetVisualCenter(out Vector3 center)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                center = default;
                return false;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            center = bounds.center;
            return true;
        }

        /// <summary>
        /// S-217 ④ — **신호등을 주워 온다.** 신호 대기 로직(S-076 ②)은 원래 있었는데,
        /// 런타임에 붙는 상주 NPC(`AlternatingNpcAnimation`이 AddComponent)는 빌더가 참조를
        /// 꽂아 줄 기회가 없어 `_signal`이 비어 있었다 — 그래서 지혜가 적신호에도 그냥 건너
        /// 차에 치였다(남규님 관찰). 교차로는 씬에 하나뿐이라 이름 검색 없이 타입으로 집는다.
        /// 이미 꽂혀 있으면(빌더가 넣어 준 행인) 건드리지 않는다.
        /// </summary>
        private void AdoptCrossingSignal()
        {
            if (_signal != null) return;

            TrafficLight[] lights = FindObjectsByType<TrafficLight>(FindObjectsInactive.Exclude);
            if (lights.Length == 0) return;

            TrafficLight nearest = lights[0];
            float best = Mathf.Abs(nearest.transform.position.x - transform.position.x);
            for (int i = 1; i < lights.Length; i++)
            {
                float d = Mathf.Abs(lights[i].transform.position.x - transform.position.x);
                if (d >= best) continue;
                best = d;
                nearest = lights[i];
            }

            _signal = nearest;
            _roadX = nearest.transform.position.x; // 도로 중심 = 신호등이 선 x
        }

        private WalkableVolume FindNearestWalkableVolume()
        {
            // 반입 시 정렬 인자 제거 — Unity 6.5에서 해당 오버로드가 폐기 경고(CS0618)를 낸다.
            // 여기선 정렬이 필요 없다(아래에서 최단거리 하나만 고른다).
            WalkableVolume[] volumes = FindObjectsByType<WalkableVolume>(FindObjectsInactive.Exclude);
            WalkableVolume nearest = null;
            float nearestDistance = float.MaxValue;
            Vector3 position = transform.position;
            for (int i = 0; i < volumes.Length; i++)
            {
                Bounds bounds = volumes[i].Bounds;
                float dx = Mathf.Max(bounds.min.x - position.x, 0f, position.x - bounds.max.x);
                float dz = Mathf.Max(bounds.min.z - position.z, 0f, position.z - bounds.max.z);
                float distance = dx * dx + dz * dz;
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearest = volumes[i];
            }
            return nearest;
        }

        private float ClampWalkableZ(float z)
        {
            if (_walkableVolume == null) return z;
            const float MARGIN = 0.25f;
            Bounds bounds = _walkableVolume.Bounds;
            float min = bounds.min.z + MARGIN;
            float max = bounds.max.z - MARGIN;
            return min <= max ? Mathf.Clamp(z, min, max) : bounds.center.z;
        }

        // ── S-080 ① — 인사 인터랙션: E로 말 걸면 멈춰서 바라보고 한마디, 소셜앱에 등재 ──

        private static readonly string[] Greetings =
        {
            "안녕하세요!", "오늘도 바쁘네요.", "배달 힘내요!", "날씨 좋죠?", "수고가 많아요.",
        };

        public void Interact(PlayerContext ctx)
        {
            if (ctx?.Player != null && ctx.Player.GameState != null)
                _gameState = ctx.Player.GameState;

            // S-123 ⑤ — 가방에 꽃이 있으면 선물이 인사를 대신한다(호감도 큰 폭 상승).
            // 가방에서 직접 소모하는 이유: BagItemConsumed 이벤트는 음료 마시기가 같이 듣고 있어
            // "주기"와 "마시기"가 충돌한다(정적 이벤트는 취소 불가).
            int gift = _gameState != null ? _gameState.bagItems.FindIndex(b => b.id == GIFT_ITEM_ID) : -1;
            if (gift >= 0 && !string.IsNullOrEmpty(_npcId))
            {
                BagStorage.RemoveOne(_gameState, gift);
                BagView.Instance?.Refresh();
                NpcAffinityLedger.Add(_gameState, _npcId, GIFT_AFFINITY);
                _watched = ctx.Transform;
                _watchTimer = 2f;
                ShowGreeting("어머, 꽃을... 고마워요!");
                return;
            }

            _watched = ctx.Transform;
            _rotationBeforeInteraction = transform.rotation;
            if (_gameState != null && !string.IsNullOrEmpty(_npcId))
                NpcAffinityLedger.Meet(_gameState, _npcId);
            RandomTalkLineData line = GetRandomTalkLine();
            if (!ShowInteractionDialogue(line))
            {
                _watchTimer = 2f;
                ShowGreeting(line.text);
            }
        }

        private RandomTalkLineData GetRandomTalkLine()
        {
            EnsureRandomTalkLoaded();
            RandomTalkLineData[] lines = _randomTalkPool?.lines;
            if (lines == null || lines.Length == 0)
                return new RandomTalkLineData
                {
                    speaker = _randomTalkPool?.displayName ?? gameObject.name,
                    text = Greetings[Random.Range(0, Greetings.Length)],
                };

            // S-224 — **순차 재생**(남규님 지시). 종전엔 무작위로 뽑고 직전 것만 피했는데,
            // 50줄이 되면 무작위는 "아까 그 말 또 하네"가 금방 온다(생일 문제) — 순서대로 돌면
            // 한 바퀴를 다 듣기 전엔 절대 반복되지 않는다. 작가가 순서로 흐름을 설계할 수도 있다.
            // 풀 끝에 닿으면 처음으로 돌아간다.
            int index = (_lastRandomTalkIndex + 1) % lines.Length;
            _lastRandomTalkIndex = index;
            RandomTalkLineData line = lines[index];
            if (line != null && !string.IsNullOrEmpty(line.text)) return line;
            return new RandomTalkLineData
            {
                speaker = _randomTalkPool.displayName ?? gameObject.name,
                text = Greetings[Random.Range(0, Greetings.Length)],
            };
        }

        private bool ShowInteractionDialogue(RandomTalkLineData line)
        {
            WorldDialogueManager manager = WorldDialogueManager.Instance;
            if (manager == null || line == null) return false;
            if (manager.IsPlaying) return true;

            if (_interactionScenario == null)
            {
                _interactionScenario = ScriptableObject.CreateInstance<DialogueScenarioSO>();
                _interactionScenario.hideFlags = HideFlags.HideAndDontSave;
                _interactionScenario.name = string.IsNullOrEmpty(_npcId)
                    ? gameObject.name + "_RandomTalk"
                    : _npcId + "_RandomTalk";
                _interactionScenario.lines = new[] { new DialogueScenarioSO.Line() };
            }

            DialogueScenarioSO.Line dialogueLine = _interactionScenario.lines[0];
            dialogueLine.speaker = string.IsNullOrEmpty(line.speaker)
                ? _randomTalkPool?.displayName ?? gameObject.name
                : line.speaker;
            dialogueLine.text = line.text;
            dialogueLine.portrait = null;
            _interactionDialogueActive = true;
            _watchTimer = 0f;
            ShowNpcInfo();
            manager.PlayScenario(_interactionScenario);
            return true;
        }

        private void ShowNpcInfo()
        {
            if (_npcInfoLabel == null)
            {
                _npcInfoLabel = GetComponent<NpcNameLabel>();
                if (_npcInfoLabel == null) _npcInfoLabel = gameObject.AddComponent<NpcNameLabel>();
            }

            string displayName = _randomTalkPool?.displayName;
            if (string.IsNullOrEmpty(displayName)) displayName = gameObject.name;
            _npcInfoLabel.Configure(displayName, _gameState, _npcId, _npcInfoBackground);
            _npcInfoLabel.ShowDialogueInfo();
        }

        private void LookAtWatched()
        {
            if (_watched == null) return;
            Vector3 look = _watched.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                SetRotationAroundVisualCenter(Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(look), Time.deltaTime * 10f));
        }

        private void EnsureRandomTalkLoaded()
        {
            if (_randomTalkLoaded) return;
            _randomTalkLoaded = true;
            if (_randomTalkSource == null || string.IsNullOrEmpty(_randomTalkSource.text)) return;

            _randomTalkPool = JsonUtility.FromJson<RandomTalkPoolData>(_randomTalkSource.text);
            if (_randomTalkPool != null && !string.IsNullOrEmpty(_randomTalkPool.npcId))
                _npcId = _randomTalkPool.npcId;
        }

        private void EnsureInteractionPhysics()
        {
            if (!TryGetComponent(out Collider _))
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                    Vector3 scale = transform.lossyScale;
                    float scaleX = Mathf.Max(Mathf.Abs(scale.x), 0.001f);
                    float scaleY = Mathf.Max(Mathf.Abs(scale.y), 0.001f);
                    float scaleZ = Mathf.Max(Mathf.Abs(scale.z), 0.001f);
                    CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
                    // S-218 ② — 중심의 **x·z는 0으로 못박는다**(남규님 지시).
                    // 렌더 바운즈 중심을 그대로 쓰면 메시 피벗이 치우친 모델(Tripo 산출물)에서
                    // 판정이 몸 밖으로 밀려난다 — 나아라 실측 center.x −2.53. 상호작용·피격이
                    // 허공에서 일어난다. 높이(y)만 바운즈를 따르고 좌우·앞뒤는 발밑 축에 맞춘다.
                    capsule.center = new Vector3(0f, transform.InverseTransformPoint(bounds.center).y, 0f);
                    capsule.radius = Mathf.Max(bounds.size.x / scaleX, bounds.size.z / scaleZ) * 0.35f;
                    capsule.height = Mathf.Max(bounds.size.y / scaleY, capsule.radius * 2f);
                    capsule.isTrigger = true;
                }
            }

            if (!TryGetComponent(out Rigidbody body)) body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        private const string GIFT_ITEM_ID = "flower"; // S-123 ⑤ — 선물은 꽃만 (드링크까지 포함하면 오소비)
        private const int GIFT_AFFINITY = 25;

        public void SetHighlight(bool on)
        {
            if (TryGetComponent(out NpcNameLabel nameLabel)) nameLabel.Show(on); // S-120 — 근접 이름표
            if (_bodyRenderer == null) return;
            if (_normalMaterial == null) _normalMaterial = _bodyRenderer.sharedMaterial;
            _bodyRenderer.sharedMaterial = on && _highlightMaterial != null ? _highlightMaterial : _normalMaterial;
        }

        // 머리 위 한마디 — S-123 ①에서 SpeechBubble(Utils)로 추출했다. 사용처가 셋이 되어
        // (행인 인사·상자 명중 욕/응원·주인공 독백) 렌더를 한 곳으로 모았다.
        private void ShowGreeting(string message) => SpeechBubble.ShowOn(gameObject, message);

    }
}
