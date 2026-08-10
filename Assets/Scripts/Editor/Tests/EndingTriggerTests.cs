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
        private readonly List<GameObject> _createdModels = new List<GameObject>();

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
            foreach (GameObject model in _createdModels) Object.DestroyImmediate(model);
            _createdModels.Clear();
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

        private List<WorldEndingManager.EndingCastEntry> PickParty()
            => (List<WorldEndingManager.EndingCastEntry>)TestSupport.Invoke(_ending, "PickParty");

        private static string NpcIdOf(WorldEndingManager.EndingCastEntry entry) => entry.npcId;

        /// <summary>명부를 깐다 — 인물마다 **다른** 더미 모델을 준다(1인 1종 판정을 통과시키기 위해).</summary>
        private void SetCast(params (string id, string name)[] rows)
        {
            var cast = new WorldEndingManager.EndingCastEntry[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                GameObject model = new GameObject("TestModel_" + rows[i].id);
                _createdModels.Add(model);
                cast[i] = new WorldEndingManager.EndingCastEntry
                {
                    npcId = rows[i].id, displayName = rows[i].name, model = model,
                };
            }
            TestSupport.SetField(_ending, "_cast", cast);
        }

        /// <summary>두 인물이 **같은 모델**을 쓰는 경우 — 중복 제거를 확인하기 위한 구성.</summary>
        private void SetCastSharingOneModel(params (string id, string name)[] rows)
        {
            GameObject shared = new GameObject("TestModel_Shared");
            _createdModels.Add(shared);
            var cast = new WorldEndingManager.EndingCastEntry[rows.Length];
            for (int i = 0; i < rows.Length; i++)
                cast[i] = new WorldEndingManager.EndingCastEntry
                {
                    npcId = rows[i].id, displayName = rows[i].name, model = shared,
                };
            TestSupport.SetField(_ending, "_cast", cast);
        }

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

        // ── 대열 구성 (S-228에서 계약이 바뀌었다) ────────────
        // 종전: 호감도 장부 상위 + 도감 충원, 상한 6명.
        // 이후: **모델을 가진 인물 전원**이 1인 1종으로 선다(남규님 "1마리씩 다 나오게").
        //       호감도는 선두 다음 자리의 **순서**에만 쓴다. 명부의 출처가 도감(NpcSO) →
        //       빌더가 채우는 `_cast`로 바뀌었다 — 도감에 없는 아트 NPC(오지혜·나아라)를 세우기 위해서다.

        [Test]
        public void 대열_선두는_언제나_박말순이다()
        {
            SetCast(("kimboss", "김사장"), ("parkmalsoon", "박말순"), ("naara", "나아라"));
            _gameState.npcAffinities.Add(new NpcAffinity { npcId = "kimboss", affinity = 90 });

            var party = PickParty();

            Assert.AreEqual("parkmalsoon", NpcIdOf(party[0]));
        }

        [Test]
        public void 동행은_호감도_내림차순으로_선다()
        {
            SetCast(("parkmalsoon", "박말순"), ("low", "로우"), ("high", "하이"), ("mid", "미드"));
            _gameState.npcAffinities.Add(new NpcAffinity { npcId = "low", affinity = 5 });
            _gameState.npcAffinities.Add(new NpcAffinity { npcId = "high", affinity = 80 });
            _gameState.npcAffinities.Add(new NpcAffinity { npcId = "mid", affinity = 40 });

            var party = PickParty();

            Assert.AreEqual("high", NpcIdOf(party[1]));
            Assert.AreEqual("mid", NpcIdOf(party[2]));
            Assert.AreEqual("low", NpcIdOf(party[3]));
        }

        [Test]
        public void 호감도가_없어도_모델이_있으면_전원_선다_S228()
        {
            SetCast(("parkmalsoon", "박말순"), ("a", "가"), ("b", "나"));
            // 호감도 장부는 비어 있다

            var party = PickParty();

            Assert.AreEqual(3, party.Count, "이웃이 다같이 모이는 것이 이 엔딩의 포인트다");
        }

        [Test]
        public void 모델이_있는_인물은_상한_없이_전원_선다_S228()
        {
            var rows = new List<(string, string)> { ("parkmalsoon", "박말순") };
            for (int i = 0; i < 10; i++) rows.Add(("npc" + i, "행인" + i));
            SetCast(rows.ToArray());

            var party = PickParty();

            Assert.AreEqual(11, party.Count, "종전 6명 상한은 S-228에서 폐기됐다");
        }

        [Test]
        public void 박말순은_동행_자리에_다시_들어가지_않는다()
        {
            SetCast(("parkmalsoon", "박말순"), ("kimboss", "김사장"));
            _gameState.npcAffinities.Add(new NpcAffinity { npcId = "parkmalsoon", affinity = 100 });
            _gameState.npcAffinities.Add(new NpcAffinity { npcId = "kimboss", affinity = 50 });

            var party = PickParty();

            Assert.AreEqual(2, party.Count);
            Assert.AreEqual(1, party.FindAll(e => NpcIdOf(e) == "parkmalsoon").Count);
        }

        [Test]
        public void 같은_모델은_두_번_서지_않는다_S228()
        {
            SetCastSharingOneModel(("parkmalsoon", "박말순"), ("twin", "쌍둥이"));

            var party = PickParty();

            Assert.AreEqual(1, party.Count, "1인 1종 — 같은 fbx가 두 번 서면 대열이 복제로 보인다");
        }

        [Test]
        public void 명부가_비어_있으면_대열도_빈다()
        {
            var party = PickParty();

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
