// Street furniture, signs, lot fill and road markings for the Eastside, placed from the block layout.
// Every model comes from Assets/Art/Models/Props: Poly Haven CC0 props and our own modeled props, all
// finished and checked in Sovereign Asset (docs/sovereign-assets/props). Their textures are pulled out by
// tools/extract_prop_textures.mjs so Unity compresses them; this file makes the URP materials from that manifest.
//
// Layout, per block (Unity space, distances from the block center; the sidewalk ring runs 19 to 22 m and the
// pedestrians walk a rounded loop at 21 m, so nothing with a collider sits between 20.4 and 21.6 m):
//   curb line 21.75 m: street lamps, hydrants, utility poles, stop signs, signals, street name posts
//   inner line 19.6 m, only in the gap between the two corner buildings of a side: benches, bins, boxes, bus stop
//   the open lots between the buildings (7 to 19 m in, 13 m wide): pocket park, parking lot, fenced vacant
//   lot, fenced side yard or service yard, dealt so neighbors differ; the backyards in the middle get trees,
//   fences and clutter
//   the street: parked cars 24 m out, manholes, crosswalks and stop bars at every crossing (one mesh)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StreetEmpire.City;
using StreetEmpire.Game;
using StreetEmpire.Vehicles;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace StreetEmpire.EditorTools
{
    public static class PropBuilder
    {
        const string Models = "Assets/Art/Models/Props";
        const string Tex = "Assets/Art/Textures/Props";
        const string MatDir = "Assets/Art/Materials/Props";
        const string MeshDir = "Assets/Art/Meshes";

        public const float SidewalkTop = 0.22f, YardTop = 0.24f, LotTop = 0.255f;
        public const float CurbLine = 21.75f, InnerLine = 19.6f, ParkLine = 24.1f;
        // street names: the east-west streets (constant z, south to north) and the north-south avenues (constant x)
        public static readonly string[] XStreetNames = { "E 31ST ST", "E 33RD ST", "E 35TH ST", "E 37TH ST" };
        public static readonly string[] ZStreetNames = { "KING AVE", "GARFIELD AVE", "PRAIRIE AVE", "INDIANA AVE" };

        [Serializable] class Entry { public string prop, material, alpha, basecolor, normal, metalsmooth; public float cutoff, metallic, roughness; public float[] color, tiling; }
        [Serializable] class Manifest { public Entry[] materials; }

        enum Col { None, Bounds, Pole }
        static Dictionary<string, Entry> manifest;
        static readonly Dictionary<string, Material> mats = new();
        static readonly Dictionary<string, int> counts = new();
        static long tris;
        static List<(Vector3 p, float r)> keep;
        static System.Random rng;
        static TrafficSignalCycle signals;
        static readonly Dictionary<string, PropBatch> batches = new();
        static readonly Dictionary<string, List<Matrix4x4>> placed = new();

        public struct BlockInfo { public int id; public Vector3 center; }

        public static void Build(IList<BlockInfo> blocks, float[] streetX, float[] streetZ, Transform parent)
        {
            LoadManifest(); mats.Clear(); counts.Clear(); tris = 0; rng = new System.Random(1931);
            batches.Clear(); placed.Clear();
            KeepClear();
            var root = new GameObject("Props").transform; root.SetParent(parent, false);
            var cycle = new GameObject("Traffic Signals"); cycle.transform.SetParent(root, false);
            signals = cycle.AddComponent<TrafficSignalCycle>();
            var instancer = new GameObject("Prop Instancer").AddComponent<PropInstancer>(); instancer.transform.SetParent(root, false);

            var marks = new Marks();
            foreach (var b in blocks)
            {
                var bt = new GameObject($"Block {b.id} props").transform; bt.SetParent(root, false);
                for (int k = 0; k < 4; k++) Side(b, k, bt, marks);
                Backyards(b, bt);
                Joints(b, marks);
            }
            var streets = new GameObject("Street props").transform; streets.SetParent(root, false);
            Intersections(blocks, streetX, streetZ, streets, marks);
            Manholes(streetX, streetZ, streets);
            PowerLines(blocks, streets);
            foreach (var b in blocks) for (int k = 0; k < 4; k++) Grime(b, k, StreetAt(b, k, streetX, streetZ), marks);
            Graffiti(marks);
            marks.Save(root);

            foreach (var kv in batches) kv.Value.matrices = placed[kv.Key].ToArray();
            instancer.batches = batches.Values.Where(b => b.matrices.Length > 0).OrderBy(b => b.name).ToArray();
            signals.instancer = instancer;
            Material Lens(string group, string color) => batches.TryGetValue("traffic-signal@mx#" + group, out var sb)
                ? sb.materials.FirstOrDefault(m => m != null && m.name.Contains("lens_" + color)) : null;
            signals.lenses = new[] { Lens("A", "red"), Lens("A", "yellow"), Lens("A", "green"), Lens("B", "red"), Lens("B", "yellow"), Lens("B", "green") };
            int draws = instancer.batches.Sum(b => b.materials.Length * (b.lod1 ? 2 : 1));
            Debug.Log($"[PropBuilder] {counts.Values.Sum()} props, {tris} triangles at full detail, {instancer.batches.Length} instanced models " +
                $"({draws} draws at most): " + string.Join(", ", counts.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key} {kv.Value}")));
            foreach (var b in instancer.batches)
                Debug.Log($"[PropBuilder] {b.name}: {b.matrices.Length} x {MeshMerge.Triangles(b.lod0)} tris, {b.materials.Length} materials, " +
                    $"lod1 {(b.lod1 ? MeshMerge.Triangles(b.lod1) + " tris at " + b.lod1Distance + " m" : "none")}, drawn to {b.cullDistance} m, shadows {(b.castShadows ? "to " + b.shadowDistance + " m" : "off")}");
        }

        // ---------------------------------------------------------------- per block side

        static Vector3 N(int k) => k switch { 0 => Vector3.right, 1 => Vector3.left, 2 => Vector3.forward, _ => Vector3.back };
        static Vector3 T(int k) { var n = N(k); return new Vector3(-n.z, 0, n.x); }
        static Vector3 At(BlockInfo b, int k, float d, float s, float y) { var p = b.center + N(k) * d + T(k) * s; p.y = y; return p; }
        static Quaternion Face(Vector3 dir) => Quaternion.LookRotation(dir, Vector3.up);
        static float Jit(float a) => (float)(rng.NextDouble() * 2 - 1) * a;

        static void Side(BlockInfo b, int k, Transform parent, Marks marks)
        {
            var n = N(k); var t = T(k); int deal = b.id * 4 + k;
            // a lamp per side, staggered so facing sides do not line up
            Place("street-lamp-1", "streetlight", b.id, At(b, k, CurbLine, k % 2 == 0 ? -10 : 10, SidewalkTop), Face(-n), parent, Col.Bounds, 0f);
            // street palms along the curb like the original web Eastside (two slots a side, about a third left
            // empty); the east curb carries the utility poles at 14 m, so its palms stand nearer the middle
            for (int i = 0; i < 2; i++)
            {
                if ((deal * 7 + i * 3) % 3 == 0) continue;
                float s = (i == 0 ? -1 : 1) * (k == 0 ? 6.5f : 12.8f) + Jit(0.6f);
                Place((deal + i) % 3 == 0 ? "palm-short" : "palm-tall", "tree", b.id, At(b, k, CurbLine, s, SidewalkTop), Quaternion.Euler(0, Jit(180), 0), parent, Col.Pole, 0f, pole: 0.25f);
            }
            if (k == b.id % 4) Place(b.id % 3 == 0 ? "hydrant-aged" : "hydrant", "hydrant", b.id, At(b, k, CurbLine, 3.2f, SidewalkTop), Face(-n) * Quaternion.Euler(0, Jit(25), 0), parent, Col.Bounds, 0.01f);

            bool bus = (b.id == 1 && k == 3) || (b.id == 19 && k == 2) || (b.id == 9 && k == 1);
            if (bus)
            {
                Place("bus-shelter", "bus stop", b.id, At(b, k, InnerLine, 0, SidewalkTop), Face(n), parent, Col.Bounds, 0.01f);
                Place("bus-stop-sign", "bus stop sign", b.id, At(b, k, CurbLine, -3.2f, SidewalkTop), Face(t), parent, Col.Pole, 0.01f, pole: 0.05f);
            }
            else switch (deal % 5)
            {
                case 0: case 4:
                    Place("bench", "bench", b.id, At(b, k, InnerLine, -2.2f, SidewalkTop), Face(n), parent, Col.Bounds, 0.012f);
                    Place("trash-can", "trash can", b.id, At(b, k, InnerLine, -0.2f, SidewalkTop), Quaternion.Euler(0, Jit(180), 0), parent, Col.Bounds, 0.012f);
                    break;
                case 1:
                    Place("newspaper-box", "newspaper box", b.id, At(b, k, InnerLine, -1.6f, SidewalkTop), Face(n), parent, Col.Bounds, 0.012f);
                    Place("newspaper-box", "newspaper box", b.id, At(b, k, InnerLine, -1.0f, SidewalkTop), Face(n), parent, Col.Bounds, 0.012f);
                    Place("mailbox", "mailbox", b.id, At(b, k, InnerLine, 1.4f, SidewalkTop), Face(n), parent, Col.Bounds, 0.012f);
                    break;
                case 2:
                    Place("trash-can-rust", "trash can", b.id, At(b, k, InnerLine, -3f, SidewalkTop), Quaternion.Euler(0, Jit(180), 0), parent, Col.Bounds, 0.012f);
                    break;
                case 3:
                    Place("utility-box-2", "utility box", b.id, At(b, k, InnerLine, 2.4f, SidewalkTop), Face(n), parent, Col.Bounds, 0.012f);
                    break;
            }

            Lot(b, k, deal, parent, marks);

            // parked cars on about half the sides, in the curb lane, nose either way
            if ((b.id + k) % 2 == 0)
            {
                float s = (deal % 3 - 1) * 7f + Jit(1.5f);
                var dir = deal % 2 == 0 ? t : -t;
                string kind = deal % 7 == 0 ? "police" : "sedan";
                Car(kind, deal % 3 != 1, b, At(b, k, ParkLine, s, 0), Face(dir), parent);
                if (deal % 4 == 1) Car("sedan", deal % 2 == 1, b, At(b, k, ParkLine, s + (deal % 2 == 0 ? -6.2f : 6.2f), 0), Face(dir), parent);
            }
        }

        /// <summary>The open lot between the two corner buildings of a side.</summary>
        static void Lot(BlockInfo b, int k, int deal, Transform parent, Marks marks)
        {
            var n = N(k); var t = T(k);
            Vector3 L(float d, float s, float y = YardTop) => At(b, k, d, s, y);
            int type = Pick(b.id, k);
            switch (type)
            {
                case 0: // pocket park: two palms, a hedge along the walk, a bench looking out
                    Place("palm-tall", "tree", b.id, L(12.5f, -3f), Quaternion.Euler(0, Jit(180), 0), parent, Col.Pole, 0f, pole: 0.28f);
                    Place("palm-short", "tree", b.id, L(9.5f, 3.6f), Quaternion.Euler(0, Jit(180), 0), parent, Col.Pole, 0f, pole: 0.2f);
                    Place("dry-hedge", "hedge", b.id, L(18.2f, -3.6f), Face(n), parent, Col.Bounds, 0.01f);
                    Place("dry-hedge", "hedge", b.id, L(18.2f, 3.6f), Face(n), parent, Col.Bounds, 0.01f);
                    Place("bench", "bench", b.id, L(15.2f, 0.4f), Face(n), parent, Col.Bounds, 0.012f);
                    Place("dry-shrub", "bush", b.id, L(8.4f, -5f), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.01f);
                    break;
                case 1: // parking lot: cracked concrete pad, stalls, two cars, a barrier at the mouth
                    marks.Pad("Lot", L(13f, 0, 0), t, n, 12.6f, 11.6f, LotTop);
                    for (int i = 0; i < 4; i++) marks.Line(L(10.5f, -6f + i * 4f, 0), t, n, 0.12f, 5f, LotTop + 0.002f);
                    Car("sedan", deal % 2 == 0, b, L(10.5f, -4f, LotTop), Face(n), parent);
                    Car(deal % 3 == 0 ? "police" : "sedan", deal % 2 == 1, b, L(10.5f, 4f, LotTop), Face(-n), parent, 0.5f);
                    Place("road-barrier", "barrier", b.id, L(18.4f, -5.2f), Face(n), parent, Col.Bounds, 0.01f);
                    Place("road-barrier", "barrier", b.id, L(18.4f, 5.2f), Face(n) * Quaternion.Euler(0, Jit(8), 0), parent, Col.Bounds, 0.01f);
                    Place("security-light", "security light", b.id, L(7.6f, 0f, LotTop), Face(n), parent, Col.Bounds, 0.015f);
                    break;
                case 2: // fenced vacant lot: dirt, chain link along the walk, junk inside
                    marks.Pad("Dirt", L(13f, 0, 0), t, n, 12.6f, 11.6f, LotTop);
                    for (int i = 0; i < 6; i++)
                        Place("chainlink-panel", "chain link fence", b.id, L(18.6f, -4.7f + i * 1.88f), Face(n), parent, Col.Bounds, 0.01f, new Vector3(2f, 1f, 1f));
                    Place("chainlink-post", "chain link fence", b.id, L(18.6f, -5.64f), Quaternion.identity, parent, Col.Bounds, 0.01f);
                    Place("chainlink-post", "chain link fence", b.id, L(18.6f, 5.64f), Quaternion.identity, parent, Col.Bounds, 0.01f);
                    Place("tyre", "junk", b.id, L(13f, -2f, LotTop), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.02f);
                    Place("tyre", "junk", b.id, L(12.2f, -1.4f, LotTop), Quaternion.Euler(90, Jit(180), 0), parent, Col.None, 0.02f, lift: 0.08f);
                    Place("trashbag", "junk", b.id, L(10f, 3f, LotTop), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.02f);
                    Place("trashbag", "junk", b.id, L(9.6f, 3.7f, LotTop), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.02f);
                    Place("barrel", "junk", b.id, L(8.4f, -4.8f, LotTop), Quaternion.Euler(0, Jit(180), 0), parent, Col.Bounds, 0.02f);
                    Place("cardboard-box", "junk", b.id, L(15.5f, 2.2f, LotTop), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.02f);
                    Place("dry-shrub", "bush", b.id, L(16.8f, -4.6f, LotTop), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.01f);
                    Place("dry-shrub", "bush", b.id, L(11f, 5.4f, LotTop), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.01f);
                    break;
                case 3: // side yard: wood fence along the walk, a tall palm, a bush
                    for (int i = 0; i < 5; i++)
                        Place("wood-fence", "wood fence", b.id, L(18.6f, -5f + i * 2.5f), Face(n), parent, Col.Bounds, 0.01f);
                    Place("palm-tall", "tree", b.id, L(12f, 1.5f), Quaternion.Euler(0, Jit(180), 0), parent, Col.Pole, 0f, pole: 0.32f);
                    Place("dry-shrub", "bush", b.id, L(16.6f, -4.4f), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.01f);
                    Place("aircon", "aircon", b.id, L(9f, -6.1f), Face(t), parent, Col.Bounds, 0.015f);
                    break;
                default: // service yard: dumpsters, bags, boxes, crates, a utility box
                    marks.Pad("Lot", L(13f, 0, 0), t, n, 12.6f, 11.6f, LotTop);
                    Place("dumpster", "dumpster", b.id, L(9f, -3.4f, LotTop), Face(n), parent, Col.Bounds, 0.012f);
                    Place("dumpster", "dumpster", b.id, L(9f, -1.1f, LotTop), Face(n) * Quaternion.Euler(0, Jit(6), 0), parent, Col.Bounds, 0.012f);
                    Place("trashbag", "junk", b.id, L(10.6f, 0.6f, LotTop), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.02f);
                    Place("trashbag", "junk", b.id, L(10.2f, 1.2f, LotTop), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.02f);
                    Place("trashbag", "junk", b.id, L(9.4f, 0.9f, LotTop), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.02f);
                    Place("cardboard-box", "junk", b.id, L(11f, -5.2f, LotTop), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.02f);
                    Place("cardboard-box", "junk", b.id, L(11.4f, -4.6f, LotTop), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.02f);
                    Place("crate", "junk", b.id, L(12.8f, 3.6f, LotTop), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.02f);
                    Place("crate", "junk", b.id, L(12.8f, 3.6f, LotTop + 0.25f), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.02f);
                    Place("utility-box-1", "utility box", b.id, L(15.8f, 5.6f, LotTop), Face(n), parent, Col.Bounds, 0.012f);
                    Place("road-barrier", "barrier", b.id, L(18.4f, 0f), Face(n), parent, Col.Bounds, 0.01f);
                    break;
            }
        }

        /// <summary>Lot type per side: five kinds dealt so the four sides of a block are all different and
        /// the same kind does not repeat across a street as often.</summary>
        public static int Pick(int blockId, int side) => (blockId * 2 + side * 3 + blockId / 9) % 5;

        /// <summary>The middle of the block behind the buildings: back fences, two trees, some clutter.</summary>
        static void Backyards(BlockInfo b, Transform parent)
        {
            for (int i = 0; i < 5; i++)
                Place("wood-fence", "wood fence", b.id, b.center + new Vector3(0, YardTop, -5f + i * 2.5f), Quaternion.Euler(0, 90, 0), parent, Col.Bounds, 0.01f);
            Place(b.id % 2 == 0 ? "palm-short" : "palm-tall", "tree", b.id, b.center + new Vector3(-3.6f, YardTop, 3.4f), Quaternion.Euler(0, Jit(180), 0), parent, Col.Pole, 0f, pole: 0.3f);
            Place("palm-short", "tree", b.id, b.center + new Vector3(3.8f, YardTop, -3.2f), Quaternion.Euler(0, Jit(180), 0), parent, Col.Pole, 0f, pole: 0.2f);
            Place("barrel", "junk", b.id, b.center + new Vector3(1.2f, YardTop, 4.6f), Quaternion.Euler(0, Jit(180), 0), parent, Col.Bounds, 0.02f);
            Place("tyre", "junk", b.id, b.center + new Vector3(-1.4f, YardTop, -4.4f), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.02f);
            Place("dry-shrub", "bush", b.id, b.center + new Vector3(-5.6f, YardTop, -5.4f), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.01f);
            Place("aircon", "aircon", b.id, b.center + new Vector3(5.8f, YardTop, 5.9f), Quaternion.Euler(0, 180, 0), parent, Col.Bounds, 0.015f);
        }

        // ---------------------------------------------------------------- crossings, signs and signals

        static void Intersections(IList<BlockInfo> blocks, float[] xs, float[] zs, Transform parent, Marks marks)
        {
            for (int i = 0; i < xs.Length; i++)
                for (int j = 0; j < zs.Length; j++)
                {
                    var p = new Vector3(xs[i], 0, zs[j]);
                    var around = blocks.Where(b => Mathf.Abs(Mathf.Abs(b.center.x - p.x) - 29f) < 1f && Mathf.Abs(Mathf.Abs(b.center.z - p.z) - 29f) < 1f).ToList();
                    bool signal = i > 0 && i < xs.Length - 1 && j > 0 && j < zs.Length - 1;
                    var cross = new GameObject($"Crossing {ZStreetNames[i]} and {XStreetNames[j]}").transform; cross.SetParent(parent, false);
                    bool post = false;
                    foreach (var b in around)
                    {
                        float sx = Mathf.Sign(p.x - b.center.x), sz = Mathf.Sign(p.z - b.center.z);
                        // drivers keep right: the item for traffic on the north-south avenue stands at the corners
                        // where sx * sz < 0, the one for the east-west street where sx * sz > 0
                        bool avenue = sx * sz < 0;
                        var corner = avenue ? b.center + new Vector3(sx * CurbLine, SidewalkTop, sz * 20.5f) : b.center + new Vector3(sx * 20.5f, SidewalkTop, sz * CurbLine);
                        var face = avenue ? new Vector3(0, 0, -sz) : new Vector3(-sx, 0, 0);     // toward oncoming traffic
                        var arm = avenue ? new Vector3(sx, 0, 0) : new Vector3(0, 0, sz);         // out over the street
                        if (signal) Signal(corner, arm, avenue, cross);
                        else Place("stop-sign", "stop sign", -1, corner, Face(face), cross, Col.Pole, 0.004f, pole: 0.05f);
                        if (!post)
                        {
                            post = true;
                            var at = b.center + new Vector3(sx * CurbLine, SidewalkTop, sz * CurbLine);
                            Place("sign-post", "street name sign", -1, at, Quaternion.identity, cross, Col.Pole, 0.004f, pole: 0.05f);
                            Place($"street-blade-{j}", "street name blade", -1, at + Vector3.up * 3.02f, Quaternion.identity, cross, Col.None, 0.004f);
                            Place($"street-blade-{4 + i}", "street name blade", -1, at + Vector3.up * 2.84f, Quaternion.Euler(0, 90, 0), cross, Col.None, 0.004f);
                        }
                    }
                    Crosswalks(p, marks);
                }
        }

        /// <summary>A mast arm signal with its pole on the corner. The model has its arm along -X and the heads
        /// facing +Z (Unity space); mirrored on X it matches drive-on-the-right, so the arm runs along the
        /// holder's +X (out over the street) and the heads face its +Z (oncoming traffic).</summary>
        static void Signal(Vector3 corner, Vector3 arm, bool avenue, Transform parent)
        {
            var fwd = new Vector3(-arm.z, 0, arm.x);    // right = arm, so forward = (-arm.z, 0, arm.x)
            var holder = Place("traffic-signal", "traffic signal", -1, corner + arm * 2.35f, Face(fwd), parent, Col.None, 0f, mirrorX: true, variant: avenue ? "A" : "B");
            if (holder == null) return;
            var pole = new GameObject("Pole collider"); pole.transform.SetParent(holder.transform, false);
            pole.transform.position = corner + Vector3.up * 2.8f;
            var c = pole.AddComponent<BoxCollider>(); c.size = new Vector3(0.24f, 5.6f, 0.24f);
        }

        /// <summary>Zebra crosswalks on all four approaches and a stop bar in the lane that comes in.</summary>
        static void Crosswalks(Vector3 p, Marks marks)
        {
            foreach (var dir in new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left })
            {
                var across = new Vector3(dir.z, 0, -dir.x);
                for (float o = -7.6f; o <= 7.61f; o += 1.0f)
                    marks.Line(p + dir * 11f + across * o, across, dir, 0.5f, 3f, 0.016f);
                // the lane coming in toward the crossing is on its driver's right: driving along -dir
                var right = new Vector3(-dir.z, 0, dir.x); // right of -dir: right(d) = (d.z, 0, -d.x)
                marks.Line(p + dir * 13.2f + right * 4.4f, across, dir, 8.4f, 0.45f, 0.016f);
            }
        }

        static void Manholes(float[] xs, float[] zs, Transform parent)
        {
            for (int i = 0; i < xs.Length; i++)
                for (int j = 0; j + 1 < zs.Length; j++)
                {
                    float m = (zs[j] + zs[j + 1]) / 2;
                    Place("manhole", "manhole", -1, new Vector3(xs[i] + 2.4f, -0.045f, m - 7f), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.01f);
                    Place("manhole", "manhole", -1, new Vector3(xs[i] - 2.6f, -0.045f, m + 9f), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.01f);
                }
            for (int j = 0; j < zs.Length; j++)
                for (int i = 0; i + 1 < xs.Length; i++)
                {
                    float m = (xs[i] + xs[i + 1]) / 2;
                    Place("manhole", "manhole", -1, new Vector3(m + 6f, -0.045f, zs[j] - 2.4f), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.01f);
                    Place("manhole", "manhole", -1, new Vector3(m - 8f, -0.045f, zs[j] + 2.6f), Quaternion.Euler(0, Jit(180), 0), parent, Col.None, 0.01f);
                }
        }

        /// <summary>Wood utility poles down the east curb of every block, strung with three sagging wires per
        /// span (line renderers: simple geometry made here, not a model).</summary>
        static void PowerLines(IList<BlockInfo> blocks, Transform parent)
        {
            var wire = Plain("Wire", new Color(0.05f, 0.05f, 0.05f), 0.3f);
            foreach (var col in blocks.GroupBy(b => Mathf.Round(b.center.x)))
            {
                var tops = new List<Transform>();
                foreach (var b in col.OrderBy(b => b.center.z))
                    foreach (float s in new[] { -14f, 14f })
                    {
                        var h = Place("utility-pole", "utility pole", b.id, b.center + new Vector3(CurbLine, SidewalkTop, s), Quaternion.identity, parent, Col.Pole, 0f, pole: 0.14f, poleAt: new Vector3(0, 0, 0.25f));
                        if (h != null) tops.Add(h.transform);
                    }
                for (int a = 0; a + 1 < tops.Count; a++)
                    foreach (float x in new[] { -1.05f, 0f, 1.05f })
                    {
                        var p0 = tops[a].TransformPoint(new Vector3(x, 9.47f, 0.25f)); var p1 = tops[a + 1].TransformPoint(new Vector3(x, 9.47f, 0.25f));
                        var go = new GameObject("Wire"); go.transform.SetParent(parent, false);
                        var lr = go.AddComponent<LineRenderer>();
                        lr.useWorldSpace = true; lr.widthMultiplier = 0.028f; lr.numCapVertices = 0; lr.sharedMaterial = wire;
                        lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false;
                        const int seg = 10; lr.positionCount = seg + 1;
                        float sag = 0.022f * Vector3.Distance(p0, p1);
                        for (int q = 0; q <= seg; q++) { float u = q / (float)seg; lr.SetPosition(q, Vector3.Lerp(p0, p1, u) + Vector3.down * sag * 4f * u * (1 - u)); }
                        Count("power line");
                    }
            }
        }

        static void Joints(BlockInfo b, Marks marks)
        {
            // sidewalk slab joints every 1.5 m across the ring, and a pale curb edge
            for (int k = 0; k < 4; k++)
            {
                var n = N(k); var t = T(k);
                for (float s = -21f; s <= 21.01f; s += 1.5f)
                    marks.Line(b.center + n * 20.5f + t * s, n, t, 3f, 0.05f, SidewalkTop + 0.003f, "Joint");
                marks.Line(b.center + n * 21.86f, t, n, 44f, 0.28f, SidewalkTop + 0.003f, "CurbEdge");
            }
        }

        // ---------------------------------------------------------------- placing

        static void Car(string kind, bool painted, BlockInfo b, Vector3 at, Quaternion rot, Transform parent, float jitter = 0f)
        {
            if (Blocked(at, 4f)) return;
            string key = $"car-{kind}{(painted && kind != "police" ? "-painted" : "")}";
            if (!batches.TryGetValue(key, out var batch))
            {
                // the full kit car up close and its Sovereign Asset cut (Models/Cars/LOD1) past 25 m, fitted the same way
                var near = VehicleBuilder.ParkedCar(kind, painted);
                var order = new List<Material>();
                var lod0 = MeshMerge.Merge(near, m => m, order, false, $"{MeshDir}/Props/{key}.asset");
                Mesh lod1 = null;
                var glb = $"Assets/Art/Models/Cars/LOD1/{kind}.glb";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(glb) != null)
                {
                    var far = VehicleBuilder.ParkedCar(kind, painted, glb);
                    lod1 = MeshMerge.Merge(far, m => m, order, false, $"{MeshDir}/Props/{key}-lod1.asset");
                    Object.DestroyImmediate(far);
                }
                Object.DestroyImmediate(near);
                batch = NewBatch(key, lod0, lod1, order, 15f, 150f, true);
                batch.shadowDistance = 40f;
            }
            var car = new GameObject(kind == "police" ? "Parked cruiser" : painted ? "Parked sedan (red)" : "Parked sedan");
            car.transform.SetParent(parent, false);
            car.transform.SetPositionAndRotation(at, rot * Quaternion.Euler(0, Jit(jitter * 6f), 0));
            var box = car.AddComponent<BoxCollider>(); box.center = batch.lod0.bounds.center; box.size = batch.lod0.bounds.size;
            var mk = car.AddComponent<PropMarker>(); mk.kind = "parked car"; mk.model = kind; mk.blockId = b.id;
            placed[key].Add(car.transform.localToWorldMatrix);
            tris += MeshMerge.Triangles(batch.lod0);
            Count("parked car");
        }

        static PropBatch NewBatch(string key, Mesh lod0, Mesh lod1, List<Material> order, float lod1At, float cullAt, bool shadows)
        {
            var bs = lod0.bounds; if (lod1) bs.Encapsulate(lod1.bounds);
            foreach (var m in order) if (m != null && !m.enableInstancing) { m.enableInstancing = true; EditorUtility.SetDirty(m); }
            var batch = new PropBatch
            {
                name = key, lod0 = lod0, lod1 = lod1, materials = order.ToArray(), lod1Distance = lod1At, cullDistance = cullAt,
                castShadows = shadows, center = bs.center, radius = bs.extents.magnitude,
            };
            batches[key] = batch; placed[key] = new List<Matrix4x4>();
            return batch;
        }

        /// <summary>Draw rules per model: where the lower detail mesh takes over, how far it draws, shadows.</summary>
        static (float lod1, float cull, bool shadows) Rule(string model)
        {
            if (model is "street-lamp-1") return (20f, 170f, true);
            if (model is "utility-pole" or "traffic-signal" or "stop-sign" or "sign-post" or "bus-shelter" or "bus-stop-sign"
                || model.StartsWith("tree-") || model.StartsWith("palm-") || model.StartsWith("street-blade")) return (40f, 170f, true);
            if (model is "manhole" or "cardboard-box" or "crate" or "trashbag" or "tyre") return (15f, 45f, false);
            if (model is "wood-fence" or "chainlink-panel" or "chainlink-post" or "hedge" or "dry-hedge" or "dumpster" or "road-barrier" or "bench") return (22f, 80f, true);
            return (18f, 60f, true);
        }

        /// <summary>How far a prop's shadow is drawn: tall ones throw long shadows that read from farther away;
        /// the rest stop at 30 m (the buildings keep the full Ultra 100 m).</summary>
        static float ShadowReach(string model) =>
            model is "street-lamp-1" or "utility-pole" or "traffic-signal" or "bus-shelter" || model.StartsWith("tree-") || model.StartsWith("palm-") ? 60f : 30f;

        /// <summary>The model merged into one mesh per material (and its LOD1, from Models/Props/LOD1), made once.</summary>
        static PropBatch Batch(string model, bool mirrorX, string variant, Vector3 scale)
        {
            // a placement scale (the stretched chain link panels) is baked in the model's own space, like the mirror
            var rootScale = new Vector3(mirrorX ? -scale.x : scale.x, scale.y, scale.z);
            string key = model + (mirrorX ? "@mx" : "") + (scale != Vector3.one ? $"@{scale.x:0.##}x{scale.y:0.##}x{scale.z:0.##}" : "") + (variant != null ? "#" + variant : "");
            if (batches.TryGetValue(key, out var have)) return have;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}/{model}.glb");
            if (asset == null) { Debug.LogError($"[PropBuilder] missing prop model {model}"); return null; }
            Func<Material, Material> map = m => Variant(PropMat(model, m), variant);
            var order = new List<Material>();
            string name = key.Replace("#", "-").Replace("@", "-").Replace(".", "_");
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            var lod0 = MeshMerge.Merge(inst, map, order, rootScale, $"{MeshDir}/Props/{name}.asset");
            Object.DestroyImmediate(inst);
            Mesh lod1 = null;
            var low = AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}/LOD1/{model}.glb");
            if (low != null)
            {
                inst = (GameObject)PrefabUtility.InstantiatePrefab(low);
                lod1 = MeshMerge.Merge(inst, map, order, rootScale, $"{MeshDir}/Props/{name}-lod1.asset");
                Object.DestroyImmediate(inst);
            }
            var (l1, cull, sh) = Rule(model);
            var nb = NewBatch(key, lod0, lod1, order, l1, cull, sh);
            nb.shadowDistance = ShadowReach(model);
            return nb;
        }

        /// <summary>The second signal group (the east-west streets) lights its own copies of the lens materials.</summary>
        static Material Variant(Material m, string variant)
        {
            if (m == null || variant != "B" || !m.name.Contains("lens_")) return m;
            string path = $"{MatDir}/{m.name}_B.mat";
            var b = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (b == null) { b = new Material(m); AssetDatabase.CreateAsset(b, path); } else b.CopyPropertiesFromMaterial(m);
            b.shader = m.shader; b.shaderKeywords = m.shaderKeywords; b.enableInstancing = true;
            EditorUtility.SetDirty(b);
            return b;
        }

        static GameObject Place(string model, string kind, int blockId, Vector3 pos, Quaternion rot, Transform parent, Col col, float cull,
            Vector3? scale = null, float pole = 0.1f, Vector3? poleAt = null, bool mirrorX = false, float lift = 0f, string variant = null)
        {
            if (col != Col.None && Blocked(pos, 1.2f)) return null;
            var sc = scale ?? Vector3.one;
            var batch = Batch(model, mirrorX, variant, sc);
            if (batch == null) return null;
            var holder = new GameObject($"{kind} ({model})");
            holder.transform.SetParent(parent, false);
            // the collider is fitted in the holder's own space before it is posed
            var b = batch.lod0.bounds;
            if (col == Col.Bounds) { var c = holder.AddComponent<BoxCollider>(); c.center = b.center; c.size = b.size; }
            else if (col == Col.Pole)
            {
                var c = holder.AddComponent<BoxCollider>();
                var at = poleAt ?? Vector3.zero;
                c.center = new Vector3(at.x, Mathf.Min(b.size.y, 4f) / 2, at.z); c.size = new Vector3(pole * 2, Mathf.Min(b.size.y, 4f), pole * 2);
            }
            holder.transform.SetPositionAndRotation(pos + Vector3.up * lift, rot);
            var mk = holder.AddComponent<PropMarker>(); mk.kind = kind; mk.model = model; mk.blockId = blockId;
            placed[batch.name].Add(holder.transform.localToWorldMatrix);
            tris += MeshMerge.Triangles(batch.lod0);
            Count(kind);
            return holder;
        }

        static void Count(string kind) { counts.TryGetValue(kind, out int c); counts[kind] = c + 1; }

        static void Static(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }

        /// <summary>Places the scene builder already used: the player, the cars, Unc, people, the job.</summary>
        static void KeepClear()
        {
            keep = new List<(Vector3, float)>();
            foreach (var c in Object.FindObjectsByType<CharacterController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) keep.Add((c.transform.position, 1.6f));
            foreach (var c in Object.FindObjectsByType<CarController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) keep.Add((c.transform.position, 4.5f));
            foreach (var m in Object.FindObjectsByType<MissionRunner>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (m.giver) keep.Add((m.giver.position, 2.5f));
        }

        static bool Blocked(Vector3 p, float r)
        {
            foreach (var (q, kr) in keep) { var d = p - q; d.y = 0; if (d.magnitude < r + kr) return true; }
            return false;
        }

        // ---------------------------------------------------------------- materials

        static void LoadManifest()
        {
            manifest = new Dictionary<string, Entry>();
            var path = $"{Tex}/manifest.json";
            if (!File.Exists(path)) { Debug.LogWarning("[PropBuilder] no prop texture manifest"); return; }
            foreach (var e in JsonUtility.FromJson<Manifest>(File.ReadAllText(path)).materials) manifest[$"{e.prop}|{e.material}"] = e;
        }

        /// <summary>One loose copy of a prop model with its URP materials (the mugger's bat), outside the merged batches.</summary>
        public static GameObject Loose(string model)
        {
            if (manifest == null) LoadManifest();
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}/{model}.glb");
            if (asset == null) { Debug.LogError($"[PropBuilder] missing prop model {model}"); return null; }
            var go = (GameObject)Object.Instantiate(asset); go.name = model;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(m => PropMat(model, m)).ToArray();
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            return go;
        }

        static Material PropMat(string prop, Material src)
        {
            if (src == null) return null;
            string key = $"{prop}|{src.name}";
            if (mats.TryGetValue(key, out var done) && done != null) return done;
            if (!manifest.TryGetValue(key, out var e))
                e = manifest.Values.FirstOrDefault(x => x.prop == prop && (src.name.StartsWith(x.material) || x.material.StartsWith(src.name)));
            if (e == null)
            {
                Debug.LogWarning($"[PropBuilder] no manifest entry for {prop} material {src.name}, using a plain copy");
                e = new Entry { prop = prop, material = src.name, alpha = "OPAQUE", color = new[] { 0.5f, 0.5f, 0.5f, 1f }, metallic = 0, roughness = 0.8f, tiling = new[] { 1f, 1f } };
            }
            Directory.CreateDirectory(MatDir);
            string path = $"{MatDir}/{Safe(prop)}__{Safe(e.material)}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); } else m.shader = lit;
            Texture2D Tx(string f) => string.IsNullOrEmpty(f) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}/{f}");
            var c = e.color != null && e.color.Length == 4 ? new Color(e.color[0], e.color[1], e.color[2], e.color[3]) : Color.white;
            var col = c.gamma; col.a = c.a;     // glTF factors are linear; material colors are entered in sRGB
            m.SetTexture("_BaseMap", Tx(e.basecolor));
            var tiling = e.tiling != null && e.tiling.Length == 2 ? new Vector2(e.tiling[0], e.tiling[1]) : Vector2.one;
            m.SetTextureScale("_BaseMap", tiling);
            var nm = Tx(e.normal); m.SetTexture("_BumpMap", nm); m.SetFloat("_BumpScale", 1f);
            if (nm != null) m.EnableKeyword("_NORMALMAP"); else m.DisableKeyword("_NORMALMAP");
            var ms = Tx(e.metalsmooth);
            if (ms != null)
            {
                m.SetTexture("_MetallicGlossMap", ms); m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.SetFloat("_Metallic", 1f); m.SetFloat("_Smoothness", 1f); m.SetFloat("_SmoothnessTextureChannel", 0f);
            }
            else
            {
                m.SetTexture("_MetallicGlossMap", null); m.DisableKeyword("_METALLICSPECGLOSSMAP");
                m.SetFloat("_Metallic", Mathf.Clamp01(e.metallic)); m.SetFloat("_Smoothness", Mathf.Clamp(1f - e.roughness, 0f, 0.9f));
            }
            bool glass = e.material.ToLowerInvariant().Contains("glass");
            bool cutout = !glass && e.alpha != "OPAQUE";
            m.SetShaderPassEnabled("ShadowCaster", !glass);
            if (glass)
            {
                if (ms == null && Tx(e.basecolor) == null) col = new Color(0.62f, 0.72f, 0.78f, 0.28f); else col.a = Mathf.Min(col.a, 0.45f);
                m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetFloat("_AlphaClip", 0); m.DisableKeyword("_ALPHATEST_ON");
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent; m.SetFloat("_Cull", 0f);
            }
            else
            {
                m.SetFloat("_Surface", 0); m.SetFloat("_ZWrite", 1); m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetFloat("_SrcBlend", (float)BlendMode.One); m.SetFloat("_DstBlend", (float)BlendMode.Zero);
                if (cutout)
                {
                    m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", e.alpha == "MASK" ? e.cutoff : 0.5f); m.EnableKeyword("_ALPHATEST_ON");
                    m.SetOverrideTag("RenderType", "TransparentCutout"); m.renderQueue = (int)RenderQueue.AlphaTest;
                }
                else
                {
                    m.SetFloat("_AlphaClip", 0); m.DisableKeyword("_ALPHATEST_ON");
                    m.SetOverrideTag("RenderType", "Opaque"); m.renderQueue = -1;
                }
                // Poly Haven scans can be open shells (inside of a bin, a lamp cage), so they stay two sided;
                // our own modeled props are closed and cull their backs
                m.SetFloat("_Cull", cutout || ms != null ? 0f : 2f);
            }
            m.SetColor("_BaseColor", col);
            if (e.material.StartsWith("lens_"))
            {
                // the traffic light lenses glow through TrafficSignalCycle; off by default
                m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else if (prop == "street-lamp-1" && glass)
            {
                // golden hour: the street lamps are already on, a warm glow the bloom picks up, as in the web game
                m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.4f) * 2.4f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else { m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            mats[key] = m;
            return m;
        }

        public static Material Plain(string name, Color c, float smooth, bool unlit = false)
        {
            Directory.CreateDirectory(MatDir);
            string path = $"{MatDir}/{name}.mat";
            var sh = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); } else m.shader = sh;
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Textured(string name, string basecolor, string normal, float tile, float smooth, Color tint)
        {
            var m = Plain(name, tint, smooth);
            var bc = AssetDatabase.LoadAssetAtPath<Texture2D>(basecolor);
            var nm = normal != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(normal) : null;
            m.SetTexture("_BaseMap", bc); m.SetTextureScale("_BaseMap", new Vector2(tile, tile));
            m.SetTexture("_BumpMap", nm); m.SetFloat("_BumpScale", 1f);
            if (nm != null) m.EnableKeyword("_NORMALMAP"); else m.DisableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(m);
            return m;
        }

        static string Safe(string s) => new string((s ?? "mat").Select(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '_').ToArray());

        // ---------------------------------------------------------------- flat markings, one mesh per material

        // ---------------------------------------------------------------- decals (cracks, oil, litter, graffiti)

        /// <summary>The decal atlas tiles (tools/draw_decals.py, finished by Sovereign Asset as decal-set.glb):
        /// two rows of four 256 px ground tiles, then four 512 x 256 graffiti pieces.</summary>
        static Rect Tile(int i) => new Rect((i % 4) * 0.25f, (i / 4) * 0.25f, 0.25f, 0.25f);
        static Rect Piece(int i) => new Rect((i % 2) * 0.5f, 0.5f + (i / 2) * 0.25f, 0.5f, 0.25f);
        static readonly int[] Cracks = { 0, 1, 6 }, Oils = { 2, 3, 7 }, Litters = { 4, 5 };
        static int Hash(int a, int b, int c) { unchecked { int h = a * 73856093 ^ b * 19349663 ^ c * 83492791; return h & 0x7fffffff; } }

        /// <summary>How far the street center lies past the block center on side k (0 if the side faces the edge).</summary>
        static float StreetAt(BlockInfo b, int k, float[] xs, float[] zs)
        {
            float c = k < 2 ? b.center.x : b.center.z; float sign = k % 2 == 0 ? 1 : -1;
            var lines = k < 2 ? xs : zs; float best = 0;
            foreach (var l in lines) { float d = (l - c) * sign; if (d > CurbLine && (best == 0 || d < best)) best = d; }
            return best;
        }

        /// <summary>Street grime along one block side: cracks in the asphalt and the sidewalk, oil where cars park and
        /// stop, litter in the gutter and on the walk. Dealt by a hash so the build is the same every time.</summary>
        static void Grime(BlockInfo b, int k, float street, Marks marks)
        {
            var n = N(k); var t = T(k);
            float far = street > 0 ? street - 0.6f : CurbLine + 5f;    // stay on this block's half of the street
            for (int i = 0; i < 6; i++)
            {
                float s = -17f + i * 6.8f; int h = Hash(b.id, k, i);
                float ang = (h % 360) * Mathf.Deg2Rad; var r = Mathf.Cos(ang) * t + Mathf.Sin(ang) * n; var u = Vector3.Cross(Vector3.up, r) * -1f;
                if (h % 3 == 0) marks.Decal("Decal", At(b, k, Mathf.Lerp(CurbLine + 1.5f, far, (h / 7 % 10) / 10f), s, 0.008f), r, u, Vector3.up, 2.2f, 2.2f, Tile(Cracks[h / 3 % 3]));
                if (h % 4 == 1 && ParkLine < far) marks.Decal("Decal", At(b, k, ParkLine + (h / 5 % 3 - 1) * 0.4f, s + 2f, 0.009f), r, u, Vector3.up, 1.7f, 1.7f, Tile(Oils[h / 4 % 3]));
                if (h % 5 == 2) marks.Decal("Decal", At(b, k, CurbLine + 0.55f, s - 1.5f, 0.01f), r, u, Vector3.up, 1.3f, 1.3f, Tile(Litters[h / 5 % 2]));
                if (h % 7 == 3) marks.Decal("Decal", At(b, k, 20.8f, s + 1f, SidewalkTop + 0.006f), r, u, Vector3.up, 1.4f, 1.4f, Tile(h % 2 == 0 ? Cracks[0] : Litters[1]));
            }
        }

        /// <summary>Tags and pieces on the side walls of about half the buildings, found by a ray against the real
        /// wall (a mesh collider on the near model while the scene is built, removed after) so they sit on it.</summary>
        static void Graffiti(Marks marks)
        {
            int tagged = 0;
            foreach (var bm in Object.FindObjectsByType<BuildingMarker>(FindObjectsSortMode.None).OrderBy(x => x.blockId).ThenBy(x => x.index))
            {
                int h = Hash(bm.blockId, bm.index, 5);
                if (h % 2 == 1) continue;
                var lod = bm.GetComponent<RuntimeLod>(); var box = bm.GetComponent<BoxCollider>();
                if (lod == null || box == null || lod.near == null) continue;
                var temp = new List<MeshCollider>();
                foreach (var r in lod.near) { var mf = r != null ? r.GetComponent<MeshFilter>() : null; if (mf != null && mf.sharedMesh != null) { var mc = r.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; temp.Add(mc); } }
                Physics.SyncTransforms();
                var bounds = box.bounds; var side = bm.transform.right * (h % 4 == 0 ? 1 : -1); var fwd = bm.transform.forward;
                var from = bounds.center + side * (bounds.extents.magnitude + 2f) + fwd * (bounds.extents.z * 0.3f);
                from.y = 1.5f;
                var hit = Physics.RaycastAll(from, -side, bounds.extents.magnitude + 3f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(x => temp.Contains(x.collider as MeshCollider)).OrderBy(x => x.distance).FirstOrDefault();
                if (hit.collider != null && Mathf.Abs(hit.normal.y) < 0.3f)
                {
                    var nrm = new Vector3(hit.normal.x, 0, hit.normal.z).normalized;
                    var right = Vector3.Cross(Vector3.up, -nrm);
                    marks.Decal("Graffiti", hit.point + nrm * 0.03f, right, Vector3.up, nrm, 2.6f, 1.3f, Piece(h / 2 % 4));
                    tagged++;
                }
                foreach (var mc in temp) Object.DestroyImmediate(mc);
            }
            Debug.Log($"[PropBuilder] graffiti on {tagged} walls");
        }

        /// <summary>The decal material: lit, alpha blended over the ground or wall, no shadow of its own.</summary>
        static Material DecalMat(string name)
        {
            string path = $"{MatDir}/{name.Replace(" ", "_")}.mat";
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { Directory.CreateDirectory(MatDir); m = new Material(lit); AssetDatabase.CreateAsset(m, path); } else m.shader = lit;
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}/decal-set__decals_basecolor.png"));
            m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.1f); m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetFloat("_AlphaClip", 0); m.DisableKeyword("_ALPHATEST_ON");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent - 100; m.SetFloat("_Cull", 2f);
            m.SetShaderPassEnabled("ShadowCaster", false);
            m.enableInstancing = true; EditorUtility.SetDirty(m);
            return m;
        }

        class Marks
        {
            readonly Dictionary<string, (List<Vector3> v, List<Vector2> uv, List<int> t)> sets = new();
            readonly Dictionary<string, List<Vector3>> normals = new();

            /// <summary>A decal: a w by h quad centered on c, `right` across it and `up` along it, facing n, showing the
            /// atlas rectangle uv (0 to 1, from the top left of the decal atlas).</summary>
            public void Decal(string set, Vector3 c, Vector3 right, Vector3 up, Vector3 n, float w, float h, Rect uv)
            {
                if (!sets.TryGetValue(set, out var s)) sets[set] = s = (new List<Vector3>(), new List<Vector2>(), new List<int>());
                if (!normals.TryGetValue(set, out var nl)) normals[set] = nl = new List<Vector3>();
                while (nl.Count < s.v.Count) nl.Add(Vector3.up);
                var a = right.normalized * (w / 2); var b = up.normalized * (h / 2);
                int i = s.v.Count;
                s.v.Add(c - a - b); s.v.Add(c - a + b); s.v.Add(c + a + b); s.v.Add(c + a - b);
                // atlas rows run from the top (glTF), Unity v runs from the bottom
                float u0 = uv.xMin, u1 = uv.xMax, v0 = 1f - uv.yMax, v1 = 1f - uv.yMin;
                s.uv.Add(new Vector2(u0, v0)); s.uv.Add(new Vector2(u0, v1)); s.uv.Add(new Vector2(u1, v1)); s.uv.Add(new Vector2(u1, v0));
                for (int q = 0; q < 4; q++) nl.Add(n.normalized);
                bool front = Vector3.Dot(Vector3.Cross(b, a), n) > 0;
                if (front) { s.t.Add(i); s.t.Add(i + 1); s.t.Add(i + 2); s.t.Add(i); s.t.Add(i + 2); s.t.Add(i + 3); }
                else { s.t.Add(i); s.t.Add(i + 2); s.t.Add(i + 1); s.t.Add(i); s.t.Add(i + 3); s.t.Add(i + 2); }
            }

            /// <summary>A flat rectangle centered on c, `along` meters along a and `across` along b, at height y.</summary>
            public void Line(Vector3 c, Vector3 a, Vector3 b, float along, float across, float y, string set = "White") => Quad(set, c, a, b, along, across, y, 1f);
            public void Pad(string set, Vector3 c, Vector3 a, Vector3 b, float along, float across, float y) => Quad(set, c, a, b, along, across, y, 0.25f);

            void Quad(string set, Vector3 c, Vector3 a, Vector3 b, float along, float across, float y, float uvPerMeter)
            {
                if (!sets.TryGetValue(set, out var s)) sets[set] = s = (new List<Vector3>(), new List<Vector2>(), new List<int>());
                a = a.normalized * (along / 2); b = b.normalized * (across / 2); c.y = y;
                int i = s.v.Count;
                s.v.Add(c - a - b); s.v.Add(c - a + b); s.v.Add(c + a + b); s.v.Add(c + a - b);
                float u = along * uvPerMeter, w = across * uvPerMeter;
                s.uv.Add(new Vector2(0, 0)); s.uv.Add(new Vector2(0, w)); s.uv.Add(new Vector2(u, w)); s.uv.Add(new Vector2(u, 0));
                // wind so the face points up whichever way a and b turn
                bool up = Vector3.Cross(b, a).y > 0;
                if (up) { s.t.Add(i); s.t.Add(i + 1); s.t.Add(i + 2); s.t.Add(i); s.t.Add(i + 2); s.t.Add(i + 3); }
                else { s.t.Add(i); s.t.Add(i + 2); s.t.Add(i + 1); s.t.Add(i); s.t.Add(i + 3); s.t.Add(i + 2); }
            }

            public void Save(Transform parent)
            {
                Directory.CreateDirectory(MeshDir);
                var look = new Dictionary<string, Material>
                {
                    ["White"] = Plain("Marking", new Color(0.66f, 0.65f, 0.61f), 0.25f),   // worn grey like the web crosswalks (9a9a92)
                    ["Joint"] = Plain("SidewalkJoint", new Color(0.36f, 0.35f, 0.33f), 0.05f),
                    ["CurbEdge"] = Plain("CurbEdge", new Color(0.72f, 0.71f, 0.68f), 0.1f),
                    ["Lot"] = Textured("LotConcrete", "Assets/Art/Textures/Ground/cracked_concrete_basecolor.jpg", "Assets/Art/Textures/Ground/cracked_concrete_normal.jpg", 1f, 0.1f, Color.white),
                    ["Decal"] = DecalMat("Decal Ground"),
                    ["Graffiti"] = DecalMat("Decal Graffiti"),
                    ["Dirt"] = Textured("LotDirt", "Assets/Art/Textures/Ground/park_dirt_basecolor.jpg", "Assets/Art/Textures/Ground/park_dirt_normal.jpg", 1f, 0.05f, Color.white),
                };
                foreach (var kv in sets)
                {
                    string path = $"{MeshDir}/Markings-{kv.Key}.asset";
                    if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
                    var mesh = new Mesh { name = $"Markings {kv.Key}", indexFormat = IndexFormat.UInt32 };
                    mesh.SetVertices(kv.Value.v); mesh.SetUVs(0, kv.Value.uv); mesh.SetTriangles(kv.Value.t, 0);
                    var nl = normals.TryGetValue(kv.Key, out var got) ? got : new List<Vector3>();
                    while (nl.Count < kv.Value.v.Count) nl.Add(Vector3.up);
                    mesh.SetNormals(nl);
                    mesh.RecalculateTangents(); mesh.RecalculateBounds();
                    AssetDatabase.CreateAsset(mesh, path);
                    var go = new GameObject($"Markings {kv.Key}"); go.transform.SetParent(parent, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = look[kv.Key];
                    r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true;
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
                    tris += kv.Value.t.Count / 3;
                }
            }
        }
    }
}
