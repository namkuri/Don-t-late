using UnityEngine;

namespace DontLate
{
    public class ChristmasStringLights : MonoBehaviour
    {
        [SerializeField] private Renderer[] _bulbs;
        [SerializeField] private Light[] _fillLights;
        [SerializeField] private Color[] _palette =
        {
            new Color(1f, 0.35f, 0.37f),
            new Color(1f, 0.82f, 0.40f),
            new Color(0.32f, 0.82f, 0.45f),
            new Color(0.30f, 0.79f, 0.94f),
        };
        [SerializeField, Min(0.05f)] private float _stepSeconds = 0.2f;
        [SerializeField, Min(0f)] private float _emissionIntensity = 14f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock _block;
        private float _nextStepTime;
        private int _step;
        private bool _isLit;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            SetLit(false);
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
            if (WorldDayNightManager.Instance != null)
                SetLit(IsLitPhase(WorldDayNightManager.Instance.Phase));
        }

        private void Update()
        {
            if (!_isLit || Time.time < _nextStepTime) return;

            _nextStepTime = Time.time + _stepSeconds;
            _step++;
            ApplyBulbs(true);
        }

        private void OnPhaseChanged(DayPhase phase)
        {
            SetLit(IsLitPhase(phase));
        }

        private static bool IsLitPhase(DayPhase phase)
        {
            return phase == DayPhase.Evening || phase == DayPhase.Night;
        }

        private void SetLit(bool lit)
        {
            _isLit = lit;
            _nextStepTime = Time.time;
            ApplyBulbs(lit);

            if (_fillLights == null) return;
            foreach (Light fillLight in _fillLights)
            {
                if (fillLight != null) fillLight.enabled = lit;
            }
        }

        private void ApplyBulbs(bool lit)
        {
            if (_bulbs == null || _palette == null || _palette.Length == 0) return;
            _block ??= new MaterialPropertyBlock();

            for (int i = 0; i < _bulbs.Length; i++)
            {
                Renderer bulb = _bulbs[i];
                if (bulb == null) continue;

                Color color = _palette[(i + _step) % _palette.Length];
                float pulse = ((i + _step) % 4 == 0) ? 1f : 0.48f;

                bulb.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, lit ? color : color * 0.16f);
                _block.SetColor(EmissionColorId, lit ? color * (_emissionIntensity * pulse) : Color.black);
                bulb.SetPropertyBlock(_block);
            }

            if (!lit || _fillLights == null || _fillLights.Length == 0) return;
            for (int i = 0; i < _fillLights.Length; i++)
            {
                Light fillLight = _fillLights[i];
                if (fillLight != null) fillLight.color = _palette[(i + _step) % _palette.Length];
            }
        }
    }
}
