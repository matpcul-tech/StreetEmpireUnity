// Draws all the Eastside street props with GPU instancing: one draw per model, detail level and material
// (Graphics.RenderMeshInstanced) instead of one per prop part. Each frame it keeps the copies that are in front
// of the camera (or close enough that their shadow can fall into view) and inside the model's draw distance,
// and picks the lower detail mesh past the model's switch distance. The colliders and PropMarkers stay on
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
        readonly List<Matrix4x4> near = new(), far = new();
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
                near.Clear(); far.Clear();
                Bounds nb = default, fb = default; bool hn = false, hf = false;
                float cull = b.cullDistance * distanceScale, sw = b.lod1Distance * distanceScale;
                foreach (var m in b.matrices)
                {
                    var c = m.MultiplyPoint3x4(b.center);
                    float d = Vector3.Distance(eye, c);
                    if (d > cull) continue;
                    var s = m.lossyScale; float r = b.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                    var box = new Bounds(c, Vector3.one * (2 * r));
                    if (d > shadowKeep + r && !GeometryUtility.TestPlanesAABB(planes, box)) continue;
                    if (d > sw && b.lod1 != null) { far.Add(m); if (hf) fb.Encapsulate(box); else { fb = box; hf = true; } }
                    else { near.Add(m); if (hn) nb.Encapsulate(box); else { nb = box; hn = true; } }
                }
                shown += near.Count + far.Count;
                tris += near.Count * Tris(b.lod0) + far.Count * Tris(b.lod1);
                if (hn) draws += Draw(b, b.lod0, near, nb);
                if (hf) draws += Draw(b, b.lod1, far, fb);
            }
            LastInstances = shown; LastDraws = draws; LastTriangles = tris;
        }

        static long Tris(Mesh m)
        {
            if (m == null) return 0;
            long t = 0; for (int i = 0; i < m.subMeshCount; i++) t += m.GetIndexCount(i) / 3;
            return t;
        }

        int Draw(PropBatch b, Mesh mesh, List<Matrix4x4> list, Bounds bounds)
        {
            int n = 0;
            int subs = Mathf.Min(mesh.subMeshCount, b.materials.Length);
            for (int s = 0; s < subs; s++)
            {
                var mat = b.materials[s]; if (mat == null || mesh.GetSubMesh(s).indexCount == 0) continue;
                var rp = new RenderParams(mat)
                {
                    shadowCastingMode = b.castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
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
