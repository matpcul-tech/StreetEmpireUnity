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

        CarController car; float planT, stuckT, backT, leaveT, stillT; int stuckCount;
        public int StuckCount => stuckCount; bool direct = true; List<Vector3> path = new();

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

            // follow the streets: a path over the crossings to the street point nearest the goal, then straight in
            planT -= dt;
            if (planT <= 0 || path.Count == 0)
            {
                planT = 1f;
                path = StreetRoute.Path(s, transform.position, goal);
                // drop crossings already reached or behind (it is driving away from them)
                while (path.Count > 1 && Flat(path[0] - transform.position).magnitude < 7f) path.RemoveAt(0);
            }
            while (path.Count > 1 && Flat(path[0] - transform.position).magnitude < 7f) path.RemoveAt(0);
            direct = path.Count <= 1 && (Flat(goal - transform.position).magnitude < 20f || StreetRoute.Clear(transform.position, goal, 1.1f));
            var wp = direct ? goal : path[0];
            var to = Flat(wp - transform.position);
            bool turnAhead = path.Count > 1;
            float want = Mathf.Atan2(to.x, to.z), yaw = car.state.yaw;
            float diff = Mathf.Atan2(Mathf.Sin(want - yaw), Mathf.Cos(want - yaw));
            float thr = Mathf.Abs(diff) < 1.3f ? 1f : 0.35f;
            // slow for a corner: no faster than it can turn in the distance left
            if (turnAhead && to.magnitude < 25f && Mathf.Abs(car.Speed) > 13f) thr = -0.6f;
            // jammed against a wall or a car: back up for a second with the wheel the other way, then replan
            // (the JS backs up only until it is moving, which is a few centimeters here)
            stuckT = Mathf.Abs(car.Speed) < 1f && backT <= 0 ? stuckT + dt : 0;
            if (stuckT > 1.2f) { stuckT = 0; backT = 1.1f; stuckCount++; planT = 0; }
            if (backT > 0) { backT -= dt; thr = -1; diff = -diff; }
            // stop short of a player on foot: start braking with room to stop (it brakes at about 34 m/s2, the JS
            // brakes at 16 m, which at full speed would carry it onto the player here)
            if (!going && !pCar && (d < 16f || d < 9f + car.Speed * car.Speed / 40f)) thr = car.Speed > 2f ? -1f : d > 10f ? 0.25f : 0f;
            if (!going && pCar && d < 8f) thr = 0.6f;
            car.aiThrottle = thr; car.aiSteer = Mathf.Clamp(diff * 2f, -1f, 1f);

            if (!going && d < 16f && Mathf.Abs(car.Speed) < 3f && !pCar) Unload(s);
            // a cruiser that keeps jamming lets its officers out to run the rest (not in the JS, where its
            // path finding gets it through; the corner routing here is simpler)
            else if (!going && !pCar && stuckCount >= 3 && Mathf.Abs(car.Speed) < 3f) Unload(s);
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
