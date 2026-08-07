using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DontLate.Tests
{
    /// <summary>
    /// WorldWeatherManager — 날씨 추첨·예보 승계·맑음 고정 (S-058·184·189·202, 테스트는 S-206).
    /// 파티클·구름·컬러그레이드는 전부 미주입(null)이라 시각 경로는 조기 반환한다.
    /// `_clouds`만 빈 배열로 채운다 — 유일하게 null 검사 없이 순회하는 곳이라서.
    /// </summary>
    public class WeatherRollTests
    {
        private GameObject _go;
        private WorldWeatherManager _weather;
        private GameStateSO _gameState;

        [SetUp]
        public void SetUp()
        {
            _gameState = ScriptableObject.CreateInstance<GameStateSO>();
            _gameState.introGraceActive = false; // 기본은 자연 날씨 — 유예는 개별 테스트에서 켠다
            _gameState.day = 1;

            _go = new GameObject("WeatherUnderTest");
            _weather = _go.AddComponent<WorldWeatherManager>();
            TestSupport.SetField(_weather, "_gameState", _gameState);
            TestSupport.SetField(_weather, "_clouds", new SpriteRenderer[0]);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_gameState);
        }

        private void SetForecast(WeatherType tomorrow)
        {
            TestSupport.SetField(_weather, "<TomorrowWeather>k__BackingField", tomorrow);
            TestSupport.SetField(_weather, "_hasForecast", true);
        }

        // ── 기온표 ───────────────────────────────────────────

        [Test]
        public void 날씨별_대표_기온이_표대로_나온다()
        {
            Assert.AreEqual(34, WorldWeatherManager.TemperatureFor(WeatherType.Heat));
            Assert.AreEqual(24, WorldWeatherManager.TemperatureFor(WeatherType.Clear));
            Assert.AreEqual(20, WorldWeatherManager.TemperatureFor(WeatherType.Cloudy));
            Assert.AreEqual(17, WorldWeatherManager.TemperatureFor(WeatherType.Rain));
            Assert.AreEqual(14, WorldWeatherManager.TemperatureFor(WeatherType.Fog));
            Assert.AreEqual(-2, WorldWeatherManager.TemperatureFor(WeatherType.Snow));
        }

        [Test]
        public void 표에_없는_날씨는_기본_20도로_떨어진다()
        {
            Assert.AreEqual(20, WorldWeatherManager.TemperatureFor(WeatherType.Storm));
        }

        // ── 추첨 ─────────────────────────────────────────────

        [Test]
        public void 추첨은_가중치표_안의_날씨만_뽑는다()
        {
            var allowed = new HashSet<WeatherType>
            {
                WeatherType.Clear, WeatherType.Cloudy, WeatherType.Rain,
                WeatherType.Snow, WeatherType.Fog, WeatherType.Heat, WeatherType.Storm,
            };

            for (int i = 0; i < 300; i++)
            {
                var drawn = (WeatherType)TestSupport.Invoke(_weather, "Draw");
                Assert.IsTrue(allowed.Contains(drawn), "표 밖의 날씨가 나왔다: " + drawn);
            }
        }

        // ── 예보 승계 (S-058) ────────────────────────────────

        [Test]
        public void 어제_예보가_오늘_날씨가_된다_S058()
        {
            SetForecast(WeatherType.Rain);

            TestSupport.Invoke(_weather, "Reroll");

            Assert.AreEqual(WeatherType.Rain, _weather.Weather);
        }

        [Test]
        public void 승계_후에는_내일_예보를_새로_뽑는다()
        {
            SetForecast(WeatherType.Rain);

            TestSupport.Invoke(_weather, "Reroll");

            Assert.AreEqual(WeatherType.Rain, _weather.Weather);
            // 새 예보는 추첨이라 값을 못 박을 수 없다 — "오늘로 소비된 값이 예보에 그대로 남아 있지 않다"는
            // 것도 확률상 못 박는다(같은 값이 다시 뽑힐 수 있다). 여기서 잠그는 건 예보 플래그 유지.
            Assert.IsTrue((bool)TestSupport.GetField(_weather, "_hasForecast"));
        }

        [Test]
        public void 예보가_없는_첫날은_바로_추첨해서_쓴다()
        {
            TestSupport.SetField(_weather, "_hasForecast", false);

            TestSupport.Invoke(_weather, "Reroll");

            Assert.IsTrue((bool)TestSupport.GetField(_weather, "_hasForecast"), "첫날 이후로는 예보가 서 있어야 한다");
        }

        [Test]
        public void 같은_날에_두_번_틱해도_다시_굴리지_않는다()
        {
            SetForecast(WeatherType.Rain);
            var clock = new GameClock { Day = 1, Hour = 9, Minute = 0 };

            TestSupport.Invoke(_weather, "OnClockTicked", clock);
            Assert.AreEqual(WeatherType.Rain, _weather.Weather);

            SetForecast(WeatherType.Snow); // 예보를 바꿔 둬도
            TestSupport.Invoke(_weather, "OnClockTicked", clock); // 같은 날이면 무시

            Assert.AreEqual(WeatherType.Rain, _weather.Weather);
        }

        // ── 맑음 고정 3종 ────────────────────────────────────

        [Test]
        public void 인트로_유예_동안은_맑음으로_고정된다_S184()
        {
            _gameState.introGraceActive = true;
            SetForecast(WeatherType.Snow);

            TestSupport.Invoke(_weather, "Reroll");

            Assert.AreEqual(WeatherType.Clear, _weather.Weather);
        }

        [Test]
        public void 유예_중에도_예보는_굴려_둔다_S184()
        {
            _gameState.introGraceActive = true;
            TestSupport.SetField(_weather, "_hasForecast", false);

            TestSupport.Invoke(_weather, "Reroll");

            Assert.IsTrue((bool)TestSupport.GetField(_weather, "_hasForecast"),
                "고정이 풀린 뒤 예보 승계가 끊기면 안 된다");
        }

        [Test]
        public void 먹자골목은_맑음_고정_구역이다_S189()
        {
            Assert.IsTrue((bool)TestSupport.InvokeStatic(typeof(WorldWeatherManager), "PinsClear", GameScene.FoodStreet));
            Assert.IsFalse((bool)TestSupport.InvokeStatic(typeof(WorldWeatherManager), "PinsClear", GameScene.Village));
            Assert.IsFalse((bool)TestSupport.InvokeStatic(typeof(WorldWeatherManager), "PinsClear", GameScene.Camp));
        }

        [Test]
        public void 고정_구역_안에서는_날짜가_넘어가도_맑음이다_S189()
        {
            TestSupport.SetField(_weather, "_pinnedClearScene", true);
            SetForecast(WeatherType.Rain);

            TestSupport.Invoke(_weather, "Reroll");

            Assert.AreEqual(WeatherType.Clear, _weather.Weather);
        }

        [Test]
        public void 엔딩이_시작되면_맑음으로_바꾼다_S202()
        {
            _weather.SetWeather(WeatherType.Rain);

            TestSupport.Invoke(_weather, "OnEndingStartedWeather");

            Assert.AreEqual(WeatherType.Clear, _weather.Weather);
            Assert.IsTrue((bool)TestSupport.GetField(_weather, "_endingClear"));
        }

        [Test]
        public void 엔딩_고정_후에는_재추첨해도_맑음을_깨지_않는다_S202()
        {
            TestSupport.Invoke(_weather, "OnEndingStartedWeather");
            SetForecast(WeatherType.Snow);

            TestSupport.Invoke(_weather, "Reroll");

            Assert.AreEqual(WeatherType.Clear, _weather.Weather);
        }
    }
}
