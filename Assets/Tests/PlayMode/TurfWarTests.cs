// The turf loop in the town (TurfWar): your own guard on your block, the Locals' guards holding their posts on
// the next one, knocking them down, holding the ring, and the block changing hands on the HUD.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StreetEmpire.Game;
using StreetEmpire.Sim;
using StreetEmpire.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class TurfWarTests
    {
        [TearDown] public void Reset() { MuggerOpening.AutoStart = false; Time.timeScale = 1f; TurfWar.AutoStart = true; }

        [UnityTest]
        public IEnumerator TakeTheBlockNextDoor()
        {
            MuggerOpening.AutoStart = false; TurfWar.AutoStart = true; Time.timeScale = 1f;
            SceneManager.LoadScene("Eastside");
            yield return null; yield return null; yield return null;
            var s = GameSession.Instance; var turf = TurfWar.Instance; var hud = Object.FindFirstObjectByType<GameHud>();
            Assert.IsNotNull(s, "session"); Assert.IsNotNull(turf, "turf"); Assert.IsNotNull(turf.State, "turf state");
            Assert.AreEqual(9, turf.State.blocks.Count); Assert.AreEqual(1, turf.State.Owned(Turf.You));
            Assert.AreEqual(3, turf.templates.Length);
            var c0 = turf.Center(0);
            s.player.Teleport(c0 + new Vector3(0, 0.3f, 12f), 180f);
            yield return null; yield return null;
            Assert.AreEqual(0, turf.NearBlock, "on your own block");
            StringAssert.Contains("Turf 1/9", hud.sub.text);

            // your guard is on your block, and he is not a brawler
            float t = 0; while (turf.FriendlyBodies < 1 && t < 3f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(1, turf.FriendlyBodies, "your guard stands on your block");
            Assert.AreEqual(0, Brawler.All.Count, "your own guard never comes for you");

            // onto the Car Wash block, out on its sidewalk: the two Locals show up and hold their posts
            var c = turf.Center(1);
            s.player.Teleport(c + new Vector3(0, 0.3f, 21f), 180f);
            t = 0; while (turf.GuardBodies < 2 && t < 4f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(1, turf.NearBlock);
            Assert.AreEqual(2, turf.GuardBodies, "two guards on the block");
            Assert.AreEqual(2, Brawler.All.Count(b => b.posted));
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(Brawler.All.Any(b => b.Alerted), "they hold their posts while you stay off the middle");
            Assert.AreEqual(Turf.Locals, turf.State.Block(1).owner);

            // knock them down, then hold the ring
            foreach (var b in Brawler.All.ToArray()) b.GetComponent<Health>().Damage(999f, s.player.gameObject);
            yield return null; yield return null;
            Assert.AreEqual(0, turf.State.Block(1).guards, "both down");
            Assert.AreEqual(0, s.Stars, "no heat for fighting a crew");
            s.player.Teleport(new Vector3(c.x + 2.5f, 0.3f, c.z - 0.5f), 0f);   // in the ring, off the backyard fence
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(1, turf.ZoneBlock, "standing in the ring");
            Assert.Greater(turf.State.Block(1).capT, 0f, "the ring is counting");
            StringAssert.Contains("Taking Car Wash", turf.prompt.text);
            yield return new WaitForSeconds(turf.world.captureTime + 0.5f);
            Assert.AreEqual(Turf.You, turf.State.Block(1).owner, "the block is yours");
            StringAssert.Contains("Car Wash is yours", s.Message);
            yield return null;
            StringAssert.Contains("Turf 2/9", hud.sub.text);
            Assert.AreEqual(25, s.rep);
        }
    }
}
