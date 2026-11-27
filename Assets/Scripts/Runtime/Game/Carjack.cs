// Taking a car off somebody in traffic, from the web build (state.js, the 'car' action on an AI car): USE beside a
// traffic car pulls the driver out and puts you in the seat. It costs 6 heat, the driver lands on the street side
// shouting and runs, except one in five, who fights to get it back. The traffic car becomes a car that drives like any other you own.
using StreetEmpire.Sim.Data;
using StreetEmpire.Vehicles;
using UnityEngine;

namespace StreetEmpire.Game
{
    public static class Carjack
    {
        public const float Heat = 6f;
        /// <summary>Every Nerve-th driver fights for his car.</summary>
        public const int Nerve = 5;
        public static readonly string[] Lines = { "That is my car!", "Give it back!", "You are dead, you hear me?" };
        /// <summary>Cars taken this session.</summary>
        public static int Count;

        /// <summary>The nearest car in traffic that can be taken, within range.</summary>
        public static JobCar Nearest(Vector3 p, float range)
        {
            JobCar best = null; float bd = range * range;
            foreach (var c in JobCar.All)
            {
                if (c == null || !c.jackable || !c.enabled) continue;
                float d = (c.transform.position - p).sqrMagnitude;
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        /// <summary>Turn the traffic car into one the thief drives, and put its driver on the street.</summary>
        public static CarController Take(JobCar theirs, Transform thief)
        {
            if (theirs == null) return null;
            var go = theirs.gameObject; var t = go.transform; Vector3 at = t.position; float yaw = t.eulerAngles.y;
            var box = go.GetComponent<BoxCollider>(); float len = box != null ? box.size.z : 4.4f; bool big = len > 6.2f;
            var lamps = go.GetComponent<CarLamps>(); if (lamps != null) lamps.Remove();
            theirs.enabled = false; Object.Destroy(theirs);   // off the traffic lists now, gone at the end of the frame
            t.SetParent(null, true); go.name = "Car jacked";
            var car = go.AddComponent<CarController>();
            // the same figures a parked car gets (ParkedCars.Make)
            car.stats = new VehicleData { id = "jacked", maxSpeed = big ? Mathf.Lerp(23f, 18f, Mathf.InverseLerp(6.2f, 13f, len)) : 28f, accel = big ? 9f : 16f, hp = big ? 220f : 100f, wheelbase = Mathf.Clamp(len * 0.6f, 2.4f, 7.5f) };
            car.hp = car.stats.hp; car.Place(at, yaw);

            var s = GameSession.Instance; string line = Lines[Count % Lines.Length];
            // one driver in five wants the car back: he does not run, he comes for the thief (45 hp, fists)
            bool angry = Count % Nerve == Nerve - 1; Vector3 door = at - t.right * 2.2f;
            if (s != null) { s.AddHeat(Heat); s.Say(angry ? "You jacked a ride. This one wants it back." : $"You jacked a ride. \"{line}\"", 3f); }
            var turf = TurfWar.Instance; Health fighter = null;
            if (angry && turf != null) fighter = turf.SpawnHostile(StreetEmpire.Sim.Turf.Locals, door, false, "Angry driver", false);
            if (fighter != null) { fighter.max = fighter.hp = 45f; }
            else Driver(door, yaw, thief);
            Count++;
            return car;
        }

        /// <summary>The driver, out on the street side and running from the thief. One of the town's own people.</summary>
        static void Driver(Vector3 at, float yaw, Transform thief)
        {
            var life = TownLife.Instance; if (life == null || life.templates == null || life.templates.Length == 0) return;
            GameObject tpl = null;
            for (int k = 0; k < life.templates.Length && tpl == null; k++) { var c = life.templates[(Count + k) % life.templates.Length]; if (c != null && !BlockLife.StandsBadly(c)) tpl = c; }
            if (tpl == null) return;
            var who = Object.Instantiate(tpl, at + Vector3.up * 0.1f, Quaternion.Euler(0f, yaw, 0f));
            who.name = "Jacked driver";
            var townie = who.GetComponent<Townie>(); if (townie != null) Object.DestroyImmediate(townie);
            who.AddComponent<JackedDriver>().from = thief;
            who.SetActive(true);   // the templates are kept inactive
        }
    }
}
