using System.Collections;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 저녁에 자동 점등되는 가로등. 점등 phase 진입 순간 랜덤 플리커 후 안정된다.
    /// 통신 규약(D-027): WorldEvents.DayPhaseChanged만 구독하고 매니저를 직접 참조하지 않는다.
    /// 최초 수신 phase는 '현재 상태'로 간주해 플리커 없이 즉시 반영한다.
    /// </summary>
    public class StreetLampLight : MonoBehaviour
    {
        [SerializeField] private Light _light;
        [SerializeField] private Color _color = new Color(1f, 0.624f, 0.271f); // #ff9f45
        [SerializeField] private float _flickerDuration = 1.5f;
        [SerializeField] private float _flickerDelayMax = 0.5f;
        [Tooltip("이 phase로 진입하면 점등(플리커).")]
        [SerializeField] private DayPhase _litPhase = DayPhase.Evening;

        private float _baseIntensity;
        private bool _initialized;
        private System.Random _rng;
        private Coroutine _flicker;

        private void Awake()
        {
            if (_light == null) _light = GetComponent<Light>();
            _baseIntensity = _light.intensity;
            _light.color = _color;
            _rng = new System.Random(System.Guid.NewGuid().GetHashCode()); // 등마다 상이한 시드
            _light.enabled = false;
        }

        private void OnEnable()
        {
            WorldEvents.DayPhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            WorldEvents.DayPhaseChanged -= OnPhaseChanged;
            if (_flicker != null) { StopCoroutine(_flicker); _flicker = null; }
        }

        private void OnPhaseChanged(DayPhase phase)
        {
            bool first = !_initialized;
            _initialized = true;

            if (first) { SetLit(IsLitPhase(phase)); return; } // 최초 상태는 즉시 반영

            if (phase == _litPhase) StartFlicker();           // 저녁 진입 → 플리커 점등
            else if (phase == DayPhase.Morning) SetLit(false); // 아침 진입 → 소등
        }

        private bool IsLitPhase(DayPhase p) => p == _litPhase || p == DayPhase.Night;

        private void SetLit(bool on)
        {
            if (_flicker != null) { StopCoroutine(_flicker); _flicker = null; }
            _light.enabled = on;
            _light.intensity = _baseIntensity;
        }

        private void StartFlicker()
        {
            if (_flicker != null) StopCoroutine(_flicker);
            _flicker = StartCoroutine(FlickerRoutine());
        }

        private IEnumerator FlickerRoutine()
        {
            yield return new WaitForSeconds((float)_rng.NextDouble() * _flickerDelayMax);

            _light.enabled = true;
            float elapsed = 0f;
            while (elapsed < _flickerDuration)
            {
                elapsed += Time.deltaTime;
                float amp = 1f - (elapsed / _flickerDuration); // 진폭 감쇠 → 안정
                float wobble = (float)_rng.NextDouble();
                _light.intensity = _baseIntensity * (1f - amp * wobble);
                yield return null;
            }

            _light.intensity = _baseIntensity;
            _flicker = null;
        }
    }
}
