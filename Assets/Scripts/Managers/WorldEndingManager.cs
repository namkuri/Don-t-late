using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 엔딩 시퀀스 (S-104 — 남규님 기획 6단, 매니페스트 직교 추가).
    /// 빚 0 + Home 도착 → 독백 1회 → Camp 도착 시: 박말순+동행(호감도 장부 상위)이 걸어와
    /// 한마디씩 감사 인사 → 플레이어 작별·우측 퇴장 → 카메라 상승 + 크레딧(늦지마→잊지마)
    /// → 타이틀 복귀. 트리거 상태는 GameState 영속(endingMonologuePlayed·endingPlayed).
    /// </summary>
    public class WorldEndingManager : MonoBehaviour
    {
        public static WorldEndingManager Instance { get; private set; }

        [SerializeField] private GameStateSO _gameState;
        [Tooltip("동행 후보 NPC 도감 — 빌더 주입 (Data/Npcs 전량). 호감도 장부와 npcId로 대조.")]
        [SerializeField] private NpcSO[] _npcs;
        [SerializeField] private EndingCreditsView _creditsView;

        private const int FOLLOWER_MAX = 5;
        private const float WALK_SPEED = 2.4f;

        private bool _sequenceRunning;
        private readonly Collider[] _hits = new Collider[16];

        // 동행 감사 멘트 풀 — npcId 해시로 결정적 선택 (중복 회피).
        private static readonly string[] THANKS_LINES =
        {
            "덕분에 골목이 살았어요. 고마웠어요, 늦지마맨!",
            "비 오는 날에도 와줬잖아요. 그거, 못 잊어요.",
            "우리 집 앞에서 넘어질 뻔했던 거 기억나요? 그래도 늦지 않았죠.",
            "당신이 나른 건 상자가 아니라 안부였어요.",
            "다음 동네에서도 잘 부탁해요. 몸 조심하고!",
        };

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; // Core 씬 상주 — DontDestroyOnLoad 쓰지 않는다
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable() => WorldEvents.SceneTransitionCompleted += OnSceneArrived;
        private void OnDisable() => WorldEvents.SceneTransitionCompleted -= OnSceneArrived;

        private void OnSceneArrived(GameScene scene)
        {
            if (_gameState == null || _gameState.endingPlayed) return;
            // S-105 — 배송 이력 조건(completedCount) 제거: 치트 정산 경로(배송 0건)를 막던 과잉 방어.
            // 빚은 세션 시작 시 startDebt(양수)로 리셋되므로 신규 세션 오발동은 구조적으로 불가.
            bool debtCleared = _gameState.debt <= 0;

            if (scene == GameScene.Home && debtCleared && !_gameState.endingMonologuePlayed)
                StartCoroutine(PlayMonologue());
            else if (scene == GameScene.Camp && debtCleared && _gameState.endingMonologuePlayed && !_sequenceRunning)
                StartCoroutine(PlayEndingSequence());
        }

        // ── 1단 — Home 독백 ──────────────────────────────────
        private IEnumerator PlayMonologue()
        {
            _gameState.endingMonologuePlayed = true;
            // Home 도착 연출(전화 등)이 돌고 있으면 끝날 때까지 양보.
            yield return WaitClamped(1.2f);
            while (WorldDialogueManager.Instance != null && WorldDialogueManager.Instance.IsPlaying)
                yield return null;
            WorldDialogueManager.Instance?.PlayScenario(MakeScenario("Ending_Monologue",
                ("주인공", "(…빚, 다 갚았다.)"),
                ("주인공", "빚 다 갚았으니까 박말순씨한테 가서 인사해야겠다.")));
        }

        // ── 2~5단 — Camp 엔딩 시퀀스 ─────────────────────────
        private IEnumerator PlayEndingSequence()
        {
            _sequenceRunning = true;
            Debug.Log("[엔딩] 시퀀스 시작 t=" + Time.time.ToString("0.0"));
            WorldEvents.RaiseEndingStarted(); // S-107 ① — 엔딩 BGM 전환 (클립 도착 전엔 무해)

            // S-203 — 엔딩에 들어서는 순간 **대화창만 남기고 전부 끈다**(남규님 지시).
            // 종전엔 크레딧 직전에야 껐다 — 그전까지 HUD·폰·상단바·'정산하기' 버튼이 그대로 떠서
            // 작별 장면 위에 얹혀 있었다. 남기는 둘:
            //   DialogueCanvas — 작별 대사가 여기서 재생된다(끄면 엔딩이 안 보인다)
            //   FadeCanvas     — 씬 전환용. 끄면 마지막 타이틀 복귀가 검은 화면 없이 툭 끊긴다.
            var hiddenCanvases = new List<Canvas>();
            foreach (Canvas canvas in FindObjectsByType<Canvas>())
            {
                if (canvas == null || !canvas.enabled) continue;
                if (canvas.name == "DialogueCanvas" || canvas.name == "FadeCanvas") continue;
                canvas.enabled = false;
                hiddenCanvases.Add(canvas);
            }
            Debug.Log("[엔딩] UI 소등 " + hiddenCanvases.Count + "개 (대화창·페이드 유지)");
            _keepUiHidden = true;
            StartCoroutine(KeepUiHidden(hiddenCanvases)); // 뒤늦게 켜지는 것까지 잡는다(아래 참조)

            // Home → Camp 도착 시 DistrictEdgeGate가 Start에서 한 프레임 뒤 플레이어를 게이트 앞으로
            // 옮긴다. 그 지연 스폰보다 먼저 엔딩 좌표를 적용하면 다시 덮이므로 두 프레임 양보한다.
            yield return null;
            yield return null;

            Transform player = FindPlayer();
            if (player == null) { _sequenceRunning = false; yield break; }

            // 엔딩 시작 실측 월드 좌표. 카메라는 기존 추적 로직에 맡긴다.
            CharacterController startController = player.GetComponent<CharacterController>();
            bool controllerWasEnabled = startController != null && startController.enabled;
            if (controllerWasEnabled) startController.enabled = false;
            player.position = new Vector3(-30.2522926f, 3.16810144e-07f, 0.957655907f);
            if (controllerWasEnabled) startController.enabled = true;

            yield return WaitClamped(1.2f); // 도착 한 박자

            // S-107 ③ 보강 — 씬의 배회 행인도 멈춰서 플레이어를 바라본다: "다같이 모여 격려"의 일부이자,
            // 어슬렁거리다 대열에 난입하는 것 방지 (캡처 게이트 적발 — 배회 개체가 대열 사이 끼어듦).
            foreach (PedestrianNpc walker in FindObjectsByType<PedestrianNpc>())
            {
                if (walker == null) continue;
                walker.enabled = false; // 배회 정지 (씬 수명 한정 — 엔딩 후 타이틀 전환으로 함께 소멸)
                Vector3 look = player.position - walker.transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f) walker.transform.rotation = Quaternion.LookRotation(look);
            }

            // 2단 — 박말순 선두 + 동행이 오른쪽에서 걸어온다.
            List<NpcSO> party = PickParty();
            List<Color> colors = DistinctColors(party); // 동색 워커 구분 (캡처 게이트 적발 — 팔레트 재지정)
            var figures = new List<Transform>();
            for (int i = 0; i < party.Count; i++)
            {
                // 대열은 카메라 앞줄(z-)에 선다 — 캠프 소품(게시판·트럭, 깊은 쪽)과의 z-겹침 방지 (캡처 게이트 적발)
                Vector3 spawn = player.position + new Vector3(10f + i * 1.4f, 0f, -0.4f - (i % 2) * 0.7f);
                figures.Add(MakeFigure(party[i], colors[i], spawn));
            }
            for (int i = 0; i < figures.Count; i++)
            {
                Vector3 goal = player.position + new Vector3(2.2f + i * 1.1f, 0f, -0.4f - (i % 2) * 0.7f);
                StartCoroutine(WalkTo(figures[i], goal));
            }
            yield return WaitClamped(3.2f); // 걸어오는 시간

            // 한마디씩 — 박말순(변화 서사) → 동행 각 1줄 → 주인공 작별.
            var lines = new List<(string, string)>
            {
                ("박말순", "…왔구먼. 늦지도 않았네, 이번엔."),
                ("박말순", "빚 다 갚았다며. 그동안 내가 좀 심하게 굴었지. …고생했어, 정말."),
            };
            for (int i = 1; i < party.Count; i++)
                lines.Add((party[i].displayName, ThanksLine(party[i].npcId)));
            lines.Add(("박말순", "어딜 가든 밥은 챙겨 먹고. …잊지 마, 여기 사람들."));
            lines.Add(("주인공", "고마웠습니다, 다들. …늦지 않게, 또 올게요."));
            WorldDialogueManager.Instance?.PlayScenario(MakeScenario("Ending_Farewell", lines.ToArray()));
            while (WorldDialogueManager.Instance != null && WorldDialogueManager.Instance.IsPlaying)
                yield return null;
            Debug.Log("[엔딩] 작별 대화 종료 t=" + Time.time.ToString("0.0"));

            // 3단 — 늦지마맨 퇴장: 왼쪽으로 걸어가 사라진다 (조작 잠금).
            PlayerManager hub = player.GetComponent<PlayerManager>();
            if (hub != null)
            {
                if (hub.Input != null) hub.Input.enabled = false;
                if (hub.Locomotion != null) hub.Locomotion.enabled = false;
            }
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            yield return StartCoroutine(WalkTo(player, player.position + Vector3.left * 14f, faceLeft: true));
            player.gameObject.SetActive(false);
            _keepUiHidden = false; // S-204 — 감시 종료: 이 뒤로 크레딧 캔버스가 생긴다
            Debug.Log("[엔딩] 퇴장 완료 t=" + Time.time.ToString("0.0"));

            // 4단 — 카메라 상승(하늘) + 크레딧 (늦지마 → 잊지마).
            // S-203 — 소등은 시퀀스 시작에서 이미 했다. 여기선 **대화창까지** 마저 끈다:
            // 작별 대사는 끝났고 크레딧 화면에 대화 박스가 남아 있으면 안 된다.
            foreach (Canvas canvas in FindObjectsByType<Canvas>())
            {
                if (canvas == null || !canvas.enabled || canvas.name == "FadeCanvas") continue;
                canvas.enabled = false;
                hiddenCanvases.Add(canvas);
            }

            Camera camera = Camera.main;
            if (camera != null)
            {
                CameraFollowX follow = camera.GetComponent<CameraFollowX>();
                if (follow != null) follow.enabled = false;
                StartCoroutine(RaiseCamera(camera.transform, 13f, 11f));
            }
            Debug.Log("[엔딩] 크레딧 시작 t=" + Time.time.ToString("0.0") + " view=" + (_creditsView != null));
            if (_creditsView != null) yield return _creditsView.Play();
            else yield return WaitClamped(11f);
            Debug.Log("[엔딩] 크레딧 종료 t=" + Time.time.ToString("0.0"));

            // 5단 — 타이틀 복귀. Core 상주 캔버스(HUD 등)는 되살린다 — 전환 페이드가 깜빡임을 덮는다.
            foreach (Canvas canvas in hiddenCanvases)
                if (canvas != null) canvas.enabled = true;
            _gameState.endingPlayed = true;
            _sequenceRunning = false;
            WorldSceneFlowManager.Instance?.Request(GameScene.Main);
        }

        // ── 부품 ─────────────────────────────────────────────

        /// <summary>
        /// Find 금지 규칙 — 물리 쿼리로 찾는다 (CampBossNpc 관례).
        ///
        /// S-202 — **고정 버퍼(NonAlloc 16칸)를 쓰면 안 된다.** `OverlapSphereNonAlloc`은 가까운
        /// 순서가 아니라 **임의 순서**로 채우고 버퍼가 차면 나머지를 버린다. 캠프 반경 60u에는
        /// 콜라이더가 22개 있고 플레이어가 그중 22번째로 들어와(실측) 16칸 밖으로 밀렸다 —
        /// 그래서 엔딩 시퀀스가 시작하자마자 `player == null`로 조용히 빠져나갔다.
        /// 무대에 콜라이더가 하나 늘 때마다 다시 터질 수 있는 구조라 **상한 없는 쪽**으로 바꾼다.
        /// 엔딩 진입에 한 번 도는 코드라 할당 비용은 문제되지 않는다.
        /// </summary>
        private Transform FindPlayer()
        {
            foreach (Collider hit in Physics.OverlapSphere(Vector3.zero, 60f))
            {
                PlayerManager player = hit.GetComponentInParent<PlayerManager>();
                if (player != null) return player.transform;
            }
            return null;
        }

        private List<NpcSO> PickParty()
        {
            var party = new List<NpcSO>();
            NpcSO malsoon = FindNpc("parkmalsoon");
            if (malsoon != null) party.Add(malsoon); // 선두는 언제나 박말순

            var ranked = new List<NpcAffinity>(_gameState.npcAffinities);
            ranked.Sort((a, b) => b.affinity.CompareTo(a.affinity));
            foreach (NpcAffinity entry in ranked)
            {
                if (party.Count >= 1 + FOLLOWER_MAX) break;
                if (entry.npcId == "parkmalsoon") continue;
                NpcSO npc = FindNpc(entry.npcId);
                if (npc != null && !party.Contains(npc)) party.Add(npc);
            }
            // S-107 ③ — 부족분은 도감에서 충원: 호감도가 없어도 이웃들이 다같이 모여
            // 격려하는 것이 이 엔딩의 포인트 (호감도 인원은 앞줄 우선일 뿐).
            foreach (NpcSO npc in _npcs)
            {
                if (party.Count >= 1 + FOLLOWER_MAX) break;
                if (npc == null || npc.npcId == "parkmalsoon" || party.Contains(npc)) continue;
                party.Add(npc);
            }
            return party;
        }

        private NpcSO FindNpc(string npcId)
        {
            if (_npcs == null) return null;
            foreach (NpcSO npc in _npcs)
                if (npc != null && npc.npcId == npcId) return npc;
            return null;
        }

        private static string ThanksLine(string npcId)
            => THANKS_LINES[Mathf.Abs(npcId.GetHashCode()) % THANKS_LINES.Length];

        /// <summary>도감 색 기반 + 유사색 중복 시 색상환 분산 — 6명이 육안 구분되게 (S-107 게이트 적발).</summary>
        private static List<Color> DistinctColors(List<NpcSO> party)
        {
            var colors = new List<Color>();
            for (int i = 0; i < party.Count; i++)
            {
                Color color = party[i].placeholderColor;
                bool similar = false;
                foreach (Color used in colors)
                    if (Mathf.Abs(color.r - used.r) + Mathf.Abs(color.g - used.g) + Mathf.Abs(color.b - used.b) < 0.35f)
                        similar = true;
                if (similar) color = Color.HSVToRGB((i * 0.17f) % 1f, 0.5f, 0.8f);
                colors.Add(color);
            }
            return colors;
        }

        // 런타임 감사 인사 피겨 — 캡슐+머리 (그레이박스 NPC 동형, 세션 한정이라 SO 에셋 불요).
        private static Transform MakeFigure(NpcSO npc, Color bodyColor, Vector3 position)
        {
            GameObject root = new GameObject("EndingNpc_" + npc.npcId);
            root.transform.position = position;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);
            body.GetComponent<Renderer>().material.color = bodyColor;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.95f, 0f);
            head.transform.localScale = Vector3.one * 0.42f;
            head.GetComponent<Renderer>().material.color = new Color(0.93f, 0.82f, 0.70f);
            return root.transform;
        }

        /// <summary>
        /// S-203 — 한 번 끄는 것으로는 부족하다. 이름표(`NameCanvas`)처럼 **엔딩 중에 생기거나
        /// 스스로 다시 켜지는 UI**가 있어 시작 시점의 일괄 소등을 빠져나간다(실측: 소등 13개 뒤에도
        /// NameCanvas가 남았다 — 엔딩 NPC가 스폰되며 이름표를 켠다).
        /// 그래서 퇴장이 끝날 때까지 **지켜보며 계속 끈다.** 대화창·페이드는 예외.
        /// </summary>
        private IEnumerator KeepUiHidden(List<Canvas> hidden)
        {
            // S-204 — 감시는 **퇴장까지만**. 그 뒤엔 크레딧이 자기 캔버스(`EndingCreditsCanvas`)를
            // 새로 만드는데, 계속 훑으면 그것까지 매 프레임 꺼 버려 **크레딧과 로고 전환이 아예
            // 안 보인다**(S-203에서 내가 넣은 감시가 만든 회귀 — 남규님 보고).
            while (_keepUiHidden)
            {
                foreach (Canvas canvas in FindObjectsByType<Canvas>())
                {
                    if (canvas == null || !canvas.enabled) continue;
                    if (canvas.name == "DialogueCanvas" || canvas.name == "FadeCanvas") continue;
                    if (canvas.name == "EndingCreditsCanvas") continue; // 안전망 — 순서가 어긋나도 크레딧은 산다
                    canvas.enabled = false;
                    if (!hidden.Contains(canvas)) hidden.Add(canvas);
                }
                yield return null;
            }
        }

        private bool _keepUiHidden;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");

        /// <summary>
        /// 대상을 목표 지점까지 걸린다. **애니메이터에 속도를 직접 먹인다** —
        /// S-203: 종전엔 트랜스폼만 옮겨서, 이동은 하는데 걷기 모션이 안 나오고 Idle인 채로
        /// 미끄러졌다(남규님 보고). 평소엔 `PlayerAnimationManager`가 `Locomotion.PlanarVelocity`를
        /// 읽어 Speed를 세우는데, 퇴장 직전에 조작 잠금으로 **Locomotion을 꺼 버려** 그 공급이
        /// 끊긴 것이 원인이다. 연출이 직접 움직이는 구간에선 연출이 신호도 책임진다.
        /// </summary>
        private static IEnumerator WalkTo(Transform mover, Vector3 goal, bool faceLeft = false)
        {
            if (faceLeft) mover.rotation = Quaternion.LookRotation(Vector3.left);

            Animator animator = mover != null ? mover.GetComponentInChildren<Animator>() : null;
            if (animator != null) animator.SetBool(GroundedHash, true);

            while (mover != null && Vector3.Distance(mover.position, goal) > 0.05f)
            {
                // 속도를 **매 프레임** 다시 세운다. `PlayerAnimationManager.Update()`가 같은 프레임에
                // `Locomotion.PlanarVelocity`(퇴장 중엔 0)로 Speed를 덮어쓰기 때문이다 — 한 번만
                // 세우면 다음 프레임에 지워져 Idle로 미끄러진다(실측). 코루틴은 Update 뒤에 돌아
                // 이 대입이 마지막에 남는다.
                if (animator != null) animator.SetFloat(SpeedHash, WALK_SPEED); // 블렌드 임계 2.5=걷기

                // dt 클램프 — 프레임 스톨(알탭·에디터 왕복) 순간이동 방지 (S-104 실측 교훈)
                mover.position = Vector3.MoveTowards(mover.position, goal, WALK_SPEED * Mathf.Min(Time.deltaTime, 0.05f));
                yield return null;
            }

            if (animator != null) animator.SetFloat(SpeedHash, 0f); // 도착하면 Idle로
        }

        /// <summary>클램프 누적 대기 — WaitForSeconds는 스톨 dt를 통째로 삼켜 연출 단계를 건너뛴다 (S-104).</summary>
        internal static IEnumerator WaitClamped(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Mathf.Min(Time.deltaTime, 0.05f);
                yield return null;
            }
        }

        private static IEnumerator RaiseCamera(Transform cameraTransform, float height, float seconds)
        {
            Vector3 from = cameraTransform.position;
            float t = 0f;
            while (t < 1f && cameraTransform != null)
            {
                t += Mathf.Min(Time.deltaTime, 0.05f) / seconds;
                float eased = t * t * (3f - 2f * t); // smoothstep — 서서히
                cameraTransform.position = from + Vector3.up * (height * eased);
                yield return null;
            }
        }

        private static DialogueScenarioSO MakeScenario(string name, params (string speaker, string text)[] lines)
        {
            DialogueScenarioSO scenario = ScriptableObject.CreateInstance<DialogueScenarioSO>();
            scenario.name = name; // 런타임 인스턴스 — 에셋 저장 없음 (심부름 런타임 주문 관례)
            scenario.lines = new DialogueScenarioSO.Line[lines.Length];
            for (int i = 0; i < lines.Length; i++)
                scenario.lines[i] = new DialogueScenarioSO.Line { speaker = lines[i].speaker, text = lines[i].text };
            return scenario;
        }
    }
}
