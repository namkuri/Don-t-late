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

        [MenuItem("DontLate/Build District Stage", priority = 12)]
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

            BuildSlots();

            EditorSceneManager.SaveScene(scene, DISTRICT_PATH);
            Debug.Log("[DistrictSceneBuilder] District.unity 조립 완료 — 매니저 제외 무대 + 슬롯 마커 "
                    + (BUILDING_SLOTS + PROP_SLOTS) + "개.");
        }

        // ── 카메라·조명 (빈 씬 보강) ─────────────────────────

        private static void EnsureCamera()
        {
            if (Camera.main != null) return;

            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.AddComponent<Camera>();
            // Core엔 AudioListener가 없으므로 District 카메라가 유일한 리스너를 맡는다.
            go.AddComponent<AudioListener>();
            // ConfigureCamera(GreyboxStageBuilder)가 FOV·위치·피치를 잡는다.
        }

        // ── 슬롯 마커 (스크립트 없는 빈 GameObject) ──────────

        private static void BuildSlots()
        {
            GameObject root = new GameObject(SLOTS_ROOT);

            float buildingStart = -(BUILDING_SLOTS - 1) * SLOT_SPACING * 0.5f;
            for (int i = 0; i < BUILDING_SLOTS; i++)
            {
                float x = buildingStart + i * SLOT_SPACING;
                CreateSlot(root.transform, $"slot_building_{i + 1:00}", new Vector3(x, 0f, BUILDING_Z));
            }

            float propStart = -(PROP_SLOTS - 1) * SLOT_SPACING * 0.5f;
            for (int i = 0; i < PROP_SLOTS; i++)
            {
                float x = propStart + i * SLOT_SPACING;
                CreateSlot(root.transform, $"slot_prop_{i + 1:00}", new Vector3(x, 0f, PROP_Z));
            }
        }

        private static void CreateSlot(Transform parent, string name, Vector3 localPosition)
        {
            GameObject slot = new GameObject(name);
            slot.transform.SetParent(parent, false);
            slot.transform.localPosition = localPosition;
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
