using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DontLate.EditorTools
{
    /// <summary>
    /// 씬 흐름 골격 UI를 각 콘텐츠 씬에 조립하는 개발 도구.
    /// Core에서 Play → Main(타이틀)부터 클릭만으로 하루 사이클을 완주할 수 있게 최소 UI를 깐다.
    /// 생성물은 전부 "__ui_" 접두 루트 캔버스 하나에 담고, 다시 실행하면 지우고 새로 만든다(멱등).
    /// Main.unity는 UI 캔버스 추가만 한다 — 기존 오브젝트는 건드리지 않는다(사람 승인 범위).
    /// </summary>
    public static class SceneFlowUIBuilder
    {
        private const string SCENES_ROOT = "Assets/Scenes";
        private const string FONT_PATH = "Assets/Art/UI/Fonts/DNFBitBitOTF SDF.asset";
        private const string PANEL_ART_ROOT = "Assets/Art/UI/panel/";

        // 타이틀 UI 공통 축소율 — S-139 후속(남규님 씬 실조정 2026-08-04 굽기).
        // 로고·서브로고·시작버튼을 한 값으로 묶어 배경(살아 있는 거리)과의 균형을 한 곳에서 조인다.
        private const float TITLE_UI_SCALE = 0.7f;
        private const string UI_PREFIX = "__ui_";

        private static readonly Color AMBER = new Color(1f, 0.624f, 0.271f, 1f);      // #ff9f45 목표
        private static readonly Color CYAN = new Color(0.208f, 0.878f, 0.784f, 1f);   // #35e0c8 상호작용
        private static readonly Color NAVY = new Color(0.039f, 0.051f, 0.086f, 1f);   // #0a0d16 배경

        [MenuItem("DontLate/Build/Scene Flow UI", priority = 15)]
        public static void BuildSceneFlowUI()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            if (font == null)
            {
                // S-142 — 종전엔 경고만 찍고 그대로 진행했다. 그 조용한 성공이 사고의 근원이다:
                // 폰트가 null이면 TMP가 기본 LiberationSans로 조립되는데 거기엔 **한글 글리프가
                // 없어** 전 UI가 두부(□)로 저장된다. 컴파일도 콘솔 에러도 통과하므로 아무도
                // 모르고, 씬이 저장된 뒤에야 화면에서 발견된다(실제로 6개 씬이 그렇게 저장됨).
                // 조립을 중단한다 — 깨진 씬을 저장하느니 아무것도 안 하는 게 낫다.
                Debug.LogError("[SceneFlowUIBuilder] DNFBitBit 폰트 로드 실패 — 조립 중단. "
                    + $"경로 확인: {FONT_PATH} (한글이 두부로 저장되는 것을 막기 위해 진행하지 않는다)");
                return;
            }

            BuildMain(font);
            BuildHome(font);
            BuildLabeledAction("Camp", "물류캠프 — 패드에서 E로 적재", "짐 다 실었다 — 출발", GameScene.Travel, font);
            BuildTravel(font);
            BuildDistrict(font);
            BuildDeliveryEndUI("Apartment", "아파트단지 — 대차에 싣고 비번·엘베로", font); // S-038
            BuildDeliveryEndUI("Hillside", "언덕주택가 — 오르막 조심, 비 오면 미끄럽다", font); // S-049

            Debug.Log("[SceneFlowUIBuilder] 씬 흐름 UI 조립 완료 — Main·Home·Camp·Travel·District·Apartment 6씬.");
        }

        // ── 씬별 조립 ────────────────────────────────────────

        // Main = 타이틀 화면. 기존 오브젝트 불변, __ui_ 캔버스만 추가.
        private static void BuildMain(TMP_FontAsset font)
        {
            Scene scene = EditorSceneManager.OpenScene(SCENES_ROOT + "/Main.unity", OpenSceneMode.Single);
            Transform root = CreateFlowCanvas().transform;

            // 아트팀 발주 (S-026): "배경이 뭐든 로고보다 명도 50% 낮게" — 반투명 검정 스크림.
            Image bg = CreateImage(root, "Bg", new Color(0f, 0f, 0f, 0.5f));
            StretchFull(bg.rectTransform);
            bg.raycastTarget = true; // 타이틀 배경 — 뒤 씬으로의 클릭 통과 차단

            // 타이틀 로고 — 실아트(ui_title) 있으면 이미지, 없으면 TMP 폴백 (S-025 스왑 계약).
            Sprite logoArt = CoreSceneBuilder.LoadUISprite("ui_title");
            if (logoArt != null)
            {
                Image logo = CreateImage(root, "Title", Color.white);
                logo.sprite = logoArt;
                logo.preserveAspect = true;
                // S-027 ⑥: 민지 목업 점유율 — 로고 폭 ≈ 화면 46% (크롭 아트 1.74:1이라 렉트=실표시).
                // S-139 후속(남규님 씬 실조정 2026-08-04 굽기) — 배경이 정지 이미지에서 살아 있는
                // 거리로 바뀌면서 UI가 화면을 너무 먹었다. 세 요소를 0.7배로 줄이고 로고를 내렸다.
                // 렉트(900×518)는 그대로 두고 스케일만 건드린다 — 아트 비율을 지키기 위해서다.
                AnchorCentered(logo.rectTransform, new Vector2(0f, 156f), new Vector2(900f, 518f));
                logo.rectTransform.localScale = Vector3.one * TITLE_UI_SCALE;
            }
            else
            {
                TMP_Text title = CreateText(root, "Title", "늦지마!!", font, 180f, AMBER,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                AnchorCentered(title.rectTransform, new Vector2(0f, 130f), new Vector2(1500f, 280f));
            }

            // 서브 로고 — ui_title_sub.
            Sprite subArt = CoreSceneBuilder.LoadUISprite("ui_title_sub");
            if (subArt != null)
            {
                Image sub = CreateImage(root, "Subtitle", Color.white);
                sub.sprite = subArt;
                sub.preserveAspect = true;
                // S-027 ⑥⑦: 목업 폭 ≈ 43% + 알파 펄스 폐지 → 사선 광 좌→우 시머 스윕(UIShine).
                AnchorCentered(sub.rectTransform, new Vector2(0f, -80f), new Vector2(830f, 104f));
                sub.rectTransform.localScale = Vector3.one * TITLE_UI_SCALE; // S-139 후속
                sub.gameObject.AddComponent<UIShine>();
            }
            else
            {
                TMP_Text sub = CreateText(root, "Subtitle", "지각 압박 배달 생존기", font, 48f, Color.white,
                    TextAlignmentOptions.Center, FontStyles.Normal);
                AnchorCentered(sub.rectTransform, new Vector2(0f, -30f), new Vector2(1200f, 80f));
            }

            // 늦지마맨 일러스트(ui_title_man) 은퇴 — S-139 후속(남규님 씬 실조정 2026-08-04).
            // 타이틀 배경이 정지 이미지에서 살아 있는 거리로 바뀌면서, 좌하단을 덮던 이 일러스트가
            // 배경(가로등·행인·달리는 배달원)을 가렸다. 아트 자체는 남아 있으므로 되살리려면
            // 이 블록을 복구하면 된다 — 아트 삭제가 아니라 배치 은퇴다.

            // 시작 버튼 — 실아트(ui_start_button — "▶시작" 자체 텍스트 포함) 있으면 이미지 버튼 (S-026).
            Sprite startArt = CoreSceneBuilder.LoadUISprite("ui_start_button");
            if (startArt != null)
            {
                GameObject startGo = new GameObject("StartButton", typeof(RectTransform));
                startGo.transform.SetParent(root, false);
                Image startImage = startGo.AddComponent<Image>();
                startImage.sprite = startArt;
                startImage.preserveAspect = true;
                RectTransform startRect = (RectTransform)startGo.transform;
                startRect.anchorMin = startRect.anchorMax = startRect.pivot = new Vector2(0.5f, 0f);
                startRect.sizeDelta = new Vector2(460f, 222f); // 목업 폭 ≈ 23%, 크롭 아트 2.07:1 (S-027 ⑥)
                startRect.anchoredPosition = new Vector2(0f, 208f); // S-139 후속 — 90→208로 올림
                startRect.localScale = Vector3.one * TITLE_UI_SCALE;
                Button startButton = startGo.AddComponent<Button>();
                startButton.targetGraphic = startImage;
                SceneAdvanceButton advance = startGo.AddComponent<SceneAdvanceButton>();
                SetField(advance, "_target", GameScene.Home);
                EditorUtility.SetDirty(advance);
            }
            else
            {
                CreateButton(root, "StartButton", "시작", GameScene.Home, font, CYAN,
                    new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(440f, 118f), 48f);
            }

            EditorSceneManager.SaveScene(scene, SCENES_ROOT + "/Main.unity");
        }

        // Home/Camp/Travel = 좌상 라벨 + 하단 중앙 진행 버튼.
        private static void BuildLabeledAction(string sceneName, string labelText,
            string buttonText, GameScene target, TMP_FontAsset font)
        {
            Scene scene = EditorSceneManager.OpenScene(SCENES_ROOT + "/" + sceneName + ".unity", OpenSceneMode.Single);
            Transform root = CreateFlowCanvas().transform;

            if (sceneName == "Camp")
                CreateTutorialBanner(root, labelText, font, -94f, "tutorial");
            else
            {
                TMP_Text label = CreateText(root, "Label", labelText, font, 46f, Color.white,
                    TextAlignmentOptions.Top, FontStyles.Normal);
                AnchorCorner(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(1000f, 72f));
            }

            // S-072 ⑥ — 캠프는 출발 버튼 없음: 출발은 엣지 워크(해금 구역 도보)나 트럭 인터랙트 몫.
            if (sceneName != "Camp")
                CreateButton(root, "AdvanceButton", buttonText, target, font, CYAN,
                    new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(600f, 104f), 40f);

            // S-134 ⑥ — '집으로'는 **Home이 아닌 모든 씬**에 둔다(정수님 QA: 캠프에서만 돼서 불편).
            // 정산 UI가 씬마다 있어야 도보 귀가(S-134 ⑤)도 어디서든 같은 마감을 탈 수 있다.
            // 구 규칙(S-062 ⑥ 캠프 전용)은 폐기 — 배송지에서 바로 귀가가 막혀 있던 원인.
            if (sceneName != "Home")
                BuildDeliveryEndCanvas(root, font, navButtons: false);

            EditorSceneManager.SaveScene(scene, SCENES_ROOT + "/" + sceneName + ".unity");
        }

        // Home = 라벨 + 진행 버튼. 버튼은 인트로 전화(대화)가 끝나야 나타난다 (S-009 게이트).
        private static void BuildHome(TMP_FontAsset font)
        {
            Scene scene = EditorSceneManager.OpenScene(SCENES_ROOT + "/Home.unity", OpenSceneMode.Single);
            Transform root = CreateFlowCanvas().transform;

            TMP_Text label = CreateText(root, "Label", "집 — 아침", font, 46f, Color.white,
                TextAlignmentOptions.Top, FontStyles.Normal);
            AnchorCorner(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(1000f, 72f)); // S-030 ②: 상단 중앙 — HUD 카드(좌상)와 중첩 소멸

            CreateHomeAdvanceButton(root, font);

            // 게이트는 상시 활성인 캔버스에 붙인다 — 버튼 자신에 붙이면 꺼질 때 구독이 끊긴다.
            HideDuringDialogue gate = root.gameObject.AddComponent<HideDuringDialogue>();
            SetField(gate, "_target", root.Find("AdvanceButton").gameObject);
            EditorUtility.SetDirty(gate);

            EditorSceneManager.SaveScene(scene, SCENES_ROOT + "/Home.unity");
        }

        // Travel = 폰 지도 앱이 목적지 선택 전담(S-036 — 구 노드 버튼·TravelMapView 은퇴).
        // 씬은 안내 라벨 + 캠프 복귀 버튼만 유지한다.
        private static void BuildTravel(TMP_FontAsset font)
        {
            Scene scene = EditorSceneManager.OpenScene(SCENES_ROOT + "/Travel.unity", OpenSceneMode.Single);

            // UI 전용 씬이라도 카메라는 있어야 한다 — 없으면 게임뷰에 "No camera" 워터마크 (S-009 ④).
            if (Camera.main == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                Camera cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = NAVY;
                // AudioListener는 Core 소유(D-041) — 붙이지 않는다.
            }

            Transform root = CreateFlowCanvas().transform;

            // S-036: 노드 버튼 UI 은퇴 — 목적지 선택은 폰 지도 앱(PhoneView Map)이 전담. 씬엔 안내+복귀만.
            CreateTutorialBanner(root, "이동 — 폰 지도에서 목적지를 골라라", font);

            // S-062 ⑤ — 좌상단 ← 뒤로가기 (직전 씬 복귀 · Backspace/Delete 동일).
            GameObject backGo = new GameObject("BackButton", typeof(RectTransform));
            backGo.transform.SetParent(root, false);
            Image backImg = backGo.AddComponent<Image>();
            backImg.color = new Color(0.20f, 0.24f, 0.34f, 0.95f);
            RectTransform backRect = (RectTransform)backGo.transform;
            backRect.anchorMin = backRect.anchorMax = backRect.pivot = new Vector2(0f, 1f);
            backRect.sizeDelta = new Vector2(120f, 72f);
            backRect.anchoredPosition = new Vector2(28f, -28f);
            Button backButton = backGo.AddComponent<Button>();
            backButton.targetGraphic = backImg;
            backGo.AddComponent<SceneBackButton>();
            TMP_Text backLabel = CreateText(backGo.transform, "Label", "←", font, 44f, Color.white,
                TextAlignmentOptions.Center, FontStyles.Bold);
            StretchFull(backLabel.rectTransform);

            CreateButton(root, "AdvanceButton", "캠프로 돌아간다", GameScene.Camp, font, AMBER,
                new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(420f, 74f), 30f);

            EditorSceneManager.SaveScene(scene, SCENES_ROOT + "/Travel.unity");
        }

        // District = 우상 작은 "하루 끝" 버튼만. 무대는 기존 DistrictSceneBuilder 산출물 유지.
        private static void BuildDistrict(TMP_FontAsset font)
        {
            Scene scene = EditorSceneManager.OpenScene(SCENES_ROOT + "/District.unity", OpenSceneMode.Single);

            bool hasStage = false;
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
                if (go != null && go.name.StartsWith("__gb_")) { hasStage = true; break; }
            if (!hasStage)
                Debug.LogWarning("[SceneFlowUIBuilder] District 무대 없음 — 'DontLate/Build District Stage'를 먼저 실행하라. UI만 얹는다.");

            Transform root = CreateFlowCanvas().transform;
            // S-134 ⑥ — 캔버스를 켠다(종전 SetActive(false)). '집으로'가 배송지에서도 필요하다.
            // 구 내비 버튼('다른 구역으로')은 계속 끈다 — 이동은 엣지 워크·지도 체제(S-062 ⑥).
            BuildDeliveryEndCanvas(root, font, navButtons: false);
            EditorSceneManager.SaveScene(scene, SCENES_ROOT + "/District.unity");
        }

        // S-038: 아파트 등 배송 씬 공용 — 라벨 + 정산 UI. District와 같은 마감 UI를 얹는다.
        private static void BuildDeliveryEndUI(string sceneName, string labelText, TMP_FontAsset font)
        {
            Scene scene = EditorSceneManager.OpenScene(SCENES_ROOT + "/" + sceneName + ".unity", OpenSceneMode.Single);
            Transform root = CreateFlowCanvas().transform;

            CreateTutorialBanner(root, labelText, font);

            // S-134 ⑥ — 캔버스 활성 + 내비 버튼만 억제 (District와 동일 규칙).
            BuildDeliveryEndCanvas(root, font, navButtons: false);
            EditorSceneManager.SaveScene(scene, SCENES_ROOT + "/" + sceneName + ".unity");
        }

        // "집으로"(정산)·"다른 구역으로"·정산 패널 — District·Apartment 공용 마감 블록.
        // S-062 ⑥: navButtons=false면 정산 블록만 (Camp 이식용 — 이동은 엣지 워크·지도 몫).
        private static void BuildDeliveryEndCanvas(Transform root, TMP_FontAsset font, bool navButtons = true)
        {
            // "집으로" = 즉시 전이가 아니라 정산 패널을 연다 (S-009 ⑥) — SceneAdvanceButton 없이 만든다.
            GameObject endDay = new GameObject("EndDayButton", typeof(RectTransform));
            endDay.transform.SetParent(root, false);
            Image endImg = endDay.AddComponent<Image>();
            endImg.color = new Color(1f, 1f, 1f, 0.001f); // 클릭 영역 전용. 실아트는 자식 이미지.
            RectTransform endRect = (RectTransform)endDay.transform;
            endRect.anchorMin = endRect.anchorMax = endRect.pivot = new Vector2(1f, 1f);
            endRect.sizeDelta = new Vector2(380f, 125f);
            endRect.anchoredPosition = new Vector2(-40f, -160f);
            Button endButton = endDay.AddComponent<Button>();
            endButton.targetGraphic = endImg;
            // gohome.png는 1536×1024 투명 캔버스 안 실제 패널이 984×324다.
            // 패널을 380×125로 보이게 하려면 원본 Image Rect는 593×395로 둔다.
            Image endArt = CreateImage(endDay.transform, "BackgroundArt", Color.white);
            endArt.sprite = LoadPanelSprite("gohome");
            endArt.preserveAspect = true;
            endArt.raycastTarget = false;
            AnchorCentered(endArt.rectTransform, Vector2.zero, new Vector2(593f, 395f));

            CreatePanelIcon(endDay.transform, "TwinkleIcon", "twincle_icon",
                new Vector2(-142f, 0f), new Vector2(275f, 183f));
            CreatePanelIcon(endDay.transform, "HouseIcon", "house_icon",
                new Vector2(142f, 0f), new Vector2(178f, 118f));

            TMP_Text endLabel = CreateText(endDay.transform, "Label", "정산하기(집)", font, 30f, NAVY, // S-161 남규님 문구
                TextAlignmentOptions.Center, FontStyles.Bold);
            AnchorCentered(endLabel.rectTransform, Vector2.zero, new Vector2(250f, 70f));

            if (navButtons)
            {
                // S-028 ③: 다른 구역 이동 — Travel(구역 선택)로 재진입.
                CreateButton(root, "TravelButton", "다른 구역으로", GameScene.Travel, font, AMBER,
                    new Vector2(1f, 1f), new Vector2(-40f, -310f), new Vector2(380f, 74f), 30f);

                // S-053 ④: 캠프 직행.
                CreateButton(root, "CampButton", "캠프로 (추가 상차)", GameScene.Camp, font, new Color(0.55f, 0.62f, 0.75f, 1f),
                    new Vector2(1f, 1f), new Vector2(-40f, -400f), new Vector2(380f, 74f), 30f);
            }

            // S-087 — 영수증 스킨: 흰 종이 + 상하 톱니 절취선 + 네이비 잉크 (참고 이미지 정합).
            Color paper = new Color(0.97f, 0.97f, 0.95f, 1f);
            Color ink = new Color(0.16f, 0.22f, 0.30f, 1f);
            GameObject panel = CreateImage(root, "SettlementPanel", paper).gameObject;
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(620f, 720f);
            panelRect.anchoredPosition = Vector2.zero;

            // 톱니 절취선 — 상하 가장자리에 45도 다이아몬드 이빨.
            foreach (float edge in new[] { 1f, -1f })
                for (int tooth = 0; tooth < 21; tooth++)
                {
                    Image diamond = CreateImage(panel.transform, "Tooth", paper);
                    RectTransform dRect = diamond.rectTransform;
                    dRect.anchorMin = dRect.anchorMax = new Vector2(0f, edge > 0 ? 1f : 0f);
                    dRect.pivot = new Vector2(0.5f, 0.5f);
                    dRect.sizeDelta = new Vector2(21f, 21f);
                    dRect.anchoredPosition = new Vector2(15f + tooth * 29.5f, 0f);
                    dRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    diamond.raycastTarget = false;
                }

            Image panelInner = CreateImage(panel.transform, "Inner", paper);
            panelInner.raycastTarget = true; // 뒤 클릭 차단
            RectTransform innerRect = panelInner.rectTransform;
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(0f, 0f);
            innerRect.offsetMax = new Vector2(0f, 0f);

            // S-075 ⑥ — 폰트 26·하단 여백: 항목 리스트가 확인 버튼과 겹치지 않게. (S-087 — 잉크색)
            TMP_Text body = CreateText(panelInner.transform, "Body", string.Empty, font, 26f, ink,
                TextAlignmentOptions.TopLeft, FontStyles.Normal);
            RectTransform bodyRect = body.rectTransform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(48f, 140f);
            bodyRect.offsetMax = new Vector2(-48f, -40f);

            GameObject confirm = new GameObject("ConfirmButton", typeof(RectTransform));
            confirm.transform.SetParent(panelInner.transform, false);
            Image confirmImg = confirm.AddComponent<Image>();
            confirmImg.color = AMBER;
            RectTransform confirmRect = (RectTransform)confirm.transform;
            confirmRect.anchorMin = confirmRect.anchorMax = confirmRect.pivot = new Vector2(0.5f, 0f);
            confirmRect.sizeDelta = new Vector2(320f, 84f);
            confirmRect.anchoredPosition = new Vector2(0f, 32f);
            Button confirmButton = confirm.AddComponent<Button>();
            confirmButton.targetGraphic = confirmImg;
            TMP_Text confirmLabel = CreateText(confirm.transform, "Label", "확인 — 집으로", font, 32f, NAVY,
                TextAlignmentOptions.Center, FontStyles.Bold);
            StretchFull(confirmLabel.rectTransform);

            SettlementView view = root.gameObject.AddComponent<SettlementView>();
            SetField(view, "_gameState", AssetDatabase.LoadAssetAtPath<GameStateSO>("Assets/Data/GameState.asset")); // S-087
            SetField(view, "_openButton", endButton);
            SetField(view, "_panel", panel);
            SetField(view, "_bodyLabel", body);
            SetField(view, "_confirmButton", confirmButton);
            EditorUtility.SetDirty(view);
            panel.SetActive(false);
        }

        private static void CreateHomeAdvanceButton(Transform root, TMP_FontAsset font)
        {
            GameObject go = new GameObject("AdvanceButton", typeof(RectTransform));
            go.transform.SetParent(root, false);

            Image clickArea = go.AddComponent<Image>();
            clickArea.color = new Color(1f, 1f, 1f, 0.001f);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(500f, 165f);
            rect.anchoredPosition = new Vector2(0f, 150f);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = clickArea;
            SceneAdvanceButton advance = go.AddComponent<SceneAdvanceButton>();
            SetField(advance, "_target", GameScene.Camp);
            EditorUtility.SetDirty(advance);

            // gohome.png의 1536×1024 원본 비율을 유지한다. 실제 패널은 약 500×165로 보인다.
            Image background = CreateImage(go.transform, "BackgroundArt", Color.white);
            background.sprite = LoadPanelSprite("gohome");
            background.preserveAspect = true;
            background.raycastTarget = false;
            AnchorCentered(background.rectTransform, Vector2.zero, new Vector2(780f, 520f));

            CreatePanelIcon(go.transform, "SunIcon", "sun",
                new Vector2(-160f, 0f), new Vector2(270f, 180f));

            TMP_Text label = CreateText(go.transform, "Label", "하루 시작 → 물류캠프", font, 30f, NAVY,
                TextAlignmentOptions.Center, FontStyles.Bold);
            AnchorCentered(label.rectTransform, new Vector2(45f, 0f), new Vector2(380f, 72f));
        }

        // ── UI 헬퍼 ──────────────────────────────────────────

        private static void CreateTutorialBanner(Transform root, string message, TMP_FontAsset font,
            float anchoredY = -34f, string panelSpriteName = "tutorial_long")
        {
            bool useCampPanel = panelSpriteName == "tutorial";
            GameObject banner = new GameObject("TutorialBanner", typeof(RectTransform));
            banner.transform.SetParent(root, false);
            RectTransform bannerRect = (RectTransform)banner.transform;
            bannerRect.anchorMin = bannerRect.anchorMax = bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.sizeDelta = useCampPanel ? new Vector2(746f, 170f) : new Vector2(900f, 148f);
            bannerRect.anchoredPosition = new Vector2(0f, anchoredY);

            // 두 패널 모두 1536×1024 원본 비율 그대로 사용한다. Image Rect 역시 1006×671
            // (동일한 1.5:1)이므로 가로·세로가 따로 늘어나 찌부되는 일이 없다.
            Image background = CreateImage(banner.transform, "BackgroundArt", Color.white);
            background.sprite = LoadPanelSprite(panelSpriteName);
            background.preserveAspect = true;
            background.raycastTarget = false;
            AnchorCentered(background.rectTransform, new Vector2(0f, -24f), new Vector2(1006f, 671f));

            CreatePanelIcon(banner.transform, "ParcelIcon", "box_icon",
                new Vector2(useCampPanel ? -310f : -385f, 0f), new Vector2(265f, 177f));

            TMP_Text label = CreateText(banner.transform, "Label", message, font, 34f, NAVY,
                TextAlignmentOptions.Center, FontStyles.Bold);
            AnchorCentered(label.rectTransform, new Vector2(useCampPanel ? 35f : 45f, 8f),
                new Vector2(useCampPanel ? 600f : 760f, 76f));

            CreateTutorialCloseButton(banner.transform);
        }

        private static void CreateTutorialCloseButton(Transform banner)
        {
            GameObject closeGo = new GameObject("CloseButton", typeof(RectTransform));
            closeGo.transform.SetParent(banner, false);
            Image closeImage = closeGo.AddComponent<Image>();
            closeImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/x.png");
            closeImage.color = Color.white;
            closeImage.preserveAspect = true;
            RectTransform closeRect = closeImage.rectTransform;
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(47f, 47f);
            closeRect.anchoredPosition = new Vector2(-20f, -16f);
            Button closeButton = closeGo.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeGo.AddComponent<DismissTutorialButton>();
        }

        private static void CreatePanelIcon(Transform parent, string objectName, string spriteName,
            Vector2 anchoredPosition, Vector2 sourceCanvasSize)
        {
            Image icon = CreateImage(parent, objectName, Color.white);
            icon.sprite = LoadPanelSprite(spriteName);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            AnchorCentered(icon.rectTransform, anchoredPosition, sourceCanvasSize);
        }

        private static Sprite LoadPanelSprite(string name)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PANEL_ART_ROOT + name + ".png");
            if (sprite == null)
                Debug.LogWarning("[SceneFlowUIBuilder] 패널 스프라이트 로드 실패: " + name);
            return sprite;
        }

        private static Canvas CreateFlowCanvas()
        {
            ClearFlowUI();
            EnsureCoreLoader();

            GameObject go = new GameObject(UI_PREFIX + "FlowCanvas");
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20; // HUD(10) 위, Fade(100) 아래
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        // 씬 단독 Play 지원(S-013) — 콘텐츠 씬마다 Core 사후 로더 1개 보장(멱등).
        private static void EnsureCoreLoader()
        {
            foreach (EnsureCoreLoaded existing in Object.FindObjectsByType<EnsureCoreLoaded>(FindObjectsInactive.Include))
                Object.DestroyImmediate(existing.gameObject);

            GameObject go = new GameObject("__ui_EnsureCore");
            go.AddComponent<EnsureCoreLoaded>();
        }

        private static void ClearFlowUI()
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go == null || go.transform.parent != null) continue;
                if (go.name.StartsWith(UI_PREFIX)) Object.DestroyImmediate(go);
            }
        }

        private static void CreateButton(Transform parent, string name, string label, GameScene target,
            TMP_FontAsset font, Color bgColor, Vector2 anchor, Vector2 anchoredPos, Vector2 size, float fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Image img = go.AddComponent<Image>();
            img.color = bgColor;

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = img;

            SceneAdvanceButton advance = go.AddComponent<SceneAdvanceButton>();
            SetField(advance, "_target", target);
            EditorUtility.SetDirty(advance);

            TMP_Text text = CreateText(go.transform, "Label", label, font, fontSize, NAVY,
                TextAlignmentOptions.Center, FontStyles.Bold);
            StretchFull(text.rectTransform);
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, TMP_FontAsset font,
            float fontSize, Color color, TextAlignmentOptions align, FontStyles style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
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
            return img;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AnchorCentered(RectTransform rect, Vector2 anchoredPos, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
        }

        private static void AnchorCorner(RectTransform rect, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
        }

        private static void SetField(Object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                Debug.LogError("[SceneFlowUIBuilder] 필드 없음: " + target.GetType().Name + "." + fieldName);
                return;
            }
            field.SetValue(target, value);
        }
    }
}
