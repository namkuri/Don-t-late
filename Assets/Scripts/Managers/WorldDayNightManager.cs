using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace DontLate
{
    /// <summary>
    /// 게임 시계의 유일한 진행 주체. 시각 원본은 GameStateSO가 보관하고 여기서만 쓴다.
    /// 다른 매니저는 SO를 읽거나 ClockTicked를 구독한다 — 매니저 간 직접 참조는 없다.
    /// 시각에 따른 하늘·태양광 구동을 담당한다. 구조만 코드에 있고 감각값(색·강도·노출)은
    /// 전부 인스펙터에 노출되어 사람이 튜닝한다(D-027). 갱신은 매 프레임이 아니라 게임 분 틱.
    /// </summary>
    public class WorldDayNightManager : MonoBehaviour
    {
        private const float MINUTES_PER_DAY = 24f * 60f;

        public static WorldDayNightManager Instance { get; private set; }

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private TuningConfigSO _tuning;

        [Header("태양 (Directional) — 감각값은 인스펙터 튜닝")]
        [SerializeField] private Light _sun;
        [Tooltip("태양의 좌우 방위(Y). 하루 동안 고정, X(고도)만 시각으로 회전.")]
        [SerializeField] private float _sunYaw = -30f;
        [Tooltip("시각(0~24h 정규화) → 태양광 색.")]
        [SerializeField] private Gradient _sunColor;
        [Tooltip("시각(0~24h 정규화) → 태양광 강도. 밤은 0 근처.")]
        [SerializeField] private AnimationCurve _sunIntensity;

        [Header("주변광·하늘 — 감각값은 인스펙터 튜닝")]
        [Tooltip("시각(0~24h 정규화) → 앰비언트 색.")]
        [SerializeField] private Gradient _ambientColor;
        [Tooltip("시각(0~24h 정규화) → 하늘 틴트(스카이박스 _SkyTint / 배경색 폴백).")]
        [SerializeField] private Gradient _skyColor;
        [Tooltip("시각(0~24h 정규화) → 스카이박스 노출.")]
        [SerializeField] private AnimationCurve _skyExposure;
        [Tooltip("스카이박스가 없을 때 배경색을 구동할 카메라(폴백).")]
        [SerializeField] private Camera _backgroundCamera;

        [Header("거리 안개 (Linear — S-145) — 감각값은 인스펙터 튜닝")]
        [Tooltip("끄면 거리 안개가 완전히 사라진다. 원경 깊이감도 함께 사라지니 룩 비교용.")]
        [SerializeField] private bool _fogEnabled = false;
        [Tooltip("시각(0~24h 정규화) → 안개 색. 밤 짙은 남색·낮 옅은 회백.")]
        [SerializeField] private Gradient _fogColor;
        [Tooltip("시각(0~24h 정규화) → 안개 짙기(0~1). 밤이 짙다. 끝 거리를 조이는 데 쓴다.")]
        [SerializeField] private AnimationCurve _fogDensity;

        // S-145 — 근경 면제. 종전 ExponentialSquared는 **시작 거리 개념이 없어** 카메라에서
        // 1u만 떨어져도 안개가 먹는다. 이 프로젝트는 카메라가 (0,8.1,−40.4) 망원(FOV 22)이라
        // **플레이 구간조차 카메라에서 약 41u** — 포그 계산상 이미 원경이라 캐릭터·소품까지
        // 뿌옇게 떴다(남규님 지적: "근경에 있는 얘들은 영향 안받게"). Linear로 바꿔 시작 거리를
        // 플레이 구간 밖에 두면 근경은 원본 색 그대로고, 원경만 깊이감을 얻는다.
        // S-151 — 46u는 **도로 위**였다. 지면이 z −40~40이라 카메라(z −40.4)에서 41~81u인데,
        // 시작이 46이면 도로 절반이 포그 구간이 되어 평평한 아스팔트에 그라디언트가 깔린다 —
        // 남규님이 "원거리랑 근거리 텍스처가 다르다"고 읽은 것이 이 이음매다.
        // 78u면 경계가 배경 건물 뒤(z 38~)로 넘어가 도로 면에는 안 걸린다.
        [Tooltip("이 거리 안쪽은 안개 영향 0. 도로 면(카메라에서 41~81u) 바깥에 두어야 노면에 이음매가 안 생긴다.")]
        [SerializeField] private float _fogStartDistance = 78f;
        [Tooltip("안개가 가장 옅을 때의 끝 거리. 짙기 1이면 아래 값까지 좁혀진다.")]
        [SerializeField] private float _fogEndFar = 260f;
        [Tooltip("안개가 가장 짙을 때의 끝 거리. 작을수록 원경이 빨리 묻힌다.")]
        [SerializeField] private float _fogEndNear = 110f; // S-151 — 시작이 78로 밀린 만큼 함께 뒤로

        private static readonly int SkyTintId = Shader.PropertyToID("_SkyTint");
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");

        private Material _skyInstance;
        private Material _originalSkybox;
        private bool _driveSkyTint;
        private bool _driveExposure;

        private int _lastTickedMinute = -1;
        private DayPhase _phase;

        public DayPhase Phase => _phase;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (_skyInstance != null)
            {
                if (RenderSettings.skybox == _skyInstance) RenderSettings.skybox = _originalSkybox;
                Destroy(_skyInstance);
                _skyInstance = null;
            }
        }

        private void Reset()
        {
            BuildVisualDefaults();
        }

        private void Start()
        {
            EnsureVisualDefaults();
            InitSky();
            ApplyVisuals(_gameState.minuteOfDay);

            _phase = ResolvePhase(_gameState.minuteOfDay);
            WorldEvents.RaiseDayPhaseChanged(_phase);
        }

        // S-042: 날씨별 안개 배율 — WeatherChanged 구독 (조명·안개는 이 매니저가 단일 소유).
        private float _weatherFogMultiplier = 1f;

        private void OnEnable() => WorldEvents.WeatherChanged += OnWeatherChanged;
        private void OnDisable() => WorldEvents.WeatherChanged -= OnWeatherChanged;

        private void OnWeatherChanged(WeatherType weather)
        {
            _weatherFogMultiplier = weather switch
            {
                WeatherType.Fog => 6f,
                WeatherType.Rain => 2.4f,
                WeatherType.Snow => 1.8f,
                WeatherType.Cloudy => 1.3f,
                _ => 1f
            };
            ApplyVisuals(_gameState.minuteOfDay);
        }

        private void Update()
        {
#if UNITY_EDITOR
            DebugPhaseSkip();
#endif
            _gameState.minuteOfDay += _tuning.gameMinutesPerRealSecond * Time.deltaTime;

            while (_gameState.minuteOfDay >= MINUTES_PER_DAY)
            {
                _gameState.minuteOfDay -= MINUTES_PER_DAY;
                _gameState.day++;
                _lastTickedMinute = -1;
            }

            int minute = Mathf.FloorToInt(_gameState.minuteOfDay);
            if (minute == _lastTickedMinute) return;

            _lastTickedMinute = minute;
            WorldEvents.RaiseClockTicked(BuildClock());
            ApplyVisuals(_gameState.minuteOfDay);

            DayPhase phase = ResolvePhase(_gameState.minuteOfDay);
            if (phase == _phase) return;

            _phase = phase;
            WorldEvents.RaiseDayPhaseChanged(phase);
        }

#if UNITY_EDITOR
        /// <summary>
        /// T = 다음 페이즈 경계로 점프 (에디터 전용 — 낮밤 전환을 기다리지 않고 확인하기 위함).
        /// 하루가 실시간 12분이라 Night 구간만 5분이다. 게임 입력 계약(InputAction)에는 넣지 않는다.
        /// </summary>
        private void DebugPhaseSkip()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            return; // S-068 — 시간 스킵 치트는 릴리스 제외
#endif
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.tKey.wasPressedThisFrame) return;

            int hour = Mathf.FloorToInt(_gameState.minuteOfDay / 60f);
            int[] bounds =
            {
                _tuning.morningStartHour, _tuning.dayStartHour,
                _tuning.eveningStartHour, _tuning.nightStartHour
            };

            int next = bounds[0]; // 어느 경계도 남지 않았으면 다음 날 아침
            foreach (int bound in bounds)
            {
                if (bound <= hour) continue;
                next = bound;
                break;
            }

            SetTime(next, 0);
        }
#endif

        /// <summary>시각을 앞으로 흘린다(이동맵의 "노드 선택 = 시간 소모" — S-006). 시계 소유자는 여기뿐.</summary>
        public void AdvanceMinutes(float minutes)
        {
            _gameState.minuteOfDay = Mathf.Repeat(_gameState.minuteOfDay + minutes, MINUTES_PER_DAY);
            _lastTickedMinute = -1;
        }

        /// <summary>시각을 특정 시:분으로 강제 이동(디버그·연출용).</summary>
        public void SetTime(int hour, int minute)
        {
            _gameState.minuteOfDay = Mathf.Repeat(hour * 60f + minute, MINUTES_PER_DAY);
            _lastTickedMinute = -1;
        }

        private GameClock BuildClock()
        {
            int total = Mathf.FloorToInt(_gameState.minuteOfDay);
            return new GameClock
            {
                Day = _gameState.day,
                Hour = total / 60,
                Minute = total % 60
            };
        }

        // ── 비주얼 구동 ───────────────────────────────────────

        /// <summary>스카이박스가 있으면 런타임 복제본을 만들어 구동(원본 에셋 보호).</summary>
        private void InitSky()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.fog = _fogEnabled;
            RenderSettings.fogMode = FogMode.Linear; // S-145 — 시작 거리를 쓰려면 Linear여야 한다

            Material sky = RenderSettings.skybox;
            if (sky != null && (sky.HasProperty(SkyTintId) || sky.HasProperty(ExposureId)))
            {
                _originalSkybox = sky;
                _skyInstance = new Material(sky);
                RenderSettings.skybox = _skyInstance;
                _driveSkyTint = _skyInstance.HasProperty(SkyTintId);
                _driveExposure = _skyInstance.HasProperty(ExposureId);
                return;
            }

            // 폴백: 스카이박스 없음 → 카메라 배경색 그라디언트.
            if (_backgroundCamera != null) _backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private void ApplyVisuals(float minuteOfDay)
        {
            float t = Mathf.Repeat(minuteOfDay, MINUTES_PER_DAY) / MINUTES_PER_DAY;

            if (_sun != null)
            {
                _sun.color = _sunColor.Evaluate(t);
                _sun.intensity = _sunIntensity.Evaluate(t);
                // 아침 지평선 → 정오 머리 위 → 저녁 지평선 → 밤 지평선 아래.
                _sun.transform.rotation = Quaternion.Euler(t * 360f - 90f, _sunYaw, 0f);
            }

            RenderSettings.ambientLight = _ambientColor.Evaluate(t);

            // S-145 — Linear 안개: 낮밤 커브와 날씨 배수를 버리지 않고 **끝 거리**를 조이는 데 쓴다.
            // 짙을수록(=값이 클수록) 끝이 가까워져 원경이 빨리 묻힌다. 시작 거리는 고정이라
            // 플레이 구간은 어떤 시각·날씨에도 안개를 먹지 않는다.
            RenderSettings.fog = _fogEnabled;
            RenderSettings.fogColor = _fogColor.Evaluate(t);
            // ⚠ `_fogDensity` 커브는 **지수 포그 시절 값**(낮 ~0.004 · 밤 ~0.025)이 그대로 들어
            // 있다. 0~1로 쓰려면 환산이 필요하다 — 안 하면 짙기가 최대 0.15에 그쳐 끝 거리가
            // 사실상 고정된다. 커브를 다시 그리지 않고 여기서 환산해 기존 튜닝을 살린다.
            const float LEGACY_DENSITY_FULL = 0.05f; // 이 값이 짙기 1(=끝 거리 최소)
            float thickness = Mathf.Clamp01(
                _fogDensity.Evaluate(t) * _weatherFogMultiplier / LEGACY_DENSITY_FULL);
            RenderSettings.fogStartDistance = _fogStartDistance;
            RenderSettings.fogEndDistance = Mathf.Max(
                _fogStartDistance + 1f, Mathf.Lerp(_fogEndFar, _fogEndNear, thickness));

            // S-043: 전광판 점등 전역값 — 17~19시 램프업 · 새벽 5~7시 램프다운 (자정~5시 점등 유지).
            float night01;
            if (minuteOfDay >= 17f * 60f) night01 = Mathf.Clamp01((minuteOfDay - 17f * 60f) / 120f);
            else if (minuteOfDay < 7f * 60f) night01 = 1f - Mathf.Clamp01((minuteOfDay - 5f * 60f) / 120f);
            else night01 = 0f;
            Shader.SetGlobalFloat("_DL_SignNight", night01);

            if (_skyInstance != null)
            {
                if (_driveSkyTint) _skyInstance.SetColor(SkyTintId, _skyColor.Evaluate(t));
                if (_driveExposure) _skyInstance.SetFloat(ExposureId, _skyExposure.Evaluate(t));
            }
            else if (_backgroundCamera != null)
            {
                _backgroundCamera.backgroundColor = _skyColor.Evaluate(t);
            }
        }

        private void EnsureVisualDefaults()
        {
            if (_sunColor == null || _sunColor.colorKeys.Length < 2) BuildVisualDefaults();
            if (_fogColor == null || _fogColor.colorKeys.Length < 2) BuildFogDefaults();
        }

        /// <summary>인스펙터 튜닝의 출발점이 되는 기본 곡선·그라디언트. 사람이 덮어쓴다.</summary>
        private void BuildVisualDefaults()
        {
            _sunColor = Gradient(
                (0.00f, "#0a0d16"), (0.25f, "#ff9e6b"), (0.42f, "#fff4e0"),
                (0.50f, "#ffffff"), (0.71f, "#ff9f45"), (0.83f, "#0a0d16"), (1.00f, "#0a0d16"));

            _sunIntensity = new AnimationCurve(
                new Keyframe(0.00f, 0.02f), new Keyframe(0.24f, 0.02f), new Keyframe(0.30f, 0.7f),
                new Keyframe(0.42f, 1.0f), new Keyframe(0.50f, 1.3f), new Keyframe(0.71f, 0.7f),
                new Keyframe(0.80f, 0.15f), new Keyframe(0.86f, 0.02f), new Keyframe(1.00f, 0.02f));

            _ambientColor = Gradient(
                (0.00f, "#0a0d16"), (0.25f, "#6b5a4a"), (0.42f, "#b8bcc4"),
                (0.50f, "#ccd0d8"), (0.71f, "#8a6a4a"), (0.83f, "#12151f"), (1.00f, "#0a0d16"));

            // 밤 하늘: 순검정 대신 어두운 남색~보라 톤(#0f0d1f). 별밭 쿼드의 상단 보라 그라디언트와 조화.
            _skyColor = Gradient(
                (0.00f, "#0f0d1f"), (0.25f, "#e08a5a"), (0.42f, "#a8c8e8"),
                (0.50f, "#9ec4e6"), (0.71f, "#ff9f45"), (0.83f, "#0f0d1f"), (1.00f, "#0f0d1f"));

            // 밤 구간 노출 소폭 상향(0.15→0.25) — 남보라가 검정으로 뭉개지지 않게.
            _skyExposure = new AnimationCurve(
                new Keyframe(0.00f, 0.25f), new Keyframe(0.25f, 0.5f), new Keyframe(0.42f, 1.0f),
                new Keyframe(0.50f, 1.1f), new Keyframe(0.71f, 0.6f), new Keyframe(0.83f, 0.30f),
                new Keyframe(1.00f, 0.25f));

            BuildFogDefaults();
        }

        /// <summary>거리 안개 기본값 — 밤 짙은 남색·고밀도, 낮 옅은 회백·저밀도. 사람이 덮어쓴다.</summary>
        private void BuildFogDefaults()
        {
            // 밤 #0f0d1f(남보라) — 하늘·별밭 상단 톤과 조화. 낮은 옅은 회백으로 원경만 살짝.
            _fogColor = Gradient(
                (0.00f, "#0f0d1f"), (0.25f, "#5a5560"), (0.42f, "#c2c6cc"),
                (0.50f, "#cdd1d7"), (0.71f, "#8a6a5a"), (0.83f, "#1a1626"), (1.00f, "#0f0d1f"));

            // 밤 ~0.012 · 낮 ~0.004 (사람 피드백 2026-07-21 "너무 찐하다" → 절반). exp² 라 원경일수록 잠긴다.
            _fogDensity = new AnimationCurve(
                new Keyframe(0.00f, 0.012f), new Keyframe(0.25f, 0.007f), new Keyframe(0.33f, 0.004f),
                new Keyframe(0.50f, 0.004f), new Keyframe(0.66f, 0.005f), new Keyframe(0.79f, 0.008f),
                new Keyframe(0.86f, 0.012f), new Keyframe(1.00f, 0.012f));
        }

        private static Gradient Gradient(params (float t, string hex)[] stops)
        {
            var g = new Gradient();
            var ck = new GradientColorKey[stops.Length];
            for (int i = 0; i < stops.Length; i++)
            {
                ColorUtility.TryParseHtmlString(stops[i].hex, out Color c);
                ck[i] = new GradientColorKey(c, stops[i].t);
            }
            g.SetKeys(ck, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        private DayPhase ResolvePhase(float minuteOfDay)
        {
            float hour = minuteOfDay / 60f;
            if (hour >= _tuning.nightStartHour || hour < _tuning.morningStartHour) return DayPhase.Night;
            if (hour >= _tuning.eveningStartHour) return DayPhase.Evening;
            if (hour >= _tuning.dayStartHour) return DayPhase.Day;
            return DayPhase.Morning;
        }
    }
}
