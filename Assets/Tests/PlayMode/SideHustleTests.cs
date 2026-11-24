// The side money: stashes in the town, a race run from its start marker in a car, and a taxi fare.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StreetEmpire.Game;
using StreetEmpire.Player;
using StreetEmpire.Vehicles;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class SideHustleTests
    {
        [UnityTest, Timeout(300000)]
        public IEnumerator StashesRacesAndFaresPay()
        {
            Time.timeScale = 1f; MuggerOpening.AutoStart = false; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var s = GameSession.Instance; var player = Object.FindFirstObjectByType<PlayerMotor>(); var side = SideHustles.Instance;
            Assert.IsNotNull(side, "side hustles"); Assert.IsNotNull(side.runner);
            Assert.GreaterOrEqual(side.stashes.Length, 3, "stashes that fall in the town"); Assert.AreEqual(100, side.stashPay);
            Assert.AreEqual(3, side.races.Length, "the three races, fitted to the town"); Assert.AreEqual(side.races.Length, side.raceStarts.Length);
            float lox = Mathf.Min(s.spanX.x, s.spanX.y) - 5f, hix = Mathf.Max(s.spanX.x, s.spanX.y) + 5f, loz = Mathf.Min(s.spanZ.x, s.spanZ.y) - 5f, hiz = Mathf.Max(s.spanZ.x, s.spanZ.y) + 5f;
            foreach (var r in side.races) foreach (var q in r.points.Append(r.start))
                Assert.IsTrue(q.x > lox && q.x < hix && q.z > loz && q.z < hiz, $"{r.name} stays in town ({q})");
            for (int i = 0; i < 10; i++) yield return null;

            // a stash pays on the spot
            float cash0 = s.cash; var stash = side.stashes[0];
            Assert.IsTrue(stash.gameObject.activeSelf, "a stash to find");
            player.Teleport(stash.position + Vector3.up * 0.2f, 0); yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(1, side.StashesFound); Assert.IsFalse(stash.gameObject.activeSelf, "picked up");
            Assert.That(s.cash - cash0, Is.InRange(100f, 130f), "stashPay from jobs.json");

            // races wait until Bag Back is done, then start when a car drives into the start marker
            Assert.IsFalse(side.raceStarts[0].gameObject.activeSelf, "no races before Bag Back");
            JobBoard.Instance.SkipTo(1); yield return null; yield return null;
            Assert.IsTrue(side.raceStarts[0].gameObject.activeSelf, "race starts are marked");
            CarController sedan = Object.FindObjectsByType<CarController>(FindObjectsSortMode.None).First(c => c.name == "Sedan");
            var user = player.GetComponent<VehicleUser>();
            player.Teleport(sedan.transform.position + sedan.transform.right * 2f, 0); yield return null;
            Assert.IsTrue(user.TryEnter(), "in the sedan");
            var race = side.races[0];
            sedan.Place(race.start, 0f); yield return new WaitForFixedUpdate(); yield return null; yield return null;
            Assert.AreEqual(SideHustles.Mode.Race, side.Current, "driving into the start marker starts the race");
            Assert.IsTrue(side.Busy); Assert.AreEqual(MissionRunner.State.GetBag, side.runner.Stage);
            cash0 = s.cash;
            foreach (var q in race.points) { sedan.Place(q, 0f); yield return new WaitForFixedUpdate(); yield return null; yield return null; yield return null; }
            Assert.AreEqual(1, side.RacesRun, "every marker hit"); Assert.AreEqual("gold", side.LastMedal, "well inside the gold time");
            Assert.That(s.cash - cash0, Is.InRange(400f, 430f), "gold pays racePay.gold");
            Assert.IsFalse(side.Busy);
            user.Exit(); yield return null;
        }
    }
}
