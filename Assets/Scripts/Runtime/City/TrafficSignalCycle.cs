// Runs the Eastside traffic lights: the signals over the north-south streets (phase A) and the east-west
// streets (phase B) take turns, green then yellow then red. The lenses light through a MaterialPropertyBlock
// on their own renderers, so the shared lens materials (and the SRP batcher for everything else) stay as built.
using UnityEngine;

namespace StreetEmpire.City
{
    public class TrafficSignalCycle : MonoBehaviour
    {
        public Renderer[] redA, yellowA, greenA, redB, yellowB, greenB;
        public float green = 14f, yellow = 3f, allRed = 1f;
        public Color redOn = new(4f, 0.25f, 0.15f), yellowOn = new(4f, 2.6f, 0.2f), greenOn = new(0.3f, 4f, 1.4f);
        static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock mpb; int shown = -1;

        void Awake() { mpb = new MaterialPropertyBlock(); Show(Phase(0)); }

        void Update() { Show(Phase(Time.time)); }

        /// <summary>0 A green, 1 A yellow, 2 all red, 3 B green, 4 B yellow, 5 all red.</summary>
        public int Phase(float t)
        {
            float cycle = 2 * (green + yellow + allRed), x = t % cycle;
            float half = green + yellow + allRed; int off = x >= half ? 3 : 0; if (off > 0) x -= half;
            return off + (x < green ? 0 : x < green + yellow ? 1 : 2);
        }

        void Show(int phase)
        {
            if (phase == shown) return; shown = phase;
            Light(greenA, phase == 0 ? greenOn : Color.black); Light(yellowA, phase == 1 ? yellowOn : Color.black); Light(redA, phase >= 2 ? redOn : Color.black);
            Light(greenB, phase == 3 ? greenOn : Color.black); Light(yellowB, phase == 4 ? yellowOn : Color.black); Light(redB, phase < 3 || phase == 5 ? redOn : Color.black);
        }

        void Light(Renderer[] rs, Color c)
        {
            if (rs == null) return;
            foreach (var r in rs) { if (r == null) continue; r.GetPropertyBlock(mpb); mpb.SetColor(Emission, c); r.SetPropertyBlock(mpb); }
        }

        /// <summary>Which phase the tests and the shot director see: is the north-south green lit.</summary>
        public bool AGreen => shown == 0;
    }
}
