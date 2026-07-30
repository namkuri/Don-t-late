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

            // S-076 ② — 전방 회피: 행인·플레이어·장애물이 코앞이면 잠깐 멈춤, 오래 막히면 반전.
            if (FrontBlocked())
            {
                _blockedTime += Time.deltaTime;
                if (_blockedTime > 2f) { _direction = -_direction; Face(); _blockedTime = 0f; }
                return;
            }
            _blockedTime = 0f;

            transform.position = new Vector3(nextX, transform.position.y, transform.position.z);

            float offset = transform.position.x - _centerX;
            if (Mathf.Abs(offset) >= _patrolHalf)
            {
                _direction = offset > 0f ? -1 : 1;
                _pauseTimer = Random.Range(0.8f, 2.4f); // 끝에서 잠깐 두리번
                Face();
            }
        }

        private bool FrontBlocked()
        {
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            if (!Physics.Raycast(origin, Vector3.right * _direction, out RaycastHit hit, 0.8f,
                ~0, QueryTriggerInteraction.Ignore)) return false;
            if (hit.collider.transform.IsChildOf(transform)) return false;
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
            // 처음 보는 행인은 감점하지 않는다 — Ledger.Add가 Meet을 먼저 부르므로 미등재 상태에서
            // 맞히면 "호감도 20으로 등재된 뒤 5"라는 역설(때려서 친구 추가)이 생긴다.
            if (_gameState != null && !string.IsNullOrEmpty(_npcId)
                && _gameState.npcAffinities.Exists(n => n.npcId == _npcId))
                NpcAffinityLedger.Add(_gameState, _npcId, HIT_AFFINITY_PENALTY);
            ShowGreeting(Curses[Random.Range(0, Curses.Length)]);
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
