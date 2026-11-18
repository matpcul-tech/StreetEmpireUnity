// Real light from the street lamps nearest the camera at night: a few unshadowed point lights moved onto the
// closest lamp heads each frame, so people and cars walking under a lamp are lit, not only the ground (the Lamp
// Pools mesh paints the pools on every lamp; this adds true light where the camera is). Ultra uses four, High two.
using StreetEmpire.UI;
using UnityEngine;

namespace StreetEmpire.City
{
    public class LampLights : MonoBehaviour
    {
        public Vector3[] lamps = new Vector3[0];   // lamp head positions
        public Light[] lights = new Light[0];
        public DayCycle cycle;
        public float intensity = 3.2f;
        readonly float[] best = new float[8]; readonly int[] pick = new int[8];

        void LateUpdate()
        {
            var cam = Camera.main; float night = cycle ? cycle.Night : 0f;
            int use = Mathf.Min(lights.Length, GraphicsQuality.Tier == GraphicsQuality.Ultra ? 4 : 2);
            bool on = cam != null && night > 0.02f && lamps.Length > 0;
            for (int i = 0; i < lights.Length; i++) if (lights[i]) lights[i].enabled = on && i < use;
            if (!on) return;
            var c = cam.transform.position;
            for (int k = 0; k < use; k++) { best[k] = float.MaxValue; pick[k] = -1; }
            for (int i = 0; i < lamps.Length; i++)
            {
                float d = (lamps[i] - c).sqrMagnitude;
                for (int k = 0; k < use; k++)
                    if (d < best[k]) { for (int j = use - 1; j > k; j--) { best[j] = best[j - 1]; pick[j] = pick[j - 1]; } best[k] = d; pick[k] = i; break; }
            }
            for (int k = 0; k < use; k++)
            {
                if (pick[k] < 0) { lights[k].enabled = false; continue; }
                lights[k].transform.position = lamps[pick[k]];
                lights[k].intensity = intensity * night;
            }
        }
    }
}
