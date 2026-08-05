using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 행인 NPC (S-052 ② → S-076 ② 확장). 시작 지점 중심 X 왕복 배회.
    /// - 신호 대기: 교차 도로 앞에서 차량 신호가 끝날 때까지(보행 가능=적신호) 멈춘다.
    /// - 회피: 전방에 행인·플레이어·장애물이 있으면 잠깐 멈췄다 계속, 오래 막히면 되돌아간다.
    /// - 반응: 플레이어가 근처를 뛰어가면 잠시 바라보고 다시 걷는다. (호감도 인사말은 소셜 후속.)
    /// - 피격: 차에 치이면 소멸(TrafficCar 몫) — 씬 재입장 시 복귀. 감지용 트리거+키네마틱 RB 장착.
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
        private readonly Collider[] _senseHits = new Collider[8];

        private void Start()
        {
            _centerX = transform.position.x;
            _centerZ = transform.position.z;
            _direction = Random.value < 0.5f ? 1 : -1;
            // 같은 씬 행인들이 발맞추지 않게 시작 위상 분산.
            transform.position += Vector3.right * Random.Range(-_patrolHalf * 0.5f, _patrolHalf * 0.5f);
            Face();
        }

        private void Update()
        {
            // S-076 ② — 뛰는 플레이어 구경: 걸음을 멈추고 그쪽을 바라본다.
            // S-081 후속(R31b) — 좌우 스냅이 아니라 실제 플레이어 방향으로 (눈을 마주친다).
            if (_watchTimer > 0f)
            {
                _watchTimer -= Time.deltaTime;
                if (_watched != null)
                {
                    Vector3 look = _watched.position - transform.position;
                    look.y = 0f;
                    if (look.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.Slerp(transform.rotation,
                            Quaternion.LookRotation(look), Time.deltaTime * 10f);
                }
                if (_watchTimer <= 0f) Face(); // 다시 갈 길 간다
                return;
            }

            _senseTimer -= Time.deltaTime;
            if (_senseTimer <= 0f)
            {
                _senseTimer = 0.3f;
                SenseRunner();
            }

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

        private void Face() => transform.rotation = Quaternion.Euler(0f, _direction > 0 ? 90f : -90f, 0f);

        // ── S-080 ① — 인사 인터랙션: E로 말 걸면 멈춰서 바라보고 한마디, 소셜앱에 등재 ──

        private static readonly string[] Greetings =
        {
            "안녕하세요!", "오늘도 바쁘네요.", "배달 힘내요!", "날씨 좋죠?", "수고가 많아요.",
        };

        public void Interact(PlayerContext ctx)
        {
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
            _watchTimer = 2f; // 잠시 멈춰 바라본다
            if (_gameState != null && !string.IsNullOrEmpty(_npcId))
                NpcAffinityLedger.Meet(_gameState, _npcId);
            ShowGreeting(Greetings[Random.Range(0, Greetings.Length)]);
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
