using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// Apartment.unity(아파트단지 — S-038 · D-067) 그레이박스 무대 조립.
    /// 레이아웃(X축 세그먼트): 외부 마당(-20..-2) → 공동현관(비번 게이트) → 로비(2..16, 엘베) →
    /// 층 복도 2층(24..38) · 3층(44..58) · 4층(64..78). 엘베 이동 = 세그먼트 텔레포트 + 게임분 소모.
    /// 대차·짐 비콘(도크)·비번 패널·엘베 패널·세대 비콘 스포너까지 전부 여기서 배선. 멱등(__gb_ Clear).
    /// </summary>
    public static class ApartmentStageBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/Apartment.unity";

        // 층 복도 시작 X — 인덱스 0=1층(로비 엘베 앞), 1=2층…
        private static readonly float[] FloorExitX = { 13f, 25f, 45f, 65f };

        [MenuItem("DontLate/Build/Apartment Stage", priority = 14)]
        public static void BuildApartmentStage()
        {
            Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            GreyboxStageBuilder.Clear();

            var (gameState, tuning, _) = GreyboxStageBuilder.GetOrCreateStageData();

            Material ground = GreyboxStageBuilder.GetOrCreateMaterial("Ground", new Color(0.24f, 0.24f, 0.26f), false);
            Material lobby = GreyboxStageBuilder.GetOrCreateMaterial("AptLobby", new Color(0.36f, 0.34f, 0.30f), false);
            Material corridor = GreyboxStageBuilder.GetOrCreateMaterial("AptCorridor", new Color(0.42f, 0.40f, 0.36f), false);
            Material wall = GreyboxStageBuilder.GetOrCreateMaterial("AptWall", new Color(0.50f, 0.48f, 0.44f), false);
            Material panelMat = GreyboxStageBuilder.GetOrCreateMaterial("AptPanel", new Color(0.16f, 0.20f, 0.30f), false);
            Material highlight = GreyboxStageBuilder.GetOrCreateMaterial("Highlight", GreyboxStageBuilder.ParseColor("#35e0c8"), true);
            Material dockMat = GreyboxStageBuilder.GetOrCreateMaterial("AptDock", GreyboxStageBuilder.ParseColor("#ff9f45"), true);

        // ── 지면·구획 ────────────────────────────────────
            BuildStrip("YardGround", -20f, -1f, ground);          // 외부 마당
            BuildStrip("LobbyGround", -1f, 17f, lobby);           // 로비
            BuildStrip("Corridor2F", 23f, 39f, corridor);
            BuildStrip("Corridor3F", 43f, 59f, corridor);
            BuildStrip("Corridor4F", 63f, 79f, corridor);

            // 공동현관 벽 (마당-로비 사이 — 시각 구획, 통행은 게이트 텔레포트만)
            BuildWall("EntranceWall", new Vector3(0f, 2f, 0f), new Vector3(0.4f, 4f, 6f), wall);
            // 복도 뒷벽 + 층 라벨 대비 색벽
            BuildWall("LobbyBack", new Vector3(8f, 2f, 3f), new Vector3(18f, 4f, 0.3f), wall);
            BuildWall("Back2F", new Vector3(31f, 2f, 3f), new Vector3(16f, 4f, 0.3f), wall);
            BuildWall("Back3F", new Vector3(51f, 2f, 3f), new Vector3(16f, 4f, 0.3f), wall);
            BuildWall("Back4F", new Vector3(71f, 2f, 3f), new Vector3(16f, 4f, 0.3f), wall);
            // 세그먼트 사이 갭 시각 차단벽
            BuildWall("Gap12", new Vector3(20f, 2f, 0f), new Vector3(0.4f, 4f, 6f), wall);
            BuildWall("Gap23", new Vector3(41f, 2f, 0f), new Vector3(0.4f, 4f, 6f), wall);
            BuildWall("Gap34", new Vector3(61f, 2f, 0f), new Vector3(0.4f, 4f, 6f), wall);

            // 걷기 볼륨 — 전 세그먼트 커버 (게이트·엘베가 세그먼트 사이를 텔레포트로 잇는다)
            GameObject volume = GreyboxStageBuilder.CreateEmpty("Walkable", Vector3.zero);
            BoxCollider walkable = volume.AddComponent<BoxCollider>();
            walkable.isTrigger = true;
            walkable.size = new Vector3(100f, 4f, 6f);
            walkable.center = new Vector3(29f, 2f, 0f);
            volume.AddComponent<WalkableVolume>();

            // ── 대차 (공용 헬퍼 — S-039 트리거 콜라이더) ─────
            GreyboxStageBuilder.BuildDeliveryCart(new Vector3(-9f, 0f, 0f));

            // ── 도크(짐 전용 비콘 패드) + 비번 게이트 ────────
            GameObject dock = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, "CartDockPad", new Vector3(-4.5f, 0.03f, 0f));
            Object.DestroyImmediate(dock.GetComponent<Collider>());
            dock.transform.localScale = new Vector3(2.4f, 0.06f, 2.4f);
            dock.GetComponent<Renderer>().sharedMaterial = dockMat;

            GameObject lobbySpawn = GreyboxStageBuilder.CreateEmpty("LobbySpawn", new Vector3(3f, 0f, 0f));
            BuildPasswordGate(gameState, panelMat, highlight, dock.transform, lobbySpawn.transform);

            // ── 엘리베이터 패널 (로비 + 각 층) ───────────────
            Transform[] exits = new Transform[FloorExitX.Length];
            for (int i = 0; i < FloorExitX.Length; i++)
                exits[i] = GreyboxStageBuilder.CreateEmpty("FloorExit_" + (i + 1), new Vector3(FloorExitX[i] + 1.5f, 0f, 0f)).transform;
            for (int floor = 1; floor <= FloorExitX.Length; floor++)
                BuildElevatorPanel(floor, exits, tuning, panelMat, highlight);

            // ── 세대 비콘 스포너 (cargo 아파트 건 → 층별 앵커) ──
            AttachSpawner(gameState);

            // ── 플레이어·카메라 ─────────────────────────────
            GreyboxStageBuilder.BuildPlayer(gameState, tuning);
            GameObject player = GameObject.Find("__gb_Player");
            if (player != null) player.transform.position = new Vector3(-16f, 0f, 0f); // 마당에서 시작
            GreyboxStageBuilder.BuildPostVolume();
            GreyboxStageBuilder.ConfigureCamera();
            GreyboxStageBuilder.AttachCameraFollow();

            // ── 아파트 UI (키패드·층 선택) ───────────────────
            BuildApartmentCanvas();

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log("[Apartment] 무대 조립 완료 — 마당→비번→로비→엘베→층 복도 (S-038).");
        }

        private static void BuildStrip(string name, float fromX, float toX, Material material)
        {
            float width = toX - fromX;
            GameObject strip = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, name,
                new Vector3(fromX + width * 0.5f, -0.05f, 0f));
            strip.transform.localScale = new Vector3(width, 0.1f, 6f);
            strip.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void BuildWall(string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject wallGo = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, name, position);
            wallGo.transform.localScale = size;
            wallGo.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void BuildPasswordGate(GameStateSO gameState, Material material, Material highlight,
            Transform dockPoint, Transform lobbySpawn)
        {
            GameObject panel = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, "PasswordGate", new Vector3(-1.2f, 1.2f, -1.4f));
            panel.transform.localScale = new Vector3(0.25f, 0.5f, 0.4f);
            panel.GetComponent<Renderer>().sharedMaterial = material;
            BoxCollider trigger = panel.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(6f, 4f, 8f); // 근접 포커스 여유

            ApartmentPasswordGate gate = panel.AddComponent<ApartmentPasswordGate>();
            GreyboxStageBuilder.SetReference(gate, "_gameState", gameState);
            GreyboxStageBuilder.SetReference(gate, "_renderer", panel.GetComponent<Renderer>());
            GreyboxStageBuilder.SetReference(gate, "_normalMaterial", material);
            GreyboxStageBuilder.SetReference(gate, "_highlightMaterial", highlight);
            GreyboxStageBuilder.SetReference(gate, "_dockPoint", dockPoint);
            GreyboxStageBuilder.SetReference(gate, "_lobbySpawn", lobbySpawn);
        }

        private static void BuildElevatorPanel(int floor, Transform[] exits, TuningConfigSO tuning,
            Material material, Material highlight)
        {
            float x = FloorExitX[floor - 1];
            GameObject panel = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, "Elevator_" + floor + "F",
                new Vector3(x, 1.2f, 2.6f));
            panel.transform.localScale = new Vector3(1.6f, 2.4f, 0.3f);
            panel.GetComponent<Renderer>().sharedMaterial = material;
            BoxCollider trigger = panel.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.5f, 2f, 12f);

            ApartmentElevator elevator = panel.AddComponent<ApartmentElevator>();
            GreyboxStageBuilder.SetReference(elevator, "_tuning", tuning);
            GreyboxStageBuilder.SetReference(elevator, "_renderer", panel.GetComponent<Renderer>());
            GreyboxStageBuilder.SetReference(elevator, "_normalMaterial", material);
            GreyboxStageBuilder.SetReference(elevator, "_highlightMaterial", highlight);
            SerializedObject serialized = new SerializedObject(elevator);
            serialized.FindProperty("_floor").intValue = floor;
            SerializedProperty exitsProp = serialized.FindProperty("_floorExits");
            exitsProp.arraySize = exits.Length;
            for (int i = 0; i < exits.Length; i++)
                exitsProp.GetArrayElementAtIndex(i).objectReferenceValue = exits[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AttachSpawner(GameStateSO gameState)
        {
            GameObject go = new GameObject("__gb_CargoSpawner");
            DistrictCargoSpawner spawner = go.AddComponent<DistrictCargoSpawner>();

            Transform boxOrigin = GreyboxStageBuilder.CreateEmpty("BoxOrigin", new Vector3(-17f, 0f, -1.2f)).transform;
            // 층별 세대 비콘 앵커 — 2·3·4층 복도 안쪽.
            var anchors = new Transform[3];
            anchors[0] = GreyboxStageBuilder.CreateEmpty("BeaconAnchor2F", new Vector3(30f, 0f, 0f)).transform;
            anchors[1] = GreyboxStageBuilder.CreateEmpty("BeaconAnchor3F", new Vector3(50f, 0f, 0f)).transform;
            anchors[2] = GreyboxStageBuilder.CreateEmpty("BeaconAnchor4F", new Vector3(70f, 0f, 0f)).transform;

            SerializedObject serialized = new SerializedObject(spawner);
            serialized.FindProperty("_gameState").objectReferenceValue = gameState;
            serialized.FindProperty("_boxVisualPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Auto/prop_box_parcel.prefab");
            serialized.FindProperty("_beaconPrefab").objectReferenceValue = GreyboxStageBuilder.GetOrCreateBeaconPrefab();
            serialized.FindProperty("_boxHighlight").objectReferenceValue =
                GreyboxStageBuilder.GetOrCreateMaterial("Highlight", GreyboxStageBuilder.ParseColor("#35e0c8"), true);
            serialized.FindProperty("_boxFallback").objectReferenceValue =
                GreyboxStageBuilder.GetOrCreateMaterial("Box", GreyboxStageBuilder.ParseColor("#ff9f45"), false);
            serialized.FindProperty("_tuning").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TuningConfigSO>("Assets/Data/Tuning.asset");
            serialized.FindProperty("_boxOrigin").objectReferenceValue = boxOrigin;
            SerializedProperty anchorsProp = serialized.FindProperty("_floorBeaconAnchors");
            anchorsProp.arraySize = anchors.Length;
            for (int i = 0; i < anchors.Length; i++)
                anchorsProp.GetArrayElementAtIndex(i).objectReferenceValue = anchors[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildApartmentCanvas()
        {
            GameObject canvasGo = new GameObject("__gb_ApartmentCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60; // 폰(그 아래)·대화(90)의 사이
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            ApartmentUIView view = canvasGo.AddComponent<ApartmentUIView>();
            GreyboxStageBuilder.SetReference(view, "_font",
                AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Art/UI/Fonts/Pretendard-Regular SDF.asset"));
        }
    }
}
