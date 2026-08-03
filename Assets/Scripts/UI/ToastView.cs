using System.Collections;
using TMPro;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 획득 알림 토스트 (S-133 ⑤ — 정수님 QA "보상 표시가 비직관적").
    /// "OO을 획득하였습니다." 한 줄이 화면 중앙 위에 잠깐 떴다 사라진다.
    ///
    /// Core 씬 상주 — 씬이 바뀌어도 살아 있어야 어디서 얻든 뜬다.
    /// 정산창(timeScale=0) 위에서도 보여야 하므로 시간은 **unscaled**로 센다.
    /// </summary>
    public class ToastView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TMP_Text _label;

        private const float HOLD = 1.6f;   // 완전 불투명 유지
        private const float FADE = 0.5f;   // 사라지는 시간
        private const float RISE = 26f;    // 뜨면서 올라가는 픽셀

        private Coroutine _routine;
        private Vector2 _homePosition;
        private RectTransform _rect;

        private void Awake()
        {
            _rect = _group != null ? _group.GetComponent<RectTransform>() : null;
            if (_rect != null) _homePosition = _rect.anchoredPosition;
            if (_group != null) _group.alpha = 0f;
        }

        private void OnEnable() => WorldEvents.ItemAcquired += Show;
        private void OnDisable() => WorldEvents.ItemAcquired -= Show;

        private void Show(string label)
        {
            if (_group == null || _label == null) return;
            _label.text = label;
            if (_routine != null) StopCoroutine(_routine); // 연속 획득 — 최신 것만 보여준다
            _routine = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            _group.alpha = 1f;
            if (_rect != null) _rect.anchoredPosition = _homePosition;

            float t = 0f;
            while (t < HOLD)
            {
                t += Time.unscaledDeltaTime; // 정산창 위에서도 흐른다
                yield return null;
            }

            t = 0f;
            while (t < FADE)
            {
                t += Time.unscaledDeltaTime;
                float k = t / FADE;
                _group.alpha = 1f - k;
                if (_rect != null) _rect.anchoredPosition = _homePosition + new Vector2(0f, RISE * k);
                yield return null;
            }
            _group.alpha = 0f;
            _routine = null;
        }
    }
}
