// Night look: the night post volume and road sheen follow the clock, traffic carries lights, and only a few
// storefront spills burn at once.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StreetEmpire.City;
using StreetEmpire.Game;
using StreetEmpire.Vehicles;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class NightLookTests
    {
        [TearDown] public void Reset() { TrafficFlow.AutoStart = false; GameClock.Frozen = false; Time.timeScale = 1f; }

        [UnityTest, Timeout(300000)]
        public IEnumerator TheNightChangesTheStreet()
        {
            Time.timeScale = 1f; MuggerOpening.AutoStart = false; TrafficFlow.AutoStart = true; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var look = Object.FindFirstObjectByType<NightLook>(); Assert.IsNotNull(look, "the night look");
            Assert.IsTrue(look.Ready, "its post volume and night profile"); Assert.IsNotNull(look.road, "the road material");
            var cycle = Object.FindFirstObjectByType<DayCycle>();
            Assert.Greater(cycle.nightLights.Length, DayCycle.MaxStorefronts, "a spill for every signed store");

            var neon = Object.FindFirstObjectByType<NeonGlow>(); Assert.IsNotNull(neon, "the neon glow");
            Assert.IsNotNull(neon.steady, "the neon material"); Assert.IsNotNull(neon.steady.GetTexture("_BaseMap"), "the neon atlas");
            var signs = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(r => r.name.StartsWith("Neon ")).ToArray();
            Assert.GreaterOrEqual(signs.Length, 14, "neon on the towers, clubs, stores and apartments");
            Assert.IsTrue(signs.Any(r => r.name == "Neon CalderGrand"), "the Calder Grand's sign");

            GameClock.Frozen = true; GameClock.FrozenHour = 14f;
            for (int i = 0; i < 5; i++) yield return null;
            float daySheen = look.road.GetFloat("_Smoothness");
            Assert.AreEqual(0f, look.Level, 1e-3f, "no night look at 14:00"); Assert.AreEqual(0f, look.Weight, 1e-3f);
            Assert.AreEqual(0, cycle.nightLights.Count(l => l != null && l.enabled), "storefront spills off by day");
            Assert.Less(neon.Level, 0.2f, "neon barely lit by day");

            GameClock.FrozenHour = 22f;
            yield return new WaitForSeconds(9f);
            Assert.Greater(look.Weight, 0.95f, "the night volume is in at 22:00");
            Assert.Greater(neon.Level, 1.5f, "neon full at night"); Assert.Greater(neon.steady.GetColor("_BaseColor").maxColorComponent, 1.5f, "bright enough to bloom");
            Assert.Greater(look.road.GetFloat("_Smoothness"), daySheen + 0.2f, "the road takes a sheen at night");
            int lit = cycle.nightLights.Count(l => l != null && l.enabled);
            Assert.That(lit, Is.InRange(1, DayCycle.MaxStorefronts), "only the nearest storefront spills are lit");
            var flow = TrafficFlow.Instance; Assert.IsNotNull(flow); Assert.GreaterOrEqual(flow.Cars.Count, 1, "traffic is out");
            foreach (var c in flow.Cars) { var lamps = c.GetComponent<CarLamps>(); Assert.IsNotNull(lamps, "traffic has lamps"); Assert.IsTrue(lamps.On, "traffic drives with its lights on at night"); }

            GameClock.FrozenHour = 14f;
            for (int i = 0; i < 5; i++) yield return null;
            Assert.AreEqual(daySheen, look.road.GetFloat("_Smoothness"), 1e-3f, "and the road is dry again by day");
            Assert.IsTrue(flow.Cars.All(c => !c.GetComponent<CarLamps>().On), "lights off by day");
        }
    }
}
