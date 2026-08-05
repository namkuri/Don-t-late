using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DontLate
{
    /// <summary>
    /// 날씨 (S-042) — 하루 1회 추첨(맑음·흐림·비·눈·안개·폭염), 파티클(비·눈·아지랑이)·구름·
    /// 컬러 그레이드("LUT" — 날씨×시간대×구역 합성, 수 초 러프)를 소유한다. Core 상주.
    /// 안개 밀도는 WorldDayNightManager가 WeatherChanged를 구독해 협조(조명과 한 몸).
    /// 연출물은 전부 코드 조립 — 씬 재조립만으로 완성(그레이박스 원칙).
    /// </summary>
    public class WorldWeatherManager : MonoBehaviour
    {
        public static WorldWeatherManager Instance { get; private set; }

        [SerializeField] private GameStateSO _gameState;
        [Tooltip("구름 실아트 스왑 슬롯 (S-047 — bom_id: fx_cloud_a/b/c, Art/Backgrounds). 비면 코드 블롭 폴백.")]
        [SerializeField] private Sprite[] _cloudSprites;
        [Tooltip("그레이드 전이 속도 (1/초) — 낮을수록 느긋한 트랜지션.")]
        [SerializeField] private float _gradeLerpSpeed = 0.5f;
        [Tooltip("색보정 수치표 (S-131) — 시간대·날씨·구역별 노출·채도·색온도·필터·블룸. 비면 무보정.")]
        [SerializeField] private ColorGradeSO _grade;

        public WeatherType Weather { get; private set; } = WeatherType.Clear;
        /// <summary>태풍 바람 방향 (S-088 ⑤) — -1(좌) / +1(우) / 0(무풍). Locomotion이 읽는다.
        /// S-122 ⑤ — 실내(Home·Apartment)에선 0: 방 안에서 보행 감속·상자 흔들림이 나지 않는다.</summary>
        public float WindX => _indoorScene ? 0f : _windX;
        private float _windX;
        /// <summary>내일 예보 (S-058 날씨앱) — 오늘 추첨 때 함께 뽑고, 다음 날 그대로 승계된다.</summary>
        public WeatherType TomorrowWeather { get; private set; } = WeatherType.Clear;
        private bool _hasForecast;

        /// <summary>날씨별 대표 기온(°C) — 날씨앱 표시용 (S-058, 그레이박스 근사).</summary>
        public static int TemperatureFor(WeatherType weather) => weather switch
        {
            WeatherType.Heat => 34, WeatherType.Clear => 24, WeatherType.Cloudy => 20,
            WeatherType.Rain => 17, WeatherType.Fog => 14, WeatherType.Snow => -2, _ => 20
        };

        private int _lastRolledDay = -1;
        private ParticleSystem _rain;
        private ParticleSystem _snow;
        private GameObject _hazeRoot; // S-044 ③ — 일렁 셰이더 쿼드 (파티클 박스룩 폐지)
        private Transform _cloudRoot;
        private const float CLOUD_Y_OFFSET = -15f; // S-160 — 구름 높이(남규님 감각값)
        private float _snowAmount;
        private SpriteRenderer[] _clouds;
        private DayPhase _phase = DayPhase.Morning;

        // 런타임 글로벌 볼륨 (그레이드 소유 — 씬 볼륨(블룸)과 별개, 우선순위 높음).
        private ColorAdjustments _colorAdjust;
        private WhiteBalance _whiteBalance;
        private Bloom _bloom; // S-043 — 밤/낮 강도
        private float _targetExposure, _targetSaturation, _targetTemperature, _targetBloom = 0.3f;
        private Color _targetFilter = Color.white;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            WorldEvents.ClockTicked += OnClockTicked;
            WorldEvents.DayPhaseChanged += OnDayPhaseChanged;
            WorldEvents.SceneTransitionCompleted += OnSceneChanged;
        }

        private void OnDisable()
        {
            WorldEvents.ClockTicked -= OnClockTicked;
            WorldEvents.DayPhaseChanged -= OnDayPhaseChanged;
            WorldEvents.SceneTransitionCompleted -= OnSceneChanged;
        }

        private void Start()
        {
            BuildEffects();
            BuildGradeVolume();
            Reroll();
        }

        private void Update()
        {
            // S-045 ③: Y키 = 날씨 순환 (검증·튜닝용). S-067 ⑦ — 릴리스 빌드 자동 제외.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame)
            {
                SetWeather((WeatherType)(((int)Weather + 1) % 7)); // S-088 — Storm 포함
                // S-124 — 디버그 전환은 눈덮임을 즉시 채운다: 평시 누적은 24초라 Y로 눈을 켜고
                // 바로 걸으면 발자국이 안 남아 "고장"으로 보인다(남규님 실관찰). 평시 체감은 불변.
                if (!_indoorScene) _snowAmount = Weather == WeatherType.Snow ? 0.30f : 0f;
            }
#endif

            DriftClouds();
            // S-149 — **매 프레임 표를 다시 읽는다.** 종전엔 날씨·시간대가 바뀔 때만 읽어서,
            // 플레이 중 ColorGrade SO를 만져도 다음 전환이 올 때까지 화면이 그대로였다
            // (남규님 실관찰 "인게임에서 조절하는데 변화가 없다"). 읽는 비용은 구조체 4개
            // 합산이라 무시할 수준이고, 대신 인스펙터 값이 곧바로 화면에 반영된다.
            RefreshGradeTarget();
            LerpGrade();
            UpdateSnowCover();
            TickThunder(); // S-088 ⑥ — 비·태풍 중 가끔 천둥번개
        }

        // S-044 ①: 실내 씬(Home)에선 강수·아지랑이를 창밖 원경(z+)으로 민다.
        private float _sceneZOffset;

        private void LateUpdate()
        {
            // 연출 리그는 카메라 X를 따라간다 (씬·구역 무관).
            Camera camera = Camera.main;
            if (camera == null) return;
            Vector3 cam = camera.transform.position;
            transform.position = new Vector3(cam.x, 0f, _sceneZOffset);
        }

        // ── 추첨 ─────────────────────────────────────────────
        private void OnClockTicked(GameClock clock)
        {
            if (clock.Day == _lastRolledDay) return;
            Reroll();
        }

        private static readonly (WeatherType type, int weight)[] Weights =
        {
            (WeatherType.Clear, 26), (WeatherType.Cloudy, 20), (WeatherType.Rain, 15),
            (WeatherType.Snow, 10), (WeatherType.Fog, 11), (WeatherType.Heat, 11),
            (WeatherType.Storm, 7), // S-088 ⑤
        };

        private void Reroll()
        {
            _lastRolledDay = _gameState != null ? _gameState.day : 0;
            // S-058 — 예보 승계: 어제 예보가 오늘 날씨가 된다 (예보 신뢰). 내일 것은 새로 추첨.
            WeatherType today = _hasForecast ? TomorrowWeather : Draw();
            TomorrowWeather = Draw();
            _hasForecast = true;
            SetWeather(today);
        }

        private WeatherType Draw()
        {
            int total = 0;
            foreach (var w in Weights) total += w.weight;
            int roll = Random.Range(0, total);
            foreach (var w in Weights)
            {
                roll -= w.weight;
                if (roll < 0) return w.type;
            }
            return WeatherType.Clear;
        }

        /// <summary>디버그·튜닝용 강제 설정 (unity-cli exec 검증 포함).</summary>
        public void SetWeather(WeatherType weather)
        {
            Weather = weather;
            ApplyWeatherVisuals();
            RefreshGradeTarget();
            WorldEvents.RaiseWeatherChanged(weather);
        }

        // ── 파티클·구름 ──────────────────────────────────────
        private void ApplyWeatherVisuals()
        {
            // S-122 ⑤ — 실내(Home·Apartment)엔 강수·아지랑이·바람 리본·안개뭉치가 없다.
            // 발생 박스가 90u(±45)라 z 오프셋으로는 방 안 발생을 못 막는다 — 소스를 끈다.
            bool outdoor = !_indoorScene;
            // S-124 — 실내라고 강수를 아예 끄면 "비 오는 날인데 창밖도 맑다"가 된다(남규님 반려).
            // Home은 끄지 않고 발생 박스를 창 너머 크기로 줄여 **창문으로만 보이게** 한다.
            ApplyFallVolume();
            ClearAirborneIfIndoor(); // S-128 ① — 반드시 Toggle 앞에서: 실외에서 데려온 잔류 입자를 먼저 비운다
            bool fallOk = outdoor || _windowScene;
            Toggle(_rain, fallOk && Weather == WeatherType.Rain); // Storm의 간헐 비는 StormRainCycle 코루틴이 굴린다
            Toggle(_snow, fallOk && Weather == WeatherType.Snow);
            // S-090 ① — 태풍 진입: 이전 날씨의 눈 입자를 즉시 걷는다 (Stop만으론 수명만큼 잔류).
            if (Weather == WeatherType.Storm && _snow != null)
                _snow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_hazeRoot != null) _hazeRoot.SetActive(outdoor && Weather == WeatherType.Heat);

            // S-088 ⑤ — 태풍: 바람 방향 추첨(좌/우) + 간헐 비 사이클.
            if (Weather == WeatherType.Storm)
            {
                _windX = Random.value < 0.5f ? -1f : 1f;
                if (_stormRoutine == null) _stormRoutine = StartCoroutine(StormRainCycle());
            }
            else
            {
                _windX = 0f;
                if (_stormRoutine != null) { StopCoroutine(_stormRoutine); _stormRoutine = null; }
            }
            ApplyWindToFall(_rain);   // S-089 ① — 강수 기울기 (무풍이면 0)
            ApplyWindToFall(_snow);
            ToggleWindStreaks(outdoor && Weather == WeatherType.Storm); // S-089 ④ — 공중 바람 줄기
            ToggleFogBanks(outdoor && Weather == WeatherType.Fog);      // S-091 ② — 안개 뭉치 층

            int cloudCount = Weather switch
            {
                WeatherType.Clear => 1,
                WeatherType.Heat => 0,
                WeatherType.Cloudy => 7,
                WeatherType.Rain => 8,
                WeatherType.Snow => 6,
                WeatherType.Fog => 4,
                WeatherType.Storm => 8, // S-088 ⑤ — 먹구름 가득
                _ => 3
            };
            Color cloudColor = Weather == WeatherType.Rain || Weather == WeatherType.Storm
                ? new Color(0.30f, 0.31f, 0.36f, 0.92f)   // 먹구름
                : new Color(0.92f, 0.93f, 0.96f, 0.82f);
            for (int i = 0; i < _clouds.Length; i++)
            {
                _clouds[i].gameObject.SetActive(i < cloudCount);
                _clouds[i].color = cloudColor * new Color(1f, 1f, 1f, 0.6f + 0.4f * ((i * 37) % 10) / 10f);
            }
        }

        private static void Toggle(ParticleSystem system, bool on)
        {
            if (system == null) return;
            if (on && !system.isPlaying) system.Play();
            else if (!on && system.isPlaying) system.Stop();
        }

        private void DriftClouds()
        {
            if (_cloudRoot == null) return;
            for (int i = 0; i < _clouds.Length; i++)
            {
                if (!_clouds[i].gameObject.activeSelf) continue;
                Transform cloud = _clouds[i].transform;
                float speed = 0.35f + 0.1f * (i % 3);
                Vector3 pos = cloud.localPosition + Vector3.right * (speed * Time.deltaTime);
                if (pos.x > 45f) pos.x = -45f; // 랩
                cloud.localPosition = pos;
            }
        }

        // ── 그레이드 ("LUT" — 날씨×시간대×구역) ─────────────
        private void OnDayPhaseChanged(DayPhase phase)
        {
            _phase = phase;
            RefreshGradeTarget();
        }

        private void OnSceneChanged(GameScene scene)
        {
            _sceneZOffset = scene == GameScene.Home ? 10f : 0f; // S-044 ① — 방 뒷벽(z3) 너머 창밖
            _indoorScene = scene == GameScene.Apartment || scene == GameScene.Home; // S-050 ④·S-053 ② — 실내엔 눈 안 쌓임
            // S-124 — Home은 창이 있는 실내: 강수를 끄지 않고 창밖으로만 보낸다.
            // Apartment(반옥외 복도)는 창 개구부가 없어 현행 차단 유지.
            _windowScene = scene == GameScene.Home;
            // S-122 ⑤ — 실내/실외 전환을 즉시 반영. ApplyWeatherVisuals 전체를 부르면 태풍 바람 방향이
            // 씬 전환마다 재추첨되고 StormRainCycle 소유권이 흔들리므로, 게이트만 다시 적용한다.
            // _clouds 가드 = 씬 단독 Play: CoreBootstrap의 도착 통지가 이 매니저의 Start보다 먼저 돌 수 있다.
            if (_clouds != null) ApplyIndoorGate();
            RefreshGradeTarget();
        }

        // S-122 ⑤ — 실내 게이트만 재적용 (구름 개수·WindX 추첨·StormRainCycle 소유권 불가침).
        private void ApplyIndoorGate()
        {
            bool outdoor = !_indoorScene;
            if (_hazeRoot != null) _hazeRoot.SetActive(outdoor && Weather == WeatherType.Heat);
            ToggleWindStreaks(outdoor && Weather == WeatherType.Storm);
            ToggleFogBanks(outdoor && Weather == WeatherType.Fog);
            ApplyFallVolume();
            ClearAirborneIfIndoor(); // S-128 ① — 창밖 모드도 한 번 비운다(잔류 입자가 방 좌표에 남는다)
            if (_indoorScene && !_windowScene) return;
            // Storm의 비는 StormRainCycle 몫이라 평소엔 여기서 켜지 않는다. 단 창밖 모드는 위에서 방금
            // 비웠으므로 한 번 켜준다 — 안 그러면 사이클이 다음 '내림' 구간까지 최대 30초 창밖이 마른다.
            Toggle(_rain, Weather == WeatherType.Rain || (_windowScene && Weather == WeatherType.Storm));
            Toggle(_snow, Weather == WeatherType.Snow);
        }

        // S-124 — 강수 발생 부피: 실외는 씬 전체(90u), Home은 창밖 원경만.
        // ⚠ S-128 ① — shape.scale은 **이미터 로컬 축** 기준이고, 이미터는 낙하 방향으로 회전해 있다
        // (BuildFallSystem의 LookRotation). 그래서 기저가 비·눈에서 다르다:
        //   비(tilt 15°)  로컬X→월드−Z ⇒ (16,24,6)이 월드 z 3.5~19.5 (뒷벽 z=3 바깥) — 정상
        //   눈(tilt 0°)   forward가 up과 반평행이라 LookRotation이 축퇴 → Euler(90,0,0) 폴백,
        //                 로컬Y→월드+Z · 로컬Z→월드−Y ⇒ 같은 (16,24,6)이 월드 z −0.5~23.5가 되어
        //                 방(z −3~3)을 3.5u 침범했다. "비는 안 들어오는데 눈은 들어온다"의 절반.
        // 그래서 창밖 박스는 **시스템별로 로컬 축에 맞춰** 적는다 (월드 결과는 둘 다 창 너머).
        private static readonly Vector3 OutdoorFallBox = new Vector3(90f, 30f, 90f);
        private static readonly Vector3 WindowFallBoxRain = new Vector3(16f, 24f, 6f); // S-124 캡처 검증분 — 무수정
        private static readonly Vector3 WindowFallBoxSnow = new Vector3(16f, 6f, 24f); // 로컬Y=깊이6·로컬Z=높이24 ⇒ 월드 z 8.5~14.5

        private void ApplyFallVolume()
        {
            SetFallBox(_rain, _windowScene ? WindowFallBoxRain : OutdoorFallBox);
            SetFallBox(_snow, _windowScene ? WindowFallBoxSnow : OutdoorFallBox);
        }

        private static void SetFallBox(ParticleSystem system, Vector3 box)
        {
            if (system == null) return;
            var shape = system.shape;
            if (shape.scale != box) shape.scale = box;
        }

        // 실내 진입: 체공 중인 빗줄기(2.2s)·눈송이(12s)·퇴적(50s·World 공간)까지 그 자리에서 지운다.
        // S-128 ① — 창밖 모드(Home)도 포함한다. 입자가 World 공간이라 씬 전환으로 사라지지 않아,
        // 실외에서 데려온 눈·퇴적이 방 좌표에 그대로 떠 있던 것이 눈에 보이던 직접 원인이었다.
        // 소거 뒤 호출자가 창밖 박스로 다시 Play한다 — **소거가 Toggle보다 앞**인 것이 계약이다.
        private void ClearAirborneIfIndoor()
        {
            if (!_indoorScene) return;
            if (_rain != null) _rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_snow != null) _snow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_snowPile != null) _snowPile.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private ParticleSystem _snowPile; // S-122 ⑤ — 실내 진입 시 퇴적 입자까지 걷기 위한 참조
        private bool _windowScene;        // S-124 — 창밖 강수만 허용하는 실내(Home)
        private bool _indoorScene; // S-050 ④ — 아파트 실내: SnowCover·발자국(HasSnowCover) 억제
        private bool _snowCovered; // AU-018 ③ — 기존 HasSnowCover 전환 감지용(SnowCoverChanged 발행)

        // S-131 — 수치는 전부 ColorGradeSO(인스펙터)로 나갔다. 여기는 합성 규칙만 소유한다:
        //   노출·채도·색온도·블룸 = 시간대 + 날씨 + 구역 (가산) / 컬러필터 = 셋의 곱 (흰색이 무변화).
        // 조명 자체는 DayNight 몫 — 여기는 필름 톤만 얹는다.
        private void RefreshGradeTarget()
        {
            if (_grade == null) return; // SO 미주입 — 현 상태 유지(무보정으로 시작)

            ColorGradeSO.Layer phase = _grade.ForPhase(_phase);
            ColorGradeSO.Layer weather = _grade.ForWeather(Weather);
            ColorGradeSO.Layer district = _grade.ForDistrict(_gameState != null ? _gameState.currentDistrict : null);

            // S-145 — `baseGrade`는 상황과 무관하게 **항상** 더해지는 기준선이다(남규님 요구).
            // 곱셈인 필터도 같은 규칙: 기본 필터가 흰색이면 무영향, 색을 주면 전 상황에 배어든다.
            ColorGradeSO.Layer basis = _grade.baseGrade;

            _targetExposure = basis.exposure + phase.exposure + weather.exposure + district.exposure;
            _targetSaturation = basis.saturation + phase.saturation + weather.saturation + district.saturation;
            _targetTemperature = basis.temperature + phase.temperature + weather.temperature + district.temperature;
            _targetFilter = basis.SafeFilter * phase.SafeFilter * weather.SafeFilter * district.SafeFilter;
            _targetBloom = basis.bloom + phase.bloom + weather.bloom + district.bloom;
        }

        private void LerpGrade()
        {
            if (_colorAdjust == null) return;
            float t = Time.deltaTime * _gradeLerpSpeed;
            _colorAdjust.postExposure.value = Mathf.Lerp(_colorAdjust.postExposure.value, _targetExposure, t);
            _colorAdjust.saturation.value = Mathf.Lerp(_colorAdjust.saturation.value, _targetSaturation, t);
            _colorAdjust.colorFilter.value = Color.Lerp(_colorAdjust.colorFilter.value, _targetFilter, t);
            _whiteBalance.temperature.value = Mathf.Lerp(_whiteBalance.temperature.value, _targetTemperature, t);
            _bloom.intensity.value = Mathf.Lerp(_bloom.intensity.value, _targetBloom, t);
        }

        private void BuildGradeVolume()
        {
            GameObject go = new GameObject("WeatherGradeVolume");
            go.transform.SetParent(transform, false);
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 50f; // 씬 블룸 볼륨보다 위

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>(); // 런타임 전용 — 에셋 무오염
            _colorAdjust = profile.Add<ColorAdjustments>();
            _colorAdjust.postExposure.overrideState = true;
            _colorAdjust.saturation.overrideState = true;
            _colorAdjust.colorFilter.overrideState = true;
            _whiteBalance = profile.Add<WhiteBalance>();
            _whiteBalance.temperature.overrideState = true;
            _bloom = profile.Add<Bloom>(); // S-043 — 밤 간판 발광 증폭, 낮 절제
            _bloom.intensity.overrideState = true;
            _bloom.threshold.overrideState = true;
            _bloom.threshold.value = 0.9f;
            volume.profile = profile;
        }

        // ── 연출물 조립 (코드 — 그레이박스) ──────────────────
        private void BuildEffects()
        {
            _rain = BuildFallSystem("RainEmitter", new Color(0.62f, 0.72f, 0.92f, 0.55f),
                startSpeed: 26f, size: 0.05f, lengthScale: 6f, rate: 340f, gravity: 1.2f,
                tiltDegrees: 15f); // 아트 피드백 (2026-07-24) — 수직 낙하는 부자연, 15° 사선
            ConfigureRainSplash(_rain); // S-044 ② — 충돌 지점 물 튀김
            _snow = BuildFallSystem("SnowEmitter", new Color(0.98f, 0.98f, 1f, 0.9f),
                startSpeed: 1.6f, size: 0.09f, lengthScale: 1f, rate: 120f, gravity: 0.06f, noise: true,
                lifetime: 12f); // S-046 ① — 14u 상공에서 지면까지 (2.2s는 공중 소멸)
            ConfigureSnowPile(_snow);   // S-046 ③ — 낙하 지점 실누적 (반드시 _snow 생성 후)
            _hazeRoot = BuildHazeQuads();
            BuildClouds();
        }

        private ParticleSystem BuildFallSystem(string name, Color color, float startSpeed,
            float size, float lengthScale, float rate, float gravity, bool noise = false, float tiltDegrees = 0f, float lifetime = 2.2f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 14f, 1.5f);
            // 낙하 방향 — 기울기(도)만큼 X로 사선 (스트레치 렌더가 속도 정렬이라 빗줄기도 같이 기운다).
            float tilt = tiltDegrees * Mathf.Deg2Rad;
            go.transform.localRotation = Quaternion.LookRotation(
                new Vector3(Mathf.Sin(tilt), -Mathf.Cos(tilt), 0f));

            ParticleSystem system = go.AddComponent<ParticleSystem>();
            var main = system.main;
            main.startSpeed = startSpeed;
            main.startSize = size;
            main.startLifetime = lifetime;
            main.startColor = color;
            main.maxParticles = 3200; // S-046 ② 영역 확대분(눈 12s 체공 포함)
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            // ⚠ 이미터 로컬 축은 월드와 다르다 (로컬 Y=월드 Z · 로컬 Z=월드 Y — S-128 ① 실측).
            // 실외 기본값 = 월드 90 가로 × 90 높이 × 30 깊이. 씬별 갱신은 ApplyFallVolume가 한다.
            shape.scale = new Vector3(90f, 30f, 90f);

            var emission = system.emission;
            emission.rateOverTime = rate;

            if (noise)
            {
                var noiseModule = system.noise;
                noiseModule.enabled = true;
                noiseModule.strength = 0.55f;
                noiseModule.frequency = 0.4f;
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = MakeParticleMaterial(color);
            if (lengthScale > 1.01f)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = lengthScale; // 빗줄기
            }
            system.Stop();
            return system;
        }

        // S-128 ③ — 파티클 충돌은 Ground 레이어(10)만 본다. 기본값(Everything)이면 WalkableVolume 같은
        // 트리거 부피와 소품 콜라이더 윗면에도 부딪혀 눈이 허공에 쌓인다(남규님 지적). 레이어가 없는
        // 프로젝트(구 씬·테스트)에서는 종전대로 전부 충돌시킨다.
        private static int GroundMask()
        {
            int layer = LayerMask.NameToLayer("Ground");
            return layer >= 0 ? 1 << layer : ~0;
        }

        // 눈 실누적 (S-046 ③) — 눈송이가 닿은 지점에 퇴적 입자가 남는다 (균일 커버는 보조 톤으로 강등).
        private void ConfigureSnowPile(ParticleSystem snow)
        {
            var collision = snow.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.bounce = 0f;
            collision.lifetimeLoss = 1f;
            collision.quality = ParticleSystemCollisionQuality.High; // S-047 ① — Medium 근사 평면이 부유 퇴적의 원흉
            collision.collidesWith = GroundMask(); // S-128 ③

            GameObject pileGo = new GameObject("SnowPile");
            pileGo.transform.SetParent(snow.transform, false);
            ParticleSystem pile = pileGo.AddComponent<ParticleSystem>();
            var main = pile.main;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.20f);
            main.startLifetime = 50f; // 퇴적 잔류 — 이후 서서히 소멸(녹음)
            main.startColor = new Color(0.96f, 0.97f, 1f, 0.85f);
            main.gravityModifier = 0f;
            main.maxParticles = 4000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = pile.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var fade = pile.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0.85f, 0.8f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var pileRenderer = pileGo.GetComponent<ParticleSystemRenderer>();
            pileRenderer.material = MakeParticleMaterial(new Color(0.96f, 0.97f, 1f, 0.85f));
            pileRenderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard; // S-047 ① — 하늘 보기(바닥에 눕는다)
            pile.Stop();
            _snowPile = pile; // S-122 ⑤ — 실내 진입 시 소거 대상

            var subEmitters = snow.subEmitters;
            subEmitters.enabled = true;
            subEmitters.AddSubEmitter(pile, ParticleSystemSubEmitterType.Collision,
                ParticleSystemSubEmitterProperties.InheritNothing);
        }

        private void UpdateSnowCover()
        {
            if (_indoorScene) _snowAmount = 0f; // S-050 ④ — 실내 진입 즉시 걷힘 (HasSnowCover도 false → 발자국 없음)
            float target = !_indoorScene && Weather == WeatherType.Snow ? 0.30f : 0f;
            float rate = Weather == WeatherType.Snow ? 0.030f : 0.018f; // 쌓임은 느긋(~24s), 녹음은 더 느긋
            _snowAmount = Mathf.MoveTowards(_snowAmount, target, rate * Time.deltaTime);

            bool covered = HasSnowCover; // AU-018 ③ — 기존 게이트(>0.25) 전환 시 발소리 스왑 통지
            if (covered != _snowCovered) { _snowCovered = covered; WorldEvents.RaiseSnowCoverChanged(covered); }

            // S-053 ⑤ — 평면 quad 폐기: 그레이박스 스노 셰이더 전역 파라미터로 모든 윗면에 쌓인다
            // (경사·상자·지붕 포함 — "눈 안 쌓이는 곳" 소멸). 실퇴적 파티클은 그대로 주역.
            Shader.SetGlobalFloat("_DL_SnowMix", Mathf.Clamp01(_snowAmount / 0.30f));
        }

        /// <summary>플레이어 발자국용 — 지금 눈이 쌓여 있는가 (PlayerEffects가 WeatherChanged와 함께 사용).</summary>
        public bool HasSnowCover => _snowAmount > 0.25f;

        // ── S-089 ① — 강수 바람 기울기: velocityOverLifetime.x = WindX × 세기 ──
        private void ApplyWindToFall(ParticleSystem fall)
        {
            if (fall == null) return;
            var velocity = fall.velocityOverLifetime;
            velocity.enabled = WindX != 0f;
            velocity.space = ParticleSystemSimulationSpace.World;
            // S-090 ② — 세 축이 같은 커브 모드여야 한다 (x만 TwoConstants면 에러 다량).
            velocity.x = new ParticleSystem.MinMaxCurve(WindX * 7f, WindX * 10f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }

        // ── S-092 — 윈드워커식 카툰 바람 리본 (참조: Zelda Wind Waker 스타일) ──
        // 보이지 않는 헤드가 바람 방향으로 사인 요동하며 날다 원형 고리를 1~2회 말고 지나간다.
        // TrailRenderer가 얇아지는 흰 리본으로 곡선을 남긴다. 태풍 시만, 간헐 스폰.

        private Coroutine _ribbonSpawner;
        private readonly System.Collections.Generic.List<GameObject> _windRibbons
            = new System.Collections.Generic.List<GameObject>();
        private Material _ribbonMaterial;

        private void ToggleWindStreaks(bool on)
        {
            if (on && _ribbonSpawner == null)
                _ribbonSpawner = StartCoroutine(SpawnWindRibbons());
            if (!on && _ribbonSpawner != null)
            {
                StopCoroutine(_ribbonSpawner);
                _ribbonSpawner = null;
                foreach (GameObject ribbon in _windRibbons) if (ribbon != null) Destroy(ribbon);
                _windRibbons.Clear();
            }
        }

        private System.Collections.IEnumerator SpawnWindRibbons()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(0.6f, 1.8f));
                _windRibbons.RemoveAll(r => r == null);
                if (_windRibbons.Count < 5) StartCoroutine(WindRibbonFly());
            }
        }

        private System.Collections.IEnumerator WindRibbonFly()
        {
            Camera camera = Camera.main;
            if (camera == null) yield break;

            GameObject head = new GameObject("WindRibbon");
            _windRibbons.Add(head);
            float direction = WindX == 0f ? 1f : WindX;
            float baseY = Random.Range(1.6f, 6.5f);
            head.transform.position = new Vector3(
                camera.transform.position.x - direction * 26f, baseY, Random.Range(-1f, 3f));

            if (_ribbonMaterial == null)
            {
                _ribbonMaterial = new Material(Shader.Find("Sprites/Default"));
            }
            TrailRenderer trail = head.AddComponent<TrailRenderer>();
            trail.material = _ribbonMaterial;
            trail.time = 0.85f;
            trail.minVertexDistance = 0.05f;
            trail.numCapVertices = 4;
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.02f), new Keyframe(0.25f, 0.13f), new Keyframe(1f, 0.015f));
            trail.startColor = new Color(1f, 1f, 1f, 0.55f);
            trail.endColor = new Color(1f, 1f, 1f, 0f);

            float speed = Random.Range(9f, 14f);
            float wobbleAmp = Random.Range(0.35f, 0.7f);
            float wobbleFreq = Random.Range(0.9f, 1.5f);
            int loopsLeft = Random.Range(1, 3);           // 시그니처 고리 1~2회
            float nextLoopAt = Random.Range(1.1f, 2.2f);  // 다음 고리까지 비행 시간
            float loopRadius = Random.Range(0.45f, 0.85f);

            float flightTime = 0f;
            const float TOTAL = 5.2f;
            while (flightTime < TOTAL && head != null)
            {
                flightTime += Time.deltaTime;
                nextLoopAt -= Time.deltaTime;

                if (loopsLeft > 0 && nextLoopAt <= 0f)
                {
                    // 원형 고리 — 전진을 늦추고 한 바퀴 말아 돈다 (윈드워커 시그니처).
                    loopsLeft--;
                    nextLoopAt = Random.Range(1.2f, 2.0f);
                    Vector3 anchor = head.transform.position;
                    float theta = 0f;
                    const float LOOP_SECONDS = 0.55f;
                    while (theta < Mathf.PI * 2f && head != null)
                    {
                        float dtLoop = Time.deltaTime;
                        theta += dtLoop * (Mathf.PI * 2f / LOOP_SECONDS);
                        float clamped = Mathf.Min(theta, Mathf.PI * 2f);
                        anchor.x += direction * speed * 0.22f * dtLoop; // 고리 도는 동안도 살짝 전진
                        head.transform.position = anchor + new Vector3(
                            direction * loopRadius * Mathf.Sin(clamped),
                            loopRadius * (1f - Mathf.Cos(clamped)), 0f);
                        yield return null;
                    }
                    continue;
                }

                Vector3 position = head.transform.position;
                position.x += direction * speed * Time.deltaTime;
                position.y = baseY + Mathf.Sin(flightTime * wobbleFreq * Mathf.PI) * wobbleAmp;
                head.transform.position = position;
                yield return null;
            }

            if (head != null)
            {
                trail.emitting = false;
                yield return new WaitForSeconds(0.9f); // 꼬리가 다 사라질 때까지
                Destroy(head);
            }
        }

        // ── S-091 ② — 안개 뭉치: 거리 포그만이던 Fog 날씨에 실체 — 대형 저알파 블롭이
        //    저층을 느리게 배회한다 (구름 블롭 스프라이트 재사용, 2겹 층감) ──
        private ParticleSystem _fogBanks;

        private void ToggleFogBanks(bool on)
        {
            if (on && _fogBanks == null)
            {
                GameObject go = new GameObject("FogBanks");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 0.6f, 5f); // 씬 뒤편 — 화이트아웃 방지
                _fogBanks = go.AddComponent<ParticleSystem>();
                var main = _fogBanks.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(14f, 24f);
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(3.5f, 7f);
                main.startColor = new Color(0.90f, 0.92f, 0.96f, 0.07f); // 저알파 — 겹쳐도 은은
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.maxParticles = 22;
                main.prewarm = true;
                main.loop = true;
                var emission = _fogBanks.emission;
                emission.rateOverTime = 1.1f;
                var shape = _fogBanks.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(52f, 1.8f, 6f); // 저층 넓게
                var velocity = _fogBanks.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.World;
                velocity.x = new ParticleSystem.MinMaxCurve(0.25f, 0.6f); // 느린 드리프트
                velocity.y = new ParticleSystem.MinMaxCurve(0f, 0.04f);
                velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                // 스르륵 짙어졌다 옅어지는 뭉치.
                var colorLife = _fogBanks.colorOverLifetime;
                colorLife.enabled = true;
                Gradient fogFade = new Gradient();
                fogFade.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f),
                            new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
                colorLife.color = fogFade;
                // 미세 요동 — 안개가 살아 있는 느낌.
                var noise = _fogBanks.noise;
                noise.enabled = true;
                noise.strength = new ParticleSystem.MinMaxCurve(0.35f);
                noise.frequency = 0.08f;
                noise.scrollSpeed = 0.12f;
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                // S-091b — 네모 플레인 수리 2차: URP 파티클 셰이더는 런타임 블렌드 키워드가 안 잡혀
                // 불투명 판이 됐다 — Sprites/Default(알파 블렌드·_MainTex 기본)로 블롭을 그린다.
                Material fogMaterial = new Material(Shader.Find("Sprites/Default"));
                fogMaterial.mainTexture = MakeCloudSprite().texture;
                renderer.material = fogMaterial;
                renderer.sortMode = ParticleSystemSortMode.Distance;
            }
            if (_fogBanks == null) return;
            if (on) _fogBanks.Play();
            else _fogBanks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // ── S-088 ⑤ — 태풍 간헐 비: 비가 왔다 그쳤다 한다 ──
        private Coroutine _stormRoutine;

        private System.Collections.IEnumerator StormRainCycle()
        {
            while (Weather == WeatherType.Storm)
            {
                Toggle(_rain, !_indoorScene || _windowScene); // S-122 ⑤ · S-124 창밖 모드는 허용
                yield return new WaitForSeconds(Random.Range(8f, 18f));
                if (Weather != WeatherType.Storm) break;
                Toggle(_rain, false);
                yield return new WaitForSeconds(Random.Range(6f, 14f));
            }
            Toggle(_rain, (!_indoorScene || _windowScene) && Weather == WeatherType.Rain);
            _stormRoutine = null;
        }

        // ── S-088 ⑥ — 천둥번개: 비·태풍 중 랜덤 간격 섬광 2연발 + 천둥 SFX ──
        private float _thunderTimer = 25f;
        private UnityEngine.UI.Image _flashOverlay;

        private void TickThunder()
        {
            bool stormy = Weather == WeatherType.Rain || Weather == WeatherType.Storm;
            if (!stormy) return;
            _thunderTimer -= Time.deltaTime;
            if (_thunderTimer > 0f) return;
            _thunderTimer = Random.Range(18f, 45f);
            StartCoroutine(ThunderFlash());
        }

        private System.Collections.IEnumerator ThunderFlash()
        {
            if (_flashOverlay == null)
            {
                GameObject canvasGo = new GameObject("ThunderCanvas");
                canvasGo.transform.SetParent(transform, false);
                Canvas canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 90;
                _flashOverlay = new GameObject("Flash", typeof(RectTransform)).AddComponent<UnityEngine.UI.Image>();
                _flashOverlay.transform.SetParent(canvasGo.transform, false);
                _flashOverlay.raycastTarget = false;
                RectTransform rect = _flashOverlay.rectTransform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                _flashOverlay.color = new Color(1f, 1f, 1f, 0f);
            }

            WorldAudioManager.Instance?.PlayThunderSfx(); // 소켓 비면 무음 (AU-022)
            // 섬광 2연발 — 번쩍, 짧게 죽었다 다시 번쩍 후 감쇠.
            foreach (float peak in new[] { 0.55f, 0.8f })
            {
                _flashOverlay.color = new Color(1f, 1f, 1f, peak);
                yield return new WaitForSeconds(0.06f);
                float alpha = peak;
                while (alpha > 0.02f)
                {
                    alpha = Mathf.MoveTowards(alpha, 0f, Time.deltaTime * 3.2f);
                    _flashOverlay.color = new Color(1f, 1f, 1f, alpha);
                    yield return null;
                }
            }
            _flashOverlay.color = new Color(1f, 1f, 1f, 0f);
        }

        // 비 스플래시 (S-044 ②) — 빗방울이 월드 콜라이더에 닿으면 소멸 + 자잘한 물방울 튐.
        private void ConfigureRainSplash(ParticleSystem rain)
        {
            var collision = rain.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.bounce = 0f;
            collision.lifetimeLoss = 1f; // 닿는 순간 소멸 → 스플래시로 교대
            collision.quality = ParticleSystemCollisionQuality.Medium;
            collision.collidesWith = GroundMask(); // S-128 ③ — 지면에서만 튄다

            // 스플래시 서브 시스템 — 위로 톡 튀는 물방울 3~4개.
            GameObject splashGo = new GameObject("RainSplash");
            splashGo.transform.SetParent(rain.transform, false);
            ParticleSystem splash = splashGo.AddComponent<ParticleSystem>();
            var main = splash.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.2f); // S-046 ④ — 더 높이
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.03f); // S-045 ② — 절반
            main.startLifetime = 2f; // S-046 ④ — 남규님 지정
            main.startColor = new Color(0.75f, 0.84f, 1f, 0.75f);
            main.gravityModifier = 1.6f;
            main.maxParticles = 900;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = splash.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3, 4, 1, 0.01f) });

            var shape = splash.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.04f;

            var splashFade = splash.colorOverLifetime;
            splashFade.enabled = true;
            Gradient splashGradient = new Gradient();
            splashGradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0.5f, 0.4f), new GradientAlphaKey(0f, 0.75f) });
            splashFade.color = splashGradient;

            splashGo.GetComponent<ParticleSystemRenderer>().material
                = MakeParticleMaterial(new Color(0.75f, 0.84f, 1f, 0.75f));
            splash.Stop();

            var subEmitters = rain.subEmitters;
            subEmitters.enabled = true;
            subEmitters.AddSubEmitter(splash, ParticleSystemSubEmitterType.Collision,
                ParticleSystemSubEmitterProperties.InheritNothing);
        }

        // 아지랑이 (S-044 ③) — HeatHaze 셰이더 쿼드 2겹: 정점 일렁임+상승 노이즈.
        private GameObject BuildHazeQuads()
        {
            GameObject root = new GameObject("HeatHaze");
            root.transform.SetParent(transform, false);

            Shader shader = Shader.Find("DontLate/HeatHaze");
            Material material = shader != null ? new Material(shader) : null;

            for (int i = 0; i < 2; i++)
            {
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "HazeQuad_" + i;
                Object.Destroy(quad.GetComponent<Collider>());
                quad.transform.SetParent(root.transform, false);
                quad.transform.localPosition = new Vector3(0f, 1.15f + i * 0.4f, 0.6f + i * 1.2f);
                quad.transform.localScale = new Vector3(44f, 2.3f, 1f);
                if (material != null) quad.GetComponent<Renderer>().sharedMaterial = material;
            }
            root.SetActive(false);
            return root;
        }

        private void BuildClouds()
        {
            _cloudRoot = new GameObject("Clouds").transform;
            _cloudRoot.SetParent(transform, false);
            // S-160 — 구름을 Y −15 내린다(남규님 지시). 매니저는 Core 상주라 여기서 한 번 내리면
            // 전 씬에 함께 적용된다 — 씬마다 따로 손볼 곳이 없다.
            _cloudRoot.localPosition = new Vector3(0f, CLOUD_Y_OFFSET, 0f);

            Sprite blob = MakeCloudSprite();
            _clouds = new SpriteRenderer[8];
            for (int i = 0; i < _clouds.Length; i++)
            {
                Sprite sprite = _cloudSprites != null && _cloudSprites.Length > 0
                    ? _cloudSprites[i % _cloudSprites.Length] : blob; // 실아트 로테이션
                GameObject cloud = new GameObject("Cloud_" + i);
                cloud.transform.SetParent(_cloudRoot, false);
                cloud.transform.localPosition = new Vector3(-40f + i * 11f, 21f + (i * 13 % 7), 64f + (i % 3) * 3f);
                cloud.transform.localScale = new Vector3(9f + (i * 7 % 5), 3.2f + (i * 5 % 3), 1f);
                SpriteRenderer renderer = cloud.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                cloud.SetActive(false);
                _clouds[i] = renderer;
            }
        }

        private static Material MakeParticleMaterial(Color tint)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            material.SetFloat("_Surface", 1f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            return material;
        }

        private static Sprite _cloudSpriteCache;

        private static Sprite MakeCloudSprite()
        {
            if (_cloudSpriteCache != null) return _cloudSpriteCache;
            const int W = 64, H = 32;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    // 타원 3개 겹친 소프트 블롭.
                    float a = BlobAlpha(x, y, 20f, 18f, 16f, 9f)
                            + BlobAlpha(x, y, 36f, 14f, 14f, 8f)
                            + BlobAlpha(x, y, 46f, 19f, 12f, 7f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                }
            tex.Apply();
            _cloudSpriteCache = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 16f);
            return _cloudSpriteCache;
        }

        private static float BlobAlpha(int x, int y, float cx, float cy, float rx, float ry)
        {
            float dx = (x - cx) / rx;
            float dy = (y - cy) / ry;
            float d = dx * dx + dy * dy;
            return Mathf.Clamp01(1.15f - d);
        }
    }
}
