using NUnit.Framework;
using UnityEngine;

namespace DontLate.Tests
{
    /// <summary>WorldDeliveryManager.SettleDeliveries — S-034 ④ 일괄 판정 (배치 일치/오배치/미배치·초기화).</summary>
    public class DeliverySettleTests
    {
        private GameObject _go;
        private WorldDeliveryManager _delivery;
        private GameStateSO _gameState;
        private TuningConfigSO _tuning;
        private DeliveryOrderSO _order;

        [SetUp]
        public void SetUp()
        {
            _gameState = ScriptableObject.CreateInstance<GameStateSO>();
            _tuning = ScriptableObject.CreateInstance<TuningConfigSO>();
            _tuning.latePenalty = 500;
            _go = new GameObject("DeliveryUnderTest");
            _delivery = _go.AddComponent<WorldDeliveryManager>();
            TestSupport.SetField(_delivery, "_gameState", _gameState);
            TestSupport.SetField(_delivery, "_tuning", _tuning);

            _order = ScriptableObject.CreateInstance<DeliveryOrderSO>();
            _order.orderId = 7;
            _order.address = "행복빌라 301호";
            _order.reward = 1500;
            _gameState.cargo.Add(_order);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_gameState);
            Object.DestroyImmediate(_tuning);
            Object.DestroyImmediate(_order);
        }

        [Test]
        public void 배치_주소_일치면_성공_보상_지급()
        {
            _delivery.PlaceDelivery(_order, "행복빌라 301호");

            DeliveryDaySummary summary = _delivery.SettleDeliveries();

            Assert.AreEqual(1, summary.SuccessCount);
            Assert.AreEqual(0, summary.FailCount);
            Assert.AreEqual(1500, _gameState.money);
            Assert.AreEqual(1, _gameState.deliveryHistory.Count);
        }

        [Test]
        public void 오배치면_실패_벌금_차감()
        {
            _gameState.money = 2000;
            _delivery.PlaceDelivery(_order, "골목슈퍼"); // 엉뚱한 비콘

            DeliveryDaySummary summary = _delivery.SettleDeliveries();

            Assert.AreEqual(1, summary.FailCount);
            Assert.AreEqual(500, summary.PenaltyTotal);
            Assert.AreEqual(1500, _gameState.money);
        }

        [Test]
        public void 미배치면_실패_잔액_부족분은_빚()
        {
            _gameState.money = 200;
            _gameState.debt = 1000;

            _delivery.SettleDeliveries();

            Assert.AreEqual(0, _gameState.money);   // 200 − 500 → 0
            Assert.AreEqual(1300, _gameState.debt); // 부족 300 이 빚으로
        }

        [Test]
        public void 정산_후_상차_스캔_배치_전부_초기화()
        {
            _gameState.scannedOrderIds.Add(7);
            _delivery.PlaceDelivery(_order, "행복빌라 301호");

            _delivery.SettleDeliveries();

            Assert.AreEqual(0, _gameState.cargo.Count);
            Assert.AreEqual(0, _gameState.scannedOrderIds.Count);
            Assert.AreEqual(0, _gameState.placedDeliveries.Count);
        }

        [Test]
        public void 재픽업하면_배치_철회()
        {
            _delivery.PlaceDelivery(_order, "행복빌라 301호");
            Assert.IsTrue(_delivery.IsPlaced(7));

            _delivery.UnplaceDelivery(7);

            Assert.IsFalse(_delivery.IsPlaced(7));
        }
    }
}
