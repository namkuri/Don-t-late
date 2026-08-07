using NUnit.Framework;
using UnityEngine;

namespace DontLate.Tests
{
    /// <summary>
    /// PlayerStatusManager — 스태미나 패널티 구간제 (S-088 ④ 상한 차감 모델, 테스트는 S-206).
    /// 드레인은 Update의 프레임 계산이라 EditMode에서 못 돌린다. 여기서 잠그는 것은
    /// **패널티 표와 상한 계산** — 폭염·한파·태풍·적재가 각각 얼마를 깎는지, 그리고
    /// 사용 가능 최대치가 어떻게 나오는지.
    /// </summary>
    public class StaminaPenaltyTests
    {
        private GameObject _go;
        private PlayerManager _hub;
        private PlayerStatusManager _status;
        private GameStateSO _gameState;
        private TuningConfigSO _tuning;
        private DeliveryOrderSO _box1;
        private DeliveryOrderSO _box2;

        private int _penaltyEventCount;

        [SetUp]
        public void SetUp()
        {
            _gameState = ScriptableObject.CreateInstance<GameStateSO>();
            _tuning = ScriptableObject.CreateInstance<TuningConfigSO>();
            _tuning.staminaMax = 100f;
            _tuning.staminaPenaltyHeat = 15f;
            _tuning.staminaPenaltyCold = 15f;
            _tuning.staminaPenaltyStorm = 15f;
            _tuning.staminaPenaltyCarryPerBox = 10f;

            _go = new GameObject("PlayerUnderTest");
            _hub = _go.AddComponent<PlayerManager>();
            _status = _go.AddComponent<PlayerStatusManager>();
            TestSupport.SetField(_hub, "_tuning", _tuning);
            TestSupport.SetField(_hub, "_gameState", _gameState);
            TestSupport.SetField(_status, "_hub", _hub); // Awake가 안 도는 EditMode — 허브 직접 주입

            _box1 = ScriptableObject.CreateInstance<DeliveryOrderSO>();
            _box2 = ScriptableObject.CreateInstance<DeliveryOrderSO>();

            _penaltyEventCount = 0;
            WorldEvents.StaminaPenaltyChanged += OnPenaltyChanged;
        }

        [TearDown]
        public void TearDown()
        {
            WorldEvents.StaminaPenaltyChanged -= OnPenaltyChanged;
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_gameState);
            Object.DestroyImmediate(_tuning);
            Object.DestroyImmediate(_box1);
            Object.DestroyImmediate(_box2);
        }

        private void OnPenaltyChanged(StaminaPenalties _) => _penaltyEventCount++;

        private void SetWeather(WeatherType weather) => TestSupport.SetField(_status, "_weather", weather);

        /// <summary>캐리 상태는 자동 프로퍼티라 백킹 필드로 세운다 (TryCarry는 시각물 생성까지 끌고 온다).</summary>
        private void SetCarried(DeliveryOrderSO first, DeliveryOrderSO second)
        {
            TestSupport.SetField(_status, "<CarriedOrder>k__BackingField", first);
            TestSupport.SetField(_status, "<CarriedOrder2>k__BackingField", second);
        }

        private StaminaPenalties Tick()
        {
            TestSupport.Invoke(_status, "TickPenalties");
            return _status.CurrentPenalties;
        }

        [Test]
        public void 맑은_날_맨몸이면_패널티가_없다()
        {
            SetWeather(WeatherType.Clear);

            StaminaPenalties p = Tick();

            Assert.AreEqual(0f, p.Total, 0.001f);
        }

        [Test]
        public void 폭염이면_더움_패널티가_붙는다()
        {
            SetWeather(WeatherType.Heat);

            StaminaPenalties p = Tick();

            Assert.AreEqual(15f, p.Heat, 0.001f);
            Assert.AreEqual(0f, p.Cold, 0.001f);
            Assert.AreEqual(15f, p.Total, 0.001f);
        }

        [Test]
        public void 눈이_오면_추움_패널티가_붙는다()
        {
            SetWeather(WeatherType.Snow);

            StaminaPenalties p = Tick();

            Assert.AreEqual(15f, p.Cold, 0.001f);
            Assert.AreEqual(0f, p.Heat, 0.001f);
        }

        [Test]
        public void 태풍이면_태풍_패널티가_붙는다()
        {
            SetWeather(WeatherType.Storm);

            StaminaPenalties p = Tick();

            Assert.AreEqual(15f, p.Storm, 0.001f);
        }

        [Test]
        public void 더움_해소_음료를_마신_동안은_폭염_패널티가_0이다()
        {
            SetWeather(WeatherType.Heat);
            TestSupport.SetField(_status, "_heatReliefUntil", Time.time + 90f);

            StaminaPenalties p = Tick();

            Assert.AreEqual(0f, p.Heat, 0.001f);
        }

        [Test]
        public void 상자_수만큼_적재_패널티가_쌓인다()
        {
            SetWeather(WeatherType.Clear);
            SetCarried(_box1, _box2);

            StaminaPenalties p = Tick();

            Assert.AreEqual(20f, p.Carry, 0.001f); // 10 × 2칸
        }

        [Test]
        public void 날씨와_적재_패널티는_합산된다()
        {
            SetWeather(WeatherType.Heat);
            SetCarried(_box1, null);

            StaminaPenalties p = Tick();

            Assert.AreEqual(25f, p.Total, 0.001f); // 더움 15 + 상자 10
        }

        [Test]
        public void 사용_가능_최대치는_상한에서_패널티_합만큼_깎인다()
        {
            SetWeather(WeatherType.Heat);
            SetCarried(_box1, null);
            Tick();

            Assert.AreEqual(75f, _status.EffectiveStaminaMax, 0.001f); // 100 − (15 + 10)
        }

        [Test]
        public void 사용_가능_최대치는_10_밑으로_내려가지_않는다()
        {
            _tuning.staminaMax = 20f;
            _tuning.staminaPenaltyCarryPerBox = 30f;
            SetWeather(WeatherType.Clear);
            SetCarried(_box1, _box2);
            Tick();

            Assert.AreEqual(10f, _status.EffectiveStaminaMax, 0.001f); // 20 − 60 = 음수 → 하한 10
        }

        [Test]
        public void 패널티가_바뀔_때만_이벤트가_나간다()
        {
            SetWeather(WeatherType.Clear);
            Tick();
            Assert.AreEqual(0, _penaltyEventCount, "무변화(전부 0)면 발화하지 않는다");

            SetWeather(WeatherType.Heat);
            Tick();
            Assert.AreEqual(1, _penaltyEventCount, "0 → 더움 15 로 바뀌면 1회");

            Tick();
            Assert.AreEqual(1, _penaltyEventCount, "같은 상태를 다시 계산해도 추가 발화 없음");
        }
    }
}
