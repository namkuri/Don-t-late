using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DontLate.Tests
{
    /// <summary>
    /// CampOrderBoard.IsConsumed(완료/여유 부족 — S-031 ⑦ 개정: 미적재+여유 120분 미만이면 교체)·GenerateOrder (S-024).
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
        public void IsConsumed_마감경과_미적재면_소진()
        {
            _gameState.minuteOfDay = 700f; // 마감 600 경과
            Assert.IsTrue(IsConsumed(_order));
        }

        [Test]
        public void IsConsumed_여유_120분_미만이면_미적재는_소진_S031()
        {
            _gameState.minuteOfDay = 599f; // 여유 1분 — "싣는 중 마감" 원흉
            Assert.IsTrue(IsConsumed(_order));
        }

        [Test]
        public void IsConsumed_마감경과라도_적재_중이면_유지()
        {
            _gameState.minuteOfDay = 700f;
            _gameState.cargo.Add(_order); // 실은 짐은 소진 아님
            Assert.IsFalse(IsConsumed(_order));
        }

        [Test]
        public void IsConsumed_여유_120분_이상이면_유지()
        {
            _gameState.minuteOfDay = 400f; // 여유 200분
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
        public void GenerateOrder_마감은_현재시각_300분_이후이되_1435를_넘지_않는다()
        {
            _gameState.nextOrderSerial = 300; // %12==0 → 빌라촌 픽(마감 상향 미적용) · %3==0 → 오프셋 300분 (S-049 풀 12종 정합)
            _gameState.minuteOfDay = 1300f;   // 1300+300=1600 → 1435로 캡

            DeliveryOrderSO capped = Generate();
            Assert.AreEqual(1435f, capped.deadlineMinuteOfDay);

            _gameState.minuteOfDay = 500f;    // 캡 미달 구간
            _gameState.nextOrderSerial = 300;
            DeliveryOrderSO normal = Generate();
            Assert.AreEqual(800f, normal.deadlineMinuteOfDay);
        }

        [Test]
        public void GenerateOrder_먹자골목은_마감이_19시_이후로_밀린다_S035()
        {
            _gameState.nextOrderSerial = 303; // %12==3 → 먹자골목 픽 · %3==0 → 오프셋 300분 (S-049 풀 12종 정합)
            _gameState.minuteOfDay = 500f;    // 기본 마감 800(13.3시) → 저녁 19시(1140)로 상향
            DeliveryOrderSO order = Generate();
            Assert.AreEqual(DeliveryOrderSO.DISTRICT_FOODALLEY, order.district);
            Assert.AreEqual(1140f, order.deadlineMinuteOfDay);
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
