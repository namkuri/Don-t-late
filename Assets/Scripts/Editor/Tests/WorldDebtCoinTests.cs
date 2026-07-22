using NUnit.Framework;
using UnityEngine;

namespace DontLate.Tests
{
    /// <summary>WorldDebtManager 코인 API — CoinPrice/BuyCoin/SellAllCoin 경계값 (S-024).</summary>
    public class WorldDebtCoinTests
    {
        private GameObject _go;
        private WorldDebtManager _debt;
        private GameStateSO _gameState;
        private TuningConfigSO _tuning;

        [SetUp]
        public void SetUp()
        {
            _gameState = ScriptableObject.CreateInstance<GameStateSO>();
            _tuning = ScriptableObject.CreateInstance<TuningConfigSO>();
            _go = new GameObject("DebtUnderTest");
            _debt = _go.AddComponent<WorldDebtManager>();
            TestSupport.SetField(_debt, "_gameState", _gameState);
            TestSupport.SetField(_debt, "_tuning", _tuning);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_gameState);
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void CoinPrice_같은_시각이면_같은_가격_결정론()
        {
            _gameState.day = 3;
            _gameState.minuteOfDay = 615f;
            Assert.AreEqual(_debt.CoinPrice(), _debt.CoinPrice());
        }

        [Test]
        public void CoinPrice_변동성이_극단이어도_바닥_100원을_뚫지_않는다()
        {
            _tuning.coinVolatility = 100f; // 파동 음수 구간이면 클램프 없인 음수 가격
            int min = int.MaxValue;
            for (int m = 0; m < 1440; m += 10)
            {
                _gameState.minuteOfDay = m;
                int price = _debt.CoinPrice();
                Assert.GreaterOrEqual(price, 100);
                if (price < min) min = price;
            }
            Assert.AreEqual(100, min); // 바닥 클램프가 실제로 발동한 구간이 있어야 한다
        }

        [Test]
        public void BuyCoin_잔액_부족이면_거부하고_상태_불변()
        {
            _gameState.money = 500;
            Assert.IsFalse(_debt.BuyCoin(1000));
            Assert.AreEqual(500, _gameState.money);
            Assert.AreEqual(0f, _gameState.coinUnits);
        }

        [Test]
        public void BuyCoin_0원_이하는_거부()
        {
            _gameState.money = 5000;
            Assert.IsFalse(_debt.BuyCoin(0));
            Assert.IsFalse(_debt.BuyCoin(-100));
            Assert.AreEqual(5000, _gameState.money);
        }

        [Test]
        public void BuyCoin_성공_시_현재가_기준_수량_가산()
        {
            _gameState.money = 5000;
            _gameState.day = 1;
            _gameState.minuteOfDay = 480f;
            int price = _debt.CoinPrice(); // 결정론 — 매수 시점 가격

            Assert.IsTrue(_debt.BuyCoin(3000));
            Assert.AreEqual(2000, _gameState.money);
            Assert.AreEqual(3000f / price, _gameState.coinUnits, 1e-5f);
        }

        [Test]
        public void SellAllCoin_보유_0이면_0원_반환_잔액_불변()
        {
            _gameState.money = 1000;
            Assert.AreEqual(0, _debt.SellAllCoin());
            Assert.AreEqual(1000, _gameState.money);
        }

        [Test]
        public void SellAllCoin_전량_매도_후_수량_0()
        {
            _gameState.money = 0;
            _gameState.day = 2;
            _gameState.minuteOfDay = 720f;
            _gameState.coinUnits = 2.5f;
            int price = _debt.CoinPrice();

            int gained = _debt.SellAllCoin();

            Assert.AreEqual(Mathf.RoundToInt(2.5f * price), gained);
            Assert.AreEqual(gained, _gameState.money);
            Assert.AreEqual(0f, _gameState.coinUnits);
        }
    }
}
