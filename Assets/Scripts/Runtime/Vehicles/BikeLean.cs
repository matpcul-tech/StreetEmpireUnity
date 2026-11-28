// A motorcycle leans into its turns as far as the turn pulls it sideways (speed squared over the turn's radius), up
// to 32 degrees. The lean turns the drawn model about the line its tyres stand on; the collider stays upright. The
// rider leans with it (VehicleUser reads Roll).
using UnityEngine;

namespace StreetEmpire.Vehicles
{
    [RequireComponent(typeof(CarController))]
    public class BikeLean : MonoBehaviour
    {
        public float maxLean = 32f, ease = 6f;
        /// <summary>Degrees about the bike's own length: negative leans right, as Unity turns about +Z.</summary>
        public float Roll { get; private set; }
        CarController car; Transform model;

        void Awake() { car = GetComponent<CarController>(); model = transform.Find("Model"); }

        /// <summary>The lean that balances the turn: steering angle (radians, positive right), speed (m/s) and wheelbase
        /// give the sideways pull, and the bike tips until gravity answers it. Nothing at a standstill, a lot at speed
        /// even with the bars barely turned.</summary>
        public static float LeanFor(float steer, float speed, float wheelbase, float maxLean)
            => Mathf.Clamp(-Mathf.Atan(speed * speed * Mathf.Tan(steer) / (Mathf.Max(0.5f, wheelbase) * 9.81f)) * Mathf.Rad2Deg, -maxLean, maxLean);

        void Update()
        {
            float want = LeanFor(car.state.steer, car.Speed, car.stats.wheelbase, maxLean);
            Roll = Mathf.Lerp(Roll, want, 1f - Mathf.Exp(-ease * Time.deltaTime));
            if (model != null) model.localRotation = Quaternion.Euler(0f, 0f, Roll);
        }
    }
}
