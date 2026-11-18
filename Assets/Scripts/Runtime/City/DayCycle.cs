// The sun follows the clock the way updateDay() in the web game's main.js does: angle (hour - 6) / 24 of a turn,
// so it sets at 18:00, about 4 s into a new game. The built scene is the golden hour look (dusk); this blends
// from it to a peach day when the sun is high and to a blue purple moonlit night (with a strong sky fill so
// nothing goes black, the window lights up) after it sets. The sky cubemap is dimmed and tinted by the same
// amount as the fog, so its horizon stays the fog color and the district edge stays hidden.
// v73 night: darker ambient instead of the flat purple wash, a dim cool moon, and the street lamps' pools of light
// (the Lamp Pools mesh and LampLights) carry the street. The character fill light follows the same curve.
using StreetEmpire.Game;
using UnityEngine;

namespace StreetEmpire.City
{
    public class DayCycle : MonoBehaviour
    {
        public Light sun, fill;
        public Material sky, lampPools;
        /// <summary>0 by day and at dusk, 1 at full night (LampLights reads it).</summary>
        public float Night { get; private set; }
        [Header("Built golden hour values (filled by the scene builder)")]
        public Color duskSun = new(1f, 0.77f, 0.58f), duskFog = new(0.66f, 0.53f, 0.50f);
        public Color duskAmbSky = new(0.63f, 0.61f, 0.735f), duskAmbEq = new(0.58f, 0.51f, 0.53f), duskAmbGround = new(0.40f, 0.33f, 0.31f);
        public float duskSunIntensity = 0.95f, duskElevation = 7f, reflection = 0.35f, windowGlow = 0.7f, fillIntensity = 0.55f;
        public Vector2 azimuth = new(0.8f, 0.45f);
        public Material[] windowMaterials = new Material[0];

        static readonly Color DaySun = new(1f, 0.94f, 0.86f), NightSun = new(0.62f, 0.68f, 0.86f);
        static readonly Color DayFogLin = new Color(0.71f, 0.54f, 0.40f).linear, NightMul = new(0.09f, 0.095f, 0.13f);
        float lastNight = -1;

        void Start()
        {
            // work on copies: the sky and the building materials are assets, and play mode in the editor (the tests)
            // would otherwise write the night values into them
            if (sky) { sky = new Material(sky); RenderSettings.skybox = sky; }
            if (lampPools)
            {
                var copy = new Material(lampPools);
                foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (r.sharedMaterial == lampPools) r.sharedMaterial = copy;
                lampPools = copy;
            }
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

        public static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        void Update()
        {
            float h = GameClock.Hour;
            float ang = (h - 6f) / 24f * Mathf.PI * 2f, elev = Mathf.Sin(ang);
            float day = Smooth(0.05f, 0.35f, elev);                       // the built look holds through the low sun
            float night = 1f - Smooth(-0.12f, 0.03f, elev); Night = night;
            // the sun keeps the built azimuth; its height follows the clock (never below 9 deg so shadows stay sane);
            // after sunset the same light is the moon, higher up
            float e = Mathf.Lerp(Mathf.Max(duskElevation, Mathf.Asin(Mathf.Clamp(elev, -1f, 1f)) * Mathf.Rad2Deg), 38f, night);
            var hz = azimuth.normalized; float er = e * Mathf.Deg2Rad;
            var dir = new Vector3(hz.x * Mathf.Cos(er), Mathf.Sin(er), hz.y * Mathf.Cos(er));
            if (sun)
            {
                sun.transform.rotation = Quaternion.LookRotation(-dir);
                var c = Color.Lerp(duskSun, DaySun, day); c = Color.Lerp(c, NightSun, night);
                sun.color = c; sun.intensity = Mathf.Lerp(Mathf.Lerp(duskSunIntensity, 1.4f, day), 0.2f, night);
            }
            // sky, fog and fill: one multiplier (linear) on the built dusk colors toward day and night
            var fogLin = Color.Lerp(duskFog.linear, DayFogLin, day);
            var mul = Color.Lerp(Color.white, NightMul, night);
            fogLin = new Color(fogLin.r * mul.r, fogLin.g * mul.g, fogLin.b * mul.b);
            RenderSettings.fogColor = fogLin.gamma;
            RenderSettings.fogStartDistance = Mathf.Lerp(90f, 55f, night);
            var skyMul = new Color(fogLin.r / Mathf.Max(1e-4f, duskFog.linear.r), fogLin.g / Mathf.Max(1e-4f, duskFog.linear.g), fogLin.b / Mathf.Max(1e-4f, duskFog.linear.b));
            if (sky)
            {
                // Skybox/Cubemap: color = tex * Tint (linear) * 4.59 (unity_ColorSpaceDouble) * Exposure
                float ex = Mathf.Max(skyMul.r, skyMul.g, skyMul.b);
                var tintLin = new Color(skyMul.r / ex / 4.5948f, skyMul.g / ex / 4.5948f, skyMul.b / ex / 4.5948f);
                sky.SetColor("_Tint", tintLin.gamma); sky.SetFloat("_Exposure", ex);
            }
            float ambMul = Mathf.Lerp(1f, 0.34f, night);   // v73: darker nights (was 0.62, a flat purple wash)
            Color Amb(Color dusk, Color nightTint) => Color.Lerp(dusk, new Color(dusk.r * nightTint.r, dusk.g * nightTint.g, dusk.b * nightTint.b), night) * ambMul;
            RenderSettings.ambientSkyColor = Amb(duskAmbSky, new Color(0.6f, 0.64f, 0.88f));
            RenderSettings.ambientEquatorColor = Amb(duskAmbEq, new Color(0.55f, 0.56f, 0.72f));
            RenderSettings.ambientGroundColor = Amb(duskAmbGround, new Color(0.9f, 0.7f, 0.55f));   // warm bounce off lit streets
            RenderSettings.reflectionIntensity = reflection * Mathf.Lerp(1f, 0.35f, night);
            // window lights: dim at dusk, full at night, faint by day (only when it changes enough to matter)
            float glow = Mathf.Lerp(Mathf.Lerp(1f, 0.2f, day), 1.8f, night);
            if (fill) fill.intensity = fillIntensity * Mathf.Lerp(Mathf.Lerp(1f, 0.6f, day), 0.55f, night);
            // lamp pools: off while the sun is up (at 0.18 they read as odd bright discs on a sunlit road), full at night
            if (lampPools) lampPools.SetColor("_BaseColor", new Color(1f, 1f, 1f, Mathf.Lerp(Mathf.Lerp(0.03f, 0f, day), 1f, night)));
            if (Mathf.Abs(glow - lastNight) > 0.03f)
            {
                lastNight = glow;
                foreach (var m in windowMaterials) if (m) m.SetColor("_EmissionColor", Color.white * windowGlow * glow);
            }
        }
    }
}
