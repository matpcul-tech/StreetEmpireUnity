// Block life: a group stands on a corner of the blocks near the player, and goes when the player is far away.
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
    public class BlockLifeTests
    {
        [TearDown] public void Reset() { BlockLife.AutoStart = false; Time.timeScale = 1f; }

        [UnityTest, Timeout(300000)]
        public IEnumerator PeopleStandOnTheCornersNearThePlayer()
        {
            Time.timeScale = 1f; MuggerOpening.AutoStart = false; BlockLife.AutoStart = true; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var s = GameSession.Instance; var player = Object.FindFirstObjectByType<PlayerMotor>(); var life = BlockLife.Instance;
            Assert.IsNotNull(life, "block life");
            yield return new WaitForSeconds(1.5f);
            Assert.GreaterOrEqual(life.groups.Count, 12, "most blocks have a clear corner for a group");
            Assert.That(life.AwakeGroups, Is.InRange(1, life.maxAwake), "the groups near the player are out");
            var near = life.groups.First(g => g.on);
            Assert.AreEqual(life.perGroup, near.people.Count);
            foreach (var who in near.people)
            {
                Assert.IsTrue(who.activeSelf); Assert.IsNull(who.GetComponent<Townie>(), "they stay put");
                Assert.IsNotNull(who.GetComponent<Loiterer>()); Assert.IsNotNull(who.GetComponent<NpcBody>());
                Assert.Less(Vector3.Distance(who.transform.position, near.at), 2.5f, "standing together");
            }
            // a group far from the player is not out
            var far = life.groups.OrderByDescending(g => Vector3.Distance(g.at, player.transform.position)).First();
            Assert.IsFalse(far.on, "the far side of town sleeps");
            player.Teleport(far.at + new Vector3(4f, 0.2f, 4f), 0); yield return new WaitForSeconds(1f);
            Assert.IsTrue(far.on, "walk up and they are there"); Assert.IsFalse(near.on && Vector3.Distance(near.at, far.at) > 110f, "and the ones left behind go");
        }
    }
}
