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
        [Tooltip("S-146 — 7단계 튜토리얼 진행부. 붙어 있으면 이쪽이 우선한다.")]
        [SerializeField] private CampTutorialDirector _tutorial;
        [SerializeField] private DialogueScenarioSO[] _cheerScenarios;
        [Tooltip("재방문 때 자리를 비울 확률 (간혹 안 나온다).")]
        [SerializeField] private float _absentChance = 0.25f;
        [SerializeField] private Renderer _highlightRenderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;

        private enum Phase { Waiting, Approaching, Talking, Returning, Idle }
        private Phase _phase = Phase.Idle;
        private Vector3 _homePosition;
        private Transform _player;
        private float _pollTimer;
        private readonly Collider[] _hits = new Collider[16];

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

        private void OnEnable()  { WorldEvents.PackageDamaged += OnPackageDamaged; }
        private void OnDisable() { WorldEvents.PackageDamaged -= OnPackageDamaged; }

        private void OnDestroy()
        {
            if (_scoldCanvasGo != null) Destroy(_scoldCanvasGo);
        }

        private void Start()
        {
            _homePosition = transform.position;

            if (_gameState != null && _gameState.bossIntroPlayed)
            {
                if (Random.value < _absentChance)
                {
                    gameObject.SetActive(false); // 오늘은 자리 비움
                    return;
                }
                _phase = Phase.Idle;
            }
            else
            {
                _phase = Phase.Waiting; // 첫 방문 — 플레이어를 기다렸다 다가간다
            }
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
                return;
            }
        }

        private void Approach()
        {
            if (_player == null) { _phase = Phase.Idle; return; }
            Vector3 target = _player.position;
            target.y = transform.position.y;
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
                Debug.Log("[사장님] 첫 방문 튜토리얼 시작.");
                return;
            }
            transform.position = Vector3.MoveTowards(transform.position, target, APPROACH_SPEED * Time.deltaTime);
        }

        private void WaitTalkEnd()
        {
            if (WorldDialogueManager.Instance != null && WorldDialogueManager.Instance.IsPlaying) return;
            // S-146 — 튜토리얼은 대사 사이에 **행동 대기 구간**이 있어 그때마다 대화가 멈춘다.
            // 대화 중단만 보고 복귀하면 1단계 만에 사장님이 돌아가 버린다 — 진행부가 끝나야 간다.
            if (_tutorial != null && _tutorial.Running) return;
            _phase = Phase.Returning;
        }

        private void ReturnHome()
        {
            FaceTowards(_homePosition);
            transform.position = Vector3.MoveTowards(transform.position, _homePosition, APPROACH_SPEED * Time.deltaTime);
            if (Vector3.Distance(transform.position, _homePosition) < 0.05f) _phase = Phase.Idle;
        }

        private void FaceTowards(Vector3 target)
        {
            Vector3 dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(dir), 360f * Time.deltaTime);
        }

        public void Interact(PlayerContext ctx)
        {
            if (_phase != Phase.Idle) return;
            if (WorldDialogueManager.Instance == null || WorldDialogueManager.Instance.IsPlaying) return;
            if (_cheerScenarios == null || _cheerScenarios.Length == 0) return;
            FaceTowards(ctx.Transform.position);
            NpcAffinityLedger.Meet(_gameState, "boss"); // S-079 ④ — 소셜앱 등재
            WorldDialogueManager.Instance.PlayScenario(_cheerScenarios[Random.Range(0, _cheerScenarios.Length)]);
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
