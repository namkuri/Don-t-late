using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DontLate.Tests
{
    /// <summary>
    /// WorldEndingManager — 엔딩 발동 조건과 대열 구성 (S-104·105·107, 테스트는 S-206).
    ///
    /// 시퀀스 본체는 코루틴이라 EditMode에서 못 돌린다. 여기서 잠그는 것은 둘:
    /// ① **발동하지 않아야 할 조건에서 발동하지 않는다** — 잘못 터지면 하루가 통째로 날아가는 쪽이라
    ///    음성 경로가 곧 회귀 위험이다. 양성 경로(StartCoroutine)는 EditMode 범위 밖.
    /// ② **대열 구성 규칙** — 박말순 선두·호감도 순·정원·도감 충원. 순수 함수라 그대로 검사된다.
    /// </summary>
    public class EndingTriggerTests
    {
        private GameObject _go;
        private WorldEndingManager _ending;
        private GameStateSO _gameState;
        private readonly List<NpcSO> _created = new List<NpcSO>();

        [SetUp]
        public void SetUp()
        {
            _gameState = ScriptableObject.CreateInstance<GameStateSO>();
            _gameState.debt = 0;
            _gameState.endingPlayed = false;
            _gameState.endingMonologuePlayed = false;

            _go = new GameObject("EndingUnderTest");
            _ending = _go.AddComponent<WorldEndingManager>();
            TestSupport.SetField(_ending, "_gameState", _gameState);
            TestSupport.SetField(_ending, "_npcs", new NpcSO[0]);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_gameState);
            foreach (NpcSO npc in _created) Object.DestroyImmediate(npc);
            _created.Clear();
        }

        private NpcSO MakeNpc(string id)
        {
            NpcSO npc = ScriptableObject.CreateInstance<NpcSO>();
            npc.npcId = id;
            npc.displayName = id;
            _created.Add(npc);
            return npc;
        }

        private void Arrive(GameScene scene) => TestSupport.Invoke(_ending, "OnSceneArrived", scene);

        private List<NpcSO> PickParty() => (List<NpcSO>)TestSupport.Invoke(_ending, "PickParty");

        // ── 발동 조건 (음성 경로) ────────────────────────────

        [Test]
        public void 엔딩을_이미_봤으면_아무것도_시작하지_않는다()
        {
            _gameState.endingPlayed = true;

            Arrive(GameScene.Home);

            Assert.IsFalse(_gameState.endingMonologuePlayed);
        }

        [Test]
        public void 빚이_남아_있으면_독백이_시작되지_않는다()
        {
            _gameState.debt = 100;

            Arrive(GameScene.Home);

            Assert.IsFalse(_gameState.endingMonologuePlayed);
        }

        [Test]
        public void 독백_전에_캠프에_도착해도_시퀀스가_시작되지_않는다()
        {
            _gameState.endingMonologuePlayed = false;

            Arrive(GameScene.Camp);

            Assert.IsFalse((bool)TestSupport.GetField(_ending, "_sequenceRunning"));
        }

        [Test]
        public void 빚이_남은_채_캠프에_도착해도_시퀀스가_시작되지_않는다()
        {
            _gameState.debt = 500;
            _gameState.endingMonologuePlayed = true;

            Arrive(GameScene.Camp);

            Assert.IsFalse((bool)TestSupport.GetField(_ending, "_sequenceRunning"));
        }

        [Test]
        public void 집도_캠프도_아닌_씬_도착은_무시한다()
        {
            _gameState.endingMonologuePlayed = true;

            Arrive(GameScene.Village);
            Arrive(GameScene.FoodStreet);

            Assert.IsFalse((bool)TestSupport.GetField(_ending, "_sequenceRunning"));
        }

        // ── 대열 구성 ────────────────────────────────────────

        [Test]
        public void 대열_선두는_언제나_박말순이다()
        {
            TestSupport.SetField(_ending, "_npcs", new[] { MakeNpc("kimboss"), MakeNpc("parkmalsoon"), MakeNpc("naara") });
            _gameState.npcAffinities.Add(new NpcAffinity { npcId = "kimboss", affinity = 90 });

            List<NpcSO> party = PickParty();

            Assert.AreEqual("parkmalsoon", party[0].npcId);
        }

        [Test]
        public void 동행은_호감도_내림차순으로_선다()
        {
            TestSupport.SetField(_ending, "_npcs",
                new[] { MakeNpc("parkmalsoon"), MakeNpc("low"), MakeNpc("high"), MakeNpc("mid") });
            _gameState.npcAffinities.Add(new NpcAffinity { npcId = "low", affinity = 5 });
            _gameState.npcAffinities.Add(new NpcAffinity { npcId = "high", affinity = 80 });
            _gameState.npcAffinities.Add(new NpcAffinity { npcId = "mid", affinity = 40 });

            List<NpcSO> party = PickParty();

            Assert.AreEqual("high", party[1].npcId);
            Assert.AreEqual("mid", party[2].npcId);
            Assert.AreEqual("low", party[3].npcId);
        }

        [Test]
        public void 호감도_장부에_없어도_도감에서_충원한다_S107()
        {
            TestSupport.SetField(_ending, "_npcs",
                new[] { MakeNpc("parkmalsoon"), MakeNpc("a"), MakeNpc("b") });
            // 호감도 장부는 비어 있다

            List<NpcSO> party = PickParty();

            Assert.AreEqual(3, party.Count, "이웃이 다같이 모이는 것이 이 엔딩의 포인트다");
        }

        [Test]
        public void 대열은_박말순_포함_6명을_넘지_않는다()
        {
            var npcs = new List<NpcSO> { MakeNpc("parkmalsoon") };
            for (int i = 0; i < 10; i++) npcs.Add(MakeNpc("npc" + i));
            TestSupport.SetField(_ending, "_npcs", npcs.ToArray());

            List<NpcSO> party = PickParty();

            Assert.AreEqual(6, party.Count); // 1 + FOLLOWER_MAX(5)
        }

        [Test]
        public void 박말순은_동행_자리에_다시_들어가지_않는다()
        {
            TestSupport.SetField(_ending, "_npcs", new[] { MakeNpc("parkmalsoon"), MakeNpc("kimboss") });
            _gameState.npcAffinities.Add(new NpcAffinity { npcId = "parkmalsoon", affinity = 100 });
            _gameState.npcAffinities.Add(new NpcAffinity { npcId = "kimboss", affinity = 50 });

            List<NpcSO> party = PickParty();

            Assert.AreEqual(2, party.Count);
            Assert.AreEqual(1, party.FindAll(n => n.npcId == "parkmalsoon").Count);
        }

        [Test]
        public void 도감이_비어_있으면_대열도_빈다()
        {
            List<NpcSO> party = PickParty();

            Assert.AreEqual(0, party.Count);
        }

        // ── 감사 멘트 ────────────────────────────────────────

        [Test]
        public void 감사_멘트는_같은_npc면_항상_같은_줄이_나온다()
        {
            var first = (string)TestSupport.InvokeStatic(typeof(WorldEndingManager), "ThanksLine", "kimboss");
            var second = (string)TestSupport.InvokeStatic(typeof(WorldEndingManager), "ThanksLine", "kimboss");

            Assert.AreEqual(first, second);
            Assert.IsFalse(string.IsNullOrEmpty(first));
        }
    }
}
