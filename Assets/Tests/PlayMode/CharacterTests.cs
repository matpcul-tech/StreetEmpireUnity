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
        static string MatOf(GameObject go) => go.GetComponentInChildren<SkinnedMeshRenderer>(true)?.sharedMaterial?.name ?? "";

        /// <summary>Standing height from the posed skeleton: head bone over the lowest foot bone, plus the crown
        /// above the head bone (skinned mesh bounds are padded per bone and read 10 cm or more over).</summary>
        static float Height(GameObject go)
        {
            var a = go.GetComponentInChildren<Animator>();
            float Y(HumanBodyBones b) { var t = a.GetBoneTransform(b); return t != null ? t.position.y : float.MaxValue; }
            float feet = Mathf.Min(Y(HumanBodyBones.LeftToes), Y(HumanBodyBones.RightToes), Y(HumanBodyBones.LeftFoot) - 0.08f, Y(HumanBodyBones.RightFoot) - 0.08f);
            return Y(HumanBodyBones.Head) + 0.2f - feet;
        }

        [UnityTest]
        public IEnumerator FinishedModelsAndFightClips()
        {
            StreetEmpire.Game.MuggerOpening.AutoStart = false; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var s = GameSession.Instance; Assert.IsNotNull(s);
            var player = Object.FindFirstObjectByType<PlayerMotor>().gameObject;
            var playerMesh = player.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;

            var unc = GameObject.Find("Unc"); Assert.IsNotNull(unc, "Unc");
            StringAssert.StartsWith("unc", MatOf(unc), "Unc wears the Unc v2 model's own material");
            // the Meshy meshes are all named output_unwrapped, so compare the mesh assets, not names
            Assert.AreNotSame(playerMesh, unc.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh, "Unc is not the player model");

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
            StreetEmpire.Game.MuggerOpening.AutoStart = false; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var s = GameSession.Instance;
            // the center lines are part of the combined street mesh: no triangle of the line submesh reaches a crossing
            // v75c: the street mesh comes in chunks (block sides, yards, center lines by cell) so the lamps light it
            var streets = GameObject.Find("Streets Combined"); Assert.IsNotNull(streets, "combined street meshes");
            int boxes = 0, chunks = 0;
            foreach (var mf in streets.GetComponentsInChildren<MeshFilter>())
            {
                chunks++;
                var mesh = mf.sharedMesh; var mats = mf.GetComponent<MeshRenderer>().sharedMaterials;
                int sub = System.Array.FindIndex(mats, m => m != null && m.name.StartsWith("LaneYellow"));
                if (sub < 0) continue;
                var v = mesh.vertices; var tri = mesh.GetTriangles(sub); boxes += tri.Length / 36;
                for (int i = 0; i < tri.Length; i += 3)
                {
                    var b = new Bounds(v[tri[i]], Vector3.zero); b.Encapsulate(v[tri[i + 1]]); b.Encapsulate(v[tri[i + 2]]);
                    foreach (var x in s.streetX) foreach (var z in s.streetZ)
                    {
                        bool inside = Mathf.Abs(x - b.center.x) < b.extents.x + 3f && Mathf.Abs(z - b.center.z) < b.extents.z + 3f;
                        Assert.IsFalse(inside, $"a center line crosses the intersection at ({x:F0}, {z:F0})");
                    }
                }
            }
            Assert.Greater(boxes, 16, "center line boxes");
            Assert.Greater(chunks, 20, "street mesh split into chunks for the lamp pools");
            var tiles = GameObject.Find("Ground Tiles"); Assert.IsNotNull(tiles, "asphalt tiles");
            Assert.Greater(tiles.transform.childCount, 16, "asphalt drawn as tiles");
            // the Eastside models are in and no block repeats a building: houses are the three stucco models
            // (the clapboard and brick ones are out), told apart by model and stucco wash
            var bs = Object.FindObjectsByType<StreetEmpire.City.BuildingMarker>(FindObjectsSortMode.None);
            foreach (var g in bs.GroupBy(b => b.blockId))
                Assert.AreEqual(g.Count(), g.Select(b => b.model + "#" + b.wash).Distinct().Count(), $"block {g.Key} repeats a building: {string.Join(", ", g.Select(b => b.model + "#" + b.wash))}");
            // 25 block town: the clapboard and brick houses stand in Midtown (grid x + z over 4), never on the Eastside
            bool East(StreetEmpire.City.BuildingMarker b) => b.blockId % 9 + b.blockId / 9 <= 4;
            Assert.IsFalse(bs.Any(b => East(b) && (b.model is "house-wood-duplex" or "house-shotgun" or "house-boarded" or "house-rowhouse-pair")), "no clapboard or brick houses on the Eastside");
            Assert.IsTrue(bs.Any(b => !East(b) && b.model == "house-rowhouse-pair") && bs.Any(b => !East(b) && b.model == "apt-motel-style") && bs.Any(b => !East(b) && b.model == "store-bodega"), "the older models are back, in Midtown");
            Assert.GreaterOrEqual(bs.Select(b => b.model).Distinct().Count(), 15, "the stores, apartments and the three stucco houses are placed");
            // v75l: all seven house models of the rotation are placed; v75o: eight, house-03-duplex is back
            Assert.AreEqual(8, bs.Where(b => b.kind == "house" && East(b)).Select(b => b.model).Distinct().Count(), "eight house models on the Eastside");
            Assert.IsTrue(bs.Any(b => b.model == "store-barber") && bs.Any(b => b.model == "civic-church") && bs.Any(b => b.model == "apt-dingbat"));   // v75l: the dingbat has the motel's lot
            // v75t: the twenty wave A civic and wave B buildings are placed, each once, on landmark lots inside their blocks
            var wave = new[] { "civic-bank", "civic-church", "civic-clinic", "civic-police-station", "civic-school", "crew-bluehand-club", "crew-goldline-harbor", "crew-redkings-club", "dock-ice-house", "dock-pier-warehouse", "job-car-wash", "job-chop-shop", "job-loading-lot", "job-pool-hall", "tower-brutalist-concrete", "tower-copper-mansard", "tower-deco-setback", "tower-terracotta", "warehouse-cold-storage", "warehouse-sawtooth" };
            foreach (var wn in wave) Assert.AreEqual(1, bs.Count(x => x.model == wn), $"{wn} placed once");
            var blockAt = Object.FindObjectsByType<StreetEmpire.City.BlockMarker>(FindObjectsSortMode.None).ToDictionary(k => k.blockId, k => k.transform.position);
            foreach (var lm in bs.Where(x => wave.Contains(x.model)))
            {
                var lb = lm.GetComponent<BoxCollider>().bounds; var ctr = blockAt[lm.blockId];
                Assert.LessOrEqual(Mathf.Max(Mathf.Abs(lb.min.x - ctr.x), Mathf.Abs(lb.max.x - ctr.x), Mathf.Abs(lb.min.z - ctr.z), Mathf.Abs(lb.max.z - ctr.z)), 20.6f, $"{lm.model} stays inside block {lm.blockId}");
                foreach (var nbr in bs.Where(x => x != lm && x.blockId == lm.blockId))
                {
                    var nb = nbr.GetComponent<BoxCollider>().bounds; nb.Expand(new Vector3(-0.3f, 0f, -0.3f));
                    Assert.IsFalse(lb.Intersects(nb), $"{lm.model} overlaps {nbr.model} on block {lm.blockId}");
                }
            }
            foreach (var b in bs)
                // the neon signs hung on a building are unlit tubes with no metallic value at all; the walls are what is checked
                foreach (var m in b.GetComponentsInChildren<Renderer>().Where(r => !r.name.StartsWith("Neon ")).SelectMany(r => r.sharedMaterials))
                    Assert.AreEqual(0f, m.GetFloat("_Metallic"), 1e-4f, $"{b.model} is not metallic");
            // v75i (Matthew): no LODs or simplified meshes for the Sovereign Asset buildings, the finished file at every distance
            Assert.IsTrue(bs.All(b => b.GetComponent<LODGroup>() == null && b.transform.Find("Far") == null && b.transform.Find("Low") == null && b.transform.Find("Shadow") == null), "buildings draw their finished mesh at every distance");
            var label = GameObject.Find("BuildLabel"); Assert.IsNotNull(label, "build label on the HUD");
            StringAssert.StartsWith("v0.5", label.GetComponent<UnityEngine.UI.Text>().text);
            var sedan = Object.FindObjectsByType<CarController>(FindObjectsSortMode.None).First(c => c.name == "Sedan");
            Assert.IsTrue(sedan.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Any(m => m != null && m.name == "v2-sedan-clean_body"), "the sedan is the v2 clean sedan");
        }
    }
}
