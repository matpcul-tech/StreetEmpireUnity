// Every parked car, van, truck and bus can be driven. Parked vehicles are drawn by PropInstancer as copies of one
// mesh with only a collider and a PropMarker of their own, so taking one swaps it: its copy leaves the instancer,
// and the same mesh and materials go on the parked object with a CarController. Bikes and the semi trailer stay put.
using StreetEmpire.City;
using StreetEmpire.Sim.Data;
using UnityEngine;

namespace StreetEmpire.Vehicles
{
    public static class ParkedCars
    {
        static readonly string[] Fixed = { "cruiser", "dirt-bike", "sport-bike", "semi-trailer" };

        public static bool Drivable(PropMarker m) => m != null && m.kind == "parked car" && System.Array.IndexOf(Fixed, m.model) < 0 && m.GetComponent<CarController>() == null;

        /// <summary>The nearest parked vehicle the player can take within range of p, or null.</summary>
        public static PropMarker Nearest(Vector3 p, float range)
        {
            PropMarker best = null; float bd = float.MaxValue;
            foreach (var m in Object.FindObjectsByType<PropMarker>(FindObjectsSortMode.None))
            {
                if (!Drivable(m)) continue;
                var box = m.GetComponent<BoxCollider>(); if (box == null) continue;
                // range from the body, not the middle: a bus is 12 m long
                float d = (box.ClosestPoint(p) - p).magnitude;
                if (d < range && d < bd) { bd = d; best = m; }
            }
            return best;
        }

        /// <summary>Make a parked vehicle a car that drives. Null if its drawn copy cannot be found.</summary>
        public static CarController Take(PropMarker m)
        {
            if (!Drivable(m)) return null;
            var inst = Object.FindFirstObjectByType<PropInstancer>(); if (inst == null) return null;
            Vector3 at = m.transform.position;
            foreach (var b in inst.batches)
            {
                if (b == null || b.matrices == null || b.name == null || !b.name.StartsWith("car-")) continue;
                for (int i = 0; i < b.matrices.Length; i++)
                {
                    Vector3 c = b.matrices[i].GetColumn(3);
                    if ((c - at).sqrMagnitude > 0.01f) continue;
                    var rest = new Matrix4x4[b.matrices.Length - 1];
                    System.Array.Copy(b.matrices, 0, rest, 0, i); System.Array.Copy(b.matrices, i + 1, rest, i, rest.Length - i);
                    b.matrices = rest;
                    return Make(m, b);
                }
            }
            return null;
        }

        static CarController Make(PropMarker m, PropBatch b)
        {
            var go = m.gameObject; string kind = m.model;
            go.isStatic = false; go.name = "Car " + kind;
            var model = new GameObject("Model"); model.transform.SetParent(go.transform, false);
            model.AddComponent<MeshFilter>().sharedMesh = b.lod0;
            model.AddComponent<MeshRenderer>().sharedMaterials = b.materials;
            float len = go.GetComponent<BoxCollider>().size.z;
            bool big = len > 6.2f;
            Object.Destroy(m);
            var car = go.AddComponent<CarController>();
            // heavier and slower with length: a sedan near 28 m/s, a bus near 19
            car.stats = new VehicleData { id = kind, maxSpeed = big ? Mathf.Lerp(23f, 18f, Mathf.InverseLerp(6.2f, 13f, len)) : 28f, accel = big ? 9f : 16f, hp = big ? 220f : 100f, wheelbase = Mathf.Clamp(len * 0.6f, 2.4f, 7.5f) };
            car.hp = car.stats.hp;
            return car;
        }
    }
}
