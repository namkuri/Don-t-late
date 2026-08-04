using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// 아트 테스트 씬 (A-006 — 민지님 요청). 반입된 프리팹(Auto·Hand)·캐릭터를 게임 룩
    /// (픽셀화 렌더·실그림자·간이 낮밤) 그대로 한 줄 진열한다. 단독 씬 — Core 불요.
    /// 조작: ←→/A·D 카메라, T 낮밤 사이클 토글. WebGL 별도 빌드로 /art-test/ 배포.
    /// </summary>
    public static class ArtTestSceneBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/ArtTest.unity";
        private const string FONT_PATH = "Assets/Art/UI/Fonts/DNFBitBitOTF SDF.asset";
        private const float SLOT_SPACING = 4f;

        [MenuItem("DontLate/Build/Art Test Scene", priority = 30)]
        public static void BuildArtTestScene()
        {
            Scene scene = File.Exists(SCENE_PATH)
                ? EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!File.Exists(SCENE_PATH)) EditorSceneManager.SaveScene(scene, SCENE_PATH);
            GreyboxStageBuilder.Clear();

            // ── 바닥·배경 ──
            Material ground = GreyboxStageBuilder.GetOrCreateMaterial("Ground", new Color(0.24f, 0.24f, 0.26f), false);
            GameObject floor = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Plane, "Floor", Vector3.zero);
            floor.transform.localScale = new Vector3(30f, 1f, 4f);
            floor.GetComponent<Renderer>().sharedMaterial = ground;

            // ── 태양 (컨트롤러가 회전) ──
            GameObject sunGo = GreyboxStageBuilder.CreateEmpty("Sun", Vector3.zero);
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.intensity = 1.1f;

            // ── 진열: Auto·Hand 프리팹 + 캐릭터 FBX ──
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            var entries = new List<(string label, GameObject asset)>();

            void CollectFolder(string folder)
            {
                if (!AssetDatabase.IsValidFolder(folder)) return;
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null) entries.Add((Path.GetFileNameWithoutExtension(path), prefab));
                }
            }

            CollectFolder("Assets/Prefabs/Auto");
            CollectFolder("Assets/Prefabs/Hand");
            GameObject courier = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/chr_courier.fbx");
            if (courier != null) entries.Insert(0, ("chr_courier", courier));

            // ── S-110 — 스케일 캘리브레이션 레퍼런스 (진열 0번 앞) ──
            // 인체 1.7u 마네킹 + 건물 출입문 게이지(2.1~2.4u) — 모든 에셋을 이 옆에서 육안 비율 판정.
            Material refMat = GreyboxStageBuilder.GetOrCreateMaterial("ScaleRef", new Color(1f, 0.42f, 0.36f), false);
            GameObject mannequin = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Capsule, "__gb_Ref_Human_1.7u",
                new Vector3(-SLOT_SPACING, 0.85f, 0f));
            mannequin.transform.localScale = new Vector3(0.44f, 0.85f, 0.44f); // 캡슐 높이 2u × 0.85 = 1.7u
            mannequin.GetComponent<Renderer>().sharedMaterial = refMat;

            GameObject doorMin = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, "__gb_Ref_Door_2.1u",
                new Vector3(-SLOT_SPACING * 2f, 1.05f, 0f));
            doorMin.transform.localScale = new Vector3(0.9f, 2.1f, 0.08f);
            doorMin.GetComponent<Renderer>().sharedMaterial = refMat;
            GameObject doorMax = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, "__gb_Ref_Door_2.4u",
                new Vector3(-SLOT_SPACING * 2f - 1.4f, 1.2f, 0f));
            doorMax.transform.localScale = new Vector3(0.9f, 2.4f, 0.08f);
            doorMax.GetComponent<Renderer>().sharedMaterial = refMat;

            if (font != null)
            {
                GameObject refLabelGo = new GameObject("__gb_Label_ScaleRef");
                refLabelGo.transform.position = new Vector3(-SLOT_SPACING * 1.5f, 3.4f, 0.6f);
                TextMeshPro refLabel = refLabelGo.AddComponent<TextMeshPro>();
                refLabel.font = font;
                refLabel.fontSize = 4.2f;
                refLabel.alignment = TextAlignmentOptions.Center;
                refLabel.text = "인체 1.7u · 문 2.1/2.4u";
                refLabel.color = new Color(1f, 0.42f, 0.36f);
            }

            float cursorX = 0f; // S-112 — 실폭 기반 누적 배치: 캘리브레이션 후 대형 건물 겹침 방지
            for (int i = 0; i < entries.Count; i++)
            {
                GameObject instance = (GameObject)Object.Instantiate(entries[i].asset, Vector3.zero, Quaternion.identity);
                instance.name = "__gb_Art_" + entries[i].label;

                // 실폭 실측 → 좌단을 커서에 맞추고 커서를 폭+여백만큼 전진.
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                Vector3 slot = new Vector3(cursorX, 0f, 0f);
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
                    slot = new Vector3(cursorX + bounds.extents.x, 0f, 0f);
                    instance.transform.position = new Vector3(
                        cursorX + bounds.extents.x - bounds.center.x, -bounds.min.y, -bounds.center.z);
                    cursorX += bounds.size.x + 2.5f; // 여백 2.5u
                }
                else cursorX += SLOT_SPACING;

                // 이름표 (3D TMP — 카메라 정면).
                if (font != null)
                {
                    GameObject labelGo = new GameObject("__gb_Label_" + entries[i].label);
                    labelGo.transform.position = slot + new Vector3(0f, 3.4f, 0.6f); // 머리 위 — 망원 카메라에서도 보이게
                    TextMeshPro label = labelGo.AddComponent<TextMeshPro>();
                    label.font = font;
                    label.fontSize = 4.2f;
                    label.alignment = TextAlignmentOptions.Center;
                    label.text = entries[i].label;
                    label.color = new Color(0.208f, 0.878f, 0.784f);
                    label.rectTransform.sizeDelta = new Vector2(3.8f, 2.2f); // 슬롯 간격(4u) 안쪽 — 겹침 방지, 2줄 허용
                }
            }

            // ── 카메라 (게임 픽셀화 파이프라인 그대로) + 컨트롤러 ──
            GreyboxStageBuilder.BuildPostVolume();
            GreyboxStageBuilder.ConfigureCamera();
            Camera camera = Camera.main;
            if (camera != null)
            {
                // 게임 카메라 리그(ConfigureCamera가 배치한 y·z·각도) 유지 — x만 진열 시작점으로.
                camera.transform.position = new Vector3(1.5f, camera.transform.position.y, camera.transform.position.z);
                if (camera.GetComponent<AudioListener>() == null) camera.gameObject.AddComponent<AudioListener>(); // 단독 씬 — Core 없음

                GameObject controllerGo = GreyboxStageBuilder.CreateEmpty("ArtTestController", Vector3.zero);
                ArtTestController controller = controllerGo.AddComponent<ArtTestController>();
                GreyboxStageBuilder.SetReference(controller, "_camera", camera);
                GreyboxStageBuilder.SetReference(controller, "_sun", sun);
            }

            // ── 안내 (화면 좌상 월드 텍스트 대신 오버레이 캔버스 한 줄) ──
            if (font != null)
            {
                GameObject canvasGo = new GameObject("__gb_HelpCanvas");
                Canvas canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                TMP_Text help = new GameObject("Help").AddComponent<TextMeshProUGUI>();
                help.transform.SetParent(canvasGo.transform, false);
                help.font = font;
                help.fontSize = 26f;
                help.color = Color.white;
                help.alignment = TextAlignmentOptions.TopLeft;
                help.text = "늦지마 아트 테스트 — ←→/A·D 이동 · T 낮밤 토글  (반입 에셋 " + entries.Count + "종)";
                RectTransform rect = help.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.offsetMin = new Vector2(16f, -52f); rect.offsetMax = new Vector2(-16f, -8f);
            }

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log("[ArtTest] 진열 " + entries.Count + "종 조립 완료.");
        }

        /// <summary>아트 테스트 씬 단독 WebGL 빌드 — Builds/ArtTestWebGL (배포는 gh-pages /art-test/).</summary>
        [MenuItem("DontLate/Build/Art Test WebGL Build", priority = 31)]
        public static void BuildArtTestWebGL()
        {
            BuildArtTestScene();
            var options = new BuildPlayerOptions
            {
                scenes = new[] { SCENE_PATH },
                locationPathName = "Builds/ArtTestWebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[ArtTest] WebGL 빌드 결과: " + report.summary.result + " · " + report.summary.totalSize / (1024 * 1024) + "MB");
        }
    }
}
