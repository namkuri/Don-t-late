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
        private const string FONT_PATH = "Assets/Art/UI/Fonts/Pretendard-Regular SDF.asset";
        private const string BGM_FOLDER = "Assets/Audio/BGM";
        private const string BGM_LIBRARY_PATH = DATA_ROOT + "/BgmLibrary.asset";
        private static readonly Color AMBER = new Color(1f, 0.624f, 0.271f, 1f); // #ff9f45
        private static readonly Color CYAN = new Color(0.208f, 0.878f, 0.784f, 1f); // #35e0c8
        private static readonly Color NAVY = new Color(0.039f, 0.051f, 0.086f, 0.9f); // #0a0d16 반투명

        private const string BLIP_PATH = "Assets/Audio/SFX/sfx_dialogue_blip.wav";
        private const string DIALOGUE_DATA_ROOT = "Assets/Data/Dialogue";
        private const string PARK_SCENARIO_PATH = DIALOGUE_DATA_ROOT + "/Scenario_ParkMalsoon_Intro.asset";

        private static readonly string[] ContentSceneNames = { "Home", "Camp", "Travel", "District", "Apartment", "Hillside" };

        // 빌드 세팅 등록 순서 — Core(0) → Main → 콘텐츠 5종. SampleScene·Greybox 제외.
        private static readonly string[] BuildOrder =
        {
            SCENES_ROOT + "/Core.unity",
            SCENES_ROOT + "/Main.unity",
            SCENES_ROOT + "/Home.unity",
            SCENES_ROOT + "/Camp.unity",
            SCENES_ROOT + "/Travel.unity",
            SCENES_ROOT + "/District.unity",
            SCENES_ROOT + "/Apartment.unity", // S-038
            SCENES_ROOT + "/Hillside.unity",  // S-049
        };

        // ── 메뉴 ─────────────────────────────────────────────

        [MenuItem("DontLate/Build/Core Scene", priority = 10)]
        public static void BuildCoreScene()
        {
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
            BuildHUDCanvas(gameState);
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
            CreateContentScenes();          // 씬 파일이 없으면 빈 씬부터 생성 (멱등)
            BuildCoreScene();               // 매니저·HUD·대화·미니게임·폰 캔버스
            CampStageBuilder.BuildCampStage();
            HomeStageBuilder.BuildHomeStage();
            DistrictSceneBuilder.BuildDistrictStage();
            ApartmentStageBuilder.BuildApartmentStage(); // S-038
            HillsideStageBuilder.BuildHillsideStage();   // S-049
            SceneFlowUIBuilder.BuildSceneFlowUI();  // 씬별 전환 UI + 정산 패널 (무대 뒤에 얹는다)
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

            WorldDialogueManager dialogue = managers.AddComponent<WorldDialogueManager>();
            EnsureTestScenario(); // 박말순 인트로 SO 확보(멱등)
            SetField(dialogue, "_homeIntroScenario",
                AssetDatabase.LoadAssetAtPath<DialogueScenarioSO>(PARK_SCENARIO_PATH)); // S-009 Home 인트로 전화

            WorldDebtManager debt = managers.AddComponent<WorldDebtManager>(); // S-005
            SetField(debt, "_gameState", gameState);
            SetField(debt, "_tuning", tuning);

            WorldMinigameManager minigame = managers.AddComponent<WorldMinigameManager>(); // S-007
            SetField(minigame, "_tuning", tuning);

            WorldWeatherManager weather = managers.AddComponent<WorldWeatherManager>(); // S-042
            SetField(weather, "_gameState", gameState);
            // S-047: 구름 실아트 소켓 — Art/Backgrounds/fx_cloud_*.png 있으면 배선 (없으면 코드 블롭 폴백).
            var cloudSprites = new System.Collections.Generic.List<Sprite>();
            foreach (string suffix in new[] { "a", "b", "c" })
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/fx_cloud_" + suffix + ".png");
                if (sprite != null) cloudSprites.Add(sprite);
            }
            if (cloudSprites.Count > 0)
            {
                SerializedObject weatherSerialized = new SerializedObject(weather);
                SerializedProperty cloudsProp = weatherSerialized.FindProperty("_cloudSprites");
                cloudsProp.arraySize = cloudSprites.Count;
                for (int i = 0; i < cloudSprites.Count; i++)
                    cloudsProp.GetArrayElementAtIndex(i).objectReferenceValue = cloudSprites[i];
                weatherSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

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
            SetField(audio, "_sfxPenalty", LoadSfx("sfx_penalty"));
            SetField(audio, "_sfxVending", LoadSfx("sfx_vending"));
            SetField(audio, "_sfxThrow", LoadSfx("sfx_throw"));
            SetField(audio, "_sfxCoin", LoadSfx("sfx_coin"));
            SetField(audio, "_sfxPhone", LoadSfx("sfx_phone"));
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

        /// <summary>
        /// BGM 목록 SO를 확보한다(없으면 계약 폴더의 클립으로 생성). 슬롯 분류는 사람 청취로 확정하므로
        /// 자동 생성분은 전부 Unsorted — 제목으로 낮/밤을 추정하지 않는다(D-039 실수→규칙).
        /// </summary>
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
            cutIn.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            StretchFull(cutIn.rectTransform);
            cutInGo.SetActive(false);

            FadeScreen fade = canvasGo.AddComponent<FadeScreen>();
            SetField(fade, "_group", group);
            SetField(fade, "_lateCutIn", cutInGo);
        }

        // ── HUD 캔버스 (Core 상주) ───────────────────────────

        private static void BuildHUDCanvas(GameStateSO gameState)
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

            // 시계 (우상).
            TMP_Text clock = CreateText(content.transform, "Clock", "Day 1 · 08:00", font,
                40f, Color.white, TextAlignmentOptions.TopRight);
            AnchorCorner(clock.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(460f, 60f));
            SetField(hud, "_clockLabel", clock);

            // 돈·빚 (우상, 시계 아래).
            TMP_Text money = CreateText(content.transform, "Money", "₩0", font,
                34f, Color.white, TextAlignmentOptions.TopRight);
            AnchorCorner(money.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -104f), new Vector2(460f, 48f));
            SetField(hud, "_moneyLabel", money);

            TMP_Text debt = CreateText(content.transform, "Debt", "빚 ₩10,000", font,
                30f, new Color(0.95f, 0.55f, 0.55f, 1f), TextAlignmentOptions.TopRight);
            AnchorCorner(debt.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -156f), new Vector2(460f, 44f));
            SetField(hud, "_debtLabel", debt);

            // 배송 카드 (좌상) — 배경 + 주소 + 남은시간.
            GameObject card = CreateImage(content.transform, "DeliveryCard",
                new Color(0.10f, 0.12f, 0.16f, 0.85f)).gameObject;
            Image cardBg = card.GetComponent<Image>();
            AnchorCorner(cardBg.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(560f, 150f));
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

            // 스태미나 바 (좌하) — 배경 + 채움.
            Image staBg = CreateImage(content.transform, "StaminaBg", new Color(0.10f, 0.12f, 0.16f, 0.85f));
            AnchorCorner(staBg.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(380f, 34f));

            Image staFill = CreateImage(staBg.transform, "StaminaFill", new Color(0.45f, 0.85f, 0.55f, 1f));
            StretchFull(staFill.rectTransform);
            staFill.type = Image.Type.Filled;
            staFill.fillMethod = Image.FillMethod.Horizontal;
            staFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            staFill.fillAmount = 1f;
            SetField(hud, "_staminaFill", staFill);

            // "E" 상호작용 안내 (하단 중앙) — 기본 숨김.
            TMP_Text ePrompt = CreateText(content.transform, "EPrompt", "[E] 상호작용", font,
                38f, CYAN, TextAlignmentOptions.Center);
            AnchorMiddleBottom(ePrompt.rectTransform, new Vector2(0f, 120f), new Vector2(640f, 60f));
            SetField(hud, "_ePrompt", ePrompt.gameObject);
        }

        // ── 대화 캔버스 (Core 상주) ──────────────────────────

        private static void BuildDialogueCanvas()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
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
            // S-027 ①: 실아트 원본 비율(크롭 후 1612×477 ≈ 3.38:1) 그대로 — 찌그러짐 금지.
            AnchorMiddleBottom(borderRect, new Vector2(0f, 50f),
                boxArt != null ? new Vector2(1350f, 400f) : new Vector2(1720f, 260f));
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
            TMP_Text nameLabel = CreateText(inner.transform, "Name", "박말순", font,
                34f, boxArt != null ? new Color(0.10f, 0.30f, 0.22f) : AMBER,
                boxArt != null ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft);
            nameLabel.fontStyle = FontStyles.Bold; // S-027 ② (민지: 이름·내용 볼드)
            AnchorCorner(nameLabel.rectTransform, new Vector2(0f, 1f),
                boxArt != null ? new Vector2(60f, -8f) : new Vector2(44f, -18f),
                boxArt != null ? new Vector2(450f, 115f) : new Vector2(600f, 46f));
            SetField(view, "_nameLabel", nameLabel);

            // 본문 — 실아트 내부가 밝아서 어두운 글자 (흰 글자는 소실). 흰 영역은 명찰 탭 아래부터.
            TMP_Text body = CreateText(inner.transform, "Body", string.Empty, font,
                40f, boxArt != null ? new Color(0.12f, 0.14f, 0.18f) : Color.white, TextAlignmentOptions.TopLeft);
            body.fontStyle = FontStyles.Bold; // S-027 ②
            body.textWrappingMode = TextWrappingModes.Normal;
            RectTransform bodyRect = body.rectTransform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = boxArt != null ? new Vector2(80f, 55f) : new Vector2(44f, 24f);
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
            panelRect.anchoredPosition = hasFrame ? new Vector2(-67f, 0f) : new Vector2(-28f, 24f); // S-050 ①: 폰 하강(-106)에 맞춰 개구 정합 130→0
            panelRect.sizeDelta = hasFrame ? new Vector2(354f, 622f) : new Vector2(430f, 610f);
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

            // 폰 본체 — 우하단 앵커(사람 요청 S-011 후속).
            // 실아트(ui_phone_frame — 민지 민트 폰, 723×1353 크롭·화면 개구 실측) 있으면 프레임 사용,
            // 없으면 시안 테두리 폴백 (스왑 계약). 화면(navy)이 아트의 흰 스크린 영역을 정확히 덮는다.
            Sprite frameArt = LoadUISprite("ui_phone_frame");
            GameObject panel = CreateImage(canvasGo.transform, "Panel", frameArt != null ? Color.white : CYAN).gameObject;
            if (frameArt != null) panel.GetComponent<Image>().sprite = frameArt;
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.sizeDelta = frameArt != null ? new Vector2(430f, 805f) : new Vector2(430f, 610f); // 아트 비율 0.534
            panelRect.anchoredPosition = new Vector2(-28f, frameArt != null ? -840f : -640f); // 닫힘 = 화면 밖
            SetField(view, "_panel", panelRect);
            SetField(view, "_hiddenY", frameArt != null ? -840f : -640f);
            // S-050 ①: 열림 = 화면 개구 바닥(패널바닥+106px)이 뷰포트 바닥에 딱 — 하단 베젤은 화면 밖.
            SetField(view, "_shownY", frameArt != null ? -106f : 24f);

            Image screen = CreateImage(panel.transform, "Screen", NAVY);
            RectTransform screenRect = screen.rectTransform;
            screenRect.anchorMin = Vector2.zero;
            screenRect.anchorMax = Vector2.one;
            // 아트 화면 개구 실측값 (정규화 좌 0.086 · 우 0.910 · 상 0.095 · 하 0.868 → 430×805 환산).
            screenRect.offsetMin = frameArt != null ? new Vector2(37f, 106f) : new Vector2(4f, 4f);
            screenRect.offsetMax = frameArt != null ? new Vector2(-39f, -77f) : new Vector2(-4f, -4f);
            screen.raycastTarget = true; // 폰 위 클릭이 월드 스캔으로 새지 않게
            // 화면 내부 위젯은 PhoneView v2가 런타임 생성 (S-019 ⑥ — 홈+앱 6종).

            EditorUtility.SetDirty(view);
        }

        // 가구 카탈로그 4종 (S-019 ④ — 그레이박스 색박스, 실모델은 prefab 스왑 계약).
        private static FurnitureSO[] GetOrCreateFurnitureCatalog()
        {
            // 앞 4종 = 구매 그리드 노출분. fur_bed(S-031 ③)는 시드 전용 — 목록·배치 조회에만 잡힌다.
            (string id, string label, int price, Vector3 size, Color color, bool wall)[] items =
            {
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
    }
}
