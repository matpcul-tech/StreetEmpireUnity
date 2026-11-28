// Wrapped round every test in this assembly by the test framework's run callback. Two things that kept turning one problem into several red tests:
// - a test that failed half way left its switches thrown (the clock frozen at 22:00, street life on), and the tests
//   after it failed for that alone. Every switch is put back as it stood before the test, pass or fail.
// - tests pick street spots with Random, so a run could land beside a wall one time and in the open the next. Each
//   test now starts from the same seed, so a run repeats far more closely.
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using StreetEmpire.City;
using StreetEmpire.Game;
using UnityEngine;
using UnityEngine.TestRunner;

[assembly: TestRunCallback(typeof(StreetEmpire.Tests.TestGuard))]

namespace StreetEmpire.Tests
{
    /// <summary>The test framework calls this round every test of the run (TestRunCallback).</summary>
    public class TestGuard : ITestRunCallback
    {
        static bool sideHustles, blockLife, traffic, calls, mugger, jobs, dice, persist, turf, frozen, culler;
        static float hour, timeScale; static int jacked;
        /// <summary>How many tests this has wrapped (a test checks it is really running).</summary>
        public static int Wrapped { get; private set; }

        public void RunStarted(ITest testsToRun) { }
        public void RunFinished(ITestResult testResults) { }

        public void TestStarted(ITest test)
        {
            if (test.IsSuite) return;
            sideHustles = SideHustles.AutoStart; blockLife = BlockLife.AutoStart; traffic = TrafficFlow.AutoStart; calls = StreetCalls.AutoStart;
            mugger = MuggerOpening.AutoStart; jobs = JobBoard.AutoStart; dice = StreetDice.AutoStart; persist = TurfWar.Persist; turf = TurfWar.AutoStart;
            frozen = GameClock.Frozen; hour = GameClock.FrozenHour; culler = BuildingCuller.On; timeScale = Time.timeScale; jacked = Carjack.Count;
            Random.InitState(20261127);
            Wrapped++;
        }

        public void TestFinished(ITestResult result)
        {
            if (result.Test.IsSuite) return;
            SideHustles.AutoStart = sideHustles; BlockLife.AutoStart = blockLife; TrafficFlow.AutoStart = traffic; StreetCalls.AutoStart = calls;
            MuggerOpening.AutoStart = mugger; JobBoard.AutoStart = jobs; StreetDice.AutoStart = dice; TurfWar.Persist = persist; TurfWar.AutoStart = turf;
            GameClock.Frozen = frozen; GameClock.FrozenHour = hour; BuildingCuller.On = culler; Time.timeScale = timeScale; Carjack.Count = jacked;
        }
    }

    public class TestGuardTests
    {
        [Test, Order(1)]
        public void LeavesSwitchesThrown()
        {
            Assert.Greater(TestGuard.Wrapped, 0, "the guard wraps the tests in this assembly");
            // thrown on purpose and not put back: the guard has to do it
            GameClock.Frozen = !GameClock.Frozen; BuildingCuller.On = !BuildingCuller.On; StreetDice.AutoStart = !StreetDice.AutoStart;
            thrown = true; frozenWas = !GameClock.Frozen; cullerWas = !BuildingCuller.On; diceWas = !StreetDice.AutoStart;
        }

        static bool thrown, frozenWas, cullerWas, diceWas;

        [Test, Order(2)]
        public void AndFindsThemPutBack()
        {
            Assert.IsTrue(thrown, "the first half ran");
            Assert.AreEqual(frozenWas, GameClock.Frozen, "the clock"); Assert.AreEqual(cullerWas, BuildingCuller.On, "the culling"); Assert.AreEqual(diceWas, StreetDice.AutoStart, "street dice");
        }
    }
}
