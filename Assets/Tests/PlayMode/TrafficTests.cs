// Moving traffic: cars come in on the streets around the player, drive, and keep the rules.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StreetEmpire.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class TrafficTests
    {
        [TearDown] public void Reset() { TrafficFlow.AutoStart = false; Time.timeScale = 1f; }

        [UnityTest, Timeout(300000)]
        public IEnumerator TrafficDrivesTheStreets()
        {
            Time.timeScale = 1f; MuggerOpening.AutoStart = false; TrafficFlow.AutoStart = true; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var s = GameSession.Instance; var flow = TrafficFlow.Instance; Assert.IsNotNull(flow, "the traffic flow");
            yield return new WaitForSeconds(9f);
            Assert.GreaterOrEqual(flow.Cars.Count, 3, "cars on the streets"); Assert.LessOrEqual(flow.Cars.Count, flow.max);
            foreach (var c in flow.Cars)
            {
                Assert.IsTrue(c.obeyLights, "traffic keeps the rules");
                Assert.IsNotNull(c.GetComponentInChildren<MeshRenderer>(), "it has a body");
                var q = c.Position;
                Assert.IsTrue(s.streetX.Any(x => Mathf.Abs(x - q.x) < 0.5f) || s.streetZ.Any(z => Mathf.Abs(z - q.z) < 0.5f), "it is on a street");
            }
            var was = flow.Cars.Select(c => c.Position).ToArray();
            yield return new WaitForSeconds(6f);
            int moved = 0; for (int i = 0; i < was.Length && i < flow.Cars.Count; i++) if (flow.Cars[i] != null && Vector3.Distance(was[i], flow.Cars[i].Position) > 4f) moved++;
            Assert.GreaterOrEqual(moved, 1, "traffic moves (some of it may be waiting at a light)");
        }
    }
}
