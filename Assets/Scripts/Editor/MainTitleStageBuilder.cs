using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// S-144 — 타이틀(Main) 배경을 **District와 같은 조립**으로 세운다.
    /// 남규님 요구: "Main 씬 오브젝트 배치 District씬이랑 동일하게" + "게임 시작시 하늘에서
    /// 카메라 수직으로 현재 위치까지 천천히 떨어지는 카메라 워크".
    ///
    /// 핵심은 **베끼지 않고 같은 빌더를 부르는 것**이다. 종전(S-139)엔 지면·도로·가로등·배경을
    /// 여기서 따로 조립해 District와 구성이 갈렸다(안개·별·달·해·횡단보도·신호등·행인·소품
    /// 슬롯이 통째로 빠져 있었다). `DistrictSceneBuilder.BuildStage`가 씬 경로를 받으므로
    /// Main 경로로 그대로 호출한다 — 앞으로 District가 바뀌면 Main도 같이 바뀐다.
    ///
    /// 그 위에 타이틀 전용만 얹는다: 플레이 전용 오브젝트 제거 · 달리는 배달원 ·
    /// 시간대/날씨 순환 · 카메라 강하.
    ///
    /// ⚠ UI 캔버스(`__ui_FlowCanvas`·`__ui_EnsureCore`)는 건드리지 않는다 — SceneFlowUIBuilder
    /// 소유다. `GreyboxStageBuilder.Clear()`가 `__gb_` 접두어만 지우므로 안전하게 살아남는다.
    /// </summary>
    public static class MainTitleStageBuilder
    {
        private const string MAIN_PATH = "Assets/Scenes/Main.unity";
        private const string STAGE_ROOT = "__gb_TitleStage";
        private const string COURIER_FBX = "Assets/Art/Characters/chr_courier.fbx";
        private const string COURIER_AC = "Assets/Art/Characters/AC_chr_courier.controller";
        // S-195 — 러너가 드는 상자. 플레이어가 드는 것과 같은 프리팹이라 룩이 어긋나지 않는다.
        private const string PARCEL_PREFAB = "Assets/Prefabs/Auto/prop_box_parcel.prefab";
        private const string CLOUD_A = "Assets/Art/Backgrounds/fx_cloud_a.png";
        private const string CLOUD_B = "Assets/Art/Backgrounds/fx_cloud_b.png";

        /// <summary>
        /// 타이틀 화면에 있으면 안 되는 플레이 전용 오브젝트.
        /// District 조립을 그대로 쓰는 대가로 딸려오므로 여기서 걷어낸다.
        /// </summary>
        private static readonly string[] GameplayOnlyRoots =
        {
            "__gb_Player",         // 입력으로 조종되는 플레이어 — 타이틀에선 러너가 대신한다
            "__gb_CargoSpawner",   // 배송 화물 스폰
            "__gb_ErrandGranny",   // 심부름 NPC
            "__gb_EdgeGate_Prev",  // 구역 이동 게이트
            "__gb_EdgeGate_Next",
            "__gb_Door",           // 배송지 문·간판 — 타이틀엔 상호작용 대상이 없다
            "__gb_Sign",
            "SceneLabel_District", // District 조립이 남기는 씬 이름표

            // S-194 — 씬에 굴러다니던 유니티 기본 프리미티브. 언젠가 손으로 얹은 것이 Main.unity에
            // 저장돼 남았고, `GreyboxStageBuilder.Clear()`는 `__gb_` 접두어만 지우므로 재조립을
            // 해도 계속 살아났다(타이틀 화면의 회색 원기둥·바닥판 — 남규님 지적).
            // 다른 씬엔 없다(전 씬 조회 확인) — 여기서만 걷어낸다.
            "Plane",
            "Capsule",
        };

        [MenuItem("DontLate/Build/Main Title Stage (타이틀 배경)", priority = 16)]
        public static void BuildMainTitleStage()
        {
            // ① District와 **같은 조립**을 Main 경로로 돌린다(베끼기 금지 — 이중 관리의 근원).
            DistrictSceneBuilder.BuildStage(MAIN_PATH);

            Scene scene = SceneManager.GetActiveScene();

            // ② 플레이 전용 걷어내기.
            foreach (string name in GameplayOnlyRoots) DestroyRoot(name);

            // ③ 타이틀 전용 얹기.
            GameObject stage = new GameObject(STAGE_ROOT);
            GameObject runner = BuildRunner(stage.transform);
            AttachDirector(stage, runner);
            ConfigureTitleCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MAIN_PATH);
            Debug.Log("[타이틀무대] Main 조립 완료 — District와 동일 구성 + 왕복 러너 · 시간·날씨 순환 "
                + "· 카메라 강하. UI 캔버스는 보존(SceneFlowUIBuilder 소유).");
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

            GreyboxStageBuilder.EnsureIkPass(); // S-195 — 없으면 OnAnimatorIK가 안 불린다
            BuildRunnerParcel(root, animator);
            return root;
        }

        /// <summary>
        /// S-195 — 러너에게 **택배 상자를 들려 보낸다.**
        ///
        /// 타이틀은 이 게임이 무슨 게임인지 3초 안에 말해야 하는 화면인데, 빈손으로 달리면
        /// 그냥 조깅이다. 상자를 들면 실루엣만으로 "배달"이 읽힌다.
        ///
        /// 플레이어의 캐리 경로(PlayerStatusManager)를 쓰지 않는 이유: 러너는 연출 인형이라
        /// 상태·주문 데이터가 없다. 겉모습만 같은 규격(0.7u·바닥 정렬)으로 직접 얹는다.
        /// </summary>
        private static void BuildRunnerParcel(GameObject runner, Animator animator)
        {
            // 앵커 위치는 플레이어와 같은 값 — 손 IK 오프셋이 그 기준으로 잡혀 있다.
            GameObject anchor = new GameObject("CarryAnchor");
            anchor.transform.SetParent(runner.transform, false);
            anchor.transform.localPosition = new Vector3(0f, 1.05f, 0.45f);

            GameObject parcel = AssetDatabase.LoadAssetAtPath<GameObject>(PARCEL_PREFAB);
            if (parcel == null)
            {
                Debug.Log("[타이틀무대] 택배 상자 프리팹 미발견 — 러너는 빈손으로 달린다: " + PARCEL_PREFAB);
                return;
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(parcel, anchor.transform);
            visual.name = "CarriedBox";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            // 캐리 상자 규격: 높이 0.7u 정규화 + 바닥을 앵커 원점에 맞춘다(PlayerStatusManager와 동일).
            Bounds bounds = RenderBounds(visual);
            if (bounds.size.y > 0.001f)
            {
                visual.transform.localScale = Vector3.one * (0.7f / bounds.size.y);
                bounds = RenderBounds(visual);
                visual.transform.position += anchor.transform.position
                    - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }
            foreach (Collider c in visual.GetComponentsInChildren<Collider>(true)) c.enabled = false;

            // 캐리 자세(IsCarrying)는 **디렉터가 플레이에서 세운다** — 애니메이터 파라미터는
            // 에디터에서 넣어도 플레이 시작에 초기화되므로 여기서 켜 봐야 소용없다.
            if (animator != null)
                GreyboxStageBuilder.AttachCarryHandIK(animator.gameObject, anchor.transform);
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

        /// <summary>
        /// District와 같은 앵글(GreyboxStageBuilder.ConfigureCamera가 이미 잡아둔 값)을 그대로 두고,
        /// 강하 연출만 붙인다. **착지점을 따로 적지 않는 것이 요점** — TitleCameraDrop이 씬에
        /// 저장된 카메라 위치를 착지점으로 읽으므로, District 앵글이 바뀌면 자동으로 따라간다.
        /// </summary>
        private static void ConfigureTitleCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("[타이틀무대] 메인 카메라 없음 — 강하 연출 스킵.");
                return;
            }
            camera.farClipPlane = Mathf.Max(camera.farClipPlane, 500f); // 강하 시작 고도에서 원경 유지

            TitleCameraDrop drop = camera.GetComponent<TitleCameraDrop>();
            if (drop == null) drop = camera.gameObject.AddComponent<TitleCameraDrop>();
            BuildIntroClouds(camera, drop);
        }

        private static void BuildIntroClouds(Camera camera, TitleCameraDrop drop)
        {
            Transform old = camera.transform.Find("IntroClouds");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            Texture2D[] cloudTextures =
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(CLOUD_A),
                AssetDatabase.LoadAssetAtPath<Texture2D>(CLOUD_B),
            };

            SerializedObject serialized = new SerializedObject(drop);
            SerializedProperty cloudProp = serialized.FindProperty("_introCloudTextures");
            cloudProp.arraySize = cloudTextures.Length;
            for (int i = 0; i < cloudTextures.Length; i++)
                cloudProp.GetArrayElementAtIndex(i).objectReferenceValue = cloudTextures[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
