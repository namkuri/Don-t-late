using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 간판 위에 씌우는 additive 발광판. 밤 전용 이미시브(아키텍처 §8-7) — 저녁/밤에 켜지고
    /// 아침에 꺼진다. 가로등과 달리 플리커 없이 즉시 점등(YAGNI).
    /// 통신 규약(D-027): WorldEvents.DayPhaseChanged만 구독하고, 초기 상태만 현재 phase에서 읽는다.
    /// 색·강도는 MaterialPropertyBlock으로 밀어넣어 머티리얼 복제 없이 인스턴스별로 다를 수 있다.
    /// </summary>
    public class SignGlowPlate : MonoBehaviour
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Color _color = new Color(0.208f, 0.878f, 0.784f); // #35e0c8
        [Tooltip("이 phase로 진입하면 점등. 밤은 항상 점등.")]
        [SerializeField] private DayPhase _litPhase = DayPhase.Evening;
        [SerializeField] private float _intensity = 1f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            ApplyLook();
            _renderer.enabled = false;
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
            // 초기 상태는 현재 phase에서 즉시 반영(구독 전에 지나간 브로드캐스트 대비).
            if (WorldDayNightManager.Instance != null)
                _renderer.enabled = IsLitPhase(WorldDayNightManager.Instance.Phase);
        }

        private void OnPhaseChanged(DayPhase phase)
        {
            _renderer.enabled = IsLitPhase(phase);
        }

        private bool IsLitPhase(DayPhase p) => p == _litPhase || p == DayPhase.Night;

        private void ApplyLook()
        {
            _mpb ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorId, _color);
            _mpb.SetFloat(IntensityId, _intensity);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
