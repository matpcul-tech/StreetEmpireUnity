// v75 night: a real warm spot light under every street lamp head, pointing straight down (a 100 degree cone,
// range about 13 m, about 3000 K, no shadows), so each lamp throws a pool on the road and sidewalk with a soft edge
// and still reaches the wall beside it and anyone standing under it. They follow DayCycle.Lamps (off by day,
// up through dusk, full from the blue hour on). URP shades each object with its nearest few lights: 6 on Ultra,
// 3 on High (GraphicsSetup), and the street is in chunks (SceneBuilder) so each chunk has its own lamps.
// v75c: URP keeps only so many visible lights per frame (16 on WebGL and many phones) and drops the rest in its
// own order, not by distance, so with 35 lamps on the near ones could be the dropped ones (no pool at all under
// the lamp in front of the camera). Only the nearest maxOn lamps to the camera are switched on, rechecked a few
// times a second. The lamp globes glow with the same level, so they are dark by day.
using UnityEngine;

namespace StreetEmpire.City
{
    public class LampLights : MonoBehaviour
    {
        public Light[] lights = new Light[0];
        public DayCycle cycle;
        public float intensity = 45f;   // v75b raised 34 to 80 for faint pools; v75c: the faint pools were per vertex lighting (GraphicsSetup), 45 per pixel
        public int maxOn = 11;   // night look: one fewer, the storefront spills share the cap
        /// <summary>The street lamp globe material (PropBuilder); a run-time copy glows with the lamp level.</summary>
        public Material globe;
        public PropInstancer instancer;
        public Color globeColor = new(1f, 0.7f, 0.4f);
        public float globeIntensity = 2.4f;
        float last = -1f, nextSort;
        Material live;
        int[] order = new int[0]; float[] dist = new float[0];
        public Material LiveGlobe => live;

        void Awake()
        {
            if (globe != null)
            {
                live = new Material(globe) { name = globe.name + " (live)" };
                if (instancer) instancer.Replace(globe, live);
                live.SetColor("_EmissionColor", Color.black);
            }
        }

        void LateUpdate()
        {
            float lamps = cycle ? cycle.Lamps : 0f;
            bool on = lamps > 0.02f;
            bool changed = Mathf.Abs(lamps - last) >= 0.005f;
            if (changed)
            {
                last = lamps;
                if (live) live.SetColor("_EmissionColor", globeColor * globeIntensity * lamps);
                foreach (var l in lights) if (l) l.intensity = intensity * lamps;
            }
            if (!on) { if (changed) foreach (var l in lights) if (l) l.enabled = false; return; }
            if (!changed && Time.unscaledTime < nextSort) return;
            nextSort = Time.unscaledTime + 0.25f;
            var cam = Camera.main; var at = cam ? cam.transform.position : Vector3.zero;
            if (order.Length != lights.Length) { order = new int[lights.Length]; dist = new float[lights.Length]; }
            for (int i = 0; i < lights.Length; i++) { order[i] = i; dist[i] = lights[i] ? (lights[i].transform.position - at).sqrMagnitude : float.MaxValue; }
            System.Array.Sort(dist, order);
            for (int k = 0; k < order.Length; k++) { var l = lights[order[k]]; if (l) l.enabled = k < maxOn; }
        }
    }
}
