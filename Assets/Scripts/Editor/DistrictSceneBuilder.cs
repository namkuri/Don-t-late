using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// District.unity에 코어루프 무대를 조립하는 개발 도구.
    /// 매니저(WorldDelivery·Deadline·DayNight·SceneFlow)는 <b>Core 씬 상주</b>이므로 여기서 만들지 않는다.
    /// GreyboxStageBuilder.BuildStageContent를 재사용해 무대만 깔고, 조립용 슬롯 마커를 배치한다.
    /// 다시 실행하면 이전 조립물(__gb_ 루트 + Slots)을 지우고 새로 만든다(멱등).
    /// </summary>
    public static class DistrictSceneBuilder
    {
        private const string DISTRICT_PATH = "Assets/Scenes/Village.unity"; // S-186 ③ — District 은퇴, 빌라촌이 승계
        private static string[] _buildingWhitelist;
        private const string SLOTS_ROOT = "Slots";

        // 슬롯 규약(발주): 건물 16칸 X간격 6u·길 안쪽 Z=2.6 / 소품 10칸 보도변 Z=-2.6.
        // S-116 ③ — 12칸·8u → 16칸·6u: 보행 구간 건물 공백을 오밀조밀하게 (남규님 실관찰).
        private const int BUILDING_SLOTS = 16;
        private const int PROP_SLOTS = 10;
        private const float SLOT_SPACING = 6f;
        private const float PROP_SLOT_SPACING = 8f; // 소품 밀도는 유지 (S-114 규약)
        private const float BUILDING_Z = 2.6f;
        private const float PROP_Z = -2.6f;

        /// <summary>
        /// S-186 ③ — 빌라촌 건물 풀. 주거 위주(빌라·주택·아파트)로 좁혀 먹자골목과 갈라놓는다.
        /// 종전엔 풀 전량을 써서 두 구역이 같은 재료로 지어졌다.
        /// </summary>
        private static readonly string[] VillaBuildings =
        {
            "blue_house", "blue_narroow_house", "mint_house", "white_brown_house",
            "white_korea_house", "pink_korea_house", "pink_korea_house_2",
            "old_korea_house", "retro_korean_house", "red_korean_house",
            "Cream_home_unity", "Laundry_Home_unity", "residence",
            "black_modern_house", "black_modern_residence", "old_apartment",
        };

        [MenuItem("DontLate/Build/Village Stage (빌라촌)", priority = 14)]
        public static void BuildDistrictStage()
        {
            EnsureSceneFile(DISTRICT_PATH);
            BuildStage(DISTRICT_PATH, VillaBuildings);

            // S-215 — 상주 NPC 3인은 빌라촌 전용이라 `BuildStage` 밖에서 얹는다
            // (같은 조립을 먹자골목·촬영용 District 1도 쓰므로 안에 넣으면 거기까지 따라간다).
            Scene village = EditorSceneManager.GetSceneByPath(DISTRICT_PATH);
            VillageCastBuilder.Build(village);
            EditorSceneManager.SaveScene(village, DISTRICT_PATH);
        }

        /// <summary>씬 파일이 없으면 만든다 — `BuildStage`는 OpenScene으로 시작한다.</summary>
        internal static void EnsureSceneFile(string path)
        {
            if (System.IO.File.Exists(path)) return;
            Scene fresh = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(fresh, path);
            Debug.Log("[씬] 신규 생성 — " + path);
        }

        // S-116 ⑤ — 촬영용 District 1도 같은 조립을 재사용한다 (District1SceneBuilder가 호출).
        // S-186 ③ — 구역마다 다른 거리를 만들려면 **건물 풀**이 달라야 한다.
        // 화이트리스트를 넘기면 그 이름들만 쓰고, null이면 종전대로 Art/Buildings 전량을 쓴다.
        // S-192 — 구역별 배경 세트. null이면 District 공용 세트(빌라촌·Main 촬영용).
        private static ArtBackdropKit.SetPlacement? _backdrop;

        internal static void BuildStage(string scenePath, string[] buildingWhitelist = null,
            ArtBackdropKit.SetPlacement? backdrop = null)
        {
            _buildingWhitelist = buildingWhitelist;
            _backdrop = backdrop;
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 멱등: 이전 조립물 정리.
            GreyboxStageBuilder.Clear();
            DestroyRoot(SLOTS_ROOT);
            DestroyLegacyCenterLines(); // S-150 — 접두어 없이 저장돼 Clear()가 못 지우던 구버전 잔재

            EnsureCamera();
            // Directional Light는 만들지 않는다 — 태양은 Core 소유(D-021). 이중 광원 방지.

            var (gameState, tuning, order) = GreyboxStageBuilder.GetOrCreateStageData();
            // 매니저·세션 리셋 제외 — District엔 무대만. 상주 매니저(Core)가 처리한다.
            GreyboxStageBuilder.BuildStageContent(gameState, tuning, order);

            // S-074 ⑨ — 교차 도로가 x=0으로 오면서 기본 스폰(0,0,0)이 차도 정중앙이 됐다:
            // 입장 즉시 교통사고 나던 것(실측 재현) — 스폰을 보도(x=-6)로 옮긴다.
            GameObject playerGo = GameObject.Find("__gb_Player");
            if (playerGo != null) playerGo.transform.position = new Vector3(-6f, 0.1f, 0f);

            (GameObject slotsRoot, List<Transform> buildingSlots, List<Transform> propSlots) = BuildSlots();

            // S-143 — **건물 슬롯을 비워 넘긴다.** S-141로 민지님 세트가 거리 정본이 되면서
            // 건물 층이 둘이 됐다: 절차적 슬롯 건물은 전면 z=3.0에서 뒤로 서고(BUILDING_FRONT_Z),
            // 세트는 z −0.95~38.95를 차지해 **z 3~15가 통째로 겹친다**(남규님 실관찰 — 건물이
            // 서로 뚫고 도로까지 나옴). 거리를 두 겹으로 지을 이유가 없으므로 건물은 세트가 맡고,
            // 슬롯 생성기는 소품·가로수만 담당한다.
            // ⚠ 대가: 구역별(빌라촌·상가·주택가) 건물 다양성이 사라진다 — 소품만 구역색을 낸다.
            //   구역별 거리를 되살리려면 세트를 구역 수만큼 만들어 교체하는 쪽이 정도다.
            // S-214 ① — **빌라촌은 생성기를 달지 않는다**(남규님 지시 — 아트가 수동 배치 후 반입).
            // 슬롯 마커는 그대로 남긴다: 생성기만 빠지면 런타임에 `GeneratedLayout`(절차적 소품·
            // 가로수)이 아예 생기지 않고, 나중에 되살릴 때 슬롯을 다시 깔 필요가 없다.
            // 다른 구역(먹자골목·촬영용 District 1)은 종전대로 — 지시는 빌라촌 한정이다.
            if (scenePath != DISTRICT_PATH)
                AttachLayoutGenerator(slotsRoot, new List<Transform>(), propSlots, gameState);
            else
                Debug.Log("[DistrictSceneBuilder] 빌라촌 — 절차적 배치 생성기 생략 (S-214 ①, 아트 수동 배치 예정).");

            // S-141 — 민지님 세트 프리팹(`set_district_2`)을 배경 파사드로 깐다.
            // 절차적 슬롯을 대체하지 않는다: 슬롯은 Z=2.6 근경에서 구역별로 채워지고,
            // 이건 Z=4~36 원경에 고정으로 깔리는 배경층이다. 프리팹 링크를 유지하므로
            // 민지님이 프리팹을 고치면 코드 수정 없이 반영된다.
            // S-192 — 구역마다 제 세트를 꽂는다(호출자가 지정). 종전엔 District 하나를 공유해
            // 먹자골목에서 담으면 빌라촌까지 바뀌었다.
            ArtBackdropKit.Build(_backdrop ?? ArtBackdropKit.District);

            // S-015: 정적 짐·비콘 제거 — 도착 시 cargo 실데이터로 스폰(DistrictCargoSpawner)한다.
            DestroyRoot("__gb_Box");
            DestroyRoot("__gb_Beacon");
            AttachCargoSpawner(gameState);

            // S-052 ③ — 심부름 할머니 (길 건너까지 짐 옮기기). 행인 3명은 신호등 생성 뒤에 (S-076 ② 주입).
            NpcBuildKit.BuildErrandNpc("ErrandGranny", "할머니", new Vector3(12f, 0f, -1.8f),
                new Vector3(-6f, 0f, -1.8f), gameState, 1500);

            // S-057 — 교차(Z) 골목 도로: 진행축과 직각으로 차가 관통한다. 보고 건너라.
            // S-074 ⑨ — 도로를 슬롯 사이(x=0, 건물 슬롯 ±4 스킵됨)로 이설 + 횡단보도 + 신호등.
            const float ROAD_X = 0f;
            Material road = GreyboxStageBuilder.GetOrCreateMaterial("CrossRoad", new Color(0.16f, 0.17f, 0.19f), false);
            GameObject crossRoad = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, "CrossRoad", new Vector3(ROAD_X, 0.01f, 0f));
            Object.DestroyImmediate(crossRoad.GetComponent<Collider>());
            // S-150 — 지면 Z 전폭까지 깐다. 종전 20u는 지면(80u)의 1/4이라 z ±10에서 도로가
            // 끊기고 흙바닥이 드러났다(남규님 지적). 지면 치수에서 역산해 하드코딩을 피한다.
            crossRoad.transform.localScale = new Vector3(4.2f, 0.02f, GroundDepth());
            crossRoad.GetComponent<Renderer>().sharedMaterial = road;

            // 횡단보도 — S-079 ②: 줄을 z(도로 진행) 방향 길쭉·x 나열로 90도 회전(남규님 판정),
            // y도 도로면 위로 올려 z-fighting 묻힘 해소.
            Material zebra = GreyboxStageBuilder.GetOrCreateMaterial("Crosswalk", new Color(0.92f, 0.93f, 0.95f), false);
            GameObject zebraRoot = GreyboxStageBuilder.CreateEmpty("Crosswalk", new Vector3(ROAD_X, 0f, 0f));
            for (int stripe = 0; stripe < 5; stripe++)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Stripe_" + stripe;
                line.transform.SetParent(zebraRoot.transform, false);
                line.transform.localPosition = new Vector3(-1.6f + stripe * 0.8f, 0.045f, 0f);
                line.transform.localScale = new Vector3(0.42f, 0.015f, 5.6f);
                Object.DestroyImmediate(line.GetComponent<Collider>());
                line.GetComponent<Renderer>().sharedMaterial = zebra;
            }

            // S-124 — 거리 자판기: 캠프에만 있던 자판기를 District에도 손배치한다(남규님 관찰
            // "자판기 인터랙션 안 됨" — 프랍 풀의 자판기는 배경 데코라 상호작용이 없었다).
            // 프랍 추첨에 맡기지 않는 이유: 뽑히는 슬롯·방향이 시드마다 달라 "항상 쓸 수 있다"를 보장 못한다.
            BuildStreetVending(new Vector3(-8.5f, 0f, -2.2f), tuning);

            // S-123 ① — 횡단보도 앞 독백: 차도를 건너기 직전 스스로 조심하라고 되뇐다.
            AttachRemarkSpot(zebraRoot, 3.2f, new[]
            {
                "차 조심해야겠다.", "신호 보고 건너자.", "여기서 치이면 하루가 끝난다.",
            });

            // S-076 ③ — 중앙선(황색): 방향별 1차선 시각 표지. 횡단보도 구간은 비운다.
            // S-150 — 도로가 지면 전폭으로 길어졌으므로 통짜 2개 → **전 구간 점선**으로 바꾼다.
            // ⚠ 루트를 `__gb_` 아래 둔다: 종전엔 접두어 없는 최상위 오브젝트라
            // `GreyboxStageBuilder.Clear()`(=`__gb_*` 루트만 파기)가 못 지워 재조립마다 쌓였다.
            BuildCenterLine(ROAD_X, GroundDepth());

            TrafficLight signal = BuildTrafficLight(new Vector3(ROAD_X + 2.6f, 0f, -3.4f));

            // S-052 ② 행인 3 — S-076 ②: 신호를 지키고, 전방 회피·뛰는 플레이어 구경까지.
            NpcBuildKit.BuildPedestrian("Walker_A", new Vector3(-8f, 0f, 2.2f), new Color(0.45f, 0.52f, 0.62f), 6f, signal, ROAD_X, "walker_a", gameState);
            NpcBuildKit.BuildPedestrian("Walker_B", new Vector3(6f, 0f, 2.6f), new Color(0.60f, 0.48f, 0.40f), 7f, signal, ROAD_X, "walker_b", gameState);
            NpcBuildKit.BuildPedestrian("Walker_C", new Vector3(18f, 0f, 2.0f), new Color(0.50f, 0.58f, 0.45f), 5f, signal, ROAD_X, "walker_c", gameState);

            GameObject trafficGo = GreyboxStageBuilder.CreateEmpty("Traffic", new Vector3(ROAD_X, 0f, 0f));
            TrafficRoad trafficRoad = trafficGo.AddComponent<TrafficRoad>();
            SerializedObject trafficSo = new SerializedObject(trafficRoad);
            trafficSo.FindProperty("_signal").objectReferenceValue = signal;
            // S-122 ⑬ — 차량 1.3배(길이 3.77u·반길이 1.885u > 정지창 1.2u): 정지선을 4.2 → 4.8로
            // 물려야 정지한 차 앞코(최대 z −2.915)가 횡단보도(z ±2.8) 밖에 남는다.
            trafficSo.FindProperty("_stopLineZ").floatValue = 4.8f;
            trafficSo.ApplyModifiedPropertiesWithoutUndo();
            // S-116 게이트 후속 — 본편도 실모델 차량(white_van·yellow_taxi): 회색 큐브 차가
            // "무텍스처 플레이스홀더"로 읽힌다(캡처 게이트 적발). 프리팹 없으면 큐브 폴백(소켓).
            // 에셋 참조는 리플렉션 직접 주입 — SerializedObject 경유는 SaveScene 시 {fileID:0} 유실 (2026-07-20 실측).
            var carPrefabs = new List<GameObject>();
            foreach (string carName in new[] { "white_van", "yellow_taxi" })
            {
                GameObject carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Auto/" + carName + ".prefab");
                if (carPrefab != null) carPrefabs.Add(carPrefab);
            }
            typeof(TrafficRoad).GetField("_carVisualPrefabs",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(trafficRoad, carPrefabs.ToArray());
            EditorUtility.SetDirty(trafficRoad);

            // S-054b 엣지 워크 — 좌=이전 동네/캠프, 우=다음 동네(미해금이면 안내 후 차단).
            EdgeGateBuildKit.BuildGate("EdgeGate_Prev", new Vector3(-19f, 0f, 0f), DontLate.DistrictEdgeGate.Direction.Prev, gameState);
            EdgeGateBuildKit.BuildGate("EdgeGate_Next", new Vector3(19f, 0f, 0f), DontLate.DistrictEdgeGate.Direction.Next, gameState);

            // S-199 — 저장 직전에 아트 우선 규칙을 한 번 더 적용한다. 백드롭 배치(위) 이후에
            // 생기는 빌더물(교차도로·중앙선 등)이 세트에도 담겨 있으면 여기서 사본이 정리된다.
            ArtBackdropKit.SweepBuilderDuplicates();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[DistrictSceneBuilder] " + scenePath + " 조립 완료 — 매니저 제외 무대 + 슬롯 마커 "
                    + (BUILDING_SLOTS + PROP_SLOTS) + "개.");
        }

        // S-074 ⑨ — 그레이박스 신호등: 기둥 + 등2(적 위·녹 아래). 등화 전환은 TrafficLight가 MPB로.
        private static TrafficLight BuildTrafficLight(Vector3 position)
        {
            GameObject root = GreyboxStageBuilder.CreateEmpty("TrafficLight", position);

            Material pole = GreyboxStageBuilder.GetOrCreateMaterial("LampPole", new Color(0.22f, 0.23f, 0.26f), false);
            GameObject poleGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            poleGo.name = "Pole";
            poleGo.transform.SetParent(root.transform, false);
            poleGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            poleGo.transform.localScale = new Vector3(0.16f, 3.2f, 0.16f);
            Object.DestroyImmediate(poleGo.GetComponent<Collider>());
            poleGo.GetComponent<Renderer>().sharedMaterial = pole;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 3.35f, 0f);
            head.transform.localScale = new Vector3(0.42f, 1.25f, 0.3f); // S-076 ① — 3등 수용
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.GetComponent<Renderer>().sharedMaterial = pole;

            Material lampBase = GreyboxStageBuilder.GetOrCreateMaterial("SignalLamp", Color.gray, true); // 이미시브 지원
            Renderer red = MakeLamp(root.transform, "RedLamp", new Vector3(0f, 3.72f, -0.18f), lampBase);
            Renderer yellow = MakeLamp(root.transform, "YellowLamp", new Vector3(0f, 3.35f, -0.18f), lampBase); // S-076 ①
            Renderer green = MakeLamp(root.transform, "GreenLamp", new Vector3(0f, 2.98f, -0.18f), lampBase);

            TrafficLight light = root.AddComponent<TrafficLight>();
            SerializedObject so = new SerializedObject(light);
            so.FindProperty("_redLamp").objectReferenceValue = red;
            so.FindProperty("_yellowLamp").objectReferenceValue = yellow;
            so.FindProperty("_greenLamp").objectReferenceValue = green;
            so.ApplyModifiedPropertiesWithoutUndo();
            return light;
        }

        private static Renderer MakeLamp(Transform parent, string name, Vector3 localPos, Material material)
        {
            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = name;
            lamp.transform.SetParent(parent, false);
            lamp.transform.localPosition = localPos;
            lamp.transform.localScale = Vector3.one * 0.3f;
            Object.DestroyImmediate(lamp.GetComponent<Collider>());
            Renderer renderer = lamp.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        // 짐·비콘 런타임 스포너 (S-015).
        private static void AttachCargoSpawner(GameStateSO gameState)
        {
            GameObject go = new GameObject("__gb_CargoSpawner");
            DistrictCargoSpawner spawner = go.AddComponent<DistrictCargoSpawner>();

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
                AssetDatabase.LoadAssetAtPath<TuningConfigSO>("Assets/Data/Tuning.asset"); // 취급주의 HP (S-019)
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 카메라·조명 (빈 씬 보강) ─────────────────────────

        /// <summary>
        /// S-150 — 구버전 중앙선 정리. 종전엔 최상위에 `CenterLine`이라는 이름으로 저장돼
        /// `GreyboxStageBuilder.Clear()`(=`__gb_*`만 파기)를 빠져나갔다. 이미 저장된 씬에
        /// 남아 있는 것들을 여기서 걷어낸다 — 안 하면 새 점선과 겹쳐 두 겹으로 보인다.
        /// </summary>
        private static void DestroyLegacyCenterLines()
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go == null || go.name != "CenterLine") continue;
                if (go.transform.parent != null) continue; // 최상위 잔재만
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// 지면(`__gb_Ground`)의 Z 전폭(u). 도로·중앙선 길이를 여기서 역산해 지면과 항상 맞춘다 —
        /// 값을 손으로 적어두면 지면 치수가 바뀔 때 조용히 어긋난다(S-147 차선 40u 사고와 같은 부류).
        /// 지면을 못 찾으면 현 규격(Plane 스케일 8 × 기본 10u)을 폴백으로 쓴다.
        /// </summary>
        private static float GroundDepth()
        {
            GameObject ground = GameObject.Find("__gb_Ground");
            Renderer renderer = ground != null ? ground.GetComponent<Renderer>() : null;
            return renderer != null ? renderer.bounds.size.z : 80f;
        }

        /// <summary>
        /// 중앙선 점선. 도로 전 구간에 일정 간격으로 깔되 횡단보도 구간(중앙)은 비운다.
        /// </summary>
        private static void BuildCenterLine(float roadX, float depth)
        {
            const float DASH_LENGTH = 2.4f;   // 한 칸 길이
            const float DASH_GAP = 1.8f;      // 칸 사이
            const float CROSSWALK_HALF = 3.6f; // 이 안쪽은 비운다 (횡단보도 줄이 z ±2.8까지)

            Material material = GreyboxStageBuilder.GetOrCreateMaterial(
                "RoadCenterLine", new Color(0.94f, 0.78f, 0.22f), false);
            GameObject root = GreyboxStageBuilder.CreateEmpty("CenterLines", new Vector3(roadX, 0f, 0f));

            float pitch = DASH_LENGTH + DASH_GAP;
            int perSide = Mathf.FloorToInt((depth * 0.5f - CROSSWALK_HALF) / pitch);
            int index = 0;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < perSide; i++)
                {
                    float z = side * (CROSSWALK_HALF + DASH_LENGTH * 0.5f + i * pitch);
                    GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    dash.name = "Dash_" + index.ToString("00");
                    dash.transform.SetParent(root.transform, false);
                    dash.transform.localPosition = new Vector3(0f, 0.025f, z);
                    dash.transform.localScale = new Vector3(0.14f, 0.012f, DASH_LENGTH);
                    Object.DestroyImmediate(dash.GetComponent<Collider>());
                    dash.GetComponent<Renderer>().sharedMaterial = material;
                    index++;
                }
            }
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null) return;

            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.AddComponent<Camera>();
            // AudioListener는 Core 소유(D-041) — 콘텐츠 씬 카메라에 붙이지 않는다.
            // 붙이면 Core 것과 합쳐 2개가 되어 Unity가 경고를 내고 한쪽이 무시된다.
            // ConfigureCamera(GreyboxStageBuilder)가 FOV·위치·피치를 잡는다.
        }

        // ── 슬롯 마커 (스크립트 없는 빈 GameObject) ──────────

        private static (GameObject root, List<Transform> buildings, List<Transform> props) BuildSlots()
        {
            GameObject root = new GameObject(SLOTS_ROOT);
            var buildings = new List<Transform>();
            var props = new List<Transform>();

            float buildingStart = -(BUILDING_SLOTS - 1) * SLOT_SPACING * 0.5f;
            for (int i = 0; i < BUILDING_SLOTS; i++)
            {
                float x = buildingStart + i * SLOT_SPACING;
                if (Mathf.Abs(x) < 5f) continue; // S-074 ⑨ — 교차 도로(x=0) 자리: 건물이 도로를 깔고 앉지 않게
                buildings.Add(CreateSlot(root.transform, $"slot_building_{i + 1:00}", new Vector3(x, 0f, BUILDING_Z)));
            }

            float propStart = -(PROP_SLOTS - 1) * PROP_SLOT_SPACING * 0.5f;
            for (int i = 0; i < PROP_SLOTS; i++)
            {
                float x = propStart + i * PROP_SLOT_SPACING;
                props.Add(CreateSlot(root.transform, $"slot_prop_{i + 1:00}", new Vector3(x, 0f, PROP_Z)));
            }

            return (root, buildings, props);
        }

        private static Transform CreateSlot(Transform parent, string name, Vector3 localPosition)
        {
            GameObject slot = new GameObject(name);
            slot.transform.SetParent(parent, false);
            slot.transform.localPosition = localPosition;
            return slot.transform;
        }

        // 슬롯 루트에 배치 생성기를 얹고 슬롯 Transform 배열을 직렬화로 주입한다(런타임 이름 검색 금지 규약).
        // S-035(D-064): GameState 주입 — 런타임엔 currentDistrict가 구역 프로필·시드를 정한다
        // (_districtId 기본값 "빌라촌"은 씬 단독 Play 폴백). 프리팹 풀은 Prefabs/Auto pull 조립.
        private static void AttachLayoutGenerator(GameObject slotsRoot, List<Transform> buildings, List<Transform> props,
            GameStateSO gameState)
        {
            DistrictLayoutGenerator generator = slotsRoot.AddComponent<DistrictLayoutGenerator>();
            SetObjectArray(generator, "_buildingSlots", buildings);
            SetObjectArray(generator, "_propSlots", props);
            GreyboxStageBuilder.SetReference(generator, "_gameState", gameState);

            // 건물 풀 = Prefabs/Auto 중 소스가 Art/Buildings 인 프리팹 (pull 조립 — S-011).
            // S-116 ③ — 비건물 단품(door·old_stair)과 전고 2.5u 미만(스케일 미캘리브레이션)은 슬롯을
            // 잡아먹고 거리를 비워 보이게 한다(실측: store_2 0.7u가 슬롯 점유) — 풀에서 배제.
            var pool = new List<GameObject>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Auto" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name == "door" || name == "old_stair") continue;
                if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Buildings/" + name + ".fbx") == null) continue;
                // S-186 ③ — 구역 전용 풀. 화이트리스트가 있으면 그 안에 든 것만 쓴다.
                if (_buildingWhitelist != null && System.Array.IndexOf(_buildingWhitelist, name) < 0) continue;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
                if (bounds.size.y < 2.5f)
                {
                    Debug.LogWarning("[DistrictSceneBuilder] 건물 풀 제외(전고 " + bounds.size.y.ToString("0.0")
                        + "u < 2.5u — 스케일 미캘리브레이션 의심): " + name);
                    continue;
                }
                pool.Add(prefab);
            }
            SerializedObject serialized = new SerializedObject(generator);
            SerializedProperty poolProp = serialized.FindProperty("_buildingPrefabPool");
            poolProp.arraySize = pool.Count;
            for (int i = 0; i < pool.Count; i++)
                poolProp.GetArrayElementAtIndex(i).objectReferenceValue = pool[i];

            // S-114 — 보도 프랍 풀: 거리에 어울리는 소품만 선별 (실내 가구·차량 제외).
            string[] streetProps =
            {
                "basic_tree", "blossom_tree", "Bench_unity", "Trash_Bin_unity", "black_Trash_unity",
                "White_Trash_unity", "3_trash", "Bending_Mechine", "Signboard_unity", "bycle", "trash_spot",
            };
            var propPool = new List<GameObject>();
            foreach (string propName in streetProps)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Auto/" + propName + ".prefab");
                if (prefab != null) propPool.Add(prefab);
            }
            SerializedProperty propPoolProp = serialized.FindProperty("_propPrefabPool");
            propPoolProp.arraySize = propPool.Count;
            for (int i = 0; i < propPool.Count; i++)
                propPoolProp.GetArrayElementAtIndex(i).objectReferenceValue = propPool[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(Object target, string fieldName, List<Transform> values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // S-124 — 거리 자판기 손배치 + S-125 ② 구매창 배선. 배경 콜라이더는 PlaceCatalog가 이미 끄므로
        // (S-119 ① 규약) 보행·상자 물리 간섭 0이고, 상호작용 전용 트리거만 새로 얹는다.
        private static void BuildStreetVending(Vector3 groundPos, TuningConfigSO tuning)
        {
            GameObject vend = GreyboxStageBuilder.PlaceCatalog("Bending_Mechine", groundPos, 180f);
            if (vend == null) return; // 카탈로그 미도착 — 소켓 생략

            Renderer[] renderers = vend.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);

            BoxCollider trigger = vend.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = bounds.size + new Vector3(0.8f, 0f, 0.8f); // 앞에 서면 잡히게 여유
            trigger.center = vend.transform.InverseTransformPoint(bounds.center);
            GreyboxStageBuilder.AddSolidBlocker(vend); // S-166 ③ — 통과 금지

            VendingMachine vending = vend.AddComponent<VendingMachine>(); // E → 구매창 (S-125 ②)
            GreyboxStageBuilder.SetReference(vending, "_tuning", tuning);
            GreyboxStageBuilder.SetReference(vending, "_drinkMaterial",
                GreyboxStageBuilder.GetOrCreateMaterial("Drink", GreyboxStageBuilder.ParseColor("#e04a35"), false));
            GreyboxStageBuilder.SetReference(vending, "_highlightMaterial",
                GreyboxStageBuilder.GetOrCreateHighlightMaterial());
            GreyboxStageBuilder.SetReference(vending, "_renderer", renderers[0]);

            // S-193 — 편의점 데코(`__gb_Deco_store_2`) 철거(남규님 지시). 이 모델은 전고 0.7u로
            // 캘리브레이션이 어긋나 있어(위 건물 풀 배제 사유와 같은 개체) 거리에서 장난감처럼 보였다.
            // ⚠ 함께 사라지는 것: 여기 붙어 있던 **편의점 구매창**(KioskBuildKit). 구역 상점은
            //   자판기만 남는다 — 편의점을 되살리려면 제대로 된 모델에 다시 붙여야 한다.
        }

        // S-123 ① — 독백 스팟 부착 (문자열 배열이라 SerializedObject로 주입).
        internal static void AttachRemarkSpot(GameObject host, float radius, string[] lines)
        {
            AmbientRemarkSpot spot = host.AddComponent<AmbientRemarkSpot>();
            SerializedObject serialized = new SerializedObject(spot);
            serialized.FindProperty("_radius").floatValue = radius;
            SerializedProperty array = serialized.FindProperty("_lines");
            array.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
                array.GetArrayElementAtIndex(i).stringValue = lines[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 헬퍼 ─────────────────────────────────────────────

        private static void DestroyRoot(string name)
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go == null || go.name != name) continue;
                if (go.transform.parent != null) continue;
                Object.DestroyImmediate(go);
            }
        }
    }
}
