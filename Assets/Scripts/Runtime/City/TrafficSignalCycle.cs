// Runs the Eastside traffic lights: the signals over the north-south avenues (phase A) and the east-west
// streets (phase B) take turns, green then yellow then red. The signals are drawn by PropInstancer, each
// group with its own lens materials; on start this makes run-time copies of those six materials, hands them
// to the instancer and lights the copies, so the saved materials never change.
using UnityEngine;

namespace StreetEmpire.City
{
    public class TrafficSignalCycle : MonoBehaviour
    {
        public PropInstancer instancer;
        /// <summary>Lens materials: red, yellow, green for phase A, then the same for phase B.</summary>
        public Material[] lenses = new Material[6];
        public float green = 14f, yellow = 3f, allRed = 1f;
        public Color redOn = new(4f, 0.25f, 0.15f), yellowOn = new(4f, 2.6f, 0.2f), greenOn = new(0.35f, 3.4f, 0.5f);   // v75c: a true signal green (the old 1.4 blue read cyan at night under ACES)
        static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        Material[] live; int shown = -1;

        void Awake()
        {
            live = new Material[lenses.Length];
            for (int i = 0; i < lenses.Length; i++)
            {
                if (lenses[i] == null) continue;
                live[i] = new Material(lenses[i]) { name = lenses[i].name + " (live)" };
                if (instancer) instancer.Replace(lenses[i], live[i]);
            }
            Show(Phase(0));
        }

        void Update() { Show(Phase(Time.time)); }

        /// <summary>0 A green, 1 A yellow, 2 all red, 3 B green, 4 B yellow, 5 all red.</summary>
        public int Phase(float t)
        {
            float half = green + yellow + allRed, x = t % (2 * half);
            int off = x >= half ? 3 : 0; if (off > 0) x -= half;
            return off + (x < green ? 0 : x < green + yellow ? 1 : 2);
        }

        public void Show(int phase)
        {
            if (phase == shown || live == null) return; shown = phase;
            Set(2, phase == 0 ? greenOn : Color.black); Set(1, phase == 1 ? yellowOn : Color.black); Set(0, phase >= 2 ? redOn : Color.black);
            Set(5, phase == 3 ? greenOn : Color.black); Set(4, phase == 4 ? yellowOn : Color.black); Set(3, phase < 3 || phase == 5 ? redOn : Color.black);
        }

        void Set(int i, Color c) { if (i < live.Length && live[i] != null) live[i].SetColor(Emission, c); }

        public bool AGreen => shown == 0;
        public Material Live(int i) => live != null && i < live.Length ? live[i] : null;
    }
}
