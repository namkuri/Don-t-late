using System.Collections;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 씬 전환 페이드와 "늦지마!" 컷인. 표시만 담당하고 판단은 하지 않는다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private float _fadeDuration = 0.35f;
        [SerializeField] private GameObject _lateCutIn;
        [SerializeField] private float _cutInDuration = 1.2f;

        private Coroutine _fadeRoutine;
        private Coroutine _cutInRoutine;

        private void Awake()
        {
            if (_group == null) _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            if (_lateCutIn != null) _lateCutIn.SetActive(false);
        }

        private void OnEnable()
        {
            WorldEvents.SceneTransitionStarted += OnTransitionStarted;
            WorldEvents.SceneTransitionCompleted += OnTransitionCompleted;
            WorldEvents.DeliveryFailed += OnDeliveryFailed;
        }

        private void OnDisable()
        {
            WorldEvents.SceneTransitionStarted -= OnTransitionStarted;
            WorldEvents.SceneTransitionCompleted -= OnTransitionCompleted;
            WorldEvents.DeliveryFailed -= OnDeliveryFailed;
        }

        private void OnTransitionStarted(GameScene scene) => Fade(1f, blockRaycasts: true);
        private void OnTransitionCompleted(GameScene scene) => Fade(0f, blockRaycasts: false);

        private void Fade(float target, bool blockRaycasts)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _group.blocksRaycasts = blockRaycasts;
            _fadeRoutine = StartCoroutine(SpriteTween.Fade(_group, target, _fadeDuration));
        }

        private void OnDeliveryFailed(DeliveryData data)
        {
            if (_lateCutIn == null) return;
            if (_cutInRoutine != null) StopCoroutine(_cutInRoutine);
            _cutInRoutine = StartCoroutine(CutInRoutine());
        }

        private IEnumerator CutInRoutine()
        {
            _lateCutIn.SetActive(true);
            yield return new WaitForSeconds(_cutInDuration);
            _lateCutIn.SetActive(false);
            _cutInRoutine = null;
        }
    }
}
