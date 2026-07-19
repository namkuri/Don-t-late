using System.Collections;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 코드 트윈 헬퍼. 문 개폐·페이드처럼 애니메이션 클립을 쓰지 않는 연출에 쓴다.
    /// </summary>
    public static class SpriteTween
    {
        public static IEnumerator MoveLocal(Transform target, Vector3 to, float duration)
        {
            Vector3 from = target.localPosition;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                target.localPosition = Vector3.LerpUnclamped(from, to, SmoothStep(t / duration));
                yield return null;
            }
            target.localPosition = to;
        }

        public static IEnumerator Fade(CanvasGroup group, float to, float duration)
        {
            float from = group.alpha;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.LerpUnclamped(from, to, SmoothStep(t / duration));
                yield return null;
            }
            group.alpha = to;
        }

        public static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
