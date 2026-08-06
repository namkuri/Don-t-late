using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// Hillside.unity (S-049 → S-051 달동네 → **S-129 유선형 산 개편**) — 언덕주택가:
    /// 무대 전체가 <b>hill.fbx 한 덩어리</b>다(남규님 블렌더 제작). 좌우 대칭 —
    /// 평지(x −20~2) → 완만한 상승 → 정상(x 31.9 · y 11.1) → 대칭 하강 → 평지(x 66~84). 최대 경사 27°.
    /// 스위치백 3굽이·턴패드·옹벽·긴 계단은 폐기(남규님 반려 — S-129 발주).
    /// 배치물은 전부 <see cref="GroundY"/>로 지형을 찍어 앉힌다 — 좌표 손기입 금지. 멱등(__gb_ Clear).
    /// </summary>
    public static class HillsideStageBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/Hillside.unity";
        private const string HILL_FBX = "Assets/Art/Terrains/hill.fbx";
        private const string UPHILL_SET_PREFAB = "Assets/Prefabs/Hand/set_hillside_uphill.prefab";

        // 남규님이 씬에서 직접 맞춘 값(S-129 실측) — x·y는 그대로 쓴다.
        // z만 2.70 → 4.00으로 넓혔다: 5.4u 폭에는 보행 레인(±2.6)과 판잣집이 같이 설 자리가 없다.
        private static readonly Vector3 HILL_POS = new Vector3(31.9f, 0f, 0f);
        private static readonly Vector3 HILL_SCALE = new Vector3(51.79f, 2.78f, 4.00f);
        private const float LANE_HALF_Z = 2.6f;   // 걷기 허용 폭 (능선 위)
        private const float BACK_ROW_Z = 3.3f;    // 능선 뒤편 — 판잣집·데코 줄 (레인 밖)

        private static Material _dirtMat;
        private static Material _wallMat;

        [MenuItem("DontLate/Build/Hillside Stage", priority = 15)]
        public static void BuildHillsideStage()
        {
            Scene scene;
            if (System.IO.File.Exists(SCENE_PATH))
            {
                scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }
            else
            {
                // 최초 실행 — 카메라·라이트 포함 기본 씬으로 생성 (Apartment 선례).
                scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, SCENE_PATH);
            }
            GreyboxStageBuilder.Clear();
            StripHandPlacedHill(); // Clear는 __gb_만 지운다 — 손으로 놓은 "hill"이 남으면 지형이 겹친다
            EnsureUphillSet(scene); // S-183 — 민지님 수제 오르막 세트 (병합에서 유실된 것 복원)

            var (gameState, tuning, _) = GreyboxStageBuilder.GetOrCreateStageData();

            Material asphalt = GreyboxStageBuilder.GetOrCreateMaterial("HillAsphalt", new Color(0.23f, 0.24f, 0.26f), false);
            _dirtMat = GreyboxStageBuilder.GetOrCreateMaterial("HillDirt", new Color(0.43f, 0.35f, 0.26f), false);
            _wallMat = GreyboxStageBuilder.GetOrCreateMaterial("HillWall", new Color(0.48f, 0.42f, 0.33f), false);
            Material moonHouse = GreyboxStageBuilder.GetOrCreateMaterial("HillMoonHouse", new Color(0.66f, 0.51f, 0.42f), false);
            Material slate = GreyboxStageBuilder.GetOrCreateMaterial("HillSlate", new Color(0.29f, 0.31f, 0.33f), false);

            // ── 지형 ─────────────────────────────────────────────
            // 산 아래를 받치는 평지(들머리·날머리 바깥과 능선 뒤편). 산의 평지 구간과 같은 y0라
            // Z파이팅을 피해 살짝 내려 깐다.
            GameObject baseGround = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Plane, "BaseGround",
                new Vector3(32f, -0.02f, 0f));
            baseGround.transform.localScale = new Vector3(12f, 1f, 1.2f); // x −28~92 · z −6~6
            baseGround.GetComponent<Renderer>().sharedMaterial = _dirtMat;
            baseGround.layer = GreyboxStageBuilder.LAYER_GROUND;

            BuildHill(asphalt);
            Physics.SyncTransforms(); // 이 아래 배치는 전부 GroundY 레이캐스트에 의존한다

            // ── 달동네 판잣집 — 능선 뒤편(레인 밖)에 고도를 따라 줄지어 실루엣을 만든다 ──
            BuildMoonHouse("MoonHouse_S1", OnGround(9f, BACK_ROW_Z, -0.25f), moonHouse, slate);
            BuildMoonHouse("MoonHouse_S2", OnGround(17f, BACK_ROW_Z, -0.25f), moonHouse, slate);
            BuildMoonHouse("MoonHouse_S3", OnGround(24f, BACK_ROW_Z, -0.25f), moonHouse, slate);
            BuildMoonHouse("MoonHouse_P1", OnGround(30f, BACK_ROW_Z, -0.2f), moonHouse, slate);
            BuildMoonHouse("MoonHouse_P2", OnGround(35f, BACK_ROW_Z, -0.2f), moonHouse, slate);
            BuildMoonHouse("MoonHouse_D1", OnGround(43f, BACK_ROW_Z, -0.25f), moonHouse, slate);
            BuildMoonHouse("MoonHouse_D2", OnGround(51f, BACK_ROW_Z, -0.25f), moonHouse, slate);

            // ── 걷기 볼륨 — 능선 위만. z는 산 폭(±4)보다 좁게 잡아 옆으로 떨어지지 않게 한다 ──
            GameObject volume = GreyboxStageBuilder.CreateEmpty("Walkable", Vector3.zero);
            BoxCollider walkable = volume.AddComponent<BoxCollider>();
            walkable.isTrigger = true;
            walkable.size = new Vector3(96f, 28f, LANE_HALF_Z * 2f);
            walkable.center = new Vector3(28f, 12f, 0f); // x −20~76 · y −2~26
            volume.AddComponent<WalkableVolume>();

            // ── 스포너 (밴드별 앵커 — floor 2=오르막 중턱, 3=정상, 4=내리막 중턱) ──
            AttachSpawner(gameState);

            // S-052 ②③ — 들머리 평지 행인 2 + 심부름 할머니(평지 → 정상: 진짜 등반 심부름이 된다).
            NpcBuildKit.BuildPedestrian("Walker_A", OnGround(-12f, 2.0f), new Color(0.45f, 0.52f, 0.62f), 5f);
            NpcBuildKit.BuildPedestrian("Walker_B", OnGround(-6f, 2.4f), new Color(0.60f, 0.48f, 0.40f), 6f);
            NpcBuildKit.BuildErrandNpc("ErrandGranny", "할머니", OnGround(-9f, 1.8f),
                OnGround(31f, 0.6f), gameState, 2500);

            // S-054b 엣지 워크 — 왼쪽 끝 = 이전 동네(먹자골목).
            // S-186 ② — 언덕주택가가 3번째가 되면서 **오른쪽에 Next(아파트단지)가 생겼다**.
            // 종전엔 종점이라 게이트가 하나뿐이었다. 산 능선은 x 66~84가 평지라 날머리에 세운다.
            EdgeGateBuildKit.BuildGate("EdgeGate_Prev", OnGround(-19.5f, 0f),
                DontLate.DistrictEdgeGate.Direction.Prev, gameState);
            EdgeGateBuildKit.BuildGate("EdgeGate_Next", OnGround(76f, 0f),
                DontLate.DistrictEdgeGate.Direction.Next, gameState);

            // S-059 — 달동네 고양이 (정상 마당 · 데려오면 집에 정착).
            BuildCat(gameState, OnGround(34f, -1.2f));

            // ── 플레이어·카메라(Y 팔로우) ────────────────────
            ArtBackdropKit.Build(ArtBackdropKit.Hillside); // S-180 ② — 아트 세트 소켓(프리팹 없으면 무시)
            GreyboxStageBuilder.BuildPlayer(gameState, tuning);
            GameObject player = GameObject.Find("__gb_Player");
            if (player != null) player.transform.position = OnGround(-16f, 0f, 0.1f);

            // S-115 — 실물 데코: 들머리·날머리 평지에 한옥 (산비탈은 판잣집 몫).
            GreyboxStageBuilder.PlaceCatalog("old_korea_house", OnGround(-17f, BACK_ROW_Z + 0.6f));
            GreyboxStageBuilder.PlaceCatalog("retro_korean_house", OnGround(-6f, BACK_ROW_Z + 0.6f));
            GreyboxStageBuilder.PlaceCatalog("red_korean_house", OnGround(70f, BACK_ROW_Z + 0.6f));
            GreyboxStageBuilder.PlaceCatalog("Pot_unity", OnGround(-11.5f, 2.4f));
            GreyboxStageBuilder.PlaceCatalog("black_Trash_unity", OnGround(4f, 2.4f));
            GreyboxStageBuilder.PlaceCatalog("bycle", OnGround(-4f, 2.4f), 15f);

            GreyboxStageBuilder.BuildGroundMist();
            GreyboxStageBuilder.BuildStarField();
            GreyboxStageBuilder.BuildPostVolume();
            GreyboxStageBuilder.ConfigureCamera();
            GreyboxStageBuilder.AttachCameraFollow();
            Camera camera = Camera.main;
            if (camera != null && camera.TryGetComponent(out CameraFollowX follow))
            {
                SerializedObject serialized = new SerializedObject(follow);
                serialized.FindProperty("_followY").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log("[Hillside] 유선형 산 무대 조립 완료 — 정상 y" + GroundY(31.9f).ToString("F2")
                + " · 최대 경사 27° · 배치물 지면 스냅 (S-129).");
        }

        // ── 지형 ────────────────────────────────────────────────

        /// <summary>손으로 씬에 끌어다 놓은 hill 인스턴스 제거 — Clear()가 __gb_ 접두어만 지우기 때문.</summary>
        private static void StripHandPlacedHill()
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go == null || go.transform.parent != null) continue;
                if (go.name != "hill" && !go.name.StartsWith("hill ")) continue;
                Undo.DestroyObjectImmediate(go);
            }
        }

        /// <summary>
        /// 민지님 수제 오르막 세트를 꽂는다. S-183 — 이 메서드는 민지님이 PR#34에 넣었는데
        /// 본인이 main을 병합할 때(49c6b67a) 충돌을 main 쪽으로 해소하며 **통째로 지워졌다**.
        /// 프리팹만 남고 꽂아줄 코드가 없어 배치가 게임에 안 나오는 상태였다 — 원문 그대로 복원.
        /// </summary>
        private static void EnsureUphillSet(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "set_hillside_uphill") return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UPHILL_SET_PREFAB);
            if (prefab == null)
            {
                Debug.Log("[Hillside] set_hillside_uphill 미배치 — 수제 오르막 세트 생략.");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null) return;
            instance.name = "set_hillside_uphill";
            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = GreyboxStageBuilder.LAYER_GROUND;
        }

        private static void BuildHill(Material surface)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(HILL_FBX);
            if (source == null)
            {
                Debug.LogError("[Hillside] " + HILL_FBX + " 없음 — 지형 없이 조립한다.");
                return;
            }
            // 프리팹 Variant 결합 회피 — 독립 클론으로 만든다 (2026-07-20 실수→규칙).
            GameObject hill = (GameObject)Object.Instantiate(source);
            hill.name = "__gb_Hill";
            hill.transform.SetPositionAndRotation(HILL_POS, Quaternion.identity);
            hill.transform.localScale = HILL_SCALE;
            hill.layer = GreyboxStageBuilder.LAYER_GROUND;

            foreach (Renderer renderer in hill.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterial = surface;

            foreach (MeshFilter filter in hill.GetComponentsInChildren<MeshFilter>())
            {
                filter.gameObject.layer = GreyboxStageBuilder.LAYER_GROUND; // S-128 ③ — 눈·비가 여기 쌓인다
                MeshCollider collider = filter.GetComponent<MeshCollider>();
                if (collider == null) collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }
        }

        /// <summary>지형 표면 높이. Ground 레이어만 본다 — 이미 놓인 데코·플레이어에 걸리지 않는다.</summary>
        private static float GroundY(float x, float z = 0f)
        {
            int mask = 1 << GreyboxStageBuilder.LAYER_GROUND;
            if (Physics.Raycast(new Vector3(x, 60f, z), Vector3.down, out RaycastHit hit, 120f,
                    mask, QueryTriggerInteraction.Ignore))
                return hit.point.y;
            return 0f;
        }

        private static Vector3 OnGround(float x, float z, float lift = 0f)
            => new Vector3(x, GroundY(x, z) + lift, z);

        // ── 판잣집: 몸통 + 슬레이트 지붕(살짝 기울임) ──
        private static void BuildMoonHouse(string id, Vector3 basePos, Material body, Material roof)
        {
            BuildBox(id, basePos + Vector3.up * 1f, new Vector3(2.6f, 2f, 1.8f), body);
            GameObject roofGo = BuildBox(id + "_roof", basePos + new Vector3(0f, 2.12f, 0f), new Vector3(3f, 0.18f, 2.1f), roof);
            roofGo.transform.rotation = Quaternion.Euler(0f, 0f, 6f);
        }

        // S-059 고양이 — 작은 주황 덩어리 + 상호작용 트리거.
        private static void BuildCat(GameStateSO gameState, Vector3 position)
        {
            GameObject root = GreyboxStageBuilder.CreateEmpty("Cat", position);
            Material fur = GreyboxStageBuilder.GetOrCreateMaterial("CatFur", new Color(0.85f, 0.55f, 0.25f), false);
            Material highlight = GreyboxStageBuilder.GetOrCreateHighlightMaterial();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.22f, 0.26f, 0.22f);
            Renderer bodyRenderer = body.GetComponent<Renderer>();
            bodyRenderer.sharedMaterial = fur;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0.22f, 0.3f, 0f);
            head.transform.localScale = Vector3.one * 0.22f;
            head.GetComponent<Renderer>().sharedMaterial = fur;

            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.6f;
            trigger.center = new Vector3(0f, 0.25f, 0f);

            DialogueScenarioSO meet = NpcBuildKit.GetOrCreateScenario("Scenario_Cat_Meet",
                ("고양이", "...야옹. (경계하다가 코를 킁킁거린다 — 따라오고 싶어 하는 눈치다)"),
                ("고양이", "야옹! (먼저 집 쪽으로 뛰어갔다. 사료를 챙겨줘야 할 것 같다 — 쇼핑 앱)"));

            HillsideCat cat = root.AddComponent<HillsideCat>();
            GreyboxStageBuilder.SetReference(cat, "_gameState", gameState);
            GreyboxStageBuilder.SetReference(cat, "_meetScenario", meet);
            GreyboxStageBuilder.SetReference(cat, "_highlightRenderer", bodyRenderer);
            GreyboxStageBuilder.SetReference(cat, "_normalMaterial", fur);
            GreyboxStageBuilder.SetReference(cat, "_highlightMaterial", highlight);
        }

        private static GameObject BuildBox(string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject box = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, name, position);
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        private static void AttachSpawner(GameStateSO gameState)
        {
            GameObject go = new GameObject("__gb_CargoSpawner");
            DistrictCargoSpawner spawner = go.AddComponent<DistrictCargoSpawner>();

            Transform boxOrigin = GreyboxStageBuilder.CreateEmpty("BoxOrigin", OnGround(-17f, -1.2f)).transform;
            // floor 2=오르막 중턱 · 3=정상 · 4=내리막 중턱 — 배송이 "올라갔다 내려오는" 리듬이 된다.
            var anchors = new Transform[3];
            anchors[0] = GreyboxStageBuilder.CreateEmpty("BeaconAnchor_Up", OnGround(14f, -0.6f)).transform;
            anchors[1] = GreyboxStageBuilder.CreateEmpty("BeaconAnchor_Top", OnGround(29f, 0.4f)).transform;
            anchors[2] = GreyboxStageBuilder.CreateEmpty("BeaconAnchor_Down", OnGround(48f, -0.6f)).transform;

            SerializedObject serialized = new SerializedObject(spawner);
            serialized.FindProperty("_gameState").objectReferenceValue = gameState;
            serialized.FindProperty("_boxVisualPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Auto/prop_box_parcel.prefab");
            serialized.FindProperty("_beaconPrefab").objectReferenceValue = GreyboxStageBuilder.GetOrCreateBeaconPrefab();
            serialized.FindProperty("_boxHighlight").objectReferenceValue =
                GreyboxStageBuilder.GetOrCreateHighlightMaterial();
            serialized.FindProperty("_boxFallback").objectReferenceValue =
                GreyboxStageBuilder.GetOrCreateMaterial("Box", GreyboxStageBuilder.ParseColor("#ff9f45"), false);
            serialized.FindProperty("_tuning").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TuningConfigSO>("Assets/Data/Tuning.asset");
            serialized.FindProperty("_boxOrigin").objectReferenceValue = boxOrigin;
            // S-129 — 비탈에서는 층 앵커의 슬롯 간격(+5u X)이 지면을 벗어난다. 스냅 켠다.
            serialized.FindProperty("_snapBeaconsToGround").boolValue = true;
            SerializedProperty anchorsProp = serialized.FindProperty("_floorBeaconAnchors");
            anchorsProp.arraySize = anchors.Length;
            for (int i = 0; i < anchors.Length; i++)
                anchorsProp.GetArrayElementAtIndex(i).objectReferenceValue = anchors[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
