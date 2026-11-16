// M4: Unc's "Bag Back" from jobs.json end to end, and 1 to 2 star police that chase, hit and bust.
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
    public class JobAndPoliceTests
    {
        GameSession s; PlayerMotor player; MissionRunner job;

        IEnumerator Load()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            s = GameSession.Instance; player = Object.FindFirstObjectByType<PlayerMotor>(); job = Object.FindFirstObjectByType<MissionRunner>();
            Assert.IsNotNull(s, "session"); Assert.IsNotNull(player, "player"); Assert.IsNotNull(job, "Bag Back runner");
            for (int i = 0; i < 10; i++) yield return null;
        }

        [TearDown] public void Reset() => Time.timeScale = 1f;

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        [UnityTest]
        public IEnumerator BagBackPaysOnReturn()
        {
            yield return Load();
            Assert.AreEqual("Bag Back", job.mission.title);
            Assert.AreEqual("Unc", job.contact.name);
            Assert.AreEqual(90f, job.mission.time, 0.01f); Assert.AreEqual(120, job.mission.reward);
            Assert.Less(Flat(job.giver.position, s.spawn), 40f, "Unc is a short walk from spawn");
            Assert.AreEqual(MissionRunner.State.Offered, job.Stage);
            float cash0 = s.cash; int rep0 = s.rep;

            player.Teleport(job.giver.position + job.giver.forward * 1.5f, 0); yield return null;
            job.scriptTalk = true; yield return null; yield return null;
            Assert.AreEqual(MissionRunner.State.GetBag, job.Stage, "talking to Unc starts the job");
            Assert.GreaterOrEqual(Flat(job.Target, job.giver.position), 12f, "bag drops 12 m or more from Unc");
            Assert.IsTrue(job.bag.gameObject.activeSelf && job.marker.gameObject.activeSelf, "bag and marker shown");
            Assert.That(job.TimeLeft, Is.InRange(85f, 90f));

            player.Teleport(job.Target + Vector3.up * 0.2f, 0);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(MissionRunner.State.ReturnBag, job.Stage, "picked up the bag");
            Assert.IsFalse(job.bag.gameObject.activeSelf);

            player.Teleport(job.giver.position + job.giver.forward * 2f, 0);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(MissionRunner.State.Passed, job.Stage, "bag back to Unc");
            Assert.AreEqual(cash0 + 120, s.cash, 0.01f, "reward from jobs.json");
            Assert.AreEqual(rep0 + 12, s.rep);
            StringAssert.Contains("Job done: Bag Back", s.Message);
        }

        static int policeRuns;

        // M5: repeated to check for flakiness (the M4 version failed once with officers stuck at a block)
        [UnityTest, Repeat(5)]
        public IEnumerator HittingPeopleBringsStarsAndPoliceBust()
        {
            policeRuns++;
            Debug.Log($"[PoliceTest] run {policeRuns}");
            yield return Load();
            Assert.AreEqual(0, s.Stars);
            // punch a pedestrian twice: hitPed 7, hitPed 7 + koPed 8 = 22 heat, one star
            var ped = Object.FindObjectsByType<Ped>(FindObjectsSortMode.None).OrderBy(p => Flat(p.transform.position, player.transform.position)).First();
            var combat = player.GetComponent<PlayerCombat>();
            for (int i = 0; i < 2; i++)
            {
                player.Teleport(ped.transform.position - ped.transform.forward * 1.2f, ped.transform.eulerAngles.y);
                yield return null;
                combat.scriptAttack = true;
                yield return new WaitForSeconds(0.4f);
            }
            Assert.IsTrue(ped.GetComponent<Health>().Down, "two punches knock a pedestrian down (30 hp, 18 a punch)");
            Assert.AreEqual(22f, s.heat, 0.01f, "hitPed + hitPed + koPed");
            Assert.AreEqual(1, s.Stars);

            yield return new WaitForSeconds(0.5f);
            var first = s.cruisers.FirstOrDefault(c => c != null);
            Assert.IsNotNull(first, "one star sends a cruiser");
            Assert.That(first.riders, Is.InRange(1, 2), "one star: up to two officers in it");
            Assert.That(first.SpawnDistance, Is.InRange(55f, 115f), "the cruiser starts 60 to 110 m out (police.json spawnRing)");

            s.AddHeat(23); // 45: two stars, with room for the calm decay while they close in
            Assert.AreEqual(2, s.Stars);
            float cash0 = s.cash;

            // stand still and let them come: the cruiser drives in, stops within 16 m, the officers get out and hit for 9
            Time.timeScale = 4f;
            float t = 0; bool unloaded = false;
            while (s.Busts == 0 && t < 90f) { t += Time.deltaTime; unloaded |= s.cruisers.Any(c => c != null && c.Unloaded); yield return null; }
            Time.timeScale = 1f;
            string where = string.Join(" ", s.cruisers.Where(c => c != null).Select(c => $"car {Flat(c.transform.position, player.transform.position):F0}m v{c.GetComponent<StreetEmpire.Vehicles.CarController>().Speed:F1} r{c.riders}"))
                + " " + string.Join(" ", s.cops.Where(c => c != null).Select(c => $"cop {Flat(c.transform.position, player.transform.position):F0}m"));
            Assert.AreEqual(1, s.Busts, $"busted with heat (heat {s.heat:F0}, hp {s.playerHealth.hp:F0}, sent {s.CruisersSent}, {where}, t {t:F0}s)");
            Assert.IsTrue(unloaded, "officers got out of a cruiser");
            Assert.That(s.cops.Count(c => c != null) + s.cruisers.Where(c => c != null).Sum(c => c.riders), Is.InRange(1, 4), "two stars allow up to four officers");
            Assert.AreEqual(Mathf.Floor(cash0 * 0.4f), s.LastBail, 0.01f, "bail is 40% of cash");
            StringAssert.StartsWith("BUSTED", s.Message);

            yield return new WaitForSeconds(1.5f); // downTime 1.1 s, then back at the start
            Assert.Less(Flat(player.transform.position, s.spawn), 1.5f, "respawned at the start");
            Assert.AreEqual(0f, s.heat, 0.01f); Assert.AreEqual(0, s.Stars);
            Assert.AreEqual(100f, s.playerHealth.hp, 0.01f);
            Assert.IsTrue(s.cops.Where(c => c != null).All(c => c.leave), "officers leave after the bust");
            Assert.IsTrue(s.cruisers.Where(c => c != null).All(c => c.leave), "cruisers leave after the bust");
        }
    }
}
