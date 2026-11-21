// Town life: the tire shop and the gas station have activity spots a person can stand at, and the townies
// come out of their doors and walk to them without ending up inside a building.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StreetEmpire.City;
using StreetEmpire.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class TownLifeTests
    {
        [Test]
        public void OpeningHours()
        {
            Assert.IsTrue(ActivitySpot.IsOpen(7, 19, 12), "open at noon");
            Assert.IsTrue(ActivitySpot.IsOpen(7, 19, 17.8f), "open when a new game starts");
            Assert.IsFalse(ActivitySpot.IsOpen(7, 19, 20), "closed in the evening");
            Assert.IsFalse(ActivitySpot.IsOpen(7, 19, 3), "closed at night");
            Assert.IsTrue(ActivitySpot.IsOpen(20, 4, 2), "hours past midnight wrap");
            Assert.IsFalse(ActivitySpot.IsOpen(20, 4, 12));
            Assert.IsTrue(ActivitySpot.IsOpen(0, 24, 3), "always open");
        }

        static bool InBuilding(Vector3 p, float radius) =>
            Physics.OverlapSphere(p + Vector3.up, radius, ~0, QueryTriggerInteraction.Ignore).Any(c => c.GetComponentInParent<BuildingMarker>() != null);

        [UnityTest]
        public IEnumerator ShopsHaveSpotsAndPeopleUseThem()
        {
            MuggerOpening.AutoStart = false; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var life = TownLife.Instance; Assert.IsNotNull(life, "TownLife in the scene");
            var spots = life.spots;
            Assert.IsTrue(spots.Any(s => s.shop && s.label == "bay 1" && s.kind == SpotKind.Work), "the tire shop has a work spot");
            Assert.IsTrue(spots.Any(s => s.shop && s.label == "office door" && s.kind == SpotKind.Door), "the tire shop has a door");
            Assert.AreEqual(2, spots.Count(s => s.shop && s.label.StartsWith("pump") && s.kind == SpotKind.Visit), "the gas station has two pumps to stand at");
            Assert.IsTrue(spots.Any(s => s.kind == SpotKind.Door && !s.shop), "the houses on those blocks have front doors");
            foreach (var s in spots)
            {
                Assert.IsFalse(InBuilding(s.Stand, 0.25f), $"{s.name} is somewhere a person can stand, not inside a building");
                var d = s.Stand - s.blockCenter;
                Assert.Less(Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.z)), 22f, $"{s.name} is on its block, not in the street");
            }

            Assert.That(life.townies.Count, Is.InRange(1, life.maxTownies), "townies spawned, under the cap");
            Assert.IsTrue(life.townies.Any(t => t.role == Townie.Role.Worker), "the shops have staff");
            // the shops are two blocks from the start: keep everyone awake however far the player is
            life.sleepDistance = life.wakeDistance = 100000f;
            float t0 = Time.time; bool used = false;
            while (Time.time - t0 < 40f)
            {
                yield return new WaitForSeconds(0.5f);
                foreach (var t in life.townies)
                {
                    if (!t.gameObject.activeSelf) continue;
                    if (t.State == Townie.Mode.Use && t.Target != null) used = true;
                    if (t.GetComponent<CharacterController>().enabled)
                        Assert.IsFalse(InBuilding(t.transform.position, 0.15f), $"{t.name} ({t.State}) is inside a building at {t.transform.position}");
                }
            }
            float walked = life.townies.Max(t => t.Walked);
            Debug.Log($"[TownLifeTests] {spots.Length} spots, {life.townies.Count} townies, farthest walked {walked:0.0} m, a spot was used: {used}");
            Assert.Greater(walked, 3f, "people came out and walked");
            Assert.IsTrue(used, "someone stood at a work spot, a pump or the wait by the bay");
        }
    }
}
