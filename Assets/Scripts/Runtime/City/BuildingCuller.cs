// The 25 block town has a hundred buildings at about 58k triangles each and no far versions yet. A building
// farther than hideAt from the camera is not drawn (its renderers go off; its collider stays), and it comes back
// inside showAt. The gap between the two keeps one at the edge from flickering. Checked a few buildings a frame.
// Until the buildings have far versions this is what keeps a phone at speed; they do pop in down a long street.
using UnityEngine;

namespace StreetEmpire.City
{
    public class BuildingCuller : MonoBehaviour
    {
        public float showAt = 200f, hideAt = 215f;
        public int perFrame = 12;
        /// <summary>Screenshots and tests that want the whole town drawn turn it off.</summary>
        public static bool On = true;

        BuildingMarker[] buildings; Renderer[][] parts; bool[] shown; Vector3[] at; int next;
        public int Shown { get { int n = 0; if (shown != null) foreach (var s in shown) if (s) n++; return n; } }
        public int Total => buildings != null ? buildings.Length : 0;

        void Start()
        {
            buildings = FindObjectsByType<BuildingMarker>(FindObjectsSortMode.None);
            parts = new Renderer[buildings.Length][]; shown = new bool[buildings.Length]; at = new Vector3[buildings.Length];
            for (int i = 0; i < buildings.Length; i++)
            {
                parts[i] = buildings[i].GetComponentsInChildren<Renderer>(true);
                var c = buildings[i].GetComponent<Collider>();
                at[i] = c != null ? c.bounds.center : buildings[i].transform.position;
                shown[i] = true;
            }
        }

        void Set(int i, bool on)
        {
            if (shown[i] == on) return;
            shown[i] = on;
            foreach (var r in parts[i]) if (r != null) r.enabled = on;
        }

        void LateUpdate()
        {
            if (buildings == null || buildings.Length == 0) return;
            var cam = Camera.main;
            if (!On || cam == null) { for (int i = 0; i < buildings.Length; i++) Set(i, true); return; }
            var p = cam.transform.position;
            int n = Mathf.Min(perFrame, buildings.Length);
            for (int k = 0; k < n; k++)
            {
                int i = next; next = (next + 1) % buildings.Length;
                float dx = at[i].x - p.x, dz = at[i].z - p.z; float d2 = dx * dx + dz * dz;
                if (shown[i]) { if (d2 > hideAt * hideAt) Set(i, false); }
                else if (d2 < showAt * showAt) Set(i, true);
            }
        }
    }
}
