using NUnit.Framework;
using UnityEngine;

namespace DontLate.Tests
{
    /// <summary>숙련도 레벨링 수식 (S-063 · 회고 3차 백로그 ③). 순수 로직 — 씬 불요.
    /// S-165 ④ — 규격 변경 반영: 상한이 **레벨 무관 고정 15**(5칸 × 3)이고 배송 1건 = 3점.
    /// 종전(100 + (lv-1)*25)을 전제하던 기대값을 새 규격으로 갱신했다.</summary>
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
            MasteryProgress.Add(_gameState, 6f); // 상한 5 → Lv2 + 이월 1
            Assert.AreEqual(2, _gameState.playerLevel);
            Assert.AreEqual(1f, _gameState.mastery, 0.01f);
        }

        [Test]
        public void 감점은_0_아래로_내려가지_않는다()
        {
            MasteryProgress.Add(_gameState, 2f);   // 2칸 — 아직 레벨업 전
            MasteryProgress.Add(_gameState, -100f);
            Assert.AreEqual(0f, _gameState.mastery, 0.01f);
            Assert.AreEqual(1, _gameState.playerLevel); // 레벨은 감점으로 내려가지 않는다
        }

        [Test]
        public void 대량_가산은_다중_레벨업을_연쇄한다()
        {
            MasteryProgress.Add(_gameState, 5f + 5f + 2f); // 상한 5를 두 번 관통 → Lv3 + 이월 2
            Assert.AreEqual(3, _gameState.playerLevel);
            Assert.AreEqual(2f, _gameState.mastery, 0.01f);
        }

        [Test]
        public void 배송_1건은_정확히_2칸이다()
        {
            // S-174 ② — 남규님 규격: 5칸 게이지 · 1건 = 2칸. 이 관계가 깨지면 "몇 건 남았는지"를
            // 화면만 보고 셀 수 없게 된다 — 수치 튜닝이 이 불변식을 넘지 않도록 못을 박는다.
            MasteryProgress.Add(_gameState, MasteryProgress.SUCCESS_GAIN);
            Assert.AreEqual(2f, _gameState.mastery, 0.01f);
            Assert.AreEqual(1, _gameState.playerLevel);
        }

        [Test]
        public void 배송_2건_반이면_한_레벨_오른다()
        {
            // 1건 2칸 · 상한 5칸 → 3건째에 관통(6 ≥ 5)하고 1칸 이월.
            for (int i = 0; i < 3; i++)
                MasteryProgress.Add(_gameState, MasteryProgress.SUCCESS_GAIN);
            Assert.AreEqual(2, _gameState.playerLevel);
            Assert.AreEqual(1f, _gameState.mastery, 0.01f);
        }

        [Test]
        public void 레벨2_달성은_상자2개_해금으로_보고된다()
        {
            Assert.AreEqual("택배 상자 2개 들기", LevelPerks.PerkGainedBetween(1, 2));
            Assert.IsNull(LevelPerks.PerkGainedBetween(2, 2)); // 안 올랐으면 알리지 않는다
        }
    }
}
