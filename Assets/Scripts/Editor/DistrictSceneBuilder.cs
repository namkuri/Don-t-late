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
        private const string DISTRICT_PATH = "Assets/Scenes/District.unity";
        private const string SLOTS_ROOT = "Slots";

        // 슬롯 규약(발주): 건물 12칸 X간격 8u·길 안쪽 Z=2.6 / 소품 10칸 보도변 Z=-2.6.
        private const int BUILDING_SLOTS = 12;
        private const int PROP_SLOTS = 10;
        private const float SLOT_SPACING = 8f;
        private const float BUILDING_Z = 2.6f;
        private const float PROP_Z = -2.6f;

        [MenuItem("DontLate/Build/District Stage", priority = 14)]
        public static void BuildDistrictStage()
        {
            Scene scene = EditorSceneManager.OpenScene(DISTRICT_PATH, OpenSceneMode.Single);

            // 멱등: 이전 조립물 정리.
            GreyboxStageBuilder.Clear();
            DestroyRoot(SLOTS_ROOT);

            EnsureCamera();
            // Directional Light는 만들지 않는다 — 태양은 Core 소유(D-021). 이중 광원 방지.

            var (gameState, tuning, order) = GreyboxStageBuilder.GetOrCreateStageData();
            // 매니저·세션 리셋 제외 — District엔 무대만. 상주 매니저(Core)가 처리한다.
            GreyboxStageBuilder.BuildStageContent(gameState, tuning, order);

            (GameObject slotsRoot, List<Transform> buildingSlots, List<Transform> propSlots) = BuildSlots();
            AttachLayoutGenerator(slotsRoot, buildingSlots, propSlots, gameState);

            // S-015: 정적 짐·비콘 제거 — 도착 시 cargo 실데이터로 스폰(DistrictCargoSpawner)한다.
            DestroyRoot("__gb_Box");
            DestroyRoot("__gb_Beacon");
            AttachCargoSpawner(gameState);

            // S-052 ②③ — 행인 3 + 심부름 할머니 (길 건너까지 짐 옮기기).
            NpcBuildKit.BuildPedestrian("Walker_A", new Vector3(-8f, 0f, 2.2f), new Color(0.45f, 0.52f, 0.62f), 6f);
            NpcBuildKit.BuildPedestrian("Walker_B", new Vector3(6f, 0f, 2.6f), new Color(0.60f, 0.48f, 0.40f), 7f);
            NpcBuildKit.BuildPedestrian("Walker_C", new Vector3(18f, 0f, 2.0f), new Color(0.50f, 0.58f, 0.45f), 5f);
            NpcBuildKit.BuildErrandNpc("ErrandGranny", "할머니", new Vector3(12f, 0f, -1.8f),
                new Vector3(-6f, 0f, -1.8f), gameState, 1500);

            EditorSceneManager.SaveScene(scene, DISTRICT_PATH);
            Debug.Log("[DistrictSceneBuilder] District.unity 조립 완료 — 매니저 제외 무대 + 슬롯 마커 "
                    + (BUILDING_SLOTS + PROP_SLOTS) + "개.");
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
                GreyboxStageBuilder.GetOrCreateMaterial("Highlight", GreyboxStageBuilder.ParseColor("#35e0c8"), true);
            serialized.FindProperty("_boxFallback").objectReferenceValue =
                GreyboxStageBuilder.GetOrCreateMaterial("Box", GreyboxStageBuilder.ParseColor("#ff9f45"), false);
            serialized.FindProperty("_tuning").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TuningConfigSO>("Assets/Data/Tuning.asset"); // 취급주의 HP (S-019)
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 카메라·조명 (빈 씬 보강) ─────────────────────────

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
                buildings.Add(CreateSlot(root.transform, $"slot_building_{i + 1:00}", new Vector3(x, 0f, BUILDING_Z)));
            }

            float propStart = -(PROP_SLOTS - 1) * SLOT_SPACING * 0.5f;
            for (int i = 0; i < PROP_SLOTS; i++)
            {
                float x = propStart + i * SLOT_SPACING;
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
            var pool = new List<GameObject>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Auto" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Buildings/" + name + ".fbx") != null)
                    pool.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            }
            SerializedObject serialized = new SerializedObject(generator);
            SerializedProperty poolProp = serialized.FindProperty("_buildingPrefabPool");
            poolProp.arraySize = pool.Count;
            for (int i = 0; i < pool.Count; i++)
                poolProp.GetArrayElementAtIndex(i).objectReferenceValue = pool[i];
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
