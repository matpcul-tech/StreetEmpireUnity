// The jobs after Bag Back: Dee, Rook and Sal stand in the town, the board offers the jobs in jobs.json order and
// runs each kind (a drop run, a tail, an escape, a hit, holding blocks).
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
    public class JobBoardTests
    {
        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        [UnityTest, Timeout(300000)]
        public IEnumerator TheBoardRunsTheLaterJobs()
        {
            Time.timeScale = 1f; MuggerOpening.AutoStart = false; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var s = GameSession.Instance; var player = Object.FindFirstObjectByType<PlayerMotor>();
            var runner = Object.FindFirstObjectByType<MissionRunner>(); var board = JobBoard.Instance;
            Assert.IsNotNull(board, "the job board"); Assert.AreSame(runner, board.runner);
            Assert.AreEqual(19, board.missions.Count, "every job from jobs.json"); Assert.AreEqual(4, board.contacts.Count);
            Assert.AreEqual(4, board.contactNodes.Length);
            Assert.AreEqual(4, board.contactNodes.Select(n => n.name).Distinct().Count(), "Unc, Dee, Rook and Sal each stand somewhere");
            foreach (var n in board.contactNodes) Assert.IsNotNull(n.GetComponent<CharacterController>(), n.name + " is a person in the town");
            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(MissionRunner.State.Offered, runner.Stage, "Bag Back first");
            Assert.IsFalse(runner.External, "the board waits for Bag Back");

            // past Bag Back: Unc's next job, a drop run on a clock
            board.SkipTo(1); yield return null; yield return null;
            Assert.AreEqual("Corner Collection", board.Current.title); Assert.AreEqual("Unc", runner.contact.name);
            Assert.AreEqual(MissionRunner.State.Offered, runner.Stage); StringAssert.Contains("Unc", runner.Objective);
            float cash0 = s.cash;
            board.scriptTalk = true; yield return null; yield return null;
            Assert.IsTrue(board.Running, "talking starts the job"); Assert.AreEqual(MissionRunner.State.GetBag, runner.Stage);
            for (int i = 0; i < board.Current.need; i++)
            {
                Assert.IsTrue(runner.marker.gameObject.activeSelf, "a marker on the drop");
                player.Teleport(board.Target + Vector3.up * 0.2f, 0); yield return new WaitForSeconds(0.25f);
            }
            Assert.AreEqual(MissionRunner.State.ReturnBag, runner.Stage, "every drop picked up");
            player.Teleport(runner.giver.position + runner.giver.forward * 2f, 0); yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(1, board.Done, "back to Unc"); // the home block earns a little while the job runs, so the reward is a floor
            Assert.That(s.cash - cash0, Is.InRange(200f, 230f), "the reward from jobs.json");
            StringAssert.Contains("Job done: Corner Collection", s.Message);
            Assert.AreEqual("Chop Job", board.Current.title, "the next job is on offer");

            // Dee gives the driving jobs from her own spot
            board.SkipTo(6); yield return null;
            Assert.AreEqual("Dee", runner.contact.name); Assert.AreEqual("Dee", runner.giver.name);
            Assert.Greater(Flat(runner.giver.position, board.contactNodes[0].position), 30f, "Dee is not at Unc's");
            board.scriptTalk = true; yield return null; yield return null;
            Assert.IsNotNull(board.Mark, "a marked car to follow");
            Vector3 was = board.Mark.Position; yield return new WaitForSeconds(1f);
            Assert.Greater(Vector3.Distance(was, board.Mark.Position), 3f, "it drives");

            // Rook: lose the police
            board.SkipTo(12); yield return null; board.scriptTalk = true; yield return null; yield return null;
            Assert.IsNull(board.Mark, "the last job's car is gone"); Assert.Greater(s.heat, 10f, "wanted");
            int done = board.Done; s.heat = 0f; yield return new WaitForSeconds(1.3f);
            Assert.AreEqual(done + 1, board.Done, "no heat left: the job is done");

            // Rook: one named man
            board.SkipTo(13); yield return null; board.scriptTalk = true; yield return null; yield return null;
            Assert.AreEqual(1, board.Hostiles.Count, "the lieutenant is on the street");
            done = board.Done; board.Hostiles[0].Damage(9999f, player.gameObject); yield return null; yield return null;
            Assert.AreEqual(done + 1, board.Done, "he is down");

            // Sal: hold six blocks
            board.SkipTo(17); yield return null; board.scriptTalk = true; yield return null; yield return null;
            Assert.IsTrue(board.Running); StringAssert.Contains("Hold 6 blocks", runner.Objective);
            s.heat = 0f;
        }
    }
}
