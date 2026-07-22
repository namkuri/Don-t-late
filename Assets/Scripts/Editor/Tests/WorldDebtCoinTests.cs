using NUnit.Framework;
using UnityEngine;

namespace DontLate.Tests
{
    /// <summary>WorldDebtManager 코인 API — CoinPrice/BuyOneCoin/SellOneCoin/SellAllCoin 경계값 (S-024 → S-032 ⑤ 1개 단위 개편).</summary>
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
        public void BuyOneCoin_잔액_부족이면_거부하고_상태_불변()
        {
            _gameState.money = 50; // 바닥가 100 미만
            Assert.IsFalse(_debt.BuyOneCoin());
            Assert.AreEqual(50, _gameState.money);
            Assert.AreEqual(0f, _gameState.coinUnits);
            Assert.AreEqual(0f, _gameState.coinCostBasis);
        }

        [Test]
        public void BuyOneCoin_성공_시_현재가_차감_1개_가산_원가_누적()
        {
            _gameState.money = 5000;
            _gameState.day = 1;
            _gameState.minuteOfDay = 480f;
            int price = _debt.CoinPrice(); // 결정론 — 매수 시점 가격

            Assert.IsTrue(_debt.BuyOneCoin());
            Assert.AreEqual(5000 - price, _gameState.money);
            Assert.AreEqual(1f, _gameState.coinUnits);
            Assert.AreEqual(price, _gameState.coinCostBasis, 1e-3f);
        }

        [Test]
        public void SellOneCoin_보유_없으면_0_상태_불변()
        {
            _gameState.money = 1000;
            Assert.AreEqual(0, _debt.SellOneCoin());
            Assert.AreEqual(1000, _gameState.money);
        }

        [Test]
        public void SellOneCoin_현재가_가산_원가_평균법_차감()
        {
            _gameState.money = 0;
            _gameState.day = 2;
            _gameState.minuteOfDay = 720f;
            _gameState.coinUnits = 2f;
            _gameState.coinCostBasis = 1600f; // 평균 원가 800
            int price = _debt.CoinPrice();

            int gained = _debt.SellOneCoin();

            Assert.AreEqual(price, gained);
            Assert.AreEqual(price, _gameState.money);
            Assert.AreEqual(1f, _gameState.coinUnits);
            Assert.AreEqual(800f, _gameState.coinCostBasis, 1e-3f); // 평균법 — 절반 남음
        }

        [Test]
        public void 시세차익_평가액_마이너스_매수원가와_일치()
        {
            _gameState.money = 100000;
            _gameState.day = 1;
            _gameState.minuteOfDay = 480f;
            _debt.BuyOneCoin();
            _gameState.minuteOfDay = 900f; // 시세 이동
            int now = _debt.CoinPrice();

            int profit = Mathf.RoundToInt(_gameState.coinUnits * now - _gameState.coinCostBasis);
            int expected = now - _debt.CoinPriceAt(1 * 1440f + 480f);
            Assert.AreEqual(expected, profit);
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
            Assert.AreEqual(0f, _gameState.coinCostBasis); // S-032 ⑤ — 원가도 청산
        }
    }
}
