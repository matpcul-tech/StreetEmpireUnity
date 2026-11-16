// M5: the finished models in place of the tinted placeholders, the fight clips on the shared controller,
// center lines that stop at the crossings, and the painted sedan.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StreetEmpire.Game;
using StreetEmpire.Player;
using StreetEmpire.Vehicles;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class CharacterTests
    {
        static string MeshOf(GameObject go) => go.GetComponentInChildren<SkinnedMeshRenderer>(true)?.sharedMesh?.name ?? "";
        static string MatOf(GameObject go) => go.GetComponentInChildren<SkinnedMeshRenderer>(true)?.sharedMaterial?.name ?? "";

        static float Height(GameObject go)
        {
            var rs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
            return b.size.y;
        }

        [UnityTest]
        public IEnumerator FinishedModelsAndFightClips()
        {
            SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var s = GameSession.Instance; Assert.IsNotNull(s);
            var player = Object.FindFirstObjectByType<PlayerMotor>().gameObject;
            string playerMesh = MeshOf(player);

            var unc = GameObject.Find("Unc"); Assert.IsNotNull(unc, "Unc");
            StringAssert.StartsWith("unc", MatOf(unc), "Unc wears the Unc v2 model's own material");
            Assert.AreNotEqual(playerMesh, MeshOf(unc), "Unc is not the player model");

            var peds = Object.FindObjectsByType<Ped>(FindObjectsSortMode.None);
            Assert.AreEqual(4, peds.Length);
            Assert.AreEqual(4, peds.Select(p => MatOf(p.gameObject)).Distinct().Count(), "four different pedestrian models");
            Assert.IsTrue(peds.All(p => MatOf(p.gameObject).StartsWith("ped-")), "pedestrians wear the Locals models: " + string.Join(",", peds.Select(p => MatOf(p.gameObject))));

            Assert.AreEqual(2, s.copTemplates.Length, "cop-m-1 and cop-m-2");
            CollectionAssert.AreEquivalent(new[] { "cop-m-1", "cop-m-2" }, s.copTemplates.Select(MatOf).ToArray());
            Assert.IsNotNull(s.cruiserTemplate, "police cruiser template");
            Assert.IsNotNull(s.cruiserTemplate.GetComponent<PoliceCruiser>());

            foreach (var go in new[] { unc, player }.Concat(peds.Select(p => p.gameObject)))
            {
                var a = go.GetComponentInChildren<Animator>();
                Assert.IsTrue(a.isHuman, $"{go.name} is Humanoid");
                var names = a.parameters.Select(p => p.name).ToArray();
                CollectionAssert.IsSubsetOf(new[] { "Speed", "Punch", "PunchKind", "Hit", "Down" }, names, $"{go.name} has the fight parameters");
                Assert.That(Height(go), Is.InRange(1.5f, 2.0f), $"{go.name} height");
            }

            // knock a pedestrian down: the Down state plays, and it gets back up
            var ped = peds[0]; var anim = ped.GetComponentInChildren<Animator>();
            ped.GetComponent<Health>().Damage(100, player);
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(anim.GetBool("Down"), "Down set on knockout");
            Assert.IsTrue(anim.GetCurrentAnimatorStateInfo(0).IsName("Down") || anim.GetNextAnimatorStateInfo(0).IsName("Down"), "the knockdown clip plays");

            // a punch plays the jab or cross state
            var pa = player.GetComponentInChildren<Animator>();
            player.GetComponent<PlayerCombat>().scriptAttack = true;
            yield return new WaitForSeconds(0.15f);
            var st = pa.GetCurrentAnimatorStateInfo(0); var nx = pa.GetNextAnimatorStateInfo(0);
            Assert.IsTrue(st.IsName("Punch Jab") || st.IsName("Punch Cross") || nx.IsName("Punch Jab") || nx.IsName("Punch Cross"), "punch clip plays");
        }

        [UnityTest]
        public IEnumerator CenterLinesStopAtCrossingsAndSedanIsPainted()
        {
            SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var s = GameSession.Instance;
            var lines = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("Line ")).ToArray();
            Assert.Greater(lines.Length, 16);
            foreach (var l in lines)
            {
                var c = l.position; var h = l.lossyScale * 0.5f;
                foreach (var x in s.streetX) foreach (var z in s.streetZ)
                {
                    bool inside = Mathf.Abs(x - c.x) < h.x + 3f && Mathf.Abs(z - c.z) < h.z + 3f;
                    Assert.IsFalse(inside, $"{l.name} crosses the intersection at ({x:F0}, {z:F0})");
                }
            }
            var sedan = Object.FindObjectsByType<CarController>(FindObjectsSortMode.None).First(c => c.name == "Sedan");
            Assert.IsTrue(sedan.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Any(m => m != null && m.name.EndsWith("_painted")), "sedan wears the painted atlas");
        }
    }
}
