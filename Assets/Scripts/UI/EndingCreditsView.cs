using System.Collections;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 엔딩 크레딧 오버레이 (S-104 ④) — 카메라가 하늘로 오르는 동안 로고 "늦지마"가
    /// "잊지마"로 크로스페이드되고 크레딧이 차례로 밝아진다. 표시 전용 — 진행은 WorldEndingManager 소유.
    /// </summary>
    public class EndingCreditsView : MonoBehaviour
    {
        private static readonly Color LATE_YELLOW = new Color(1f, 0.78f, 0.25f);
        private static readonly Color REMEMBER_SKY = new Color(0.45f, 0.78f, 1f);

        private static readonly string[] CREDITS =
        {
            "늦지마 (Don't Late)",
            "기획·디렉팅  남규",
            "오디오  정수",
            "아트  민지",
            "시공  Claude Code — AI 공장",
            "플레이해 주셔서 감사합니다",
        };

        /// <summary>크레딧 전체 재생 — 끝나면 자체 정리. 매니저가 yield로 기다린다.</summary>
        public IEnumerator Play()
        {
            GameObject canvasGo = new GameObject("EndingCreditsCanvas");
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 95; // 페이드(100) 아래 · 게임 UI 위

            TMPro.TMP_Text late = MakeText(canvasGo.transform, "LogoLate", "늦지마", 96f, LATE_YELLOW,
                new Vector2(0f, 120f));
            TMPro.TMP_Text remember = MakeText(canvasGo.transform, "LogoRemember", "잊지마", 96f, REMEMBER_SKY,
                new Vector2(0f, 120f));
            remember.alpha = 0f;

            yield return Fade(late, 0f, 1f, 1.6f); // 로고 등장
            yield return WorldEndingManager.WaitClamped(1.4f);

            // "늦지마" → "잊지마" 크로스페이드 — 까칠함이 기억으로 바뀌는 순간.
            float t = 0f;
            while (t < 1f)
            {
                t += Mathf.Min(Time.deltaTime, 0.05f) / 2.2f;
                late.alpha = 1f - t;
                remember.alpha = t;
                yield return null;
            }
            yield return WorldEndingManager.WaitClamped(0.8f);

            // 크레딧 — 로고 아래 차례로.
            for (int i = 0; i < CREDITS.Length; i++)
            {
                TMPro.TMP_Text line = MakeText(canvasGo.transform, "Credit" + i, CREDITS[i],
                    i == 0 ? 34f : 26f, new Color(1f, 1f, 1f, 1f),
                    new Vector2(0f, -10f - i * 46f));
                line.alpha = 0f;
                StartCoroutine(Fade(line, 0f, 1f, 0.9f));
                yield return WorldEndingManager.WaitClamped(0.55f);
            }
            yield return WorldEndingManager.WaitClamped(3f);

            // 전체 서서히 소등 — 씬 전환 페이드에 바통.
            var labels = canvasGo.GetComponentsInChildren<TMPro.TMP_Text>();
            float fade = 0f;
            while (fade < 1f)
            {
                fade += Mathf.Min(Time.deltaTime, 0.05f) / 1.2f;
                for (int i = 0; i < labels.Length; i++)
                    if (labels[i] != null) labels[i].alpha = Mathf.Min(labels[i].alpha, 1f - fade);
                yield return null;
            }
            Destroy(canvasGo);
        }

        private static TMPro.TMP_Text MakeText(Transform parent, string name, string text,
            float size, Color color, Vector2 anchoredPosition)
        {
            var label = new GameObject(name, typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
            label.transform.SetParent(parent, false);
            if (UiOverlayFont.Korean != null) label.font = UiOverlayFont.Korean;
            label.fontSize = size;
            label.fontStyle = TMPro.FontStyles.Bold;
            label.color = color;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(1000f, size + 24f);
            label.text = text;
            return label;
        }

        private static IEnumerator Fade(TMPro.TMP_Text label, float from, float to, float seconds)
        {
            float t = 0f;
            while (t < 1f && label != null)
            {
                t += Mathf.Min(Time.deltaTime, 0.05f) / seconds;
                label.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
        }
    }
}
