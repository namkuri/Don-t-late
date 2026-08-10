using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DontLate.EditorTools
{
    /// <summary>
    /// Core 상주 씬과 콘텐츠 씬 5종을 코드로 조립하는 개발 도구.
    /// 씬 파일은 커밋하지 않으므로 이 빌더가 정본이다. 다시 실행하면 처음부터 새로 조립한다(멱등).
    /// Main.unity(사람 샌드박스)는 열지도 저장하지도 않는다.
    /// </summary>
    public static class CoreSceneBuilder
    {
        private const string SCENES_ROOT = "Assets/Scenes";
        private const string CORE_PATH = SCENES_ROOT + "/Core.unity";
        private const string DATA_ROOT = "Assets/Data";
        private const string FONT_PATH = "Assets/Art/UI/Fonts/DNFBitBitOTF SDF.asset";
        private const string DIALOGUE_FONT_PATH = "Assets/Art/UI/Fonts/Ramche SDF.asset";
        private const string KIOSK_UI_PREFAB_PATH = "Assets/Prefabs/Hand/UI/KioskPanel.prefab";
        private const string INVENTORY_UI_PREFAB_PATH = "Assets/Prefabs/Hand/UI/InventoryPanel.prefab";
        private const string PANEL_UI_ROOT = "Assets/Art/UI/panel/";
        private const string BGM_FOLDER = "Assets/Audio/BGM";
        private const string BGM_LIBRARY_PATH = DATA_ROOT + "/BgmLibrary.asset";
        private static readonly Color AMBER = new Color(1f, 0.624f, 0.271f, 1f); // #ff9f45
        // 경험치 전용 노랑 (S-174 후속) — 앰버(주황기)와 구분되게 더 밝고 노랗다.
        private static readonly Color MASTERY_YELLOW = new Color(1f, 0.85f, 0.2f, 1f); // #ffd933
        private static readonly Color CYAN = new Color(0.208f, 0.878f, 0.784f, 1f); // #35e0c8
        private static readonly Color NAVY = new Color(0.039f, 0.051f, 0.086f, 0.9f); // #0a0d16 반투명

        private const string BLIP_PATH = "Assets/Audio/SFX/sfx_dialogue_blip.wav";
        private const string DIALOGUE_DATA_ROOT = "Assets/Data/Dialogue";
        private const string PARK_SCENARIO_PATH = DIALOGUE_DATA_ROOT + "/Scenario_ParkMalsoon_Intro.asset";

        // S-186 ③ — District 은퇴, Village·FoodStreet 신설 (구역 : 씬 1:1).
        private static readonly string[] ContentSceneNames = { "Home", "Camp", "Travel", "Village", "FoodStreet", "Apartment", "Hillside" };

        // 빌드 세팅 등록 순서 — Core(0) → Main → 콘텐츠 5종. SampleScene·Greybox 제외.
        private static readonly string[] BuildOrder =
        {
            SCENES_ROOT + "/Core.unity",
            SCENES_ROOT + "/Main.unity",
            SCENES_ROOT + "/Home.unity",
            SCENES_ROOT + "/Camp.unity",
            SCENES_ROOT + "/Travel.unity",
            SCENES_ROOT + "/Village.unity",    // S-186 ③ — 빌라촌 (구 District)
            SCENES_ROOT + "/FoodStreet.unity", // S-186 ③ — 먹자골목 (신설)
            SCENES_ROOT + "/Apartment.unity", // S-038
            SCENES_ROOT + "/Hillside.unity",  // S-049
        };

        // ── 메뉴 ─────────────────────────────────────────────

        [MenuItem("DontLate/Build/Core Scene", priority = 10)]
        public static void BuildCoreScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[CoreSceneBuilder] 저장되지 않은 씬이 있어 Core 재조립을 취소했다.");
                return;
            }

            GameStateSO gameState = AssetDatabase.LoadAssetAtPath<GameStateSO>(DATA_ROOT + "/GameState.asset");
            TuningConfigSO tuning = AssetDatabase.LoadAssetAtPath<TuningConfigSO>(DATA_ROOT + "/Tuning.asset");
            if (gameState == null || tuning == null)
            {
                Debug.LogError("[CoreSceneBuilder] GameState.asset / Tuning.asset 을 찾지 못했다.");
                return;
            }

            CleanCoreDuplicatesInMain();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildManagers(gameState, tuning);
            BuildCore(gameState);
            BuildFadeCanvas();
            BagView bagView = BuildBagCanvas(gameState);        // S-064
            SettingsView settingsView = BuildSettingsCanvas();  // S-065
            BuildAccidentCanvas();                              // S-066 ③
            BuildToastCanvas();                                // S-133 ⑤ — 획득 알림
            BuildInvoiceCanvas(gameState);                      // S-071 ②
            BuildKioskCanvas(gameState);                        // S-125 ② 노점 구매창
            BuildHUDCanvas(gameState, bagView, settingsView);
            BuildTutorialCardCanvas();   // S-162 — 튜토리얼 미션 카드
            BuildTutorialDirector(gameState); // S-164 — 튜토리얼 진행부(씬 넘나듦)
            BuildDialogueCanvas();
            BuildMinigameCanvas();
            BuildPhoneCanvas();
            BuildEventSystem();

            EditorSceneManager.SaveScene(scene, CORE_PATH);
            Debug.Log("[CoreSceneBuilder] Core.unity 조립 완료 — Managers(Sun 포함)·Core·FadeCanvas·HUDCanvas·EventSystem 구성.");
        }

        [MenuItem("DontLate/Build/Core + Content Scenes (최초 셋업)", priority = 21)]
        public static void BuildAll()
        {
            CreateContentScenes();
            BuildCoreScene();
            RegisterBuildSettings();
        }

        /// <summary>
        /// 전 씬 일괄 재조립 (S-022) — clone 직후든 규칙 변경 후든 이 하나로 프로젝트가 완성 상태가 된다.
        /// 순서: 씬 파일 확보 → Core(매니저·캔버스) → 무대 3종 → 흐름 UI → 빌드 세팅 → Core 열기.
        /// </summary>
        [MenuItem("DontLate/Build/★ All Scenes", priority = 0)]
        public static void BuildAllScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[Build All] 저장되지 않은 씬이 있어 전 씬 재조립을 취소했다.");
                return;
            }

            CreateContentScenes();          // 씬 파일이 없으면 빈 씬부터 생성 (멱등)
            BuildCoreScene();               // 매니저·HUD·대화·미니게임·폰 캔버스
            CampStageBuilder.BuildCampStage();
            HomeStageBuilder.BuildHomeStage();
            DistrictSceneBuilder.BuildDistrictStage();     // S-186 ③ — 빌라촌
            FoodStreetSceneBuilder.BuildFoodStreetStage(); // S-186 ③ — 먹자골목
            ApartmentStageBuilder.BuildApartmentStage(); // S-038
            HillsideStageBuilder.BuildHillsideStage();   // S-049
            SceneFlowUIBuilder.BuildSceneFlowUI();  // 씬별 전환 UI + 정산 패널 (무대 뒤에 얹는다)
            // S-144 — 타이틀 무대(District 동일 배치 + 러너 + 카메라 강하). **UI 뒤에 온다**:
            // SceneFlowUIBuilder가 Main의 UI 캔버스를 다시 만들므로 그보다 먼저 세우면 순서상
            // 문제는 없지만, 무대가 UI를 참조하지 않으므로 뒤에 두어 "무대 → UI → 무대연출" 대신
            // "무대 → UI" 한 방향을 유지한다.
            // ⚠ 여기 등록을 빠뜨리면 다른 PC에서 Build All을 돌려도 **타이틀이 로고만 뜬다**
            //   (민지님 실제 사례 2026-08-04 — 빌더만 만들고 일괄 조립에 안 넣은 관제 누락).
            MainTitleStageBuilder.BuildMainTitleStage();
            RegisterBuildSettings();
            EditorSceneManager.OpenScene(CORE_PATH); // Play 시작점으로 복귀
            Debug.Log("[Build All] 전 씬 재조립 완료 — Core에서 Play.");
        }

        // ── Core 씬 구성 ─────────────────────────────────────

        private static void BuildManagers(GameStateSO gameState, TuningConfigSO tuning)
        {
            GameObject managers = new GameObject("Managers");

            WorldSceneFlowManager flow = managers.AddComponent<WorldSceneFlowManager>();
            SetField(flow, "_gameState", gameState);

            WorldDeliveryManager delivery = managers.AddComponent<WorldDeliveryManager>();
            SetField(delivery, "_gameState", gameState);
            SetField(delivery, "_tuning", tuning); // S-034 — 정산 실패 벌금

            WorldDeadlineManager deadline = managers.AddComponent<WorldDeadlineManager>();
            SetField(deadline, "_gameState", gameState);
            SetField(deadline, "_tuning", tuning);

            WorldDayNightManager dayNight = managers.AddComponent<WorldDayNightManager>();
            SetField(dayNight, "_gameState", gameState);
            SetField(dayNight, "_tuning", tuning);
            // S-160 — 거리 안개 **끔 고정**(남규님 지시 "Fog Enabled는 계속 true로 바뀌는데 false로").
            // 코드 기본값에만 의존하면, 이미 true로 직렬화된 씬이 그 값을 이겨 계속 켜진다.
            // 빌더가 정본이므로 여기서 명시적으로 박는다 — 재조립하면 항상 false가 된다.
            SetField(dayNight, "_fogEnabled", false);

            WorldDialogueManager dialogue = managers.AddComponent<WorldDialogueManager>();
            EnsureTestScenario(); // 박말순 인트로 SO 확보(멱등)
            SetField(dialogue, "_homeIntroScenario",
                AssetDatabase.LoadAssetAtPath<DialogueScenarioSO>(PARK_SCENARIO_PATH)); // S-009 Home 인트로 전화

            WorldDebtManager debt = managers.AddComponent<WorldDebtManager>(); // S-005
            SetField(debt, "_gameState", gameState);
            SetField(debt, "_tuning", tuning);

            WorldMinigameManager minigame = managers.AddComponent<WorldMinigameManager>(); // S-007
            SetField(minigame, "_tuning", tuning);

            DebugCheats cheats = managers.AddComponent<DebugCheats>(); // S-134 ⑦ — QA 치트(F9/F10/F11)
            SetField(cheats, "_gameState", gameState);

            WorldWeatherManager weather = managers.AddComponent<WorldWeatherManager>(); // S-042
            SetField(weather, "_gameState", gameState);
            SetField(weather, "_grade", GetOrCreateColorGrade()); // S-131 — 색보정 수치표(인스펙터 조절)
            SetField(weather, "_skyBackgroundTexture",
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/sky_bg.png"));
            // S-047: 구름 실아트 소켓 — Art/Backgrounds/fx_cloud_*.png 있으면 배선 (없으면 코드 블롭 폴백).
            var cloudTextures = new System.Collections.Generic.List<Texture2D>();
            foreach (string suffix in new[] { "a", "b", "c" })
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Backgrounds/fx_cloud_" + suffix + ".png");
                if (texture != null) cloudTextures.Add(texture);
            }
            if (cloudTextures.Count > 0)
            {
                SerializedObject weatherSerialized = new SerializedObject(weather);
                SerializedProperty cloudsProp = weatherSerialized.FindProperty("_cloudTextures");
                cloudsProp.arraySize = cloudTextures.Count;
                for (int i = 0; i < cloudTextures.Count; i++)
                    cloudsProp.GetArrayElementAtIndex(i).objectReferenceValue = cloudTextures[i];
                weatherSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            WorldEndingManager ending = managers.AddComponent<WorldEndingManager>(); // S-104
            SetField(ending, "_gameState", gameState);
            SetField(ending, "_npcs", GetOrCreateNpcCatalog());
            SetField(ending, "_creditsView", managers.AddComponent<EndingCreditsView>());
            SetField(ending, "_cast", BuildEndingCast()); // S-228 — 모델 보유 인물 전원


            WorldJuiceManager juice = managers.AddComponent<WorldJuiceManager>(); // S-023
            SetField(juice, "_font", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH));

            WorldAudioManager audio = managers.AddComponent<WorldAudioManager>();
            SetField(audio, "_library", GetOrCreateBgmLibrary());
            SfxSynthGenerator.EnsurePlaceholders();
            SetField(audio, "_sfxPickup", LoadSfx("sfx_pickup"));
            SetField(audio, "_sfxDeliveryOk", LoadSfx("sfx_delivery_ok"));
            SetField(audio, "_sfxLateBuzzer", LoadSfx("sfx_late_buzzer"));
            SetField(audio, "_sfxBoxBreak", LoadSfx("sfx_box_break"));   // AU-008 신기능 7종
            SetField(audio, "_sfxBarcode", LoadSfx("sfx_barcode"));
            SetField(audio, "_sfxFanfare", LoadSfx("sfx_fanfare")); // S-086
            SetField(audio, "_sfxThunder", LoadSfx("sfx_thunder")); // S-088 ⑥
            SetField(audio, "_sfxPenalty", LoadSfx("sfx_penalty"));
            SetField(audio, "_sfxVending", LoadSfx("sfx_vending"));
            SetField(audio, "_sfxThrow", LoadSfx("sfx_throw"));
            SetField(audio, "_sfxCoin", LoadSfx("sfx_coin"));
            SetField(audio, "_sfxPhone", LoadSfx("sfx_phone"));
            SetField(audio, "_sfxTutorialStep", LoadSfx("sfx_tutorial_step")); // AU-028 — 없으면 무음 폴백
            SetField(audio, "_sfxDeadlineWarn", LoadSfx("sfx_deadline_warn"));  // AU-009 잔여 배선 8종
            SetField(audio, "_sfxPhoneRing", LoadSfx("sfx_phone_ring"));
            SetField(audio, "_sfxRhythmHit", LoadSfx("sfx_rhythm_hit"));
            SetField(audio, "_sfxRhythmMiss", LoadSfx("sfx_rhythm_miss"));
            SetField(audio, "_sfxSceneWhoosh", LoadSfx("sfx_scene_whoosh"));
            SetField(audio, "_sfxFootstep", LoadSfx("sfx_footstep"));
            SetField(audio, "_sfxDrink", LoadSfx("sfx_drink"));
            SetField(audio, "_ambNight", LoadSfx("amb_night"));
            SetField(audio, "_sfxSettleOk", LoadSfx("sfx_settle_ok"));           // AU-010 신규 4종
            SetField(audio, "_sfxSettleBad", LoadSfx("sfx_settle_bad"));
            SetField(audio, "_sfxFurniturePlace", LoadSfx("sfx_furniture_place"));
            SetField(audio, "_sfxUiTick", LoadSfx("sfx_ui_tick"));
            SetField(audio, "_ambVillatown", LoadSfx("amb_villatown"));          // AU-011 구역 앰비언스+지도 5종
            SetField(audio, "_ambFoodalley", LoadSfx("amb_foodalley"));
            SetField(audio, "_sfxMapPin", LoadSfx("sfx_map_pin"));
            SetField(audio, "_sfxMapRoute", LoadSfx("sfx_map_route"));
            SetField(audio, "_sfxMapDepart", LoadSfx("sfx_map_depart"));
            SetField(audio, "_sfxArrive", LoadSfx("sfx_arrive"));                 // AU-018 ④ 배송지 도착 차임
            SetField(audio, "_sfxBoxDamage", LoadSfx("sfx_box_damage"));          // AU-018 ③ 액션 4종
            SetField(audio, "_sfxJump", LoadSfx("sfx_jump"));
            SetField(audio, "_sfxLand", LoadSfx("sfx_land"));
            SetField(audio, "_sfxFootstepSnow", LoadSfx("sfx_footstep_snow"));
            SetField(audio, "_sfxCarCrash", LoadSfx("sfx_car_crash"));            // S-066 ③ (AU-020 — 도착 전 null 무음)
            SetField(audio, "_sfxLevelUp", LoadSfx("sfx_level_up"));              // AU-027 (도착 전 null 무음)
            SetField(audio, "_ambWeatherRain", LoadSfx("amb_weather_rain"));      // AU-018 ① 날씨 앰비언스 3종
            SetField(audio, "_ambWeatherSnow", LoadSfx("amb_weather_snow"));
            SetField(audio, "_ambWeatherHeat", LoadSfx("amb_weather_heat"));
            SetField(audio, "_bgmRainNight", LoadBgm("Neon Rain"));               // AU-018 ②/AU-025 비·밤(원제)
            SetField(audio, "_bgmRainDay", LoadBgm("Rain on the Window"));         // AU-025 비·낮
            SetField(audio, "_bgmSnowNight", LoadBgm("Neon Snowfall"));           // AU-026 눈·밤(원제)
            SetField(audio, "_bgmSnowDay", LoadBgm("Daylight Snowfall"));          // AU-026 눈·낮
            SetField(audio, "_bgmHeatDay", LoadBgm("Heatwave Afternoon"));         // AU-032 폭염·낮(구 Midnight Heatwave)
            SetField(audio, "_bgmHeatNight", LoadBgm("Heatwave Night Drive"));     // AU-032 폭염·밤
            SetField(audio, "_bgmFogDay", LoadBgm("Pale White Haze"));             // AU-032 안개·낮
            SetField(audio, "_bgmFogNight", LoadBgm("Sodium Fog"));                // AU-032 안개·밤(원제)
            SetField(audio, "_gameState", AssetDatabase.LoadAssetAtPath<GameStateSO>(DATA_ROOT + "/GameState.asset"));

            // 태양은 Core 소유(D-021 교정) — 콘텐츠 씬은 자체 Directional Light를 두지 않는다.
            GameObject sunGo = new GameObject("Sun");
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            SetField(dayNight, "_sun", sun);

            // AudioListener는 Core 소유(D-041) — 태양과 같은 이유다. Core는 항상 로드돼 있으므로
            // 콘텐츠 씬이 교체되는 순간에도 리스너가 끊기지 않는다(콘텐츠 씬 소유로 두면
            // 언로드→로드 사이 구간에 "no audio listeners" 경고가 매 프레임 발생).
            GameObject listenerGo = new GameObject("AudioListener");
            listenerGo.AddComponent<AudioListener>();
        }

        /// <summary>
        /// Main.unity(사람 샌드박스)에서 **Core 소유물의 중복분만** 떼어낸다. 지오메트리·조명 등
        /// 사람이 배치한 내용은 손대지 않는다.
        /// - AudioListener: 리스너는 Core 소유(D-041) — Main이 들고 오면 씬에 2개가 된다
        /// - CoreBootstrap: Main은 부트스트랩이 로드하는 씬인데 그 안에 또 부트스트랩이 있으면
        ///   Request(Main)이 두 번 발생해 "Main → Main 는 허용되지 않은 전이" 경고가 난다
        /// </summary>
        private static void CleanCoreDuplicatesInMain()
        {
            const string mainPath = SCENES_ROOT + "/Main.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(mainPath) == null) return;

            Scene scene = EditorSceneManager.OpenScene(mainPath, OpenSceneMode.Single);
            int listeners = 0;
            int bootstraps = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>(true))
                {
                    Object.DestroyImmediate(listener);
                    listeners++;
                }
                foreach (CoreBootstrap bootstrap in root.GetComponentsInChildren<CoreBootstrap>(true))
                {
                    Object.DestroyImmediate(bootstrap);
                    bootstraps++;
                }
            }

            if (listeners + bootstraps <= 0) return;

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[CoreSceneBuilder] Main.unity 정리 — AudioListener " + listeners
                    + "개 · CoreBootstrap " + bootstraps + "개 제거 (둘 다 Core 소유).");
        }

        /// <summary>bom_id 로 SFX 클립을 집는다. 실음원이 같은 이름으로 들어오면 그대로 교체된다.</summary>
        internal static AudioClip LoadSfx(string bomId)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/" + bomId + ".wav");
        }

        /// <summary>원제(파일명)로 BGM 클립을 집는다 — BGM은 원제 유지가 스왑 계약(AU-018 ② 날씨 곡).</summary>
        internal static AudioClip LoadBgm(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(BGM_FOLDER + "/" + fileName + ".wav");
        }

        /// <summary>
        /// BGM 목록 SO를 확보한다(없으면 계약 폴더의 클립으로 생성). 슬롯 분류는 사람 청취로 확정하므로
        /// 자동 생성분은 전부 Unsorted — 제목으로 낮/밤을 추정하지 않는다(D-039 실수→규칙).
        /// </summary>
        /// <summary>
        /// 색보정 수치표 (S-131). **생성 시에만** 기본값을 굽는다 — 남규님이 인스펙터에서 만진 값을
        /// 빌더 재실행이 덮어쓰면 안 되기 때문(D-064 GetOrCreate 규약).
        /// 기본값 = S-042~S-088에서 하드코딩돼 있던 수치 원본 — 현행 룩이 그대로 재현된다.
        /// </summary>
        internal static ColorGradeSO GetOrCreateColorGrade()
        {
            const string path = "Assets/Data/ColorGrade.asset";
            ColorGradeSO grade = AssetDatabase.LoadAssetAtPath<ColorGradeSO>(path);
            if (grade != null) return grade;

            grade = ScriptableObject.CreateInstance<ColorGradeSO>();

            // 시간대 — bloom은 S-043(밤 간판 HDR 번짐 / 낮 절제).
            grade.morning = Grade(0f, 0f, 4f, Color.white, 0.30f);
            grade.day = Grade(0.05f, 0f, 0f, Color.white, 0.20f);
            grade.evening = Grade(0f, 6f, 14f, Color.white, 0.60f);
            grade.night = Grade(-0.05f, -6f, -10f, Color.white, 0.85f);

            // 날씨.
            grade.clear = Grade(0f, 0f, 0f, Color.white, 0f);
            grade.cloudy = Grade(-0.12f, -8f, 0f, Color.white, 0f);
            grade.rain = Grade(-0.28f, -18f, -10f, new Color(0.88f, 0.92f, 1f), 0.10f); // 젖은 밤거리 번짐
            grade.snow = Grade(0.08f, -12f, -18f, Color.white, 0f);
            grade.fog = Grade(-0.18f, -14f, 0f, Color.white, 0f);
            grade.heat = Grade(0.06f, 6f, 22f, new Color(1f, 0.97f, 0.90f), 0f);
            grade.storm = Grade(-0.42f, -24f, -8f, new Color(0.82f, 0.88f, 0.98f), 0f); // S-088 ⑤ — 어둡다

            // 구역.
            grade.villaTown = Grade(0f, 0f, 6f, Color.white, 0f);                        // 웜그레이 골목
            grade.foodAlley = Grade(0f, 8f, 0f, new Color(1f, 0.96f, 0.99f), 0f);        // 네온끼
            grade.apartment = Grade(0f, -4f, 0f, Color.white, 0f);                       // 무채 단지

            AssetDatabase.CreateAsset(grade, path);
            AssetDatabase.SaveAssets();
            return grade;
        }

        private static ColorGradeSO.Layer Grade(float exposure, float saturation, float temperature,
            Color filter, float bloom)
            => new ColorGradeSO.Layer
            {
                exposure = exposure,
                saturation = saturation,
                temperature = temperature,
                filter = filter,
                bloom = bloom,
            };

        internal static BgmLibrarySO GetOrCreateBgmLibrary()
        {
            BgmLibrarySO library = AssetDatabase.LoadAssetAtPath<BgmLibrarySO>(BGM_LIBRARY_PATH);
            if (library != null) return library;

            library = ScriptableObject.CreateInstance<BgmLibrarySO>();

            if (AssetDatabase.IsValidFolder(BGM_FOLDER))
            {
                foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { BGM_FOLDER }))
                {
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                        AssetDatabase.GUIDToAssetPath(guid));
                    if (clip == null) continue;
                    library.entries.Add(new BgmLibrarySO.Entry { clip = clip, slot = BgmSlot.Unsorted });
                }
            }

            AssetDatabase.CreateAsset(library, BGM_LIBRARY_PATH);
            AssetDatabase.SaveAssets();
            return library;
        }

        private static void BuildCore(GameStateSO gameState)
        {
            GameObject core = new GameObject("Core");
            CoreBootstrap bootstrap = core.AddComponent<CoreBootstrap>();
            SetField(bootstrap, "_gameState", gameState);
            SetField(bootstrap, "_firstScene", GameScene.Main);

            // S-100 ② — 우하단 버전 라벨 (BuildVersionStamp 산출물 표시 — 팀원 빌드 식별).
            new GameObject("VersionLabel").AddComponent<VersionLabel>();
        }

        private static void BuildFadeCanvas()
        {
            // Screen Space - Overlay 캔버스 + CanvasGroup + FadeScreen.
            GameObject canvasGo = new GameObject("FadeCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            CanvasGroup group = canvasGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            // 검은 풀스크린 페이드 이미지.
            GameObject blackGo = new GameObject("Black");
            blackGo.transform.SetParent(canvasGo.transform, false);
            Image black = blackGo.AddComponent<Image>();
            black.color = Color.black;
            StretchFull(black.rectTransform);

            // "늦지마!" 컷인 — 비활성 자식 텍스트.
            GameObject cutInGo = new GameObject("LateCutIn");
            cutInGo.transform.SetParent(canvasGo.transform, false);
            Text cutIn = cutInGo.AddComponent<Text>();
            cutIn.text = "늦지마!";
            cutIn.alignment = TextAnchor.MiddleCenter;
            cutIn.fontSize = 120;
            cutIn.color = new Color(1f, 0.25f, 0.25f, 1f);
            cutIn.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/UI/Fonts/DNFBitBitOTF.otf")
                ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            StretchFull(cutIn.rectTransform);
            cutInGo.SetActive(false);

            FadeScreen fade = canvasGo.AddComponent<FadeScreen>();
            SetField(fade, "_group", group);
            SetField(fade, "_lateCutIn", cutInGo);
        }

        // ── HUD 캔버스 (Core 상주) ───────────────────────────

        /// <summary>
        /// S-161 — 대사 한 덩어리를 줄바꿈 기준으로 여러 대화 라인으로 쪼갠다.
        /// 대화창은 고정 높이라 긴 문단을 한 라인에 담으면 밖으로 넘친다.
        /// </summary>
        private static (string speaker, string text)[] SplitLines(string body)
        {
            if (string.IsNullOrEmpty(body)) return new[] { ("사장님", string.Empty) };
            string[] chunks = body.Split('\n');
            var lines = new System.Collections.Generic.List<(string, string)>();
            foreach (string chunk in chunks)
            {
                string trimmed = chunk.Trim();
                if (trimmed.Length > 0) lines.Add(("사장님", trimmed));
            }
            return lines.Count > 0 ? lines.ToArray() : new[] { ("사장님", body) };
        }

        /// <summary>S-164 ② — 튜토리얼 하이라이트 대상 등록(대상이 자기 id를 듣고 반응).</summary>
        private static void AttachHighlightTarget(GameObject go, string id)
        {
            if (go == null) return;
            var target = go.GetComponent<TutorialHighlightTarget>() ?? go.AddComponent<TutorialHighlightTarget>();
            SetField(target, "_id", id);
        }

        /// <summary>
        /// S-164 ② — 게이트별 하이라이트 대상 id. 대상이 없는 단계(이동·설명·도착)는 빈 문자열이다 —
        /// 화면 어디를 봐야 할지가 없는 단계까지 뭔가를 흔들면 오히려 헷갈린다.
        /// </summary>
        private static string HighlightIdFor(CampTutorialDirector.Gate gate) => gate switch
        {
            CampTutorialDirector.Gate.BagOpen => "bag",
            CampTutorialDirector.Gate.DrinkUse => "bag",   // 드링크도 가방에서 꺼낸다
            CampTutorialDirector.Gate.BoxPickup => "box",
            CampTutorialDirector.Gate.Barcode => "box",
            CampTutorialDirector.Gate.KioskOpen => "vending",
            _ => string.Empty,
        };

        /// <summary>
        /// S-164 — 튜토리얼 진행부(Core 상주). 단계 저술이 여기 있는 이유: 진행부가
        /// **씬을 넘나들어야** 하기 때문이다(배송지역 이동·NPC 대화 미션). Camp 씬
        /// 오브젝트였을 땐 씬을 떠나는 순간 파괴돼 그 뒤 행동을 못 받았다.
        /// </summary>
        private static void BuildTutorialDirector(GameStateSO gameState)
        {
            // S-151 — 말투를 따뜻하게 다시 썼다(남규님 "사장이 따뜻한 말투로 해야하는데 너무 딱딱함").
            // 사장님은 감독관이 아니라 **먼저 이 일을 해본 사람**이다. 지시문 대신 권유·염려로 쓰고,
            // 단계마다 칭찬을 붙여 플레이어가 잘 따라오고 있다는 신호를 준다.
            var steps = new (string title, string line, CampTutorialDirector.Gate gate, string hint, string praise, string card)[]
            {
                ("Move",   "어어, 왔구나! 기다렸어. 오늘부터 같이 일하는 거지?\n"
                         + "긴장 풀고, WASD로 천천히 좀 걸어봐. 몸부터 풀어야 안 다쳐.",
                    CampTutorialDirector.Gate.Move,      "WASD로 이동해 보세요",
                    "그래 그래, 자연스럽네. 발놀림이 좋은데?", "걸어보기"),

                // S-152 — 남규님 지적: I키만 알려주면 화면 위 [가방] 버튼을 못 찾는다. 둘 다 안내한다.
                ("Bag",    "가방 한번 열어볼래? I키를 누르거나, 화면 위쪽 [가방] 버튼을 눌러도 열려.\n"
                         + "드링크나 길에서 주운 것들이 여기 들어가. 급할 때 요긴하다고.",
                    CampTutorialDirector.Gate.BagOpen,   "I키 또는 화면 위 [가방] 버튼",
                    "옳지. 뭐 들었나 가끔 확인해 보면 좋아.", "가방 열기"),

                ("Phone",  "이제 폰이야. Tab 눌러봐.\n"
                         + "주문도 지도도 은행도 전부 여기 있어. 하루 종일 들여다볼 물건이지.",
                    CampTutorialDirector.Gate.PhoneOpen, "Tab키로 휴대폰을 열어 보세요",
                    "잘했어. 길 잃으면 지도부터 켜는 거, 잊지 말고.", "휴대폰 켜기"),

                // S-152 — 종전 설명이 틀렸다(남규님 정정). 폰을 먼저 켜는 게 아니라,
                // 바코드에 마우스를 올리면 **폰이 알아서 올라오고** 카메라 중앙에 맞춰야 찍힌다.
                ("Barcode","자, 이제 진짜 일이다. 짐은 바코드를 찍어야 실을 수 있어.\n"
                         + "상자 가까이 가서 마우스로 상자를 클릭해봐. 송장이 뜰 거야.\n"
                         + "거기 바코드에 마우스를 갖다 대면 폰이 저절로 올라와. 그 상태로\n"
                         + "카메라 한가운데에 바코드를 맞추고 잠깐 있으면 — 찰칵, 알아서 찍힌다.",
                    CampTutorialDirector.Gate.Barcode,   "상자에 가까이 가서 클릭 > 송장 바코드에 마우스 > 카메라 중앙",
                    "찰칵! 바로 그거야. 처음엔 손이 떨리는데 금방 익숙해져.", "바코드 스캔"),

                // S-155 — 남규님: 픽업 때 목적지를 알려주면 좋겠다.
                ("Pickup", "스캔했으면 이제 들면 돼. 상자 앞에서 E를 눌러봐.\n"
                         + "이 건은 빌라촌이야. 골목 모퉁이 돌아서 양옥집 쪽으로 가면\n"
                         + "바닥이 은은하게 빛나는 자리가 있을 거야 — 거기가 내려놓는 데다.\n"
                         + "무거우면 무리하지 말고, 천천히 가도 괜찮아.",
                    CampTutorialDirector.Gate.BoxPickup, "E키로 상자를 집어 보세요",
                    // S-158 — 짐을 든 직후가 "어디로 가지?"가 생기는 순간이다. 방향을 여기서 준다(남규님).
                    "좋아 좋아, 허리 조심하고!\n"
                  + "여기서 오른쪽으로 쭉 걸어가면 빌라촌이 나와. 오늘 갈 데가 거기야.\n"
                  + "거기서 더 가면 다른 동네들도 이어져 있고 — 차차 알게 될 거야.", "상자 집기"),

                // S-155 — 시작 가방에 드링크 1개를 넣어뒀다(CoreBootstrap). 여기서 써 보게 한다.
                // S-156 — 조작까지 적는다(남규님: 우클릭 → 사용 버튼). 물건만 주고 쓰는 법을 안 알려주면
                // 가방을 열어놓고도 못 쓴다.
                ("Drink",  "아 참, 가방에 에너지드링크 하나 넣어놨어. 내가 주는 거야.\n"
                         // ⚠ 마크다운(**)은 TMP에서 별표 그대로 보인다 — 강조는 리치텍스트 태그로.
                         + "가방(I) 열고 그 드링크를 <b>우클릭</b>하면 [사용] 버튼이 뜰 거야.\n"
                         + "그거 눌러서 한번 마셔봐. 지쳤을 때 이만한 게 없거든.",
                    CampTutorialDirector.Gate.DrinkUse,  "가방(I) > 드링크 우클릭 > [사용]",
                    "그렇지! 힘들 때 미루지 말고 바로 마셔. 쓰러지고 나면 늦어.", "드링크 마시기"),

                // S-156 — 넷인데 셋이라고 했다(남규님 정정). 먹자골목이 빠져 있었다.
                ("Area",   "구역은 넷이야. 빌라촌, 먹자골목, 아파트단지, 언덕주택가.\n"
                         + "먹자골목은 사람도 많고 노점도 많아 — 배 고프면 거기서 뭐 사 먹어도 되고.\n"
                         + "언덕은 비 오면 미끄러우니까 그런 날은 특히 조심하고. 아파트는 엘리베이터랑\n"
                         + "현관 비밀번호가 있어. 헷갈리면 폰 지도 보면 돼, 다 나와 있으니까.",
                    CampTutorialDirector.Gate.ReadOnly,  "",
                    "뭐, 다니다 보면 몸이 먼저 기억할 거야.", "지역 익히기"),

                // S-164 ⑤ — NPC 대화 전에 **배송지역까지 가는** 미션(남규님 지시).
                // 짐만 들려 보내면 어디로 가야 할지 모른 채 캠프에서 서성인다.
                ("Travel", "자 이제 나가볼까. 오른쪽 끝까지 걸어가면 빌라촌이야.\n"
                         + "가는 길 조심하고, 도착하면 다시 알려줄게.",
                    CampTutorialDirector.Gate.ReachDistrict, "오른쪽 끝으로 걸어 빌라촌까지 가세요",
                    "왔구나! 여기가 빌라촌이야. 이제 진짜 배달이다.", "배송지역 가기"),

                // S-178 — NPC 대화 **앞에** 배송 배치. 종전엔 짐을 들고 빌라촌까지 가 놓고
                // 다음 지시가 "사람한테 말 걸어봐"라, 정작 배달을 안 하고 튜토리얼이 끝났다
                // (남규님 지적). 코어루프의 결말을 튜토리얼이 안 가르치던 구멍이다.
                ("Place",  "자, 이제 진짜다. 주소지 앞 바닥에 파랗게 빛나는 자리가 보일 거야.\n"
                         + "그 위에 서서 E를 누르면 짐이 내려간다. 그게 배달 끝이야.\n"
                         + "아, 내려놨다고 돈이 바로 들어오는 건 아니고 — 정산은 집에 가서 한번에 한다.",
                    CampTutorialDirector.Gate.PlaceDelivery, "파란 비콘 위에서 E로 짐을 내려놓으세요",
                    "좋아, 그게 네 첫 배달이다. 별거 아니지? 이걸 하루에 몇 번씩 하는 거야.", "배송지에 내려놓기"),

                ("Npc",    "길에서 사람 마주치면 E로 말 한번 걸어봐.\n"
                         + "이 동네 사람들 은근히 정 많아. 얼굴 트면 팁도 챙겨주고 그래.",
                    CampTutorialDirector.Gate.NpcTalk,   "NPC에게 E로 말을 걸어 보세요",
                    "거봐, 나쁘지 않지? 인사만 잘해도 하루가 편해.", "NPC와 대화"),

                ("Kiosk",  "마지막이야. 자판기랑 편의점, 포장마차는 E로 열어서 사면 돼.\n"
                         + "힘들면 꼭 뭐라도 챙겨 먹어. 굶고 뛰다 쓰러지는 애들 여럿 봤다.",
                    CampTutorialDirector.Gate.KioskOpen, "자판기·편의점·포장마차를 E로 열어 보세요",
                    "그래, 이제 다 알려준 것 같네. 무리하지 말고 다녀와. 늦으면... 뭐, 나한테 혼나는 거지!", "자판기 이용"),
            };

            GameObject tutorialGo = new GameObject("__gb_CampTutorial");
            CampTutorialDirector director = tutorialGo.AddComponent<CampTutorialDirector>();
            SerializedObject dirSo = new SerializedObject(director);
            SerializedProperty stepList = dirSo.FindProperty("_steps");
            stepList.arraySize = steps.Length;
            for (int i = 0; i < steps.Length; i++)
            {
                // S-161 — 한 덩어리로 넣으면 대화창 밖으로 흘러넘친다(남규님 캡처).
                // 위 대사에 이미 의미 단위로 줄바꿈을 넣어뒀으므로 **그 경계로 라인을 쪼갠다** —
                // 각 조각이 한 번의 클릭으로 넘어가고 창 안에 들어온다.
                DialogueScenarioSO line = NpcBuildKit.GetOrCreateScenario(
                    "Scenario_Tutorial_" + steps[i].title, SplitLines(steps[i].line));
                DialogueScenarioSO praise = NpcBuildKit.GetOrCreateScenario(
                    "Scenario_Tutorial_" + steps[i].title + "_Praise", SplitLines(steps[i].praise));
                SerializedProperty element = stepList.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("scenario").objectReferenceValue = line;
                element.FindPropertyRelative("gate").enumValueIndex = (int)steps[i].gate;
                element.FindPropertyRelative("hint").stringValue = steps[i].hint;
                element.FindPropertyRelative("praise").objectReferenceValue = praise;
                element.FindPropertyRelative("title").stringValue = steps[i].card; // S-162 미션 카드 제목
                // S-164 ② — 하이라이트 대상은 게이트에서 파생한다. 단계 표에 열을 하나 더 만들면
                // 대상이 없는 단계까지 빈 칸을 적어야 해 표가 길어진다 — 규칙 하나로 정한다.
                element.FindPropertyRelative("highlightId").stringValue = HighlightIdFor(steps[i].gate);
            }
            dirSo.ApplyModifiedPropertiesWithoutUndo();
            // S-266 — **Apply 뒤에** 주입한다. `SerializedObject`는 생성 시점의 값을 들고 있어,
            // 리플렉션으로 먼저 넣어도 Apply가 그 옛 값(null)으로 되돌린다(실측: `_gameState=NULL`).
            // 이 누락 때문에 튜토리얼의 상태 판정이 통째로 죽어 있었다.
            // `SetReference`는 이름과 달리 SerializedObject 경유라 씬 저장 때 `{fileID: 0}`으로
            // 날아간다(CODE_RULES §6 실수→규칙). 리플렉션 직접 주입만 살아남는다.
            SetField(director, "_gameState", gameState);

        }

        /// <summary>
        /// S-162 — 튜토리얼 미션 카드. 화면 **오른쪽, 세로 아래 1/3** 지점에서 슬라이드 인 한다.
        /// HUD(sortOrder 10)보다 위, 페이드(100)보다 아래에 둔다 — HUD를 덮되 전환 연출은 못 덮게.
        /// </summary>
        private static void BuildTutorialCardCanvas()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);

            GameObject canvasGo = new GameObject("TutorialCardCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 카드 본체 — 우측 정렬, 아래에서 1/3 높이(1080 × 1/3 = 360).
            GameObject cardGo = new GameObject("MissionCard", typeof(RectTransform));
            cardGo.transform.SetParent(canvasGo.transform, false);
            Image background = cardGo.AddComponent<Image>();
            background.color = new Color(0.09f, 0.11f, 0.16f, 0.94f);
            RectTransform card = (RectTransform)cardGo.transform;
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(1f, 0f);
            card.sizeDelta = new Vector2(420f, 118f);
            // S-163 — x=0: 피벗이 우하단이라 **우측 모서리가 화면 우측에 밀착**한다(종전 −40은 떠 있었다).
            // y=180: 종전 360에서 아래 여백을 절반으로 내렸다(남규님 지시).
            card.anchoredPosition = new Vector2(0f, 180f);

            TMP_Text title = MakeCardLabel(cardGo.transform, "Title", font, 30f, FontStyles.Bold,
                new Vector2(0f, -14f), new Vector2(-32f, 40f));
            TMP_Text detail = MakeCardLabel(cardGo.transform, "Detail", font, 24f, FontStyles.Normal,
                new Vector2(0f, -58f), new Vector2(-32f, 52f));
            detail.color = new Color(0.78f, 0.83f, 0.92f, 1f);

            var view = canvasGo.AddComponent<TutorialMissionCardView>();
            SetField(view, "_card", card);
            SetField(view, "_background", background);
            SetField(view, "_titleLabel", title);
            SetField(view, "_detailLabel", detail);
        }

        private static TMP_Text MakeCardLabel(Transform parent, string name, TMP_FontAsset font,
            float size, FontStyles style, Vector2 offset, Vector2 rect)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            // S-163 — **가로 스트레치**로 잡는다. 종전엔 앵커를 고정(min==max)해 두고
            // `sizeDelta.x`에 −32를 넣었는데, 고정 앵커에서 sizeDelta는 **절대 크기**라
            // 폭이 음수가 됐다 → 한 글자마다 줄바꿈 → 세로 글씨(남규님 캡처).
            // 스트레치일 때만 sizeDelta.x가 "좌우 여백"으로 동작한다.
            RectTransform rt = label.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = rect;          // x = 좌우 여백(음수), y = 높이
            rt.anchoredPosition = offset;
            return label;
        }

        private static void BuildHUDCanvas(GameStateSO gameState, BagView bagView, SettingsView settingsView)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            if (font == null)
                Debug.LogWarning("[CoreSceneBuilder] Pretendard 폰트 에셋을 못 찾음 — TMP 기본 폰트로 진행.");

            // Canvas: Screen Space - Overlay · sortOrder 10 · Scale With Screen Size 1920×1080.
            GameObject canvasGo = new GameObject("HUDCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            HUDView hud = canvasGo.AddComponent<HUDView>();
            SetField(hud, "_gameState", gameState);

            // 가시성 루트 — 전체 화면 스트레치.
            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(canvasGo.transform, false);
            StretchFull((RectTransform)content.transform);
            SetField(hud, "_content", content);

            // 시계 (우상). S-117 — 실아트 시계 아이콘(ui_coin과 동일 계약) 있으면 텍스트 오른쪽에 붙인다.
            Sprite clockArt = LoadUISprite("ui_clock");
            TMP_Text clock = CreateText(content.transform, "Clock", "Day 1 · 08:00", font,
                40f, Color.white, TextAlignmentOptions.TopRight);
            AnchorCorner(clock.rectTransform, new Vector2(1f, 1f),
                clockArt != null ? new Vector2(-96f, -30f) : new Vector2(-40f, -30f), new Vector2(460f, 60f));
            SetField(hud, "_clockLabel", clock);
            if (clockArt != null)
            {
                Image clockIcon = CreateImage(content.transform, "ClockIcon", Color.white);
                clockIcon.sprite = clockArt;
                clockIcon.raycastTarget = false;
                AnchorCorner(clockIcon.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -26f), new Vector2(48f, 48f));
            }

            // ── S-063 상단 바 ─────────────────────────────
            Color chipColor = new Color(0.10f, 0.12f, 0.16f, 0.85f);
            Texture2D basicChipArt = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/basic_ui_box.png");
            Color chipTextColor = basicChipArt != null ? new Color(0.20f, 0.18f, 0.15f, 1f) : Color.white;

            // 캐릭터 카드 — Lv·닉네임 + 숙련도(앰버)·스태미나(초록) 게이지.
            Image charCard = CreateImage(content.transform, "CharacterCard", chipColor);
            AnchorCorner(charCard.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -20f), new Vector2(400f, 116f));

            TMP_Text level = CreateText(charCard.transform, "Level", "Lv.1  늦지마맨", font,
                34f, Color.white, TextAlignmentOptions.TopLeft);
            AnchorCorner(level.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -12f), new Vector2(220f, 44f));
            SetField(hud, "_levelLabel", level);

            // S-134 ④ — 체력 5칸. 레벨 라벨 오른쪽 빈자리에 붙인다(카드 높이 불변).
            // S-168 — 칸 치수는 여기가 단일 소유다. 경험치 칸도 같은 상수를 쓴다(남규님: 크기 동일).
            const float PIP_SIZE = 22f;
            const float PIP_STRIDE = 28f;   // 칸 폭 + 6px 틈
            var healthPips = new Image[GameStateSO.HEALTH_MAX];
            for (int i = 0; i < healthPips.Length; i++)
            {
                healthPips[i] = CreateImage(charCard.transform, "HealthPip" + i, new Color(0.90f, 0.35f, 0.32f, 1f));
                AnchorCorner(healthPips[i].rectTransform, new Vector2(0f, 1f),
                    new Vector2(248f + i * PIP_STRIDE, -18f), new Vector2(PIP_SIZE, PIP_SIZE));
            }
            SetField(hud, "_healthPips", healthPips);

            // S-174 후속 — 경험치는 **바 형태·노란색**(남규님 지시). S-168에서 HP 칸과 같은
            // 22×22 낱개로 맞췄다가, 스태미나와 나란한 게이지 줄로 되돌린다.
            // 5칸 구분은 유지한다 — 칸 단위 순차 펀치(S-174 ④)가 여기에 걸려 있고,
            // "몇 건 더 하면 오르는지"를 세는 감각도 칸에서 나온다. 낱개가 아니라 **이어진 바**를
            // 다섯으로 나눈 모양이다: 스태미나 바(360×16)와 같은 자리·같은 두께.
            const int MASTERY_CELLS = 5;
            const float MASTERY_BAR_W = 360f;
            const float MASTERY_BAR_H = 16f;
            const float MASTERY_GAP = 3f; // 칸 경계선 — 0이면 한 덩어리로 읽힌다
            Image masteryBg = CreateImage(charCard.transform, "MasteryBg", new Color(0.06f, 0.07f, 0.10f, 1f));
            AnchorCorner(masteryBg.rectTransform, new Vector2(0f, 0f),
                new Vector2(20f, 44f), new Vector2(MASTERY_BAR_W, MASTERY_BAR_H));
            SetField(hud, "_masteryBar", masteryBg); // 호버 툴팁 판정 대상

            float cellWidth = (MASTERY_BAR_W - MASTERY_GAP * (MASTERY_CELLS - 1)) / MASTERY_CELLS;
            var masteryPips = new Image[MASTERY_CELLS];
            for (int i = 0; i < MASTERY_CELLS; i++)
            {
                masteryPips[i] = CreateImage(masteryBg.transform, "MasteryPip" + i, MASTERY_YELLOW);
                // 피벗=앵커=(0,0.5) → anchoredPosition.x는 칸의 **왼쪽 모서리** 위치다.
                AnchorCorner(masteryPips[i].rectTransform, new Vector2(0f, 0.5f),
                    new Vector2(i * (cellWidth + MASTERY_GAP), 0f), new Vector2(cellWidth, MASTERY_BAR_H));
            }
            SetField(hud, "_masteryPips", masteryPips);

            Image staminaBg = CreateImage(charCard.transform, "StaminaBg", new Color(0.06f, 0.07f, 0.10f, 1f));
            AnchorCorner(staminaBg.rectTransform, new Vector2(0f, 0f), new Vector2(20f, 16f), new Vector2(360f, 16f));
            Image staminaFill = CreateImage(staminaBg.transform, "StaminaFill", new Color(0.45f, 0.85f, 0.55f, 1f));
            StretchFull(staminaFill.rectTransform);
            ConfigureGaugeFill(staminaFill, 1f); // S-070 ②
            SetField(hud, "_staminaFill", staminaFill);

            // S-088 ④ — 패널티 세그먼트: 오른쪽부터 더움(주황)·추움(파랑)·무거움(갈색)·강풍(회색).
            (string fieldName, Color color)[] penaltySegments =
            {
                ("_penaltyHeatFill", new Color(1f, 0.45f, 0.20f, 0.95f)),
                ("_penaltyColdFill", new Color(0.62f, 0.86f, 1f, 0.95f)), // S-097 ③ — 버프 파랑(0.31,0.58,1)과 구분: 얼음빛으로
                ("_penaltyCarryFill", new Color(0.55f, 0.40f, 0.25f, 0.95f)),
                ("_penaltyStormFill", new Color(0.60f, 0.62f, 0.68f, 0.95f)),
            };
            foreach (var segment in penaltySegments)
            {
                Image seg = CreateImage(staminaBg.transform, segment.fieldName.TrimStart('_'), segment.color);
                StretchFull(seg.rectTransform);
                seg.raycastTarget = false;
                seg.gameObject.SetActive(false);
                SetField(hud, segment.fieldName, seg);
            }

            // 현금 칩. S-117 — 실아트 코인 아이콘 있으면 칩 왼쪽에 붙인다 (스왑 계약 — 없으면 텍스트만).
            Image moneyChip = CreateImage(content.transform, "MoneyChip", basicChipArt != null ? Color.clear : chipColor);
            AnchorCorner(moneyChip.rectTransform, new Vector2(0f, 1f), new Vector2(460f, -20f), new Vector2(250f, 64f));
            AddBasicChipBackground(moneyChip.transform, basicChipArt);
            Sprite coinArt = LoadUISprite("ui_coin");
            if (coinArt != null)
            {
                Image coinIcon = CreateImage(moneyChip.transform, "CoinIcon", Color.white);
                coinIcon.sprite = coinArt;
                coinIcon.raycastTarget = false;
                AnchorCorner(coinIcon.rectTransform, new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector2(44f, 44f));
            }
            TMP_Text money = CreateText(moneyChip.transform, "Money", "₩0", font,
                32f, chipTextColor, TextAlignmentOptions.Center);
            StretchFull(money.rectTransform);
            if (basicChipArt != null) money.rectTransform.offsetMin = new Vector2(48f, 0f);
            SetField(hud, "_moneyLabel", money);

            // 당일 배송수량 칩.
            Image countChip = CreateImage(content.transform, "DeliveryCountChip", basicChipArt != null ? Color.clear : chipColor);
            AnchorCorner(countChip.rectTransform, new Vector2(0f, 1f), new Vector2(730f, -20f), new Vector2(220f, 64f));
            AddBasicChipBackground(countChip.transform, basicChipArt);
            Sprite boxArt = LoadUISprite("ui_dialogue_arrow");
            if (boxArt != null)
            {
                Image boxIcon = CreateImage(countChip.transform, "BoxIcon", Color.white);
                boxIcon.sprite = boxArt;
                boxIcon.preserveAspect = true;
                boxIcon.raycastTarget = false;
                AnchorCorner(boxIcon.rectTransform, new Vector2(0f, 1f), new Vector2(10f, -4f), new Vector2(48f, 48f));
                Vector3 boxIconPosition = boxIcon.rectTransform.localPosition;
                boxIconPosition.z = 2f;
                boxIcon.rectTransform.localPosition = boxIconPosition;
            }
            TMP_Text countLabel = CreateText(countChip.transform, "Count", "박스 0/0", font,
                30f, chipTextColor, TextAlignmentOptions.Center);
            StretchFull(countLabel.rectTransform);
            if (basicChipArt != null) countLabel.rectTransform.offsetMin = new Vector2(52f, 0f);
            SetField(hud, "_deliveryCountLabel", countLabel);

            // 가방·설정 버튼 (시계 왼쪽).
            GameObject bagButton = BuildTopBarButton(content.transform, "BagButton", "bag_icon", new Vector2(-650f, -20f),
                bagView != null ? new UnityEngine.Events.UnityAction(bagView.Toggle) : null);
            // S-164 ② — 튜토리얼이 "가방" 단계일 때 이 버튼이 맥동한다(대상이 스스로 반응 — Find 불요).
            AttachHighlightTarget(bagButton, "bag");
            BuildTopBarButton(content.transform, "SettingsButton", "setting_button", new Vector2(-560f, -20f),
                settingsView != null ? new UnityEngine.Events.UnityAction(settingsView.Toggle) : null);

            // 빚 (우상, 시계 아래).
            TMP_Text debt = CreateText(content.transform, "Debt", "빚 ₩10,000", font,
                30f, new Color(0.95f, 0.55f, 0.55f, 1f), TextAlignmentOptions.TopRight);
            AnchorCorner(debt.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -104f), new Vector2(460f, 44f));
            SetField(hud, "_debtLabel", debt);

            // 배송 카드 (좌상 — 상단 바 아래) — 배경 + 주소 + 남은시간.
            GameObject card = CreateImage(content.transform, "DeliveryCard",
                new Color(0.10f, 0.12f, 0.16f, 0.85f)).gameObject;
            Image cardBg = card.GetComponent<Image>();
            AnchorCorner(cardBg.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -152f), new Vector2(560f, 150f));
            SetField(hud, "_cardRoot", card);
            SetField(hud, "_cardBackground", cardBg);

            TMP_Text address = CreateText(card.transform, "Address", "행복빌라 301호", font,
                40f, Color.white, TextAlignmentOptions.TopLeft);
            AnchorCorner(address.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -18f), new Vector2(512f, 56f));
            SetField(hud, "_addressLabel", address);

            TMP_Text remaining = CreateText(card.transform, "Remaining", "마감까지 --분", font,
                32f, CYAN, TextAlignmentOptions.BottomLeft);
            AnchorCorner(remaining.rectTransform, new Vector2(0f, 0f), new Vector2(24f, 18f), new Vector2(512f, 48f));
            SetField(hud, "_remainingLabel", remaining);

            // "E" 상호작용 안내 (하단 중앙) — 기본 숨김.
            TMP_Text ePrompt = CreateText(content.transform, "EPrompt", "[E] 상호작용", font,
                38f, CYAN, TextAlignmentOptions.Center);
            AnchorMiddleBottom(ePrompt.rectTransform, new Vector2(0f, 120f), new Vector2(640f, 60f));
            SetField(hud, "_ePrompt", ePrompt.gameObject);

            // S-169 — 보조 안내 한 줄. E 프롬프트(y 120, 높이 60) **바로 밑**에 눕힌다.
            // 형제로 두는 이유: E 프롬프트는 포커스에 따라 통째로 꺼지는데, 보조 안내는 조건이
            // 달라 따로 켜고 꺼야 한다(자식이면 부모가 꺼질 때 같이 죽어 제어가 겹친다).
            TMP_Text focusHint = CreateText(content.transform, "FocusHint", "", font,
                28f, new Color(1f, 0.72f, 0.25f, 1f), TextAlignmentOptions.Center);
            AnchorMiddleBottom(focusHint.rectTransform, new Vector2(0f, 84f), new Vector2(640f, 40f));
            focusHint.gameObject.SetActive(false);
            SetField(hud, "_focusHintLabel", focusHint);
        }

        // ── 상단 바 이미지 버튼 (S-063) ──────────────────────

        private static GameObject BuildTopBarButton(Transform parent, string name, string spriteName,
            Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f); // 기존 120×64 클릭 영역만 담당.
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(76f, 64f);
            rect.anchoredPosition = anchoredPos;

            // 교체된 두 PNG는 1536×1024 투명 캔버스 안 약 1.2:1 정사각형 버튼 아트다.
            // Image Rect를 183×122로 보정하면 실제 보이는 패널이 약 70×58이 된다.
            Image art = CreateImage(go.transform, "Art", Color.white);
            art.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PANEL_UI_ROOT + spriteName + ".png");
            art.preserveAspect = true;
            art.raycastTarget = false;
            RectTransform artRect = art.rectTransform;
            artRect.anchorMin = artRect.anchorMax = artRect.pivot = new Vector2(0.5f, 0.5f);
            artRect.sizeDelta = new Vector2(183f, 122f);
            artRect.anchoredPosition = Vector2.zero;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = art; // 호버·클릭 색 변화는 실아트에 적용.
            if (onClick != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, onClick);
            return go; // S-164 ② — 호출부가 하이라이트 대상으로 등록할 수 있게 돌려준다
        }

        // ── 가방 캔버스 (S-064) ──────────────────────────────

        /// <summary>
        /// S-205 — 가방 슬롯 호버 안내('[좌클릭] 사용')를 보장한다(멱등).
        /// 가방 UI는 **아트 프리팹 경로와 코드 폴백 경로 둘**이라 한쪽에만 넣으면 조용히 빠진다 —
        /// 실제로 쓰이는 건 프리팹 쪽이었다. 두 경로가 같은 것을 쓰도록 헬퍼로 뺀다.
        /// 붙일 곳은 패널 하단 가운데. 평소엔 꺼져 있고 슬롯에 마우스를 올릴 때만 켜진다.
        /// </summary>
        private static void EnsureBagHoverHint(BagView view, Transform parent)
        {
            if (view == null || parent == null) return;

            Transform existing = parent.Find("HoverHint");
            TMP_Text hint = existing != null ? existing.GetComponent<TMP_Text>() : null;
            if (hint == null)
            {
                TMP_FontAsset hintFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
                hint = CreateText(parent, "HoverHint", "[좌클릭] 사용", hintFont, 24f,
                    new Color(0.86f, 0.90f, 0.97f, 1f), TextAlignmentOptions.Center);
            }

            RectTransform rect = hint.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(420f, 34f);
            rect.anchoredPosition = new Vector2(0f, 14f);
            hint.gameObject.SetActive(false); // 호버할 때만 보인다

            SetField(view, "_hoverHint", hint);
        }

        /// <summary>
        /// S-243 — 우클릭 메뉴에 '사용' 칸을 보장한다. 가방 UI는 **아트 프리팹 경로가 실제로 쓰이는 쪽**
        /// 이라(S-205 실측), 코드 폴백에만 버튼을 넣으면 게임에는 안 나온다.
        /// 프리팹에는 [손에 들기][버리기] 둘뿐이므로 '손에 들기'를 복제해 스타일을 그대로 물려받고,
        /// 세 칸이 들어가게 간격만큼 아래로 밀고 메뉴를 늘린다.
        /// </summary>
        private static void EnsureBagConsumeButton(BagView view)
        {
            if (view == null) return;
            GameObject context = GetField<GameObject>(view, "_contextMenu");
            Button hold = GetField<Button>(view, "_useButton");
            Button drop = GetField<Button>(view, "_dropButton");
            if (context == null || hold == null || drop == null) return;

            Transform existing = context.transform.Find("ConsumeButton");
            Button consume = existing != null ? existing.GetComponent<Button>() : null;
            if (consume == null)
            {
                GameObject clone = Object.Instantiate(hold.gameObject, context.transform);
                clone.name = "ConsumeButton";
                consume = clone.GetComponent<Button>();
                consume.onClick.RemoveAllListeners(); // 원본에 직렬화된 연결이 있으면 끊는다
                TMP_Text label = clone.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = "사용";
                if (consume.targetGraphic is Image image) image.color = new Color(0.23f, 0.50f, 0.62f, 1f);
                clone.transform.SetAsFirstSibling();
            }

            // S-252 — '손에 들기' 칸을 없앤다(남규님 지시). 아트 프리팹의 그 버튼 라벨이 "사용"이라
            // 화면에는 `[사용][사용][버리기]`로 같은 글자가 두 번 떴다. 자리를 비우는 대신 아예 끄고
            // 참조도 끊는다 — 살아 있으면 `BagView`가 상황에 따라 도로 켠다.
            //   ⚠ 부수 효과: '손에 들기'(S-064 들고 던지기) 진입로가 우클릭에서 사라진다.
            //   좌클릭은 소비 리스너가 없는 씬(집 등)에서 여전히 손에 들기로 떨어진다.
            ((RectTransform)consume.transform).anchoredPosition = ((RectTransform)hold.transform).anchoredPosition;
            hold.gameObject.SetActive(false);
            SetField(view, "_useButton", null);

            SetField(view, "_consumeButton", consume);
        }

        private static T GetField<T>(object target, string fieldName) where T : class
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(target) as T;
        }

        private static BagView BuildBagCanvas(GameStateSO gameState)
        {
            GameObject inventoryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(INVENTORY_UI_PREFAB_PATH);
            if (inventoryPrefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(inventoryPrefab);
                instance.name = "BagCanvas";
                BagView prefabView = instance.GetComponent<BagView>();
                SetField(prefabView, "_gameState", gameState);
                EnsureBagConsumeButton(prefabView); // S-243
                // S-205 — 아트 프리팹 경로에도 호버 안내를 얹는다. 이 분기가 **실제로 쓰이는 쪽**이라
                // 아래 코드 폴백에만 넣었다가 안내가 안 뜬다(실측 — HoverHint 오브젝트 자체가 없었다).
                EnsureBagHoverHint(prefabView, instance.transform);
                EditorUtility.SetDirty(prefabView);
                return prefabView;
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            Sprite inventoryArt = LoadSpriteSubAsset("Assets/Art/UI/KioskPanel/ui_back.png");
            Sprite slotFrame = LoadSpriteSubAsset("Assets/Art/UI/KioskPanel/square-box.png");
            Sprite buttonFrame = LoadSpriteSubAsset("Assets/Art/UI/KioskPanel/xButton.png");
            bool hasInventoryArt = inventoryArt != null;
            Texture2D drinkIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/ui_kiosk_drink.png");
            Texture2D waterIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/ui_kiosk_water.png");
            Texture2D cocoaIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/ui_kiosk_cocoa.png");
            Texture2D odengIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/ui_kiosk_odeng.png");
            Texture2D flowerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/ui_kiosk_flower.png");

            GameObject canvasGo = new GameObject("BagCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            BagView view = canvasGo.AddComponent<BagView>();
            SetField(view, "_gameState", gameState);

            Image panel = CreateImage(canvasGo.transform, "Panel", hasInventoryArt ? Color.white : CYAN);
            if (hasInventoryArt)
            {
                panel.sprite = inventoryArt;
                panel.preserveAspect = true;
            }
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = hasInventoryArt ? new Vector2(680f, 796f) : new Vector2(680f, 300f);
            Image inner = CreateImage(panel.transform, "Inner", hasInventoryArt ? Color.clear : NAVY);
            inner.raycastTarget = true;
            RectTransform innerRect = inner.rectTransform;
            innerRect.anchorMin = Vector2.zero; innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(3f, 3f); innerRect.offsetMax = new Vector2(-3f, -3f);

            TMP_Text title = CreateText(inner.transform, "Title", "가방", font, 36f,
                hasInventoryArt ? new Color(0.13f, 0.19f, 0.22f) : Color.white,
                TextAlignmentOptions.TopLeft);
            AnchorCorner(title.rectTransform, new Vector2(0f, 1f),
                hasInventoryArt ? new Vector2(40f, -112f) : new Vector2(24f, -16f), new Vector2(200f, 48f));

            GameObject closeGo = new GameObject("CloseButton", typeof(RectTransform));
            closeGo.transform.SetParent(inner.transform, false);
            Image closeImg = closeGo.AddComponent<Image>();
            closeImg.color = hasInventoryArt ? Color.white : new Color(0.55f, 0.25f, 0.25f, 1f);
            if (hasInventoryArt && buttonFrame != null)
            {
                closeImg.sprite = buttonFrame;
                closeImg.type = Image.Type.Sliced;
            }
            RectTransform closeRect = (RectTransform)closeGo.transform;
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(72f, 48f);
            closeRect.anchoredPosition = hasInventoryArt ? new Vector2(-42f, -106f) : new Vector2(-16f, -12f);
            Button closeButton = closeGo.AddComponent<Button>();
            closeButton.targetGraphic = closeImg;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(closeButton.onClick,
                new UnityEngine.Events.UnityAction(view.Close));
            TMP_Text closeLabel = CreateText(closeGo.transform, "Label", "←", font, 30f,
                hasInventoryArt ? new Color(0.13f, 0.25f, 0.27f) : Color.white,
                hasInventoryArt ? TextAlignmentOptions.CenterGeoAligned : TextAlignmentOptions.Center);
            StretchFull(closeLabel.rectTransform);

            GameObject gridGo = new GameObject("SlotGrid", typeof(RectTransform));
            gridGo.transform.SetParent(inner.transform, false);
            RectTransform gridRect = (RectTransform)gridGo.transform;
            gridRect.anchorMin = gridRect.anchorMax = new Vector2(0.5f, 1f);
            gridRect.pivot = new Vector2(0.5f, 1f);
            gridRect.sizeDelta = new Vector2(570f, 140f);
            gridRect.anchoredPosition = new Vector2(0f, hasInventoryArt ? -185f : -100f);
            GridLayoutGroup grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = hasInventoryArt ? new Vector2(120f, 120f) : new Vector2(112f, 112f);
            grid.spacing = hasInventoryArt ? new Vector2(20f, 0f) : new Vector2(14f, 0f);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = BagStorage.CAPACITY;

            var slots = new BagSlot[BagStorage.CAPACITY];
            for (int i = 0; i < slots.Length; i++)
            {
                GameObject slotGo = new GameObject("Slot_" + i, typeof(RectTransform));
                slotGo.transform.SetParent(gridGo.transform, false);
                Image slotBg = slotGo.AddComponent<Image>();
                slotBg.color = hasInventoryArt ? Color.white : new Color(0.14f, 0.17f, 0.24f, 0.9f);
                if (hasInventoryArt && slotFrame != null)
                {
                    slotBg.sprite = slotFrame;
                    slotBg.type = Image.Type.Simple;
                    slotBg.preserveAspect = true;
                }
                RectTransform slotRect = (RectTransform)slotGo.transform;
                slotRect.sizeDelta = hasInventoryArt ? new Vector2(120f, 120f) : new Vector2(112f, 112f);

                GameObject iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(slotGo.transform, false);
                RawImage icon = iconGo.AddComponent<RawImage>();
                icon.raycastTarget = false;
                icon.enabled = false;
                RectTransform iconRect = icon.rectTransform;
                iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = Vector2.zero;

                TMP_Text label = CreateText(slotGo.transform, "Label", string.Empty, font, 22f,
                    hasInventoryArt ? new Color(0.13f, 0.19f, 0.22f) : Color.white,
                    TextAlignmentOptions.Center);
                StretchFull(label.rectTransform);

                TMP_Text count = CreateText(slotGo.transform, "Count", string.Empty, font, 22f,
                    hasInventoryArt ? new Color(0.20f, 0.48f, 0.47f) : CYAN,
                    TextAlignmentOptions.BottomRight);
                StretchFull(count.rectTransform);

                BagSlot slot = slotGo.AddComponent<BagSlot>();
                SetField(slot, "_view", view);
                SetField(slot, "_index", i);
                SetField(slot, "_background", slotBg);
                SetField(slot, "_label", label);
                SetField(slot, "_countLabel", count);
                SetField(slot, "_icon", icon);
                SetField(slot, "_drinkIcon", drinkIcon);
                SetField(slot, "_waterIcon", waterIcon);
                SetField(slot, "_cocoaIcon", cocoaIcon);
                SetField(slot, "_odengIcon", odengIcon);
                SetField(slot, "_flowerIcon", flowerIcon);
                SetField(slot, "_illustratedStyle", hasInventoryArt);
                slots[i] = slot;
            }
            SerializedObject viewSerialized = new SerializedObject(view);
            SerializedProperty slotsProp = viewSerialized.FindProperty("_slots");
            slotsProp.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            viewSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject context = new GameObject("ContextMenu", typeof(RectTransform));
            context.transform.SetParent(canvasGo.transform, false);
            Image contextBg = context.AddComponent<Image>();
            contextBg.color = new Color(0.08f, 0.10f, 0.15f, 0.98f);
            RectTransform contextRect = (RectTransform)context.transform;
            contextRect.sizeDelta = new Vector2(150f, 118f); // S-252 — [사용][버리기] 두 칸

            Button MakeContextButton(string btnName, string btnLabel, float y, Color color)
            {
                GameObject go = new GameObject(btnName, typeof(RectTransform));
                go.transform.SetParent(context.transform, false);
                Image img = go.AddComponent<Image>();
                img.color = color;
                RectTransform rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(130f, 46f);
                rect.anchoredPosition = new Vector2(0f, y);
                Button b = go.AddComponent<Button>();
                b.targetGraphic = img;
                TMP_Text t = CreateText(go.transform, "Label", btnLabel, font, 26f, Color.white,
                    TextAlignmentOptions.Center);
                StretchFull(t.rectTransform);
                return b;
            }

            // S-243 — '사용'을 첫 칸으로. 우클릭으로 쓰려는 손이 실재한다(남규님 지적).
            // S-252 — '손에 들기' 칸은 만들지 않는다(남규님 지시).
            Button consumeButton = MakeContextButton("ConsumeButton", "사용", -10f, new Color(0.23f, 0.50f, 0.62f, 1f));
            Button dropButton = MakeContextButton("DropButton", "버리기", -62f, new Color(0.55f, 0.30f, 0.28f, 1f));

            SetField(view, "_panel", panel.gameObject);
            SetField(view, "_contextMenu", context);
            SetField(view, "_consumeButton", consumeButton);
            SetField(view, "_dropButton", dropButton);
            EnsureBagHoverHint(view, panel.transform); // S-205
            EditorUtility.SetDirty(view);
            panel.gameObject.SetActive(false);
            context.SetActive(false);
            return view;
        }

        // ── 설정 캔버스 (S-065) ──────────────────────────────

        private static SettingsView BuildSettingsCanvas()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);

            GameObject canvasGo = new GameObject("SettingsCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 62;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            SettingsView view = canvasGo.AddComponent<SettingsView>();

            Image panel = CreateImage(canvasGo.transform, "Panel", CYAN);
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(560f, 420f);
            Image inner = CreateImage(panel.transform, "Inner", NAVY);
            inner.raycastTarget = true;
            RectTransform innerRect = inner.rectTransform;
            innerRect.anchorMin = Vector2.zero; innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(3f, 3f); innerRect.offsetMax = new Vector2(-3f, -3f);

            TMP_Text title = CreateText(inner.transform, "Title", "설정", font, 36f, Color.white,
                TextAlignmentOptions.TopLeft);
            AnchorCorner(title.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(200f, 48f));

            Slider MakeVolumeSlider(string name, string labelText, float y)
            {
                TMP_Text label = CreateText(inner.transform, name + "Label", labelText, font, 28f, Color.white,
                    TextAlignmentOptions.Left);
                AnchorCorner(label.rectTransform, new Vector2(0f, 1f), new Vector2(32f, y), new Vector2(160f, 44f));

                GameObject sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
                sliderGo.name = name;
                sliderGo.transform.SetParent(inner.transform, false);
                RectTransform sliderRect = (RectTransform)sliderGo.transform;
                sliderRect.anchorMin = sliderRect.anchorMax = sliderRect.pivot = new Vector2(0f, 1f);
                sliderRect.sizeDelta = new Vector2(300f, 30f);
                sliderRect.anchoredPosition = new Vector2(200f, y - 8f);
                return sliderGo.GetComponent<Slider>();
            }

            Slider bgm = MakeVolumeSlider("BgmSlider", "배경음", -90f);
            Slider sfx = MakeVolumeSlider("SfxSlider", "효과음", -160f);

            Button MakePanelButton(string name, string labelText, float y, Color color)
            {
                GameObject go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(inner.transform, false);
                Image img = go.AddComponent<Image>();
                img.color = color;
                RectTransform rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
                rect.sizeDelta = new Vector2(360f, 64f);
                rect.anchoredPosition = new Vector2(0f, y);
                Button b = go.AddComponent<Button>();
                b.targetGraphic = img;
                TMP_Text t = CreateText(go.transform, "Label", labelText, font, 28f, Color.white,
                    TextAlignmentOptions.Center);
                StretchFull(t.rectTransform);
                return b;
            }

            Button titleButton = MakePanelButton("TitleButton", "처음 화면으로", 110f, new Color(0.55f, 0.30f, 0.28f, 1f));
            Button closeButton = MakePanelButton("CloseButton", "뒤로가기", 32f, new Color(0.21f, 0.42f, 0.55f, 1f));

            SetField(view, "_panel", panel.gameObject);
            SetField(view, "_bgmSlider", bgm);
            SetField(view, "_sfxSlider", sfx);
            SetField(view, "_titleButton", titleButton);
            SetField(view, "_closeButton", closeButton);
            EditorUtility.SetDirty(view);
            panel.gameObject.SetActive(false);
            return view;
        }

        // ── 획득 알림 토스트 (S-133 ⑤) ──────────────────────
        // sortingOrder 96 — 미니게임(95) 위·페이드(100) 아래. 정산창 위에서도 보여야 한다.
        private static void BuildToastCanvas()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);

            GameObject canvasGo = new GameObject("ToastCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 96;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>().enabled = false; // 표시 전용 — 입력을 막지 않는다

            Image plate = CreateImage(canvasGo.transform, "ToastPlate", new Color(0.06f, 0.07f, 0.10f, 0.88f));
            AnchorCorner(plate.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(720f, 72f));
            CanvasGroup group = plate.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            TMP_Text label = CreateText(plate.transform, "ToastLabel", "", font,
                36f, Color.white, TextAlignmentOptions.Center);
            StretchFull(label.rectTransform);

            ToastView view = canvasGo.AddComponent<ToastView>();
            SetField(view, "_group", group);
            SetField(view, "_label", label);
            EditorUtility.SetDirty(view);
        }

        // ── 교통사고 캔버스 (S-066 ③) ───────────────────────

        private static void BuildAccidentCanvas()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);

            GameObject canvasGo = new GameObject("AccidentCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 70;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            AccidentView view = canvasGo.AddComponent<AccidentView>();

            // 붉은 플래시 — 풀스크린.
            Image flash = CreateImage(canvasGo.transform, "RedFlash", new Color(0.85f, 0.1f, 0.08f, 0f));
            StretchFull(flash.rectTransform);
            flash.raycastTarget = false;

            // S-119 ② — 병원 영수증 종이 (S-087 정산 영수증 룩: 종이색 + 상하 톱니 절취선).
            Color paper = new Color(0.97f, 0.97f, 0.95f, 1f);
            Image panel = CreateImage(canvasGo.transform, "Panel", paper);
            panel.raycastTarget = true;
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(560f, 640f);
            for (int tooth = 0; tooth < 14; tooth++) // 톱니 절취선 — 위·아래
            {
                foreach (float side in new[] { 1f, -1f })
                {
                    Image diamond = CreateImage(panel.transform, "Tooth", paper);
                    diamond.raycastTarget = false;
                    RectTransform toothRect = diamond.rectTransform;
                    toothRect.anchorMin = toothRect.anchorMax = new Vector2(0f, side > 0 ? 1f : 0f);
                    toothRect.pivot = new Vector2(0.5f, 0.5f);
                    toothRect.sizeDelta = new Vector2(28f, 28f);
                    toothRect.anchoredPosition = new Vector2(20f + tooth * 40f, 0f);
                    toothRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
                }
            }

            TMP_Text body = CreateText(panel.transform, "Body", string.Empty, font, 26f,
                new Color(0.13f, 0.15f, 0.19f), TextAlignmentOptions.Top);
            RectTransform bodyRect = body.rectTransform;
            bodyRect.anchorMin = Vector2.zero; bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(44f, 120f); bodyRect.offsetMax = new Vector2(-44f, -40f);

            GameObject homeGo = new GameObject("HomeButton", typeof(RectTransform));
            homeGo.transform.SetParent(panel.transform, false);
            Image homeImg = homeGo.AddComponent<Image>();
            homeImg.color = new Color(0.208f, 0.878f, 0.784f, 1f);
            RectTransform homeRect = (RectTransform)homeGo.transform;
            homeRect.anchorMin = homeRect.anchorMax = homeRect.pivot = new Vector2(0.5f, 0f);
            homeRect.sizeDelta = new Vector2(360f, 72f);
            homeRect.anchoredPosition = new Vector2(0f, 32f);
            Button homeButton = homeGo.AddComponent<Button>();
            homeButton.targetGraphic = homeImg;
            TMP_Text homeLabel = CreateText(homeGo.transform, "Label", "치료 후 집으로", font, 30f, NAVY,
                TextAlignmentOptions.Center);
            StretchFull(homeLabel.rectTransform);

            SetField(view, "_gameState",
                AssetDatabase.LoadAssetAtPath<GameStateSO>(DATA_ROOT + "/GameState.asset")); // S-119 ② — 잔액·빚
            SetField(view, "_panel", panel.gameObject);
            SetField(view, "_bodyLabel", body);
            SetField(view, "_homeButton", homeButton);
            SetField(view, "_redFlash", flash);
            EditorUtility.SetDirty(view);
            panel.gameObject.SetActive(false);
            flash.gameObject.SetActive(false);
        }

        // ── 노점 구매창 (S-125 ② — 자판기·편의점·포장마차 공용) ──
        private static void BuildKioskCanvas(GameStateSO gameState)
        {
            GameObject kioskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(KIOSK_UI_PREFAB_PATH);
            if (kioskPrefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(kioskPrefab);
                instance.name = "KioskCanvas";
                KioskView prefabView = instance.GetComponent<KioskView>();
                SetField(prefabView, "_gameState", gameState);
                EditorUtility.SetDirty(prefabView);
                return;
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            Sprite vendingArt = LoadSpriteSubAsset("Assets/Art/UI/KioskPanel/ui_back.png");
            Sprite rowFrame = LoadSpriteSubAsset("Assets/Art/UI/KioskPanel/square-box.png");
            Sprite closeFrame = LoadSpriteSubAsset("Assets/Art/UI/KioskPanel/xButton.png");
            Sprite purchaseButtonFrame = LoadSpriteSubAsset("Assets/Art/UI/KioskPanel/rec_box.png");
            bool hasVendingArt = vendingArt != null;

            GameObject canvasGo = new GameObject("KioskCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80; // 폰(85)보다 아래, HUD보다 위
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            KioskView view = canvasGo.AddComponent<KioskView>();
            Texture2D drinkIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/ui_kiosk_drink.png");
            Texture2D waterIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/ui_kiosk_water.png");
            Texture2D cocoaIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/ui_kiosk_cocoa.png");
            Texture2D odengIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/ui_kiosk_odeng.png");
            Texture2D flowerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/ui_kiosk_flower.png");

            Image panel = CreateImage(canvasGo.transform, "Panel",
                hasVendingArt ? Color.white : new Color(0.10f, 0.12f, 0.17f, 0.97f));
            if (hasVendingArt)
            {
                panel.sprite = vendingArt;
                panel.preserveAspect = true;
            }
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = hasVendingArt ? new Vector2(620f, 728f) : new Vector2(620f, 520f);

            TMP_Text title = CreateText(panel.transform, "Title", "자판기", font, 40f,
                hasVendingArt ? new Color(0.13f, 0.19f, 0.22f) : CYAN, TextAlignmentOptions.Top);
            AnchorCorner(title.rectTransform, new Vector2(0.5f, 1f),
                new Vector2(0f, hasVendingArt ? -112f : -18f), new Vector2(560f, 54f));

            TMP_Text money = CreateText(panel.transform, "Money", "소지금 ₩0", font, 26f,
                hasVendingArt ? new Color(0.26f, 0.48f, 0.49f) : new Color(0.75f, 0.80f, 0.90f),
                TextAlignmentOptions.Top);
            AnchorCorner(money.rectTransform, new Vector2(0.5f, 1f),
                new Vector2(0f, hasVendingArt ? -160f : -74f), new Vector2(560f, 36f));

            GameObject list = new GameObject("List", typeof(RectTransform));
            list.transform.SetParent(panel.transform, false);
            RectTransform listRect = (RectTransform)list.transform;
            listRect.anchorMin = new Vector2(0f, 0f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.offsetMin = hasVendingArt ? new Vector2(52f, 110f) : new Vector2(0f, 90f);
            listRect.offsetMax = hasVendingArt ? new Vector2(-52f, -220f) : new Vector2(0f, -116f);
            if (hasVendingArt)
            {
                VerticalLayoutGroup listLayout = list.AddComponent<VerticalLayoutGroup>();
                listLayout.padding = new RectOffset(0, 0, 0, 0);
                listLayout.spacing = 8f;
                listLayout.childAlignment = TextAnchor.UpperCenter;
                listLayout.childControlWidth = true;
                listLayout.childControlHeight = true;
                listLayout.childForceExpandWidth = true;
                listLayout.childForceExpandHeight = false;
            }

            GameObject closeGo = new GameObject("CloseButton", typeof(RectTransform));
            closeGo.transform.SetParent(panel.transform, false);
            Image closeImg = closeGo.AddComponent<Image>();
            closeImg.color = hasVendingArt ? Color.white : new Color(0.25f, 0.28f, 0.34f, 1f);
            if (hasVendingArt && closeFrame != null)
            {
                closeImg.sprite = closeFrame;
                closeImg.type = Image.Type.Sliced;
            }
            RectTransform closeRect = (RectTransform)closeGo.transform;
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot =
                hasVendingArt ? new Vector2(1f, 1f) : new Vector2(0.5f, 0f);
            closeRect.sizeDelta = hasVendingArt ? new Vector2(58f, 58f) : new Vector2(300f, 60f);
            closeRect.anchoredPosition = hasVendingArt ? new Vector2(-74f, -80f) : new Vector2(0f, 18f);
            Button closeButton = closeGo.AddComponent<Button>();
            closeButton.targetGraphic = closeImg;
            TMP_Text closeLabel = CreateText(closeGo.transform, "Label", hasVendingArt ? "X" : "닫기 (ESC)", font,
                hasVendingArt ? 30f : 26f,
                hasVendingArt ? new Color(0.28f, 0.32f, 0.34f) : Color.white,
                hasVendingArt ? TextAlignmentOptions.CenterGeoAligned : TextAlignmentOptions.Center);
            StretchFull(closeLabel.rectTransform);

            SetField(view, "_gameState", gameState);
            SetField(view, "_panel", panel.gameObject);
            SetField(view, "_titleLabel", title);
            SetField(view, "_moneyLabel", money);
            SetField(view, "_listRoot", listRect);
            SetField(view, "_closeButton", closeButton);
            SetField(view, "_font", font);
            SetField(view, "_drinkIcon", drinkIcon);
            SetField(view, "_waterIcon", waterIcon);
            SetField(view, "_cocoaIcon", cocoaIcon);
            SetField(view, "_odengIcon", odengIcon);
            SetField(view, "_flowerIcon", flowerIcon);
            SetField(view, "_rowFrame", rowFrame);
            SetField(view, "_buttonFrame", purchaseButtonFrame);
            EditorUtility.SetDirty(view);
            panel.gameObject.SetActive(false);
        }

        // ── 송장 캔버스 (S-071 ② — 상자 좌클릭 → 주문 정보) ──
        private static void BuildInvoiceCanvas(GameStateSO gameState)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);

            GameObject canvasGo = new GameObject("InvoiceCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            InvoiceView view = canvasGo.AddComponent<InvoiceView>();

            // 송장지 — 흰 종이 + 네이비 헤더 (실물 운송장 무드).
            Image paper = CreateImage(canvasGo.transform, "Paper", new Color(0.96f, 0.95f, 0.92f, 1f));
            RectTransform paperRect = paper.rectTransform;
            paperRect.anchorMin = paperRect.anchorMax = paperRect.pivot = new Vector2(0.5f, 0.5f);
            paperRect.sizeDelta = new Vector2(620f, 500f);

            Image header = CreateImage(paper.transform, "Header", NAVY);
            RectTransform headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0f, 1f); headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.offsetMin = new Vector2(0f, -64f); headerRect.offsetMax = Vector2.zero;
            TMP_Text headerLabel = CreateText(header.transform, "Title", "택배 송장", font, 32f,
                Color.white, TextAlignmentOptions.Center);
            StretchFull(headerLabel.rectTransform);

            TMP_Text customer = CreateText(paper.transform, "Customer", string.Empty, font, 30f,
                new Color(0.12f, 0.13f, 0.18f), TextAlignmentOptions.TopLeft);
            AnchorCorner(customer.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -84f), new Vector2(548f, 44f));

            TMP_Text address = CreateText(paper.transform, "Address", string.Empty, font, 34f,
                new Color(0.12f, 0.13f, 0.18f), TextAlignmentOptions.TopLeft);
            AnchorCorner(address.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -132f), new Vector2(548f, 92f));

            TMP_Text deadline = CreateText(paper.transform, "Deadline", string.Empty, font, 30f,
                new Color(0.12f, 0.13f, 0.18f), TextAlignmentOptions.TopLeft);
            AnchorCorner(deadline.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -232f), new Vector2(548f, 44f));

            TMP_Text detail = CreateText(paper.transform, "Detail", string.Empty, font, 26f,
                new Color(0.30f, 0.32f, 0.38f), TextAlignmentOptions.TopLeft);
            AnchorCorner(detail.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -282f), new Vector2(548f, 84f));

            // 바코드 밴드 — 흰 바탕 위 세로 줄무늬(런타임 생성) + 번호.
            Image barcodeBand = CreateImage(paper.transform, "BarcodeBand", Color.white);
            RectTransform bandRect = barcodeBand.rectTransform;
            bandRect.anchorMin = new Vector2(0f, 0f); bandRect.anchorMax = new Vector2(1f, 0f);
            bandRect.pivot = new Vector2(0.5f, 0f);
            bandRect.offsetMin = new Vector2(36f, 66f); bandRect.offsetMax = new Vector2(-36f, 130f);
            TMP_Text barcodeNumber = CreateText(paper.transform, "BarcodeNo", string.Empty, font, 22f,
                new Color(0.30f, 0.32f, 0.38f), TextAlignmentOptions.Center);
            AnchorCorner(barcodeNumber.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(400f, 32f));

            TMP_Text hint = CreateText(paper.transform, "Hint", "ESC 또는 클릭으로 닫기", font, 18f,
                new Color(0.55f, 0.57f, 0.62f), TextAlignmentOptions.Center);
            AnchorCorner(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(400f, 24f));

            SetField(view, "_gameState", gameState);
            SetField(view, "_root", paper.gameObject);
            SetField(view, "_customerLabel", customer);
            SetField(view, "_addressLabel", address);
            SetField(view, "_deadlineLabel", deadline);
            SetField(view, "_detailLabel", detail);
            SetField(view, "_barcodeNumberLabel", barcodeNumber);
            SetField(view, "_barcodeRoot", barcodeBand.rectTransform);
            EditorUtility.SetDirty(view);
            paper.gameObject.SetActive(false);

            // S-073 ③ — 상자 호버 툴팁 (같은 오버레이 캔버스에 얹는다 — 마우스 추적 라벨 1개).
            BoxTooltipView tooltip = canvasGo.AddComponent<BoxTooltipView>();
            TMP_Text tooltipLabel = CreateText(canvasGo.transform, "BoxTooltip", string.Empty, font, 24f,
                Color.white, TextAlignmentOptions.BottomLeft);
            RectTransform tooltipRect = tooltipLabel.rectTransform;
            tooltipRect.anchorMin = tooltipRect.anchorMax = Vector2.zero;
            tooltipRect.pivot = new Vector2(0f, 0f);
            tooltipRect.sizeDelta = new Vector2(640f, 34f);
            tooltipLabel.textWrappingMode = TextWrappingModes.NoWrap;
            tooltipLabel.fontStyle = FontStyles.Bold;
            tooltipLabel.gameObject.SetActive(false);
            SetField(tooltip, "_gameState", gameState);
            SetField(tooltip, "_label", tooltipLabel);
            EditorUtility.SetDirty(tooltip);
        }

        // ── 대화 캔버스 (Core 상주) ──────────────────────────

        private static void BuildDialogueCanvas()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            TMP_FontAsset dialogueFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DIALOGUE_FONT_PATH) ?? font;
            AudioClip blip = EnsureBlipClip();
            EnsureTestScenario();

            // Canvas: Overlay · sortOrder 90 (HUD 위) · Scale With Screen Size 1920×1080.
            GameObject canvasGo = new GameObject("DialogueCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            DialogueView view = canvasGo.AddComponent<DialogueView>();

            // 로컬 2D 블립 소스.
            AudioSource blipSource = canvasGo.AddComponent<AudioSource>();
            blipSource.playOnAwake = false;
            blipSource.spatialBlend = 0f;
            SetField(view, "_blipSource", blipSource);
            SetField(view, "_blipClip", blip);

            // 박스 루트 (평소 숨김). 하단 가로 박스 — 실아트(ui_dialogue_box) 있으면 사용, 없으면 시안 테두리 폴백 (S-025).
            Sprite boxArt = LoadUISprite("ui_dialogue_box");
            Image borderImage = CreateImage(canvasGo.transform, "Box", boxArt != null ? Color.white : CYAN);
            if (boxArt != null) borderImage.sprite = boxArt;
            GameObject border = borderImage.gameObject;
            RectTransform borderRect = border.GetComponent<RectTransform>();
            // Ramche 적용 후 본문 가로 공간이 부족해 실아트 박스를 좌우로 확장한다.
            AnchorMiddleBottom(borderRect, new Vector2(0f, 50f),
                boxArt != null ? new Vector2(1450f, 447f) : new Vector2(1820f, 260f));
            borderRect.localScale = Vector3.one * 0.7f;
            SetField(view, "_box", border);

            // 네이비 반투명 내부 (테두리보다 3px 안쪽) — 클릭 진행용 Button 타겟.
            // 실아트가 배경을 가지므로 그때는 투명(클릭 타겟 역할만).
            Image inner = CreateImage(border.transform, "Inner", boxArt != null ? Color.clear : NAVY);
            inner.raycastTarget = true;
            RectTransform innerRect = inner.rectTransform;
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(3f, 3f);
            innerRect.offsetMax = new Vector2(-3f, -3f);
            Button advanceButton = inner.gameObject.AddComponent<Button>();
            advanceButton.transition = Selectable.Transition.None;
            advanceButton.targetGraphic = inner;
            SetField(view, "_advanceButton", advanceButton);

            // 이름표 — 실아트 좌상 명찰 탭 중앙에 (탭 위치 = 크롭 아트 좌표 ×0.8375 스케일 환산, S-027).
            TMP_Text nameLabel = CreateText(inner.transform, "Name", "박말순", dialogueFont,
                34f, boxArt != null ? new Color(0.10f, 0.30f, 0.22f) : AMBER,
                boxArt != null ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft);
            nameLabel.fontStyle = FontStyles.Bold; // S-027 ② (민지: 이름·내용 볼드)
            AnchorCorner(nameLabel.rectTransform, new Vector2(0f, 1f),
                boxArt != null ? new Vector2(60f, -8f) : new Vector2(44f, -18f),
                boxArt != null ? new Vector2(450f, 115f) : new Vector2(600f, 46f));
            SetField(view, "_nameLabel", nameLabel);

            // 본문 — 실아트 내부가 밝아서 어두운 글자 (흰 글자는 소실). 흰 영역은 명찰 탭 아래부터.
            TMP_Text body = CreateText(inner.transform, "Body", string.Empty, dialogueFont,
                40f, boxArt != null ? new Color(0.12f, 0.14f, 0.18f) : Color.white, TextAlignmentOptions.TopLeft);
            body.fontStyle = FontStyles.Bold; // S-027 ②
            body.textWrappingMode = TextWrappingModes.Normal;
            RectTransform bodyRect = body.rectTransform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            // 사용자 실조정: 첫 글자가 좌측 프레임과 충분히 떨어지도록 오른쪽 이동.
            bodyRect.offsetMin = boxArt != null ? new Vector2(120f, 55f) : new Vector2(84f, 24f);
            bodyRect.offsetMax = boxArt != null ? new Vector2(-80f, -150f) : new Vector2(-44f, -74f);
            SetField(view, "_bodyLabel", body);

            // 대기 화살표 (우하, 기본 숨김) — 실아트(ui_dialogue_arrow) 있으면 이미지, 없으면 "▼" 텍스트 (S-025).
            Sprite arrowArt = LoadUISprite("ui_dialogue_arrow");
            GameObject arrowGo;
            if (arrowArt != null)
            {
                Image arrowImage = CreateImage(inner.transform, "Arrow", Color.white);
                arrowImage.sprite = arrowArt;
                arrowImage.preserveAspect = true;
                // S-027 ⑤: 테두리 안쪽 흰 영역 우하단에 (민지 목업 배치). 크롭 아트 비율 0.75.
                AnchorCorner(arrowImage.rectTransform, new Vector2(1f, 0f), new Vector2(-95f, 62f), new Vector2(78f, 104f));
                arrowImage.gameObject.AddComponent<UIPulse>().Configure(0.3f, 1f, 5f); // "▼ 대신 박스 깜박" (아트팀)
                arrowGo = arrowImage.gameObject;
            }
            else
            {
                TMP_Text arrow = CreateText(inner.transform, "Arrow", "▼", font,
                    40f, CYAN, TextAlignmentOptions.BottomRight);
                AnchorCorner(arrow.rectTransform, new Vector2(1f, 0f), new Vector2(-30f, 18f), new Vector2(60f, 60f));
                arrowGo = arrow.gameObject;
            }
            arrowGo.SetActive(false);
            SetField(view, "_arrow", arrowGo);
        }

        // 진상 전화 리듬 오버레이 (S-007). 대화 캔버스보다 위(sortOrder 95) — 평소 패널 숨김.
        private static void BuildMinigameCanvas()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            TuningConfigSO tuning = AssetDatabase.LoadAssetAtPath<TuningConfigSO>(DATA_ROOT + "/Tuning.asset");

            GameObject canvasGo = new GameObject("MinigameCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 95;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            MinigameRhythmView view = canvasGo.AddComponent<MinigameRhythmView>();
            SetField(view, "_tuning", tuning);

            // S-031 ⑧: 패널을 폰 열림 위치에 정합 — "폰 화면 안에서 진행"으로 읽히게 (sort 95 = 폰 위).
            // 폰 프레임 실아트가 있으면 화면 개구 영역에 정확히 맞춘다.
            bool hasFrame = LoadUISprite("ui_phone_frame") != null;
            GameObject panel = CreateImage(canvasGo.transform, "Panel", CYAN).gameObject;
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.anchoredPosition = hasFrame ? new Vector2(-98f, 0f) : new Vector2(-28f, 24f); // S-117: 새 프레임 개구 정합 (폰 하강 -146)
            panelRect.sizeDelta = hasFrame ? new Vector2(298f, 532f) : new Vector2(430f, 610f);
            SetField(view, "_panel", panel);

            Image inner = CreateImage(panel.transform, "Inner", NAVY);
            RectTransform innerRect = inner.rectTransform;
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(3f, 3f);
            innerRect.offsetMax = new Vector2(-3f, -3f);

            TMP_Text title = CreateText(inner.transform, "Title", "진상 전화!", font,
                34f, AMBER, TextAlignmentOptions.Top);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -20f);
            titleRect.sizeDelta = new Vector2(0f, 46f);
            SetField(view, "_titleLabel", title);

            TMP_Text seq = CreateText(inner.transform, "Sequence", string.Empty, font,
                64f, Color.white, TextAlignmentOptions.Center);
            RectTransform seqRect = seq.rectTransform;
            seqRect.anchorMin = Vector2.zero;
            seqRect.anchorMax = Vector2.one;
            seqRect.offsetMin = new Vector2(20f, 16f);
            seqRect.offsetMax = new Vector2(-20f, -70f);
            SetField(view, "_sequenceLabel", seq);

            panel.SetActive(false);
        }

        // 스마트폰 "배송상차" (S-011) — Tab으로 좌하단 슬라이드. 대화(90)보다 아래, HUD보다 위.
        private static void BuildPhoneCanvas()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);

            GameObject canvasGo = new GameObject("PhoneCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 85;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>(); // 없으면 폰 버튼 클릭이 전부 무시된다 (실사고 2026-07-22)

            PhoneView view = canvasGo.AddComponent<PhoneView>();
            SetField(view, "_font", font);
            SetField(view, "_tuning", AssetDatabase.LoadAssetAtPath<TuningConfigSO>(DATA_ROOT + "/Tuning.asset"));
            SetField(view, "_gameState", AssetDatabase.LoadAssetAtPath<GameStateSO>(DATA_ROOT + "/GameState.asset"));
            SetField(view, "_furnitureCatalog", GetOrCreateFurnitureCatalog()); // S-019 ④
            SetField(view, "_npcCatalog", GetOrCreateNpcCatalog());            // S-079 ④ — 소셜앱
            SetField(view, "_angryMalsoon", AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Art/Characters/Textures/angry_malsoon.png"));

            // 폰 본체 — 우하단 앵커(사람 요청 S-011 후속).
            // 실아트(ui_phone_frame — S-117 크림+네이비 폰, 387×715 캔버스·개구 x56~323·y105~583 실측)
            // 있으면 프레임 사용, 없으면 시안 테두리 폴백 (스왑 계약). 화면(navy)이 흰 스크린 영역을 덮는다.
            Sprite frameArt = LoadUISprite("ui_phone_frame");
            GameObject panel = CreateImage(canvasGo.transform, "Panel", frameArt != null ? Color.white : CYAN).gameObject;
            if (frameArt != null) panel.GetComponent<Image>().sprite = frameArt;
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.sizeDelta = frameArt != null ? new Vector2(430f, 795f) : new Vector2(430f, 610f); // 아트 비율 0.541
            panelRect.anchoredPosition = new Vector2(-28f, frameArt != null ? -830f : -640f); // 닫힘 = 화면 밖
            SetField(view, "_panel", panelRect);
            SetField(view, "_hiddenY", frameArt != null ? -830f : -640f);
            // S-050 ①: 열림 = 화면 개구 바닥(패널바닥+146px)이 뷰포트 바닥에 딱 — 하단 베젤은 화면 밖.
            SetField(view, "_shownY", frameArt != null ? -146f : 24f);

            Image screen = CreateImage(panel.transform, "Screen", NAVY);
            RectTransform screenRect = screen.rectTransform;
            screenRect.anchorMin = Vector2.zero;
            screenRect.anchorMax = Vector2.one;
            // 아트 화면 개구 실측값 (387×715 캔버스 → 430×795 환산: 좌 56 · 우 63 · 상 105 · 하 131px).
            screenRect.offsetMin = frameArt != null ? new Vector2(62f, 146f) : new Vector2(4f, 4f);
            screenRect.offsetMax = frameArt != null ? new Vector2(-70f, -117f) : new Vector2(-4f, -4f);
            screen.raycastTarget = true; // 폰 위 클릭이 월드 스캔으로 새지 않게
            // 화면 내부 위젯은 PhoneView v2가 런타임 생성 (S-019 ⑥ — 홈+앱 6종).

            EditorUtility.SetDirty(view);
        }

        // 가구 카탈로그 4종 (S-019 ④ — 그레이박스 색박스, 실모델은 prefab 스왑 계약).
        // S-079 ④ — NPC 프로필 카탈로그 (Data/Npcs/*.asset — 멱등 생성, 초상은 실아트 소켓).
        private static NpcSO[] GetOrCreateNpcCatalog()
        {
            (string id, string npcName, string intro, Color color)[] npcs =
            {
                ("boss", "사장님", "물류캠프의 왕고참. 스캔 안 한 짐은 안 실어준다.", new Color(0.55f, 0.42f, 0.30f)),
                ("granny", "할머니", "길 건너까지 짐을 옮겨 달라는 단골 심부름 의뢰인.", new Color(0.62f, 0.50f, 0.60f)),
                ("parkmalsoon", "박말순", "전화 너머의 진상 고객. 목소리가 크다.", new Color(0.70f, 0.35f, 0.35f)),
                ("walker_a", "회색 코트 아저씨", "빌라촌을 산책하는 조용한 이웃.", new Color(0.45f, 0.52f, 0.62f)),
                ("walker_b", "장바구니 아주머니", "먹자골목 단골. 오늘 저녁 메뉴 고민 중.", new Color(0.60f, 0.48f, 0.40f)),
                ("walker_c", "초록 점퍼 청년", "동네 러닝 크루라고 주장한다.", new Color(0.50f, 0.58f, 0.45f)),
                ("camp_walker_a", "새벽 출근러", "캠프 앞을 지나 첫차를 타러 간다.", new Color(0.45f, 0.52f, 0.62f)),
                ("camp_walker_b", "야간 산책러", "잠이 안 와서 걷는 중이라고 한다.", new Color(0.60f, 0.48f, 0.40f)),
            };
            string folder = DATA_ROOT + "/Npcs";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(DATA_ROOT, "Npcs");
            var result = new NpcSO[npcs.Length];
            for (int i = 0; i < npcs.Length; i++)
            {
                string path = folder + "/npc_" + npcs[i].id + ".asset";
                NpcSO npc = AssetDatabase.LoadAssetAtPath<NpcSO>(path);
                if (npc == null)
                {
                    npc = ScriptableObject.CreateInstance<NpcSO>();
                    npc.npcId = npcs[i].id;
                    npc.displayName = npcs[i].npcName;
                    npc.intro = npcs[i].intro;
                    npc.placeholderColor = npcs[i].color;
                    AssetDatabase.CreateAsset(npc, path);
                }
                result[i] = npc;
            }
            return result;
        }

        private static FurnitureSO[] GetOrCreateFurnitureCatalog()
        {
            // 앞 4종 = 구매 그리드 노출분. fur_bed(S-031 ③)는 시드 전용 — 목록·배치 조회에만 잡힌다.
            // S-114 — 앞 4종 = 구매 그리드 노출: 카탈로그 실물 가구(couch·desk·chair·clock — Prefabs/Auto
            // 동명 프리팹 자동 연결)로 교체. 반려 재반입 대기분(fur_*)은 뒤(시드·조회 전용) 유지.
            (string id, string label, int price, Vector3 size, Color color, bool wall)[] items =
            {
                ("couch", "소파", 6500, new Vector3(1.8f, 0.85f, 0.8f), new Color(0.55f, 0.42f, 0.35f), false),
                ("desk", "책상", 5500, new Vector3(1.2f, 0.75f, 0.6f), new Color(0.5f, 0.38f, 0.28f), false),
                ("chair", "의자", 3000, new Vector3(0.5f, 0.9f, 0.5f), new Color(0.4f, 0.32f, 0.26f), false),
                ("teddy_bear", "곰인형", 1800, new Vector3(0.4f, 0.45f, 0.35f), new Color(0.75f, 0.6f, 0.45f), false),
                ("clock", "시계", 2500, new Vector3(0.35f, 0.35f, 0.1f), new Color(0.85f, 0.8f, 0.7f), true),
                ("fur_plant", "화분", 2000, new Vector3(0.4f, 0.7f, 0.4f), new Color(0.35f, 0.75f, 0.4f), false),
                ("fur_lamp", "스탠드", 3500, new Vector3(0.35f, 1.4f, 0.35f), new Color(1f, 0.85f, 0.55f), false),
                ("fur_rug", "러그", 4000, new Vector3(2.0f, 0.05f, 1.4f), new Color(0.7f, 0.35f, 0.35f), false),
                ("fur_tv", "TV", 8000, new Vector3(1.6f, 1.0f, 0.25f), new Color(0.15f, 0.15f, 0.2f), true),
                ("fur_bed", "침대", 15000, new Vector3(2.2f, 0.5f, 1.4f), new Color(0.30f, 0.42f, 0.55f), false),
            };

            string folder = DATA_ROOT + "/Furniture";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(DATA_ROOT, "Furniture");

            var catalog = new FurnitureSO[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                string path = folder + "/" + items[i].id + ".asset";
                FurnitureSO so = AssetDatabase.LoadAssetAtPath<FurnitureSO>(path);
                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<FurnitureSO>();
                    AssetDatabase.CreateAsset(so, path);
                }
                // 필드는 매 조립마다 표와 동기화 (멱등 — wallMountable 같은 신설 필드 소급 주입).
                so.furnitureId = items[i].id;
                so.displayName = items[i].label;
                so.price = items[i].price;
                so.size = items[i].size;
                so.color = items[i].color;
                so.wallMountable = items[i].wall;
                // S-109 — 실아트 스왑 계약: Art/Props/<id>.fbx → 팩토리 프리팹이 있으면 배선 (없으면 색박스 폴백).
                so.prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Auto/" + items[i].id + ".prefab");
                // S-201 — 모델 회전 보정. 의자 원본이 누운 채로 나와 배치하면 뒤집혀 있었다(남규님 지적).
                // 표에 새 열을 붙이지 않고 여기서 예외만 잡는다 — 지금 보정이 필요한 모델이 하나뿐이라
                // 열을 늘리면 나머지 9줄이 전부 0으로 채워진 잡음이 된다. 늘어나면 그때 표로 옮긴다.
                so.prefabRotation = items[i].id == "chair" ? new Vector3(90f, 0f, 0f) : Vector3.zero;
                EditorUtility.SetDirty(so);
                catalog[i] = so;
            }
            AssetDatabase.SaveAssets();
            return catalog;
        }

        /// <summary>
        /// UI 실아트 로더 (S-025 스왑 계약) — `Assets/Art/UI/<bomId>.png`가 있으면 스프라이트로,
        /// 없으면 null(호출부가 코드 폴백). 텍스처가 Sprite 타입이 아니면 임포터를 교정한다.
        /// </summary>
        private static Sprite LoadSpriteSubAsset(string path)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                    return sprite;
            }

            return null;
        }

        internal static Sprite LoadUISprite(string bomId)
        {
            string path = "Assets/Art/UI/" + bomId + ".png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null) return null;

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single; // Multiple+슬라이스 0 = 서브에셋 없음 (실사고)
                importer.SaveAndReimport();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return sprite;
        }

        // ── 블립 합성 (없을 때만 — 진짜 SFX 스왑 계약) ───────
        // 사각파 ~1000Hz · 0.045s · 즉시 어택 · 짧은 페이드아웃 · 44.1kHz 16bit mono WAV.
        private static AudioClip EnsureBlipClip()
        {
            if (!System.IO.File.Exists(BLIP_PATH))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(BLIP_PATH));
                System.IO.File.WriteAllBytes(BLIP_PATH, SynthBlipWav());
                AssetDatabase.ImportAsset(BLIP_PATH, ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("[CoreSceneBuilder] 블립 WAV 생성 — " + BLIP_PATH);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(BLIP_PATH);
        }

        private static byte[] SynthBlipWav()
        {
            const int sampleRate = 44100;
            const float durationSec = 0.045f;
            const float freq = 1000f;
            const float amp = 0.45f;
            int sampleCount = Mathf.RoundToInt(sampleRate * durationSec);
            int fadeStart = Mathf.RoundToInt(sampleCount * 0.6f); // 뒤 40% 페이드아웃

            short[] samples = new short[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float phase = (i * freq / sampleRate) % 1f;
                float square = phase < 0.5f ? 1f : -1f; // 즉시 어택 (엔벨로프 없음)
                float env = i < fadeStart ? 1f : 1f - (float)(i - fadeStart) / (sampleCount - fadeStart);
                samples[i] = (short)(square * env * amp * short.MaxValue);
            }

            int dataBytes = sampleCount * 2;
            using (var ms = new System.IO.MemoryStream())
            using (var w = new System.IO.BinaryWriter(ms))
            {
                w.Write(new char[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + dataBytes);
                w.Write(new char[] { 'W', 'A', 'V', 'E' });
                w.Write(new char[] { 'f', 'm', 't', ' ' });
                w.Write(16);                 // Subchunk1Size
                w.Write((short)1);           // PCM
                w.Write((short)1);           // mono
                w.Write(sampleRate);
                w.Write(sampleRate * 2);     // ByteRate
                w.Write((short)2);           // BlockAlign
                w.Write((short)16);          // BitsPerSample
                w.Write(new char[] { 'd', 'a', 't', 'a' });
                w.Write(dataBytes);
                foreach (short s in samples) w.Write(s);
                w.Flush();
                return ms.ToArray();
            }
        }

        // ── 테스트 시나리오 (없을 때만) ──────────────────────
        private static void EnsureTestScenario()
        {
            if (AssetDatabase.LoadAssetAtPath<DialogueScenarioSO>(PARK_SCENARIO_PATH) != null) return;

            System.IO.Directory.CreateDirectory(DIALOGUE_DATA_ROOT);
            DialogueScenarioSO so = ScriptableObject.CreateInstance<DialogueScenarioSO>();
            so.lines = new[]
            {
                new DialogueScenarioSO.Line { speaker = "박말순", text = "어이~ 총각!! 내 김치냉장고 어디 갔어?!" },
                new DialogueScenarioSO.Line { speaker = "박말순", text = "행복빌라 301호! 10시까지 안 오면 알지?!" },
                new DialogueScenarioSO.Line { speaker = "주인공", text = "(…오늘도 시작이다.)" },
            };
            AssetDatabase.CreateAsset(so, PARK_SCENARIO_PATH);
            AssetDatabase.SaveAssets();
            Debug.Log("[CoreSceneBuilder] 테스트 시나리오 생성 — " + PARK_SCENARIO_PATH);
        }

        private static void BuildEventSystem()
        {
            // Input System 프로젝트 — StandaloneInputModule은 무동작이므로 InputSystemUIInputModule.
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        // ── 콘텐츠 씬 4종 ────────────────────────────────────

        public static void CreateContentScenes()
        {
            foreach (string name in ContentSceneNames)
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject label = new GameObject("SceneLabel_" + name);
                label.transform.position = Vector3.zero;

                // 카메라는 콘텐츠 씬 소유(기존 구조 — Core는 카메라를 갖지 않는다).
                // AudioListener는 붙이지 않는다 — Core가 소유하므로 여기 두면 2개가 된다.
                GameObject cameraGo = new GameObject("Main Camera");
                cameraGo.tag = "MainCamera";
                cameraGo.AddComponent<Camera>();

                EditorSceneManager.SaveScene(scene, SCENES_ROOT + "/" + name + ".unity");
            }
            Debug.Log("[CoreSceneBuilder] 콘텐츠 씬 4종 생성 — Home·Camp·Travel·District (각 카메라 포함).");
        }

        // ── 빌드 세팅 등록 ───────────────────────────────────

        public static void RegisterBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[BuildOrder.Length];
            for (int i = 0; i < BuildOrder.Length; i++)
                scenes[i] = new EditorBuildSettingsScene(BuildOrder[i], true);

            EditorBuildSettings.scenes = scenes;
            Debug.Log("[CoreSceneBuilder] 빌드 세팅 " + BuildOrder.Length + "씬 등록 완료.");
        }

        // ── 헬퍼 ─────────────────────────────────────────────

        private static void AddBasicChipBackground(Transform parent, Texture2D texture)
        {
            if (texture == null) return;
            RawImage background = new GameObject("BasicBackground", typeof(RectTransform)).AddComponent<RawImage>();
            background.transform.SetParent(parent, false);
            background.transform.SetAsFirstSibling();
            background.texture = texture;
            background.uvRect = new Rect(0.17f, 0.24f, 0.66f, 0.48f);
            background.raycastTarget = false;
            StretchFull(background.rectTransform);
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text,
            TMP_FontAsset font, float fontSize, Color color, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            return t;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        // S-070 ② — 게이지 fill 규격: 순백 직사각 스프라이트 + Filled·Horizontal·왼쪽 기점.
        // (S-068의 UISprite는 라운드 나인슬라이스라 fill이 알약형·중앙정렬처럼 보였다 — R20 지적.)
        private const string GAUGE_SPRITE_PATH = "Assets/Art/UI/ui_gauge_fill.png";

        private static void ConfigureGaugeFill(Image img, float initialFill)
        {
            img.sprite = GetOrCreateGaugeSprite(); // sprite 없으면 fillAmount 무시 (S-068 ⑥)
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = initialFill;
        }

        private static Sprite GetOrCreateGaugeSprite()
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(GAUGE_SPRITE_PATH);
            if (existing != null) return existing;

            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            System.IO.File.WriteAllBytes(GAUGE_SPRITE_PATH, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(GAUGE_SPRITE_PATH);

            var importer = (TextureImporter)AssetImporter.GetAtPath(GAUGE_SPRITE_PATH);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single; // 미지정 시 Sprite 서브에셋이 안 생겨 로드 실패 (S-070 실측)
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(GAUGE_SPRITE_PATH);
        }

        // 코너 앵커 배치: pivot=anchor로 두고 anchoredPos·size 지정.
        private static void AnchorCorner(RectTransform rect, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
        }

        private static void AnchorMiddleBottom(RectTransform rect, Vector2 anchoredPos, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
        }

        // [SerializeField] private 필드에 직접 값을 꽂는다.
        // SerializedObject.objectReferenceValue는 새로 AddComponent한 컴포넌트에서
        // SaveScene 시 에셋 참조가 {fileID:0}으로 유실되는 사례가 있어 리플렉션으로 확정한다.
        private static void SetField(Object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                Debug.LogError("[CoreSceneBuilder] 필드 없음: " + target.GetType().Name + "." + fieldName);
                return;
            }
            field.SetValue(target, value);
        }

        /// <summary>
        /// S-228 — 엔딩 대열 명부. **모델이 있는 인물만** 올린다("1마리씩 다 나오게" — 남규님).
        /// 걷기 클립이 없는 인물(오지혜)은 서 있는 클립을 준다 — 없는 것보다 낫다(정지 자세로
        /// 미끄러지는 것이 종전 증상이었다). 경로가 하나라도 비면 그 인물만 조용히 빠지고
        /// 엔딩 자체는 돈다.
        ///
        /// S-237 — `clean` 열이 채워진 인물은 **루트 이동 커브를 걷어낸 정리본**을 쓴다.
        /// 대열의 위치는 `WalkTo`가 소유하는데 클립에 루트 위치 커브가 살아 있으면 그것이
        /// 매 프레임 위치를 덮어써 땅에 박힌다. 실측상 원본에 루트 위치 커브가 있는 인물은
        /// **김사장 하나뿐**이다(나머지 넷은 0개) — 그래서 김사장만 채운다.
        /// 캠프가 이미 같은 이름의 정리본을 쓰므로 새로 만들지 않고 그대로 공유한다.
        /// </summary>
        private static DontLate.WorldEndingManager.EndingCastEntry[] BuildEndingCast()
        {
            // S-244 — 박말순·오지혜는 종전에 `malsoon_Angry`·`jihye_Idle`을 들고 있었다. 둘 다
            // **걷는 동작이 아니라서**(실측 보속 0.00) 이동하는 내내 미끄러졌다. 전용 걷기 클립이
            // 없지만 두 모델 모두 Humanoid + 유효 Avatar라 **공용 걷기 클립이 리타깃된다** —
            // 그게 Humanoid 리그를 쓰는 이유다.
            // S-250 — `yaw`는 대열에 설 때의 Y 회전이다. 기본 −90°(왼쪽의 플레이어를 보고 걸어온다)인데
            // **김사장 모델만 기본 정면이 90° 틀어져 있어** 그 값을 그대로 주면 혼자 옆을 본다
            // (캠프도 같은 이유로 시각물에 +90°를 따로 건다 — `CampStageBuilder.BOSS_VISUAL_YAW`).
            const string SHARED_WALK = "Assets/Art/Characters/dummy/dummy_npc_walking.fbx";
            (string id, string name, string model, string clip, string clean, string skin, float yaw)[] table =
            {
                ("parkmalsoon", "박말순", "Assets/Art/Characters/malsoon/malsoon.fbx",
                    SHARED_WALK, null,
                    "Assets/Art/Characters/Materials/malsoon.fbm.mat", -90f),
                ("boss", "김사장", "Assets/Art/Characters/Kimboss/kim_boss.fbx",
                    "Assets/Art/Characters/Kimboss/kimboss_Walking (2).fbx", "kim_boss_walk_clean.anim",
                    "Assets/Art/Characters/kimsajng.mat", 0f),
                ("yoo_jihye", "오지혜", "Assets/Art/Characters/jihye/jihye.fbx",
                    SHARED_WALK, null, null, -90f),
                ("na_ara", "나아라", "Assets/Art/Characters/naara/gs_girl_mixamo_rig_final.fbx",
                    "Assets/Art/Characters/naara/gs_girl_walking.fbx", null,
                    "Assets/Art/Characters/Materials/gs_girl.mat", -90f),
                ("walker_a", "이웃 주민", "Assets/Art/Characters/dummy/dummy_npc_walking.fbx",
                    "Assets/Art/Characters/dummy/dummy_npc_walking.fbx", null,
                    "Assets/Art/Characters/Materials/dummynpc.fbm.mat", -90f),
            };

            var cast = new System.Collections.Generic.List<DontLate.WorldEndingManager.EndingCastEntry>();
            foreach (var row in table)
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(row.model);
                if (model == null) { Debug.LogWarning("[엔딩대열] 모델 없음 — " + row.model); continue; }
                // S-250 — 클립을 집기 **전에** 루프를 보장한다. 걷기 클립인데 Loop Time이 꺼져 있으면
                // 1초쯤 걷다가 마지막 프레임에서 굳는다(남규님 관찰: "처음에 잠깐 걷는 애니메이션 나왔음").
                // 실측: 나아라 클립만 loop=True였고 — 그래서 나아라만 걷는 것처럼 보였다.
                EnsureLoopingImport(row.clip);
                AnimationClip clip = string.IsNullOrEmpty(row.clean)
                    ? LoadFirstClip(row.clip)
                    : CampStageBuilder.GetOrCreateCleanAnimationClip(row.clip, row.clean);
                EnsureLoopingClip(clip); // 정리본(.anim)은 임포터가 없다 — 클립 설정을 직접 켠다
                cast.Add(new DontLate.WorldEndingManager.EndingCastEntry
                {
                    npcId = row.id,
                    displayName = row.name,
                    model = model,
                    walkClip = clip,
                    avatar = LoadAvatar(row.model),
                    skin = string.IsNullOrEmpty(row.skin) ? null : AssetDatabase.LoadAssetAtPath<Material>(row.skin),
                    // S-244 — 클립이 1배속으로 나아가는 초당 거리. 런타임이 이걸로 걸음 주기를
                    // 이동 속도에 맞춘다. Generic 클립은 0이 나온다(휴머노이드 전용 값) —
                    // 김사장이 그 경우이고, 원본조차 루트 이동이 0인 **제자리 걷기**라 보정 대상이 아니다.
                    clipGroundSpeed = clip != null ? clip.apparentSpeed : 0f,
                    facingYaw = row.yaw, // S-250
                });
            }
            Debug.Log("[엔딩대열] " + cast.Count + "인 배선 (S-228 — 모델 보유 전원).");
            return cast.ToArray();
        }

        /// <summary>
        /// S-250 — FBX 서브에셋 클립의 Loop Time을 켠다. 클립 객체에 직접 쓰면 재임포트 때 사라지므로
        /// **임포터 설정**을 고쳐야 영구히 남는다. 이미 켜져 있으면 재임포트하지 않는다(멱등·비용).
        /// </summary>
        private static void EnsureLoopingImport(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer) return;

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0) return;

            bool changed = false;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                if (clip.loopTime) continue;
                clip.loopTime = true;
                changed = true;
            }
            if (!changed) return;

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            Debug.Log("[엔딩대열] Loop Time 켬 — " + path);
        }

        /// <summary>`.anim` 에셋(임포터가 없는 생성물)의 Loop Time을 켠다 (S-250).</summary>
        private static void EnsureLoopingClip(AnimationClip clip)
        {
            if (clip == null || clip.isLooping) return;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (settings.loopTime) return;
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            Debug.Log("[엔딩대열] Loop Time 켬 — " + clip.name);
        }

        /// <summary>FBX 안의 애니메이션 클립(서브에셋). `__preview__`는 거른다.</summary>
        private static AnimationClip LoadFirstClip(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) return clip;
            return null;
        }

        /// <summary>모델 FBX가 만든 Avatar. 리그가 Generic이면 없다 — 그러면 클립도 안 돈다.</summary>
        private static Avatar LoadAvatar(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Avatar avatar) return avatar;
            return null;
        }
    }
}
