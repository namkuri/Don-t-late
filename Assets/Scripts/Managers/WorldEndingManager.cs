using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations; // S-228 — 엔딩 대열 걷기 클립 재생
using UnityEngine.Playables;

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

        /// <summary>
        /// S-228 — 엔딩 대열 1인. **모델이 있는 인물만** 여기 오른다(빌더가 채운다).
        /// 종전 명부는 호감도 장부·도감에서 뽑았는데, 도감(8종)에 오지혜·나아라가 없어
        /// 실모델 인물이 다 나오지 못했다 — 명부의 기준을 "모델 보유"로 바꾼다.
        /// </summary>
        [System.Serializable]
        public struct EndingCastEntry
        {
            public string npcId;
            public string displayName;
            public GameObject model;
            [Tooltip("걸어오는 동안 재생할 클립. 비면 정지 자세로 미끄러진다.")]
            public AnimationClip walkClip;
            public Avatar avatar;
            [Tooltip("S-228 — FBX 임베디드 머티리얼이 텍스처를 못 찾는 모델용. 비면 원본 그대로.")]
            public Material skin;
        }

        [Tooltip("S-228 — 엔딩 대열(모델 보유 인물 전원, 1인 1종). 빌더 주입.")]
        [SerializeField] private EndingCastEntry[] _cast;

        private const int FOLLOWER_MAX = 5;
        private const float WALK_SPEED = 2.4f;

        // S-230 ① — 엔딩에서 늦지마맨이 서는 자리(남규님 지정).
        private static readonly Vector3 ENDING_PLAYER_START = new Vector3(-30.2522926f, 3.16810144e-07f, 0.957655907f);

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
            Transform player = FindPlayer();
            if (player == null) { _sequenceRunning = false; yield break; }

            // S-229 ③ — **조작을 여기서 잠근다.** 종전엔 3단(퇴장)에서야 잠갔는데, 그전까지
            // 대열이 걸어오고 대사가 도는 동안 입력이 살아 있었다 — 플레이어가 엣지워크로
            // 걸어 나가 Home으로 돌아가면 엔딩이 통째로 깨진다(남규님 실관찰).
            // 이동만 막는다: 대화 진행 입력은 DialogueView가 따로 받는다.
            PlayerManager hub = player.GetComponent<PlayerManager>();
            if (hub != null)
            {
                if (hub.Input != null) hub.Input.enabled = false;
                if (hub.Locomotion != null) hub.Locomotion.enabled = false;
            }

            // S-230 ① — 늦지마맨을 정해진 자리에 세운다(남규님 좌표). 대열이 오른쪽에서 걸어오므로
            // 시작 위치가 어긋나면 대열 간격·카메라 잡이가 통째로 밀린다.
            player.position = ENDING_PLAYER_START;

            // S-230 ②④⑤ — 엔딩 무대 정리: 캠프의 상시 소품이 마지막 장면에 끼어든다.
            //   `__gb_BossNpc`  — 김사장이 대열에도 서므로 **두 명**이 된다(남규님 관찰)
            //   `PickupBox`     — 박말순이 서는 자리와 겹친다
            // 씬 수명 한정으로 끄기만 한다 — 엔딩 뒤엔 타이틀로 가므로 되돌릴 일이 없다.
            int propsOff = 0;
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root == null || !root.activeSelf) continue;
                if (root.name != "__gb_BossNpc" && root.GetComponentInChildren<PickupBox>(true) == null) continue;
                root.SetActive(false);
                propsOff++;
            }
            if (propsOff > 0) Debug.Log("[엔딩] 무대 정리 " + propsOff + "개 소등 — 사장님 중복·상자 겹침 (S-230 ②④).");

            // S-229 ② — 엣지워크 화살표를 끈다. 조작을 막아도 "나갈 수 있다"는 신호가 남아 있으면
            // 마지막 장면에 안 어울린다. 게이트 자체를 꺼서 판정도 같이 죽인다.
            int gatesOff = 0;
            foreach (DistrictEdgeGate gate in FindObjectsByType<DistrictEdgeGate>(FindObjectsInactive.Exclude))
            {
                if (gate == null) continue;
                gate.gameObject.SetActive(false);
                gatesOff++;
            }
            if (gatesOff > 0) Debug.Log("[엔딩] 엣지워크 게이트 " + gatesOff + "개 소등 (S-229 ②).");

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
            List<EndingCastEntry> party = PickParty();
            var figures = new List<Transform>();
            for (int i = 0; i < party.Count; i++)
            {
                // 대열은 카메라 앞줄(z-)에 선다 — 캠프 소품(게시판·트럭, 깊은 쪽)과의 z-겹침 방지 (캡처 게이트 적발)
                Vector3 spawn = player.position + new Vector3(10f + i * 1.4f, 0f, -0.4f - (i % 2) * 0.7f);
                figures.Add(MakeFigure(party[i], spawn));
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

            // 3단 — 늦지마맨 퇴장: 왼쪽으로 걸어가 사라진다 (조작은 S-229 ③에서 이미 잠갔다).
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

        /// <summary>
        /// S-228 — **모델 보유 인물 전원을 1인 1종으로** 세운다(남규님 지시 "1마리씩 다 나오게").
        ///
        /// 종전엔 호감도 장부 상위 + 도감 충원으로 뽑았는데, 도감(`Data/Npcs` 8종)에 오지혜·나아라가
        /// 없어 실모델 인물이 대열에 못 들어왔다. 이제 기준은 **모델을 가졌는가** 하나다 —
        /// 호감도는 대열 순서(선두 다음 자리)에만 쓴다. 박말순은 서사상 언제나 선두.
        /// 같은 모델이 두 번 서지 않게 모델 기준으로도 한 번 더 거른다.
        /// </summary>
        private List<EndingCastEntry> PickParty()
        {
            var party = new List<EndingCastEntry>();
            if (_cast == null || _cast.Length == 0) return party;

            var usedModels = new List<GameObject>();
            void TryAdd(EndingCastEntry entry)
            {
                if (entry.model == null || usedModels.Contains(entry.model)) return;
                usedModels.Add(entry.model);
                party.Add(entry);
            }

            foreach (EndingCastEntry entry in _cast)
                if (entry.npcId == "parkmalsoon") TryAdd(entry); // 선두 고정

            // 호감도 높은 순으로 그 다음 자리를 채운다(있는 만큼만 — 없어도 전원 등장은 아래에서 보장).
            var ranked = new List<NpcAffinity>(_gameState != null ? _gameState.npcAffinities : new List<NpcAffinity>());
            ranked.Sort((a, b) => b.affinity.CompareTo(a.affinity));
            foreach (NpcAffinity affinity in ranked)
                foreach (EndingCastEntry entry in _cast)
                    if (entry.npcId == affinity.npcId) TryAdd(entry);

            foreach (EndingCastEntry entry in _cast) TryAdd(entry); // 나머지 전원
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
        /// <summary>
        /// S-228 — 실모델로 세운다. 모델이 없으면 종전 캡슐로 떨어진다(빌더 주입이 비어도 엔딩은 돈다).
        /// 전고는 1.7u로 정규화한다 — 모델마다 원 크기가 제각각이라 그대로 두면 키가 들쭉날쭉해진다.
        /// 걷기 클립이 있으면 재생한다: 없으면 정지 자세로 미끄러져 온다(종전 증상).
        /// </summary>
        private static Transform MakeFigure(EndingCastEntry entry, Vector3 position)
        {
            GameObject root = new GameObject("EndingNpc_" + entry.npcId);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, -90f, 0f); // 플레이어(왼쪽)를 보고 걸어온다

            if (entry.model == null) { MakeGreyboxFigure(root); return root.transform; }

            GameObject visual = Instantiate(entry.model, root.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) { Destroy(visual); MakeGreyboxFigure(root); return root.transform; }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            if (bounds.size.y > 0.001f) visual.transform.localScale = Vector3.one * (1.7f / bounds.size.y);

            // 발끝을 지면에 맞춘다 — 스케일을 바꾼 뒤 다시 재야 한다.
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            visual.transform.position += Vector3.up * (root.transform.position.y - bounds.min.y);

            // S-228 — 동반 머티리얼. FBX 임베디드는 텍스처를 못 찾아 새하얗게 선다
            // (S-215 박말순·나아라, S-221 행인에서 겪은 것과 같은 함정 — 엔딩에서 또 밟았다).
            if (entry.skin != null)
                foreach (Renderer renderer in renderers) renderer.sharedMaterial = entry.skin;

            PlayWalkClip(visual, entry);
            return root.transform;
        }

        /// <summary>모델이 없을 때의 대체 — 캡슐+머리(종전 그레이박스).</summary>
        private static void MakeGreyboxFigure(GameObject root)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.95f, 0f);
            head.transform.localScale = Vector3.one * 0.42f;
            head.GetComponent<Renderer>().material.color = new Color(0.93f, 0.82f, 0.70f);
        }

        /// <summary>
        /// S-228 — 걷기 클립을 물린다. `AlternatingNpcAnimation`을 쓰지 않는 이유:
        /// 그 컴포넌트는 LateUpdate에서 루트 위치를 되돌려 놓는데(제자리 연출용),
        /// 여기선 코루틴이 루트를 움직이므로 서로 싸운다. 그래서 최소 그래프만 직접 돌린다.
        /// </summary>
        private static void PlayWalkClip(GameObject visual, EndingCastEntry entry)
        {
            if (entry.walkClip == null) return;

            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator == null) animator = visual.AddComponent<Animator>();
            if (entry.avatar != null) animator.avatar = entry.avatar;
            if (entry.walkClip.isHumanMotion && animator.avatar == null) return; // 아바타 없이 휴머노이드 클립은 안 돈다
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = null;

            var graph = PlayableGraph.Create(visual.name + "_EndingWalk");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var clip = AnimationClipPlayable.Create(graph, entry.walkClip);
            clip.SetApplyFootIK(true);
            var output = AnimationPlayableOutput.Create(graph, "Walk", animator);
            output.SetSourcePlayable(clip);
            graph.Play();

            // S-230 ③ — 도착하면 멈출 수 있게 그래프를 들고 있는다. 종전엔 `WalkTo`가 Animator
            // 파라미터(`SetFloat`)로 멈추려 했는데, 이 대열은 컨트롤러 없이 그래프로 도니
            // 그 대입이 **아무 데도 닿지 않아** 제자리걸음이 됐다(남규님 관찰: 나아라).
            EndingClipPlayer player = visual.AddComponent<EndingClipPlayer>();
            player.Bind(graph, clip);
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

            // S-230 ③ — 그래프로 도는 대열은 위 대입이 안 먹는다. 클립을 직접 세운다.
            EndingClipPlayer clipPlayer = mover != null ? mover.GetComponentInChildren<EndingClipPlayer>(true) : null;
            if (clipPlayer != null) clipPlayer.Freeze();
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

        /// <summary>
        /// S-230 ③ — 엔딩 대열의 걷기 클립을 들고 있다가 도착하면 세운다.
        /// 이 대열은 Animator 컨트롤러 없이 PlayableGraph로 돌기 때문에 파라미터로는 못 멈춘다.
        /// 그래프 정리(OnDestroy)까지 여기서 책임진다 — 안 지우면 씬 전환 때 경고가 남는다.
        /// </summary>
        private sealed class EndingClipPlayer : MonoBehaviour
        {
            private PlayableGraph _graph;
            private AnimationClipPlayable _clip;
            private bool _bound;

            public void Bind(PlayableGraph graph, AnimationClipPlayable clip)
            {
                _graph = graph;
                _clip = clip;
                _bound = true;
            }

            /// <summary>걸음을 멈춘다 — 마지막 프레임 자세로 선다(다리를 벌린 채 굳지 않게 0초로 되감는다).</summary>
            public void Freeze()
            {
                if (!_bound || !_graph.IsValid()) return;
                _clip.SetTime(0d);
                _clip.SetSpeed(0d);
            }

            private void OnDestroy()
            {
                if (_bound && _graph.IsValid()) _graph.Destroy();
            }
        }
    }
}
