using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// S-139 — 타이틀(Main) 배경을 살아 있는 거리로 조립한다.
    /// 남규님 요구: "Main씬에 District 2 배치와 동일하게 캐릭터 좌우로 뛰어다니는거
    /// + 날씨랑 시간 바뀌는거 그대로."
    ///
    /// 구성 = District와 같은 지면·도로 + `ArtBackdropKit.District` 배치(민지님 확정분 재사용)
    /// + 달리는 배달원 + `TitleShowcaseDirector`(시간대·날씨 순환).
    ///
    /// ⚠ **UI 캔버스는 건드리지 않는다** — `__ui_FlowCanvas`(로고·시작 버튼)와 `__ui_EnsureCore`는
    /// SceneFlowUIBuilder 소유다. 여기서는 3D 배경만 세우고 그 둘은 보존한다(빌더 간 경계).
    /// 실행 순서 무관: 이 빌더를 먼저 돌리든 UI를 먼저 돌리든 결과가 같다.
    /// </summary>
    public static class MainTitleStageBuilder
    {
        private const string MAIN_PATH = "Assets/Scenes/Main.unity";
        private const string STAGE_ROOT = "__gb_TitleStage";
        private const string COURIER_FBX = "Assets/Art/Characters/chr_courier.fbx";
        private const string COURIER_AC = "Assets/Art/Characters/AC_chr_courier.controller";

        [MenuItem("DontLate/Build/Main Title Stage (타이틀 배경)", priority = 16)]
        public static void BuildMainTitleStage()
        {
            Scene scene = EditorSceneManager.OpenScene(MAIN_PATH, OpenSceneMode.Single);

            // 멱등 — 이전 조립물만 지운다. UI 캔버스·EnsureCore는 남긴다.
            DestroyRoot(STAGE_ROOT);
            // 유니티 기본 씬 잔재(Plane·Capsule)와 자체 태양을 정리한다.
            // 태양은 Core 소유(D-021) — Main이 자기 Directional을 들고 있으면 낮밤이 안 먹는다.
            DestroyRoot("Plane");
            DestroyRoot("Capsule");
            DestroyRoot("Directional Light");

            GameObject stage = new GameObject(STAGE_ROOT);

            BuildGroundAndRoad(stage.transform);
            BuildStreetLamps(stage.transform);
            ArtBackdropKit.Build(ArtBackdropKit.District, stage.transform);
            GameObject runner = BuildRunner(stage.transform);
            AttachDirector(stage, runner);
            ConfigureTitleCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MAIN_PATH);
            Debug.Log("[타이틀무대] Main 배경 조립 완료 — District 배치 + 왕복 러너 + 시간·날씨 순환. "
                + "UI 캔버스는 보존했다(SceneFlowUIBuilder 소유).");
        }

        private static void BuildGroundAndRoad(Transform parent)
        {
            Material groundMat = GreyboxStageBuilder.GetOrCreateMaterial("Ground", new Color(0.24f, 0.24f, 0.26f), false);
            Material laneMat = GreyboxStageBuilder.GetOrCreateMaterial("Lane", new Color(0.34f, 0.33f, 0.30f), false);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "TitleGround";
            ground.transform.SetParent(parent, false);
            ground.transform.localScale = new Vector3(12f, 1f, 8f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;
            Object.DestroyImmediate(ground.GetComponent<Collider>());

            GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lane.name = "TitleLane";
            lane.transform.SetParent(parent, false);
            lane.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            lane.transform.localScale = new Vector3(40f, 0.04f, 6f);
            lane.GetComponent<Renderer>().sharedMaterial = laneMat;
            Object.DestroyImmediate(lane.GetComponent<Collider>());
        }

        /// <summary>
        /// 가로등 — **밤 연출의 핵심**이라 빼면 안 된다. 없이 돌려보니 저녁·밤 구간에서 거리가
        /// 통째로 검게 죽었다(캡처 확인). 설계상 밤은 Directional을 죽이고 포인트 라이트를
        /// 부각하는 구성이라(ARCHITECTURE §3), 광원이 없으면 낮밖에 볼 게 없다.
        /// District와 같은 좌우 2열 배치를 쓴다.
        /// </summary>
        private static void BuildStreetLamps(Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hand/StreetLamp.prefab");
            if (prefab == null)
            {
                Debug.LogWarning("[타이틀무대] StreetLamp.prefab 미발견 — 밤 구간이 어두워진다. "
                    + "DontLate/Build로 District를 한 번 조립하면 생성된다.");
                return;
            }

            float[] front = { -16f, -8f, 5.4f, 12f, 18f };
            float[] back = { -12f, -5.4f, 12f };
            int index = 1;
            foreach (float x in front) PlaceLamp(prefab, parent, new Vector3(x, 0f, -2.4f), 0f, ref index);
            foreach (float x in back) PlaceLamp(prefab, parent, new Vector3(x, 0f, 2.4f), 180f, ref index);
        }

        private static void PlaceLamp(GameObject prefab, Transform parent, Vector3 position, float yaw, ref int index)
        {
            GameObject lamp = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            lamp.name = "TitleLamp_" + index.ToString("00");
            lamp.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            index++;
        }

        /// <summary>
        /// 달리는 배달원. 플레이어가 아니라 연출 인형이라 CharacterController·입력을 붙이지 않는다.
        /// 전고 1.8u 정규화 + 발끝 지면 정렬은 GreyboxStageBuilder의 플레이어 비주얼과 같은 규격.
        /// </summary>
        private static GameObject BuildRunner(Transform parent)
        {
            GameObject root = new GameObject("TitleRunner");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(-6f, 0.05f, -1.6f);

            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(COURIER_FBX);
            if (fbx == null)
            {
                Debug.LogWarning("[타이틀무대] chr_courier.fbx 미발견 — 러너 비주얼 스킵(구동부는 유지).");
                return root;
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
            visual.name = "CourierVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            Bounds bounds = RenderBounds(visual);
            if (bounds.size.y > 0.001f)
                visual.transform.localScale = Vector3.one * (1.8f / bounds.size.y);
            bounds = RenderBounds(visual);
            visual.transform.position += Vector3.up * (root.transform.position.y - bounds.min.y);

            Animator animator = visual.GetComponentInChildren<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(COURIER_AC);
            animator.applyRootMotion = false;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(COURIER_FBX))
                if (asset is Avatar avatar) { animator.avatar = avatar; break; }

            return root;
        }

        private static void AttachDirector(GameObject stage, GameObject runner)
        {
            var director = stage.AddComponent<TitleShowcaseDirector>();
            // ⚠ SerializedObject 경유로 주입한다 — 씬을 저장하는 빌더에서 AddComponent 직후
            // 리플렉션 대입을 하면 오브젝트 참조가 {fileID: 0}으로 유실되는 사례가 있었다
            // (CODE_RULES §6 실수→규칙 2026-07-20). SerializedObject는 그 경로를 타지 않는다.
            var so = new SerializedObject(director);
            so.FindProperty("_runner").objectReferenceValue = runner != null ? runner.transform : null;
            so.FindProperty("_runnerAnimator").objectReferenceValue =
                runner != null ? runner.GetComponentInChildren<Animator>() : null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>District와 같은 앵글(FOV 22·하향 10°) — 배치가 같으니 프레이밍도 같아야 한다.</summary>
        private static void ConfigureTitleCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject go = new GameObject("Main Camera") { tag = "MainCamera" };
                camera = go.AddComponent<Camera>();
            }
            camera.orthographic = false;
            camera.fieldOfView = 22f;
            camera.allowHDR = true;
            camera.farClipPlane = 500f;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 8.1f, -40.4f), Quaternion.Euler(10f, 0f, 0f));
        }

        private static Bounds RenderBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void DestroyRoot(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go != null && go.transform.parent == null) Object.DestroyImmediate(go);
        }
    }
}
