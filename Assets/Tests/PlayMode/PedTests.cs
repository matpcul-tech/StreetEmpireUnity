// Pedestrians walk laps of their sidewalk upright, feet on the slab, with a full walk cycle.
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
    public class PedTests
    {
        [UnityTest]
        public IEnumerator PedsWalkUprightOnTheSidewalk()
        {
            SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var peds = Object.FindObjectsByType<Ped>(FindObjectsSortMode.None);
            Assert.AreEqual(4, peds.Length);
            var start = peds.Select(p => p.transform.position).ToArray();
            yield return new WaitForSeconds(3f);
            float maxLean = 0, maxHover = 0, minParam = 99; int samples = 0;
            for (int k = 0; k < 24; k++)
            {
                yield return new WaitForSeconds(0.25f);
                yield return new WaitForEndOfFrame();
                foreach (var p in peds)
                {
                    var body = p.GetComponent<NpcBody>();
                    var pos = p.transform.position;
                    var d = pos - p.blockCenter;
                    float ring = Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.z));
                    Assert.That(ring, Is.InRange(Ped.SidewalkInner + 0.2f, Ped.SidewalkOuter - 0.1f), $"{p.name} stays on the sidewalk (ring {ring:0.00})");
                    Assert.IsTrue(Physics.Raycast(pos + Vector3.up * 0.5f, Vector3.down, out var hit, 2f), "ground under the ped");
                    maxHover = Mathf.Max(maxHover, Mathf.Abs(pos.y - hit.point.y));
                    if (body.Speed > 0.8f)
                    {
                        maxLean = Mathf.Max(maxLean, ShotDirector.Lean(body.animator, p.transform));
                        minParam = Mathf.Min(minParam, body.animator.GetFloat("Speed"));
                        samples++;
                    }
                }
            }
            Debug.Log($"[PedTests] max lean {maxLean:0.0} deg, max hover {maxHover:0.000} m, min walk blend {minParam:0.00}, walking samples {samples}");
            Assert.Greater(samples, 40, "peds keep walking");
            Assert.Less(maxLean, 14f, "upright, not hunched forward");
            Assert.Less(maxHover, 0.05f, "feet on the sidewalk, not floating or sunk");
            Assert.GreaterOrEqual(minParam, 1.7f, "a full walk cycle, not walk half blended with idle");
            for (int i = 0; i < peds.Length; i++) Assert.Greater(Vector3.Distance(start[i], peds[i].transform.position), 5f, $"{peds[i].name} moved along its loop");
        }
    }
}
