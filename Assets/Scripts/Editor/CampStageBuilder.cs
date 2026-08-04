using UnityEditor;
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
        private const int LOAD_ZONE_COUNT = 4; // S-039 ④ — 4번째 = 아파트행 물량

        [MenuItem("DontLate/Build/Camp Stage", priority = 12)]
        public static void BuildCampStage()
        {
            Scene scene = EditorSceneManager.OpenScene(CAMP_PATH, OpenSceneMode.Single);
            GreyboxStageBuilder.Clear();

            var (gameState, tuning, _) = GreyboxStageBuilder.GetOrCreateStageData();

            Material ground = GreyboxStageBuilder.GetOrCreateMaterial("Ground", new Color(0.24f, 0.24f, 0.26f), false);
            Material lane = GreyboxStageBuilder.GetOrCreateMaterial("Lane", new Color(0.34f, 0.33f, 0.30f), false);
            Material box = GreyboxStageBuilder.GetOrCreateMaterial("Box", GreyboxStageBuilder.ParseColor("#ff9f45"), false);
            Material truck = GreyboxStageBuilder.GetOrCreateMaterial("Truck", new Color(0.30f, 0.42f, 0.55f), false);

            Material highlight = GreyboxStageBuilder.GetOrCreateMaterial("Highlight", GreyboxStageBuilder.ParseColor("#35e0c8"), true);
            Material drink = GreyboxStageBuilder.GetOrCreateMaterial("Drink", GreyboxStageBuilder.ParseColor("#e04a35"), false);

            GreyboxStageBuilder.BuildGround(ground, lane);
            GreyboxStageBuilder.BuildWalkableVolume();
            GreyboxStageBuilder.BuildGroundMist();
            GreyboxStageBuilder.BuildStarField(); // S-033 ① — 캠프 밤하늘 별 (밤 페이드는 StarField.cs 공용)
            GreyboxStageBuilder.BuildDeliveryCart(new Vector3(-4f, 0f, 1.2f)); // S-039 ④ — 캠프에서도 대차 운반
            BuildTruck(truck, box, highlight, gameState);
            System.Collections.Generic.List<PickupBox> boxes = BuildPickupBoxes(box, highlight, tuning);
            BuildOrderBoard(gameState, boxes);
            BuildDrink(drink, highlight);
            BuildVendingMachine(tuning, drink, highlight);
            BuildBossNpc(gameState, highlight);                                  // S-052 ① 사장님
            EdgeGateBuildKit.BuildGate("EdgeGate_Next", new Vector3(14f, 0f, 0f),
                DontLate.DistrictEdgeGate.Direction.Next, gameState);             // S-054b 도보 개척 출구
            EdgeGateBuildKit.BuildGate("EdgeGate_Home", new Vector3(-14f, 0f, 0f),
                DontLate.DistrictEdgeGate.Direction.Prev, gameState);             // S-062 ② 집 방향
            // S-115 — 실물 데코: 물류 배경 건물 + 야드 소품 (없으면 생략 — 소켓).
            GreyboxStageBuilder.PlaceCatalog("logi_center", new Vector3(0f, 0f, 16f)); // 원경 1채
            GreyboxStageBuilder.PlaceCatalog("belt", new Vector3(-6.5f, 0f, 2.2f), 90f);
            // S-123 ① — 포장마차 독백. District 프랍 풀에 넣으면 결정론 배치 계약이 깨지므로
            // (풀 길이가 바뀌면 전 구역 배치가 달라진다) 캠프의 손배치 데코에 붙인다.
            GameObject foodCart = GreyboxStageBuilder.PlaceCatalog("Food_cart_unity", new Vector3(6.5f, 0f, 2.6f), 180f);
            if (foodCart != null)
            {
                DistrictSceneBuilder.AttachRemarkSpot(foodCart, 3f, new[]
                {
                    "맛있어 보인다...", "저거 한 그릇 하고 싶다.", "일 끝나고 오자. 지금은 참고.",
                });
                KioskBuildKit.MakeKiosk(foodCart, "포장마차", KioskBuildKit.StreetFoodItems); // S-125 ②
            }
            // S-116 ② — white_van 데코 철거: 실모델 트럭과 함께 서면 "트럭 2대"로 읽힌다 (남규님 실관찰).
            GreyboxStageBuilder.PlaceCatalog("Trash_Bin_unity", new Vector3(-2.2f, 0f, 2.4f));

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

            EditorSceneManager.SaveScene(scene, CAMP_PATH);
            Debug.Log("[Camp] 무대 조립 완료 — 박스 " + LOAD_ZONE_COUNT
                    + "개를 E로 들어 트럭 짐칸 뒤에서 E로 싣는다 (S-009).");
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
            }
            else
            {
                GameObject cargo = AddPart(root, "Cargo", new Vector3(-0.8f, 1.5f, 0f), new Vector3(4.0f, 2.2f, 2.0f), material);
                AddPart(root, "Cab", new Vector3(2.2f, 0.95f, 0f), new Vector3(1.6f, 1.5f, 1.9f), material);
                AddPart(root, "WheelF", new Vector3(2.2f, 0.35f, 0f), new Vector3(0.7f, 0.7f, 2.1f), material);
                AddPart(root, "WheelB", new Vector3(-1.6f, 0.35f, 0f), new Vector3(0.7f, 0.7f, 2.1f), material);
                bodyRenderer = cargo.GetComponent<Renderer>();
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
                    new Vector3(-7f + (i % 2) * 0.9f, (i / 2) * 0.705f, 1.5f), material,
                    physical: true); // 실물 스택 (S-016 ⑥) — 아래 상자를 빼면 위가 떨어진다

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
            var (go, body) = NpcBuildKit.BuildFigure("BossNpc", new Vector3(-7.5f, 0f, 1.6f),
                "NpcBoss", new Color(0.32f, 0.45f, 0.38f), 1.8f);
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
            // S-151 — 말투를 따뜻하게 다시 썼다(남규님 "사장이 따뜻한 말투로 해야하는데 너무 딱딱함").
            // 사장님은 감독관이 아니라 **먼저 이 일을 해본 사람**이다. 지시문 대신 권유·염려로 쓰고,
            // 단계마다 칭찬을 붙여 플레이어가 잘 따라오고 있다는 신호를 준다.
            var steps = new (string title, string line, CampTutorialDirector.Gate gate, string hint, string praise)[]
            {
                ("Move",   "어어, 왔구나! 기다렸어. 오늘부터 같이 일하는 거지?\n"
                         + "긴장 풀고, WASD로 천천히 좀 걸어봐. 몸부터 풀어야 안 다쳐.",
                    CampTutorialDirector.Gate.Move,      "WASD로 이동해 보세요",
                    "그래 그래, 자연스럽네. 발놀림이 좋은데?"),

                // S-152 — 남규님 지적: I키만 알려주면 화면 위 [가방] 버튼을 못 찾는다. 둘 다 안내한다.
                ("Bag",    "가방 한번 열어볼래? I키를 누르거나, 화면 위쪽 [가방] 버튼을 눌러도 열려.\n"
                         + "드링크나 길에서 주운 것들이 여기 들어가. 급할 때 요긴하다고.",
                    CampTutorialDirector.Gate.BagOpen,   "I키 또는 화면 위 [가방] 버튼",
                    "옳지. 뭐 들었나 가끔 확인해 보면 좋아."),

                ("Phone",  "이제 폰이야. Tab 눌러봐.\n"
                         + "주문도 지도도 은행도 전부 여기 있어. 하루 종일 들여다볼 물건이지.",
                    CampTutorialDirector.Gate.PhoneOpen, "Tab키로 휴대폰을 열어 보세요",
                    "잘했어. 길 잃으면 지도부터 켜는 거, 잊지 말고."),

                // S-152 — 종전 설명이 틀렸다(남규님 정정). 폰을 먼저 켜는 게 아니라,
                // 바코드에 마우스를 올리면 **폰이 알아서 올라오고** 카메라 중앙에 맞춰야 찍힌다.
                ("Barcode","자, 이제 진짜 일이다. 짐은 바코드를 찍어야 실을 수 있어.\n"
                         + "상자 가까이 가서 마우스로 상자를 클릭해봐. 송장이 뜰 거야.\n"
                         + "거기 바코드에 마우스를 갖다 대면 폰이 저절로 올라와. 그 상태로\n"
                         + "카메라 한가운데에 바코드를 맞추고 잠깐 있으면 — 찰칵, 알아서 찍힌다.",
                    CampTutorialDirector.Gate.Barcode,   "상자 클릭 → 송장의 바코드에 마우스 → 카메라 중앙에 맞추기",
                    "찰칵! 바로 그거야. 처음엔 손이 떨리는데 금방 익숙해져."),

                // S-155 — 남규님: 픽업 때 목적지를 알려주면 좋겠다.
                ("Pickup", "스캔했으면 이제 들면 돼. 상자 앞에서 E를 눌러봐.\n"
                         + "이 건은 빌라촌이야. 골목 모퉁이 돌아서 양옥집 쪽으로 가면\n"
                         + "바닥이 은은하게 빛나는 자리가 있을 거야 — 거기가 내려놓는 데다.\n"
                         + "무거우면 무리하지 말고, 천천히 가도 괜찮아.",
                    CampTutorialDirector.Gate.BoxPickup, "E키로 상자를 집어 보세요",
                    "좋아 좋아. 허리 조심하고!"),

                // S-155 — 시작 가방에 드링크 1개를 넣어뒀다(CoreBootstrap). 여기서 써 보게 한다.
                // S-156 — 조작까지 적는다(남규님: 우클릭 → 사용 버튼). 물건만 주고 쓰는 법을 안 알려주면
                // 가방을 열어놓고도 못 쓴다.
                ("Drink",  "아 참, 가방에 에너지드링크 하나 넣어놨어. 내가 주는 거야.\n"
                         // ⚠ 마크다운(**)은 TMP에서 별표 그대로 보인다 — 강조는 리치텍스트 태그로.
                         + "가방(I) 열고 그 드링크를 <b>우클릭</b>하면 [사용] 버튼이 뜰 거야.\n"
                         + "그거 눌러서 한번 마셔봐. 지쳤을 때 이만한 게 없거든.",
                    CampTutorialDirector.Gate.DrinkUse,  "가방(I) → 드링크 우클릭 → [사용]",
                    "그렇지! 힘들 때 미루지 말고 바로 마셔. 쓰러지고 나면 늦어."),

                // S-156 — 넷인데 셋이라고 했다(남규님 정정). 먹자골목이 빠져 있었다.
                ("Area",   "구역은 넷이야. 빌라촌, 먹자골목, 아파트단지, 언덕주택가.\n"
                         + "먹자골목은 사람도 많고 노점도 많아 — 배 고프면 거기서 뭐 사 먹어도 되고.\n"
                         + "언덕은 비 오면 미끄러우니까 그런 날은 특히 조심하고. 아파트는 엘리베이터랑\n"
                         + "현관 비밀번호가 있어. 헷갈리면 폰 지도 보면 돼, 다 나와 있으니까.",
                    CampTutorialDirector.Gate.ReadOnly,  "",
                    "뭐, 다니다 보면 몸이 먼저 기억할 거야."),

                ("Npc",    "길에서 사람 마주치면 E로 말 한번 걸어봐.\n"
                         + "이 동네 사람들 은근히 정 많아. 얼굴 트면 팁도 챙겨주고 그래.",
                    CampTutorialDirector.Gate.NpcTalk,   "NPC에게 E로 말을 걸어 보세요",
                    "거봐, 나쁘지 않지? 인사만 잘해도 하루가 편해."),

                ("Kiosk",  "마지막이야. 자판기랑 편의점, 포장마차는 E로 열어서 사면 돼.\n"
                         + "힘들면 꼭 뭐라도 챙겨 먹어. 굶고 뛰다 쓰러지는 애들 여럿 봤다.",
                    CampTutorialDirector.Gate.KioskOpen, "자판기·편의점·포장마차를 E로 열어 보세요",
                    "그래, 이제 다 알려준 것 같네. 무리하지 말고 다녀와. 늦으면... 뭐, 나한테 혼나는 거지!"),
            };

            GameObject tutorialGo = new GameObject("__gb_CampTutorial");
            CampTutorialDirector director = tutorialGo.AddComponent<CampTutorialDirector>();
            GreyboxStageBuilder.SetReference(director, "_gameState", gameState);
            SerializedObject dirSo = new SerializedObject(director);
            SerializedProperty stepList = dirSo.FindProperty("_steps");
            stepList.arraySize = steps.Length;
            for (int i = 0; i < steps.Length; i++)
            {
                DialogueScenarioSO line = NpcBuildKit.GetOrCreateScenario(
                    "Scenario_Tutorial_" + steps[i].title, ("사장님", steps[i].line));
                DialogueScenarioSO praise = NpcBuildKit.GetOrCreateScenario(
                    "Scenario_Tutorial_" + steps[i].title + "_Praise", ("사장님", steps[i].praise));
                SerializedProperty element = stepList.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("scenario").objectReferenceValue = line;
                element.FindPropertyRelative("gate").enumValueIndex = (int)steps[i].gate;
                element.FindPropertyRelative("hint").stringValue = steps[i].hint;
                element.FindPropertyRelative("praise").objectReferenceValue = praise;
            }
            dirSo.ApplyModifiedPropertiesWithoutUndo();

            CampBossNpc boss = go.AddComponent<CampBossNpc>();
            GreyboxStageBuilder.SetReference(boss, "_gameState", gameState);
            GreyboxStageBuilder.SetReference(boss, "_tutorialScenario", tutorial);
            GreyboxStageBuilder.SetReference(boss, "_tutorial", director);
            GreyboxStageBuilder.SetReference(boss, "_highlightRenderer", body);
            GreyboxStageBuilder.SetReference(boss, "_normalMaterial", body.sharedMaterial);
            GreyboxStageBuilder.SetReference(boss, "_highlightMaterial", highlight);
            SerializedObject serialized = new SerializedObject(boss);
            SerializedProperty cheers = serialized.FindProperty("_cheerScenarios");
            cheers.arraySize = 3;
            cheers.GetArrayElementAtIndex(0).objectReferenceValue = cheer1;
            cheers.GetArrayElementAtIndex(1).objectReferenceValue = cheer2;
            cheers.GetArrayElementAtIndex(2).objectReferenceValue = cheer3;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
                Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
                // PlaceCatalog가 데코 콜라이더를 끄므로 상호작용·투척 판정용 트리거를 새로 얹는다.
                BoxCollider trigger = go.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = bounds.size + new Vector3(0.8f, 0f, 0.8f); // 앞에 서면 잡히게 여유
                trigger.center = go.transform.InverseTransformPoint(bounds.center);
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

        // 에너지드링크 — E로 회복(EnergyDrinkPickup — S-005).
        private static void BuildDrink(Material material, Material highlight)
        {
            GameObject go = GreyboxStageBuilder.CreatePrimitive(
                PrimitiveType.Capsule, "Drink", new Vector3(4f, 0.25f, -1f));
            go.transform.localScale = new Vector3(0.22f, 0.25f, 0.22f);
            var collider = go.GetComponent<Collider>();
            collider.isTrigger = true;
            go.GetComponent<Renderer>().sharedMaterial = material;

            EnergyDrinkPickup pickup = go.AddComponent<EnergyDrinkPickup>();
            GreyboxStageBuilder.SetReference(pickup, "_renderer", go.GetComponent<Renderer>());
            GreyboxStageBuilder.SetReference(pickup, "_normalMaterial", material);
            GreyboxStageBuilder.SetReference(pickup, "_highlightMaterial", highlight);
        }

    }
}
