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
                bool running = player.Input != null && player.Input.RunHeld
                    && player.Locomotion.PlanarVelocity.sqrMagnitude > 4f;
                if (!running) continue;
                _watched = player.transform;
                _watchTimer = 1.2f;
                return;
            }
        }

        private void Face() => transform.rotation = Quaternion.Euler(0f, _direction > 0 ? 90f : -90f, 0f);

        // ── S-080 ① — 인사 인터랙션: E로 말 걸면 멈춰서 바라보고 한마디, 소셜앱에 등재 ──

        private static readonly string[] Greetings =
        {
            "안녕하세요!", "오늘도 바쁘네요.", "배달 힘내요!", "날씨 좋죠?", "수고가 많아요.",
        };

        public void Interact(PlayerContext ctx)
        {
            _watched = ctx.Transform;
            _watchTimer = 2f; // 잠시 멈춰 바라본다
            if (_gameState != null && !string.IsNullOrEmpty(_npcId))
                NpcAffinityLedger.Meet(_gameState, _npcId);
            ShowGreeting(Greetings[Random.Range(0, Greetings.Length)]);
        }

        public void SetHighlight(bool on)
        {
            if (TryGetComponent(out NpcNameLabel nameLabel)) nameLabel.Show(on); // S-120 — 근접 이름표
            if (_bodyRenderer == null) return;
            if (_normalMaterial == null) _normalMaterial = _bodyRenderer.sharedMaterial;
            _bodyRenderer.sharedMaterial = on && _highlightMaterial != null ? _highlightMaterial : _normalMaterial;
        }

        // 머리 위 인사말 — 풀해상 오버레이 1.6초 (BoxDurability HP바 패턴의 초경량판).
        private GameObject _greetCanvasGo;

        private void OnDestroy()
        {
            if (_greetCanvasGo != null) Destroy(_greetCanvasGo);
        }

        private void ShowGreeting(string message)
        {
            if (_greetCanvasGo != null) Destroy(_greetCanvasGo);
            _greetCanvasGo = new GameObject("GreetCanvas");
            Canvas canvas = _greetCanvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6;
            var label = new GameObject("Greet", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
            label.transform.SetParent(_greetCanvasGo.transform, false);
            if (UiOverlayFont.Korean != null) label.font = UiOverlayFont.Korean;
            label.fontSize = 22f;
            label.fontStyle = TMPro.FontStyles.Bold;
            label.color = new Color(1f, 1f, 1f, 1f);
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            label.rectTransform.sizeDelta = new Vector2(300f, 30f);
            label.text = message;
            StartCoroutine(GreetFollow(label));
        }

        private System.Collections.IEnumerator GreetFollow(TMPro.TMP_Text label)
        {
            float t = 0f;
            Camera camera = Camera.main;
            while (t < 1.6f && camera != null && label != null)
            {
                t += Time.deltaTime;
                Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 2.1f);
                if (screen.z > 0f) label.rectTransform.position = new Vector3(screen.x, screen.y, 0f);
                label.color = new Color(1f, 1f, 1f, t < 1.2f ? 1f : 1f - (t - 1.2f) / 0.4f);
                yield return null;
            }
            if (_greetCanvasGo != null) { Destroy(_greetCanvasGo); _greetCanvasGo = null; }
        }
    }
}
