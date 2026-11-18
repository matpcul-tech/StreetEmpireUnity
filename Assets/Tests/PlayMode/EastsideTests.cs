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
            Assert.AreEqual(9, blocks.Length, "3 x 3 Eastside blocks");
            Assert.IsTrue(blocks.All(b => b.district == 0), "all blocks are Eastside");

            var buildings = Object.FindObjectsByType<BuildingMarker>(FindObjectsSortMode.None);
            Assert.AreEqual(36, buildings.Length, "four kit buildings per block");
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
                Assert.That(bounds.size.y, Is.InRange(2f, 40f), $"{b.name} height");
                Assert.That(Mathf.Max(bounds.size.x, bounds.size.z), Is.InRange(8f, 15f), $"{b.name} fits its 13 m lot");
                var block = blocks.First(k => k.blockId == b.blockId).transform.position;
                Assert.Less(Mathf.Abs(bounds.center.x - block.x), 20.5f, $"{b.name} inside its block (x)");
                Assert.Less(Mathf.Abs(bounds.center.z - block.z), 20.5f, $"{b.name} inside its block (z)");
                Assert.That(bounds.min.y, Is.InRange(0f, 0.6f), $"{b.name} sits on the ground");
            }
            Assert.AreEqual(3, buildings.Select(b => b.kind).Distinct().Count(), "store, house and apt");

            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional && l.name == "Sun");   // not the character fill
            Assert.IsNotNull(sun, "sun light");
            Assert.AreNotEqual(LightShadows.None, sun.shadows);
            Assert.IsNotNull(Camera.main, "main camera");

            for (int i = 0; i < 30; i++) yield return null; // run a few frames; any error log fails the test
        }

        /// <summary>v73: the character fill light, the lamp pools and their lights, fight feedback and the Arm
        /// guards prompt are all built into the scene, and the night turns the lamp lights on.</summary>
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
            Assert.Greater(lamps.lamps.Length, 8, "a lamp head per street lamp");
            Assert.IsNotNull(Object.FindFirstObjectByType<StreetEmpire.Game.CombatFx>(), "fight feedback");
            Assert.IsNotNull(Object.FindFirstObjectByType<StreetEmpire.Game.ArmGuards>(), "Arm guards prompt");
            StreetEmpire.Game.GameClock.Frozen = true; StreetEmpire.Game.GameClock.FrozenHour = 22f;
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsTrue(lamps.lights.Any(l => l != null && l.enabled), "lamp lights on at night");
            StreetEmpire.Game.GameClock.FrozenHour = 12f;
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsFalse(lamps.lights.Any(l => l != null && l.enabled), "lamp lights off by day");
            StreetEmpire.Game.GameClock.Frozen = false; StreetEmpire.Game.GameClock.FrozenHour = StreetEmpire.Game.GameClock.StartHour;
        }
    }
}
