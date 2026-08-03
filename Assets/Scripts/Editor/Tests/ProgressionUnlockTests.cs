using NUnit.Framework;
using UnityEngine;

namespace DontLate.Tests
{
    /// <summary>구역 개척 언락·트럭 수령 (S-054 · 회고 3차 백로그 ③) — SettleDeliveries 경유 실호출.</summary>
    public class ProgressionUnlockTests
    {
        private GameObject _go;
        private WorldDeliveryManager _delivery;
        private GameStateSO _gameState;
        private TuningConfigSO _tuning;

        [SetUp]
        public void SetUp()
        {
            _gameState = ScriptableObject.CreateInstance<GameStateSO>();
            _tuning = ScriptableObject.CreateInstance<TuningConfigSO>();
            _go = new GameObject("DeliveryUnderTest");
            _delivery = _go.AddComponent<WorldDeliveryManager>();
            TestSupport.SetField(_delivery, "_gameState", _gameState);
            TestSupport.SetField(_delivery, "_tuning", _tuning);
            _gameState.unlockedDistricts.Add(DeliveryOrderSO.DISTRICT_VILLATOWN);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_gameState);
            Object.DestroyImmediate(_tuning);
        }

        private void SettleSuccessIn(string district, int orderId)
        {
            DeliveryOrderSO order = ScriptableObject.CreateInstance<DeliveryOrderSO>();
            order.orderId = orderId;
            order.address = district + " 테스트호";
            order.district = district;
            _gameState.cargo.Add(order);
            _gameState.placedDeliveries.Add(new PlacedDelivery { orderId = orderId, beaconAddress = order.address });
            _delivery.SettleDeliveries();
        }

        [Test]
        public void 최전선_구역_성공_정산이_다음_구역을_해금한다()
        {
            SettleSuccessIn(DeliveryOrderSO.DISTRICT_VILLATOWN, 9001);
            Assert.Contains(DeliveryOrderSO.DISTRICT_FOODALLEY, _gameState.unlockedDistricts);
            Assert.IsFalse(_gameState.unlockedDistricts.Contains(DeliveryOrderSO.DISTRICT_APARTMENT), "건너뛰기 금지");
        }

        [Test]
        public void 후방_구역_성공은_해금을_진행시키지_않는다()
        {
            _gameState.unlockedDistricts.Add(DeliveryOrderSO.DISTRICT_FOODALLEY); // 최전선=먹자
            SettleSuccessIn(DeliveryOrderSO.DISTRICT_VILLATOWN, 9002);            // 후방 성공
            Assert.IsFalse(_gameState.unlockedDistricts.Contains(DeliveryOrderSO.DISTRICT_APARTMENT));
        }

        [Test]
        public void 마지막_구역_성공_정산이_트럭을_지급한다()
        {
            _gameState.unlockedDistricts.Clear();
            foreach (string district in DeliveryOrderSO.DISTRICT_PROGRESSION)
                _gameState.unlockedDistricts.Add(district);
            _gameState.playerLevel = LevelPerks.TRUCK; // S-134 ② — 트럭은 Lv4부터
            SettleSuccessIn(DeliveryOrderSO.DISTRICT_HILLSIDE, 9003);
            Assert.IsTrue(_gameState.hasTruck);
        }

        [Test]
        public void 레벨이_낮으면_개척을_끝내도_트럭이_안_나온다()
        {
            // S-134 ② — 종전엔 개척만 끝나면 레벨과 무관하게 나왔다(정수님 QA 레벨 해금 요구).
            _gameState.unlockedDistricts.Clear();
            foreach (string district in DeliveryOrderSO.DISTRICT_PROGRESSION)
                _gameState.unlockedDistricts.Add(district);
            _gameState.playerLevel = LevelPerks.TRUCK - 1;
            SettleSuccessIn(DeliveryOrderSO.DISTRICT_HILLSIDE, 9004);
            Assert.IsFalse(_gameState.hasTruck);
        }
    }
}
