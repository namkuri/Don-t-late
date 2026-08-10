using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// Home.unity(집 — 기상·휴식)에 방 그레이박스 무대를 조립하는 개발 도구 (D-052 존치 확정).
    /// 조작 없는 연출 씬 — 방 내부(바닥·벽·침대·창문·문)와 카메라만 깐다.
    /// 진행은 기존 하단 AdvanceButton(SceneFlowUIBuilder) 그대로. 멱등(__gb_ Clear 재사용).
    /// </summary>
    public static class HomeStageBuilder
    {
        private const string HOME_PATH = "Assets/Scenes/Home.unity";

        [MenuItem("DontLate/Build/Home Stage", priority = 13)]
        public static void BuildHomeStage()
        {
            Scene scene = EditorSceneManager.OpenScene(HOME_PATH, OpenSceneMode.Single);
            GreyboxStageBuilder.Clear();

            Material floor = GreyboxStageBuilder.GetOrCreateMaterial("HomeFloor", new Color(0.42f, 0.35f, 0.27f), false);
            Material wall = GreyboxStageBuilder.GetOrCreateMaterial("HomeWall", new Color(0.55f, 0.52f, 0.46f), false);
            Material door = GreyboxStageBuilder.GetOrCreateMaterial("Door", new Color(0.45f, 0.38f, 0.32f), false);

            BuildRoom(floor, wall, door);
            BuildBed(); // S-189 — 무대 고정물로 복귀 (아래 주석 참조)
            ArtBackdropKit.Build(ArtBackdropKit.Home); // S-180 ② — 아트 세트 소켓(프리팹 없으면 무시)
            MarkArtFurnitureEditable(scene); // S-276 — 아트 세트 가구도 집어서 옮길 수 있게
            BuildSky();
            BuildFurniturePlacer();
            GreyboxStageBuilder.BuildPostVolume();
            GreyboxStageBuilder.ConfigureCamera();
            PullCameraIntoRoom();

                        // S-059 — 집 고양이 (데려온 뒤에만 활성 — HomeCat 자체 판정).
            GameObject catGo = GreyboxStageBuilder.CreateEmpty("HomeCat", new Vector3(1.6f, 0f, 1.2f));
            HomeCat homeCat = catGo.AddComponent<HomeCat>();
            GreyboxStageBuilder.SetReference(homeCat, "_gameState", AssetDatabase.LoadAssetAtPath<GameStateSO>("Assets/Data/GameState.asset"));

            BuildNightLamp(); // S-274

            EditorSceneManager.SaveScene(scene, HOME_PATH);
            Debug.Log("[Home] 방 무대 조립 완료 — 연출 전용(조작 없음), 진행은 하단 버튼.");
        }

        /// <summary>
        /// S-189 — 침대를 **무대 고정물**로 세운다.
        ///
        /// 종전엔 HomeFurniturePlacer가 플레이 시작에 시드로 만들었다(S-031 ③). 그래서 에디터에서
        /// Home을 열면 방이 비어 있었고, 아트가 배치를 맞추려면 플레이를 돌려 생긴 `fur_bed(Clone)`을
        /// 붙잡는 수밖에 없었다 — 민지님이 그 클론을 세트에 담아 온 이유다. 씬을 열면 그냥 거기
        /// 있어야 맞춰볼 수 있다(남규님 "플레이중에 생성하지말고 그냥 첨부터 배치되게 하자").
        ///
        /// 모델 자식의 로컬 오프셋은 남규님이 씬에서 직접 맞춘 값이다 — 프리팹 원본을 고치면
        /// 다른 곳의 같은 침대까지 밀리므로, 여기 씬 인스턴스에만 준다.
        /// </summary>
        private static void BuildBed()
        {
            var so = AssetDatabase.LoadAssetAtPath<FurnitureSO>("Assets/Data/Furniture/fur_bed.asset");
            if (so == null || so.prefab == null)
            {
                Debug.Log("[Home] 침대 프리팹 미배선 — fur_bed.asset 확인 (무대는 그대로 조립).");
                return;
            }

            var bed = (GameObject)PrefabUtility.InstantiatePrefab(so.prefab);
            bed.name = "__gb_Bed";
            bed.transform.SetPositionAndRotation(BED_POSITION, Quaternion.Euler(0f, BED_YAW, 0f));
            if (!Mathf.Approximately(so.prefabScale, 1f))
                bed.transform.localScale *= so.prefabScale;

            // 모델 노드는 프리팹 루트 밑에 한 겹 더 있다(팩토리 산출물 구조).
            if (bed.transform.childCount > 0)
                bed.transform.GetChild(0).localPosition = BED_MODEL_OFFSET;

            // S-273 — 기본 가구도 구매 가구와 **같은 조작**을 받게 한다(남규님 지시).
            // 마커만 붙여 두면 런타임에 `HomeFurniturePlacer`가 배치 대장에 등재하고,
            // 그 뒤로는 이동·회전·삭제가 구매 가구와 완전히 같은 경로를 탄다.
            // 씬에 실물을 세우는 것은 그대로다(S-189 — 씬을 열면 방이 비면 아트가 배치를 못 맞춘다).
            bed.AddComponent<PlacedFurnitureVisual>().Bind("fur_bed", BED_POSITION, BED_YAW);

            Debug.Log("[Home] 침대 무대 배치 — " + BED_POSITION + " · 모델 오프셋 " + BED_MODEL_OFFSET);
        }

        private static readonly Vector3 BED_POSITION = new Vector3(-2.5f, 0f, 0.75f);
        private const float BED_YAW = 90f;
        // 남규님 지정값(2026-08-06) — 모델이 프레임 안에서 앉는 자리.
        private static readonly Vector3 BED_MODEL_OFFSET = new Vector3(0f, 0.00148209929f, 0.354f);

        /// <summary>
        /// S-276 — 아트 세트(`set_home`) 안의 가구에 편집 마커를 붙인다(남규님 지시: 의자·화분도 옮기게).
        /// 이름이 가구 SO(`Assets/Data/Furniture/*.asset`)와 **정확히 일치하는 것만** 대상이다 —
        /// 대응 SO가 없으면 런타임이 프리팹을 되찾지 못해 옮긴 뒤 복원이 안 된다.
        /// 마커만 붙이고 씬 실물은 그대로 둔다(S-189 — 씬을 열면 방이 채워져 있어야 아트가 배치를 맞춘다).
        /// </summary>
        private static void MarkArtFurnitureEditable(Scene scene)
        {
            int marked = 0;
            foreach (Transform node in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (node == null || node.GetComponent<PlacedFurnitureVisual>() != null) continue;
                var so = AssetDatabase.LoadAssetAtPath<FurnitureSO>("Assets/Data/Furniture/" + node.name + ".asset");
                if (so == null) continue;
                node.gameObject.AddComponent<PlacedFurnitureVisual>()
                    .Bind(node.name, node.position, node.eulerAngles.y);
                marked++;
            }
            Debug.Log("[Home] 아트 가구 " + marked + "개를 편집 가능으로 표시 (S-276).");
        }

        private static void BuildRoom(Material floor, Material wall, Material door)
        {
            GameObject floorGo = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, "HomeFloor", new Vector3(0f, -0.05f, 0f));
            floorGo.transform.localScale = new Vector3(8f, 0.1f, 6f);
            floorGo.GetComponent<Renderer>().sharedMaterial = floor;

            // 뒷벽 = 창 개구부(x 0.9~2.3 · y 1.15~2.25)를 남기고 4분할 — 진짜 뚫린 창 (S-011).
            // 바깥은 스카이박스가 그대로 보이고, Core 태양(Directional)이 시간에 따라 다른 각도로 스민다.
            AddWall("WallBackLeft", new Vector3(-1.55f, 1.5f, 3f), new Vector3(4.9f, 3f, 0.15f), wall);
            AddWall("WallBackRight", new Vector3(3.15f, 1.5f, 3f), new Vector3(1.7f, 3f, 0.15f), wall);
            AddWall("WallBackBelowWin", new Vector3(1.6f, 0.575f, 3f), new Vector3(1.4f, 1.15f, 0.15f), wall);
            AddWall("WallBackAboveWin", new Vector3(1.6f, 2.625f, 3f), new Vector3(1.4f, 0.75f, 0.15f), wall);
            AddWall("WallLeft", new Vector3(-4f, 1.5f, 0f), new Vector3(0.15f, 3f, 6f), wall);
            AddWall("WallRight", new Vector3(4f, 1.5f, 0f), new Vector3(0.15f, 3f, 6f), wall);

            // 천장 — 실내가 하늘광을 그대로 받지 않게 막는다 (S-010).
            AddWall("Ceiling", new Vector3(0f, 3.05f, 0f), new Vector3(8f, 0.12f, 6f), wall);

            // 현관문 — 우측 벽.
            GameObject doorGo = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, "HomeDoor", new Vector3(3.9f, 1f, -1.2f));
            Object.DestroyImmediate(doorGo.GetComponent<Collider>());
            doorGo.transform.localScale = new Vector3(0.12f, 2f, 1f);
            doorGo.GetComponent<Renderer>().sharedMaterial = door;
        }

        private static void AddWall(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, name, position);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        // (구 BuildBed는 S-031 ③에서 은퇴 — 침대는 HomeFurniturePlacer가 fur_bed로 시드한다.)

        // 창밖 하늘 (S-015) — 별밭·달·해를 거리 무대와 같은 원경(z≈69)에 깔되,
        // 방 창(개구부)에서 보이는 대역이 낮아(y -5~3) 해·달 궤도를 낮춰 재조정한다.
        /// <summary>
        /// S-274 — 저녁·밤 방 조명(남규님이 씬에서 맞춘 Point Light를 빌더 정본으로 승격).
        /// 값은 캡처 인스펙터 그대로: 위치 (−2.3, 2.7, 0) · 강도 3 · 반경 10 · 그림자 없음.
        /// 그림자를 끄는 이유는 실내 한 점 광원에 실그림자를 켜면 픽셀 렌더에서 계단이 크게 튀어서다.
        /// </summary>
        private static void BuildNightLamp()
        {
            GameObject go = GreyboxStageBuilder.CreateEmpty("RoomLamp", new Vector3(-2.3f, 2.7f, 0f));
            Light lamp = go.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = Color.white;
            lamp.intensity = 3f;
            lamp.range = 10f;
            lamp.shadows = LightShadows.None;
            go.AddComponent<NightLamp>(); // 낮에는 꺼진다
        }

        private static void BuildSky()
        {
            GreyboxStageBuilder.BuildStarField();
            GreyboxStageBuilder.BuildMoon();
            GreyboxStageBuilder.BuildSunDisc();

            foreach (string name in new[] { "__gb_Moon", "__gb_SunDisc" })
            {
                GameObject body = GameObject.Find(name);
                if (body == null) continue;
                body.transform.localScale = Vector3.one * 2.2f; // 창 프레임 안에 들어오는 크기
                SkyBodyOrbit orbit = body.GetComponent<SkyBodyOrbit>();
                SerializedObject so = new SerializedObject(orbit);
                so.FindProperty("_center").vector3Value = new Vector3(1.5f, -6f, 69f); // 창(x≈1.6) 시선축
                so.FindProperty("_radiusX").floatValue = 20f;
                so.FindProperty("_radiusY").floatValue = 7.5f; // 정점 y≈1.5 — 창 대역 안
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // 가구 배치기 (S-019 ④) — 폰 가구앱의 배치 대기를 바닥 클릭으로 소비.
        // + 데코레이터 (S-031 ④) — 벽·바닥 렌더러를 수집해 팔레트 적용기를 배선.
        private static void BuildFurniturePlacer()
        {
            GameStateSO gameState = AssetDatabase.LoadAssetAtPath<GameStateSO>("Assets/Data/GameState.asset");

            GameObject go = GreyboxStageBuilder.CreateEmpty("FurniturePlacer", Vector3.zero);
            HomeFurniturePlacer placer = go.AddComponent<HomeFurniturePlacer>();
            GreyboxStageBuilder.SetReference(placer, "_gameState", gameState);
            GreyboxStageBuilder.SetReference(placer, "_hintBackground",
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/panel/tutorial_long.png"));
            GreyboxStageBuilder.SetReference(placer, "_hintIcon",
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/panel/box_icon.png"));
            GreyboxStageBuilder.SetReference(placer, "_hintCloseIcon",
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/x.png"));

            var catalog = new System.Collections.Generic.List<FurnitureSO>();
            foreach (string guid in AssetDatabase.FindAssets("t:FurnitureSO", new[] { "Assets/Data/Furniture" }))
                catalog.Add(AssetDatabase.LoadAssetAtPath<FurnitureSO>(AssetDatabase.GUIDToAssetPath(guid)));
            SerializedObject serialized = new SerializedObject(placer);
            SerializedProperty prop = serialized.FindProperty("_catalog");
            prop.arraySize = catalog.Count;
            for (int i = 0; i < catalog.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = catalog[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            HomeDecorator decorator = go.AddComponent<HomeDecorator>();
            GreyboxStageBuilder.SetReference(decorator, "_gameState", gameState);
            var walls = new System.Collections.Generic.List<Renderer>();
            var floors = new System.Collections.Generic.List<Renderer>();
            foreach (GameObject root in go.scene.GetRootGameObjects())
            {
                if (root.name.Contains("Wall") || root.name.Contains("Ceiling"))
                    walls.Add(root.GetComponent<Renderer>());
                else if (root.name.Contains("HomeFloor"))
                    floors.Add(root.GetComponent<Renderer>());
            }
            SerializedObject decoratorSerialized = new SerializedObject(decorator);
            FillRendererArray(decoratorSerialized, "_wallRenderers", walls);
            FillRendererArray(decoratorSerialized, "_floorRenderers", floors);
            decoratorSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void FillRendererArray(SerializedObject serialized, string field,
            System.Collections.Generic.List<Renderer> renderers)
        {
            SerializedProperty prop = serialized.FindProperty(field);
            prop.arraySize = renderers.Count;
            for (int i = 0; i < renderers.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
        }

        // 방은 거리 무대보다 훨씬 작다 — 표준 리그(FOV 22·y8.1·z-40)로는 방이 점이 된다.
        private static void PullCameraIntoRoom()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            camera.transform.position = new Vector3(0f, 2.2f, -7.5f);
            camera.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
        }
    }
}
