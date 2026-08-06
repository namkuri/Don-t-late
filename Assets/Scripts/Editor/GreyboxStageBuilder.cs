using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DontLate.EditorTools
{
    /// <summary>
    /// 코어 루프 확인용 그레이박스 무대를 현재 씬에 조립한다.
    /// 씬 파일을 커밋하지 않고 팀 전원이 같은 무대를 재현하기 위한 개발 도구다.
    /// 생성물은 전부 "__gb_" 접두어를 달고, 다시 실행하면 지우고 새로 만든다(멱등).
    /// </summary>
    public static class GreyboxStageBuilder
    {
        private const string PREFIX = "__gb_";


        private const string DATA_ROOT = "Assets/Data";
        private const string GREYBOX_ROOT = "Assets/Data/Greybox";
        private const string LAMP_PREFAB_PATH = "Assets/Prefabs/Hand/StreetLamp.prefab";
        private const string LAMP_LIGHT_PREFAB_PATH = "Assets/Prefabs/Hand/StreetLampLight.prefab";

        [MenuItem("DontLate/Build/Greybox Stage (개발용)", priority = 40)]
        public static void Build()
        {
            Clear();

            var (gameState, tuning, order) = GetOrCreateStageData();
            ResetSession(gameState, order);

            BuildManagers(gameState, tuning);
            BuildStageContent(gameState, tuning, order);

            Debug.Log("[Greybox] 무대 조립 완료 — Play를 누르면 WASD 이동 / E 상호작용. "
                    + "적재 1건(#" + order.orderId + ") 마감 " + (order.deadlineMinuteOfDay / 60f).ToString("0.0") + "시.");
        }

        /// <summary>무대 데이터 SO 3종을 확보한다(없으면 생성). 세션 리셋은 하지 않는다.</summary>
        public static (GameStateSO gameState, TuningConfigSO tuning, DeliveryOrderSO order) GetOrCreateStageData()
        {
            GameStateSO gameState = GetOrCreate<GameStateSO>(DATA_ROOT + "/GameState.asset", ConfigureGameState);
            TuningConfigSO tuning = GetOrCreate<TuningConfigSO>(DATA_ROOT + "/Tuning.asset", ConfigureTuning);
            DeliveryOrderSO order = GetOrCreate<DeliveryOrderSO>(DATA_ROOT + "/Order_HappyVilla.asset", ConfigureOrder);
            // S-035(D-064): GetOrCreate는 생성 시에만 configure — 기존 에셋도 정본 값으로 수렴시킨다
            // (district 문자열이 스폰 계약이라 구 구역명이 남으면 스폰 0). 같은 값 재기록 = 멱등.
            ConfigureOrder(order);
            EditorUtility.SetDirty(order);
            AssetDatabase.SaveAssetIfDirty(order);
            return (gameState, tuning, order);
        }

        /// <summary>
        /// 월드 무대(지면·레인·WalkableVolume·상자·문·비콘·플레이어·카메라)를 현재 씬에 조립한다.
        /// 매니저·세션 리셋은 포함하지 않는다 — Core 상주 씬(District)에서 무대만 재사용하기 위함.
        /// </summary>
        public static void BuildStageContent(GameStateSO gameState, TuningConfigSO tuning, DeliveryOrderSO order)
        {
            Material ground = GetOrCreateMaterial("Ground", new Color(0.24f, 0.24f, 0.26f), false);
            Material lane = GetOrCreateMaterial("Lane", new Color(0.34f, 0.33f, 0.30f), false);
            Material box = GetOrCreateMaterial("Box", ParseColor("#ff9f45"), false);
            Material door = GetOrCreateMaterial("Door", new Color(0.45f, 0.38f, 0.32f), false);
            Material highlight = GetOrCreateHighlightMaterial();
            Material beacon = GetOrCreateMaterial("Beacon", new Color(0.13f, 0.55f, 0.49f), false);

            BuildGround(ground, lane);
            BuildWalkableVolume();
            BuildGroundMist();
            BuildStarField();
            BuildMoon();
            BuildSunDisc();
            BuildPickupBox(order, box, highlight);
            BuildDoorVisual(door);
            BuildSignGlow();
            BuildBeacon(order, beacon, highlight);
            BuildPlayer(gameState, tuning);
            BuildStreetLamps();
            BuildPostVolume();
            ConfigureCamera();
            AttachCameraFollow();
        }

        // 카메라 X 팔로우 (S-016 ⑤) — 멱등: 있으면 타겟만 갱신.
        internal static void AttachCameraFollow()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            GameObject player = GameObject.Find(PREFIX + "Player");
            if (player == null) return;

            CameraFollowX follow = camera.GetComponent<CameraFollowX>();
            if (follow == null) follow = camera.gameObject.AddComponent<CameraFollowX>();
            SetReference(follow, "_target", player.transform);
        }

        [MenuItem("DontLate/Build/Clear Greybox Stage", priority = 41)]
        public static void Clear()
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go == null || !go.name.StartsWith(PREFIX)) continue;
                if (go.transform.parent != null) continue;
                Undo.DestroyObjectImmediate(go);
            }
        }

        // ── 무대 구성 ────────────────────────────────────────

        internal static void BuildGround(Material groundMaterial, Material laneMaterial)
        {
            GameObject ground = CreatePrimitive(PrimitiveType.Plane, "Ground", Vector3.zero);
            ground.transform.localScale = new Vector3(12f, 1f, 8f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            ground.layer = LAYER_GROUND; // S-128 ③ — 눈이 쌓이는 유일한 면

            GameObject lane = CreatePrimitive(PrimitiveType.Cube, "Lane", new Vector3(0f, 0.02f, 0f));
            Object.DestroyImmediate(lane.GetComponent<BoxCollider>());
            // S-147 — 지면 폭에 맞춘다. Unity Plane은 스케일 1이 10u라 Ground(스케일 12)는 **120u**인데
            // 차선은 Cube(스케일 1 = 1u)라 40u뿐이었다 — 좌우 40u 지점에서 도로가 뚝 끊겨
            // 흙바닥이 드러났다(남규님 지적 "중간에 끊키니까 어색함"). 지면과 같은 120u로 깐다.
            const float GROUND_HALF_SPAN = 12f * 10f; // Ground 스케일 × Plane 기본 10u
            lane.transform.localScale = new Vector3(GROUND_HALF_SPAN, 0.04f, 6f);
            lane.GetComponent<Renderer>().sharedMaterial = laneMaterial;
        }

        private static void BuildManagers(GameStateSO gameState, TuningConfigSO tuning)
        {
            GameObject managers = CreateEmpty("Managers", Vector3.zero);

            WorldDeliveryManager delivery = managers.AddComponent<WorldDeliveryManager>();
            SetReference(delivery, "_gameState", gameState);

            WorldDeadlineManager deadline = managers.AddComponent<WorldDeadlineManager>();
            SetReference(deadline, "_gameState", gameState);
            SetReference(deadline, "_tuning", tuning);

            WorldDayNightManager dayNight = managers.AddComponent<WorldDayNightManager>();
            SetReference(dayNight, "_gameState", gameState);
            SetReference(dayNight, "_tuning", tuning);
            SetReference(dayNight, "_sun", EnsureSun());
            SetReference(dayNight, "_backgroundCamera", Camera.main);

            WorldDebtManager debt = managers.AddComponent<WorldDebtManager>(); // S-005 — Core와 패리티
            SetReference(debt, "_gameState", gameState);
            SetReference(debt, "_tuning", tuning);

            WorldMinigameManager minigame = managers.AddComponent<WorldMinigameManager>(); // S-007
            SetReference(minigame, "_tuning", tuning);

            // BGM — 그레이박스에서도 곡 청취·판정이 되도록 같이 올린다.
            // AudioListener는 씬 기본 카메라의 것을 쓴다(여기서 추가하면 중복 경고).
            WorldAudioManager audio = managers.AddComponent<WorldAudioManager>();
            SetReference(audio, "_library", CoreSceneBuilder.GetOrCreateBgmLibrary());
            SfxSynthGenerator.EnsurePlaceholders();
            SetReference(audio, "_sfxPickup", CoreSceneBuilder.LoadSfx("sfx_pickup"));
            SetReference(audio, "_sfxDeliveryOk", CoreSceneBuilder.LoadSfx("sfx_delivery_ok"));
            SetReference(audio, "_sfxLateBuzzer", CoreSceneBuilder.LoadSfx("sfx_late_buzzer"));
        }

        /// <summary>씬의 Directional Light를 찾아 재사용(없으면 생성). DayNight의 _sun에 배선.</summary>
        private static Light EnsureSun()
        {
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
                if (light.type == LightType.Directional) return light;

            GameObject go = CreateEmpty("Sun", Vector3.zero);
            Light sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            return sun;
        }

        // StreetLamp.prefab을 길목 좌우 2열로 배치(프리팹 링크 유지 — Visual 교체가 전 인스턴스에 전파).
        // 앞줄 Z=-2.4 (yaw 0, Head가 +Z 길중앙 향함) · 뒷줄 Z=+2.4 (yaw 180, Head가 -Z 길중앙 향함).
        private static void BuildStreetLamps()
        {
            GameObject prefab = GetOrCreateLampPrefab();
            EnsureLampCone(); // StreetLampLight.prefab에 광추 Cone 보장(멱등) — 프리팹 링크로 전 인스턴스 전파
            int index = 1;
            // S-074 ⑨ — x=0 램프를 도로(x -2.1~2.1) 밖으로 이설 (교차 도로가 가로등을 뚫던 것).
            float[] front = { -16f, -8f, 5.4f, 12f, 18f };
            float[] back = { -12f, -5.4f, 12f };
            foreach (float x in front) PlaceLamp(prefab, new Vector3(x, 0f, -2.4f), 0f, ref index);
            foreach (float x in back) PlaceLamp(prefab, new Vector3(x, 0f, 2.4f), 180f, ref index);
        }

        private static void PlaceLamp(GameObject prefab, Vector3 position, float yaw, ref int index)
        {
            GameObject lamp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            lamp.name = PREFIX + "StreetLamp_" + index.ToString("00");
            lamp.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            Undo.RegisterCreatedObjectUndo(lamp, "Build Greybox Stage");
            index++;
        }

        // 아트 스왑 계약: 루트 StreetLamp / Visual(교체 대상: Pole·Head) / Light(StreetLampLight 프리팹 인스턴스).
        private static GameObject GetOrCreateLampPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(LAMP_PREFAB_PATH);
            if (existing != null) return existing;

            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Hand");

            GameObject lightPrefab = GetOrCreateLampLightPrefab();
            Material lampMaterial = GetOrCreateMaterial("Lamp", new Color(0.30f, 0.30f, 0.32f), false);

            GameObject root = new GameObject("StreetLamp");

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            // Pole: Cylinder r0.08 h4.0, 원점 바닥. 기본 실린더(r0.5 h2) → scale(0.16,2,0.16), center y=2.
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            Object.DestroyImmediate(pole.GetComponent<Collider>());
            pole.transform.SetParent(visual.transform, false);
            pole.transform.localPosition = new Vector3(0f, 2f, 0f);
            pole.transform.localScale = new Vector3(0.16f, 2f, 0.16f);
            pole.GetComponent<Renderer>().sharedMaterial = lampMaterial;

            // Head: Cube 0.5×0.15×0.25, 폴 상단(y=4)에서 길쪽(+Z)으로 돌출.
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.transform.SetParent(visual.transform, false);
            head.transform.localPosition = new Vector3(0f, 4f, 0.2f);
            head.transform.localScale = new Vector3(0.5f, 0.15f, 0.25f);
            head.GetComponent<Renderer>().sharedMaterial = lampMaterial;

            // Light: 기존 StreetLampLight.prefab 인스턴스를 Head 위치에. 프리팹의 45° 조사각 유지.
            GameObject light = (GameObject)PrefabUtility.InstantiatePrefab(lightPrefab);
            light.name = "Light";
            light.transform.SetParent(root.transform, false);
            light.transform.localPosition = new Vector3(0f, 4f, 0.2f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, LAMP_PREFAB_PATH);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // S-192 — 남규님 지시로 Outer(60)와 같게. 안쪽 각이 바깥과 같으면 가장자리 감쇠가 사라져
        // 광추 전체가 같은 밝기로 지면을 친다 — 좁은 심지 대신 꽉 찬 풀이 된다.
        private const float LAMP_INNER_ANGLE = 60f;

        private static GameObject GetOrCreateLampLightPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(LAMP_LIGHT_PREFAB_PATH);
            if (existing != null)
            {
                // S-192 — 이미 만들어진 프리팹은 그대로 재사용하되, **조사각만은 매 조립에서
                // 다시 맞춘다.** 프리팹이 있으면 통째로 건너뛰던 탓에 코드에서 각도를 바꿔도
                // 반영되지 않았다(남규님 "Inner 60으로 바꿔줘"가 재조립 후에도 22로 남던 이유).
                // 위치·색·세기는 사람이 인스펙터로 만지는 값이라 건드리지 않는다.
                // 조사각 갱신은 EnsureLampCone이 맡는다 — 그쪽은 매 조립마다 돈다(S-192).
                return existing;
            }

            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Hand");

            GameObject temp = new GameObject("StreetLampLight");
            temp.transform.localRotation = Quaternion.Euler(45f, 0f, 0f); // 광추 메시 기준각(코드가 상쇄해 수직)
            Light light = temp.AddComponent<Light>();
            // S-193 — 남규님이 Spot → **Point**로 바꿨다. 스팟은 원뿔 밖이 딱 끊겨 가로등 밑동과
            // 보도 옆면이 검게 남는데, 포인트는 사방으로 퍼져 등 주변이 자연스럽게 젖는다.
            // 코드도 같은 기준으로 만들어 둔다 — 안 그러면 프리팹을 지운 사람에게만 스팟이 부활한다.
            light.type = LightType.Point;
            light.range = 8f;
            light.spotAngle = 60f;
            light.innerSpotAngle = LAMP_INNER_ANGLE;
            light.color = ParseColor("#ff9f45");
            light.intensity = 22f; // 4u 높이·range 8에서 지면에 앰버 풀이 또렷하게 보이는 값 (사람 튜닝 대상)
            light.shadows = LightShadows.None; // S-069 — 가로등 8개 × 그림자 패스가 WebGL District 저하의 주범

            StreetLampLight lamp = temp.AddComponent<StreetLampLight>();
            SetReference(lamp, "_light", light);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, LAMP_LIGHT_PREFAB_PATH);
            Object.DestroyImmediate(temp);
            return prefab;
        }

        // StreetLampLight.prefab에 광추(원뿔) Cone을 보장한다(멱등). Light(45° 아래로 기움)의
        // 자식이므로 -45° 역회전해 콘을 수직으로 세운다. 프리팹 링크로 StreetLamp 전 인스턴스에 전파.
        private static void EnsureLampCone()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(LAMP_LIGHT_PREFAB_PATH);
            try
            {
                // S-192 — 조사각은 여기서 보장한다. 생성 함수(GetOrCreateLampLightPrefab)는
                // 프리팹이 이미 있으면 통째로 건너뛰므로 코드에서 각도를 바꿔도 반영되지 않는다
                // (남규님 "Inner 60"이 재조립 후에도 22로 남던 이유). 이 함수는 매 조립마다 돈다.
                // S-193 — 단 **광원 종류는 손대지 않는다.** 남규님이 Point로 바꿨고, 각도는
                // Point에선 의미가 없다. 스팟일 때만 각을 맞춘다.
                Light spot = root.GetComponent<Light>();
                if (spot != null && spot.type == LightType.Spot) spot.innerSpotAngle = LAMP_INNER_ANGLE;

                Transform coneT = root.transform.Find("Cone");
                GameObject cone;
                if (coneT == null)
                {
                    cone = new GameObject("Cone");
                    cone.transform.SetParent(root.transform, false);
                    cone.AddComponent<MeshFilter>();
                    cone.AddComponent<MeshRenderer>();
                }
                else cone = coneT.gameObject;

                cone.transform.localPosition = Vector3.zero;
                cone.transform.localRotation = Quaternion.Euler(-45f, 0f, 0f); // 부모 45° 상쇄 → 수직
                cone.transform.localScale = Vector3.one;

                cone.GetComponent<MeshFilter>().sharedMesh = GetOrCreateConeMesh();
                MeshRenderer mr = cone.GetComponent<MeshRenderer>();
                mr.sharedMaterial = GetOrCreateLightConeMaterial();
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;

                StreetLampLight lamp = root.GetComponent<StreetLampLight>();
                if (lamp != null) SetReference(lamp, "_coneRenderer", mr);

                PrefabUtility.SaveAsPrefabAsset(root, LAMP_LIGHT_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 절두원뿔 측면 메시(캡 없음). 상단 y=0 반경 0.2 → 하단 y=-3.5 반경 1.4. 위→아래로 벌어진다.
        // uv.y: 하단 0 · 상단 1(광원쪽). 법선은 반경 바깥 방향(프레넬 소프트 엣지용).
        private static Mesh GetOrCreateConeMesh()
        {
            string path = GREYBOX_ROOT + "/GB_LightCone_mesh.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            EnsureFolder(DATA_ROOT);
            EnsureFolder(GREYBOX_ROOT);

            const int seg = 24;
            const float rTop = 0.2f, rBot = 1.4f, h = 3.5f;

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int i = 0; i <= seg; i++)
            {
                float a = (float)i / seg * Mathf.PI * 2f;
                float cx = Mathf.Cos(a), cz = Mathf.Sin(a);
                verts.Add(new Vector3(cx * rTop, 0f, cz * rTop));    // 상단
                verts.Add(new Vector3(cx * rBot, -h, cz * rBot));    // 하단
                Vector3 n = new Vector3(cx, 0f, cz).normalized;
                norms.Add(n); norms.Add(n);
                float u = (float)i / seg;
                uvs.Add(new Vector2(u, 1f));
                uvs.Add(new Vector2(u, 0f));
            }
            for (int i = 0; i < seg; i++)
            {
                int t0 = i * 2, b0 = i * 2 + 1, t1 = (i + 1) * 2, b1 = (i + 1) * 2 + 1;
                tris.Add(t0); tris.Add(b0); tris.Add(t1);
                tris.Add(t1); tris.Add(b0); tris.Add(b1);
            }

            var mesh = new Mesh { name = "GB_LightCone_mesh" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            return mesh;
        }

        private static Material GetOrCreateLightConeMaterial()
        {
            string path = GREYBOX_ROOT + "/GB_LightCone.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            EnsureFolder(DATA_ROOT);
            EnsureFolder(GREYBOX_ROOT);

            material = new Material(Shader.Find("DontLate/LightCone"));
            material.SetColor("_Color", ParseColor("#ff9f45") * 2f); // 앰버 HDR(블룸 임계 위)
            material.SetFloat("_Alpha", 0.5f);
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return material;
        }

        // 바닥 안개층 쿼드 2장. 수평(rot X=90°)으로 눕혀 레인(폭 X)을 덮는다. y·스케일 상이.
        // GroundMist 셰이더 + StarField.cs(_GlobalAlpha 밤 페이드 재사용). 낮=소멸.
        /// <summary>S-115 — 카탈로그 프리팹 배치: Prefabs/Auto/<이름>을 로드해 바닥을 groundPos에
        /// 스냅(수평 중심 정렬). 스케일은 ScaleTable 캘리브레이션 신뢰. 없으면 조용히 생략(소켓).</summary>
        internal static GameObject PlaceCatalog(string prefabName, Vector3 groundPos, float rotationY = 0f)
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Auto/" + prefabName + ".prefab");
            if (prefab == null) return null;
            GameObject instance = (GameObject)Object.Instantiate(prefab);
            instance.name = "__gb_Deco_" + prefabName;
            instance.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

            // S-119 ① — 데코는 배경 시각물: 풋프린트 콜라이더를 끈다. 켜두면 게임 물리에 간섭 —
            // 캠프 벨트 콜라이더(top 0.9)가 상자 스폰존을 침범해 "상자 공중부양"의 진범이었다(실측).
            foreach (Collider decoCollider in instance.GetComponentsInChildren<Collider>(true))
                decoCollider.enabled = false;

            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
                instance.transform.position = new Vector3(
                    groundPos.x - bounds.center.x, groundPos.y - bounds.min.y, groundPos.z - bounds.center.z);
            }
            else instance.transform.position = groundPos;
            return instance;
        }

        /// <summary>
        /// S-166 ③ — 통과 금지 몸통을 얹는다. 자판기·트럭처럼 **부피가 있는 물건**은 데코가 아니라
        /// 장애물이다. PlaceCatalog가 데코 콜라이더를 싹 끄는 규약(S-119 ①) 때문에 지금은
        /// 플레이어가 자판기·트럭 몸을 뚫고 지나가고, 행인도 그 안으로 걸어 들어간다.
        ///
        /// **자식으로 다는 이유**: 부모엔 이미 상호작용용 트리거가 붙어 있다. 같은 오브젝트에
        /// 솔리드를 겹치면 트리거 판정과 뒤섞이므로, 이름 붙인 자식 하나에 격리한다.
        /// 살짝(INSET) 줄여 다는 이유: 눈에 보이는 면과 딱 맞추면 모서리에 끼어 못 빠져나온다.
        /// </summary>
        internal static void AddSolidBlocker(GameObject host, float insetXZ = 0.15f, float heightScale = 1f)
        {
            if (host == null) return;
            if (host.transform.Find("Blocker") != null) return; // 멱등 — 재빌드해도 하나만

            Renderer[] renderers = host.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);

            var blocker = new GameObject("Blocker");
            blocker.transform.SetParent(host.transform, false);
            blocker.transform.position = bounds.center;
            blocker.transform.rotation = Quaternion.identity; // 월드 AABB 기준이므로 회전 없음

            BoxCollider box = blocker.AddComponent<BoxCollider>();
            Vector3 lossy = blocker.transform.lossyScale;
            Vector3 size = new Vector3(
                Mathf.Max(0.2f, bounds.size.x - insetXZ * 2f),
                Mathf.Max(0.2f, bounds.size.y * heightScale),
                Mathf.Max(0.2f, bounds.size.z - insetXZ * 2f));
            // 부모 스케일이 1이 아닐 수 있다 — 로컬 크기로 되돌려야 월드에서 의도한 부피가 된다.
            box.size = new Vector3(
                size.x / Mathf.Max(0.0001f, lossy.x),
                size.y / Mathf.Max(0.0001f, lossy.y),
                size.z / Mathf.Max(0.0001f, lossy.z));
        }

        internal static void BuildGroundMist()
        {
            Material mist = GetOrCreateGroundMistMaterial();
            BuildMistQuad("GroundMist_Lo", new Vector3(0f, 0.30f, 0f), new Vector3(36f, 5f, 1f), mist);
            BuildMistQuad("GroundMist_Hi", new Vector3(1.5f, 0.55f, 0.4f), new Vector3(30f, 4f, 1f), mist);
        }

        private static void BuildMistQuad(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = PREFIX + name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // 수평으로 눕힘
            go.transform.localScale = scale;
            Undo.RegisterCreatedObjectUndo(go, "Build Greybox Stage");

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;

            StarField fade = go.AddComponent<StarField>();
            SetReference(fade, "_renderer", renderer);
        }

        private static Material GetOrCreateGroundMistMaterial()
        {
            string path = GREYBOX_ROOT + "/GB_GroundMist.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            EnsureFolder(DATA_ROOT);
            EnsureFolder(GREYBOX_ROOT);

            material = new Material(Shader.Find("DontLate/GroundMist"));
            material.SetColor("_Color", new Color(0.60f, 0.62f, 0.72f, 1f));
            material.SetFloat("_ScrollSpeed", 0.03f);
            material.SetFloat("_NoiseScale", 4f);
            material.SetFloat("_Coverage", 0.55f);
            material.SetFloat("_GlobalAlpha", 0f);
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return material;
        }

        internal static void BuildWalkableVolume()
        {
            GameObject volume = CreateEmpty("Walkable", Vector3.zero);
            BoxCollider collider = volume.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(40f, 4f, 6f);
            collider.center = new Vector3(0f, 2f, 0f);
            volume.AddComponent<WalkableVolume>();
        }

        internal const int LAYER_PLAYER = 8;    // TagManager 등록 (S-040)
        internal const int LAYER_CART_WALL = 9; // 플레이어와 충돌 무시 — CoreBootstrap이 매트릭스 해제
        internal const int LAYER_GROUND = 10;   // S-128 ③ — 눈·비 파티클이 여기에만 충돌(공중 퇴적 차단)

        // 배달 대차 (S-038 → S-040 물리 재설계 — 1.5배 바구니). 루트 콜라이더 = 트리거(센서 포커스).
        // 바닥·사방 벽 = CartWall 레이어 실콜라이더 — 상자는 가두고 플레이어는 통과(낙사 회귀 방지).
        // 위는 개방 — 던지면 튀어나갈 수 있다 (남규님 스펙).
        internal static void BuildDeliveryCart(Vector3 position, bool requiresTruck = false)
        {
            Material cartMat = GetOrCreateMaterial("Cart", new Color(0.30f, 0.55f, 0.42f), false);
            Material highlight = GetOrCreateHighlightMaterial();

            GameObject root = CreateEmpty("DeliveryCart", position);
            BoxCollider focus = root.AddComponent<BoxCollider>();
            focus.isTrigger = true;
            focus.size = new Vector3(2.4f, 1.4f, 1.8f);
            focus.center = new Vector3(0f, 0.7f, 0f);

            // S-041: 몸으로 미는 물리 — CC 히트 푸시(PlayerLocomotionManager)가 동력.
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 8f;
            body.linearDamping = 2.5f;
            body.constraints = RigidbodyConstraints.FreezeRotation; // 전복·회전 방지 (2.5D)

            // 바닥 (1.5배: 1.4→2.1 × 0.9→1.35)
            AddCartPart(root, "Bed", new Vector3(0f, 0.3f, 0f), new Vector3(2.1f, 0.12f, 1.35f), cartMat, true);
            // 사방 벽 (높이 0.55 — 위 개방)
            AddCartPart(root, "WallL", new Vector3(-1.02f, 0.6f, 0f), new Vector3(0.08f, 0.55f, 1.35f), cartMat, true);
            AddCartPart(root, "WallR", new Vector3(1.02f, 0.6f, 0f), new Vector3(0.08f, 0.55f, 1.35f), cartMat, true);
            AddCartPart(root, "WallF", new Vector3(0f, 0.6f, 0.66f), new Vector3(2.1f, 0.55f, 0.08f), cartMat, true);
            AddCartPart(root, "WallB", new Vector3(0f, 0.6f, -0.66f), new Vector3(2.1f, 0.55f, 0.08f), cartMat, true);
            // 손잡이 (시각 전용)
            AddCartPart(root, "Handle", new Vector3(-1.15f, 0.85f, 0f), new Vector3(0.08f, 1.1f, 1.0f), cartMat, false);

            GameObject drop = new GameObject("DropPoint");
            drop.transform.SetParent(root.transform, false);
            drop.transform.localPosition = new Vector3(0f, 1.5f, 0f);

            DeliveryCart cart = root.AddComponent<DeliveryCart>();
            // S-056 — 구루마 소유 게이트 배선.
            {
                var cartComp = root.GetComponentInChildren<DeliveryCart>();
                if (cartComp != null)
                {
                    SetReference(cartComp, "_stateForOwnership",
                        AssetDatabase.LoadAssetAtPath<GameStateSO>("Assets/Data/GameState.asset"));
                    var so = new SerializedObject(cartComp);
                    so.FindProperty("_requiresTruck").boolValue = requiresTruck;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            SetReference(cart, "_renderer", root.transform.Find("Bed").GetComponent<Renderer>());
            SetReference(cart, "_normalMaterial", cartMat);
            SetReference(cart, "_highlightMaterial", highlight);
            SetReference(cart, "_dropPoint", drop.transform);
        }

        private static void AddCartPart(GameObject root, string name, Vector3 localPos, Vector3 size,
            Material material, bool physical)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
            if (physical) part.layer = LAYER_CART_WALL; // 상자만 가두고 플레이어는 통과
            else Object.DestroyImmediate(part.GetComponent<Collider>());
        }

        private static void BuildPickupBox(DeliveryOrderSO order, Material normal, Material highlight)
        {
            var (go, _, _) = CreateParcelBox("Box", new Vector3(-5f, 0f, 0f), normal);

            PickupBox pickup = go.AddComponent<PickupBox>();
            SetReference(pickup, "_order", order);
            SetReference(pickup, "_highlightMaterial", highlight);

            // 거리 상자 = 배송용 — 캠프에서 실은 건만 집을 수 있다 (S-010).
            SerializedObject serialized = new SerializedObject(pickup);
            serialized.FindProperty("_requireInCargo").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 택배상자 생성 — prop_box_parcel(민지 수제, Prefabs/Auto)이 있으면 그걸 쓰고 없으면 큐브 폴백.
        /// 반환: (루트, 하이라이트용 렌더러, 원상복구용 원 머티리얼). 0.7u 정규화·발 y=0 (S-012).
        /// </summary>
        internal static (GameObject go, Renderer renderer, Material normalMaterial) CreateParcelBox(
            string name, Vector3 groundPosition, Material fallback, bool physical = false)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Auto/prop_box_parcel.prefab");
            if (prefab == null)
            {
                GameObject cube = CreatePrimitive(PrimitiveType.Cube, name, groundPosition + new Vector3(0f, 0.4f, 0f));
                cube.transform.localScale = Vector3.one * 0.8f;
                cube.GetComponent<BoxCollider>().isTrigger = !physical;
                if (physical) cube.AddComponent<Rigidbody>().mass = 2f;
                Renderer cubeRenderer = cube.GetComponent<Renderer>();
                cubeRenderer.sharedMaterial = fallback;
                return (cube, cubeRenderer, fallback);
            }

            GameObject root = CreateEmpty(name, groundPosition);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            // physical = 실물 스택 (S-016 ⑥) — 아래를 빼면 위가 무너진다. 아니면 센서용 트리거.
            collider.isTrigger = !physical;
            collider.size = Vector3.one * 0.7f;
            collider.center = new Vector3(0f, 0.35f, 0f);
            if (physical) root.AddComponent<Rigidbody>().mass = 2f;

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);

            // S-119 ① — 비주얼 자식 콜라이더(팩토리 풋프린트) 제거: 루트 0.7 콜라이더와 이중이면
            // 위 상자가 0.9u에 안착해 세로 갭 0.2u "공중부양"이 남는다 (캡처 게이트 픽셀 실측).
            foreach (Collider visualCollider in visual.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(visualCollider);

            Bounds bounds = ComputeRenderBounds(visual);
            if (bounds.size.y > 0.001f)
                visual.transform.localScale = Vector3.one * (0.7f / bounds.size.y);
            bounds = ComputeRenderBounds(visual);
            visual.transform.position += root.transform.position
                - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

            Renderer renderer = visual.GetComponentInChildren<Renderer>();
            Material normal = renderer != null ? renderer.sharedMaterial : fallback;
            return (root, renderer, normal);
        }

        // 문 큐브는 이제 시각물(배경)일 뿐 — 인증은 보도 위 비콘이 맡는다.
        private static void BuildDoorVisual(Material material)
        {
            GameObject go = CreatePrimitive(PrimitiveType.Cube, "Door", new Vector3(6f, 1f, 2.6f));
            go.transform.localScale = new Vector3(1f, 2f, 0.2f);
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        // 문(__gb_Door, x=6·top y=2·front z=2.5) 위쪽에 가게 간판 1개 — 쿼드가 간판 그 자체.
        // 저녁·밤엔 SignGlow가 이 면의 이미시브를 켠다 (D-051: 간판을 덮는 별도 발광판 폐지).
        private static void BuildSignGlow()
        {
            Material sign = GetOrCreateSignMaterial();

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = PREFIX + "Sign";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = new Vector3(6f, 2.35f, 2.45f); // 문 상단 위 · z 살짝 앞(카메라 쪽)
            go.transform.localScale = new Vector3(0.9f, 0.4f, 1f);
            Undo.RegisterCreatedObjectUndo(go, "Build Greybox Stage");

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = sign;

            SignGlow glow = go.AddComponent<SignGlow>();
            SetReference(glow, "_signRenderer", renderer);
        }

        private static Material GetOrCreateSignMaterial()
        {
            string path = GREYBOX_ROOT + "/GB_Sign.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            EnsureFolder(DATA_ROOT);
            EnsureFolder(GREYBOX_ROOT);

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", ParseColor("#1a2233")); // 소등 시 어두운 간판 베이스
            material.SetColor("_EmissionColor", Color.black);
            AssetDatabase.CreateAsset(material, path);
            // 키워드를 에셋에 켜둬야 WebGL 빌드에서 이미시브 배리언트가 스트리핑되지 않는다.
            // CreateAsset 과정(URP 머티리얼 초기화)이 키워드를 리셋하므로 생성 "후"에 켠다 — 실측 2026-07-22.
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        // S-010: 낮 하늘의 해 디스크 — 픽셀풍 정사각 발광 쿼드. 정점 13시, 달과 반대 위상.
        // S-011: 마인크래프트풍 흰색 — 캐시 머티리얼에도 매 빌드 강제(색 변경이 전파되도록).
        internal static void BuildSunDisc()
        {
            // S-015: 주변 광원 무관 순백 — Unlit (기존 Lit 이미시브는 하늘광에 눌려 어두웠음).
            Material sun = GetOrCreateMaterial("SunDisc", Color.white, true);
            sun.shader = Shader.Find("Universal Render Pipeline/Unlit");
            sun.SetColor("_BaseColor", Color.white * 1.6f); // HDR — 블룸이 살짝 문다
            EditorUtility.SetDirty(sun);

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = PREFIX + "SunDisc";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = new Vector3(0f, 8.5f, 69f); // 초기값 — 첫 틱에 궤도가 덮어쓴다
            go.transform.localScale = new Vector3(4.5f, 4.5f, 1f);
            go.GetComponent<Renderer>().sharedMaterial = sun;
            Undo.RegisterCreatedObjectUndo(go, "Build Greybox Stage");

            ConfigureOrbit(go.AddComponent<SkyBodyOrbit>(), "Arc", 13f);
        }

        // 궤도 파라미터 주입 — 가시 하늘대(z≈69에서 y 0~10)에 맞춘 공통 반타원.
        private static void ConfigureOrbit(SkyBodyOrbit orbit, string mode, float peakHour)
        {
            SerializedObject so = new SerializedObject(orbit);
            so.FindProperty("_mode").enumValueIndex = mode == "Arc" ? 0 : 1;
            so.FindProperty("_center").vector3Value = new Vector3(0f, -2f, 69f);
            so.FindProperty("_radiusX").floatValue = 30f;
            so.FindProperty("_radiusY").floatValue = 10.5f;
            so.FindProperty("_peakHour").floatValue = peakHour;
            so.FindProperty("_spinDegreesPerDay").floatValue = 30f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 밤하늘 배경 별밭 쿼드. 지평선 위 하늘 영역(z=70 원경)에 절차적 사각 별.
        // 카메라(0,8.1,-40.4)가 +Z를 보므로 무회전 쿼드가 -Z(카메라)를 향한다(SignGlow와 동일).
        // 지면(z≤40, 불투명)이 지평선 아래 별을 뎁스 오클루전한다.
        internal static void BuildStarField()
        {
            Material stars = GetOrCreateStarFieldMaterial();

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = PREFIX + "StarField";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = new Vector3(0f, 28f, 70f);
            go.transform.localScale = new Vector3(200f, 70f, 1f);
            Undo.RegisterCreatedObjectUndo(go, "Build Greybox Stage");

            // S-010: 별밭 완만 회전 — 별들이 천구처럼 호를 그린다.
            ConfigureOrbit(go.AddComponent<SkyBodyOrbit>(), "Spin", 0f);

            // 쿼드 가로/세로 비를 셰이더에 주입해 별 셀을 정사각화(_Aspect 보정).
            // 캐시된 머티리얼 대비 팔레트 틴트를 흰색으로 고정(별색은 셰이더 팔레트 소유).
            stars.SetFloat("_Aspect", go.transform.localScale.x / go.transform.localScale.y);
            stars.SetColor("_Color", Color.white);
            // 별 크기 한 단계 축소(v3) + HDR 강도 주입(블룸 임계 돌파) — 캐시 머티리얼에도 매 빌드 갱신.
            stars.SetFloat("_StarSizeMin", 0.02f);
            stars.SetFloat("_SkyGradientStrength", 0.4f); // 별 배경 한 단계 어둡게 (S-015 사람 요청, 구 0.6)
            stars.SetFloat("_StarSizeMax", 0.12f);
            stars.SetFloat("_Intensity", 1.3f);

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = stars;

            StarField field = go.AddComponent<StarField>();
            SetReference(field, "_renderer", renderer);
        }

        private static Material GetOrCreateStarFieldMaterial()
        {
            string path = GREYBOX_ROOT + "/GB_StarField.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            EnsureFolder(DATA_ROOT);
            EnsureFolder(GREYBOX_ROOT);

            material = new Material(Shader.Find("DontLate/StarField"));
            material.SetColor("_Color", Color.white); // 팔레트가 별색을 정한다 — 틴트는 흰색
            material.SetFloat("_GlobalAlpha", 0f);
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return material;
        }

        // 밤 전용 달 쿼드. 별밭(z=70)보다 카메라 쪽(z=69) · 하늘 좌상. StarField.cs 재부착해 밤 페이드 재사용
        // (_GlobalAlpha 프로퍼티명 일치). 정사각 쿼드라 셰이더 원판 마스크가 원형을 유지한다.
        internal static void BuildMoon()
        {
            Texture2D moonTex = GetOrCreateMoonTexture();
            Material moonMat = GetOrCreateMoonMaterial();
            moonMat.SetTexture("_MainTex", moonTex);   // 재실행 시에도 텍스처 연결 보장(멱등)
            moonMat.SetColor("_Color", Color.white);   // 틴트 없음 — 텍스처 색 그대로 × _Intensity
            EditorUtility.SetDirty(moonMat);

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = PREFIX + "Moon";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            // 좌상 하늘. 카메라(0,8.1,-40.4)/FOV22/10°다운에서 z=69의 화면 상단은 대략 world y≈10 —
            // y=4·scale4.5는 상단이 프레임 밖으로 짤렸다(사람 보고). y=2.6·scale4.0으로 내려/줄여 전체를 프레임 안에.
            go.transform.position = new Vector3(-15f, 2.6f, 69f);
            go.transform.localScale = new Vector3(4.0f, 4.0f, 1f);
            Undo.RegisterCreatedObjectUndo(go, "Build Greybox Stage");

            // S-010: 시간에 따라 포물선 궤적 (정점 새벽 1시 — 해와 반대 위상, 아침·저녁 교차).
            ConfigureOrbit(go.AddComponent<SkyBodyOrbit>(), "Arc", 1f);

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = moonMat;

            StarField field = go.AddComponent<StarField>();
            SetReference(field, "_renderer", renderer);
        }

        private static Material GetOrCreateMoonMaterial()
        {
            string path = GREYBOX_ROOT + "/GB_Moon.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            EnsureFolder(DATA_ROOT);
            EnsureFolder(GREYBOX_ROOT);

            material = new Material(Shader.Find("DontLate/Moon"));
            material.SetColor("_Color", Color.white);   // 틴트 없음 — 텍스처가 색을 정한다
            material.SetFloat("_Intensity", 1.4f);
            material.SetFloat("_GlobalAlpha", 0f);
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return material;
        }

        // 32×32 픽셀아트 달 텍스처를 코드로 그려 png 저장. 계약 경로(Art/Backgrounds)라 임포터가
        // Point·무압축을 자동 적용한다. 이미 있으면 재생성하지 않는다 — 사람이 직접 그린 png로
        // 교체 가능하다는 계약(덮어쓰기 금지). 각진 픽셀 원 + 얼룩(2톤) + 달 토끼 실루엣.
        private const string MOON_TEX_PATH = "Assets/Art/Backgrounds/moon_pixel.png";

        private static Texture2D GetOrCreateMoonTexture()
        {
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(MOON_TEX_PATH);
            if (existing != null) return existing;

            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/Backgrounds");

            const int N = 32;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.SetPixels32(GenerateMoonPixels(N));
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string abs = Application.dataPath + MOON_TEX_PATH.Substring("Assets".Length);
            System.IO.File.WriteAllBytes(abs, png);
            AssetDatabase.ImportAsset(MOON_TEX_PATH, ImportAssetOptions.ForceSynchronousImport);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(MOON_TEX_PATH);
        }

        // 달 픽셀맵을 생성한다. y=0이 하단(Texture2D 규약). 본체 밖은 alpha 0.
        private static Color32[] GenerateMoonPixels(int n)
        {
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 baseCream = new Color32(255, 244, 214, 255);   // #fff4d6 바탕
            Color32 crater1 = new Color32(234, 221, 180, 255);     // 얼룩 톤1 (옅은 그림자)
            Color32 crater2 = new Color32(212, 194, 144, 255);     // 얼룩 톤2 (짙은 마리아)
            Color32 rabbit = new Color32(156, 136, 80, 255);       // 달 토끼 (가장 어두운 톤)

            float cx = (n - 1) * 0.5f, cy = (n - 1) * 0.5f, r = n * 0.5f - 1f; // 반지름 15 (32px)

            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    // 안티앨리어싱 없는 정수 원 테스트 → 계단형(각진) 실루엣
                    px[y * n + x] = (dx * dx + dy * dy <= r * r) ? baseCream : clear;
                }

            // 얼룩(크레이터) — 달 본체 안에서만 칠한다. 좌/하단에 배치(우측 토끼와 분리)
            PaintDisc(px, n, cx, cy, r, 10f, 20f, 4.2f, crater1);  // 좌상 큰 마리아
            PaintDisc(px, n, cx, cy, r, 9f, 9f, 3.3f, crater2);    // 좌하
            PaintDisc(px, n, cx, cy, r, 21f, 24f, 2.6f, crater1);  // 상단 작은 얼룩
            PaintDisc(px, n, cx, cy, r, 6f, 14f, 2.2f, crater2);   // 좌측 가장자리 점

            // 달 토끼 실루엣 (위→아래 행) — 민지 원화 구도 참조(planning/reference/moon_minji_original.png):
            // 왼쪽을 보고 앉아 앞발(P열=공이)을 잡고 절구(좌하단 그릇)를 찧는 자세.
            // 귀 2개(틈 3px) + 둥근 등 + 뒷발. '.' 외 모든 문자가 토끼 톤.
            string[] rows =
            {
                ".....X...X....",
                ".....X...X....",
                ".....XX..XX...",
                ".....XXXXXX...",
                "....XXXXXXX...",
                "....XXXXXXXX..",
                ".P..XXXXXXXXX.",
                ".PXXXXXXXXXXX.",
                ".P.XXXXXXXXXX.",
                ".P..XXXXXXXXX.",
                "XXXX..XXXXXXX.",
                "XXXX...XXXXX..",
                ".XX.....XX.XX.",
            };
            int startX = 14;               // 실루엣 좌측 열(텍스처 x) — 중앙 우측
            int topY = n - 10;             // 첫 행의 텍스처 y (아래로 갈수록 y 감소)
            for (int row = 0; row < rows.Length; row++)
                for (int col = 0; col < rows[row].Length; col++)
                {
                    if (rows[row][col] == '.') continue;
                    int tx = startX + col;
                    int ty = topY - row;
                    if (tx < 0 || tx >= n || ty < 0 || ty >= n) continue;
                    float dx = tx - cx, dy = ty - cy;
                    if (dx * dx + dy * dy > r * r) continue;   // 본체 안에서만
                    px[ty * n + tx] = rabbit;
                }

            return px;
        }

        // (ccx,ccy) 반경 rad 원반을 col로 칠하되 달 본체(cx,cy,r) 안쪽만.
        private static void PaintDisc(Color32[] px, int n, float cx, float cy, float r,
                                      float ccx, float ccy, float rad, Color32 col)
        {
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x - ccx, dy = y - ccy;
                    if (dx * dx + dy * dy > rad * rad) continue;
                    float mdx = x - cx, mdy = y - cy;
                    if (mdx * mdx + mdy * mdy > r * r) continue;
                    px[y * n + x] = col;
                }
        }

        // 공용 무대 글로벌 블룸(약). 간판(HDR)·별·달·가로등이 블룸을 받는다. threshold~0.9 · intensity 0.35.
        internal static void BuildPostVolume()
        {
            VolumeProfile profile = GetOrCreatePostProfile();

            GameObject go = CreateEmpty("PostVolume", Vector3.zero);
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
        }

        private static VolumeProfile GetOrCreatePostProfile()
        {
            string path = GREYBOX_ROOT + "/GB_PostVolume.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile != null) return profile;

            EnsureFolder(DATA_ROOT);
            EnsureFolder(GREYBOX_ROOT);

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            Bloom bloom = profile.Add<Bloom>(true); // overrides=true → 하위 파라미터 오버라이드 활성
            bloom.threshold.value = 0.9f;   // 밝은 광원(HDR)만 번지게
            bloom.intensity.value = 0.35f;  // 약 — 과하지 않게(STYLE: 블룸 약)
            // scatter는 기본(0.7) 유지
            AssetDatabase.AddObjectToAsset(bloom, profile);

            AssetDatabase.SaveAssets();
            return profile;
        }

        // 비콘 패드 프리팹 (S-015) — 런타임 스포너가 주문 수만큼 찍는다. 주문은 SetOrder로 배정.
        internal static GameObject GetOrCreateBeaconPrefab()
        {
            const string path = "Assets/Prefabs/Hand/BeaconPad.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Hand");

            Material normal = GetOrCreateMaterial("Beacon", new Color(0.13f, 0.55f, 0.49f), false);
            Material highlight = GetOrCreateHighlightMaterial();
            Material rise = GetOrCreateBeaconRiseMaterial();
            Vector2 padSize = new Vector2(1f, 1f);

            GameObject root = new GameObject("BeaconPad");
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.2f, 2f, 1.2f);
            trigger.center = new Vector3(0f, 1f, 0f);

            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "Pad";
            Object.DestroyImmediate(pad.GetComponent<BoxCollider>());
            pad.transform.SetParent(root.transform, false);
            pad.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            pad.transform.localScale = new Vector3(padSize.x, 0.06f, padSize.y);
            Renderer renderer = pad.GetComponent<Renderer>();
            renderer.sharedMaterial = normal;

            DeliveryPoint point = root.AddComponent<DeliveryPoint>();
            SetReference(point, "_renderer", renderer);
            SetReference(point, "_normalMaterial", normal);
            SetReference(point, "_highlightMaterial", highlight);
            // S-133 ① → S-160 — 목적지 표시색을 **파랑**으로(남규님 지시). 종전 앰버(#ff9f45)는
            // 가로등 색과 같아 밤에 묻혔다. 들고 있는 상자의 목적지 패드가 이 색이 된다.
            SetReference(point, "_targetMaterial", GetOrCreateMaterial("BeaconTarget", ParseColor("#3aa0ff"), true));
            SetVector2(point, "_padSize", padSize);

            GameObject fx = new GameObject("Fx");
            fx.transform.SetParent(root.transform, false);
            float hw = padSize.x * 0.5f;
            float hd = padSize.y * 0.5f;
            const float fxHeight = 1.2f;
            CreateFxQuad(fx.transform, "FxPZ", new Vector3(0f, fxHeight * 0.5f, hd), new Vector3(padSize.x, fxHeight, 1f), Vector3.zero, rise);
            CreateFxQuad(fx.transform, "FxNZ", new Vector3(0f, fxHeight * 0.5f, -hd), new Vector3(padSize.x, fxHeight, 1f), Vector3.zero, rise);
            CreateFxQuad(fx.transform, "FxPX", new Vector3(hw, fxHeight * 0.5f, 0f), new Vector3(padSize.y, fxHeight, 1f), new Vector3(0f, 90f, 0f), rise);
            CreateFxQuad(fx.transform, "FxNX", new Vector3(-hw, fxHeight * 0.5f, 0f), new Vector3(padSize.y, fxHeight, 1f), new Vector3(0f, 90f, 0f), rise);
            SetReference(point, "_riseEffect", fx);

            // 주소 표시는 월드 텍스트 대신 HUD 풀해상([E] 안내 병기 — S-021 ②): 픽셀레이트에 안 뭉개진다.

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // 문 앞 보도(Z 중앙) 발광 패드. DeliveryPoint를 얹고, 감지 트리거는 패드보다 크게.
        private static void BuildBeacon(DeliveryOrderSO order, Material normal, Material highlight)
        {
            Vector2 padSize = new Vector2(1f, 1f);

            GameObject go = CreateEmpty("Beacon", new Vector3(6f, 0f, 0f));

            // 후보 감지는 넓게, 정밀 판정은 IFocusGate(_padSize)가 — 트리거는 현행 유지.
            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.2f, 2f, 1.2f);
            trigger.center = new Vector3(0f, 1f, 0f);

            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = PREFIX + "BeaconPad";
            Object.DestroyImmediate(pad.GetComponent<BoxCollider>());
            pad.transform.SetParent(go.transform, false);
            pad.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            pad.transform.localScale = new Vector3(padSize.x, 0.06f, padSize.y);

            Renderer renderer = pad.GetComponent<Renderer>();
            renderer.sharedMaterial = normal;

            DeliveryPoint point = go.AddComponent<DeliveryPoint>();
            SetReference(point, "_expectedOrder", order);
            SetReference(point, "_renderer", renderer);
            SetReference(point, "_normalMaterial", normal);
            SetReference(point, "_highlightMaterial", highlight);
            // S-133 ① → S-160 — 목적지 표시색을 **파랑**으로(남규님 지시). 종전 앰버(#ff9f45)는
            // 가로등 색과 같아 밤에 묻혔다. 들고 있는 상자의 목적지 패드가 이 색이 된다.
            SetReference(point, "_targetMaterial", GetOrCreateMaterial("BeaconTarget", ParseColor("#3aa0ff"), true));
            SetVector2(point, "_padSize", padSize);

            // 테두리 4면 빛기둥 (높이 1.2u, _padSize 연동) — 부모 __gb_BeaconFx.
            Material rise = GetOrCreateBeaconRiseMaterial();
            GameObject fx = new GameObject(PREFIX + "BeaconFx");
            fx.transform.SetParent(go.transform, false);
            fx.transform.localPosition = Vector3.zero;

            float hw = padSize.x * 0.5f;
            float hd = padSize.y * 0.5f;
            const float h = 1.2f;
            CreateFxQuad(fx.transform, "BeaconFxPZ", new Vector3(0f, h * 0.5f, hd), new Vector3(padSize.x, h, 1f), Vector3.zero, rise);
            CreateFxQuad(fx.transform, "BeaconFxNZ", new Vector3(0f, h * 0.5f, -hd), new Vector3(padSize.x, h, 1f), Vector3.zero, rise);
            CreateFxQuad(fx.transform, "BeaconFxPX", new Vector3(hw, h * 0.5f, 0f), new Vector3(padSize.y, h, 1f), new Vector3(0f, 90f, 0f), rise);
            CreateFxQuad(fx.transform, "BeaconFxNX", new Vector3(-hw, h * 0.5f, 0f), new Vector3(padSize.y, h, 1f), new Vector3(0f, 90f, 0f), rise);

            SetReference(point, "_riseEffect", fx);
        }

        private static GameObject CreateFxQuad(Transform parent, string name, Vector3 localPos, Vector3 localScale, Vector3 euler, Material material)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = PREFIX + name;
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = localPos;
            quad.transform.localScale = localScale;
            quad.transform.localRotation = Quaternion.Euler(euler);
            quad.GetComponent<Renderer>().sharedMaterial = material;
            return quad;
        }

        private static Material GetOrCreateBeaconRiseMaterial()
        {
            string path = GREYBOX_ROOT + "/GB_BeaconRise.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            EnsureFolder(DATA_ROOT);
            EnsureFolder(GREYBOX_ROOT);

            material = new Material(Shader.Find("DontLate/BeaconRise"));
            material.SetColor("_Color", ParseColor("#3fe05a"));
            material.SetFloat("_Alpha", 1f);
            material.SetFloat("_ScrollSpeed", 0.5f);
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return material;
        }

        private const string COURIER_FBX_PATH = "Assets/Art/Characters/chr_courier.fbx";
        private const string COURIER_AC_PATH = "Assets/Art/Characters/AC_chr_courier.controller";

        internal static void BuildPlayer(GameStateSO gameState, TuningConfigSO tuning)
        {
            GameObject player = CreateEmpty("Player", new Vector3(0f, 0.1f, 0f));
            player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            player.layer = LAYER_PLAYER; // S-040 — CartWall과 충돌 무시(매트릭스는 CoreBootstrap)

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            Animator animator = BuildCourierVisual(player);

            PlayerManager hub = player.AddComponent<PlayerManager>();
            PlayerAnimationManager anim = player.AddComponent<PlayerAnimationManager>();
            SetReference(hub, "_tuning", tuning);
            SetReference(hub, "_gameState", gameState);
            if (animator != null) SetReference(anim, "_animator", animator);

            // 든 상자가 붙는 자리 — 가슴 높이 앞쪽.
            GameObject carryAnchor = new GameObject(PREFIX + "CarryAnchor");
            carryAnchor.transform.SetParent(player.transform, false);
            carryAnchor.transform.localPosition = new Vector3(0f, 1.05f, 0.45f);
            SetReference(player.GetComponent<PlayerStatusManager>(), "_carryAnchor", carryAnchor.transform);

            // S-195 — 캐리 중 양손을 상자에 붙인다. **Animator와 같은 게임오브젝트**에 얹어야
            // OnAnimatorIK가 불린다(플레이어 루트가 아니라 비주얼 자식이다).
            if (animator != null) AttachCarryHandIK(animator.gameObject, carryAnchor.transform);

            // S-070 ③ — 씬 전이 복원 상자가 캠프 박스와 동일 룩을 갖도록 비주얼 프리팹 주입.
            GameObject parcelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Auto/prop_box_parcel.prefab");
            if (parcelPrefab != null)
                SetReference(player.GetComponent<PlayerStatusManager>(), "_parcelVisualPrefab", parcelPrefab);

            // S-073 ④ — 든 상자 마감 라벨용 한글 폰트 주입.
            var overlayFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
                "Assets/Art/UI/Fonts/DNFBitBitOTF SDF.asset");
            if (overlayFont != null)
                SetReference(player.GetComponent<PlayerStatusManager>(), "_overlayFont", overlayFont);

            GameObject sensor = new GameObject(PREFIX + "Sensor");
            sensor.transform.SetParent(player.transform, false);
            sensor.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            sensor.AddComponent<InteractionSensor>();
        }

        // chr_courier(Tripo·Mixamo 리그) 인스턴스를 플레이어 밑에 붙인다.
        // 렌더 바운즈 높이를 1.8u로 정규화하고 발을 지면(y=0)에 맞춘 뒤 Animator(AC+아바타)를 배선한다.
        // 캡슐/코 비주얼 대체 — CharacterController 치수는 BuildPlayer가 그대로 유지한다.
        private static Animator BuildCourierVisual(GameObject player)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(COURIER_FBX_PATH);
            if (fbx == null)
            {
                Debug.LogWarning("[Greybox] chr_courier.fbx 미발견 — 비주얼 스킵.");
                return null;
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            visual.name = PREFIX + "CourierVisual";
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // 렌더 바운즈 높이 → 1.8u 정규화
            Bounds bounds = ComputeRenderBounds(visual);
            if (bounds.size.y > 0.001f)
                visual.transform.localScale = Vector3.one * (1.8f / bounds.size.y);

            // 발끝을 지면(world y=0)에 정렬
            bounds = ComputeRenderBounds(visual);
            visual.transform.position += Vector3.up * (0f - bounds.min.y);

            Animator animator = visual.GetComponentInChildren<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(COURIER_AC_PATH);
            animator.applyRootMotion = false;

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(COURIER_FBX_PATH))
                if (asset is Avatar avatar) { animator.avatar = avatar; break; }

            EnsureIkPass(); // S-195 — 이게 꺼져 있으면 OnAnimatorIK가 아예 안 불린다
            return animator;
        }

        /// <summary>
        /// S-195 — 캐리 손 IK를 얹는다(멱등). 플레이어·타이틀 러너 공용이라 여기 둔다.
        /// </summary>
        internal static void AttachCarryHandIK(GameObject animatorHost, Transform carryAnchor)
        {
            if (animatorHost == null || carryAnchor == null) return;

            CarryHandIK ik = animatorHost.GetComponent<CarryHandIK>();
            if (ik == null) ik = animatorHost.AddComponent<CarryHandIK>();
            // 앵커는 씬 오브젝트 참조라 SerializedObject로 넣는다(CODE_RULES §6 — 리플렉션 대입은
            // 저장 시 {fileID: 0}으로 유실된 이력이 있다).
            var so = new SerializedObject(ik);
            so.FindProperty("_carryAnchor").objectReferenceValue = carryAnchor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// S-195 — 배달원 컨트롤러의 IK Pass를 켠다(멱등).
        ///
        /// 이 체크 하나가 꺼져 있으면 `OnAnimatorIK`가 **호출조차 되지 않는다** — 코드도 배선도
        /// 멀쩡한데 손이 안 붙는, 원인이 안 보이는 부류의 실패다. 사람이 인스펙터에서 켜 두는
        /// 것에 기대면 재조립·클론 때마다 되돌아가므로 빌더가 매번 보장한다.
        /// </summary>
        internal static void EnsureIkPass()
        {
            var ac = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(COURIER_AC_PATH);
            if (ac == null) return;

            UnityEditor.Animations.AnimatorControllerLayer[] layers = ac.layers;
            bool changed = false;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].iKPass) continue;
                layers[i].iKPass = true;
                changed = true;
            }
            if (!changed) return;

            ac.layers = layers; // 배열을 되대입해야 반영된다 — 요소만 고치면 사본이라 버려진다
            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
            Debug.Log("[애니] AC_chr_courier — IK Pass 활성화(캐리 양손 IK 전제).");
        }

        private static Bounds ComputeRenderBounds(GameObject root)
        {
            Bounds bounds = new Bounds(root.transform.position, Vector3.zero);
            bool initialized = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!initialized) { bounds = renderer.bounds; initialized = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        internal static void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                // 씬에 카메라가 없으면 생성 — "빌더가 정본"인데 카메라만 씬 잔존에 의존하면
                // 유실 시 복구 불가(실사례 2026-07-22: Core 카메라 소실로 화면 전체 무렌더).
                GameObject go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                camera = go.AddComponent<Camera>();
                // AudioListener는 붙이지 않는다 — Core 소유 별도 오브젝트가 이미 존재(D-041).
                Undo.RegisterCreatedObjectUndo(go, "Build Greybox Stage");
            }

            Undo.RecordObject(camera, "Greybox Camera");
            Undo.RecordObject(camera.transform, "Greybox Camera");
            camera.orthographic = false;
            camera.fieldOfView = 22f;
            camera.allowHDR = true; // 블룸(HDR 광원 번짐)이 동작하려면 HDR 버퍼 필요
            camera.transform.position = new Vector3(0f, 8.1f, -40.4f);
            camera.transform.rotation = Quaternion.Euler(10f, 0f, 0f);

            // URP 볼륨 포스트프로세싱 활성 — 이게 꺼져 있으면 블룸이 렌더되지 않는다.
            var camData = camera.GetUniversalAdditionalCameraData();
            if (camData != null) camData.renderPostProcessing = true;
        }

        // ── 데이터 ───────────────────────────────────────────

        private static void ConfigureGameState(GameStateSO state)
        {
            state.startDay = 1;
            state.startMinuteOfDay = 8f * 60f;
            state.startMoney = 0;
            state.startDebt = 10000;
        }

        private static void ConfigureTuning(TuningConfigSO tuning)
        {
            tuning.gameMinutesPerRealSecond = 2f;
        }

        private static void ConfigureOrder(DeliveryOrderSO order)
        {
            order.orderId = 7;
            order.address = "행복빌라 301호";
            order.district = DeliveryOrderSO.DISTRICT_VILLATOWN; // S-035(D-064) 구역 개편
            order.floor = 3;
            // 14시 — 인트로 대화·상차·이동(30~90분)을 거치는 실제 루프에서 잡을 수 있는 마감 (S-014, 구 10시는 구조적 지각).
            order.deadlineMinuteOfDay = 14f * 60f;
            order.reward = 5000;
            order.memo = "그레이박스 확인용";
        }

        /// <summary>메뉴를 다시 누르면 적재·정산이 초기 상태로 돌아온다.</summary>
        private static void ResetSession(GameStateSO state, DeliveryOrderSO order)
        {
            state.day = state.startDay;
            state.minuteOfDay = state.startMinuteOfDay;
            state.money = state.startMoney;
            state.debt = state.startDebt;
            state.completedCount = 0;
            state.lateCount = 0;
            state.cargo.Clear();
            state.cargo.Add(order);
            EditorUtility.SetDirty(state);
            AssetDatabase.SaveAssetIfDirty(state);
        }

        // ── 헬퍼 ─────────────────────────────────────────────

        internal static GameObject CreateEmpty(string name, Vector3 position)
        {
            GameObject go = new GameObject(PREFIX + name);
            go.transform.position = position;
            Undo.RegisterCreatedObjectUndo(go, "Build Greybox Stage");
            return go;
        }

        internal static GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = PREFIX + name;
            go.transform.position = position;
            Undo.RegisterCreatedObjectUndo(go, "Build Greybox Stage");
            return go;
        }

        internal static void SetReference(Object target, string fieldName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector2(Object target, string fieldName, Vector2 value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            property.vector2Value = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T GetOrCreate<T>(string path, System.Action<T> configure) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            EnsureFolder(DATA_ROOT);
            asset = ScriptableObject.CreateInstance<T>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>
        /// S-192 — 상호작용 하이라이트(시안 #35e0c8). **한 곳에서만 정의한다** — 공유 에셋이라
        /// 호출부마다 다른 세기를 주면 먼저 부른 쪽이 이기는 경합이 된다.
        ///
        /// 세기가 1.8이 아닌 이유: S-190에서 이미시브 키워드 유실을 고치자 이 머티리얼이
        /// 처음으로 실제 발광하기 시작했고, 1.8배 시안이 블룸 문턱(0.9)을 크게 넘겨 밤에
        /// 눈을 찔렀다(남규님 보고 — 자판기 접근 시). 문턱 아래로 낮춰 **번지지 않되
        /// 어둠 속에서 형태는 보이는** 세기로 잡는다.
        /// </summary>
        internal static Material GetOrCreateHighlightMaterial()
            => GetOrCreateMaterial("Highlight", ParseColor("#35e0c8"), true, HIGHLIGHT_EMISSION);

        private const float HIGHLIGHT_EMISSION = 0.45f;

        /// <param name="emissionStrength">
        /// 이미시브 배수. 기본 1.8은 블룸 문턱(0.9)을 넘겨 **빛나 보이라고** 잡은 값이다.
        /// 하이라이트처럼 밤에 눈을 찌르면 안 되는 것은 낮춰 부른다(S-192).
        /// </param>
        internal static Material GetOrCreateMaterial(string name, Color color, bool emissive,
            float emissionStrength = 1.8f)
        {
            string path = GREYBOX_ROOT + "/GB_" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                MigrateToSnowShader(material, emissive); // S-053 ⑤ — 기존 에셋도 스노 셰이더로 이식(멱등)
                if (emissive) EnableEmission(material, color, emissionStrength); // S-190 — 기존 에셋도 고친다
                return material;
            }

            EnsureFolder(DATA_ROOT);
            EnsureFolder(GREYBOX_ROOT);

            // S-053 ⑤: 비이미시브 = 스노 셰이더(전역 _DL_SnowMix로 윗면 적설) / 이미시브는 URP Lit 유지.
            Shader shader = emissive ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("DontLate/GreyboxSnow");
            material = new Material(shader);
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            // S-190 — 이미시브 설정은 **CreateAsset 뒤에** 한다. URP 머티리얼 초기화가 키워드를
            // 리셋해서, 앞에서 켜면 저장된 에셋에는 꺼진 채로 남는다(같은 함정을 GB_Sign에서
            // 이미 겪고 668행에 기록해 뒀는데 이 경로엔 적용이 안 돼 있었다 — 그 결과
            // GB_SignalLamp·GB_EdgeGate가 "색은 있는데 발광 안 함" 상태였다, 실측 2026-08-06).
            if (emissive) EnableEmission(material, color, emissionStrength);
            AssetDatabase.SaveAssets();
            return material;
        }

        /// <summary>이미시브를 실제로 켠다 — 키워드·GI 플래그까지(하나라도 빠지면 조용히 안 빛난다).</summary>
        private static void EnableEmission(Material material, Color color, float strength)
        {
            Color target = color * strength;
            bool ok = material.IsKeywordEnabled("_EMISSION")
                && material.GetColor("_EmissionColor") == target;
            if (ok) return;

            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", target);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
        }

        // 기존 GB_ 머티리얼을 스노 셰이더로 이식 — 색만 승계 (S-053 ⑤).
        private static void MigrateToSnowShader(Material material, bool emissive)
        {
            if (emissive) return;
            Shader snow = Shader.Find("DontLate/GreyboxSnow");
            if (snow == null || material.shader == snow) return;
            Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
            material.shader = snow;
            material.color = color;
            EditorUtility.SetDirty(material);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }

        internal static Color ParseColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
