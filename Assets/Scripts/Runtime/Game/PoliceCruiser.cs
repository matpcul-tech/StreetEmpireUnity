// A police cruiser, from the "cruisers chase" part of policeStep() in state.js: it spawns on a street 60 to
// 110 m out with its siren on, drives at the player (by the street corners when a block is in the way), and
// lets its officers out once it is within 16 m, slower than 3 m/s and the player is on foot. With the player
// in a car it keeps chasing and pushes in close. When the stars are gone or after a bust it drives off and
// is cleared, and a cruiser whose officers are out sits until they are done.
using System.Collections.Generic;
using StreetEmpire.Player;
using StreetEmpire.Vehicles;
using UnityEngine;

namespace StreetEmpire.Game
{
    [RequireComponent(typeof(CarController))]
    public class PoliceCruiser : MonoBehaviour
    {
        public int riders;                 // officers still inside
        public float riderHp = 80;
        public bool leave;
        public bool Unloaded { get; private set; }
        public float SpawnDistance { get; set; }
        public readonly List<Cop> crew = new();

        CarController car; float planT, stuckT, leaveT, stillT; bool direct = true; Vector3 waypoint;

        void Awake() { car = GetComponent<CarController>(); car.aiDriven = true; }

        void FixedUpdate()
        {
            var s = GameSession.Instance;
            if (s == null || s.player == null) return;
            float dt = Time.fixedDeltaTime;
            var p = s.player.transform.position;
            var user = s.player.GetComponent<VehicleUser>();
            bool pCar = user != null && user.Car != null;
            float d = Flat(p - transform.position).magnitude;

            if (riders == 0)
            {
                // parked: brake, and clear out once its officers are done or the chase is over
                car.aiThrottle = Mathf.Abs(car.Speed) > 0.5f ? -Mathf.Sign(car.Speed) : 0; car.aiSteer = 0;
                crew.RemoveAll(c => c == null);
                bool waiting = crew.Exists(c => !c.leave && !c.GetComponent<Health>().Down && Flat(c.transform.position - transform.position).magnitude < 45f);
                leaveT += dt;
                if (s.Stars == 0 || leave) { if ((!waiting && leaveT > 2) || leaveT > 14 || d > 80) Destroy(gameObject); }
                else if (!waiting) { if (leaveT > 10) Destroy(gameObject); }
                else leaveT = 0;
                return;
            }

            bool going = leave || s.Stars == 0;
            Vector3 goal = p;
            if (going) { var away = Flat(transform.position - p); goal = transform.position + (away.sqrMagnitude > 0.01f ? away.normalized : transform.forward) * 60f; }

            planT -= dt;
            if (planT <= 0)
            {
                planT = 0.25f;
                direct = Flat(goal - transform.position).magnitude < 6f || StreetRoute.Clear(transform.position, goal, 1.1f);
                if (!direct) waypoint = StreetRoute.Corner(s, transform.position, goal, 1.1f, 4f);
            }
            var wp = direct ? goal : waypoint;
            var to = Flat(wp - transform.position);
            if (!direct && to.magnitude < 6f) planT = 0;
            float want = Mathf.Atan2(to.x, to.z), yaw = car.state.yaw;
            float diff = Mathf.Atan2(Mathf.Sin(want - yaw), Mathf.Cos(want - yaw));
            float thr = Mathf.Abs(diff) < 1.3f ? 1f : 0.35f;
            // slow for a corner: no faster than it can turn in the distance left
            if (!direct && to.magnitude < 25f && Mathf.Abs(car.Speed) > 13f) thr = -0.6f;
            stuckT = Mathf.Abs(car.Speed) < 1f ? stuckT + dt : 0;
            if (!going && stuckT > 1.5f) { thr = -1; diff = -diff; if (stuckT > 3f) stuckT = 0; }
            if (!going && d < 16f && !pCar) thr = car.Speed > 2f ? -1f : 0f;
            if (!going && pCar && d < 8f) thr = 0.6f;
            car.aiThrottle = thr; car.aiSteer = Mathf.Clamp(diff * 2f, -1f, 1f);

            if (!going && d < 16f && Mathf.Abs(car.Speed) < 3f && !pCar) Unload(s);
            if (going)
            {
                leaveT += dt; stillT = Mathf.Abs(car.Speed) < 1f ? stillT + dt : 0;
                if (d > 90f || leaveT > 8f || stillT > 2.5f) Destroy(gameObject);
            }
            else { leaveT = 0; stillT = 0; }
        }

        void Unload(GameSession s)
        {
            Unloaded = true;
            var right = transform.right;
            for (int i = 0; i < riders; i++)
            {
                // out of the doors on both sides, the way the JS puts them 2 m either side of the car
                var side = (i % 2 == 0 ? -1f : 1f) * 2.2f;
                var at = transform.position + right * side - transform.forward * (i / 2) * 1.2f + Vector3.up * 0.1f;
                if (InWall(at)) at = transform.position - right * side - transform.forward * (i / 2) * 1.2f + Vector3.up * 0.1f;
                var cop = s.SpawnOfficer(at, riderHp);
                cop.cruiser = this;
                crew.Add(cop);
            }
            riders = 0; leaveT = 0;
        }

        static bool InWall(Vector3 at)
        {
            foreach (var c in Physics.OverlapSphere(at + Vector3.up * 0.9f, 0.45f, ~0, QueryTriggerInteraction.Ignore))
                if (c.GetComponent<StreetEmpire.City.BuildingMarker>() != null) return true;
            return false;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    }
}
