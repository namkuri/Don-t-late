using System.Collections;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 밤하늘 별밭 쿼드의 페이드 제어. 셰이더(DontLate/StarField)가 별을 절차적으로 그리고,
    /// 이 컴포넌트는 _GlobalAlpha만 시간대에 맞춰 러프한다 — 밤 1, 저녁 0.35, 낮/아침 0.
    /// 통신 규약(D-027): WorldEvents.DayPhaseChanged만 구독하고 초기 상태만 현재 phase에서 읽는다.
    /// 알파는 MaterialPropertyBlock으로 밀어넣어 머티리얼 복제 없이 인스턴스별로 다를 수 있다.
    /// </summary>
    public class StarField : MonoBehaviour
    {
        [SerializeField] private Renderer _renderer;
        [Tooltip("밤에 도달하는 최대 알파.")]
        [SerializeField] private float _nightAlpha = 1f;
        [Tooltip("저녁에 옅게 뜨는 알파.")]
        [SerializeField] private float _eveningAlpha = 0.35f;
        [Tooltip("아침·낮 알파(별 소멸).")]
        [SerializeField] private float _dayAlpha = 0f;
        [Tooltip("시간대 전환 시 알파를 러프하는 시간(초).")]
        [SerializeField] private float _fadeDuration = 2f;

        private static readonly int GlobalAlphaId = Shader.PropertyToID("_GlobalAlpha");

        private MaterialPropertyBlock _mpb;
        private float _currentAlpha;
        private Coroutine _fade;

        private void Awake()
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            SetAlpha(0f);
        }

        private void OnEnable()
        {
            WorldEvents.DayPhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            WorldEvents.DayPhaseChanged -= OnPhaseChanged;
        }

        private void Start()
        {
            // 구독 전에 지나간 브로드캐스트 대비 — 현재 phase를 즉시(러프 없이) 반영.
            if (WorldDayNightManager.Instance != null)
                SetAlpha(TargetFor(WorldDayNightManager.Instance.Phase));
        }

        private void OnPhaseChanged(DayPhase phase)
        {
            float target = TargetFor(phase);
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeTo(target));
        }

        private float TargetFor(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.Night: return _nightAlpha;
                case DayPhase.Evening: return _eveningAlpha;
                default: return _dayAlpha;
            }
        }

        private IEnumerator FadeTo(float target)
        {
            float start = _currentAlpha;
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(Mathf.Lerp(start, target, elapsed / _fadeDuration));
                yield return null;
            }
            SetAlpha(target);
            _fade = null;
        }

        private void SetAlpha(float alpha)
        {
            _currentAlpha = alpha;
            _mpb ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(GlobalAlphaId, alpha);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
