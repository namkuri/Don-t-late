using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// S-139 — 타이틀(Main) 배경을 "살아 있는 거리"로 만드는 구동부.
    /// 배경은 District 배치를 그대로 쓰고(ArtBackdropKit), 그 위에서 배달원이 좌우로 달리며
    /// 시간대·날씨가 순환한다. 첫 화면이 게임의 인상을 결정한다(ARCHITECTURE §3 — 낮밤 전환이
    /// 이 프로젝트의 최대 배당이므로 타이틀에서 미리 보여준다).
    ///
    /// **플레이어가 아니다** — 입력도 물리도 없는 연출 인형이다. 그래서 PlayerManager 계열을
    /// 붙이지 않고 여기서 위치·Animator만 직접 흔든다(도메인 침범 아님).
    ///
    /// World 매니저는 `Instance`로 **명령만** 부른다(SetTime·SetWeather) — 규칙 §4 허용 범위.
    /// 상태를 되읽지 않으므로 이벤트 구독이 필요 없다.
    /// </summary>
    public class TitleShowcaseDirector : MonoBehaviour
    {
        [Header("달리는 배달원")]
        [SerializeField] private Transform _runner;
        [SerializeField] private Animator _runnerAnimator;
        [Tooltip("왕복 구간 X 좌우 끝")]
        [SerializeField] private float _leftX = -13f;
        [SerializeField] private float _rightX = 13f;
        [SerializeField] private float _runSpeed = 4.2f;
        [Tooltip("끝에서 돌아설 때 멈칫하는 시간(초)")]
        [SerializeField] private float _turnPause = 0.35f;

        [Header("순환")]
        [Tooltip("시간대 1칸 머무는 시간(초)")]
        [SerializeField] private float _phaseSeconds = 9f;
        [Tooltip("날씨 1종 머무는 시간(초)")]
        [SerializeField] private float _weatherSeconds = 13f;

        // 시간대 순환 — 아침→낮→저녁→밤. 낮밤 전환이 보이는 게 목적이라 균등 간격으로 돈다.
        private static readonly int[] CycleHours = { 7, 12, 18, 22 };

        // 날씨 순환 — 맑음에서 시작해 눈에 띄는 것부터. Storm은 화면이 너무 어두워 타이틀에서 제외.
        private static readonly WeatherType[] CycleWeather =
        {
            WeatherType.Clear, WeatherType.Rain, WeatherType.Snow, WeatherType.Fog, WeatherType.Cloudy,
        };

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");

        private bool _initialApplied;
        private int _direction = 1;      // +1 = 오른쪽
        private float _pauseLeft;
        private float _phaseTimer;
        private float _weatherTimer;
        private int _phaseIndex;
        private int _weatherIndex;

        private void Start()
        {
            if (_runnerAnimator != null) _runnerAnimator.SetBool(GroundedHash, true);
        }

        private void Update()
        {
            // 초기 적용은 Start가 아니라 첫 Update에서 한다. Core는 런타임에 Additive로 붙어서
            // 이 Start 시점엔 WorldWeatherManager의 구름 배열이 아직 없다 — Start에서 부르면
            // ApplyWeatherVisuals가 NRE로 죽는다(실측 재현). Unity가 모든 Start를 돌린 뒤에
            // Update를 시작하므로 첫 틱이면 안전하다. DistrictCaptureDemo도 같은 방식이다.
            if (!_initialApplied)
            {
                _initialApplied = true;
                ApplyPhase();
                ApplyWeather();
            }

            TickRunner();
            TickCycles();
        }

        private void TickRunner()
        {
            if (_runner == null) return;

            if (_pauseLeft > 0f)
            {
                _pauseLeft -= Time.deltaTime;
                if (_runnerAnimator != null) _runnerAnimator.SetFloat(SpeedHash, 0f);
                return;
            }

            Vector3 position = _runner.position;
            position.x += _direction * _runSpeed * Time.deltaTime;

            if (_direction > 0 && position.x >= _rightX) { position.x = _rightX; Turn(); }
            else if (_direction < 0 && position.x <= _leftX) { position.x = _leftX; Turn(); }

            _runner.position = position;
            // 진행 방향으로 몸을 돌린다 — 사이드뷰라 좌우 두 방향뿐.
            _runner.rotation = Quaternion.Euler(0f, _direction > 0 ? 90f : 270f, 0f);
            if (_runnerAnimator != null) _runnerAnimator.SetFloat(SpeedHash, _runSpeed);
        }

        private void Turn()
        {
            _direction = -_direction;
            _pauseLeft = _turnPause;
        }

        private void TickCycles()
        {
            _phaseTimer += Time.deltaTime;
            if (_phaseTimer >= _phaseSeconds)
            {
                _phaseTimer = 0f;
                _phaseIndex = (_phaseIndex + 1) % CycleHours.Length;
                ApplyPhase();
            }

            _weatherTimer += Time.deltaTime;
            if (_weatherTimer >= _weatherSeconds)
            {
                _weatherTimer = 0f;
                _weatherIndex = (_weatherIndex + 1) % CycleWeather.Length;
                ApplyWeather();
            }
        }

        private void ApplyPhase()
        {
            if (WorldDayNightManager.Instance != null)
                WorldDayNightManager.Instance.SetTime(CycleHours[_phaseIndex], 0);
        }

        private void ApplyWeather()
        {
            if (WorldWeatherManager.Instance != null)
                WorldWeatherManager.Instance.SetWeather(CycleWeather[_weatherIndex]);
        }
    }
}
