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
            Assert.AreEqual(25, turf.State.blocks.Count); Assert.AreEqual(1, turf.State.Owned(Turf.You));
            Assert.AreEqual("reds", turf.State.Block(40).owner, "a rival on each far corner of the town");
            Assert.AreEqual("blues", turf.State.Block(4).owner); Assert.AreEqual("golds", turf.State.Block(36).owner);
            Assert.AreEqual(3, turf.templates.Length);
            Assert.AreEqual(9, turf.crewTemplates.Length, "three bodies for each rival crew");
            CollectionAssert.AreEqual(new[] { "reds", "blues", "golds" }, turf.crewTemplateIds);
            Assert.IsTrue(turf.crewTemplates.All(x => x != null && x.GetComponent<Brawler>() != null && x.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMaterial.name.StartsWith(turf.crewTemplateIds[System.Array.IndexOf(turf.crewTemplates, x) / 3])), "each crew wears its own bodies");
            var c0 = turf.Center(0);
            s.player.Teleport(c0 + new Vector3(0, 0.3f, 12f), 180f);
            yield return null; yield return null;
            Assert.AreEqual(0, turf.NearBlock, "on your own block");
            StringAssert.Contains("Turf 1/25", hud.sub.text);

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

            // knock them down, then hold the block (no ring: anywhere on its own ground, inside the sidewalk)
            foreach (var b in Brawler.All.ToArray()) b.GetComponent<Health>().Damage(999f, s.player.gameObject);
            yield return null; yield return null;
            Assert.AreEqual(0, turf.State.Block(1).guards, "both down");
            Assert.AreEqual(0, s.Stars, "no heat for fighting a crew");
            Assert.AreEqual(-1, turf.ZoneBlock, "the sidewalk does not count as holding the block");
            Assert.AreEqual(0f, turf.State.Block(1).capT, "nothing counts from the sidewalk");
            Assert.IsNull(GameObject.Find("Turf Ring 1"), "no ring on the ground");
            Assert.IsFalse(Brawler.All.Any(b => b.transform.Find("Crew Ring") != null), "no ring at anyone's feet");
            s.player.Teleport(new Vector3(c.x + 5.5f, 0.3f, c.z - 6.2f), 0f);   // 8.3 m off the middle: on the block, outside the old 7 m ring
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(1, turf.ZoneBlock, "standing on the block");
            Assert.Greater(turf.State.Block(1).capT, 0f, "the hold is counting");
            StringAssert.Contains("Holding Car Wash", turf.prompt.text);
            yield return new WaitForSeconds(turf.world.captureTime + 0.5f);
            Assert.AreEqual(Turf.You, turf.State.Block(1).owner, "the block is yours");
            StringAssert.Contains("Car Wash is yours", s.Message);
            yield return null;
            StringAssert.Contains("Turf 2/25", hud.sub.text);
            Assert.AreEqual(25, s.rep);

            // NPC v4: on the Red Kings' corner the guards are Red Kings, not the plain bodies
            var cr = turf.Center(40);
            s.player.Teleport(cr + new Vector3(0, 0.3f, 21f), 180f);
            t = 0; while (!Brawler.All.Any(b => b.posted && b.name.StartsWith("Red Kings")) && t < 4f) { t += Time.deltaTime; yield return null; }
            var redGuards = Brawler.All.Where(b => b.posted && b.name.StartsWith("Red Kings")).ToArray();
            Assert.Greater(redGuards.Length, 0, "Red Kings guards on their block");
            // wave A: it is their only block, so the first of them is the boss, and he takes more knocking down
            Assert.AreEqual(3, turf.crewBossTemplates.Length, "a boss for each rival crew");
            var boss = redGuards.FirstOrDefault(b => b.name.StartsWith("Red Kings boss"));
            // (the sim has been running: if the Red Kings took a second block by now, he is not out)
            Assert.AreEqual(turf.State.Owned("reds") == 1, boss != null, "the Red Kings boss is out exactly when this is their last block");
            if (boss != null)
            {
                Assert.AreEqual("boss-redkings", boss.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial.name);
                Assert.AreEqual(turf.bossHp, boss.GetComponent<Health>().max, 0.01f);
            }
            Assert.IsTrue(redGuards.Where(b => b != boss).All(b => b.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial.name.StartsWith("reds-")), "the rest wear the Red Kings bodies");
        }
    }
}
