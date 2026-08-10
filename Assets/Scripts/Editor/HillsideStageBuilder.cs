using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// Hillside.unity (S-049 → S-051 달동네 → **S-129 유선형 산 개편**) — 언덕주택가:
    /// 무대 전체가 <b>hill.fbx 한 덩어리</b>다(남규님 블렌더 제작). 좌우 대칭 —
    /// 평지(x −20~2) → 완만한 상승 → 정상(x 31.9 · y 11.1) → 대칭 하강 → 평지(x 66~84). 최대 경사 27°.
    /// 스위치백 3굽이·턴패드·옹벽·긴 계단은 폐기(남규님 반려 — S-129 발주).
    /// 배치물은 전부 <see cref="GroundY"/>로 지형을 찍어 앉힌다 — 좌표 손기입 금지. 멱등(__gb_ Clear).
    /// </summary>
    public static class HillsideStageBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/Hillside.unity";
        private const string UPHILL_SET_PREFAB = "Assets/Prefabs/Hand/set_hillside_uphill.prefab";

        // S-226 ① — 오르막이 쓰는 아트 흙 머티리얼. 지면도 같은 걸 써야 경계가 안 드러난다.
        private const string ART_GROUND_MAT = "Assets/Art/Materials/dirt-road.mat";

        // S-214 ③ — 남규님이 준 오르막 지형 수치. `__gb_Hill`을 대신할 몸집이라 z를 늘려 깔아 준다.
        private const float UPHILL_SCALE_Z = 5.9f;
        private const float UPHILL_POSITION_Z = 0f;

        // S-282 — 남규님 손조정(2026-08-10): 산 아래 평지 판 크기(x만 12 → 11.8).
        private static readonly Vector3 BASE_GROUND_SCALE = new Vector3(11.8f, 1f, 1.2f);

        // S-281 — 남규님 손조정(2026-08-10): 전경 지면 판.
        private static readonly Vector3 FRONT_GROUND_POSITION = new Vector3(37.6f, -0.08f, -17f);
        private static readonly Vector3 FRONT_GROUND_SCALE = new Vector3(129.43f, 0.1f, 46f);

        private const float LANE_HALF_Z = 2.6f;   // 걷기 허용 폭 (능선 위)
        private const float BACK_ROW_Z = 3.3f;    // 능선 뒤편 — 판잣집·데코 줄 (레인 밖)

        private static Material _dirtMat;
        private static Material _wallMat;

        [MenuItem("DontLate/Build/Hillside Stage", priority = 15)]
        public static void BuildHillsideStage()
        {
            Scene scene;
            if (System.IO.File.Exists(SCENE_PATH))
            {
                scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }
            else
            {
                // 최초 실행 — 카메라·라이트 포함 기본 씬으로 생성 (Apartment 선례).
                scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, SCENE_PATH);
            }
            // S-226 ① — **지면에 손으로 입힌 머티리얼을 챙겨 둔다.** Clear가 지우기 전에.
            // 담기 도구(`② 현재 배치 저장`)는 `__gb_ArtBackdrop` 안쪽만 담는다 — 빌더가 매 조립마다
            // 새로 만드는 지면은 그 밖이라, 남규님이 입힌 흙 머티리얼이 재조립마다 그레이박스
            // 기본값(`GB_HillDirt`)으로 되돌아갔다. 배치를 보존하는 S-217 ①과 같은 처방이다.
            Material keptGroundMat = FindGroundMaterial(scene);

            GreyboxStageBuilder.Clear();
            StripHandPlacedHill(); // Clear는 __gb_만 지운다 — 손으로 놓은 "hill"이 남으면 지형이 겹친다
            EnsureUphillSet(scene); // S-183 — 민지님 수제 오르막 세트 (병합에서 유실된 것 복원)

            var (gameState, tuning, _) = GreyboxStageBuilder.GetOrCreateStageData();

            // S-226 ① — 손으로 입힌 게 있으면 그것이 이긴다. 없으면 아트 흙 머티리얼(오르막과 같은 것),
            // 그것도 없으면 종전 그레이박스. 우선순위를 이 한 줄에 모아 둔다.
            _dirtMat = keptGroundMat
                ?? AssetDatabase.LoadAssetAtPath<Material>(ART_GROUND_MAT)
                ?? GreyboxStageBuilder.GetOrCreateMaterial("HillDirt", new Color(0.43f, 0.35f, 0.26f), false);
            _wallMat = GreyboxStageBuilder.GetOrCreateMaterial("HillWall", new Color(0.48f, 0.42f, 0.33f), false);

            // ── 지형 ─────────────────────────────────────────────
            // 산 아래를 받치는 평지(들머리·날머리 바깥과 능선 뒤편). 산의 평지 구간과 같은 y0라
            // Z파이팅을 피해 살짝 내려 깐다.
            GameObject baseGround = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Plane, "BaseGround",
                new Vector3(32f, -0.02f, 0f));
            baseGround.transform.localScale = BASE_GROUND_SCALE; // S-282 — 남규님 손조정
            baseGround.GetComponent<Renderer>().sharedMaterial = _dirtMat;
            baseGround.layer = GreyboxStageBuilder.LAYER_GROUND;

            // S-214 ③ — **`__gb_Hill` 생산 중단**(남규님 지시). 지형 소유가 빌더의 회색 산에서
            // 민지님 오르막 세트로 넘어간다.
            //
            // 그래서 **배경 세트를 여기로 끌어올린다**. 종전엔 배치가 다 끝난 뒤(플레이어·카메라 절)에
            // 깔았는데, 이제 밟을 지형이 그 세트 안에 있다 — 늦게 깔면 이 아래 GroundY 레이캐스트가
            // 때릴 게 BaseGround(y≈0)뿐이라 산비탈 배치물이 전부 평지로 주저앉는다.
            // 배경을 먼저 세우는 것 자체는 원래 규약이기도 하다(S-199 주석).
            ArtBackdropKit.Build(ArtBackdropKit.Hillside); // S-180 ② — 아트 세트 소켓(프리팹 없으면 무시)
            TuneUphill(scene);                            // 오르막을 지형으로 — 콜라이더 on + 수치

            // S-212(정수님) — 산 아래 평지가 z −6에서 끔겨 화면 하단 40%가 하늘이었다.
            // 세트 배치 뒤에 불러 아트판 바닥(세트의 __gb_BaseGround)의 머티리얼·바운즈를 이어 붙인다.
            // ⚠ 호출 위치를 지형 절로 올렸다(S-214에서 배경 조립이 앞으로 이동) — 원래 자리엔 이제 세트가 없다.
            // S-281 — 남규님이 씬에서 맞춘 전경 판 치수를 정본으로 승격한다.
            // 자동 계산(원본 바운즈 기준)은 아트 세트가 오른쪽으로 더 넓어진 것을 못 따라가,
            // 재조립하면 우측 끝이 다시 잘렸다.
            GameObject groundFront = GreyboxStageBuilder.ExtendGroundForward("BaseGround", -40f);
            if (groundFront != null)
            {
                groundFront.transform.position = FRONT_GROUND_POSITION;
                groundFront.transform.localScale = FRONT_GROUND_SCALE;
            }
            Physics.SyncTransforms(); // 이 아래 배치는 전부 GroundY 레이캐스트에 의존한다

            // ── 달동네 판잣집 — S-206에서 **빌더 생산 중단**(남규님 지시). ──
            // 능선 뒤편 실루엣은 이제 민지님 세트가 담당한다(blue_house·mint_house·pink_korea_house·
            // old_stair 등). 빌더의 회색 박스 7채는 같은 능선에 겹쳐 서서 아트 실루엣을 가렸다.
            // 되살릴 일이 있으면 git 이력에 원 좌표가 남아 있다 — 죽은 코드로 들고 있지 않는다.

            // ── 걷기 볼륨 — 능선 위만. z는 산 폭(±4)보다 좁게 잡아 옆으로 떨어지지 않게 한다 ──
            GameObject volume = GreyboxStageBuilder.CreateEmpty("Walkable", Vector3.zero);
            BoxCollider walkable = volume.AddComponent<BoxCollider>();
            walkable.isTrigger = true;
            walkable.size = new Vector3(96f, 28f, LANE_HALF_Z * 2f);
            walkable.center = new Vector3(28f, 12f, 0f); // x −20~76 · y −2~26
            volume.AddComponent<WalkableVolume>();

            // ── 스포너 (밴드별 앵커 — floor 2=오르막 중턱, 3=정상, 4=내리막 중턱) ──
            AttachSpawner(gameState);

            // S-052 ②③ — 들머리 평지 행인 2 + 심부름 할머니(평지 → 정상: 진짜 등반 심부름이 된다).
            NpcBuildKit.BuildPedestrian("Walker_A", OnGround(-12f, 2.0f), new Color(0.45f, 0.52f, 0.62f), 5f);
            NpcBuildKit.BuildPedestrian("Walker_B", OnGround(-6f, 2.4f), new Color(0.60f, 0.48f, 0.40f), 6f);
            // S-226 ② — 심부름 할머니 철거(남규님 지시). 빌라촌(S-222)에 이어 언덕도 뺀다.
            //   ⚠ 이로써 심부름 퀘스트가 **게임에서 사라진다** — 빌라촌·언덕이 마지막 두 곳이었다.
            //   되살릴 자리가 정해지면 좌표만 주면 된다.

            // S-054b 엣지 워크 — 왼쪽 끝 = 이전 동네(먹자골목).
            // S-186 ② — 언덕주택가가 3번째가 되면서 **오른쪽에 Next(아파트단지)가 생겼다**.
            // 종전엔 종점이라 게이트가 하나뿐이었다. 산 능선은 x 66~84가 평지라 날머리에 세운다.
            EdgeGateBuildKit.BuildGate("EdgeGate_Prev", OnGround(-19.5f, 0f),
                DontLate.DistrictEdgeGate.Direction.Prev, gameState);
            EdgeGateBuildKit.BuildGate("EdgeGate_Next", OnGround(76f, 0f),
                DontLate.DistrictEdgeGate.Direction.Next, gameState);

            // S-059 — 달동네 고양이 (정상 마당 · 데려오면 집에 정착).
            // S-279 ② — 고양이 철거(남규님 지시). 빌더에서 빼야 재조립에도 안 돌아온다.
            //   ⚠ 집 고양이(S-059)의 유일한 입수처였다 — `HomeCat`은 데려온 뒤에만 활성이므로
            //   이 씬에서 만나는 경로가 사라지면 그 콘텐츠도 함께 잠긴다. 되살리려면 이 줄만 복구하면 된다.
            //   BuildCat(gameState, OnGround(34f, -1.2f));

            // ── 플레이어·카메라(Y 팔로우) ────────────────────
            // (배경 세트는 S-214 ③에서 지형 절로 올라갔다 — 여기서 다시 깔면 두 벌이 선다.)
            GreyboxStageBuilder.BuildPlayer(gameState, tuning);
            GameObject player = GameObject.Find("__gb_Player");
            if (player != null) player.transform.position = OnGround(-16f, 0f, 0.1f);

            // S-115 — 실물 데코: 들머리·날머리 평지에 한옥 (산비탈은 판잣집 몫).
            GreyboxStageBuilder.PlaceCatalog("old_korea_house", OnGround(-17f, BACK_ROW_Z + 0.6f));
            // S-206 — `retro_korean_house`는 빌더가 놓지 않는다(남규님 지시). 민지님 세트에
            // 같은 집이 이미 서 있는데(평이름 `retro_korean_house`), 빌더 사본은 `__gb_Deco_` 접두어가
            // 붙어 이름이 어긋나 자동 교체 규칙(S-188)에 걸리지 않았다 — 그래서 두 채가 겹쳐 섰다.
            // S-207 — `red_korean_house`도 빌더가 놓지 않는다(남규님 지시). S-206에선 아트 사본이
            // 빈 껍데기라 빌더판을 남겼는데, 남규님이 그 판단을 뒤집었다 — 날머리(x70)가 비더라도
            // 겹쳐 서는 것보단 낫다. 채우는 건 아트 몫이다.
            GreyboxStageBuilder.PlaceCatalog("Pot_unity", OnGround(-11.5f, 2.4f));
            GreyboxStageBuilder.PlaceCatalog("black_Trash_unity", OnGround(4f, 2.4f));
            GreyboxStageBuilder.PlaceCatalog("bycle", OnGround(-4f, 2.4f), 15f);

            GreyboxStageBuilder.BuildGroundMist();
            GreyboxStageBuilder.BuildStarField();
            GreyboxStageBuilder.BuildPostVolume();
            GreyboxStageBuilder.ConfigureCamera();
            GreyboxStageBuilder.AttachCameraFollow();
            Camera camera = Camera.main;
            if (camera != null && camera.TryGetComponent(out CameraFollowX follow))
            {
                SerializedObject serialized = new SerializedObject(follow);
                serialized.FindProperty("_followY").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            // S-206 — 조립이 끝난 뒤 한 번 더 훑는다. `ArtBackdropKit.Build`는 카탈로그
            // 데코보다 **먼저** 도는데, 민지님 세트에 같은 데코가 담겨 있어(`__gb_Deco_bycle` 등 6종)
            // 교체 시점엔 빌더 사본이 아직 없다 — 그래서 사냥을 빠져나가 같은 자리에 두 벌이 섰다.
            // District가 S-199에서 겪은 것과 같은 결함이고, 해법도 같다(멱등이라 몇 번 불러도 안전).
            ArtBackdropKit.SweepBuilderDuplicates();

            // S-214 ③ — 스윕이 **배경층 콜라이더를 일괄로 끄기 때문에**(통행 판정에 끼면 안 된다는
            // S-119 ① 규약) 방금 켜 둔 오르막 콜라이더도 같이 꺼진다(실측: MC=False로 되돌아감).
            // 오르막은 배경이 아니라 밟는 지형이므로 스윕 뒤에 한 번 더 세운다. 멱등이라 안전하다.
            TuneUphill(scene);
            BuildHillsideStreetLamps(); // S-240 — 오르막 콜라이더를 세운 뒤라야 지면 스냅이 먹는다

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log("[Hillside] 유선형 산 무대 조립 완료 — 정상 y" + GroundY(31.9f).ToString("F2")
                + " · 최대 경사 27° · 배치물 지면 스냅 (S-129).");
        }

        // ── 지형 ────────────────────────────────────────────────

        /// <summary>
        /// S-226 ① — 지금 씬의 지면이 무슨 머티리얼을 쓰고 있나. Clear 전에 불러야 한다.
        /// 그레이박스 기본값이면 "손댄 적 없음"으로 보고 null을 돌려준다 —
        /// 그래야 아트 머티리얼로 승격할 수 있다.
        /// </summary>
        private static Material FindGroundMaterial(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "__gb_BaseGround") continue;
                Renderer renderer = root.GetComponentInChildren<Renderer>(true);
                Material material = renderer != null ? renderer.sharedMaterial : null;
                // S-281 — 종전엔 `GB_`로 시작하면 통째로 걸렀다(그레이박스 기본값 제외 의도).
                // 그런데 남규님이 고른 `GB_HillAsphalt`도 그 접두어라 함께 걸러져, 재조립마다
                // 흙 머티리얼로 되돌아갔다. **빌더가 만드는 기본값 하나만** 제외한다.
                if (material == null || material.name == "GB_HillDirt") return null;
                return material;
            }
            return null;
        }

        /// <summary>손으로 씬에 끌어다 놓은 hill 인스턴스 제거 — Clear()가 __gb_ 접두어만 지우기 때문.</summary>
        private static void StripHandPlacedHill()
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go == null || go.transform.parent != null) continue;
                if (go.name != "hill" && !go.name.StartsWith("hill ")) continue;
                Undo.DestroyObjectImmediate(go);
            }
        }

        /// <summary>
        /// 민지님 수제 오르막 세트를 꽂는다. S-183 — 이 메서드는 민지님이 PR#34에 넣었는데
        /// 본인이 main을 병합할 때(49c6b67a) 충돌을 main 쪽으로 해소하며 **통째로 지워졌다**.
        /// 프리팹만 남고 꽂아줄 코드가 없어 배치가 게임에 안 나오는 상태였다 — 원문 그대로 복원.
        /// </summary>
        private static void EnsureUphillSet(Scene scene)
        {
            // S-206 — **배경 세트가 이미 오르막을 품고 있으면 빌더는 손을 뗀다(아트 우선).**
            // 민지님이 `set_hillside`를 묶을 때 오르막 세트를 통째로 안에 넣었는데, 빌더도 따로
            // 한 벌을 세워 **원점에 정확히 두 벌**이 겹쳐 있었다(실측: 둘 다 (0,0,0)).
            // 이름이 `__gb_` 접두어가 아니라 S-188 자동 교체 규칙에 안 걸리므로 여기서 막는다.
            //
            // 이 검사가 아래 "이미 있으면 통과"보다 **먼저** 와야 한다 — Clear는 `__gb_`만 지우므로
            // 지난 조립이 남긴 빌더 사본이 씬에 그대로 살아 있고, 순서가 반대면 그놈 때문에
            // 조기 return 해서 영영 안 지워진다(실측으로 확인한 함정).
            bool setOwnsUphill = SetPrefabContainsUphill();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "set_hillside_uphill") continue;
                if (!setOwnsUphill) return; // 세트에 없으면 씬에 선 이 한 벌이 정본이다 (손질은 지형 절에서 한 번에)
                Undo.DestroyObjectImmediate(root);
            }

            if (setOwnsUphill)
            {
                Debug.Log("[Hillside] 오르막 세트가 배경 세트에 내장됨 — 빌더 사본 생략(아트 우선).");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UPHILL_SET_PREFAB);
            if (prefab == null)
            {
                Debug.Log("[Hillside] set_hillside_uphill 미배치 — 수제 오르막 세트 생략.");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null) return;
            instance.name = "set_hillside_uphill";
            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = GreyboxStageBuilder.LAYER_GROUND;
        }

        /// <summary>
        /// S-214 ③ — 오르막 세트의 `uphill`을 **지형으로 쓸 수 있게** 손질한다(남규님 수치).
        /// `__gb_Hill`을 걷어낸 자리를 이 메시가 대신 받으므로 콜라이더가 켜져 있어야 한다 —
        /// 꺼져 있으면 GroundY 레이캐스트가 전부 BaseGround(y≈0)를 때려 무대가 평지가 된다.
        /// 프리팹이 아니라 **씬 인스턴스에** 건다: 민지님이 프리팹을 다시 반입해도 이 손질은 유지된다.
        /// </summary>
        private static void TuneUphill(Scene scene)
        {
            // 오르막은 두 곳 중 하나에 있다: 독립 세트(`set_hillside_uphill`) 또는 배경 세트
            // (`__gb_ArtBackdrop`) 안. 민지님이 배경에 품어 보낸 이후로는 후자다(S-206).
            // 어디 있든 찾도록 씬 전체에서 이름으로 집는다.
            Transform uphill = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == "uphill") { uphill = child; break; }
                if (uphill != null) break;
            }

            if (uphill == null)
            {
                Debug.LogWarning("[Hillside] 씬에 `uphill`이 없다 — 지형 콜라이더 손질 생략(무대가 평지가 된다).");
                return;
            }

            Vector3 scale = uphill.localScale;
            uphill.localScale = new Vector3(scale.x, scale.y, UPHILL_SCALE_Z);
            Vector3 position = uphill.localPosition;
            uphill.localPosition = new Vector3(position.x, position.y, UPHILL_POSITION_Z);

            MeshCollider collider = uphill.GetComponent<MeshCollider>();
            if (collider == null) collider = uphill.gameObject.AddComponent<MeshCollider>();
            collider.enabled = true;

            Debug.Log("[Hillside] 오르막 지형 손질 — scale.z " + UPHILL_SCALE_Z + " · position.z "
                + UPHILL_POSITION_Z + " · MeshCollider on (S-214 ③).");
        }

        /// <summary>배경 세트 프리팹 안에 오르막 세트가 들어 있는가 (S-206 · 중복 방지).</summary>
        private static bool SetPrefabContainsUphill()
        {
            GameObject setPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArtBackdropKit.Hillside.PrefabPath);
            if (setPrefab == null) return false;

            foreach (Transform child in setPrefab.GetComponentsInChildren<Transform>(true))
                if (child.name == "set_hillside_uphill") return true;
            return false;
        }

        /// <summary>지형 표면 높이. Ground 레이어만 본다 — 이미 놓인 데코·플레이어에 걸리지 않는다.</summary>
        /// <summary>
        /// S-240 — 언덕길에도 가로등을 세운다. 걷기 영역이 x −20~76으로 District(약 40)의 두 배 넘게
        /// 길어 같은 8m 간격을 쓰면 20개가 넘는다 — 포인트 라이트가 그만큼 늘면 WebGL이 감당하지
        /// 못한다. 14m 간격 좌우 엇갈림으로 13개만 세운다.
        /// y는 `PlaceStreetLamps`가 Ground 레이어에 쏴서 잡는다(경사라 평지 y를 쓰면 뜬다).
        /// </summary>
        private static void BuildHillsideStreetLamps()
        {
            float[] front = { -12f, 2f, 16f, 30f, 44f, 58f, 70f };
            float[] back = { -5f, 9f, 23f, 37f, 51f, 65f };
            var spots = new System.Collections.Generic.List<(Vector3, float)>();
            foreach (float x in front) spots.Add((new Vector3(x, 0f, -2.2f), 0f));
            foreach (float x in back) spots.Add((new Vector3(x, 0f, 2.2f), 180f));
            GreyboxStageBuilder.PlaceStreetLamps(spots.ToArray());
        }

        private static float GroundY(float x, float z = 0f)
        {
            int mask = 1 << GreyboxStageBuilder.LAYER_GROUND;
            if (Physics.Raycast(new Vector3(x, 60f, z), Vector3.down, out RaycastHit hit, 120f,
                    mask, QueryTriggerInteraction.Ignore))
                return hit.point.y;
            return 0f;
        }

        private static Vector3 OnGround(float x, float z, float lift = 0f)
            => new Vector3(x, GroundY(x, z) + lift, z);

        // ── 판잣집: 몸통 + 슬레이트 지붕(살짝 기울임) ──
        // S-059 고양이 — 작은 주황 덩어리 + 상호작용 트리거.
        private static void BuildCat(GameStateSO gameState, Vector3 position)
        {
            GameObject root = GreyboxStageBuilder.CreateEmpty("Cat", position);
            Material fur = GreyboxStageBuilder.GetOrCreateMaterial("CatFur", new Color(0.85f, 0.55f, 0.25f), false);
            Material highlight = GreyboxStageBuilder.GetOrCreateHighlightMaterial();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.22f, 0.26f, 0.22f);
            Renderer bodyRenderer = body.GetComponent<Renderer>();
            bodyRenderer.sharedMaterial = fur;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0.22f, 0.3f, 0f);
            head.transform.localScale = Vector3.one * 0.22f;
            head.GetComponent<Renderer>().sharedMaterial = fur;

            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.6f;
            trigger.center = new Vector3(0f, 0.25f, 0f);

            DialogueScenarioSO meet = NpcBuildKit.GetOrCreateScenario("Scenario_Cat_Meet",
                ("고양이", "...야옹. (경계하다가 코를 킁킁거린다 — 따라오고 싶어 하는 눈치다)"),
                ("고양이", "야옹! (먼저 집 쪽으로 뛰어갔다. 사료를 챙겨줘야 할 것 같다 — 쇼핑 앱)"));

            HillsideCat cat = root.AddComponent<HillsideCat>();
            GreyboxStageBuilder.SetReference(cat, "_gameState", gameState);
            GreyboxStageBuilder.SetReference(cat, "_meetScenario", meet);
            GreyboxStageBuilder.SetReference(cat, "_highlightRenderer", bodyRenderer);
            GreyboxStageBuilder.SetReference(cat, "_normalMaterial", fur);
            GreyboxStageBuilder.SetReference(cat, "_highlightMaterial", highlight);
        }

        private static void AttachSpawner(GameStateSO gameState)
        {
            GameObject go = new GameObject("__gb_CargoSpawner");
            DistrictCargoSpawner spawner = go.AddComponent<DistrictCargoSpawner>();

            Transform boxOrigin = GreyboxStageBuilder.CreateEmpty("BoxOrigin", OnGround(-17f, -1.2f)).transform;
            // floor 2=오르막 중턱 · 3=정상 · 4=내리막 중턱 — 배송이 "올라갔다 내려오는" 리듬이 된다.
            var anchors = new Transform[3];
            anchors[0] = GreyboxStageBuilder.CreateEmpty("BeaconAnchor_Up", OnGround(14f, -0.6f)).transform;
            anchors[1] = GreyboxStageBuilder.CreateEmpty("BeaconAnchor_Top", OnGround(29f, 0.4f)).transform;
            anchors[2] = GreyboxStageBuilder.CreateEmpty("BeaconAnchor_Down", OnGround(48f, -0.6f)).transform;

            SerializedObject serialized = new SerializedObject(spawner);
            serialized.FindProperty("_gameState").objectReferenceValue = gameState;
            serialized.FindProperty("_boxVisualPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Auto/prop_box_parcel.prefab");
            serialized.FindProperty("_beaconPrefab").objectReferenceValue = GreyboxStageBuilder.GetOrCreateBeaconPrefab();
            serialized.FindProperty("_boxHighlight").objectReferenceValue =
                GreyboxStageBuilder.GetOrCreateHighlightMaterial();
            serialized.FindProperty("_boxFallback").objectReferenceValue =
                GreyboxStageBuilder.GetOrCreateMaterial("Box", GreyboxStageBuilder.ParseColor("#ff9f45"), false);
            serialized.FindProperty("_tuning").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TuningConfigSO>("Assets/Data/Tuning.asset");
            serialized.FindProperty("_boxOrigin").objectReferenceValue = boxOrigin;
            // S-129 — 비탈에서는 층 앵커의 슬롯 간격(+5u X)이 지면을 벗어난다. 스냅 켠다.
            serialized.FindProperty("_snapBeaconsToGround").boolValue = true;
            SerializedProperty anchorsProp = serialized.FindProperty("_floorBeaconAnchors");
            anchorsProp.arraySize = anchors.Length;
            for (int i = 0; i < anchors.Length; i++)
                anchorsProp.GetArrayElementAtIndex(i).objectReferenceValue = anchors[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
