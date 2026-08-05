using NUnit.Framework;
using UnityEngine;

namespace DontLate.Tests
{
    /// <summary>가방 수납 규칙 (S-064 · 회고 3차 백로그 ③). 순수 로직 — 씬 불요.</summary>
    public class BagStorageTests
    {
        private GameStateSO _gameState;

        [SetUp]
        public void SetUp() => _gameState = ScriptableObject.CreateInstance<GameStateSO>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_gameState);

        [Test]
        public void 겹침_아이템은_같은_칸에_쌓인다()
        {
            Assert.IsTrue(BagStorage.TryAdd(_gameState, "drink", "드링크", stackable: true, holdable: true));
            Assert.IsTrue(BagStorage.TryAdd(_gameState, "drink", "드링크", stackable: true, holdable: true));
            Assert.AreEqual(1, _gameState.bagItems.Count);
            Assert.AreEqual(2, _gameState.bagItems[0].count);
        }

        [Test]
        public void 비겹침_아이템은_칸을_따로_쓰고_상한_4칸에서_거절된다()
        {
            for (int i = 0; i < 4; i++)
                Assert.IsTrue(BagStorage.TryAdd(_gameState, "item" + i, "아이템" + i, stackable: false, holdable: false));
            Assert.IsFalse(BagStorage.TryAdd(_gameState, "item4", "아이템4", stackable: false, holdable: false));
            Assert.AreEqual(4, _gameState.bagItems.Count);
        }

        [Test]
        public void 가득_차도_겹침_기존_칸에는_더_쌓인다()
        {
            BagStorage.TryAdd(_gameState, "drink", "드링크", stackable: true, holdable: true);
            for (int i = 0; i < 3; i++) BagStorage.TryAdd(_gameState, "item" + i, "아이템" + i, false, false);
            Assert.IsTrue(BagStorage.TryAdd(_gameState, "drink", "드링크", stackable: true, holdable: true));
            Assert.AreEqual(2, _gameState.bagItems[0].count);
        }

        [Test]
        public void 하나_빼면_카운트가_줄고_0이_되면_칸이_사라진다()
        {
            BagStorage.TryAdd(_gameState, "drink", "드링크", stackable: true, holdable: true);
            BagStorage.TryAdd(_gameState, "drink", "드링크", stackable: true, holdable: true);
            BagStorage.RemoveOne(_gameState, 0);
            Assert.AreEqual(1, _gameState.bagItems[0].count);
            BagStorage.RemoveOne(_gameState, 0);
            Assert.AreEqual(0, _gameState.bagItems.Count);
        }
    }
}
