using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 캠프 사장님 NPC (S-052 ①). 첫 캠프 방문: 플레이어에게 걸어와 튜토리얼 시나리오를 재생하고
    /// 제자리로 복귀. 이후 방문: 구석에 서 있다가 말 걸면(E) 격려 대사 — 간혹 자리를 비운다(추첨).
    /// 플레이어 발견은 Find 금지 규칙에 따라 물리 쿼리(OverlapSphere 저빈도 폴링)로 실측한다.
    /// </summary>
    public class CampBossNpc : MonoBehaviour, IInteractable
    {
        private const float APPROACH_SPEED = 2.2f;
        private const float TALK_DISTANCE = 1.6f;
        private const float DETECT_RADIUS = 12f;
        private const float POLL_INTERVAL = 0.4f;

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private DialogueScenarioSO _tutorialScenario;
        // S-164 — 진행부가 **Core 상주**로 옮겨져 씬 참조로는 못 잡는다(씬이 다르다).
        // World 싱글톤 규약대로 `Instance`로 부른다 — 명령 호출용이라 규칙에 맞는다.
        private CampTutorialDirector _tutorial => CampTutorialDirector.Instance;
        [SerializeField] private DialogueScenarioSO[] _cheerScenarios;
        // S-171 — 부재 확률 필드는 제거했다(미사용 필드는 워닝 = 납품 불가). 되살릴 땐 Start의
        // 재방문 분기에 확률 조건을 다시 끼우면 된다.
        [SerializeField] private Renderer _highlightRenderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;
        [SerializeField] private Animator _animator;

        private enum Phase { Waiting, Approaching, Talking, Returning, Idle }
        private Phase _phase = Phase.Idle;
        private Vector3 _homePosition;
        private Quaternion _frontRotation;
        private Transform _player;
        private float _pollTimer;
        // S-202 — 16칸은 위험하다. `OverlapSphereNonAlloc`은 **가까운 순서가 아니라 임의 순서**로
        // 채우고 버퍼가 차면 나머지를 버린다. 반경 12u 실측이 콜라이더 13개(플레이어가 13번째)라
        // 여유가 3칸뿐이었다 — 무대에 소품이 몇 개만 늘어도 사장님이 플레이어를 못 보게 된다.
        // 같은 결함이 엔딩에서 이미 터졌다(WorldEndingManager, 22개 중 22번째로 밀림).
        // 주기 폴링이라 할당은 피하고 버퍼만 넉넉히 잡는다.
        private readonly Collider[] _hits = new Collider[64];

        // S-096 — 상자 손상 잔소리 멘트 풀 (랜덤 · 연속 중복 회피)
        private static readonly string[] SCOLD_LINES =
        {
            "이봐! 물건 안 부서지게 조심해!",
            "어이어이, 그거 다 돈이야 돈!",
            "살살 다뤄! 파손나면 월급에서 깐다?",
            "택배가 무슨 죄야, 던지지 마!",
            "취급주의 스티커 안 보여?!",
            "그렇게 다루면 별점 나락 간다니까.",
        };
        private GameObject _scoldCanvasGo;
        private Coroutine _scoldRoutine; // S-102 — 구 코루틴 정지용 (연타 시 새 말풍선을 죽이던 결함 수리)
        private int _lastScoldIndex = -1;
        private string _animationState;

        private void OnEnable()  { WorldEvents.PackageDamaged += OnPackageDamaged; }
        private void OnDisable() { WorldEvents.PackageDamaged -= OnPackageDamaged; }

        private void OnDestroy()
        {
            if (_scoldCanvasGo != null) Destroy(_scoldCanvasGo);
        }

        private void Start()
        {
            _homePosition = transform.position;
            _frontRotation = transform.rotation;

            if (_gameState != null && _gameState.bossIntroPlayed)
            {
                // S-171 — **부재 추첨 폐지**(남규님: 항상 있도록). 사장님은 캠프의 길잡이다 —
                // 게시판·상차·정산이 다 여기서 시작하는데 25%로 사라지면 그날은 물어볼 데가 없다.
                // 확률 필드는 남긴다: 되살릴 때 이 자리에 조건만 다시 끼우면 된다.
                _phase = Phase.Idle;
            }
            else
            {
                _phase = Phase.Waiting; // 첫 방문 — 플레이어를 기다렸다 다가간다
            }
            SetAnimation("Idle");
            FaceFront();
        }

        private void Update()
        {
            switch (_phase)
            {
                case Phase.Waiting: PollForPlayer(); break;
                case Phase.Approaching: Approach(); break;
                case Phase.Talking: WaitTalkEnd(); break;
                case Phase.Returning: ReturnHome(); break;
            }
        }

        private void PollForPlayer()
        {
            _pollTimer -= Time.deltaTime;
            if (_pollTimer > 0f) return;
            _pollTimer = POLL_INTERVAL;

            int count = Physics.OverlapSphereNonAlloc(transform.position, DETECT_RADIUS, _hits);
            for (int i = 0; i < count; i++)
            {
                PlayerManager player = _hits[i].GetComponentInParent<PlayerManager>();
                if (player == null) continue;
                _player = player.transform;
                _phase = Phase.Approaching;
                SetAnimation("Walk");
                return;
            }
        }

        private void Approach()
        {
            if (_player == null) { _phase = Phase.Idle; SetAnimation("Idle"); return; }
            Vector3 target = _player.position;
            target.y = transform.position.y;
            // 접근 중에는 이동 방향과 시선을 맞춘다. 대화 종료 후에는 정면으로 복귀한다.
            FaceTowards(target);
            if (Vector3.Distance(transform.position, target) <= TALK_DISTANCE)
            {
                // S-146 — 진행부가 붙어 있으면 **7단계 튜토리얼**을 넘긴다(대사+행동 검증).
                // 없으면 종전대로 한 편짜리 시나리오만 재생한다(폴백 — 다른 씬 재사용 대비).
                if (_tutorial != null)
                {
                    _tutorial.Begin(_player);
                }
                else if (WorldDialogueManager.Instance != null && _tutorialScenario != null)
                {
                    WorldDialogueManager.Instance.PlayScenario(_tutorialScenario);
                }
                if (_gameState != null) _gameState.bossIntroPlayed = true;
                _phase = Phase.Talking;
                SetAnimation("Talk");
                Debug.Log("[사장님] 첫 방문 튜토리얼 시작.");
                return;
            }
            transform.position = Vector3.MoveTowards(transform.position, target, APPROACH_SPEED * Time.deltaTime);
        }

        private void WaitTalkEnd()
        {
            if (WorldDialogueManager.Instance != null && WorldDialogueManager.Instance.IsPlaying)
            {
                if (_player != null) FaceTowards(_player.position);
                return;
            }
            // S-146 — 튜토리얼은 대사 사이에 **행동 대기 구간**이 있어 그때마다 대화가 멈춘다.
            // 대화 중단만 보고 복귀하면 1단계 만에 사장님이 돌아가 버린다 — 진행부가 끝나야 간다.
            if (_tutorial != null && _tutorial.Running)
            {
                if (_player != null) FaceTowards(_player.position);
                return;
            }
            _phase = Phase.Returning;
            SetAnimation("Walk");
            FaceTowards(_homePosition);
        }

        private void ReturnHome()
        {
            FaceTowards(_homePosition);
            transform.position = Vector3.MoveTowards(transform.position, _homePosition, APPROACH_SPEED * Time.deltaTime);
            if (Vector3.Distance(transform.position, _homePosition) < 0.05f)
            {
                FaceFront();
                _phase = Phase.Idle;
                SetAnimation("Idle");
            }
        }

        private void FaceTowards(Vector3 target)
        {
            Vector3 dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, 360f * Time.deltaTime);
        }

        public void Interact(PlayerContext ctx)
        {
            // S-155 — 튜토리얼 중 E는 **직전 안내를 다시 듣기**다(남규님 지시: 실수로 넘겨도
            // 되찾을 수 있게). 종전엔 `_phase != Phase.Idle`이라 그냥 무시돼, 놓치면 끝이었다.
            if (_tutorial != null && _tutorial.Running)
            {
                FaceTowards(ctx.Transform.position);
                _tutorial.TryRepeatCurrentLine();
                return;
            }

            if (_phase != Phase.Idle) return;
            if (WorldDialogueManager.Instance == null || WorldDialogueManager.Instance.IsPlaying) return;
            if (_cheerScenarios == null || _cheerScenarios.Length == 0) return;
            _player = ctx.Transform;
            FaceTowards(ctx.Transform.position);
            NpcAffinityLedger.Meet(_gameState, "boss"); // S-079 ④ — 소셜앱 등재
            WorldDialogueManager.Instance.PlayScenario(_cheerScenarios[Random.Range(0, _cheerScenarios.Length)]);
            _phase = Phase.Talking;
            SetAnimation("Talk");
        }

        private void FaceFront()
        {
            transform.rotation = _frontRotation;
        }

        private void SetAnimation(string stateName)
        {
            if (_animator == null || _animationState == stateName) return;
            _animationState = stateName;
            _animator.CrossFade(stateName, 0.12f);
        }

        public void SetHighlight(bool on)
        {
            if (TryGetComponent(out NpcNameLabel nameLabel)) nameLabel.Show(on); // S-120 — 근접 이름표
            if (_highlightRenderer == null) return;
            _highlightRenderer.sharedMaterial = on && _highlightMaterial != null ? _highlightMaterial : _normalMaterial;
        }

        // ── S-096 잔소리 말풍선 — PedestrianNpc Greet 캔버스와 동일 스타일 (풀해상 오버레이 추종) ──

        private void OnPackageDamaged()
        {
            int index = Random.Range(0, SCOLD_LINES.Length);
            if (index == _lastScoldIndex) index = (index + 1) % SCOLD_LINES.Length; // 연속 중복 회피
            _lastScoldIndex = index;
            ShowScold(SCOLD_LINES[index]);
        }

        private void ShowScold(string message)
        {
            // S-102 — 구 코루틴을 먼저 정지: 살려두면 그 종료 정리가 공유 필드를 타고
            // 새 말풍선까지 파괴한다 (던진 상자 바운스 연타 → 0.1초 소멸 실사고).
            if (_scoldRoutine != null) { StopCoroutine(_scoldRoutine); _scoldRoutine = null; }
            if (_scoldCanvasGo != null) Destroy(_scoldCanvasGo); // 연타 시 최신 멘트로 교체
            _scoldCanvasGo = new GameObject("ScoldCanvas");
            Canvas canvas = _scoldCanvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6;
            var label = new GameObject("Scold", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
            label.transform.SetParent(_scoldCanvasGo.transform, false);
            if (UiOverlayFont.Korean != null) label.font = UiOverlayFont.Korean;
            label.fontSize = 22f;
            label.fontStyle = TMPro.FontStyles.Bold;
            label.color = new Color(1f, 0.76f, 0.42f, 1f); // 잔소리 = 앰버 (인사 흰색과 톤만 구분)
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            label.rectTransform.sizeDelta = new Vector2(420f, 30f);
            label.text = message;
            _scoldRoutine = StartCoroutine(ScoldFollow(label, _scoldCanvasGo));
        }

        private System.Collections.IEnumerator ScoldFollow(TMPro.TMP_Text label, GameObject canvasGo)
        {
            float t = 0f;
            Camera camera = Camera.main;
            while (t < 2.2f && camera != null && label != null)
            {
                t += Mathf.Min(Time.deltaTime, 0.05f); // 프레임 스톨(알탭·에디터 왕복) dt 폭증 방어 — 콘페티(S-094)와 동일 처방
                Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 2.2f);
                if (screen.z > 0f) label.rectTransform.position = new Vector3(screen.x, screen.y, 0f);
                label.color = new Color(1f, 0.76f, 0.42f, t < 1.7f ? 1f : 1f - (t - 1.7f) / 0.5f);
                yield return null;
            }
            // S-102 — 자기 캔버스만 정리 (공유 필드 경유 파괴 금지).
            if (canvasGo != null) Destroy(canvasGo);
            if (_scoldCanvasGo == canvasGo) _scoldCanvasGo = null;
            _scoldRoutine = null;
        }
    }
}
