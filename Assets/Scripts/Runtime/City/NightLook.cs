// Night look: what changes on screen once the sun is down, on top of DayCycle's lights. A second global post
// volume (GraphicsSetup.NightProfile: more bloom, deeper color, cool shadows) fades in with DayCycle.Night, and
// the road takes a sheen, so the lamp pools, storefront spills and headlights streak on the asphalt.
using UnityEngine;
using UnityEngine.Rendering;

namespace StreetEmpire.City
{
    public class NightLook : MonoBehaviour
    {
        public DayCycle cycle;
        public Volume night;
        /// <summary>The asphalt tile material (an asset): its smoothness goes up at night and back by day.</summary>
        public Material road;
        public const float NightSheen = 0.42f;
        /// <summary>Block pass: the gutter grime and puddle material (an asset, PropBuilder.WetMaterial). Its
        /// smoothness map is scaled by this: half dry by day, so a puddle is a dark damp patch, and full at night,
        /// when the lamps and headlights glint in it.</summary>
        public Material wet;
        public const float DayWet = 0.5f, NightWet = 1f;
        /// <summary>0 by day, 1 at full night.</summary>
        public float Level { get; private set; }
        /// <summary>The night volume's weight and whether it has a profile (the tests read these).</summary>
        public float Weight => night != null ? night.weight : 0f;
        public bool Ready => night != null && night.sharedProfile != null;
        float last = -1f, daySheen = -1f;

        // the road material is a shared asset: put its day value back (play mode in the editor and the tests)
        void OnDisable()
        {
            if (road != null && daySheen >= 0f) road.SetFloat("_Smoothness", daySheen);
            if (wet != null) wet.SetFloat("_Smoothness", DayWet);
            daySheen = -1f; last = -1f;
        }

        void LateUpdate()
        {
            Level = cycle != null ? Mathf.Clamp01(cycle.Night) : 0f;
            if (Mathf.Abs(Level - last) < 0.004f) return;
            last = Level;
            if (night != null) { night.weight = Level; night.enabled = Level > 0.001f; }
            if (road != null)
            {
                if (daySheen < 0f) daySheen = road.GetFloat("_Smoothness");
                road.SetFloat("_Smoothness", Mathf.Lerp(daySheen, NightSheen, Level));
            }
            if (wet != null) wet.SetFloat("_Smoothness", Mathf.Lerp(DayWet, NightWet, Level));
        }
    }
}
