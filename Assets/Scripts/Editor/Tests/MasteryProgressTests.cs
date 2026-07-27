using NUnit.Framework;
using UnityEngine;

namespace DontLate.Tests
{
    /// <summary>숙련도 레벨링 수식 (S-063 · 회고 3차 백로그 ③). 순수 로직 — 씬 불요.</summary>
    public class MasteryProgressTests
    {
        private GameStateSO _gameState;

        [SetUp]
        public void SetUp() => _gameState = ScriptableObject.CreateInstance<GameStateSO>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_gameState);

        [Test]
        public void 만충_시_레벨업하고_초과분은_이월된다()
        {
            MasteryProgress.Add(_gameState, 130f); // Lv1 상한 100 → Lv2 + 이월 30
            Assert.AreEqual(2, _gameState.playerLevel);
            Assert.AreEqual(30f, _gameState.mastery, 0.01f);
        }

        [Test]
        public void 감점은_0_아래로_내려가지_않는다()
        {
            MasteryProgress.Add(_gameState, 20f);
            MasteryProgress.Add(_gameState, -100f);
            Assert.AreEqual(0f, _gameState.mastery, 0.01f);
            Assert.AreEqual(1, _gameState.playerLevel); // 레벨은 감점으로 내려가지 않는다
        }

        [Test]
        public void 대량_가산은_다중_레벨업을_연쇄한다()
        {
            MasteryProgress.Add(_gameState, 100f + 125f + 10f); // Lv1(100)+Lv2(125) 관통 → Lv3 + 10
            Assert.AreEqual(3, _gameState.playerLevel);
            Assert.AreEqual(10f, _gameState.mastery, 0.01f);
        }
    }
}
