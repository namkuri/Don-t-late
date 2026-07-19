using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 게임 시계의 유일한 진행 주체. 시각 원본은 GameStateSO가 보관하고 여기서만 쓴다.
    /// 다른 매니저는 SO를 읽거나 ClockTicked를 구독한다 — 매니저 간 직접 참조는 없다.
    /// 조명·LUT 구동은 P3에서 이 클래스에 추가된다.
    /// </summary>
    public class WorldDayNightManager : MonoBehaviour
    {
        private const float MINUTES_PER_DAY = 24f * 60f;

        public static WorldDayNightManager Instance { get; private set; }

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private TuningConfigSO _tuning;

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
        }

        private void Start()
        {
            _phase = ResolvePhase(_gameState.minuteOfDay);
            WorldEvents.RaiseDayPhaseChanged(_phase);
        }

        private void Update()
        {
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

            DayPhase phase = ResolvePhase(_gameState.minuteOfDay);
            if (phase == _phase) return;

            _phase = phase;
            WorldEvents.RaiseDayPhaseChanged(phase);
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
