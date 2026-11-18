// Adds the building's LODGroup when the scene loads instead of saving one in the scene file. The v42 boot
// log showed the phone failing to read the Eastside scene (level1 "corrupted, position out of bounds"), and
// LODGroup is the only engine component in that file that the M5 scene (which loaded) did not have.
using UnityEngine;

namespace StreetEmpire.City
{
    public class RuntimeLod : MonoBehaviour
    {
        public Renderer[] near;
        public Renderer[] far;
        public float nearHeight = 0.1f, farHeight = 0.005f;
        /// <summary>Optional third level (the lowest detail building mesh) and where it stops drawing.</summary>
        public Renderer[] low;
        public float lowHeight = 0.004f;

        void Awake()
        {
            if (GetComponent<LODGroup>() != null) return;
            var g = gameObject.AddComponent<LODGroup>(); g.fadeMode = LODFadeMode.None; g.animateCrossFading = false;
            if (low != null && low.Length > 0) g.SetLODs(new[] { new LOD(nearHeight, near), new LOD(farHeight, far), new LOD(lowHeight, low) });
            else g.SetLODs(new[] { new LOD(nearHeight, near), new LOD(farHeight, far) });
            g.RecalculateBounds();
        }
    }
}
