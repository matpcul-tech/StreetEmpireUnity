// The opening scuffle and the web style HUD: a mugger 2.5 s in, the grab, two friends (one with a bat),
// the brawl hitting back, no heat for fighting them, "The street goes quiet" and the onboarding steps.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StreetEmpire.Game;
using StreetEmpire.Player;
using StreetEmpire.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class MuggerTests
    {
        [TearDown] public void Reset() { MuggerOpening.AutoStart = false; Time.timeScale = 1f; }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        [UnityTest]
        public IEnumerator OpeningScufflePlaysLikeTheWebGame()
        {
            MuggerOpening.AutoStart = true; Time.timeScale = 1f;
            SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var s = GameSession.Instance; var o = Object.FindFirstObjectByType<MuggerOpening>();
            Assert.IsNotNull(s, "session"); Assert.IsNotNull(o, "opening");
            Assert.AreEqual(3, o.templates.Length, "two plain bodies and one with a bat");
            Assert.IsNotNull(o.templates.Last().transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Bat"), "bat in hand");

            // the HUD from the web game
            var hud = Object.FindFirstObjectByType<GameHud>();
            Assert.IsNotNull(hud); Assert.AreEqual(5, hud.stars.Length, "five HEAT bars");
            Assert.IsNotNull(Object.FindFirstObjectByType<Minimap>(), "radar");
            yield return null;
            StringAssert.IsMatch(@"^\d\d:\d\d$", hud.clock.text);
            StringAssert.StartsWith("17:", hud.clock.text, "the clock starts at 17:48 like the web game");
            Assert.AreEqual("FISTS", hud.weapon.text);
            StringAssert.Contains("Turf", hud.sub.text);

            Assert.AreEqual(MuggerOpening.Beat.Waiting, o.Stage);
            yield return new WaitForSeconds(2.8f);
            Assert.AreEqual(MuggerOpening.Beat.Mug, o.Stage, "the mugger comes 2.5 s in");
            StringAssert.Contains("mugger is going for your bag", s.Message);
            Assert.AreEqual(1, Brawler.All.Count);
            var mug = Brawler.All[0];
            Assert.That(Flat(mug.transform.position, s.player.transform.position), Is.InRange(4f, 10f), "he starts 9 m off (6 m at the last try)");
            Assert.AreEqual(48f, mug.GetComponent<Health>().max);
            Assert.IsTrue(hud.onboardingBox.activeSelf, "1/3 stop the muggers");
            StringAssert.Contains("Stop the muggers", hud.onboarding.text);

            // he closes in and grabs; that brings his friends
            float t = 0;
            while (o.Stage == MuggerOpening.Beat.Mug && t < 4f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(MuggerOpening.Beat.Friends, o.Stage, "the friends jump in");
            Assert.AreEqual(3, Brawler.All.Count);
            Assert.AreEqual(1, Brawler.All.Count(b => b.bat), "one of them has a bat");
            Assert.AreEqual(56f, Brawler.All.First(b => b.bat).GetComponent<Health>().max);

            // they hit back; fighting them brings no heat
            t = 0; float hp0 = s.playerHealth.max;
            while (s.playerHealth.hp >= hp0 && t < 6f) { t += Time.deltaTime; yield return null; }
            Assert.Less(s.playerHealth.hp, hp0, "the brawlers land hits");
            s.playerHealth.Restore();   // keep the player up for the rest (going down ends the scuffle)
            var combat = s.player.GetComponent<PlayerCombat>();
            s.player.Teleport(mug.transform.position - mug.transform.forward * 1.2f, mug.transform.eulerAngles.y);
            yield return null; combat.scriptAttack = true; yield return null; yield return null;
            Assert.Less(Brawler.All.Min(b => b.GetComponent<Health>().hp / b.GetComponent<Health>().max), 1f, "a punch lands on one of them");
            Assert.AreEqual(0, s.Stars, "no heat for fighting muggers");

            foreach (var b in Brawler.All.ToArray()) b.GetComponent<Health>().Damage(999f, s.player.gameObject);
            yield return null; yield return null;
            Assert.AreEqual(MuggerOpening.Beat.Done, o.Stage);
            StringAssert.Contains("Unc wants a word", s.Message);
            yield return null;
            StringAssert.Contains("Talk to Unc", hud.onboarding.text, "2/3 talk to Unc");
            o.job.scriptTalk = true;
            s.player.Teleport(o.job.giver.position + o.job.giver.forward * 1.5f, 0);
            yield return null; yield return null; yield return null;
            Assert.IsFalse(hud.onboardingBox.activeSelf, "the box goes once the job starts");
        }
    }
}
