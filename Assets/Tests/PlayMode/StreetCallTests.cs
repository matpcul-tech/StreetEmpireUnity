// Street calls (the web build's corner events): one is offered, stepping in starts the fight, dropping the men pays,
// and walking off ends it.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StreetEmpire.Game;
using StreetEmpire.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class StreetCallTests
    {
        [TearDown] public void Reset() { StreetCalls.AutoStart = false; JobBoard.AutoStart = true; SideHustles.AutoStart = true; MuggerOpening.AutoStart = true; Time.timeScale = 1f; }

        [UnityTest, Timeout(300000)]
        public IEnumerator ACornerCallIsOfferedFoughtAndPaid()
        {
            Time.timeScale = 1f; MuggerOpening.AutoStart = false; JobBoard.AutoStart = false; SideHustles.AutoStart = false; StreetCalls.AutoStart = true;
            SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var s = GameSession.Instance; var player = Object.FindFirstObjectByType<PlayerMotor>(); var calls = StreetCalls.Instance;
            Assert.IsNotNull(calls, "street calls"); Assert.AreEqual(36f, calls.first, "numbers from jobs.json"); Assert.AreEqual(5, calls.cap);
            Assert.AreEqual(StreetCalls.Phase.None, calls.Now, "nothing on the spawn beat");

            // the first call: a rival push, three men, offered a little way off
            yield return new WaitForSeconds(8.5f);
            calls.wait = 0f;
            float until = Time.time + 10f; while (calls.Now == StreetCalls.Phase.None && Time.time < until) yield return null;
            Assert.AreEqual(StreetCalls.Phase.Offer, calls.Now, "a call is offered"); Assert.AreEqual(StreetCalls.Kind.Incursion, calls.Current);
            Assert.AreEqual(3, calls.Men.Count, "three men push the corner");
            Assert.IsTrue(calls.Men[1].GetComponent<Brawler>().bat, "the second man carries a bat"); Assert.IsFalse(calls.Men[0].GetComponent<Brawler>().bat); Assert.IsFalse(calls.Men[2].GetComponent<Brawler>().bat);
            Assert.AreEqual(56f, calls.Men[0].max); Assert.AreEqual(60f, calls.Men[1].max); Assert.AreEqual(50f, calls.Men[2].max);
            Assert.GreaterOrEqual(Vector3.Distance(calls.Spot, player.transform.position), calls.minDist, "not on top of the player");
            Assert.IsTrue(calls.Men.All(m => m.GetComponent<Brawler>().posted), "they hold the corner until the player steps in");

            // stepping in starts the fight; dropping them pays
            float cash0 = s.cash; int rep0 = s.rep;
            player.Teleport(calls.Spot + new Vector3(4f, 0.2f, 0f), 0); yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(StreetCalls.Phase.Fight, calls.Now, "stepping in starts it");
            Assert.IsTrue(calls.Men.All(m => !m.GetComponent<Brawler>().posted), "and they come for him");
            foreach (var m in calls.Men.ToArray()) m.Damage(999f, player.gameObject);
            yield return null; yield return null;
            Assert.AreEqual(StreetCalls.Phase.None, calls.Now); Assert.AreEqual("win", calls.LastEnd); Assert.AreEqual(1, calls.Won);
            Assert.That(s.cash - cash0, Is.InRange(80f, 110f), "the incursion pays 80"); Assert.AreEqual(rep0 + 8, s.rep);
            Assert.Greater(calls.wait, 40f, "a cooldown before the next");

            // the second: two lookouts; walking off ends it and they are gone
            calls.wait = 0f;
            until = Time.time + 10f; while (calls.Now == StreetCalls.Phase.None && Time.time < until) yield return null;
            Assert.AreEqual(StreetCalls.Phase.Offer, calls.Now, "the next call"); Assert.AreEqual(StreetCalls.Kind.Ambush, calls.Current);
            Assert.AreEqual(2, calls.Men.Count, "two lookouts");
            Assert.IsTrue(calls.Men[1].GetComponent<Brawler>().bat, "one lookout has a bat"); Assert.AreEqual(58f, calls.Men[0].max);
            var lookouts = calls.Men.Select(m => m.gameObject).ToArray();
            Vector3 off = (player.transform.position - calls.Spot); off.y = 0; off = off.sqrMagnitude < 0.01f ? Vector3.forward : off.normalized;
            player.Teleport(calls.Spot + off * (StreetCalls.WalkOff + 6f) + Vector3.up * 0.2f, 0); yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(StreetCalls.Phase.None, calls.Now, "walking off ends it"); Assert.AreEqual("off", calls.LastEnd);
            yield return null;
            Assert.IsTrue(lookouts.All(g => g == null), "the lookouts pack it in");
            Assert.AreEqual(2, calls.Offered); Assert.AreEqual(1, calls.Won);
        }
    }
}
