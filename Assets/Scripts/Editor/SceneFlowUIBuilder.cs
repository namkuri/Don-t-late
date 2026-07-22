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
        private const string FONT_PATH = "Assets/Art/UI/Fonts/Pretendard-Regular SDF.asset";
        private const string UI_PREFIX = "__ui_";

        private static readonly Color AMBER = new Color(1f, 0.624f, 0.271f, 1f);      // #ff9f45 목표
        private static readonly Color CYAN = new Color(0.208f, 0.878f, 0.784f, 1f);   // #35e0c8 상호작용
        private static readonly Color NAVY = new Color(0.039f, 0.051f, 0.086f, 1f);   // #0a0d16 배경

        [MenuItem("DontLate/Build/Scene Flow UI", priority = 15)]
        public static void BuildSceneFlowUI()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            if (font == null)
                Debug.LogWarning("[SceneFlowUIBuilder] Pretendard 폰트 미발견 — TMP 기본 폰트로 진행.");

            BuildMain(font);
            BuildHome(font);
            BuildLabeledAction("Camp", "물류캠프 — 패드에서 E로 적재", "짐 다 실었다 — 출발", GameScene.Travel, font);
            BuildTravel(font);
            BuildDistrict(font);

            Debug.Log("[SceneFlowUIBuilder] 씬 흐름 UI 조립 완료 — Main·Home·Camp·Travel·District 5씬.");
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
                AnchorCentered(logo.rectTransform, new Vector2(0f, 240f), new Vector2(900f, 518f));
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
                sub.gameObject.AddComponent<UIShine>();
            }
            else
            {
                TMP_Text sub = CreateText(root, "Subtitle", "지각 압박 배달 생존기", font, 48f, Color.white,
                    TextAlignmentOptions.Center, FontStyles.Normal);
                AnchorCentered(sub.rectTransform, new Vector2(0f, -30f), new Vector2(1200f, 80f));
            }

            // 늦지마맨 일러스트 — ui_title_man (좌하, 시작 버튼과 비겹침). 없으면 요소 자체 생략.
            Sprite manArt = CoreSceneBuilder.LoadUISprite("ui_title_man");
            if (manArt != null)
            {
                Image man = CreateImage(root, "TitleMan", Color.white);
                man.sprite = manArt;
                man.preserveAspect = true;
                RectTransform manRect = man.rectTransform;
                manRect.anchorMin = manRect.anchorMax = manRect.pivot = new Vector2(0f, 0f);
                manRect.sizeDelta = new Vector2(380f, 576f); // 크롭 아트 0.66:1 정합 (S-027 ⑥)
                manRect.anchoredPosition = new Vector2(60f, 40f);
            }

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
                startRect.anchoredPosition = new Vector2(0f, 90f);
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

            TMP_Text label = CreateText(root, "Label", labelText, font, 46f, Color.white,
                TextAlignmentOptions.TopLeft, FontStyles.Normal);
            AnchorCorner(label.rectTransform, new Vector2(0f, 1f), new Vector2(48f, -44f), new Vector2(1000f, 72f));

            CreateButton(root, "AdvanceButton", buttonText, target, font, CYAN,
                new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(600f, 104f), 40f);

            EditorSceneManager.SaveScene(scene, SCENES_ROOT + "/" + sceneName + ".unity");
        }

        // Home = 라벨 + 진행 버튼. 버튼은 인트로 전화(대화)가 끝나야 나타난다 (S-009 게이트).
        private static void BuildHome(TMP_FontAsset font)
        {
            Scene scene = EditorSceneManager.OpenScene(SCENES_ROOT + "/Home.unity", OpenSceneMode.Single);
            Transform root = CreateFlowCanvas().transform;

            TMP_Text label = CreateText(root, "Label", "집 — 아침", font, 46f, Color.white,
                TextAlignmentOptions.TopLeft, FontStyles.Normal);
            AnchorCorner(label.rectTransform, new Vector2(0f, 1f), new Vector2(48f, -44f), new Vector2(1000f, 72f));

            CreateButton(root, "AdvanceButton", "하루 시작 → 물류캠프", GameScene.Camp, font, CYAN,
                new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(600f, 104f), 40f);

            // 게이트는 상시 활성인 캔버스에 붙인다 — 버튼 자신에 붙이면 꺼질 때 구독이 끊긴다.
            HideDuringDialogue gate = root.gameObject.AddComponent<HideDuringDialogue>();
            SetField(gate, "_target", root.Find("AdvanceButton").gameObject);
            EditorUtility.SetDirty(gate);

            EditorSceneManager.SaveScene(scene, SCENES_ROOT + "/Home.unity");
        }

        // Travel = 미니맵 노드 선택(S-006). 노드 버튼엔 SceneAdvanceButton 대신 TravelMapView —
        // 선택이 시간 소모(근거리/원거리 상이)를 시계에 가산한 뒤 District로 전이한다.
        private static void BuildTravel(TMP_FontAsset font)
        {
            TuningConfigSO tuning = AssetDatabase.LoadAssetAtPath<TuningConfigSO>("Assets/Data/Tuning.asset");

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

            TMP_Text label = CreateText(root, "Label", "이동 — 배송 구역을 골라라 (멀수록 시간을 먹는다)", font,
                46f, Color.white, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            AnchorCorner(label.rectTransform, new Vector2(0f, 1f), new Vector2(48f, -44f), new Vector2(1400f, 72f));

            CreateTravelNode(root, "NodeNear", "행복빌라 구역", "행복빌라 구역 (가까움)", false, tuning, font,
                new Vector2(-360f, 40f));
            CreateTravelNode(root, "NodeFar", "달빛맨션 구역", "달빛맨션 구역 (멀다)", true, tuning, font,
                new Vector2(360f, -60f));

            CreateButton(root, "AdvanceButton", "캠프로 돌아간다", GameScene.Camp, font, AMBER,
                new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(420f, 74f), 30f);

            EditorSceneManager.SaveScene(scene, SCENES_ROOT + "/Travel.unity");
        }

        private static void CreateTravelNode(Transform parent, string name, string district, string label, bool isFar,
            TuningConfigSO tuning, TMP_FontAsset font, Vector2 anchoredPos)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Image img = go.AddComponent<Image>();
            img.color = CYAN;

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(560f, 130f);
            rect.anchoredPosition = anchoredPos;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = img;

            TravelMapView node = go.AddComponent<TravelMapView>();
            SetField(node, "_tuning", tuning);
            SetField(node, "_isFarNode", isFar);
            SetField(node, "_district", district);
            EditorUtility.SetDirty(node);

            float minutes = isFar ? tuning.travelFarMinutes : tuning.travelNearMinutes;
            TMP_Text text = CreateText(go.transform, "Label", label + "\n<size=60%>이동 " + minutes + "분</size>",
                font, 38f, NAVY, TextAlignmentOptions.Center, FontStyles.Bold);
            StretchFull(text.rectTransform);
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

            // "집으로" = 즉시 전이가 아니라 정산 패널을 연다 (S-009 ⑥) — SceneAdvanceButton 없이 만든다.
            GameObject endDay = new GameObject("EndDayButton", typeof(RectTransform));
            endDay.transform.SetParent(root, false);
            Image endImg = endDay.AddComponent<Image>();
            endImg.color = CYAN;
            RectTransform endRect = (RectTransform)endDay.transform;
            endRect.anchorMin = endRect.anchorMax = endRect.pivot = new Vector2(1f, 1f);
            endRect.sizeDelta = new Vector2(380f, 74f);
            endRect.anchoredPosition = new Vector2(-40f, -220f);
            Button endButton = endDay.AddComponent<Button>();
            endButton.targetGraphic = endImg;
            TMP_Text endLabel = CreateText(endDay.transform, "Label", "하루 끝 — 집으로", font, 30f, NAVY,
                TextAlignmentOptions.Center, FontStyles.Bold);
            StretchFull(endLabel.rectTransform);

            // 정산 패널 — 시안 테두리 + 네이비 내부 + 확인 버튼.
            GameObject panel = CreateImage(root, "SettlementPanel", CYAN).gameObject;
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(640f, 520f);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelInner = CreateImage(panel.transform, "Inner", NAVY);
            panelInner.raycastTarget = true; // 뒤 클릭 차단
            RectTransform innerRect = panelInner.rectTransform;
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(3f, 3f);
            innerRect.offsetMax = new Vector2(-3f, -3f);

            TMP_Text body = CreateText(panelInner.transform, "Body", string.Empty, font, 40f, Color.white,
                TextAlignmentOptions.TopLeft, FontStyles.Normal);
            RectTransform bodyRect = body.rectTransform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(48f, 130f);
            bodyRect.offsetMax = new Vector2(-48f, -44f);

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
            SetField(view, "_openButton", endButton);
            SetField(view, "_panel", panel);
            SetField(view, "_bodyLabel", body);
            SetField(view, "_confirmButton", confirmButton);
            EditorUtility.SetDirty(view);
            panel.SetActive(false);

            EditorSceneManager.SaveScene(scene, SCENES_ROOT + "/District.unity");
        }

        // ── UI 헬퍼 ──────────────────────────────────────────

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
            foreach (EnsureCoreLoaded existing in Object.FindObjectsByType<EnsureCoreLoaded>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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
