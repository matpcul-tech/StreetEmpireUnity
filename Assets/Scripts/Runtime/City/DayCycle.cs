// The sun follows the clock the way updateDay() in the web game's main.js does: angle (hour - 6) / 24 of a turn,
// so it sets at 18:00, about 4 s into a new game. The built scene is the golden hour look (dusk); this blends
// from it to a peach day when the sun is high and to a blue purple moonlit night (with a strong sky fill so
// nothing goes black, the window lights up) after it sets. The sky cubemap is dimmed and tinted by the same
// amount as the fog, so its horizon stays the fog color and the district edge stays hidden.
// v74 night: the sun goes out and a blue gray moon (its own directional light, from another side) gives every
// building a lit and a dark side; a real warm light under each street lamp (LampLights); ambient a little higher
// than v73 and close to neutral, so the dark between the lamps reads. Windows: only the windows_lit glass glows, a
// flat (1.0, 0.74, 0.45) at HDR intensity 2 (x4), by the clock: off 07:00 to 18:00, up from 18:00 to 19:30, full to
// 05:30, down to 07:00. The character fill light follows the same curve.
using StreetEmpire.Game;
using UnityEngine;

namespace StreetEmpire.City
{
    public class DayCycle : MonoBehaviour
    {
        public Light sun, fill, moon;
        public Material sky;
        /// <summary>0 by day and at dusk, 1 at full night (LampLights reads it).</summary>
        public float Night { get; private set; }
        [Header("Built golden hour values (filled by the scene builder)")]
        public Color duskSun = new(1f, 0.77f, 0.58f), duskFog = new(0.68f, 0.61f, 0.54f);
        public Color duskAmbSky = new(0.63f, 0.61f, 0.735f), duskAmbEq = new(0.57f, 0.53f, 0.50f), duskAmbGround = new(0.40f, 0.33f, 0.31f);
        public float duskSunIntensity = 0.95f, duskElevation = 7f, reflection = 0.35f, fillIntensity = 0.55f, moonIntensity = 0.32f;
        public Vector2 azimuth = new(0.8f, 0.45f);
        public Material[] windowMaterials = new Material[0];

        public static readonly Color WindowLight = new(1f, 0.74f, 0.45f);
        public const float WindowHdrIntensity = 2f;   // Unity HDR intensity in stops: x 2^2
        static readonly Color DaySun = new(1f, 0.94f, 0.86f), MoonColor = new(0.62f, 0.68f, 0.80f);
        static readonly Color DayFogLin = new Color(0.70f, 0.63f, 0.55f).linear, NightMul = new(0.11f, 0.115f, 0.135f);
        float lastNight = -1;

        void Start()
        {
            // work on copies: the sky and the building materials are assets, and play mode in the editor (the tests)
            // would otherwise write the night values into them
            if (sky) { sky = new Material(sky); RenderSettings.skybox = sky; }
            var map = new System.Collections.Generic.Dictionary<Material, Material>();
            foreach (var m in windowMaterials) if (m && !map.ContainsKey(m)) map[m] = new Material(m);
            if (map.Count > 0)
                foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var ms = r.sharedMaterials; bool hit = false;
                    for (int i = 0; i < ms.Length; i++) if (ms[i] != null && map.TryGetValue(ms[i], out var c)) { ms[i] = c; hit = true; }
                    if (hit) r.sharedMaterials = ms;
                }
            windowMaterials = new Material[map.Count]; map.Values.CopyTo(windowMaterials, 0);
        }

        /// <summary>Window light by the clock (spec B.4): 0 from 07:00 to 18:00, up from 18:00 to 19:30, 1 until 05:30,
        /// down to 0 at 07:00.</summary>
        public static float WindowRamp(float h)
        {
            h = Mathf.Repeat(h, 24f);
            if (h >= 7f && h < 18f) return 0f;
            if (h >= 18f && h < 19.5f) return (h - 18f) / 1.5f;
            if (h >= 5.5f && h < 7f) return 1f - (h - 5.5f) / 1.5f;
            return 1f;
        }

        public static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        void Update()
        {
            float h = GameClock.Hour;
            float ang = (h - 6f) / 24f * Mathf.PI * 2f, elev = Mathf.Sin(ang);
            float day = Smooth(0.05f, 0.35f, elev);                       // the built look holds through the low sun
            float night = 1f - Smooth(-0.12f, 0.03f, elev); Night = night;
            // the sun keeps the built azimuth; its height follows the clock (never below the built 7 deg so shadows
            // stay sane); it fades out after sunset and the moon takes over
            float e = Mathf.Max(duskElevation, Mathf.Asin(Mathf.Clamp(elev, -1f, 1f)) * Mathf.Rad2Deg);
            var hz = azimuth.normalized; float er = e * Mathf.Deg2Rad;
            var dir = new Vector3(hz.x * Mathf.Cos(er), Mathf.Sin(er), hz.y * Mathf.Cos(er));
            if (sun)
            {
                sun.transform.rotation = Quaternion.LookRotation(-dir);
                sun.color = Color.Lerp(duskSun, DaySun, day);
                sun.intensity = Mathf.Lerp(duskSunIntensity, 1.4f, day) * (1f - night);
                sun.enabled = night < 0.98f;
            }
            if (moon)
            {
                // high in the opposite quarter of the sky, so the sides the sun lit at dusk are the dark ones at night
                float me = 34f * Mathf.Deg2Rad; var mz = new Vector2(-hz.y, hz.x);
                moon.transform.rotation = Quaternion.LookRotation(-new Vector3(mz.x * Mathf.Cos(me), Mathf.Sin(me), mz.y * Mathf.Cos(me)));
                moon.color = MoonColor; moon.intensity = moonIntensity * night; moon.enabled = night > 0.02f;
            }
            // sky, fog and fill: one multiplier (linear) on the built dusk colors toward day and night
            var fogLin = Color.Lerp(duskFog.linear, DayFogLin, day);
            var mul = Color.Lerp(Color.white, NightMul, night);
            fogLin = new Color(fogLin.r * mul.r, fogLin.g * mul.g, fogLin.b * mul.b);
            RenderSettings.fogColor = fogLin.gamma;
            RenderSettings.fogStartDistance = Mathf.Lerp(120f, 70f, night);
            var skyMul = new Color(fogLin.r / Mathf.Max(1e-4f, duskFog.linear.r), fogLin.g / Mathf.Max(1e-4f, duskFog.linear.g), fogLin.b / Mathf.Max(1e-4f, duskFog.linear.b));
            if (sky)
            {
                // Skybox/Cubemap: color = tex * Tint (linear) * 4.59 (unity_ColorSpaceDouble) * Exposure
                float ex = Mathf.Max(skyMul.r, skyMul.g, skyMul.b);
                var tintLin = new Color(skyMul.r / ex / 4.5948f, skyMul.g / ex / 4.5948f, skyMul.b / ex / 4.5948f);
                sky.SetColor("_Tint", tintLin.gamma); sky.SetFloat("_Exposure", ex);
            }
            float ambMul = Mathf.Lerp(1f, 0.42f, night);   // v74: a little above v73's 0.34, so the dark reads
            Color Amb(Color dusk, Color nightTint) => Color.Lerp(dusk, new Color(dusk.r * nightTint.r, dusk.g * nightTint.g, dusk.b * nightTint.b), night) * ambMul;
            // near neutral, a touch cool from the sky, a touch warm from the lit streets: no single brown or purple wash
            RenderSettings.ambientSkyColor = Amb(duskAmbSky, new Color(0.66f, 0.70f, 0.80f));
            RenderSettings.ambientEquatorColor = Amb(duskAmbEq, new Color(0.66f, 0.68f, 0.72f));
            RenderSettings.ambientGroundColor = Amb(duskAmbGround, new Color(0.80f, 0.72f, 0.62f));
            RenderSettings.reflectionIntensity = reflection * Mathf.Lerp(1f, 0.35f, night);
            // window lights by the clock (only when it changes enough to matter)
            float glow = WindowRamp(h);
            if (fill) fill.intensity = fillIntensity * Mathf.Lerp(Mathf.Lerp(1f, 0.6f, day), 0.55f, night);
            if (Mathf.Abs(glow - lastNight) > 0.01f || (glow == 0f) != (lastNight == 0f))
            {
                lastNight = glow;
                var em = WindowLight * Mathf.Pow(2f, WindowHdrIntensity) * glow;
                foreach (var m in windowMaterials) if (m) m.SetColor("_EmissionColor", em);
            }
        }
    }
}
