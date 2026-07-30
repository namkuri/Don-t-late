using TMPro;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 머리 위 한마디 — 풀해상 오버레이 말풍선 (S-123 ① 추출). 원래 PedestrianNpc가 혼자 갖고
    /// 있던 인사말 렌더를, 사용처가 셋(행인 인사·상자 명중 욕/응원·주인공 독백)이 되면서 공용으로 뺐다.
    /// 붙은 오브젝트를 따라다니며 표시하고, 새 말이 오면 이전 것을 지운다.
    /// </summary>
    public class SpeechBubble : MonoBehaviour
    {
        private const float HEAD_HEIGHT = 2.1f;

        private GameObject _canvasGo;
        private TMP_Text _label;
        private float _elapsed;
        private float _seconds;

        /// <summary>대상 오브젝트에 말풍선을 띄운다 (컴포넌트가 없으면 붙인다).</summary>
        public static void ShowOn(GameObject target, string message, float seconds = 1.6f)
        {
            if (target == null || string.IsNullOrEmpty(message)) return;
            SpeechBubble bubble = target.GetComponent<SpeechBubble>();
            if (bubble == null) bubble = target.AddComponent<SpeechBubble>();
            bubble.Show(message, seconds);
        }

        public void Show(string message, float seconds = 1.6f)
        {
            // 연타 시 이전 말풍선을 먼저 치운다 (S-102 실사고 교훈 — 정리 주체를 하나로).
            if (_canvasGo != null) Destroy(_canvasGo);
            _elapsed = 0f;
            _seconds = Mathf.Max(0.5f, seconds);

            _canvasGo = new GameObject("SpeechBubble");
            Canvas canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6;

            var label = new GameObject("Line", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(_canvasGo.transform, false);
            if (UiOverlayFont.Korean != null) label.font = UiOverlayFont.Korean;
            label.fontSize = 22f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            label.rectTransform.sizeDelta = new Vector2(300f, 30f);
            label.text = message;
            _label = label;
        }

        // 추종은 반드시 LateUpdate — Update에서 잡으면 카메라(CameraFollowX)가 그 뒤에 움직여
        // 말풍선이 한 프레임 뒤처진 좌표에 그려진다(캡처 게이트가 62px 우측 편향으로 적발).
        private void LateUpdate()
        {
            if (_label == null) return;
            _elapsed += Time.deltaTime;
            if (_elapsed >= _seconds)
            {
                if (_canvasGo != null) Destroy(_canvasGo);
                _canvasGo = null;
                _label = null;
                return;
            }

            Camera camera = Camera.main;
            if (camera == null) return;
            Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * HEAD_HEIGHT);
            if (screen.z > 0f) _label.rectTransform.position = new Vector3(screen.x, screen.y, 0f);
            float fadeAt = _seconds - 0.4f;
            _label.color = new Color(1f, 1f, 1f, _elapsed < fadeAt ? 1f : 1f - (_elapsed - fadeAt) / 0.4f);
        }

        private void OnDestroy()
        {
            if (_canvasGo != null) Destroy(_canvasGo);
        }
    }
}
