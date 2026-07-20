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

        [MenuItem("DontLate/Build Greybox Stage", priority = 0)]
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
            Material body = GetOrCreateMaterial("Player", new Color(0.88f, 0.88f, 0.90f), false);
            Material nose = GetOrCreateMaterial("Facing", new Color(0.95f, 0.35f, 0.35f), false);
            Material box = GetOrCreateMaterial("Box", ParseColor("#ff9f45"), false);
            Material door = GetOrCreateMaterial("Door", new Color(0.45f, 0.38f, 0.32f), false);
            Material highlight = GetOrCreateMaterial("Highlight", ParseColor("#35e0c8"), true);
            Material beacon = GetOrCreateMaterial("Beacon", new Color(0.13f, 0.55f, 0.49f), false);

            BuildGround(ground, lane);
            BuildWalkableVolume();
            BuildStarField();
            BuildMoon();
            BuildPickupBox(order, box, highlight);
            BuildDoorVisual(door);
            BuildSignGlow();
            BuildBeacon(order, beacon, highlight);
            BuildPlayer(gameState, tuning, body, nose);
            BuildStreetLamps();
            BuildPostVolume();
            ConfigureCamera();
        }

        [MenuItem("DontLate/Clear Greybox Stage", priority = 1)]
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

        private static void BuildGround(Material groundMaterial, Material laneMaterial)
        {
            GameObject ground = CreatePrimitive(PrimitiveType.Plane, "Ground", Vector3.zero);
            ground.transform.localScale = new Vector3(12f, 1f, 8f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            GameObject lane = CreatePrimitive(PrimitiveType.Cube, "Lane", new Vector3(0f, 0.02f, 0f));
            Object.DestroyImmediate(lane.GetComponent<BoxCollider>());
            lane.transform.localScale = new Vector3(40f, 0.04f, 6f);
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
            int index = 1;
            float[] front = { -16f, -8f, 0f, 8f, 16f };
            float[] back = { -12f, -4f, 12f };
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

        private static GameObject GetOrCreateLampLightPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(LAMP_LIGHT_PREFAB_PATH);
            if (existing != null) return existing;

            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Hand");

            GameObject temp = new GameObject("StreetLampLight");
            temp.transform.localRotation = Quaternion.Euler(45f, 0f, 0f); // 아래 45° 조사
            Light light = temp.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 8f;
            light.spotAngle = 60f;
            light.color = ParseColor("#ff9f45");
            light.intensity = 22f; // 4u 높이·range 8에서 지면에 앰버 풀이 또렷하게 보이는 값 (사람 튜닝 대상)
            light.shadows = LightShadows.Soft;

            StreetLampLight lamp = temp.AddComponent<StreetLampLight>();
            SetReference(lamp, "_light", light);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, LAMP_LIGHT_PREFAB_PATH);
            Object.DestroyImmediate(temp);
            return prefab;
        }

        private static void BuildWalkableVolume()
        {
            GameObject volume = CreateEmpty("Walkable", Vector3.zero);
            BoxCollider collider = volume.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(40f, 4f, 6f);
            collider.center = new Vector3(0f, 2f, 0f);
            volume.AddComponent<WalkableVolume>();
        }

        private static void BuildPickupBox(DeliveryOrderSO order, Material normal, Material highlight)
        {
            GameObject go = CreatePrimitive(PrimitiveType.Cube, "Box", new Vector3(-5f, 0.4f, 0f));
            go.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            go.GetComponent<BoxCollider>().isTrigger = true;

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = normal;

            PickupBox pickup = go.AddComponent<PickupBox>();
            SetReference(pickup, "_order", order);
            SetReference(pickup, "_renderer", renderer);
            SetReference(pickup, "_normalMaterial", normal);
            SetReference(pickup, "_highlightMaterial", highlight);
        }

        // 문 큐브는 이제 시각물(배경)일 뿐 — 인증은 보도 위 비콘이 맡는다.
        private static void BuildDoorVisual(Material material)
        {
            GameObject go = CreatePrimitive(PrimitiveType.Cube, "Door", new Vector3(6f, 1f, 2.6f));
            go.transform.localScale = new Vector3(1f, 2f, 0.2f);
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        // 문(__gb_Door, x=6·top y=2·front z=2.5) 위쪽에 가게 간판 발광판 1개.
        // 밤 전용 additive 쿼드 — SignGlowPlate가 DayPhaseChanged로 점멸.
        private static void BuildSignGlow()
        {
            Material glow = GetOrCreateSignGlowMaterial();

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = PREFIX + "SignGlow";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = new Vector3(6f, 2.35f, 2.45f); // 문 상단 위 · z 살짝 앞(카메라 쪽)
            go.transform.localScale = new Vector3(0.9f, 0.4f, 1f);
            Undo.RegisterCreatedObjectUndo(go, "Build Greybox Stage");

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = glow;

            SignGlowPlate plate = go.AddComponent<SignGlowPlate>();
            SetReference(plate, "_renderer", renderer);
        }

        private static Material GetOrCreateSignGlowMaterial()
        {
            string path = GREYBOX_ROOT + "/GB_SignGlow.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            EnsureFolder(DATA_ROOT);
            EnsureFolder(GREYBOX_ROOT);

            material = new Material(Shader.Find("DontLate/SignGlow"));
            material.SetColor("_Color", ParseColor("#35e0c8"));
            material.SetFloat("_Intensity", 1f);
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return material;
        }

        // 밤하늘 배경 별밭 쿼드. 지평선 위 하늘 영역(z=70 원경)에 절차적 사각 별.
        // 카메라(0,8.1,-40.4)가 +Z를 보므로 무회전 쿼드가 -Z(카메라)를 향한다(SignGlow와 동일).
        // 지면(z≤40, 불투명)이 지평선 아래 별을 뎁스 오클루전한다.
        private static void BuildStarField()
        {
            Material stars = GetOrCreateStarFieldMaterial();

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = PREFIX + "StarField";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = new Vector3(0f, 28f, 70f);
            go.transform.localScale = new Vector3(200f, 70f, 1f);
            Undo.RegisterCreatedObjectUndo(go, "Build Greybox Stage");

            // 쿼드 가로/세로 비를 셰이더에 주입해 별 셀을 정사각화(_Aspect 보정).
            // 캐시된 머티리얼 대비 팔레트 틴트를 흰색으로 고정(별색은 셰이더 팔레트 소유).
            stars.SetFloat("_Aspect", go.transform.localScale.x / go.transform.localScale.y);
            stars.SetColor("_Color", Color.white);
            // 별 크기 한 단계 축소(v3) + HDR 강도 주입(블룸 임계 돌파) — 캐시 머티리얼에도 매 빌드 갱신.
            stars.SetFloat("_StarSizeMin", 0.02f);
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
        private static void BuildMoon()
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
            // 봉투 예시 y=33은 화면 위로 벗어나므로 가시 상단(y≈4)으로 내려 배치(별밭 z=70보다 카메라 쪽).
            go.transform.position = new Vector3(-15f, 4f, 69f);
            go.transform.localScale = new Vector3(4.5f, 4.5f, 1f);
            Undo.RegisterCreatedObjectUndo(go, "Build Greybox Stage");

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

            // 달 토끼 실루엣 (위→아래 행). 측면 포즈 — 귀 2개(2px 간격, 블룸에도 분리 유지) +
            // 코 왼쪽 머리 + 둥근 몸통 + 뒷발. 달 우측에 배치.
            string[] rows =
            {
                "..XX..XX..",
                "..XX..XX..",
                "..XX..XX..",
                "..XXXXXX..",
                ".XXXXXXX..",
                "XXXXXXXX..",
                "XXXXXXXXX.",
                "XXXXXXXXXX",
                "XXXXXXXXXX",
                ".XXXXXXXX.",
                "..XX..XX..",
                ".XX...XX..",
            };
            int startX = 15;               // 토끼 좌측 열(텍스처 x) — 중앙 우측
            int topY = n - 11;             // 첫 행의 텍스처 y (아래로 갈수록 y 감소)
            for (int row = 0; row < rows.Length; row++)
                for (int col = 0; col < rows[row].Length; col++)
                {
                    if (rows[row][col] != 'X') continue;
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
        private static void BuildPostVolume()
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

        private static void BuildPlayer(GameStateSO gameState, TuningConfigSO tuning, Material bodyMaterial, Material noseMaterial)
        {
            GameObject player = CreateEmpty("Player", new Vector3(0f, 0.1f, 0f));
            player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = PREFIX + "Body";
            Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = PREFIX + "Facing";
            Object.DestroyImmediate(nose.GetComponent<BoxCollider>());
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0f, 1.25f, 0.42f);
            nose.transform.localScale = new Vector3(0.18f, 0.18f, 0.45f);
            nose.GetComponent<Renderer>().sharedMaterial = noseMaterial;

            PlayerManager hub = player.AddComponent<PlayerManager>();
            player.AddComponent<PlayerAnimationManager>();
            SetReference(hub, "_tuning", tuning);
            SetReference(hub, "_gameState", gameState);

            // 든 상자가 붙는 자리 — 가슴 높이 앞쪽.
            GameObject carryAnchor = new GameObject(PREFIX + "CarryAnchor");
            carryAnchor.transform.SetParent(player.transform, false);
            carryAnchor.transform.localPosition = new Vector3(0f, 1.05f, 0.45f);
            SetReference(player.GetComponent<PlayerStatusManager>(), "_carryAnchor", carryAnchor.transform);

            GameObject sensor = new GameObject(PREFIX + "Sensor");
            sensor.transform.SetParent(player.transform, false);
            sensor.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            sensor.AddComponent<InteractionSensor>();
        }

        private static void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null) return;

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
            order.floor = 3;
            order.deadlineMinuteOfDay = 10f * 60f;
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

        private static GameObject CreateEmpty(string name, Vector3 position)
        {
            GameObject go = new GameObject(PREFIX + name);
            go.transform.position = position;
            Undo.RegisterCreatedObjectUndo(go, "Build Greybox Stage");
            return go;
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = PREFIX + name;
            go.transform.position = position;
            Undo.RegisterCreatedObjectUndo(go, "Build Greybox Stage");
            return go;
        }

        private static void SetReference(Object target, string fieldName, Object value)
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

        private static Material GetOrCreateMaterial(string name, Color color, bool emissive)
        {
            string path = GREYBOX_ROOT + "/GB_" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            EnsureFolder(DATA_ROOT);
            EnsureFolder(GREYBOX_ROOT);

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.8f);
            }
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }

        private static Color ParseColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
