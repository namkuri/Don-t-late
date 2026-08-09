using NUnit.Framework;
using UnityEngine;

namespace DontLate.Tests
{
    /// <summary>
    /// WorldDeadlineManager — 마감 경고·지각 판정 (S-206).
    /// 분 경계(ClockTicked)에서만 도는 판정기라 시계를 직접 먹여 검사한다.
    /// EditMode에선 OnEnable이 안 돌아 구독이 없으므로 private 핸들러를 직접 호출한다.
    /// </summary>
    public class DeadlineJudgeTests
    {
        private GameObject _go;
        private WorldDeadlineManager _deadline;
        private GameStateSO _gameState;
        private TuningConfigSO _tuning;
        private DeliveryOrderSO _order;

        private int _warnCount;
        private int _failCount;

        [SetUp]
        public void SetUp()
        {
            _gameState = ScriptableObject.CreateInstance<GameStateSO>();
            _tuning = ScriptableObject.CreateInstance<TuningConfigSO>();
            _tuning.deadlineWarnMinutes = 30f;

            _go = new GameObject("DeadlineUnderTest");
            _deadline = _go.AddComponent<WorldDeadlineManager>();
            TestSupport.SetField(_deadline, "_gameState", _gameState);
            TestSupport.SetField(_deadline, "_tuning", _tuning);

            _order = ScriptableObject.CreateInstance<DeliveryOrderSO>();
            _order.orderId = 11;
            _order.address = "행복빌라 301호";
            _order.deadlineMinuteOfDay = 600f; // 10:00
            _gameState.cargo.Add(_order);

            _warnCount = 0;
            _failCount = 0;
            WorldEvents.DeadlineWarned += OnWarned;
            WorldEvents.DeliveryFailed += OnFailed;
        }

        [TearDown]
        public void TearDown()
        {
            WorldEvents.DeadlineWarned -= OnWarned;
            WorldEvents.DeliveryFailed -= OnFailed;
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_gameState);
            Object.DestroyImmediate(_tuning);
            Object.DestroyImmediate(_order);
        }

        private void OnWarned(DeliveryData _) => _warnCount++;
        private void OnFailed(DeliveryData _) => _failCount++;

        /// <summary>시계를 minuteOfDay로 옮기고 분 경계 판정을 1회 돌린다.</summary>
        private void Tick(float minuteOfDay)
        {
            _gameState.minuteOfDay = minuteOfDay;
            var clock = new GameClock
            {
                Day = 1,
                Hour = Mathf.FloorToInt(minuteOfDay / 60f),
                Minute = Mathf.FloorToInt(minuteOfDay % 60f),
            };
            TestSupport.Invoke(_deadline, "OnClockTicked", clock);
        }

        [Test]
        public void 남은_시간은_마감빼기_현재시각()
        {
            _gameState.minuteOfDay = 570f;

            Assert.AreEqual(30f, _deadline.RemainingMinutes(_order), 0.001f);
        }

        [Test]
        public void 여유_구간에는_경고도_실패도_없다()
        {
            Tick(500f); // 남은 100분 > 경고 30분

            Assert.AreEqual(0, _warnCount);
            Assert.AreEqual(0, _failCount);
        }

        [Test]
        public void 경고_구간에_들어가면_DeadlineWarned가_발화한다()
        {
            Tick(580f); // 남은 20분 ≤ 30분

            Assert.AreEqual(1, _warnCount);
            Assert.AreEqual(0, _failCount);
        }

        [Test]
        public void 같은_건에_경고를_두_번_보내지_않는다()
        {
            Tick(580f);
            Tick(585f);
            Tick(590f);

            Assert.AreEqual(1, _warnCount);
        }

        [Test]
        public void 마감을_지나면_DeliveryFailed가_발화한다()
        {
            Tick(600f); // 남은 0분 → 지각

            Assert.AreEqual(1, _failCount);
        }

        [Test]
        public void 같은_건에_실패를_두_번_보내지_않는다()
        {
            Tick(600f);
            Tick(610f);

            Assert.AreEqual(1, _failCount);
        }

        [Test]
        public void 배치_완료건은_마감_판정에서_면제된다_S073()
        {
            _gameState.placedDeliveries.Add(new PlacedDelivery { orderId = 11, beaconAddress = "행복빌라 301호" });

            Tick(580f); // 경고 구간
            Tick(700f); // 마감 초과 구간

            Assert.AreEqual(0, _warnCount, "배치=일 끝. 방치해도 경고가 없어야 한다");
            Assert.AreEqual(0, _failCount, "배치=일 끝. 방치해도 지각이 없어야 한다");
        }

        [Test]
        public void 배송_완료_통지를_받으면_경고_기록이_풀린다()
        {
            Tick(580f);
            Assert.AreEqual(1, _warnCount);

            TestSupport.Invoke(_deadline, "OnDeliveryCompleted", DeliveryData.From(_order));
            Tick(585f);

            Assert.AreEqual(2, _warnCount, "완료 통지로 기록이 풀렸으면 같은 건을 다시 경고할 수 있어야 한다");
        }

        [Test]
        public void 적재가_비면_아무것도_판정하지_않는다()
        {
            _gameState.cargo.Clear();

            Tick(700f);

            Assert.AreEqual(0, _warnCount);
            Assert.AreEqual(0, _failCount);
        }
    }
}
