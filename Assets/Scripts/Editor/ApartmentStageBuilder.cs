using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// Apartment.unity (S-038 → S-048 ③ 수직 재구조). 마당(-20..-2) → 자동문 → 건물(x -1..22)
    /// **수직 4층**(층고 4u: y 0/4/8/12) — 각 층 복도 슬래브, 맨 오른쪽(x 18.5..21.5) 엘베 샤프트에
    /// **실물리 캐빈**이 오르내린다(사람·대차 탑승). 카메라는 Y 팔로우. 멱등(__gb_ Clear).
    /// </summary>
    public static class ApartmentStageBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/Apartment.unity";
        private const float FLOOR_H = 4f;
        private const int FLOORS = 4;
        private const float SHAFT_X = 20f;      // 샤프트 중심
        private const float SLAB_RIGHT = 18.4f; // 슬래브는 샤프트 앞에서 끝난다

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
            Material doorMat = GreyboxStageBuilder.GetOrCreateMaterial("AptDoor", new Color(0.35f, 0.62f, 0.58f), false);
            Material cabinMat = GreyboxStageBuilder.GetOrCreateMaterial("AptCabin", new Color(0.55f, 0.50f, 0.35f), false);
            Material highlight = GreyboxStageBuilder.GetOrCreateMaterial("Highlight", GreyboxStageBuilder.ParseColor("#35e0c8"), true);
            Material dockMat = GreyboxStageBuilder.GetOrCreateMaterial("AptDock", GreyboxStageBuilder.ParseColor("#ff9f45"), true);

            // ── 지면·층 슬래브 ───────────────────────────────
            BuildBox("YardGround", new Vector3(-10.5f, -0.05f, 0f), new Vector3(19f, 0.1f, 6f), ground);
            BuildBox("LobbyGround", new Vector3(10.25f, -0.05f, 0f), new Vector3(23.5f, 0.1f, 6f), lobby);
            for (int floor = 2; floor <= FLOORS; floor++)
            {
                float y = (floor - 1) * FLOOR_H;
                BuildBox("Slab_" + floor + "F", new Vector3((SLAB_RIGHT - 1f) * 0.5f, y - 0.15f, 0f),
                    new Vector3(SLAB_RIGHT + 1f, 0.3f, 6f), corridor);
            }
            BuildBox("Roof", new Vector3(10.25f, FLOORS * FLOOR_H - 0.15f, 0f), new Vector3(23.5f, 0.3f, 6f), wall);

            // 뒷벽(전 층) + 좌우 외벽 + 샤프트 뒷벽
            BuildBox("BackWall", new Vector3(10.25f, FLOORS * FLOOR_H * 0.5f, 3.1f),
                new Vector3(23.5f, FLOORS * FLOOR_H, 0.2f), wall);
            BuildBox("RightWall", new Vector3(21.9f, FLOORS * FLOOR_H * 0.5f, 0f),
                new Vector3(0.2f, FLOORS * FLOOR_H, 6f), wall);
            // 정면 외벽(1층 마당 쪽) — 문 개구부(x -1.5±1.1) 제외 상단부
            BuildBox("FrontWallUpper", new Vector3(-1.4f, (FLOOR_H + FLOORS * FLOOR_H) * 0.5f + 0.5f, 0f),
                new Vector3(0.25f, (FLOORS - 1) * FLOOR_H + 1f, 6f), wall);
            BuildBox("FrontWallLeft", new Vector3(-1.4f, FLOOR_H * 0.5f, -2.25f), new Vector3(0.25f, FLOOR_H, 1.5f), wall);
            BuildBox("FrontWallRight", new Vector3(-1.4f, FLOOR_H * 0.5f, 2.25f), new Vector3(0.25f, FLOOR_H, 1.5f), wall);

            // ── 걷기 볼륨 (수직 전체) ────────────────────────
            GameObject volume = GreyboxStageBuilder.CreateEmpty("Walkable", Vector3.zero);
            BoxCollider walkable = volume.AddComponent<BoxCollider>();
            walkable.isTrigger = true;
            walkable.size = new Vector3(50f, FLOORS * FLOOR_H + 4f, 6f);
            walkable.center = new Vector3(1f, (FLOORS * FLOOR_H) * 0.5f, 0f);
            volume.AddComponent<WalkableVolume>();

            // ── 자동문 + 비번 게이트 + 도크 ──────────────────
            ApartmentSlidingDoor door = BuildSlidingDoor(doorMat);
            BuildPasswordGate(gameState, panelMat, highlight, door);
            GameObject dock = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, "CartDockPad", new Vector3(-4.5f, 0.03f, 0f));
            Object.DestroyImmediate(dock.GetComponent<Collider>());
            dock.transform.localScale = new Vector3(2.4f, 0.06f, 2.4f);
            dock.GetComponent<Renderer>().sharedMaterial = dockMat;

            // ── 대차 ─────────────────────────────────────────
            GreyboxStageBuilder.BuildDeliveryCart(new Vector3(-9f, 0f, 0f));

            // ── 실물리 엘리베이터 (캐빈 + 층 호출 패널) ──────
            BuildElevator(tuning, cabinMat, panelMat, highlight);

            // ── 세대 비콘 스포너 (층별 y 앵커) ───────────────
            AttachSpawner(gameState);

            // ── 플레이어·카메라(Y 팔로우) ────────────────────
            // S-052 ②③ — 마당 행인 2 + 심부름 할아버지 (마당 안 짐 옮기기).
            NpcBuildKit.BuildPedestrian("Walker_A", new Vector3(-16f, 0f, 2.0f), new Color(0.45f, 0.52f, 0.62f), 4f);
            NpcBuildKit.BuildPedestrian("Walker_B", new Vector3(-8f, 0f, 2.4f), new Color(0.60f, 0.48f, 0.40f), 5f);
            NpcBuildKit.BuildErrandNpc("ErrandGrandpa", "할아버지", new Vector3(-16f, 0f, -1.8f),
                new Vector3(-4f, 0f, 1.8f), gameState, 1200);

            GreyboxStageBuilder.BuildPlayer(gameState, tuning);
            GameObject player = GameObject.Find("__gb_Player");
            if (player != null) player.transform.position = new Vector3(-16f, 0.1f, 0f);
            GreyboxStageBuilder.BuildPostVolume();
            GreyboxStageBuilder.ConfigureCamera();
            GreyboxStageBuilder.AttachCameraFollow();
            Camera camera = Camera.main;
            if (camera != null && camera.TryGetComponent(out CameraFollowX follow))
            {
                SerializedObject serialized = new SerializedObject(follow);
                serialized.FindProperty("_followY").boolValue = true; // S-048 — 층 추종
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            BuildApartmentCanvas();

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log("[Apartment] 수직 4층 무대 조립 완료 — 자동문·실물리 엘베 (S-048).");
        }

        private static GameObject BuildBox(string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject box = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, name, position);
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        private static ApartmentSlidingDoor BuildSlidingDoor(Material doorMat)
        {
            GameObject root = GreyboxStageBuilder.CreateEmpty("SlidingDoor", new Vector3(-1.4f, 0f, 0f));

            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Panel";
            panel.transform.SetParent(root.transform, false);
            panel.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            panel.transform.localScale = new Vector3(0.18f, 2.2f, 1.6f); // 개구부(z ±0.8) 커버 — 실콜라이더
            panel.GetComponent<Renderer>().sharedMaterial = doorMat;

            // 모션 센서 존 — 문 앞뒤.
            BoxCollider sensor = root.AddComponent<BoxCollider>();
            sensor.isTrigger = true;
            sensor.center = new Vector3(0f, 1f, 0f);
            sensor.size = new Vector3(5f, 2.5f, 4f);

            ApartmentSlidingDoor door = root.AddComponent<ApartmentSlidingDoor>();
            GreyboxStageBuilder.SetReference(door, "_panel", panel.transform);
            return door;
        }

        private static void BuildPasswordGate(GameStateSO gameState, Material material, Material highlight,
            ApartmentSlidingDoor door)
        {
            GameObject panel = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, "PasswordGate", new Vector3(-2.2f, 1.2f, -1.6f));
            panel.transform.localScale = new Vector3(0.25f, 0.5f, 0.4f);
            panel.GetComponent<Renderer>().sharedMaterial = material;
            BoxCollider trigger = panel.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(8f, 5f, 8f);

            ApartmentPasswordGate gate = panel.AddComponent<ApartmentPasswordGate>();
            GreyboxStageBuilder.SetReference(gate, "_gameState", gameState);
            GreyboxStageBuilder.SetReference(gate, "_renderer", panel.GetComponent<Renderer>());
            GreyboxStageBuilder.SetReference(gate, "_normalMaterial", material);
            GreyboxStageBuilder.SetReference(gate, "_highlightMaterial", highlight);
            GreyboxStageBuilder.SetReference(gate, "_door", door);
        }

        private static void BuildElevator(TuningConfigSO tuning, Material cabinMat, Material panelMat, Material highlight)
        {
            // 캐빈 — 1층에서 시작. S-050 ③: 루트 Y90 회전 — 개구(로컬 -Z)가 복도(-X)를 향하고,
            // 카메라 쪽(-Z)이 되는 로컬 +X 벽(구 Right)은 만들지 않아 카메라가 내부를 본다.
            GameObject cabin = GreyboxStageBuilder.CreateEmpty("ElevatorCabin", new Vector3(SHAFT_X, 0f, 0f));
            cabin.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            BuildCabinPart(cabin, "Floor", new Vector3(0f, 0.1f, 0f), new Vector3(3f, 0.2f, 3f), cabinMat);
            BuildCabinPart(cabin, "Back", new Vector3(0f, 1.6f, 1.4f), new Vector3(3f, 3f, 0.2f), cabinMat);  // 월드 +X — 샤프트 안벽
            BuildCabinPart(cabin, "Left", new Vector3(-1.4f, 1.6f, 0f), new Vector3(0.2f, 3f, 3f), cabinMat); // 월드 +Z — 카메라 반대편

            ApartmentElevator elevator = cabin.AddComponent<ApartmentElevator>();
            GreyboxStageBuilder.SetReference(elevator, "_tuning", tuning);
            GreyboxStageBuilder.SetReference(elevator, "_cabin", cabin.transform);
            SerializedObject serialized = new SerializedObject(elevator);
            SerializedProperty ys = serialized.FindProperty("_floorYs");
            ys.arraySize = FLOORS;
            for (int i = 0; i < FLOORS; i++) ys.GetArrayElementAtIndex(i).floatValue = i * FLOOR_H;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // 캐빈 내부 패널 (층 선택) — Y90 회전 후 로컬 -X 벽(월드 -Z 개방부 아님, 월드 +Z의 Left 벽) 안쪽.
            BuildPanel(cabin.transform, "CabinPanel", new Vector3(-1.15f, 1.4f, 0.9f), elevator, 0, true, panelMat, highlight);

            // 층 호출 패널 — 샤프트 왼쪽 벽면.
            for (int floor = 1; floor <= FLOORS; floor++)
                BuildPanel(null, "CallPanel_" + floor + "F",
                    new Vector3(SHAFT_X - 2.2f, (floor - 1) * FLOOR_H + 1.3f, 2.6f), elevator, floor, false, panelMat, highlight);
        }

        private static void BuildCabinPart(GameObject cabin, string name, Vector3 localPos, Vector3 size, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(cabin.transform, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void BuildPanel(Transform parent, string name, Vector3 position,
            ApartmentElevator elevator, int floor, bool isCabinPanel, Material material, Material highlight)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = parent == null ? "__gb_" + name : name; // 루트면 멱등 Clear 대상 접두어
            if (parent != null) { panel.transform.SetParent(parent, false); panel.transform.localPosition = position; }
            else panel.transform.position = position;
            panel.transform.localScale = new Vector3(0.35f, 0.5f, 0.12f);
            panel.GetComponent<Renderer>().sharedMaterial = material;
            BoxCollider trigger = panel.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(6f, 5f, 16f);

            ApartmentElevatorPanel component = panel.AddComponent<ApartmentElevatorPanel>();
            GreyboxStageBuilder.SetReference(component, "_elevator", elevator);
            GreyboxStageBuilder.SetReference(component, "_renderer", panel.GetComponent<Renderer>());
            GreyboxStageBuilder.SetReference(component, "_normalMaterial", material);
            GreyboxStageBuilder.SetReference(component, "_highlightMaterial", highlight);
            SerializedObject serialized = new SerializedObject(component);
            serialized.FindProperty("_floor").intValue = floor;
            serialized.FindProperty("_isCabinPanel").boolValue = isCabinPanel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AttachSpawner(GameStateSO gameState)
        {
            GameObject go = new GameObject("__gb_CargoSpawner");
            DistrictCargoSpawner spawner = go.AddComponent<DistrictCargoSpawner>();

            Transform boxOrigin = GreyboxStageBuilder.CreateEmpty("BoxOrigin", new Vector3(-17f, 0f, -1.2f)).transform;
            var anchors = new Transform[3]; // 2·3·4층 복도
            for (int i = 0; i < 3; i++)
                anchors[i] = GreyboxStageBuilder.CreateEmpty("BeaconAnchor" + (i + 2) + "F",
                    new Vector3(4f, (i + 1) * FLOOR_H, 0f)).transform;

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
            canvas.sortingOrder = 60;
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
