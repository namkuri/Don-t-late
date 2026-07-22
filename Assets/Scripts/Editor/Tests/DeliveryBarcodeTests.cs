using NUnit.Framework;
using UnityEngine;

namespace DontLate.Tests
{
    /// <summary>WorldDeliveryManager.RegisterBarcode 중복 거부 (S-024).</summary>
    public class DeliveryBarcodeTests
    {
        private GameObject _go;
        private WorldDeliveryManager _delivery;
        private GameStateSO _gameState;
        private DeliveryOrderSO _order;

        [SetUp]
        public void SetUp()
        {
            _gameState = ScriptableObject.CreateInstance<GameStateSO>();
            _go = new GameObject("DeliveryUnderTest");
            _delivery = _go.AddComponent<WorldDeliveryManager>();
            TestSupport.SetField(_delivery, "_gameState", _gameState);

            _order = ScriptableObject.CreateInstance<DeliveryOrderSO>();
            _order.orderId = 7;
            _order.address = "행복빌라 301호";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_gameState);
            Object.DestroyImmediate(_order);
        }

        [Test]
        public void RegisterBarcode_첫_스캔은_등록된다()
        {
            Assert.IsTrue(_delivery.RegisterBarcode(_order));
            Assert.Contains(7, _gameState.scannedOrderIds);
        }

        [Test]
        public void RegisterBarcode_같은_주문_재스캔은_거부되고_목록_불변()
        {
            _delivery.RegisterBarcode(_order);

            Assert.IsFalse(_delivery.RegisterBarcode(_order));
            Assert.AreEqual(1, _gameState.scannedOrderIds.Count);
        }

        [Test]
        public void RegisterBarcode_이벤트는_첫_등록에만_1회_발행()
        {
            int raised = 0;
            System.Action<DeliveryData> handler = _ => raised++;
            WorldEvents.BarcodeScanned += handler;
            try
            {
                _delivery.RegisterBarcode(_order);
                _delivery.RegisterBarcode(_order); // 중복 — 발행 금지
                Assert.AreEqual(1, raised);
            }
            finally
            {
                WorldEvents.BarcodeScanned -= handler;
            }
        }

        [Test]
        public void IsScanned_등록_전후_상태를_정확히_반영()
        {
            Assert.IsFalse(_delivery.IsScanned(_order));
            _delivery.RegisterBarcode(_order);
            Assert.IsTrue(_delivery.IsScanned(_order));
        }
    }
}
