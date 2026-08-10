using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// S-116 ⑤ — 촬영용 "District 1" 씬 조립 (DISTRICT1_SCENE_GUIDE 재현).
    /// 본편 District 조립(DistrictSceneBuilder.BuildStage)을 재사용하고 그 위에
    /// 자동 연출(DistrictCaptureDemo)·낮 하늘 레이어·지평선 포그·벚꽃 효과·실모델 차량을 얹는다.
    /// 씬을 열고 Play하면 키 입력 없이 촬영 연출이 진행된다 (가이드 §10).
    /// </summary>
    public static class District1SceneBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/District 1.unity";
        private const string INTAKE_UI = "Assets/_intake/art/ChatGPT/UI/";

        [MenuItem("DontLate/Build/District 1 (촬영)", priority = 15)]
        public static void BuildDistrict1()
        {
            // 씬 파일 보장 — 없으면 빈 씬을 먼저 만든다 (BuildStage는 기존 씬을 연다).
            if (!System.IO.File.Exists(SCENE_PATH))
            {
                Scene created = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(created, SCENE_PATH);
            }

            // 본편 District와 동일한 무대(슬롯·교통·신호등·NPC·엣지)를 깐다.
            DistrictSceneBuilder.BuildStage(SCENE_PATH);
            StripInteractions(); // S-122 ⑯ — 촬영 씬은 구경거리만 남긴다 (상호작용·잡기능 제거)
            Scene scene = SceneManager.GetActiveScene();

            // 단독 Play 시 Core(매니저) Additive 로드 — 날씨·시간 변주가 여기 걸려 있다.
            if (Object.FindAnyObjectByType<EnsureCoreLoaded>() == null)
                new GameObject("EnsureCoreLoaded").AddComponent<EnsureCoreLoaded>();

            Camera camera = Camera.main;
            GameObject playerGo = GameObject.Find("__gb_Player");
            if (camera == null || playerGo == null)
            {
                Debug.LogError("[District1] 카메라/플레이어를 찾지 못했다 — 조립 중단.");
                return;
            }

            BuildCaptureDemo(camera, playerGo.GetComponent<PlayerManager>());
            BuildDaySkyLayers(camera);
            BuildHorizonFogBand(camera);
            BuildBlossom();
            InjectRealCars();

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log("[District1] 촬영 씬 조립 완료 — Play 시 자동 연출(카메라 하강 12s → 왕복 + 시간·날씨 변주).");
        }

        private static void BuildCaptureDemo(Camera camera, PlayerManager player)
        {
            DistrictCaptureDemo demo = camera.GetComponent<DistrictCaptureDemo>();
            if (demo == null) demo = camera.gameObject.AddComponent<DistrictCaptureDemo>();
            GreyboxStageBuilder.SetReference(demo, "_player", player);
            GreyboxStageBuilder.SetReference(demo, "_camera", camera);
            GreyboxStageBuilder.SetReference(demo, "_boxPropPrefab",
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Auto/prop_box_parcel.prefab"));
        }

        // 가이드 §5 — Main Camera > DaySkyLayers (하늘·구름 4·로고). 위치·크기는 촬영 구도 기준 초기값.
        private static void BuildDaySkyLayers(Camera camera)
        {
            Transform old = camera.transform.Find("DaySkyLayers");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            GameObject root = new GameObject("DaySkyLayers");
            root.transform.SetParent(camera.transform, false);

            SpriteRenderer sky = MakeSprite(root.transform, "SkyBackground", "sky_bg.png",
                new Vector3(0f, 12f, 60f), 36f, -10);
            SpriteRenderer back1 = MakeSprite(root.transform, "BackCloud_1", "cloud1.png",
                new Vector3(-8f, 15f, 55f), 5.5f, -8);
            SpriteRenderer back2 = MakeSprite(root.transform, "BackCloud_2", "cloud2.png",
                new Vector3(9f, 17f, 55f), 4.5f, -8);

            GameObject logoSlot = new GameObject("LogoSlot");
            logoSlot.transform.SetParent(root.transform, false);
            // 화면 정중앙보다 약 15% 위 (z50 · FOV≈22° → 화면 절반높이 ≈9.7u → +2.9u).
            SpriteRenderer logo = MakeSprite(logoSlot.transform, "DontLateLogo", "logo.png",
                new Vector3(0f, 2.9f, 50f), 7f, -6);

            SpriteRenderer front1 = MakeSprite(root.transform, "FrontCloud_1", "cloud1.png",
                new Vector3(-6f, 6f, 40f), 4.5f, -4);
            SpriteRenderer front2 = MakeSprite(root.transform, "FrontCloud_2", "cloud2.png",
                new Vector3(7f, 8f, 40f), 4f, -4);

            DaySkyLayers layers = root.AddComponent<DaySkyLayers>();
            GreyboxStageBuilder.SetReference(layers, "_sky", sky);
            GreyboxStageBuilder.SetReference(layers, "_logo", logo);
            SetArray(layers, "_clouds", new[] { back1, back2, front1, front2 });
        }

        // 가이드 §6 — 지평선 포그 밴드: 화면 고정 쿼드 (원경 검은 경계 완화).
        private static void BuildHorizonFogBand(Camera camera)
        {
            Transform old = camera.transform.Find("HorizonFogBand");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            GameObject fog = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fog.name = "HorizonFogBand";
            Object.DestroyImmediate(fog.GetComponent<Collider>());
            fog.transform.SetParent(camera.transform, false);
            fog.transform.localPosition = new Vector3(0f, 1.2f, 45f);
            fog.transform.localScale = new Vector3(110f, 7f, 1f);
            fog.AddComponent<HorizonFogBand>();
        }

        // 가이드 §7 — 데모용 벚꽃나무 1주 + 수관 위 꽃잎 효과 (결정론 배치 나무와 별개 데코).
        private static void BuildBlossom()
        {
            GameObject old = GameObject.Find("__gb_Deco_blossom_tree");
            if (old != null) Object.DestroyImmediate(old);
            GameObject oldFx = GameObject.Find("BlossomPetalEffect");
            if (oldFx != null) Object.DestroyImmediate(oldFx);

            GameObject tree = GreyboxStageBuilder.PlaceCatalog("blossom_tree", new Vector3(-10f, 0f, 2.0f));
            if (tree == null) return; // 카탈로그 미도착 — 소켓 생략

            Bounds bounds = new Bounds(tree.transform.position, Vector3.zero);
            bool initialized = false;
            foreach (Renderer renderer in tree.GetComponentsInChildren<Renderer>())
            {
                if (!initialized) { bounds = renderer.bounds; initialized = true; }
                else bounds.Encapsulate(renderer.bounds);
            }

            GameObject fx = new GameObject("BlossomPetalEffect");
            fx.transform.position = new Vector3(bounds.center.x, bounds.max.y + 0.2f, bounds.center.z);
            BlossomPetalEffect effect = fx.AddComponent<BlossomPetalEffect>();
            GreyboxStageBuilder.SetReference(effect, "_petalTexture",
                AssetDatabase.LoadAssetAtPath<Texture2D>(INTAKE_UI + "one_blossom.png"));
            // S-283 — 머티리얼 정본(에셋)을 물린다. 런타임 생성은 빌드에서 투명 배리언트가 스트립된다.
            GreyboxStageBuilder.SetReference(effect, "_petalMaterial",
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Data/Greybox/GB_BlossomPetal.mat"));
            typeof(BlossomPetalEffect).GetField("_emitBox", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(effect, new Vector3(Mathf.Max(2f, bounds.size.x), 0.6f, Mathf.Max(2f, bounds.size.z)));
        }

        // 가이드 §8 — TrafficRoad에 실모델 차량(white_van·yellow_taxi) 주입 (길이 2.9 정규화는 런타임).
        private static void InjectRealCars()
        {
            TrafficRoad road = Object.FindAnyObjectByType<TrafficRoad>();
            if (road == null) return;
            GameObject van = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Auto/white_van.prefab");
            GameObject taxi = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Auto/yellow_taxi.prefab");
            var cars = new System.Collections.Generic.List<GameObject>();
            if (van != null) cars.Add(van);
            if (taxi != null) cars.Add(taxi);
            if (cars.Count == 0) return;
            typeof(TrafficRoad).GetField("_carVisualPrefabs", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(road, cars.ToArray());
        }

        /// <summary>
        /// S-122 ⑯ 촬영 전용 — 본편 조립에서 따라온 상호작용·잡기능을 걷어낸다.
        /// 남기는 것: 카메라 연출·플레이어 왕복·차량·신호등·횡단보도·날씨·벚꽃·건물/소품 배치·행인.
        /// Core 소유 오버레이(버전 라벨·BGM 라인)는 씬에 없으므로 DistrictCaptureDemo가 런타임에 끈다.
        /// </summary>
        private static void StripInteractions()
        {
            string[] removals =
            {
                "__gb_EdgeGate_Prev", // 엣지 워크 게이트 — 밟으면 씬 전이(촬영 중단 위험)
                "__gb_EdgeGate_Next",
                "__gb_ErrandGranny",  // 심부름 NPC — 부재 추첨이라 컷마다 존재가 달라진다
                "__gb_CargoSpawner",  // 짐·패드 런타임 스폰 + GameState 오염
                "__gb_Sensor",        // InteractionSensor — 하이라이트·NPC 이름표·송장 클릭의 발원지
            };
            foreach (string name in removals)
            {
                GameObject go = GameObject.Find(name);
                if (go == null) continue;
                Object.DestroyImmediate(go);
                Debug.Log("[District1] 촬영 제외 — " + name);
            }
        }

        // ── 헬퍼 ─────────────────────────────────────────────

        private static SpriteRenderer MakeSprite(Transform parent, string name, string file,
            Vector3 localPosition, float targetHeight, int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            Sprite sprite = LoadSpriteEnsured(INTAKE_UI + file);
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            if (sprite != null && sprite.bounds.size.y > 0.001f)
                go.transform.localScale = Vector3.one * (targetHeight / sprite.bounds.size.y);
            return renderer;
        }

        // 스프라이트 타입 보장 — Multiple+슬라이스0 실사고(구름 미로드) 재발 방지: Single 강제.
        private static Sprite LoadSpriteEnsured(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[District1] 텍스처 없음: " + path);
                return null;
            }
            if (importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void SetArray(Object target, string fieldName, Object[] values)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                Debug.LogError("[District1] 필드 없음: " + fieldName);
                return;
            }
            var array = System.Array.CreateInstance(field.FieldType.GetElementType(), values.Length);
            for (int i = 0; i < values.Length; i++) array.SetValue(values[i], i);
            field.SetValue(target, array);
        }
    }
}
