// A drivable car: CarPhysics (the JS driving model) plus building and car collisions and ground following.
using System.Text.RegularExpressions;
using StreetEmpire.City;
using StreetEmpire.Player;
using StreetEmpire.Sim;
using StreetEmpire.Sim.Data;
using UnityEngine;

namespace StreetEmpire.Vehicles
{
    [RequireComponent(typeof(BoxCollider))]
    public class CarController : MonoBehaviour
    {
        public VehicleData stats = new VehicleData { id = "sedan", maxSpeed = 28, accel = 16, hp = 100, wheelbase = 2.7f };
        public Transform cameraTarget;
        public float hp;
        public bool hasDriver;

        // scripted input for tests; null means read the player's controls while someone drives
        public float? scriptThrottle, scriptSteer;
        public bool scriptHandbrake;
        // AI drivers (police cruisers) set these every frame instead
        public bool aiDriven; public float aiThrottle, aiSteer;

        public readonly CarState state = new CarState();
        public float Speed => state.v;

        BoxCollider box;
        Transform[] wheels;
        static readonly Regex Wheel = new Regex(@"^(?!.*steer)(?=.*(?:^|[^a-z])(wheel|tire|tyre|rim))", RegexOptions.IgnoreCase);
        readonly Collider[] hits = new Collider[16];

        void Awake()
        {
            box = GetComponent<BoxCollider>();
            hp = stats.hp;
            state.x = transform.position.x; state.z = transform.position.z; state.yaw = transform.eulerAngles.y * Mathf.Deg2Rad;
            var all = GetComponentsInChildren<Transform>();
            wheels = System.Array.FindAll(all, t => t != transform && Wheel.IsMatch(t.name) && t.GetComponentInChildren<Renderer>() != null);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            float throttle = 0, steer = 0; bool hb = false;
            if (hasDriver)
            {
                var c = PlayerControls.Instance;
                var m = c.Move;
                throttle = scriptThrottle ?? Throttle(m.y, c.Gas, c.Reverse, PlayerControls.TouchPedals); steer = scriptSteer ?? m.x; hb = scriptHandbrake || c.Sprint;
            }
            else if (aiDriven) { throttle = aiThrottle; steer = aiSteer; }
            CarPhysics.Drive(state, stats, throttle, steer, hb, dt);
            Collide();
            float y = GroundY(new Vector3(state.x, transform.position.y, state.z));
            transform.SetPositionAndRotation(new Vector3(state.x, y, state.z), Quaternion.Euler(0, state.yaw * Mathf.Rad2Deg, 0));
            foreach (var w in wheels) w.Rotate(state.v / 0.34f * Mathf.Rad2Deg * dt, 0, 0, Space.Self);
        }

        /// <summary>Pedals win over the stick. On a phone the stick only steers, so a thumb that drifts up or
        /// down while turning does not speed up, brake or back the car up. REV brakes first, then reverses.</summary>
        public static float Throttle(float stickY, bool gas, bool reverse, bool pedalsOnly)
        {
            if (gas || reverse || pedalsOnly) return (gas ? 1f : 0f) - (reverse ? 1f : 0f);
            return stickY;
        }

        void Collide()
        {
            var rot = Quaternion.Euler(0, state.yaw * Mathf.Rad2Deg, 0);
            var pos = new Vector3(state.x, transform.position.y, state.z);
            var center = pos + rot * box.center;
            int n = Physics.OverlapBoxNonAlloc(center, box.size * 0.5f, hits, rot, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var other = hits[i];
                if (other == box || other.transform.IsChildOf(transform)) continue;
                // parked vehicles are solid to the player's car (they only have a collider and a PropMarker until
                // someone takes one). Police cruisers pass them as before: held up at a curb they never reach the player.
                var prop = aiDriven ? null : other.GetComponent<PropMarker>();
                bool wall = other.GetComponent<BuildingMarker>() != null || other.GetComponentInParent<CarController>() != null || other.GetComponentInParent<StreetEmpire.Game.JobCar>() != null || (prop != null && prop.kind == "parked car");
                if (!wall) continue;
                if (Physics.ComputePenetration(box, pos, rot, other, other.transform.position, other.transform.rotation, out var dir, out var dist))
                {
                    dir.y = 0; if (dir.sqrMagnitude < 1e-6f) continue; dir.Normalize();
                    state.x += dir.x * (dist + 0.01f); state.z += dir.z * (dist + 0.01f);
                    pos = new Vector3(state.x, pos.y, state.z);
                    hp -= CarPhysics.HitWall(state, dir.x, dir.z);
                }
            }
        }

        float GroundY(Vector3 p)
        {
            float best = 0f;
            int n = Physics.RaycastNonAlloc(p + Vector3.up * 3f, Vector3.down, rayHits, 6f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = rayHits[i].collider;
                if (c == box || c.transform.IsChildOf(transform) || c.GetComponent<BuildingMarker>() != null || c is CharacterController) continue;
                best = Mathf.Max(best, rayHits[i].point.y);
            }
            return best;
        }
        readonly RaycastHit[] rayHits = new RaycastHit[8];
    }
}
