// M1: the Eastside 3 x 3 blocks load with kit buildings, streets, a sun and a camera.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StreetEmpire.City;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class EastsideTests
    {
        [UnityTest]
        public IEnumerator EastsideLoadsWithKitBuildings()
        {
            StreetEmpire.Game.MuggerOpening.AutoStart = false; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;

            var blocks = Object.FindObjectsByType<BlockMarker>(FindObjectsSortMode.None);
            Assert.AreEqual(49, blocks.Length, "the 7 x 7 town");
            Assert.AreEqual(15, blocks.Count(b => b.district == StreetEmpire.Sim.CityLayout.Eastside), "fifteen Eastside blocks");
            Assert.AreEqual(19, blocks.Count(b => b.district == StreetEmpire.Sim.CityLayout.Midtown), "nineteen Midtown blocks");
            Assert.AreEqual(14, blocks.Count(b => b.district == StreetEmpire.Sim.CityLayout.Downtown), "fourteen Downtown blocks toward the far corner");
            Assert.AreEqual(49, blocks.Select(b => b.blockId).Distinct().Count(), "every block once");
            Assert.IsTrue(blocks.All(b => b.gx < 7 && b.gz < 7), "the corner 7 x 7 of the city grid");

            var buildings = Object.FindObjectsByType<BuildingMarker>(FindObjectsSortMode.None);
            Assert.AreEqual(100, buildings.Length, "four kit buildings per block");
            foreach (var b in buildings)
            {
                var rs = b.GetComponentsInChildren<Renderer>();
                Assert.Greater(rs.Length, 0, $"{b.name} has meshes");
                foreach (var m in rs.SelectMany(r => r.sharedMaterials))
                {
                    Assert.IsNotNull(m, $"{b.name} material");
                    // URP players skip Built-in shaders, so the building would be invisible in the APK and WebGL
                    Assert.IsFalse(m.shader.name.StartsWith("glTF/") || m.shader.name == "Standard" || m.shader.name == "Hidden/InternalErrorShader",
                        $"{b.name} uses a URP shader (got {m.shader.name})");
                }
                // the collider is fitted to the model when the scene is built; renderer bounds are not
                // reliable at runtime once static batching has merged the meshes
                var col = b.GetComponent<BoxCollider>();
                Assert.IsNotNull(col, $"{b.name} has a collider");
                var bounds = col.bounds;
                // v75t: the wave A and B landmarks run up to the 45 m deco tower and the 23.6 m sawtooth warehouse
                bool landmark = b.kind == "landmark" || b.model == "civic-church";
                Assert.That(bounds.size.y, Is.InRange(2f, landmark ? 46f : 40f), $"{b.name} height");
                // v74: scaled to real heights, so footprints run from about 5 m (pawn shop) to 14 m (the motel)
                Assert.That(Mathf.Max(bounds.size.x, bounds.size.z), Is.InRange(4.5f, landmark ? 24f : 15f), $"{b.name} footprint about its 13 m lot");
                var block = blocks.First(k => k.blockId == b.blockId).transform.position;
                Assert.Less(Mathf.Abs(bounds.center.x - block.x), 20.5f, $"{b.name} inside its block (x)");
                Assert.Less(Mathf.Abs(bounds.center.z - block.z), 20.5f, $"{b.name} inside its block (z)");
                Assert.That(bounds.min.y, Is.InRange(0f, 0.6f), $"{b.name} sits on the ground");
            }
            Assert.AreEqual(4, buildings.Select(b => b.kind).Distinct().Count(), "store, house, apt and (v75t) landmark");

            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional && l.name == "Sun");   // not the character fill
            Assert.IsNotNull(sun, "sun light");
            Assert.AreNotEqual(LightShadows.None, sun.shadows);
            Assert.IsNotNull(Camera.main, "main camera");

            for (int i = 0; i < 30; i++) yield return null; // run a few frames; any error log fails the test
        }

        /// <summary>v73/v74: the character fill light, a real light per street lamp, the moon, fight feedback and the
        /// Arm guards prompt are built into the scene; the night turns the lamps and the moon on and the windows up.</summary>
        [UnityTest]
        public IEnumerator V73LightingNightAndFightPiecesAreBuilt()
        {
            StreetEmpire.Game.MuggerOpening.AutoStart = false; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var fill = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.name == "Character Fill");
            Assert.IsNotNull(fill, "character fill light");
            Assert.AreEqual(LightShadows.None, fill.shadows);
            var lamps = Object.FindFirstObjectByType<LampLights>();
            Assert.IsNotNull(lamps, "lamp lights");
            Assert.Greater(lamps.lights.Length, 8, "a light per street lamp");
            Assert.IsTrue(lamps.lights.All(l => l.type == LightType.Spot && l.range >= 11f && l.range <= 15f && l.spotAngle >= 100f
                && Vector3.Dot(l.transform.forward, Vector3.down) > 0.99f), "v75: a downward spot light about 13 m under each lamp head");
            Assert.Greater(StreetEmpire.City.DayCycle.SunElevation(14f), 45f, "v75: a high sun at 14:00");
            Assert.That(StreetEmpire.City.DayCycle.SunElevation(18.13f), Is.InRange(4f, 12f), "v75: the built golden hour sun at about 18:08");
            Assert.Less(StreetEmpire.City.DayCycle.SunElevation(19f), 0f, "v75: the sun is down by 19:00");
            Assert.IsTrue(StreetEmpire.City.DayCycle.WindowLight.r > StreetEmpire.City.DayCycle.WindowLight.g && StreetEmpire.City.DayCycle.WindowLight.g > StreetEmpire.City.DayCycle.WindowLight.b
                && StreetEmpire.City.DayCycle.WindowIntensity <= 2f, "v75: warm, dim window light");
            Assert.IsNull(GameObject.Find("Lamp Pools"), "no lamp pool decals");
            var moon = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(l => l.name == "Moon");
            Assert.IsNotNull(moon, "moon light");
            Assert.AreEqual(0f, StreetEmpire.City.DayCycle.WindowRamp(12f), "windows off at noon");
            Assert.AreEqual(1f, StreetEmpire.City.DayCycle.WindowRamp(22f), "windows full at 22:00");
            Assert.AreEqual(0.5f, StreetEmpire.City.DayCycle.WindowRamp(18.75f), 1e-4f, "windows half way up at 18:45");
            Assert.AreEqual(0.5f, StreetEmpire.City.DayCycle.WindowRamp(6.25f), 1e-4f, "windows half way down at 06:15");
            Assert.IsNotNull(Object.FindFirstObjectByType<StreetEmpire.Game.CombatFx>(), "fight feedback");
            Assert.IsNotNull(Object.FindFirstObjectByType<StreetEmpire.Game.ArmGuards>(), "Arm guards prompt");
            StreetEmpire.Game.GameClock.Frozen = true; StreetEmpire.Game.GameClock.FrozenHour = 22f;
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsTrue(lamps.lights.Any(l => l != null && l.enabled), "lamp lights on at night");
            // v75c: every quality tier shades the lamps per pixel (per vertex never lit the big ground faces)
            var tiers = Enumerable.Range(0, QualitySettings.count).Select(i => QualitySettings.GetRenderPipelineAssetAt(i)).Append(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline).Where(a => a != null).ToArray();
            Assert.Greater(tiers.Length, 0, "URP assets");
            foreach (var a in tiers)
            {
                var mode = a.GetType().GetProperty("additionalLightsRenderingMode")?.GetValue(a)?.ToString();
                Assert.AreEqual("PerPixel", mode, $"v75c: {a.name} additional lights per pixel");
            }
            Assert.LessOrEqual(lamps.lights.Count(l => l != null && l.enabled), lamps.maxOn, "v75c: only the nearest lamps lit (URP's visible light cap)");
            Assert.IsNotNull(lamps.LiveGlobe, "v75c: the lamp globe material is driven");
            Assert.Greater(lamps.LiveGlobe.GetColor("_EmissionColor").maxColorComponent, 1f, "v75c: globes glow at night");
            foreach (var hh in new[] { 2.13f, 5.92f })
            {
                StreetEmpire.Game.GameClock.FrozenHour = hh;
                for (int i = 0; i < 5; i++) yield return null;
                Assert.IsTrue(lamps.lights.Any(l => l != null && l.enabled && l.intensity > 25f), $"v75c: lamps cast light at {hh:0.00}");
            }
            StreetEmpire.Game.GameClock.FrozenHour = 22f;
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsTrue(moon.enabled, "moon on at night");
            Assert.Less(moon.intensity, 0.2f, "v75: a dim moon");
            var sunL = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(l => l.name == "Sun");
            Assert.IsFalse(sunL.enabled, "v75: no sunlight at 22:00");
            StreetEmpire.Game.GameClock.FrozenHour = 19f;
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsFalse(sunL.enabled, "v75: no sunlight at 19:00 (the blue hour)");
            Assert.AreNotEqual(RenderSettings.fogColor, Color.clear);
            var fog19 = RenderSettings.fogColor;
            StreetEmpire.Game.GameClock.FrozenHour = 22f;
            for (int i = 0; i < 5; i++) yield return null;
            Assert.Greater(fog19.b - RenderSettings.fogColor.b, 0.05f, "v75: 19:00 and 22:00 differ (the blue hour is lighter)");
            StreetEmpire.Game.GameClock.FrozenHour = 12f;
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsFalse(lamps.lights.Any(l => l != null && l.enabled), "lamp lights off by day");
            Assert.Less(lamps.LiveGlobe.GetColor("_EmissionColor").maxColorComponent, 0.01f, "v75c: globes dark by day");
            foreach (var hh in new[] { 7.33f, 8.38f, 9.6f })
            {
                StreetEmpire.Game.GameClock.FrozenHour = hh;
                for (int i = 0; i < 5; i++) yield return null;
                Assert.IsFalse(lamps.lights.Any(l => l != null && l.enabled), $"v75c: lamps off at {hh:0.00}");
                Assert.Less(lamps.LiveGlobe.GetColor("_EmissionColor").maxColorComponent, 0.01f, $"v75c: globes dark at {hh:0.00}");
            }
            StreetEmpire.Game.GameClock.FrozenHour = 12f;
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsFalse(moon.enabled, "moon off by day");
            Assert.IsTrue(sunL.enabled && sunL.intensity > 1.2f, "v75: full sun at midday");
            StreetEmpire.Game.GameClock.Frozen = false; StreetEmpire.Game.GameClock.FrozenHour = StreetEmpire.Game.GameClock.StartHour;
        }
    }
}
