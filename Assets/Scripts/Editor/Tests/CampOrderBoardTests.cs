using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DontLate.Tests
{
    /// <summary>
    /// CampOrderBoard.IsConsumed 3분기(완료/마감경과/미접촉)·GenerateOrder 시리얼 증가 (S-024).
    /// 두 메서드는 private — TestSupport 리플렉션으로 호출.
    /// </summary>
    public class CampOrderBoardTests
    {
        private GameObject _go;
        private CampOrderBoard _board;
        private GameStateSO _gameState;
        private DeliveryOrderSO _order;
        private readonly List<Object> _runtimeOrders = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _gameState = ScriptableObject.CreateInstance<GameStateSO>();
            _go = new GameObject("BoardUnderTest");
            _board = _go.AddComponent<CampOrderBoard>();
            TestSupport.SetField(_board, "_gameState", _gameState);

            _order = ScriptableObject.CreateInstance<DeliveryOrderSO>();
            _order.orderId = 42;
            _order.deadlineMinuteOfDay = 600f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_gameState);
            Object.DestroyImmediate(_order);
            foreach (Object o in _runtimeOrders) Object.DestroyImmediate(o);
            _runtimeOrders.Clear();
        }

        private bool IsConsumed(DeliveryOrderSO order)
            => (bool)TestSupport.Invoke(_board, "IsConsumed", order);

        private DeliveryOrderSO Generate()
        {
            var order = (DeliveryOrderSO)TestSupport.Invoke(_board, "GenerateOrder");
            _runtimeOrders.Add(order);
            return order;
        }

        // ── IsConsumed 분기 ──────────────────────────────────

        [Test]
        public void IsConsumed_히스토리에_있으면_완료_소진()
        {
            _gameState.deliveryHistory.Add(new DeliveryRecord { orderId = 42 });
            Assert.IsTrue(IsConsumed(_order));
        }

        [Test]
        public void IsConsumed_마감경과_미적재_스캔됨이면_소진()
        {
            _gameState.minuteOfDay = 700f; // 마감 600 경과
            _gameState.scannedOrderIds.Add(42);
            Assert.IsTrue(IsConsumed(_order));
        }

        [Test]
        public void IsConsumed_마감경과라도_미접촉이면_유지()
        {
            _gameState.minuteOfDay = 700f; // 스캔 안 함 = 손도 안 댄 건
            Assert.IsFalse(IsConsumed(_order));
        }

        [Test]
        public void IsConsumed_마감경과라도_적재_중이면_유지()
        {
            _gameState.minuteOfDay = 700f;
            _gameState.scannedOrderIds.Add(42);
            _gameState.cargo.Add(_order); // 실은 짐은 소진 아님
            Assert.IsFalse(IsConsumed(_order));
        }

        [Test]
        public void IsConsumed_마감_전이면_유지()
        {
            _gameState.minuteOfDay = 599f;
            _gameState.scannedOrderIds.Add(42);
            Assert.IsFalse(IsConsumed(_order));
        }

        // ── GenerateOrder ────────────────────────────────────

        [Test]
        public void GenerateOrder_시리얼이_orderId가_되고_1씩_증가()
        {
            _gameState.nextOrderSerial = 200;

            DeliveryOrderSO first = Generate();
            DeliveryOrderSO second = Generate();

            Assert.AreEqual(200, first.orderId);
            Assert.AreEqual(201, second.orderId);
            Assert.AreEqual(202, _gameState.nextOrderSerial);
        }

        [Test]
        public void GenerateOrder_마감은_현재시각_240분_이후이되_1435를_넘지_않는다()
        {
            _gameState.nextOrderSerial = 300; // serial%3==0 → 최소 오프셋 240분
            _gameState.minuteOfDay = 1300f;   // 1300+240=1540 → 1435로 캡

            DeliveryOrderSO capped = Generate();
            Assert.AreEqual(1435f, capped.deadlineMinuteOfDay);

            _gameState.minuteOfDay = 500f;    // 캡 미달 구간
            _gameState.nextOrderSerial = 300;
            DeliveryOrderSO normal = Generate();
            Assert.AreEqual(740f, normal.deadlineMinuteOfDay);
        }

        [Test]
        public void GenerateOrder_주소는_목적지_풀에서_나온다()
        {
            _gameState.nextOrderSerial = 205;
            DeliveryOrderSO order = Generate();
            Assert.IsFalse(string.IsNullOrEmpty(order.address));
            Assert.IsFalse(string.IsNullOrEmpty(order.district));
        }
    }
}
