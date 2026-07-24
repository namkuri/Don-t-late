using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// Hillside.unity (S-049 → S-051 달동네 개편 · D-064) — 언덕주택가:
    /// 저지대(포장·현대 건물) → 스위치백 비포장 등반로 3굽이(스플라인 조각 근사·옹벽 채움)
    /// → 달동네 정상부(판잣집). 긴 계단 2개 = 지름길(콜라이더는 램프, 비주얼은 계단).
    /// 픽셀화 렌더가 조각 이음새를 뭉개 곡선으로 읽힌다. 멱등(__gb_ Clear).
    /// </summary>
    public static class HillsideStageBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/Hillside.unity";
        private const float ROAD_WIDTH = 2.2f;   // 등반로 폭
        private const float ROAD_THICK = 0.25f;
        private const float WEAVE_AMP = 0.6f;    // Z 굽이 진폭 — 곡선 비포장길 감

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

            var (gameState, tuning, _) = GreyboxStageBuilder.GetOrCreateStageData();

            // ── 고도 밴드 머티리얼 (조닝: 저지대=포장 / 등반로·정상=비포장) ──
            Material asphalt = GreyboxStageBuilder.GetOrCreateMaterial("HillAsphalt", new Color(0.23f, 0.24f, 0.26f), false);
            Material curb = GreyboxStageBuilder.GetOrCreateMaterial("HillCurb", new Color(0.70f, 0.70f, 0.70f), false);
            _dirtMat = GreyboxStageBuilder.GetOrCreateMaterial("HillDirt", new Color(0.43f, 0.35f, 0.26f), false);
            _wallMat = GreyboxStageBuilder.GetOrCreateMaterial("HillWall", new Color(0.48f, 0.42f, 0.33f), false);
            Material stair = GreyboxStageBuilder.GetOrCreateMaterial("HillStair", new Color(0.60f, 0.58f, 0.54f), false);
            Material modern = GreyboxStageBuilder.GetOrCreateMaterial("HillModern", new Color(0.55f, 0.58f, 0.63f), false);
            Material moonHouse = GreyboxStageBuilder.GetOrCreateMaterial("HillMoonHouse", new Color(0.66f, 0.51f, 0.42f), false);
            Material slate = GreyboxStageBuilder.GetOrCreateMaterial("HillSlate", new Color(0.29f, 0.31f, 0.33f), false);

            // ── 저지대 (y0 · 포장 + 연석 + 현대 건물) ───────────
            BuildBox("PavedRoad", new Vector3(-5f, -0.1f, 0f), new Vector3(30f, 0.2f, 6f), asphalt);
            BuildBox("Curb", new Vector3(-5f, 0.04f, -2.7f), new Vector3(30f, 0.12f, 0.3f), curb);
            BuildBox("Modern_A", new Vector3(-15f, 2.5f, 2.9f), new Vector3(5f, 5f, 2f), modern);
            BuildBox("Modern_B", new Vector3(-8f, 3.5f, 2.9f), new Vector3(5f, 7f, 2f), modern);
            BuildBox("Modern_C", new Vector3(-1f, 3f, 2.9f), new Vector3(5f, 6f, 2f), modern);

            // 언덕 기슭 바닥 (비포장) — 등반로·계단 하단이 딛는 지면.
            BuildBox("HillBaseGround", new Vector3(28f, -0.1f, 0f), new Vector3(36f, 0.2f, 6f), _dirtMat);

            // ── 스위치백 등반로 3굽이 (스플라인 조각 근사 + 옹벽 채움) ──
            // Z 레인 계단식 후퇴(S-051 판정): Leg1=-1.6(카메라 앞) → Leg2=0 → Leg3=+1.4 —
            // 위 굽이의 옹벽 덩어리가 아래 길 "뒤"에 서서 무대 배경막처럼 겹겹이 보인다.
            BuildRibbon("Leg1", new Vector3(10f, 0.2f, -1.6f), new Vector3(36f, 3.3f, -1.6f), 14);
            BuildBox("TurnPad_R", new Vector3(37.5f, 3.3f, -0.7f), new Vector3(4f, 0.2f, 4.6f), _dirtMat);
            BuildBox("TurnPad_R_Wall", new Vector3(37.5f, 1.65f, -0.7f), new Vector3(3.6f, 3.3f, 4.2f), _wallMat);
            BuildRibbon("Leg2", new Vector3(37f, 3.5f, 0f), new Vector3(12f, 6.5f, 0f), 14);
            BuildBox("TurnPad_L", new Vector3(11f, 6.5f, 0.4f), new Vector3(4f, 0.2f, 5.6f), _dirtMat);
            BuildBox("TurnPad_L_Wall", new Vector3(11f, 3.25f, 0.4f), new Vector3(3.6f, 6.5f, 5.2f), _wallMat);
            BuildRibbon("Leg3", new Vector3(12f, 6.7f, 1.4f), new Vector3(33f, 9.5f, 1.4f), 12);

            // ── 달동네 정상부 (판잣집 마당) ─────────────────────
            BuildBox("MoonPlateau", new Vector3(39f, 9.4f, 0f), new Vector3(14f, 0.2f, 6f), _dirtMat);
            BuildBox("MoonPlateau_Wall", new Vector3(39f, 4.7f, 0.4f), new Vector3(13.4f, 9.4f, 5.2f), _wallMat);
            BuildMoonHouse("MoonHouse_P1", new Vector3(35f, 9.5f, 2.3f), moonHouse, slate);
            BuildMoonHouse("MoonHouse_P2", new Vector3(39f, 9.5f, 2.6f), moonHouse, slate);
            BuildMoonHouse("MoonHouse_P3", new Vector3(43f, 9.5f, 2.2f), moonHouse, slate);

            // 비탈 판잣집 — 등반로 뒤편(z3)에 고도를 계단식으로 올려 실루엣 형성 (지주 채움).
            BuildPerchedHouse("MoonHouse_S1", new Vector3(17f, 1.1f, 3.1f), moonHouse, slate);
            BuildPerchedHouse("MoonHouse_S2", new Vector3(24f, 2.1f, 3.2f), moonHouse, slate);
            BuildPerchedHouse("MoonHouse_S3", new Vector3(30f, 5.4f, 3.1f), moonHouse, slate);
            BuildPerchedHouse("MoonHouse_S4", new Vector3(20f, 5.7f, 3.2f), moonHouse, slate);

            // ── 긴 계단 2개 (지름길 — 콜라이더=램프, 비주얼=계단) ──
            BuildStair("StairLong", new Vector3(19f, 0f, -2.4f), new Vector3(13f, 6.5f, -2.4f), stair);
            BuildStair("StairShort", new Vector3(26f, 4.8f, -1.2f), new Vector3(22f, 8f, -1.2f), stair);

            // ── 걷기 볼륨 (전역 — Z 낙하 자유는 달동네 몫, 낙사는 안전망) ──
            GameObject volume = GreyboxStageBuilder.CreateEmpty("Walkable", Vector3.zero);
            BoxCollider walkable = volume.AddComponent<BoxCollider>();
            walkable.isTrigger = true;
            walkable.size = new Vector3(70f, 24f, 6.4f);
            walkable.center = new Vector3(13f, 10f, 0f);
            volume.AddComponent<WalkableVolume>();

            // ── 스포너 (밴드별 앵커 — floor 2=중턱, 3=달동네 초입, 4=정상) ──
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
            Debug.Log("[Hillside] 달동네 무대 조립 완료 — 스위치백 3굽이·긴 계단 2·조닝 (S-051).");
        }

        // ── 등반로 리본: 직선 보간 + Z 사인 굽이(양끝 0 복귀), 조각 박스 + 옹벽 채움 ──
        private static Vector3 SamplePath(Vector3 from, Vector3 to, float t)
        {
            Vector3 p = Vector3.Lerp(from, to, t);
            p.z += WEAVE_AMP * Mathf.Sin(t * Mathf.PI * 2f); // 한 주기 — 양끝 z 복귀
            return p;
        }

        private static void BuildRibbon(string id, Vector3 from, Vector3 to, int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                Vector3 p0 = SamplePath(from, to, (float)i / segments);
                Vector3 p1 = SamplePath(from, to, (float)(i + 1) / segments);
                Vector3 mid = (p0 + p1) * 0.5f;
                Vector3 dir = p1 - p0;
                float length = dir.magnitude;

                GameObject seg = BuildBox(id + "_" + i, mid, new Vector3(ROAD_WIDTH, ROAD_THICK, length * 1.25f), _dirtMat);
                seg.transform.rotation = Quaternion.LookRotation(dir.normalized);

                // 옹벽 채움 — 리본 아래를 바닥까지 메워 계단식 언덕 덩어리를 만든다 (요·yaw만 회전).
                if (mid.y > 0.3f)
                {
                    Vector3 flat = new Vector3(dir.x, 0f, dir.z).normalized;
                    GameObject wall = BuildBox(id + "_w" + i,
                        new Vector3(mid.x, mid.y * 0.5f - 0.1f, mid.z),
                        new Vector3(ROAD_WIDTH * 0.9f, mid.y, length * 1.15f), _wallMat);
                    wall.transform.rotation = Quaternion.LookRotation(flat);
                }
            }
        }

        // ── 긴 계단: 충돌은 경사 박스 하나(렌더러 끔 — CC 덜컹 방지), 겉모습은 계단 큐브 ──
        private static void BuildStair(string id, Vector3 from, Vector3 to, Material material)
        {
            int steps = Mathf.CeilToInt(Mathf.Abs(to.y - from.y) / 0.38f);
            for (int i = 0; i <= steps; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, (float)i / steps);
                BuildBox(id + "_s" + i, new Vector3(p.x, p.y - 0.14f, p.z), new Vector3(0.55f, 0.28f, 1.5f), material);
            }

            Vector3 slope = to - from;
            GameObject ramp = BuildBox(id + "_ramp", (from + to) * 0.5f + Vector3.up * 0.04f,
                new Vector3(1.5f, 0.1f, slope.magnitude * 1.03f), material);
            ramp.transform.rotation = Quaternion.LookRotation(slope.normalized);
            Object.DestroyImmediate(ramp.GetComponent<MeshRenderer>());

            // 양끝 착지 슬래브 — 등반로(z 굽이)와 계단(z2.5)을 잇는 다리.
            BuildBox(id + "_padA", new Vector3(from.x, from.y - 0.1f, from.z * 0.5f), new Vector3(2.2f, 0.2f, Mathf.Abs(from.z) + 2.4f), material);
            BuildBox(id + "_padB", new Vector3(to.x, to.y - 0.1f, to.z * 0.5f), new Vector3(2.2f, 0.2f, Mathf.Abs(to.z) + 2.4f), material);
        }

        // ── 판잣집: 몸통 + 슬레이트 지붕(살짝 기울임) ──
        private static void BuildMoonHouse(string id, Vector3 basePos, Material body, Material roof)
        {
            BuildBox(id, basePos + Vector3.up * 1f, new Vector3(2.6f, 2f, 1.8f), body);
            GameObject roofGo = BuildBox(id + "_roof", basePos + new Vector3(0f, 2.12f, 0f), new Vector3(3f, 0.18f, 2.1f), roof);
            roofGo.transform.rotation = Quaternion.Euler(0f, 0f, 6f);
        }

        // 비탈에 걸친 판잣집 — 지주(바닥까지)를 받쳐 언덕 실루엣을 만든다.
        private static void BuildPerchedHouse(string id, Vector3 basePos, Material body, Material roof)
        {
            BuildMoonHouse(id, basePos, body, roof);
            if (basePos.y > 0.3f)
                BuildBox(id + "_stilt", new Vector3(basePos.x, basePos.y * 0.5f, basePos.z),
                    new Vector3(2.2f, basePos.y, 1.5f), _wallMat);
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

            Transform boxOrigin = GreyboxStageBuilder.CreateEmpty("BoxOrigin", new Vector3(-17f, 0f, -1.2f)).transform;
            var anchors = new Transform[3]; // floor 2=중턱 턴패드, 3=달동네 초입, 4=정상 마당
            anchors[0] = GreyboxStageBuilder.CreateEmpty("BeaconAnchor_Mid", new Vector3(36.5f, 3.5f, -0.7f)).transform;
            anchors[1] = GreyboxStageBuilder.CreateEmpty("BeaconAnchor_Moon", new Vector3(11f, 6.7f, 0.4f)).transform;
            anchors[2] = GreyboxStageBuilder.CreateEmpty("BeaconAnchor_Top", new Vector3(38f, 9.6f, 0f)).transform;

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
