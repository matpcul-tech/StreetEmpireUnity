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
            StreetEmpire.Game.MuggerOpening.AutoStart = false; SceneManager.LoadScene("Eastside");
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
            Assert.AreEqual(64, Count("traffic signal"), "four signals at each of the sixteen middle crossings");
            Assert.GreaterOrEqual(Count("stop sign"), 20, "stop signs at the outer crossings");
            Assert.AreEqual(36, Count("street name sign"), "a street name post at every crossing");
            Assert.AreEqual(72, Count("street name blade"), "two name blades on every post");
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

        /// <summary>v75: no traffic signal pole stands on the roadway. The pole is found in the mesh the instancer
        /// actually draws (its vertices under 1 m), moved by every placement matrix, and must land on a corner
        /// sidewalk: 19 m to 22 m from a block center on both axes (the curb face is at 22.15 m).</summary>
        [UnityTest]
        public IEnumerator TrafficSignalPolesStandOnTheSidewalk()
        {
            yield return Load();
            var inst = Object.FindFirstObjectByType<PropInstancer>();
            var blocks = Object.FindObjectsByType<BlockMarker>(FindObjectsSortMode.None).Select(b => b.transform.position).ToArray();
            int n = 0;
            foreach (var b in inst.batches.Where(b => b.name.StartsWith("traffic-signal")))
            {
                Assert.IsTrue(b.lod0.isReadable, "signal mesh readable for the check");
                var sum = Vector3.zero; int k = 0;
                foreach (var v in b.lod0.vertices) if (v.y < 1f) { sum += v; k++; }
                Assert.Greater(k, 0, "the signal mesh has a pole");
                var foot = sum / k;
                foreach (var m in b.matrices)
                {
                    var w = m.MultiplyPoint3x4(foot); n++;
                    bool onWalk = blocks.Any(c => { float dx = Mathf.Abs(w.x - c.x), dz = Mathf.Abs(w.z - c.z); return dx <= 22.0f && dz <= 22.0f && Mathf.Max(dx, dz) >= 19f; });
                    Assert.IsTrue(onWalk, $"traffic signal pole at {w} is on a corner sidewalk, not the roadway");
                }
            }
            Assert.AreEqual(64, n, "all 64 signal poles checked");
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
            var carBatches = inst.batches.Where(b => b.name.StartsWith("car-")).ToArray();
            Assert.GreaterOrEqual(carBatches.Select(b => b.name.Split('@', '#')[0]).Distinct().Count(), 8, "the curbs hold a mix of the v2 cars");
            Assert.IsTrue(carBatches.All(b => b.lod1 != null), "every parked car has a far version");
            // v75t: the bodies came out in camouflage on the phone. Their normal maps were images inside the GLB,
            // which Android reads wrongly; the cars carry no normal map now and their color maps are texture files.
            var carMats = carBatches.SelectMany(b => b.materials).Where(m => m != null).Distinct().ToArray();
            Assert.IsTrue(carMats.All(m => m.GetTexture("_BumpMap") == null && !m.IsKeywordEnabled("_NORMALMAP")), "no car material has a normal map");
            var painted = carMats.Where(m => m.name.Contains("_body") || m.name.EndsWith("_wheel")).ToArray();
            Assert.GreaterOrEqual(painted.Length, 16, "body and wheel materials for the parked cars");
            Assert.IsTrue(painted.All(m => m.GetTexture("_BaseMap") != null), "every body and wheel has its color map");
            // v75u: the cruiser and the taxi carry lettering, and every car's glass has the glass map
            foreach (var kind in new[] { "police", "taxi" })
            {
                var decal = carMats.FirstOrDefault(m => m.name == $"v2-{kind}_decal");
                Assert.IsNotNull(decal, $"{kind} has a lettering panel");
                Assert.AreEqual($"{kind}_decal", decal.GetTexture("_BaseMap")?.name, $"{kind} lettering map");
                Assert.IsTrue(decal.IsKeywordEnabled("_ALPHATEST_ON"), $"{kind} lettering is cut out");
            }
            // v75u paint colors: the same cars in more than their own paint, each paint its own body map
            var repaints = carMats.Where(m => m.name.Contains("_body-")).ToArray();
            Assert.GreaterOrEqual(repaints.Length, 12, "repainted cars at the curbs");
            Assert.IsTrue(repaints.All(m => m.GetTexture("_BaseMap") != null && m.GetTexture("_BaseMap").name.Contains("_body_") && m.GetTexture("_BumpMap") == null), "each repaint has its own body map and no normal map");
            Assert.IsFalse(carMats.Any(m => m.name.StartsWith("v2-police_body-") || m.name.StartsWith("v2-taxi_body-") || m.name.StartsWith("v2-van_body-")), "the cruiser, the taxi and the van keep their one paint");
            // wave C: the three new cars are in the curb mix, and some of the long vehicles have a side to themselves
            var carKinds = carBatches.Select(b => b.name.Split('@', '#')[0]).Distinct().ToArray();
            foreach (var car in new[] { "car-hatchback", "car-minivan", "car-luxury-sedan" }) Assert.Contains(car, carKinds, "wave C car at the curb");
            var big = new[] { "city-bus", "tow-truck", "ambulance", "garbage-truck", "school-bus", "ice-cream-truck", "limo", "armored-truck", "dump-truck", "fire-engine", "swat-van", "semi-tractor", "semi-trailer" };
            Assert.GreaterOrEqual(big.Count(b => carKinds.Contains("car-" + b)), 4, "long wave C vehicles parked");
            Assert.IsFalse(carKinds.Contains("car-box-truck"), "the box truck is held back");
            var waveC = carMats.Where(m => m.name.EndsWith("_paint") || m.name.EndsWith("_trim")).ToArray();
            Assert.Greater(waveC.Length, 6); Assert.IsTrue(waveC.All(m => m.GetTexture("_BaseMap") != null && m.GetTexture("_BaseMap").name.EndsWith("_body")), "wave C paint and trim read the body map");
            var glass = carMats.Where(m => m.name.EndsWith("_glass")).ToArray();
            Assert.GreaterOrEqual(glass.Length, 8, "glass on the parked cars");
            Assert.IsTrue(glass.All(m => m.GetTexture("_BaseMap") != null && m.GetTexture("_BaseMap").name == "glass"), "every car's glass has the glass map");
            Assert.IsTrue(carBatches.Any(b => b.name.StartsWith("car-cruiser") || b.name.StartsWith("car-dirt-bike") || b.name.StartsWith("car-sport-bike")), "motorcycles at the curb");
            for (int i = 0; i < 3; i++) yield return null;
            if (SystemInfo.supportsInstancing) Assert.Greater(inst.LastDraws, 0, "the instancer draws");
        }

        [UnityTest]
        public IEnumerator PropsStayOutOfBuildingsAndOffThePedLoop()
        {
            yield return Load();
            var buildings = Object.FindObjectsByType<BuildingMarker>(FindObjectsSortMode.None).Select(b => b.GetComponent<BoxCollider>()).Where(c => c != null).ToArray();
            var blocks = Object.FindObjectsByType<BlockMarker>(FindObjectsSortMode.None);
            // the loops are made once, not once per collider: on 25 blocks that ran past the 180 s limit
            var loops = blocks.Select(blk => (blk.blockId, loop: Ped.BuildLoop(blk.transform.position, 21f, 2.5f, 0.5f).ToArray())).ToArray();
            int checkedCols = 0;
            foreach (var p in props)
                foreach (var c in p.GetComponentsInChildren<Collider>())
                {
                    checkedCols++;
                    var pb = c.bounds; pb.Expand(-0.05f);
                    foreach (var b in buildings)
                        if (pb.Intersects(b.bounds)) Assert.Fail($"{p.name} at {p.transform.position} overlaps {b.name}");
                    // the pedestrians walk a rounded square 21 m out from their block center, about 0.3 m wide
                    foreach (var (blockId, loop) in loops)
                    {
                        foreach (var q in loop)
                        {
                            var at = new Vector3(q.x, pb.center.y, q.z);
                            var cp = pb.ClosestPoint(at); cp.y = at.y;
                            if (Vector3.Distance(cp, at) <= 0.3f) Assert.Fail($"{p.name} at {p.transform.position} blocks the sidewalk loop of block {blockId}");
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
            // v75c: one lens lit per head in every phase, the off lenses dark glass, and the green a green (not cyan)
            for (int ph = 0; ph < 6; ph++)
            {
                cycle.Show(ph);
                for (int g = 0; g < 2; g++)
                {
                    int lit = Enumerable.Range(0, 3).Count(k => cycle.Live(g * 3 + k).GetColor("_EmissionColor").maxColorComponent > 0.5f);
                    Assert.AreEqual(1, lit, $"phase {ph}, group {g}: one lens lit");
                }
            }
            Assert.IsTrue(cycle.lenses.All(m => m.GetColor("_BaseColor").maxColorComponent < 0.2f), "v75c: unlit lenses are dark");
            Assert.Greater(cycle.greenOn.g, cycle.greenOn.b * 4f, "v75c: signal green, not cyan");
            for (int i = 0; i < 10; i++) yield return null;
        }
    }
}
