// Enter and exit cars like state.js: Use within 5 m takes the nearest free car, or a moving one off its driver
// (Carjack); leaving puts you
// 2.4 m out of the driver's door (the left side), or the right side if a wall is in the way.
using StreetEmpire.City;
using StreetEmpire.Vehicles;
using Unity.Cinemachine;
using UnityEngine;

namespace StreetEmpire.Player
{
    [RequireComponent(typeof(PlayerMotor))]
    public class VehicleUser : MonoBehaviour
    {
        public float enterRange = 5f, exitOffset = 2.4f;
        public CinemachineCamera followCamera;
        public Transform footCameraTarget;
        public float footRadius = CameraRig.FootRadius, carRadius = 8.5f;

        public CarController Car { get; private set; }
        /// <summary>On a motorcycle: seated in the open, not hidden inside.</summary>
        public bool Riding { get; private set; }
        /// <summary>Where his feet-level root sits on a bike, from the bike's own origin on the road (the seated clip
        /// drops his hips to chair height, so the root rides above the ground), and the camera distance.</summary>
        public Vector3 bikeSeat = new Vector3(0f, 0.3f, -0.14f);   // first photos: at -0.3 he sat on the tail, short of the bars
        public float bikeRadius = 5.4f;
        public static readonly int RidingId = Animator.StringToHash("Riding");
        BikeLean lean;
        public bool scriptUse;

        PlayerMotor motor; CharacterController cc; Renderer[] renderers;

        void Awake() { motor = GetComponent<PlayerMotor>(); cc = GetComponent<CharacterController>(); renderers = GetComponentsInChildren<Renderer>(); }

        void Update()
        {
            bool scripted = scriptUse;
            bool use = scriptUse || PlayerControls.Instance.UsePressed; scriptUse = false;
            if (!use) return;
            // beside a contact with a job to give, Use talks to them
            if (Car == null && !scripted && StreetEmpire.Game.MissionRunner.TalkNear(transform.position)) return;
            if (Car != null) Exit(); else TryEnter();
        }

        void LateUpdate()
        {
            if (Car == null) return;
            if (Riding)
            {
                // in the saddle, leaning with the bike about the line its tyres stand on
                var ct = Car.transform; var roll = Quaternion.Euler(0f, 0f, lean != null ? lean.Roll : 0f);
                transform.SetPositionAndRotation(ct.position + ct.rotation * (roll * bikeSeat), ct.rotation * roll);
            }
            else transform.SetPositionAndRotation(Car.transform.position, Car.transform.rotation);
        }

        public bool TryEnter()
        {
            CarController best = null; float bd = enterRange * enterRange;
            foreach (var c in FindObjectsByType<CarController>(FindObjectsSortMode.None))
            {
                if (c.hasDriver) continue;
                float d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = c; }
            }
            // no car that already drives in reach: take a parked one (ParkedCars swaps it for a car that drives)
            if (best == null) { var parked = ParkedCars.Nearest(transform.position, enterRange * 0.6f); if (parked != null) best = ParkedCars.Take(parked); }
            // nothing free: pull somebody out of the traffic
            if (best == null) { var theirs = StreetEmpire.Game.Carjack.Nearest(transform.position, enterRange); if (theirs != null) best = StreetEmpire.Game.Carjack.Take(theirs, transform); }
            if (best == null) return false;
            Car = best; Car.hasDriver = true;
            motor.enabled = false; cc.enabled = false;
            // in a car he is out of sight; on a motorcycle he is on show, seated with his hands out on the bars
            Riding = Car.bike; lean = Riding ? Car.GetComponent<BikeLean>() : null;
            if (!Riding) foreach (var r in renderers) r.enabled = false;
            if (motor.animator != null) motor.animator.SetBool(RidingId, Riding);
            // the camera backs off for a long vehicle
            var body = Car.GetComponent<BoxCollider>();
            SetCamera(Car.cameraTarget != null ? Car.cameraTarget : Car.transform, Riding ? bikeRadius : Mathf.Max(carRadius, body != null ? body.size.z * 1.25f + 2f : carRadius));
            return true;
        }

        public void Exit()
        {
            var car = Car; if (car == null) return;
            car.hasDriver = false; Car = null;
            Riding = false; lean = null; if (motor.animator != null) motor.animator.SetBool(RidingId, false);
            var t = car.transform;
            Vector3 pos = t.position - t.right * exitOffset;
            if (Blocked(pos)) pos = t.position + t.right * exitOffset;
            foreach (var r in renderers) r.enabled = true;
            motor.enabled = true;
            motor.Teleport(pos + Vector3.up * 0.1f, t.eulerAngles.y);
            SetCamera(footCameraTarget, footRadius);
        }

        static bool Blocked(Vector3 p)
        {
            foreach (var c in Physics.OverlapSphere(p + Vector3.up, 0.5f, ~0, QueryTriggerInteraction.Ignore))
                if (c.GetComponent<BuildingMarker>() != null || c.GetComponentInParent<CarController>() != null) return true;
            return false;
        }

        void SetCamera(Transform target, float radius)
        {
            if (followCamera == null) return;
            followCamera.Follow = target; followCamera.LookAt = target;
            var orbit = followCamera.GetComponent<CinemachineOrbitalFollow>();
            if (orbit != null) orbit.Radius = radius;
            var rig = followCamera.GetComponent<CameraRig>();
            if (rig != null) { rig.alignTo = Car != null ? Car.transform : null; rig.radius = radius; }
        }
    }
}
