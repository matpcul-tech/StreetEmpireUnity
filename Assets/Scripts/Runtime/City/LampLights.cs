// v74 night: one real warm light under every street lamp (spec C.1), about 3000 K, range about 9 m, the URP smooth
// falloff (no hard edge), no shadows. They come on with the night and reach the wall beside the lamp and anyone
// standing under it. The ground decal pools of v73 are gone. URP shades each object with its nearest few: 4 on
// Ultra, 2 on High (GraphicsSetup).
using UnityEngine;

namespace StreetEmpire.City
{
    public class LampLights : MonoBehaviour
    {
        public Light[] lights = new Light[0];
        public DayCycle cycle;
        public float intensity = 4f;
        float last = -1f;

        void LateUpdate()
        {
            float night = cycle ? cycle.Night : 0f;
            if (Mathf.Abs(night - last) < 0.005f) return;
            last = night;
            bool on = night > 0.02f;
            foreach (var l in lights) if (l) { l.enabled = on; l.intensity = intensity * night; }
        }
    }
}
