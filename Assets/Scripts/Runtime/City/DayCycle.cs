// v75 day curve, keyed on the clock (Matthew's v74 review): the 18:08 fight shot look (the built golden hour) is the
// anchor; 14:00 is plain daytime (high white sun, blue sky, short shadows); the sun sets at about 18:40 and its light
// fades to zero at the horizon, never lighting anything after; 19:00 is the blue hour (deep blue sky, city glow,
// windows and lamps on); 22:00 is full night (dark sky, a dim blue moon, the lamps carry the street). Between keys
// every value is blended. The sky mixes three cubemaps (StreetEmpire/SkyBlend) and the fog color always follows the
// sky's horizon, so the far edge stays seamless. Windows: warm and dim, by the clock (WindowRamp), only the glass
// (the windows_lit parts; about a third of each model's panes were moved to the dark glass when the scene was built).
// The far skyline towers' lit windows (skylineMaterials) come up with the night.
using StreetEmpire.Game;
using UnityEngine;

namespace StreetEmpire.City
{
    public class DayCycle : MonoBehaviour
    {
        public Light sun, fill, moon;
        public Material sky;
        /// <summary>0 by day and at dusk, 1 at full night (LampLights reads it as the lamp level).</summary>
        public float Night { get; private set; }
        /// <summary>Street lamp level, 0 to 1 (LampLights).</summary>
        public float Lamps { get; private set; }
        /// <summary>v75c: the far skyline window glow at full night (was 1.3, brighter than the town windows).</summary>
        public const float SkylineGlow = 0.42f;
        [Header("Built golden hour values (filled by the scene builder)")]
        public Color duskSun = new(1f, 0.77f, 0.58f), duskFog = new(0.68f, 0.61f, 0.54f);
        public Color duskAmbSky = new(0.63f, 0.61f, 0.735f), duskAmbEq = new(0.57f, 0.53f, 0.50f), duskAmbGround = new(0.40f, 0.33f, 0.31f);
        public float duskSunIntensity = 0.95f, reflection = 0.35f, fillIntensity = 0.55f;
        public Vector2 azimuth = new(0.8f, 0.45f);
        public Material[] windowMaterials = new Material[0], skylineMaterials = new Material[0], signMaterials = new Material[0];
        [Header("Horizon fog color of each sky cubemap (sRGB), so the fog matches the blended sky")]
        public Color dayFog = new(0.72f, 0.77f, 0.84f), nightFog = new(0.08f, 0.10f, 0.16f);

        public static readonly Color WindowLight = new(1f, 0.58f, 0.28f);
        public const float WindowIntensity = 1.6f;      // linear multiplier (v74 had x4, which ACES and bloom turned white)
        static readonly Color MoonColor = new(0.55f, 0.64f, 0.86f);
        public const float SunriseHour = 6.5f, SunsetHour = 18.67f;
        float lastGlow = -1, lastSky = -1;

        struct Key
        {
            public float h, sunI, moonI, refl, fill, lamps, skyline, exposure, fogStart, fogEnd; public Color sunC, aSky, aEq, aGround; public Vector3 w;
        }
        Key[] keys;

        void Start()
        {
            // work on copies: the sky and the building materials are assets, and play mode in the editor (the tests)
            // would otherwise write the night values into them
            if (sky) { sky = new Material(sky); RenderSettings.skybox = sky; }
            windowMaterials = Copies(windowMaterials); signMaterials = Copies(signMaterials);
            // the far towers draw through the prop instancer (no renderers to point at copies), so they use the
            // asset materials directly
            keys = BuildKeys();
        }

        Material[] Copies(Material[] src)
        {
            var map = new System.Collections.Generic.Dictionary<Material, Material>();
            foreach (var m in src) if (m && !map.ContainsKey(m)) map[m] = new Material(m);
            if (map.Count > 0)
                foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var ms = r.sharedMaterials; bool hit = false;
                    for (int i = 0; i < ms.Length; i++) if (ms[i] != null && map.TryGetValue(ms[i], out var c)) { ms[i] = c; hit = true; }
                    if (hit) r.sharedMaterials = ms;
                }
            var o = new Material[map.Count]; map.Values.CopyTo(o, 0); return o;
        }

        Key[] BuildKeys()
        {
            Key K(float h, float sunI, Color sunC, Color sky, Color eq, Color gr, Vector3 w, float exp, float moonI, float lamps, float skyline, float refl, float fillI, float fs, float fe)
                => new Key { h = h, sunI = sunI, sunC = sunC, aSky = sky, aEq = eq, aGround = gr, w = w, exposure = exp, moonI = moonI, lamps = lamps, skyline = skyline, refl = refl, fill = fillI, fogStart = fs, fogEnd = fe };
            Color nSky = new(0.13f, 0.15f, 0.22f), nEq = new(0.10f, 0.11f, 0.15f), nGr = new(0.08f, 0.07f, 0.07f);
            Color bSky = new(0.22f, 0.26f, 0.38f), bEq = new(0.17f, 0.18f, 0.25f), bGr = new(0.12f, 0.10f, 0.10f);
            Color dSky = new(0.60f, 0.68f, 0.85f), dEq = new(0.58f, 0.60f, 0.62f), dGr = new(0.38f, 0.35f, 0.31f);
            Color daySun = new(1f, 0.96f, 0.90f), lateSun = new(1f, 0.87f, 0.70f);
            var N = new Vector3(0, 0, 1); var D = new Vector3(1, 0, 0); var G = new Vector3(0, 1, 0);
            return new[]
            {
                K(0f, 0f, daySun, nSky, nEq, nGr, N, 1f, 0.12f, 1f, 1f, 0.15f, 0.30f, 70f, 420f),
                K(4.6f, 0f, daySun, nSky, nEq, nGr, N, 1f, 0.12f, 1f, 1f, 0.15f, 0.30f, 70f, 420f),
                K(5.8f, 0f, daySun, bSky, bEq, bGr, new Vector3(0, 0.1f, 0.9f), 3f, 0.07f, 1f, 0.7f, 0.25f, 0.40f, 90f, 440f),
                K(6.7f, 0.55f, duskSun, Color.Lerp(duskAmbSky, bSky, 0.3f), Color.Lerp(duskAmbEq, bEq, 0.3f), Color.Lerp(duskAmbGround, bGr, 0.3f), new Vector3(0.1f, 0.9f, 0f), 0.9f, 0f, 0f, 0.2f, 0.3f, 0.5f, 110f, 440f),   // v75c: lamps off once the sun is up
                K(8.2f, 1.2f, lateSun, Color.Lerp(duskAmbSky, dSky, 0.6f), Color.Lerp(duskAmbEq, dEq, 0.6f), Color.Lerp(duskAmbGround, dGr, 0.6f), new Vector3(0.65f, 0.35f, 0f), 1f, 0f, 0f, 0f, 0.38f, 0.5f, 130f, 500f),
                K(10f, 1.45f, daySun, dSky, dEq, dGr, D, 1f, 0f, 0f, 0f, 0.4f, 0.5f, 140f, 520f),
                K(15.3f, 1.45f, daySun, dSky, dEq, dGr, D, 1f, 0f, 0f, 0f, 0.4f, 0.5f, 140f, 520f),
                K(16.8f, 1.25f, lateSun, Color.Lerp(duskAmbSky, dSky, 0.55f), Color.Lerp(duskAmbEq, dEq, 0.55f), Color.Lerp(duskAmbGround, dGr, 0.55f), new Vector3(0.6f, 0.4f, 0f), 1f, 0f, 0f, 0f, 0.38f, 0.52f, 130f, 490f),
                // the anchor: the built golden hour, as in the 18:08 fight shot
                K(18.13f, duskSunIntensity, duskSun, duskAmbSky, duskAmbEq, duskAmbGround, G, 1f, 0f, 0f, 0f, reflection, fillIntensity, 120f, 460f),   // v75c: lamps not on before sunset
                K(18.67f, 0f, duskSun, Color.Lerp(duskAmbSky, bSky, 0.5f), Color.Lerp(duskAmbEq, bEq, 0.5f), Color.Lerp(duskAmbGround, bGr, 0.5f), new Vector3(0, 0.6f, 0.4f), 1.2f, 0f, 0.7f, 0.3f, 0.3f, 0.45f, 100f, 420f),
                K(19.1f, 0f, duskSun, bSky, bEq, bGr, new Vector3(0, 0.1f, 0.9f), 3f, 0.07f, 1f, 0.75f, 0.25f, 0.40f, 90f, 440f),
                K(20.6f, 0f, daySun, nSky, nEq, nGr, N, 1f, 0.12f, 1f, 1f, 0.15f, 0.30f, 70f, 420f),
                K(24f, 0f, daySun, nSky, nEq, nGr, N, 1f, 0.12f, 1f, 1f, 0.15f, 0.30f, 70f, 420f),
            };
        }

        /// <summary>Window light by the clock: 0 from 07:00 to 18:00, up from 18:00 to 19:30, 1 until 05:30, down to
        /// 0 at 07:00.</summary>
        public static float WindowRamp(float h)
        {
            h = Mathf.Repeat(h, 24f);
            if (h >= 7f && h < 18f) return 0f;
            if (h >= 18f && h < 19.5f) return (h - 18f) / 1.5f;
            if (h >= 5.5f && h < 7f) return 1f - (h - 5.5f) / 1.5f;
            return 1f;
        }

        /// <summary>Sun elevation in degrees: up at 06:30, highest (58) at midday, down at about 18:40.</summary>
        public static float SunElevation(float h) => 58f * Mathf.Sin(Mathf.PI * (Mathf.Repeat(h, 24f) - SunriseHour) / (SunsetHour - SunriseHour));

        public static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        void Update()
        {
            if (keys == null) keys = BuildKeys();
            float h = Mathf.Repeat(GameClock.Hour, 24f);
            int i = 0; while (i < keys.Length - 2 && keys[i + 1].h <= h) i++;
            var a = keys[i]; var b = keys[i + 1]; float t = Smooth(0f, 1f, (h - a.h) / Mathf.Max(1e-4f, b.h - a.h));
            float L(float x, float y) => Mathf.Lerp(x, y, t);
            var w = Vector3.Lerp(a.w, b.w, t); float exposure = L(a.exposure, b.exposure);
            Night = Mathf.Clamp01(w.z); Lamps = L(a.lamps, b.lamps);
            // the sun: keyed brightness times a fade to zero at the horizon, so nothing is sunlit once it has set
            float elev = SunElevation(h), sunUp = Smooth(-0.5f, 2.5f, elev);
            var hz = azimuth.normalized; float er = Mathf.Max(elev, 4f) * Mathf.Deg2Rad;
            var dir = new Vector3(hz.x * Mathf.Cos(er), Mathf.Sin(er), hz.y * Mathf.Cos(er));
            if (sun)
            {
                sun.transform.rotation = Quaternion.LookRotation(-dir);
                sun.color = Color.Lerp(a.sunC, b.sunC, t);
                sun.intensity = L(a.sunI, b.sunI) * sunUp;
                sun.enabled = sun.intensity > 0.005f;
            }
            if (moon)
            {
                // dim and blue, high in another quarter of the sky; soft and weak shadows
                float me = 38f * Mathf.Deg2Rad; var mz = new Vector2(-hz.y, hz.x);
                moon.transform.rotation = Quaternion.LookRotation(-new Vector3(mz.x * Mathf.Cos(me), Mathf.Sin(me), mz.y * Mathf.Cos(me)));
                moon.color = MoonColor; moon.intensity = L(a.moonI, b.moonI); moon.enabled = moon.intensity > 0.005f && !(sun && sun.enabled);
            }
            // fog: the blended horizon color of the three skies, so it melts into the sky
            var fogLin = (dayFog.linear * w.x + duskFog.linear * w.y + nightFog.linear * w.z) * exposure;
            RenderSettings.fogColor = new Color(fogLin.r, fogLin.g, fogLin.b).gamma;
            RenderSettings.fogStartDistance = L(a.fogStart, b.fogStart); RenderSettings.fogEndDistance = L(a.fogEnd, b.fogEnd);
            if (sky && Mathf.Abs(w.x + w.y * 3f + w.z * 7f + exposure * 11f - lastSky) > 0.002f)
            {
                lastSky = w.x + w.y * 3f + w.z * 7f + exposure * 11f;
                sky.SetVector("_Weights", new Vector4(w.x, w.y, w.z, 0)); sky.SetFloat("_Exposure", exposure);
            }
            RenderSettings.ambientSkyColor = Color.Lerp(a.aSky, b.aSky, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(a.aEq, b.aEq, t);
            RenderSettings.ambientGroundColor = Color.Lerp(a.aGround, b.aGround, t);
            RenderSettings.reflectionIntensity = L(a.refl, b.refl);
            if (fill) fill.intensity = L(a.fill, b.fill);
            float glow = WindowRamp(h), sky2 = L(a.skyline, b.skyline);
            if (Mathf.Abs(glow + sky2 * 2f - lastGlow) > 0.01f || (glow == 0f) != (lastGlow == 0f))
            {
                lastGlow = glow + sky2 * 2f;
                var em = WindowLight * WindowIntensity * glow;
                foreach (var m in windowMaterials) if (m) m.SetColor("_EmissionColor", em);
                // v75c: the far windows stay well under the town's own (Matthew: they outshone it at night)
                foreach (var m in skylineMaterials) if (m) m.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.65f) * SkylineGlow * sky2);
                // the liquor store sign: a little by day, full from the blue hour on
                foreach (var m in signMaterials) if (m) m.SetColor("_EmissionColor", Color.white * Mathf.Lerp(0.35f, 1.4f, Mathf.Max(glow, sky2)));
            }
        }
    }
}
