// Enter and exit cars like state.js: Use within 5 m takes the nearest free car; leaving puts you
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
        public float footRadius = 5.2f, carRadius = 8.5f;

        public CarController Car { get; private set; }
        public bool scriptUse;

        PlayerMotor motor; CharacterController cc; Renderer[] renderers;

        void Awake() { motor = GetComponent<PlayerMotor>(); cc = GetComponent<CharacterController>(); renderers = GetComponentsInChildren<Renderer>(); }

        void Update()
        {
            bool use = scriptUse || PlayerControls.Instance.UsePressed; scriptUse = false;
            if (!use) return;
            if (Car != null) Exit(); else TryEnter();
        }

        void LateUpdate()
        {
            if (Car != null) transform.SetPositionAndRotation(Car.transform.position, Car.transform.rotation);
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
            if (best == null) return false;
            Car = best; Car.hasDriver = true;
            motor.enabled = false; cc.enabled = false;
            foreach (var r in renderers) r.enabled = false;
            // the camera backs off for a long vehicle
            var body = Car.GetComponent<BoxCollider>();
            SetCamera(Car.cameraTarget != null ? Car.cameraTarget : Car.transform, Mathf.Max(carRadius, body != null ? body.size.z * 1.25f + 2f : carRadius));
            return true;
        }

        public void Exit()
        {
            var car = Car; if (car == null) return;
            car.hasDriver = false; Car = null;
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
