// Moving traffic: a handful of cars driving the streets around the player. Each is a JobCar that keeps the rules
// (waits at red lights and stop signs, stops behind what is in its lane), wearing a parked car's body. They come in
// on a street 70 to 130 m from the player and are taken away past 175 m, so the town has traffic wherever you are
// for the price of eight cars.
using System.Collections.Generic;
using StreetEmpire.City;
using UnityEngine;

namespace StreetEmpire.Game
{
    public class TrafficFlow : MonoBehaviour
    {
        public static TrafficFlow Instance { get; private set; }
        /// <summary>Off in the Editor, where the tests run: a test that wants traffic turns it on before the scene loads.</summary>
#if UNITY_EDITOR
        public static bool AutoStart = false;
#else
        public static bool AutoStart = true;
#endif
        public int max = 8;
        public float near = 70f, far = 130f, drop = 175f, every = 1.5f;
        public IReadOnlyList<JobCar> Cars => cars;

        static readonly string[] Kinds = { "sedan-clean", "sedan", "coupe", "lowrider", "pickup", "suv", "taxi", "van", "hatchback", "minivan", "luxury-sedan" };
        readonly List<JobCar> cars = new(); List<PropBatch> bodies; float t;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            var s = GameSession.Instance; if (!AutoStart || s == null || s.player == null) return;
            var p = s.player.transform.position;
            for (int i = cars.Count - 1; i >= 0; i--)
            {
                var c = cars[i];
                if (c == null) { cars.RemoveAt(i); continue; }
                if ((c.transform.position - p).sqrMagnitude > drop * drop) { Destroy(c.gameObject); cars.RemoveAt(i); }
            }
            t -= Time.deltaTime; if (t > 0f || cars.Count >= max) return;
            t = every;
            if (!s.SpawnPoint(out var at, near, far, 3f)) return;
            foreach (var o in JobCar.All) if (o != null && (o.transform.position - at).sqrMagnitude < 16f * 16f) return;
            var car = JobCar.Spawn(at, Random.Range(7.5f, 10.5f), Body());
            if (car == null) return;
            car.obeyLights = true; car.name = "Traffic car"; car.transform.SetParent(transform, true);
            cars.Add(car);
        }

        /// <summary>A body from the parked cars' batches: the everyday kinds, any paint.</summary>
        PropBatch Body()
        {
            if (bodies == null)
            {
                bodies = new List<PropBatch>();
                var inst = Object.FindFirstObjectByType<PropInstancer>();
                if (inst != null && inst.batches != null)
                    foreach (var b in inst.batches)
                    {
                        if (b == null || b.name == null || b.lod0 == null || !b.name.StartsWith("car-")) continue;
                        // the kind is what follows "car-" up to a variant mark, with a paint on the end for a repaint
                        string kind = b.name.Substring(4); int cut = kind.IndexOfAny(new[] { '@', '#' }); if (cut >= 0) kind = kind.Substring(0, cut);
                        foreach (var k in Kinds) if (kind == k || (kind.StartsWith(k + "-") && !(k == "sedan" && kind.StartsWith("sedan-clean")))) { bodies.Add(b); break; }
                    }
            }
            return bodies.Count > 0 ? bodies[Random.Range(0, bodies.Count)] : null;
        }
    }
}
