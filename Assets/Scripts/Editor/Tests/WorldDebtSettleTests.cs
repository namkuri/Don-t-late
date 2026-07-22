using NUnit.Framework;
using UnityEngine;

namespace DontLate.Tests
{
    /// <summary>WorldDebtManager.SettleNow 상환 수식 (S-024). 씬 불요 — 순수 로직.</summary>
    public class WorldDebtSettleTests
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
        public void SettleNow_잔액이_빚보다_적으면_잔액_전부_상환()
        {
            _gameState.money = 5000;
            _gameState.debt = 10000;

            DebtSettlement result = _debt.SettleNow();

            Assert.AreEqual(5000, result.Repaid);
            Assert.AreEqual(0, result.Money);
            Assert.AreEqual(5000, result.Debt);
            Assert.AreEqual(0, _gameState.money);
            Assert.AreEqual(5000, _gameState.debt);
        }

        [Test]
        public void SettleNow_잔액이_빚보다_많으면_빚만큼만_상환하고_잔액_보존()
        {
            _gameState.money = 12000;
            _gameState.debt = 10000;

            DebtSettlement result = _debt.SettleNow();

            Assert.AreEqual(10000, result.Repaid);
            Assert.AreEqual(2000, result.Money);
            Assert.AreEqual(0, result.Debt);
        }

        [Test]
        public void SettleNow_잔액_0원이면_아무것도_안_바뀐다()
        {
            _gameState.money = 0;
            _gameState.debt = 10000;

            DebtSettlement result = _debt.SettleNow();

            Assert.AreEqual(0, result.Repaid);
            Assert.AreEqual(0, _gameState.money);
            Assert.AreEqual(10000, _gameState.debt);
        }

        [Test]
        public void SettleNow_DebtSettled_이벤트_페이로드가_반환값과_일치()
        {
            _gameState.money = 3000;
            _gameState.debt = 7000;

            DebtSettlement captured = default;
            int raised = 0;
            System.Action<DebtSettlement> handler = s => { captured = s; raised++; };
            WorldEvents.DebtSettled += handler;
            try
            {
                DebtSettlement result = _debt.SettleNow();
                Assert.AreEqual(1, raised);
                Assert.AreEqual(result.Repaid, captured.Repaid);
                Assert.AreEqual(result.Money, captured.Money);
                Assert.AreEqual(result.Debt, captured.Debt);
            }
            finally
            {
                WorldEvents.DebtSettled -= handler;
            }
        }
    }
}
