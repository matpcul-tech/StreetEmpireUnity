// A motorcycle leans into its turns: the harder the bars are turned and the faster it goes, the further over, up to
// 32 degrees. The lean turns the drawn model about the line its tyres stand on; the collider stays upright. The
// rider leans with it (VehicleUser reads Roll).
using UnityEngine;

namespace StreetEmpire.Vehicles
{
    [RequireComponent(typeof(CarController))]
    public class BikeLean : MonoBehaviour
    {
        public float maxLean = 32f, fullLeanSpeed = 14f, ease = 6f;
        /// <summary>Degrees about the bike's own length: negative leans right, as Unity turns about +Z.</summary>
        public float Roll { get; private set; }
        CarController car; Transform model;

        void Awake() { car = GetComponent<CarController>(); model = transform.Find("Model"); }

        /// <summary>The lean for a steering angle (radians, positive right) at a speed (m/s).</summary>
        public static float LeanFor(float steer, float speed, float maxLean, float fullLeanSpeed)
            => Mathf.Clamp(-steer * Mathf.Rad2Deg * 1.4f * Mathf.Clamp01(Mathf.Abs(speed) / fullLeanSpeed), -maxLean, maxLean);

        void Update()
        {
            float want = LeanFor(car.state.steer, car.Speed, maxLean, fullLeanSpeed);
            Roll = Mathf.Lerp(Roll, want, 1f - Mathf.Exp(-ease * Time.deltaTime));
            if (model != null) model.localRotation = Quaternion.Euler(0f, 0f, Roll);
        }
    }
}
