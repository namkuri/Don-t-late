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
                if (WorldDialogueManager.Instance != null && _tutorialScenario != null)
                    WorldDialogueManager.Instance.PlayScenario(_tutorialScenario);
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
            WorldDialogueManager.Instance.PlayScenario(_cheerScenarios[Random.Range(0, _cheerScenarios.Length)]);
        }

        public void SetHighlight(bool on)
        {
            if (_highlightRenderer == null) return;
            _highlightRenderer.sharedMaterial = on && _highlightMaterial != null ? _highlightMaterial : _normalMaterial;
        }
    }
}
