// The Eastside build-out: street furniture, signs, signals, lot fill and parked cars are there, use URP
// materials made from the Sovereign Asset finished props, stay out of the buildings and leave the
// pedestrians' sidewalk loop clear.
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
    public class PropTests
    {
        static PropMarker[] props;

        IEnumerator Load()
        {
            SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            props = Object.FindObjectsByType<PropMarker>(FindObjectsSortMode.None);
        }

        int Count(string kind) => props.Count(p => p.kind == kind);

        [UnityTest]
        public IEnumerator EveryKindOfPropIsPlaced()
        {
            yield return Load();
            Assert.GreaterOrEqual(Count("streetlight"), 30, "a street lamp on each block side");
            Assert.GreaterOrEqual(Count("hydrant"), 9, "a hydrant per block");
            Assert.AreEqual(16, Count("traffic signal"), "four signals at each of the four middle crossings");
            Assert.GreaterOrEqual(Count("stop sign"), 20, "stop signs at the outer crossings");
            Assert.AreEqual(16, Count("street name sign"), "a street name post at every crossing");
            Assert.AreEqual(32, Count("street name blade"), "two name blades on every post");
            Assert.GreaterOrEqual(Count("utility pole"), 12, "utility poles");
            Assert.GreaterOrEqual(Count("parked car"), 8, "parked cars");
            Assert.GreaterOrEqual(Count("tree"), 40, "palms in the lots, the yards and along the curbs");
            Assert.GreaterOrEqual(Count("manhole"), 24, "manholes");
            Assert.Greater(Count("wood fence"), 0, "wood fences"); Assert.Greater(Count("chain link fence"), 0, "chain link");
            Assert.Greater(Count("dumpster"), 0, "dumpsters"); Assert.Greater(Count("bench"), 0, "benches");
            Assert.Greater(Count("trash can"), 0, "trash cans"); Assert.Greater(Count("mailbox"), 0, "mailboxes");
            Assert.Greater(Count("newspaper box"), 0, "newspaper boxes"); Assert.Greater(Count("bus stop"), 0, "a bus stop");
            Assert.Greater(Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).Length, 20, "power lines strung between the poles");
            var marks = GameObject.Find("Markings White");
            Assert.IsNotNull(marks, "crosswalks and stop bars");
            Assert.Greater(marks.GetComponent<MeshFilter>().sharedMesh.vertexCount, 16 * 4 * 16 * 4 / 2, "zebra stripes on the crossings");
            Debug.Log($"[PropTests] {props.Length} props: " + string.Join(", ", props.GroupBy(p => p.kind).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}")));
        }

        [UnityTest]
        public IEnumerator PropsUseTheirUrpMaterials()
        {
            yield return Load();
            var inst = Object.FindFirstObjectByType<PropInstancer>();
            Assert.IsNotNull(inst, "the props draw through the instancer");
            foreach (var p in props) Assert.AreEqual(0, p.GetComponentsInChildren<Renderer>().Length, $"{p.name} has no renderer of its own");
            foreach (var b in inst.batches)
            {
                Assert.IsNotNull(b.lod0, $"{b.name} mesh");
                Assert.AreEqual(b.materials.Length, b.lod0.subMeshCount, $"{b.name}: one submesh per material");
                if (b.lod1) Assert.AreEqual(b.lod0.subMeshCount, b.lod1.subMeshCount, $"{b.name}: LOD1 keeps the submesh order");
                foreach (var m in b.materials)
                {
                    Assert.IsNotNull(m, $"{b.name} material");
                    Assert.IsTrue(m.shader.name.StartsWith("Universal Render Pipeline/"), $"{b.name} uses URP (got {m.shader.name})");
                    Assert.IsTrue(m.enableInstancing, $"{b.name} material {m.name} instances");
                    // prop materials come from the texture manifest: named <prop>__<glTF material>
                    if (!b.name.StartsWith("car-")) StringAssert.Contains("__", m.name, $"{b.name} material {m.name} was made from the manifest");
                }
            }
            // every placed prop is drawn by some batch
            int kinds = props.Where(p => p.kind != "parked car").Select(p => p.model).Distinct().Count(m => !inst.batches.Any(b => b.name.Split('@', '#')[0] == m));
            Assert.AreEqual(0, kinds, "a batch for every placed prop model");
            Assert.AreEqual(props.Length, inst.batches.Sum(b => b.matrices.Length), "one instance per placed prop");
            var hb = inst.batches.First(b => b.name == "hydrant");
            Assert.IsNotNull(hb.lod1, "hydrant LOD1");
            var hydrant = hb.materials[0];
            Assert.IsNotNull(hydrant.GetTexture("_BaseMap"), "hydrant base color");
            Assert.IsNotNull(hydrant.GetTexture("_BumpMap"), "hydrant normal map");
            Assert.IsNotNull(hydrant.GetTexture("_MetallicGlossMap"), "hydrant metallic smoothness map");
            Assert.IsTrue(inst.batches.Any(b => b.name.StartsWith("car-sedan") && b.lod1 != null), "parked sedans have a LOD1");
            for (int i = 0; i < 3; i++) yield return null;
            if (SystemInfo.supportsInstancing) Assert.Greater(inst.LastDraws, 0, "the instancer draws");
        }

        [UnityTest]
        public IEnumerator PropsStayOutOfBuildingsAndOffThePedLoop()
        {
            yield return Load();
            var buildings = Object.FindObjectsByType<BuildingMarker>(FindObjectsSortMode.None).Select(b => b.GetComponent<BoxCollider>()).Where(c => c != null).ToArray();
            var blocks = Object.FindObjectsByType<BlockMarker>(FindObjectsSortMode.None);
            int checkedCols = 0;
            foreach (var p in props)
                foreach (var c in p.GetComponentsInChildren<Collider>())
                {
                    checkedCols++;
                    var pb = c.bounds; pb.Expand(-0.05f);
                    foreach (var b in buildings)
                        Assert.IsFalse(pb.Intersects(b.bounds), $"{p.name} at {p.transform.position} overlaps {b.name}");
                    // the pedestrians walk a rounded square 21 m out from their block center, about 0.3 m wide
                    foreach (var blk in blocks)
                    {
                        var loop = Ped.BuildLoop(blk.transform.position, 21f, 2.5f, 0.5f);
                        foreach (var q in loop)
                        {
                            var at = new Vector3(q.x, pb.center.y, q.z);
                            var cp = pb.ClosestPoint(at); cp.y = at.y;
                            Assert.Greater(Vector3.Distance(cp, at), 0.3f, $"{p.name} at {p.transform.position} blocks the sidewalk loop of block {blk.blockId}");
                        }
                    }
                }
            Assert.Greater(checkedCols, 100, "props with colliders");
        }

        [UnityTest]
        public IEnumerator TrafficLightsCycle()
        {
            yield return Load();
            var cycle = Object.FindFirstObjectByType<TrafficSignalCycle>();
            Assert.IsNotNull(cycle, "signal cycle");
            Assert.IsTrue(cycle.lenses.Length == 6 && cycle.lenses.All(m => m != null), "red, yellow and green lenses for both directions");
            Assert.AreNotEqual(cycle.lenses[2], cycle.lenses[5], "each direction lights its own lenses");
            var inst = Object.FindFirstObjectByType<PropInstancer>();
            var live = cycle.Live(2);
            Assert.IsNotNull(live, "live copy of the north-south green");
            Assert.IsTrue(inst.batches.Any(b => b.materials.Contains(live)), "the instancer draws the live lens");
            Assert.AreEqual(0, cycle.Phase(1f)); Assert.AreEqual(1, cycle.Phase(cycle.green + 1f));
            Assert.AreEqual(3, cycle.Phase(cycle.green + cycle.yellow + cycle.allRed + 1f));
            for (int i = 0; i < 10; i++) yield return null;
        }
    }
}
