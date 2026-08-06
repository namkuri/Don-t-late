using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// S-186 ③ — 먹자골목(FoodStreet.unity).
    ///
    /// 왜 별도 빌더인가: 종전엔 빌라촌·먹자골목이 `District.unity` **한 씬을 공유**해
    /// "네 구역인데 두 곳이 똑같다"가 됐다(남규님 지적). 구역 : 씬을 1:1로 갈라야
    /// 개척이 보상으로 읽힌다.
    ///
    /// 왜 베끼지 않는가: 무대 골격(지면·도로·신호·행인·비콘 스포너)은 District와 같아야 한다 —
    /// 그건 코어루프의 규격이지 구역의 개성이 아니다. 그래서 **같은 빌더를 호출**하고
    /// (S-144 Main 선례) 구역색은 **건물 풀**로만 낸다.
    /// </summary>
    public static class FoodStreetSceneBuilder
    {
        private const string FOODSTREET_PATH = "Assets/Scenes/FoodStreet.unity";
        private const string CHRISTMAS_LIGHTS_ROOT = "__gb_ChristmasStringLights";

        private static readonly Color[] ChristmasColors =
        {
            new Color(1f, 0.35f, 0.37f),
            new Color(1f, 0.82f, 0.40f),
            new Color(0.32f, 0.82f, 0.45f),
            new Color(0.30f, 0.79f, 0.94f),
        };

        /// <summary>
        /// 먹자골목 건물 풀 — 음식점·카페·주점 위주.
        /// 이름은 `Assets/Art/Buildings/*.fbx` 파일명과 맞춘다(풀 선정이 파일명으로 걸린다).
        /// 실물이 없는 이름을 적어도 조용히 건너뛰므로, 아트가 늘면 여기에 이름만 더하면 된다.
        /// </summary>
        private static readonly string[] FoodBuildings =
        {
            "Pub_unity",        // 주점
            "brown_cafe",       // 카페
            "korean_cafe",      // 한식 카페
            "korean_cafe_2",
            "chicken_house",    // 치킨집
            "store_2",          // 편의점
            "brown_hall",       // 홀(식당)
            "Hardware_store",   // 상가 채움
            "blue_store_house",
        };

        [MenuItem("DontLate/Build/Food Street Stage", priority = 14)]
        public static void BuildFoodStreetStage()
        {
            EnsureSceneFile();
            DistrictSceneBuilder.BuildStage(FOODSTREET_PATH, FoodBuildings);
            BuildChristmasStringLights();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), FOODSTREET_PATH);
            Debug.Log("[FoodStreet] 먹자골목 무대 조립 완료 — 음식점 풀 " + FoodBuildings.Length + "종.");
        }

        [MenuItem("DontLate/Art/Add Food Street Christmas Lights", priority = 30)]
        public static void AddChristmasLightsToCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != FOODSTREET_PATH)
            {
                EditorUtility.DisplayDialog("Food Street Christmas Lights",
                    "Open Assets/Scenes/FoodStreet.unity before adding the lights.", "OK");
                return;
            }

            GameObject root = BuildChristmasStringLights();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root;
        }

        public static void InstallChristmasLightsBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(FOODSTREET_PATH, OpenSceneMode.Single);
            BuildChristmasStringLights();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[FoodStreet] Christmas string lights installed and saved.");
        }

        /// <summary>
        /// 씬 파일이 없으면 만든다. `BuildStage`는 `OpenScene`으로 시작하므로 파일이 먼저 있어야 한다
        /// (Hillside 선례와 같은 이유 — 최초 실행에서 씬이 없어 터지는 것을 막는다).
        /// </summary>
        private static GameObject BuildChristmasStringLights()
        {
            GameObject existing = GameObject.Find(CHRISTMAS_LIGHTS_ROOT);
            if (existing != null) Object.DestroyImmediate(existing);

            GameObject root = new GameObject(CHRISTMAS_LIGHTS_ROOT);
            Material cableMaterial = GreyboxStageBuilder.GetOrCreateMaterial(
                "ChristmasCable", new Color(0.045f, 0.05f, 0.055f), false);

            Material[] bulbMaterials = new Material[ChristmasColors.Length];
            for (int i = 0; i < ChristmasColors.Length; i++)
            {
                bulbMaterials[i] = GreyboxStageBuilder.GetOrCreateMaterial(
                    "ChristmasBulb_" + i, ChristmasColors[i], true);
            }

            var bulbs = new List<Renderer>();
            var fillLights = new List<Light>();
            float[] streetPositions = { -18f, -6f, 6f, 18f };
            for (int i = 0; i < streetPositions.Length; i++)
            {
                BuildLightString(root.transform, i, streetPositions[i], cableMaterial,
                    bulbMaterials, bulbs, fillLights);
            }

            ChristmasStringLights controller = root.AddComponent<ChristmasStringLights>();
            var serialized = new SerializedObject(controller);
            SetObjectArray(serialized.FindProperty("_bulbs"), bulbs);
            SetObjectArray(serialized.FindProperty("_fillLights"), fillLights);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static void BuildLightString(Transform parent, int stringIndex, float z,
            Material cableMaterial, Material[] bulbMaterials, List<Renderer> bulbs, List<Light> fillLights)
        {
            GameObject stringRoot = new GameObject("LightString_" + stringIndex);
            stringRoot.transform.SetParent(parent, false);
            stringRoot.transform.localPosition = new Vector3(0f, 0f, z);

            const int cablePoints = 9;
            var cable = stringRoot.AddComponent<LineRenderer>();
            cable.useWorldSpace = false;
            cable.positionCount = cablePoints;
            cable.startWidth = 0.035f;
            cable.endWidth = 0.035f;
            cable.sharedMaterial = cableMaterial;
            cable.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            cable.receiveShadows = false;

            for (int i = 0; i < cablePoints; i++)
            {
                float t = i / (float)(cablePoints - 1);
                cable.SetPosition(i, CablePosition(t));
            }

            const int bulbCount = 18;
            for (int i = 0; i < bulbCount; i++)
            {
                float t = i / (float)(bulbCount - 1);
                GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bulb.name = "Bulb_" + i.ToString("00");
                bulb.transform.SetParent(stringRoot.transform, false);
                bulb.transform.localPosition = CablePosition(t) + Vector3.down * 0.12f;
                bulb.transform.localScale = Vector3.one * 0.17f;
                Object.DestroyImmediate(bulb.GetComponent<Collider>());

                Renderer renderer = bulb.GetComponent<Renderer>();
                renderer.sharedMaterial = bulbMaterials[(i + stringIndex) % bulbMaterials.Length];
                bulbs.Add(renderer);
            }

            GameObject lightObject = new GameObject("FillLight");
            lightObject.transform.SetParent(stringRoot.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 4.8f, 0f);
            Light fillLight = lightObject.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.range = 7f;
            fillLight.intensity = 4.25f;
            fillLight.shadows = LightShadows.None;
            fillLight.color = ChristmasColors[stringIndex % ChristmasColors.Length];
            fillLights.Add(fillLight);
        }

        private static Vector3 CablePosition(float t)
        {
            float x = Mathf.Lerp(-8.5f, 8.5f, t);
            float y = 5.8f - Mathf.Sin(t * Mathf.PI) * 0.65f;
            return new Vector3(x, y, 0f);
        }

        private static void SetObjectArray<T>(SerializedProperty property, List<T> values) where T : Object
        {
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void EnsureSceneFile()
        {
            if (System.IO.File.Exists(FOODSTREET_PATH)) return;
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, FOODSTREET_PATH);
            Debug.Log("[FoodStreet] 씬 파일 신규 생성 — " + FOODSTREET_PATH);
        }
    }
}
