using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// Camp.unity(물류캠프)에 짐싣기 그레이박스 무대를 조립하는 개발 도구 — S-008.
    /// 매니저는 Core 씬 상주이므로 만들지 않는다. GreyboxStageBuilder의 조립 헬퍼를 재사용해
    /// 지면·트럭 소품·적재존 패드 3개·박스 더미·플레이어·카메라만 깐다.
    /// LoadingZone.cs(S-005 납품 대기)가 도착하면 __gb_LoadZone_01~03에 부착한다.
    /// 다시 실행하면 이전 조립물(__gb_ 루트)을 지우고 새로 만든다(멱등).
    /// </summary>
    public static class CampStageBuilder
    {
        private const string CAMP_PATH = "Assets/Scenes/Camp.unity";
        private const string CAMP_PLANES_PREFAB_PATH = "Assets/Prefabs/Hand/set_camp_planes.prefab";
        private const string BLOSSOM_PREFAB_PATH = "Assets/Prefabs/Auto/blossom_tree.prefab";
        private const string BLOSSOM_TEXTURE_PATH = "Assets/_intake/Art/ChatGPT/UI/one_blossom.png";
        private const string BOSS_MODEL_PATH = "Assets/Art/Characters/Kimboss/kim_boss.fbx";
        private const string BOSS_IDLE_PATH = "Assets/Art/Characters/Kimboss/kimboss_Breathing Idle.fbx";
        private const string BOSS_WALK_PATH = "Assets/Art/Characters/Kimboss/kimboss_Walking (2).fbx";
        private const string BOSS_TALK_PATH = "Assets/Art/Characters/Kimboss/kim_bossTalking.fbx";
        private const string BOSS_CONTROLLER_PATH = "Assets/Art/Characters/Kimboss/AC_kim_boss.controller";
        private const float BOSS_VISUAL_YAW = 90f;
        private const int LOAD_ZONE_COUNT = 4; // S-039 ④ — 4번째 = 아파트행 물량

        [MenuItem("DontLate/Build/Camp Stage", priority = 12)]
        public static void BuildCampStage()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[Camp] 저장되지 않은 씬이 있어 재조립을 취소했다.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(CAMP_PATH, OpenSceneMode.Single);
            GreyboxStageBuilder.Clear();
            EnsureCampPlaneSet(scene);

            var (gameState, tuning, _) = GreyboxStageBuilder.GetOrCreateStageData();

            Material ground = GreyboxStageBuilder.GetOrCreateMaterial("Ground", new Color(0.24f, 0.24f, 0.26f), false);
            Material lane = GreyboxStageBuilder.GetOrCreateMaterial("Lane", new Color(0.34f, 0.33f, 0.30f), false);
            Material box = GreyboxStageBuilder.GetOrCreateMaterial("Box", GreyboxStageBuilder.ParseColor("#ff9f45"), false);
            Material truck = GreyboxStageBuilder.GetOrCreateMaterial("Truck", new Color(0.30f, 0.42f, 0.55f), false);

            Material highlight = GreyboxStageBuilder.GetOrCreateHighlightMaterial();
            Material drink = GreyboxStageBuilder.GetOrCreateMaterial("Drink", GreyboxStageBuilder.ParseColor("#e04a35"), false);

            GreyboxStageBuilder.BuildGround(ground, lane);
            GreyboxStageBuilder.BuildWalkableVolume();
            GreyboxStageBuilder.BuildGroundMist();
            GreyboxStageBuilder.BuildStarField(); // S-033 ① — 캠프 밤하늘 별 (밤 페이드는 StarField.cs 공용)
            GreyboxStageBuilder.BuildDeliveryCart(new Vector3(12.88f, 0f, 0f)); // S-039 ④ · S-160 남규님 실배치
            BuildTruck(truck, box, highlight, gameState);
            System.Collections.Generic.List<PickupBox> boxes = BuildPickupBoxes(box, highlight, tuning);
            BuildOrderBoard(gameState, boxes);
            // S-230 ⑤ — `__gb_Drink` 생성 중단(남규님 지시).
            //   ⚠ 캠프의 스태미나 회복 수단이 하나 줄었다 — 가방 드링크·먹자골목 매대는 그대로다.
            BuildVendingMachine(tuning, drink, highlight);
            BuildBossNpc(gameState, highlight);                                  // S-052 ① 사장님
            EdgeGateBuildKit.BuildGate("EdgeGate_Next", new Vector3(14f, 0f, 0f),
                DontLate.DistrictEdgeGate.Direction.Next, gameState);             // S-054b 도보 개척 출구
            EdgeGateBuildKit.BuildGate("EdgeGate_Home", new Vector3(-14f, 0f, 0f),
                DontLate.DistrictEdgeGate.Direction.Prev, gameState);             // S-062 ② 집 방향
            // S-115 — 실물 데코: 물류 배경 건물 + 야드 소품 (없으면 생략 — 소켓).
            GreyboxStageBuilder.PlaceCatalog("logi_center", new Vector3(0f, 0f, 16f)); // 원경 1채
            GreyboxStageBuilder.PlaceCatalog("belt", new Vector3(8.44f, 0f, 5.73f), 90f); // S-160 남규님 실배치
            // S-123 ① — 포장마차 독백. District 프랍 풀에 넣으면 결정론 배치 계약이 깨지므로
            // (풀 길이가 바뀌면 전 구역 배치가 달라진다) 캠프의 손배치 데코에 붙인다.
            GameObject foodCart = GreyboxStageBuilder.PlaceCatalog("Food_cart_unity", new Vector3(28.78f, 0f, 2.6f), 180f); // S-160 남규님 실배치
            if (foodCart != null)
            {
                DistrictSceneBuilder.AttachRemarkSpot(foodCart, 3f, new[]
                {
                    "맛있어 보인다...", "저거 한 그릇 하고 싶다.", "일 끝나고 오자. 지금은 참고.",
                });
                KioskBuildKit.MakeKiosk(foodCart, "포장마차", KioskBuildKit.StreetFoodItems); // S-125 ②
            }
            // S-116 ② — white_van 데코 철거: 실모델 트럭과 함께 서면 "트럭 2대"로 읽힌다 (남규님 실관찰).
            GreyboxStageBuilder.PlaceCatalog("Trash_Bin_unity", new Vector3(-1.72f, 0f, 2.4f)); // S-160 남규님 실배치

            NpcBuildKit.BuildPedestrian("Walker_A", new Vector3(-9f, 0f, 2.4f), new Color(0.45f, 0.52f, 0.62f), 5f,
                null, 0f, "camp_walker_a", gameState);
            NpcBuildKit.BuildPedestrian("Walker_B", new Vector3(4f, 0f, 2.8f), new Color(0.60f, 0.48f, 0.40f), 6f,
                null, 0f, "camp_walker_b", gameState); // S-052 ② 행인 · S-080 ① 인사
            GreyboxStageBuilder.BuildPlayer(gameState, tuning);
            GreyboxStageBuilder.BuildPostVolume();
            GreyboxStageBuilder.ConfigureCamera();
            GreyboxStageBuilder.AttachCameraFollow();

            // S-141 — 민지님 세트 프리팹(`set_camp_1`)으로 물류장 소품·건물을 깐다.
            // 프리팹이 정본이라 민지님이 고치면 코드 수정 없이 반영된다.
            ArtBackdropKit.Build(ArtBackdropKit.Camp);
            BuildCampBlossoms(scene);

            EditorSceneManager.SaveScene(scene, CAMP_PATH);
            Debug.Log("[Camp] 무대 조립 완료 — 박스 " + LOAD_ZONE_COUNT
                    + "개를 E로 들어 트럭 짐칸 뒤에서 E로 싣는다 (S-009).");
        }

        [MenuItem("DontLate/Art/Add Camp Blossom Trees + Petals", priority = 31)]
        private static void AddCampBlossomsOnly()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Camp Blossom Petals",
                    "Play Mode를 끈 뒤 다시 실행하세요.", "OK");
                return;
            }

            Scene scene = SceneManager.GetSceneByName("Camp");
            if (!scene.IsValid() || !scene.isLoaded)
            {
                EditorUtility.DisplayDialog("Camp Blossom Petals",
                    "Camp 씬을 연 뒤 다시 실행하세요. Core와 함께 열려 있어도 괜찮습니다.", "OK");
                return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name.StartsWith("__gb_CampBlossom_")
                    || root.name.StartsWith("__gb_BlossomPetalEffect_Camp_"))
                    Object.DestroyImmediate(root);
            }

            int count = BuildCampBlossoms(scene);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog("Camp Blossom Petals",
                "벚꽃나무와 꽃잎 효과 " + count + "개를 추가하고 Camp 씬을 저장했습니다.", "OK");
        }

        private static int BuildCampBlossoms(Scene scene)
        {
            GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BLOSSOM_PREFAB_PATH);
            Texture2D petalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BLOSSOM_TEXTURE_PATH);
            if (treePrefab == null || petalTexture == null)
            {
                Debug.LogWarning("[Camp] 벚꽃나무 또는 one_blossom 에셋을 찾지 못해 효과를 생략했다.");
                return 0;
            }

            var trees = new System.Collections.Generic.List<GameObject>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name.StartsWith("blossom_tree", System.StringComparison.OrdinalIgnoreCase))
                    trees.Add(root);
            }

            if (trees.Count == 0)
            {
                Vector3[] positions =
                {
                    new Vector3(-52.96024f, 1.802573e-07f, 5.701714f),
                    new Vector3(-43.71024f, 1.802573e-07f, 5.701714f),
                    new Vector3(-34.71024f, 1.802573e-07f, 5.701714f),
                };
                for (int i = 0; i < positions.Length; i++)
                {
                    GameObject tree = PrefabUtility.InstantiatePrefab(treePrefab, scene) as GameObject;
                    if (tree == null) continue;
                    tree.name = "__gb_CampBlossom_" + (i + 1).ToString("00");
                    tree.transform.position = positions[i];
                    tree.transform.localScale = Vector3.one * 9.9096f;
                    foreach (Collider collider in tree.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                    trees.Add(tree);
                }
            }

            int effectCount = 0;
            foreach (GameObject tree in trees)
            {
                Renderer[] renderers = tree.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                GameObject fx = new GameObject("__gb_BlossomPetalEffect_Camp_" + (++effectCount).ToString("00"));
                SceneManager.MoveGameObjectToScene(fx, scene);
                fx.transform.position = new Vector3(bounds.center.x, bounds.max.y + 0.2f, bounds.center.z);

                BlossomPetalEffect effect = fx.AddComponent<BlossomPetalEffect>();
                var serialized = new SerializedObject(effect);
                serialized.FindProperty("_petalTexture").objectReferenceValue = petalTexture;
                serialized.FindProperty("_emitBox").vector3Value = new Vector3(
                    Mathf.Max(2f, bounds.size.x), 0.6f, Mathf.Max(2f, bounds.size.z));
                serialized.FindProperty("_petalSizeMultiplier").floatValue = 2f;
                serialized.FindProperty("_rate").floatValue = 36f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log("[Camp] 벚꽃나무 꽃잎 효과 " + effectCount + "개 배치 — 크기·방출량 2배.");
            return effectCount;
        }

        private static void EnsureCampPlaneSet(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "set_camp_planes") return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CAMP_PLANES_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogWarning("[Camp] set_camp_planes 프리팹을 찾지 못해 수제 Plane 배치를 생략했다.");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance != null) instance.name = "set_camp_planes";
        }

        // 트럭 = 소품 + 적재존(S-009). 짐칸 뒤에서 박스를 든 채 E → LoadingZone이 짐칸에 쌓는다.
        private static void BuildTruck(Material material, Material boxMaterial, Material highlight, GameStateSO gameState)
        {
            GameObject root = GreyboxStageBuilder.CreateEmpty("Truck", new Vector3(9f, 0f, 1.8f));

            // S-116 ② — 실모델 트럭(truck.prefab)이 있으면 통짜 비주얼, 없으면 그레이박스 폴백(소켓).
            // 기능(적재 트리거·DepartPoint·StackRoot)은 루트 오프셋 기준이라 양쪽 동일.
            Renderer bodyRenderer = null;
            Material bodyNormal = material;
            GameObject truckPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Auto/truck.prefab");
            if (truckPrefab != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(truckPrefab);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                NormalizeTruckVisual(visual, root.transform.position);
                // 기존 그레이박스 파츠도 콜라이더가 없었다 — 등가 유지(적재 트리거 접근 방해 금지).
                foreach (Collider partCollider in visual.GetComponentsInChildren<Collider>(true))
                    partCollider.enabled = false;
                bodyRenderer = visual.GetComponentInChildren<Renderer>();
                if (bodyRenderer != null) bodyNormal = bodyRenderer.sharedMaterial;
                // S-166 ③ — 트럭 몸통은 통과 금지(남규님: 플레이어와 겹침). 짐칸 뒤 적재 트리거는
                // 차체 바깥(로컬 z −1.8)이라 이 블로커에 가리지 않는다.
                GreyboxStageBuilder.AddSolidBlocker(visual, 0.2f);
            }
            else
            {
                GameObject cargo = AddPart(root, "Cargo", new Vector3(-0.8f, 1.5f, 0f), new Vector3(4.0f, 2.2f, 2.0f), material);
                AddPart(root, "Cab", new Vector3(2.2f, 0.95f, 0f), new Vector3(1.6f, 1.5f, 1.9f), material);
                AddPart(root, "WheelF", new Vector3(2.2f, 0.35f, 0f), new Vector3(0.7f, 0.7f, 2.1f), material);
                AddPart(root, "WheelB", new Vector3(-1.6f, 0.35f, 0f), new Vector3(0.7f, 0.7f, 2.1f), material);
                bodyRenderer = cargo.GetComponent<Renderer>();
                GreyboxStageBuilder.AddSolidBlocker(root, 0.2f); // S-166 ③ — 폴백도 같은 부피
            }

            // 적재 감지 트리거 — 짐칸 뒤편(보도 쪽) 앞 공간.
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(-0.8f, 1f, -1.8f);
            trigger.size = new Vector3(4.2f, 2f, 1.8f);

            // 실린 상자가 쌓이는 짐칸 내부 앵커.
            GameObject stack = new GameObject("StackRoot");
            stack.transform.SetParent(root.transform, false);
            stack.transform.localPosition = new Vector3(-1.6f, 0.5f, 0f);

            // S-072 ⑦ — 트럭 출발 인터랙트: 트럭 앞쪽(운전석 방향) 트리거. 통짜 모델 교체를
            // 감안해 Cab이 아니라 루트 기준 오프셋에 깐다. 해금(hasTruck) 전엔 포커스가 안 잡힌다.
            GameObject depart = GameObject.CreatePrimitive(PrimitiveType.Cube);
            depart.name = "DepartPoint";
            depart.transform.SetParent(root.transform, false);
            depart.transform.localPosition = new Vector3(3.4f, 0.6f, -0.6f); // 앞범퍼 앞·보도 쪽
            depart.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            Object.DestroyImmediate(depart.GetComponent<Renderer>());       // 보이지 않는 트리거
            BoxCollider departCollider = depart.GetComponent<BoxCollider>();
            departCollider.isTrigger = true;
            TruckDepartPoint departPoint = depart.AddComponent<TruckDepartPoint>();
            GreyboxStageBuilder.SetReference(departPoint, "_gameState", gameState);
            GreyboxStageBuilder.SetReference(departPoint, "_renderer", bodyRenderer);
            GreyboxStageBuilder.SetReference(departPoint, "_normalMaterial", bodyNormal);
            GreyboxStageBuilder.SetReference(departPoint, "_highlightMaterial", highlight);

            LoadingZone zone = root.AddComponent<LoadingZone>();
            GreyboxStageBuilder.SetReference(zone, "_stackRoot", stack.transform);
            GreyboxStageBuilder.SetReference(zone, "_boxVisualPrefab",
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Auto/prop_box_parcel.prefab"));
            GreyboxStageBuilder.SetReference(zone, "_boxMaterial", boxMaterial);
            GreyboxStageBuilder.SetReference(zone, "_renderer", bodyRenderer);
            GreyboxStageBuilder.SetReference(zone, "_normalMaterial", bodyNormal);
            GreyboxStageBuilder.SetReference(zone, "_highlightMaterial", highlight);
        }

        // S-116 ② — 실모델 트럭 정규화: 긴 축을 X(캠프 진행축)로 돌리고 발 y=0·루트 중심 정렬.
        private static void NormalizeTruckVisual(GameObject visual, Vector3 rootPosition)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            if (bounds.size.z > bounds.size.x * 1.15f)
            {
                visual.transform.Rotate(0f, 90f, 0f, Space.Self);
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            }

            visual.transform.position += rootPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        private static GameObject AddPart(GameObject root, string name, Vector3 localPos, Vector3 size, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = size;
            Object.DestroyImmediate(part.GetComponent<BoxCollider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        // 대기 물량 = 손에 집히는 박스(PickupBox) — 주문 1건씩. E로 들고 트럭으로 나른다 (S-009).
        private static System.Collections.Generic.List<PickupBox> BuildPickupBoxes(Material material, Material highlight, TuningConfigSO tuning)
        {
            var built = new System.Collections.Generic.List<PickupBox>();
            for (int i = 0; i < LOAD_ZONE_COUNT; i++)
            {
                // 피라미드 스택 — 콜라이더(0.7u)가 겹치면 스폰 순간 물리 밀어내기로 자폭한다 (S-019 실측).
                // S-119 ① — 층 간격 0.72→0.705: 갭 0.02는 "공중부양"으로 보이고, 정지 스폰된
                // 강체는 sleep 임계 아래라 낙하도 안 한다 (남규님 실관찰 — 건드려야 떨어짐).
                var (boxGo, _, _) = GreyboxStageBuilder.CreateParcelBox(
                    "CampBox_" + (i + 1).ToString("00"),
                    new Vector3(1.31f + (i % 2) * 0.9f, (i / 2) * 0.705f, -0.21f), material, // S-160 남규님 실배치
                    physical: true); // 실물 스택 (S-016 ⑥) — 아래 상자를 빼면 위가 떨어진다

                // S-164 ② — 튜토리얼 "상자 집기"·"바코드" 단계에서 맥동한다.
                var boxTarget = boxGo.AddComponent<TutorialHighlightTarget>();
                var boxSo = new SerializedObject(boxTarget);
                boxSo.FindProperty("_id").stringValue = "box";
                boxSo.ApplyModifiedPropertiesWithoutUndo();

                BoxDurability durability = boxGo.AddComponent<BoxDurability>(); // 취급주의 (S-019 ①)
                GreyboxStageBuilder.SetReference(durability, "_tuning", tuning);

                PickupBox pickup = boxGo.AddComponent<PickupBox>();
                GreyboxStageBuilder.SetReference(pickup, "_order", GetOrCreateCampOrder(i));
                GreyboxStageBuilder.SetReference(pickup, "_highlightMaterial", highlight);

                // 상차 절차(S-011): 폰으로 바코드를 찍은 짐만 들 수 있다.
                var serialized = new UnityEditor.SerializedObject(pickup);
                serialized.FindProperty("_requireScanned").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                built.Add(pickup);
            }
            return built;
        }

        // 주문판 (S-021 ③) — 캠프 복귀 시 소진 주문을 새 목적지로 교체.
        // S-052 ① — 사장님 NPC: 첫 방문 접근 튜토리얼 + 재방문 격려(부재 추첨).
        private static void BuildBossNpc(GameStateSO gameState, Material highlight)
        {
            GameObject go;
            Renderer body;
            Animator animator = null;
            GameObject bossModel = AssetDatabase.LoadAssetAtPath<GameObject>(BOSS_MODEL_PATH);
            if (bossModel != null)
            {
                go = GreyboxStageBuilder.CreateEmpty("BossNpc", new Vector3(9.99102402f, 0.0432802439f, 0.0104106665f));
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(bossModel, go.transform);
                visual.name = "Visual";
                NormalizeBossVisual(visual, go.transform.position, 1.8f);
                foreach (Collider visualCollider in visual.GetComponentsInChildren<Collider>(true))
                    visualCollider.enabled = false;

                body = visual.GetComponentInChildren<Renderer>(true);
                animator = visual.GetComponentInChildren<Animator>(true);
                if (animator == null) animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = GetOrCreateBossAnimatorController();
                animator.applyRootMotion = false;
            }
            else
            {
                (go, body) = NpcBuildKit.BuildFigure("BossNpc", new Vector3(9.99102402f, 0.0432802439f, 0.0104106665f),
                    "NpcBoss", new Color(0.32f, 0.45f, 0.38f), 1.8f);
                Debug.LogWarning("[Camp] kim_boss.fbx 미발견 — 기존 그레이박스 사장님을 사용한다.");
            }
            NpcBuildKit.AddInteractTrigger(go, 1.8f);
            NpcBuildKit.AttachNameLabel(go, "boss", "사장님"); // S-120 — 근접 이름표

            DialogueScenarioSO tutorial = NpcBuildKit.GetOrCreateScenario("Scenario_Boss_Tutorial",
                ("사장님", "어이 신입! 왔구먼. 여기가 물류캠프야."),
                ("사장님", "저기 게시판에서 오늘 주문 받고, 상자는 바코드 스캔부터 해. 스캔 안 한 짐은 못 실어."),
                ("사장님", "스캔한 상자는 트럭에 실으면 되고, 무거우면 대차에 밀어 담아서 옮겨."),
                ("사장님", "마감 시간 넘기면 벌금이야. 늦지 마 — 그게 이 바닥 제1원칙이다."),
                ("사장님", "다 실었으면 출발해. 화이팅이야, 신입!"));
            DialogueScenarioSO cheer1 = NpcBuildKit.GetOrCreateScenario("Scenario_Boss_Cheer1",
                ("사장님", "오늘도 달리는구먼. 무릎 아끼면서 뛰어!"));
            DialogueScenarioSO cheer2 = NpcBuildKit.GetOrCreateScenario("Scenario_Boss_Cheer2",
                ("사장님", "빚은 갚으라고 있는 거야. 조급해하지 말고, 늦지만 마."));
            DialogueScenarioSO cheer3 = NpcBuildKit.GetOrCreateScenario("Scenario_Boss_Cheer3",
                ("사장님", "비 오는 날 언덕길은 조심해. 미끄러지면 짐이 먼저 구른다?"));

            // S-146 — 7단계 튜토리얼(대사 + 행동 검증). 남규님 지정 항목 순서 그대로.
            // 한 단계 = 한 줄 대사 + 통과 조건. 조건을 채우기 전엔 다음으로 넘어가지 않는다.
            // S-164 — 튜토리얼 단계 저술은 **CoreSceneBuilder로 이관**했다(진행부가 Core 상주).

            CampBossNpc boss = go.AddComponent<CampBossNpc>();
            GreyboxStageBuilder.SetReference(boss, "_gameState", gameState);
            GreyboxStageBuilder.SetReference(boss, "_tutorialScenario", tutorial);
            GreyboxStageBuilder.SetReference(boss, "_highlightRenderer", body);
            GreyboxStageBuilder.SetReference(boss, "_normalMaterial", body.sharedMaterial);
            GreyboxStageBuilder.SetReference(boss, "_highlightMaterial", highlight);
            GreyboxStageBuilder.SetReference(boss, "_animator", animator);
            SerializedObject serialized = new SerializedObject(boss);
            SerializedProperty cheers = serialized.FindProperty("_cheerScenarios");
            cheers.arraySize = 3;
            cheers.GetArrayElementAtIndex(0).objectReferenceValue = cheer1;
            cheers.GetArrayElementAtIndex(1).objectReferenceValue = cheer2;
            cheers.GetArrayElementAtIndex(2).objectReferenceValue = cheer3;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void NormalizeBossVisual(GameObject visual, Vector3 rootPosition, float targetHeight)
        {
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0f, BOSS_VISUAL_YAW, 0f);
            visual.transform.localScale = Vector3.one;

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            if (bounds.size.y > 0.001f)
                visual.transform.localScale = Vector3.one * (targetHeight / bounds.size.y);

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            visual.transform.position += rootPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        private static RuntimeAnimatorController GetOrCreateBossAnimatorController()
        {
            AnimationClip idleClip = GetOrCreateCleanAnimationClip(BOSS_IDLE_PATH, "kim_boss_idle_clean.anim");
            AnimationClip walkClip = GetOrCreateCleanAnimationClip(BOSS_WALK_PATH, "kim_boss_walk_clean.anim");
            AnimationClip talkClip = GetOrCreateCleanAnimationClip(BOSS_TALK_PATH, "kim_boss_talk_clean.anim");
            if (idleClip == null || walkClip == null || talkClip == null)
            {
                Debug.LogWarning("[Camp] kim_boss 애니메이션 클립을 모두 찾지 못했다.");
                return null;
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(BOSS_CONTROLLER_PATH);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(BOSS_CONTROLLER_PATH);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = FindOrAddState(stateMachine, "Idle");
            AnimatorState walk = FindOrAddState(stateMachine, "Walk");
            AnimatorState talk = FindOrAddState(stateMachine, "Talk");
            idle.motion = idleClip;
            walk.motion = walkClip;
            talk.motion = talkClip;
            stateMachine.defaultState = idle;
            EnsureLoopTransition(idle);
            EnsureLoopTransition(walk);
            EnsureLoopTransition(talk);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssetIfDirty(controller);
            return controller;
        }

        private static AnimatorState FindOrAddState(AnimatorStateMachine stateMachine, string name)
        {
            foreach (ChildAnimatorState child in stateMachine.states)
                if (child.state.name == name) return child.state;
            return stateMachine.AddState(name);
        }

        private static AnimationClip GetOrCreateCleanAnimationClip(string sourcePath, string generatedName)
        {
            string folder = System.IO.Path.GetDirectoryName(sourcePath).Replace('\\', '/');
            string generatedPath = folder + "/" + generatedName;
            AnimationClip generated = AssetDatabase.LoadAssetAtPath<AnimationClip>(generatedPath);
            if (generated != null) return generated;

            AnimationClip source = null;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(sourcePath))
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) { source = clip; break; }
            if (source == null) return null;

            generated = Object.Instantiate(source);
            generated.name = System.IO.Path.GetFileNameWithoutExtension(generatedName);
            generated.wrapMode = WrapMode.Loop;
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(generated))
            {
                if (binding.path == "Armature/Root" && binding.propertyName.StartsWith("m_Local"))
                    AnimationUtility.SetEditorCurve(generated, binding, null);
            }
            AssetDatabase.CreateAsset(generated, generatedPath);
            AssetDatabase.SaveAssets();
            return generated;
        }

        private static void EnsureLoopTransition(AnimatorState state)
        {
            foreach (AnimatorStateTransition existing in state.transitions)
                if (existing.destinationState == state) return;
            AnimatorStateTransition transition = state.AddTransition(state);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.duration = 0f;
            transition.hasFixedDuration = true;
            transition.canTransitionToSelf = true;
        }

        private static void BuildOrderBoard(GameStateSO gameState, System.Collections.Generic.List<PickupBox> boxes)
        {
            GameObject go = GreyboxStageBuilder.CreateEmpty("OrderBoard", Vector3.zero);
            CampOrderBoard board = go.AddComponent<CampOrderBoard>();
            SerializedObject serialized = new SerializedObject(board);
            serialized.FindProperty("_gameState").objectReferenceValue = gameState;
            SerializedProperty prop = serialized.FindProperty("_boxes");
            prop.arraySize = boxes.Count;
            for (int i = 0; i < boxes.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = boxes[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // 패드별 배송 건. 1번은 그레이박스 기존 건(빌라촌)을 재사용해 District 무대와 이어진다.
        // S-035(D-064): 로드된 기존 에셋도 정본 값으로 덮는다 — district 문자열이 스폰 계약이라
        // 구 구역명("달빛맨션 구역" 등)이 남으면 스폰 0. 같은 값 재기록 = 멱등.
        private static DeliveryOrderSO GetOrCreateCampOrder(int index)
        {
            if (index == 0)
                return AssetDatabase.LoadAssetAtPath<DeliveryOrderSO>("Assets/Data/Order_HappyVilla.asset");

            string path = "Assets/Data/Order_Camp" + (index + 1).ToString("00") + ".asset";
            DeliveryOrderSO order = AssetDatabase.LoadAssetAtPath<DeliveryOrderSO>(path);
            bool created = order == null;
            if (created) order = ScriptableObject.CreateInstance<DeliveryOrderSO>();

            order.orderId = 100 + index;
            switch (index)
            {
                case 1:
                    order.address = "골목연립 반지하";
                    order.district = DeliveryOrderSO.DISTRICT_VILLATOWN;
                    order.floor = -1;
                    order.deadlineMinuteOfDay = 15f * 60f;
                    order.reward = 900;
                    break;
                case 3: // S-039 ④ — 첫날부터 아파트행 물량
                    order.address = "늦지마아파트 202호";
                    order.district = DeliveryOrderSO.DISTRICT_APARTMENT;
                    order.floor = 2;
                    order.deadlineMinuteOfDay = 18f * 60f;
                    order.reward = 1600;
                    break;
                default: // 먹자골목(19시)은 저녁 마감 — "밤 배송량↑" 표현 (D-064).
                    order.address = "달빛호프 2층";
                    order.district = DeliveryOrderSO.DISTRICT_FOODALLEY;
                    order.floor = 2;
                    order.deadlineMinuteOfDay = 19f * 60f;
                    order.reward = 1400;
                    break;
            }

            if (created)
            {
                AssetDatabase.CreateAsset(order, path);
                AssetDatabase.SaveAssets();
            }
            else
            {
                EditorUtility.SetDirty(order);
                AssetDatabase.SaveAssetIfDirty(order);
            }
            return order;
        }

        // 자판기 (S-019 ②) — E=결제 배출, 상자 투척 명중도 배출.
        private static void BuildVendingMachine(TuningConfigSO tuning, Material drinkMaterial, Material highlight)
        {
            // S-128 ② — 실모델 자판기(Bending_Mechine). 없으면 그레이박스 큐브 폴백(소켓 계약).
            Vector3 spot = new Vector3(4.5f, 0f, 2.2f);
            GameObject go = GreyboxStageBuilder.PlaceCatalog("Bending_Mechine", spot, 180f);
            Renderer bodyRenderer;
            if (go != null)
            {
                go.name = "__gb_Vending";
                // S-164 ② — 튜토리얼 "자판기" 단계에서 맥동한다.
                var vendTarget = go.AddComponent<TutorialHighlightTarget>();
                var vendSo = new SerializedObject(vendTarget);
                vendSo.FindProperty("_id").stringValue = "vending";
                vendSo.ApplyModifiedPropertiesWithoutUndo();
                Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
                // PlaceCatalog가 데코 콜라이더를 끄므로 상호작용·투척 판정용 트리거를 새로 얹는다.
                BoxCollider trigger = go.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = bounds.size + new Vector3(0.8f, 0f, 0.8f); // 앞에 서면 잡히게 여유
                trigger.center = go.transform.InverseTransformPoint(bounds.center);
                // S-166 ③ — 자판기는 뚫고 지나갈 물건이 아니다(남규님: 플레이어와 겹침).
                GreyboxStageBuilder.AddSolidBlocker(go);
                bodyRenderer = renderers[0];
            }
            else
            {
                Material body = GreyboxStageBuilder.GetOrCreateMaterial("Vending", new Color(0.85f, 0.3f, 0.3f), false);
                go = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, "Vending", spot + Vector3.up);
                go.transform.localScale = new Vector3(1.0f, 2.0f, 0.7f);
                bodyRenderer = go.GetComponent<Renderer>();
                bodyRenderer.sharedMaterial = body;
            }

            VendingMachine vending = go.AddComponent<VendingMachine>();
            GreyboxStageBuilder.SetReference(vending, "_tuning", tuning);
            GreyboxStageBuilder.SetReference(vending, "_drinkMaterial", drinkMaterial);
            GreyboxStageBuilder.SetReference(vending, "_highlightMaterial", highlight);
            GreyboxStageBuilder.SetReference(vending, "_renderer", bodyRenderer);
        }


    }
}
