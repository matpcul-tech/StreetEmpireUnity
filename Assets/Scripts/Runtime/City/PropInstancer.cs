// Draws all the Eastside street props with GPU instancing: one draw per model, detail level and material
// (Graphics.RenderMeshInstanced) instead of one per prop part. Each frame it keeps the copies that are in front
// of the camera (or close enough that their shadow can fall into view) and inside the model's draw distance,
// and picks the lower detail mesh past the model's switch distance. Copies past the model's shadow distance
// draw without a shadow (props have a shorter shadow reach than the buildings). The colliders and PropMarkers stay on
// plain GameObjects; only the drawing moved here.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace StreetEmpire.City
{
    public class PropInstancer : MonoBehaviour
    {
        public PropBatch[] batches = new PropBatch[0];
        /// <summary>Copies this close always draw, on screen or not, so the shadows next to the camera stay.</summary>
        public float shadowKeep = 24f;
        public float distanceScale = 1f;
        const int Chunk = 500;

        readonly Plane[] planes = new Plane[6];
        readonly List<Matrix4x4>[] lists = { new(), new(), new(), new() };
        readonly Bounds[] bounds = new Bounds[4];
        readonly bool[] has = new bool[4];
        public int LastInstances { get; private set; }
        public int LastDraws { get; private set; }
        public long LastTriangles { get; private set; }

        void LateUpdate()
        {
            var cam = Camera.main; if (cam == null || !SystemInfo.supportsInstancing) return;
            GeometryUtility.CalculateFrustumPlanes(cam, planes);
            var eye = cam.transform.position;
            int shown = 0, draws = 0; long tris = 0;
            foreach (var b in batches)
            {
                if (b == null || b.lod0 == null || b.matrices == null) continue;
                for (int k = 0; k < 4; k++) { lists[k].Clear(); has[k] = false; }
                float cull = b.cullDistance * distanceScale, sw = b.lod1Distance * distanceScale;
                float sh = b.castShadows ? b.shadowDistance * distanceScale : -1f;
                foreach (var m in b.matrices)
                {
                    var c = m.MultiplyPoint3x4(b.center);
                    float d = Vector3.Distance(eye, c);
                    if (d > cull) continue;
                    var s = m.lossyScale; float r = b.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                    var box = new Bounds(c, Vector3.one * (2 * r));
                    if ((d > shadowKeep + r || d > sh) && !GeometryUtility.TestPlanesAABB(planes, box)) continue;
                    // bucket: detail level (0 full, 1 LOD1) times two, plus one when past its shadow distance
                    int k = (d > sw && b.lod1 != null ? 2 : 0) + (d > sh ? 1 : 0);
                    lists[k].Add(m); if (has[k]) bounds[k].Encapsulate(box); else { bounds[k] = box; has[k] = true; }
                }
                for (int k = 0; k < 4; k++)
                {
                    if (!has[k]) continue;
                    var mesh = k < 2 ? b.lod0 : b.lod1;
                    shown += lists[k].Count; tris += lists[k].Count * Tris(mesh);
                    draws += Draw(b, mesh, lists[k], bounds[k], (k & 1) == 0 && b.castShadows);
                }
            }
            LastInstances = shown; LastDraws = draws; LastTriangles = tris;
        }

        static long Tris(Mesh m)
        {
            if (m == null) return 0;
            long t = 0; for (int i = 0; i < m.subMeshCount; i++) t += m.GetIndexCount(i) / 3;
            return t;
        }

        int Draw(PropBatch b, Mesh mesh, List<Matrix4x4> list, Bounds bounds, bool shadows)
        {
            int n = 0;
            int subs = Mathf.Min(mesh.subMeshCount, b.materials.Length);
            for (int s = 0; s < subs; s++)
            {
                var mat = b.materials[s]; if (mat == null || mesh.GetSubMesh(s).indexCount == 0) continue;
                var rp = new RenderParams(mat)
                {
                    shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                    receiveShadows = true, worldBounds = bounds, layer = gameObject.layer,
                };
                for (int start = 0; start < list.Count; start += Chunk)
                {
                    Graphics.RenderMeshInstanced(rp, mesh, s, list, instanceCount: Mathf.Min(Chunk, list.Count - start), startInstance: start);
                    n++;
                }
            }
            return n;
        }

        /// <summary>Swap a material in every batch (the traffic lights light up their own copies).</summary>
        public void Replace(Material from, Material to)
        {
            foreach (var b in batches)
                if (b?.materials != null)
                    for (int i = 0; i < b.materials.Length; i++) if (b.materials[i] == from) b.materials[i] = to;
        }
    }
}
