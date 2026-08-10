using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// S-274 — 저녁·밤에만 켜지는 실내 등. Home 방 조명(남규님이 씬에서 맞춘 Point Light)을
    /// 빌더 정본으로 승격하면서, 낮에도 켜져 있지 않도록 낮밤에 물린다.
    ///
    /// 통신 규약(D-027)은 <see cref="SignGlow"/>와 같다: `DayPhaseChanged`만 구독하고,
    /// 초기 상태는 `Start`에서 현재 phase로 한 번 읽는다(구독 전에 지나간 브로드캐스트 대비).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class NightLamp : MonoBehaviour
    {
        [Tooltip("이 phase부터 점등한다. 밤은 항상 점등.")]
        [SerializeField] private DayPhase _litPhase = DayPhase.Evening;

        private Light _light;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _light.enabled = false;
        }

        private void OnEnable() => WorldEvents.DayPhaseChanged += OnPhaseChanged;
        private void OnDisable() => WorldEvents.DayPhaseChanged -= OnPhaseChanged;

        private void Start()
        {
            if (WorldDayNightManager.Instance != null) Apply(WorldDayNightManager.Instance.Phase);
        }

        private void OnPhaseChanged(DayPhase phase) => Apply(phase);

        private void Apply(DayPhase phase)
        {
            if (_light == null) return;
            _light.enabled = phase == DayPhase.Night || phase == _litPhase;
        }
    }
}
