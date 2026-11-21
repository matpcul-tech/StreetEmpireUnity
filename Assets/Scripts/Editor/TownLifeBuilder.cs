// Town life: writes the activity spots and the townie templates into the Eastside scene.
// A block comes to life when one of its buildings has a layout in Shops below (the tire shop and the gas
// station to start). Every building on a live block gets a door people walk in and out of; the shops also
// get work spots for their staff and places customers stand. To bring another business to life, add its
// model to Shops. Spot positions are measured from the building's own footprint:
//   x: across the front, -1 (left edge) to 1 (right edge) seen from inside looking out (Unity local +X)
//   outFront: meters out from the front edge of the footprint (the box collider), where a person can stand
// See docs/TOWN-LIFE.md.
using System.Collections.Generic;
using System.Linq;
using StreetEmpire.City;
using StreetEmpire.Game;
using UnityEngine;

namespace StreetEmpire.EditorTools
{
    public static class TownLifeBuilder
    {
        public struct Def
        {
            public SpotKind kind; public string label, anim;
            public float x, outFront, minSeconds, maxSeconds;
            public bool faceOut;
            /// <summary>Doors: how far back from the front edge of the footprint the door itself is.</summary>
            public float doorDepth;
        }

        public class Shop
        {
            public float open, close;
            public Def[] spots;
        }

        static Def Door(string label, float x, float doorDepth) =>
            new Def { kind = SpotKind.Door, label = label, x = x, outFront = 0.7f, doorDepth = doorDepth, minSeconds = 10, maxSeconds = 25 };
        static Def Work(string label, float x, float outFront, bool faceOut, string anim) =>
            new Def { kind = SpotKind.Work, label = label, x = x, outFront = outFront, faceOut = faceOut, anim = anim, minSeconds = 25, maxSeconds = 45 };
        static Def Visit(string label, float x, float outFront, string anim) =>
            new Def { kind = SpotKind.Visit, label = label, x = x, outFront = outFront, anim = anim, minSeconds = 12, maxSeconds = 25 };

        public static readonly Dictionary<string, Shop> Shops = new()
        {
            // The tire shop (shop-auto-garage, 11.7 m wide): two bays and an office door across the front.
            // Open through the day and into the evening; the game starts at 17:48.
            ["shop-auto-garage"] = new Shop
            {
                open = 7f, close = 19f,
                spots = new[]
                {
                    Door("office door", -0.43f, 0.7f),
                    Work("bay 1", 0.63f, 1.0f, false, "Work"),
                    Work("bay 2", 0f, 1.0f, false, "Work"),
                    Visit("waiting on a tire", 0.31f, 1.9f, ""),
                },
            },
            // The gas station (shop-gas-station, 6.8 m wide, 9.4 m deep): the store is the back half, the canopy
            // and its two pumps the front half. The box collider covers the canopy, so customers stand at the
            // front edge facing their pump and walk in under the canopy between the pumps to the store.
            ["shop-gas-station"] = new Shop
            {
                open = 0f, close = 24f,
                spots = new[]
                {
                    Door("store door", 0f, 4.4f),
                    Visit("pump 1", -0.33f, 0.6f, "PumpGas"),
                    Visit("pump 2", 0.33f, 0.6f, "PumpGas"),
                    Work("forecourt", 0.85f, 1.3f, true, ""),
                    Work("sweeping the lot", -0.85f, 1.7f, true, "Work"),
                },
            },
        };

        /// <summary>The layout for a building model: its own entry, else the tire shop's for any garage or tire
        /// shop file and the gas station's for any gas station file, so a renamed or replaced model (a v2) stays alive.</summary>
        public static Shop ShopFor(string model)
        {
            if (string.IsNullOrEmpty(model)) return null;
            if (Shops.TryGetValue(model, out var exact)) return exact;
            if (model.Contains("tire") || model.Contains("garage")) return Shops["shop-auto-garage"];
            if (model.Contains("gas")) return Shops["shop-gas-station"];
            return null;
        }

        public static void Build(GameObject game)
        {
            var life = game.AddComponent<TownLife>();
            var root = new GameObject("Town Life Spots").transform;
            var markers = Object.FindObjectsByType<BuildingMarker>(FindObjectsSortMode.None);
            var centers = new Dictionary<int, Vector3>();
            foreach (var block in Object.FindObjectsByType<BlockMarker>(FindObjectsSortMode.None)) centers[block.blockId] = block.transform.position;
            var live = new HashSet<int>(markers.Where(mk => ShopFor(mk.model) != null).Select(mk => mk.blockId));
            var spots = new List<ActivitySpot>();
            foreach (var m in markers.Where(mk => live.Contains(mk.blockId)).OrderBy(mk => mk.blockId).ThenBy(mk => mk.index))
            {
                var box = m.GetComponent<BoxCollider>();
                if (box == null) continue;
                var center = centers.TryGetValue(m.blockId, out var bc) ? bc : (m.transform.parent != null ? m.transform.parent.position : m.transform.position);
                float halfW = box.size.x / 2f, front = box.center.z + box.size.z / 2f;
                var shop = ShopFor(m.model);
                if (shop != null)
                {
                    foreach (var d in shop.spots)
                    {
                        var s = Spot(root, m, center, d.kind, d.label, box.center.x + d.x * halfW, front + d.outFront, d.faceOut);
                        s.shop = true; s.openHour = shop.open; s.closeHour = shop.close;
                        s.seconds = new Vector2(d.minSeconds, d.maxSeconds); s.animTrigger = d.anim ?? "";
                        if (d.kind == SpotKind.Door) { s.capacity = 4; Enter(s, m, box.center.x + d.x * halfW, front - d.doorDepth); }
                        spots.Add(s);
                    }
                }
                else
                {
                    // a house or an apartment block: its front door, where the finished file marks one, else the
                    // middle of the front
                    float x = box.center.x, doorZ = front - 0.8f;
                    if (FindDoor(m, box, out var door)) { x = door.x; doorZ = door.z; }
                    var s = Spot(root, m, center, SpotKind.Door, "front door", x, front + 0.6f, false);
                    s.seconds = new Vector2(25f, 70f); s.capacity = 3;
                    Enter(s, m, x, doorZ);
                    spots.Add(s);
                }
            }
            life.spots = spots.ToArray();

            // the people TownLife copies (inactive in the scene), one per pedestrian model, taken in turn
            life.templates = GameBuilder.PedLooks.Select((look, i) =>
            {
                var tpl = GameBuilder.Character($"Townie Template {i + 1}", new Vector3(i * 3, -70, 0), 0, look, true);
                var hp = tpl.AddComponent<Health>(); hp.max = hp.hp = 30;
                tpl.AddComponent<HitReaction>();
                tpl.AddComponent<Townie>();
                tpl.SetActive(false);
                return tpl;
            }).ToArray();
            Debug.Log($"[TownLifeBuilder] {spots.Count} activity spots on {live.Count} blocks, {life.templates.Length} townie templates");
        }

        /// <summary>A spot at a point in the building's own space (x across the front, z toward the street),
        /// on the sidewalk, facing the building or the street.</summary>
        static ActivitySpot Spot(Transform root, BuildingMarker m, Vector3 blockCenter, SpotKind kind, string label, float lx, float lz, bool faceOut)
        {
            var go = new GameObject($"{kind} {m.model} {m.blockId}.{m.index} {label}");
            go.transform.SetParent(root, false);
            var at = m.transform.TransformPoint(new Vector3(lx, 0, lz)); at.y = PropBuilder.SidewalkTop;
            var fwd = m.transform.forward; fwd.y = 0;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(faceOut ? fwd : -fwd));
            var s = go.AddComponent<ActivitySpot>();
            s.kind = kind; s.label = label; s.building = m; s.blockId = m.blockId; s.blockCenter = blockCenter;
            return s;
        }

        static void Enter(ActivitySpot s, BuildingMarker m, float lx, float lz)
        {
            var p = m.transform.TransformPoint(new Vector3(lx, 0, lz)); p.y = PropBuilder.SidewalkTop;
            s.hasEnter = true; s.enterPoint = p;
        }

        /// <summary>The front door of a finished building, in the building's own space: the "door" part of
        /// the model (Sovereign Asset material) nearest the front that is the size of one door.</summary>
        static bool FindDoor(BuildingMarker m, BoxCollider box, out Vector3 local)
        {
            local = default; float best = float.MinValue;
            foreach (var mf in m.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh; var r = mf.GetComponent<Renderer>();
                if (mesh == null || r == null) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < Mathf.Min(mesh.subMeshCount, mats.Length); i++)
                {
                    if (mats[i] == null || !SceneBuilder.Part(mats[i]).Contains("door")) continue;
                    var b = mesh.GetSubMesh(i).bounds;
                    var lo = Vector3.positiveInfinity; var hi = Vector3.negativeInfinity;
                    for (int k = 0; k < 8; k++)
                    {
                        var corner = b.center + Vector3.Scale(b.extents, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1));
                        var p = m.transform.InverseTransformPoint(mf.transform.TransformPoint(corner));
                        lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p);
                    }
                    var size = hi - lo; var c = (lo + hi) / 2f;
                    // one door leaf on the front half: not a pair of doors, a garage door or the back door
                    if (size.y < 1.5f || size.x < 0.5f || size.x > 2.2f || c.z < box.center.z) continue;
                    if (c.z > best) { best = c.z; local = c; }
                }
            }
            return best > float.MinValue;
        }
    }
}
