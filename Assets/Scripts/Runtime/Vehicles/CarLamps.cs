// Lights for a car that drives itself with no CarController (traffic, job cars): head and tail glows and a soft
// pool on the road ahead, on from dusk. Built by CarFx.StreetLamps the first night they are needed.
using StreetEmpire.City;
using UnityEngine;

namespace StreetEmpire.Vehicles
{
    public class CarLamps : MonoBehaviour
    {
        public bool On { get; private set; }
        GameObject lamps; DayCycle day; BoxCollider box;

        void Start() { box = GetComponent<BoxCollider>(); day = Object.FindFirstObjectByType<DayCycle>(); }

        void Update()
        {
            bool dark = day != null && day.Night > 0.3f;
            if (dark && lamps == null) lamps = CarFx.StreetLamps(transform, box);
            if (lamps == null) { On = false; return; }
            if (lamps.activeSelf != dark) lamps.SetActive(dark);
            On = dark;
        }
    }
}
