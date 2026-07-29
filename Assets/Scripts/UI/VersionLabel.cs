using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 화면 우하단 버전 라벨 (S-100 ②) — BuildVersionStamp가 구운 Resources/build_version.txt를
    /// 표시한다. 팀원이 에디터·빌드 어디서든 "지금 도는 게 어느 커밋인지" 즉시 식별.
    /// Core 씬 상주(빌더 부착) — 자체 소형 오버레이 캔버스(런타임 생성 관례).
    /// </summary>
    public class VersionLabel : MonoBehaviour
    {
        private TMPro.TMP_Text _label;

        // S-107 ④ — F1 토글 게이트: 폴링 담당(Core 상주라 전 씬 커버) + 자기 라벨 표시 반영.
        private void Update()
        {
            DebugOverlays.Tick();
            if (_label != null && _label.gameObject.activeSelf != DebugOverlays.Visible)
                _label.gameObject.SetActive(DebugOverlays.Visible);
        }

        private void Start()
        {
            TextAsset stamp = Resources.Load<TextAsset>("build_version");
            string text = stamp != null ? stamp.text : "v.unknown";
            if (Application.isEditor) text += " [editor]";

            var canvasGo = new GameObject("VersionCanvas");
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4; // HUD(10)보다 아래 — 게임 UI를 가리지 않는다

            var label = new GameObject("Version", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
            label.transform.SetParent(canvasGo.transform, false);
            if (UiOverlayFont.Korean != null) label.font = UiOverlayFont.Korean;
            label.fontSize = 14f;
            label.color = new Color(1f, 1f, 1f, 0.45f);
            label.alignment = TMPro.TextAlignmentOptions.BottomRight;
            label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-8f, 6f);
            rect.sizeDelta = new Vector2(360f, 20f);
            label.text = text;
            _label = label;
        }
    }
}
