using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 간판 밤 발광 — 간판 렌더러 "자체" 머티리얼의 이미시브를 구동한다 (D-051).
    /// v1 additive 발광판(간판 앞을 덮는 별도 쿼드)은 실간판을 가려 반려(R11) — 폐지.
    /// 간판은 분리 익스포트(별도 메시/머티리얼 슬롯)로 들어오는 것이 전제 (orders/art.md 공통 규격).
    /// 통신 규약(D-027): WorldEvents.DayPhaseChanged만 구독하고, 초기 상태만 현재 phase에서 읽는다.
    /// URP Lit 이미시브는 _EMISSION 키워드가 머티리얼에 있어야 해 MPB로는 못 켠다 —
    /// Awake에서 인스턴스화 후 키워드를 상시 켜고, 소등은 이미시브 색을 검정으로 내려 표현한다.
    /// </summary>
    public class SignGlow : MonoBehaviour
    {
        [SerializeField] private Renderer _signRenderer;
        [SerializeField] private Color _color = new Color(0.208f, 0.878f, 0.784f); // #35e0c8
        [Tooltip("이 phase로 진입하면 점등. 밤은 항상 점등.")]
        [SerializeField] private DayPhase _litPhase = DayPhase.Evening;
        [SerializeField] private float _intensity = 2f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private Material _material; // 인스턴스 — 씬 언로드 시 OnDestroy에서 해제

        private void Awake()
        {
            if (_signRenderer == null) _signRenderer = GetComponent<Renderer>();
            _material = _signRenderer.material;
            _material.EnableKeyword("_EMISSION");
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
            // 초기 상태는 현재 phase에서 즉시 반영(구독 전에 지나간 브로드캐스트 대비).
            if (WorldDayNightManager.Instance != null)
                SetLit(IsLitPhase(WorldDayNightManager.Instance.Phase));
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        private void OnPhaseChanged(DayPhase phase)
        {
            SetLit(IsLitPhase(phase));
        }

        private bool IsLitPhase(DayPhase p) => p == _litPhase || p == DayPhase.Night;

        private void SetLit(bool on)
        {
            _material.SetColor(EmissionColorId, on ? _color * _intensity : Color.black);
        }
    }
}
