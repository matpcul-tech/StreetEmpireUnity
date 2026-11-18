// v75 night: a real warm spot light under every street lamp head, pointing straight down (a 100 degree cone,
// range about 13 m, about 3000 K, no shadows), so each lamp throws a pool on the road and sidewalk with a soft edge
// and still reaches the wall beside it and anyone standing under it. They follow DayCycle.Lamps (a little at the
// golden hour, full from the blue hour on). URP shades each object with its nearest few lights: 6 on Ultra,
// 3 on High (GraphicsSetup).
using UnityEngine;

namespace StreetEmpire.City
{
    public class LampLights : MonoBehaviour
    {
        public Light[] lights = new Light[0];
        public DayCycle cycle;
        public float intensity = 80f;   // v75b: 34 left the pools faint at 22:00
        float last = -1f;

        void LateUpdate()
        {
            float lamps = cycle ? cycle.Lamps : 0f;
            if (Mathf.Abs(lamps - last) < 0.005f) return;
            last = lamps;
            bool on = lamps > 0.02f;
            foreach (var l in lights) if (l) { l.enabled = on; l.intensity = intensity * lamps; }
        }
    }
}
