using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// Hillside.unity (S-049 · D-064) — 언덕주택가: 계단식 테라스 4단(옹벽+램프) 그레이박스.
    /// 단마다 y+2 — 램프(경사 큐브)로 오른다. 세대 비콘은 2~4단. 카메라 Y 팔로우.
    /// 비 오는 날 미끄럼·스태미나 가중은 플레이어 도메인(S-049 메커닉)이 처리. 멱등(__gb_ Clear).
    /// </summary>
    public static class HillsideStageBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/Hillside.unity";
        private const float TERRACE_H = 2f;

        // 단: (시작x, 끝x, y) — 1단은 입구 평지.
        private static readonly (float fromX, float toX, float y)[] Terraces =
        {
            (-20f, -2f, 0f),
            (-2f, 14f, TERRACE_H),
            (14f, 30f, TERRACE_H * 2f),
            (30f, 46f, TERRACE_H * 3f),
        };

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

            var (gameState, tuning, _) = GreyboxStageBuilder.GetOrCreateStageData();

            Material ground = GreyboxStageBuilder.GetOrCreateMaterial("Ground", new Color(0.24f, 0.24f, 0.26f), false);
            Material terrace = GreyboxStageBuilder.GetOrCreateMaterial("HillTerrace", new Color(0.34f, 0.31f, 0.27f), false);
            Material retaining = GreyboxStageBuilder.GetOrCreateMaterial("HillWall", new Color(0.45f, 0.42f, 0.38f), false);
            Material ramp = GreyboxStageBuilder.GetOrCreateMaterial("HillRamp", new Color(0.40f, 0.36f, 0.31f), false);
            Material house = GreyboxStageBuilder.GetOrCreateMaterial("HillHouse", new Color(0.52f, 0.44f, 0.40f), false);

            // ── 테라스·옹벽·램프 ─────────────────────────────
            foreach (var t in Terraces)
            {
                float width = t.toX - t.fromX;
                GameObject slab = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube,
                    "Terrace_y" + t.y, new Vector3(t.fromX + width * 0.5f, t.y - 0.1f, 0f));
                slab.transform.localScale = new Vector3(width, 0.2f, 6f);
                slab.GetComponent<Renderer>().sharedMaterial = t.y < 0.1f ? ground : terrace;

                if (t.y > 0.1f)
                {
                    // 옹벽 — 단 전면(왼쪽 모서리), 램프 개구부(z -1.2..1.2) 제외 양옆.
                    BuildWall("Retain_y" + t.y + "_L", new Vector3(t.fromX, t.y - TERRACE_H * 0.5f, -2.1f),
                        new Vector3(0.3f, TERRACE_H, 1.8f), retaining);
                    BuildWall("Retain_y" + t.y + "_R", new Vector3(t.fromX, t.y - TERRACE_H * 0.5f, 2.1f),
                        new Vector3(0.3f, TERRACE_H, 1.8f), retaining);
                    // 램프 — 경사 큐브 (아래 단에서 이 단으로).
                    float rampLength = 5f;
                    GameObject rampGo = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube,
                        "Ramp_y" + t.y, new Vector3(t.fromX - rampLength * 0.45f, t.y - TERRACE_H * 0.5f - 0.05f, 0f));
                    float angle = Mathf.Atan2(TERRACE_H, rampLength) * Mathf.Rad2Deg;
                    rampGo.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                    rampGo.transform.localScale = new Vector3(Mathf.Sqrt(rampLength * rampLength + TERRACE_H * TERRACE_H), 0.2f, 2.4f);
                    rampGo.GetComponent<Renderer>().sharedMaterial = ramp;
                }
            }

            // 그레이박스 집 실루엣 (단 뒤편) — 비콘 자리 시각 앵커.
            BuildWall("House_2", new Vector3(7f, TERRACE_H + 1.5f, 2.4f), new Vector3(5f, 3f, 1.2f), house);
            BuildWall("House_3", new Vector3(23f, TERRACE_H * 2f + 1.5f, 2.4f), new Vector3(5f, 3f, 1.2f), house);
            BuildWall("House_4", new Vector3(39f, TERRACE_H * 3f + 1.5f, 2.4f), new Vector3(5f, 3f, 1.2f), house);

            // ── 걷기 볼륨 ────────────────────────────────────
            GameObject volume = GreyboxStageBuilder.CreateEmpty("Walkable", Vector3.zero);
            BoxCollider walkable = volume.AddComponent<BoxCollider>();
            walkable.isTrigger = true;
            walkable.size = new Vector3(70f, TERRACE_H * 4f + 6f, 6f);
            walkable.center = new Vector3(13f, TERRACE_H * 2f, 0f);
            volume.AddComponent<WalkableVolume>();

            // ── 스포너 (단별 앵커 — floor 2·3·4 = 2·3·4단) ──
            AttachSpawner(gameState);

            // ── 플레이어·카메라(Y 팔로우) ────────────────────
            GreyboxStageBuilder.BuildPlayer(gameState, tuning);
            GameObject player = GameObject.Find("__gb_Player");
            if (player != null) player.transform.position = new Vector3(-16f, 0.1f, 0f);
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
            Debug.Log("[Hillside] 테라스 4단 무대 조립 완료 — 램프·옹벽 (S-049).");
        }

        private static void BuildWall(string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject wall = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, name, position);
            wall.transform.localScale = size;
            wall.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void AttachSpawner(GameStateSO gameState)
        {
            GameObject go = new GameObject("__gb_CargoSpawner");
            DistrictCargoSpawner spawner = go.AddComponent<DistrictCargoSpawner>();

            Transform boxOrigin = GreyboxStageBuilder.CreateEmpty("BoxOrigin", new Vector3(-17f, 0f, -1.2f)).transform;
            var anchors = new Transform[3]; // floor 2→2단, 3→3단, 4→4단
            anchors[0] = GreyboxStageBuilder.CreateEmpty("BeaconAnchor_T2", new Vector3(6f, TERRACE_H, 0f)).transform;
            anchors[1] = GreyboxStageBuilder.CreateEmpty("BeaconAnchor_T3", new Vector3(22f, TERRACE_H * 2f, 0f)).transform;
            anchors[2] = GreyboxStageBuilder.CreateEmpty("BeaconAnchor_T4", new Vector3(38f, TERRACE_H * 3f, 0f)).transform;

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
    }
}
