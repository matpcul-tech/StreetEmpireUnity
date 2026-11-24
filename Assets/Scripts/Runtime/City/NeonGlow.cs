// The neon signs' light by the clock: tubes barely lit by day, full from dusk (the street lamp level), bright
// enough to bloom. Every fifth sign is on the second material, which stutters now and then like a tired tube.
using UnityEngine;

namespace StreetEmpire.City
{
    public class NeonGlow : MonoBehaviour
    {
        public DayCycle cycle;
        public Material steady, flicker;
        public const float DayLevel = 0.12f, NightLevel = 2.1f;
        /// <summary>The steady signs' brightness right now (DayLevel to NightLevel).</summary>
        public float Level { get; private set; } = DayLevel;
        static readonly int Base = Shader.PropertyToID("_BaseColor");
        float last = -1f;

        // the materials are shared assets: back to the day level (play mode in the editor and the tests)
        void OnDisable()
        {
            if (steady) steady.SetColor(Base, Color.white * DayLevel);
            if (flicker) flicker.SetColor(Base, Color.white * DayLevel);
            last = -1f;
        }

        void LateUpdate()
        {
            float on = cycle != null ? Mathf.Clamp01(cycle.Lamps) : 0f;
            Level = Mathf.Lerp(DayLevel, NightLevel, on);
            if (Mathf.Abs(Level - last) > 0.01f) { last = Level; if (steady) steady.SetColor(Base, Color.white * Level); }
            if (flicker)
            {
                float t = Time.time; bool dip = on > 0.5f && Mathf.PerlinNoise(t * 7f, 3.3f) > 0.72f && Mathf.PerlinNoise(t * 0.35f, 9.1f) > 0.55f;
                flicker.SetColor(Base, Color.white * (dip ? Level * 0.25f : Level));
            }
        }
    }
}
