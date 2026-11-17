// Merges the meshes under a model into one mesh with one submesh per material, so a prop made of twenty
// parts draws as a handful of instanced draws. Optionally mirrors it on X (the traffic signal), flipping
// the winding so its back faces still cull. Saves the result as a compressed mesh asset.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace StreetEmpire.EditorTools
{
    public static class MeshMerge
    {
        /// <summary>The model's renderers, relative to the parent of <paramref name="root"/>, as one mesh. Materials go
        /// through <paramref name="map"/>; <paramref name="order"/> fixes the submesh order (pass an empty list
        /// to have it filled from this model). Materials not in a fixed order land on the first submesh.</summary>
        public static Mesh Merge(GameObject root, Func<Material, Material> map, List<Material> order, bool mirror, string assetPath)
        {
            // the model as it stands unparented, so the imported root's own offset, turn and scale stay in the mesh
            var toRoot = root.transform.parent ? root.transform.parent.worldToLocalMatrix : Matrix4x4.identity;
            return Merge(root.GetComponentsInChildren<MeshRenderer>(), toRoot, map, order, mirror, assetPath);
        }

        /// <summary>Any set of renderers as one mesh, in the space <paramref name="toRoot"/> maps world space to.</summary>
        public static Mesh Merge(IEnumerable<MeshRenderer> renderers, Matrix4x4 toRoot, Func<Material, Material> map, List<Material> order, bool mirror, string assetPath, bool compress = true)
        {
            bool grow = order.Count == 0, noTangents = false;
            var pos = new List<Vector3>(); var nrm = new List<Vector3>(); var tan = new List<Vector4>(); var uv = new List<Vector2>();
            var subs = new List<List<int>>();
            for (int i = 0; i < order.Count; i++) subs.Add(new List<int>());
            foreach (var r in renderers)
            {
                var mf = r.GetComponent<MeshFilter>(); var mesh = mf ? mf.sharedMesh : null;
                if (mesh == null || !r.enabled) continue;
                var m = toRoot * r.transform.localToWorldMatrix;
                bool flip = (m.determinant < 0) ^ mirror;
                int b = pos.Count;
                var v = mesh.vertices; var n = mesh.normals; var t = mesh.tangents; var u = mesh.uv;
                if (t.Length != v.Length) noTangents = true;
                for (int i = 0; i < v.Length; i++)
                {
                    var p = m.MultiplyPoint3x4(v[i]);
                    var nn = n.Length == v.Length ? m.MultiplyVector(n[i]).normalized : Vector3.up;
                    var tt = t.Length == v.Length ? t[i] : new Vector4(1, 0, 0, 1);
                    var t3 = m.MultiplyVector(new Vector3(tt.x, tt.y, tt.z)).normalized;
                    float w = tt.w * (m.determinant < 0 ? -1 : 1);
                    if (mirror) { p.x = -p.x; nn.x = -nn.x; t3.x = -t3.x; w = -w; }
                    pos.Add(p); nrm.Add(nn); tan.Add(new Vector4(t3.x, t3.y, t3.z, w)); uv.Add(u.Length == v.Length ? u[i] : Vector2.zero);
                }
                var src = r.sharedMaterials;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    if (mesh.GetTopology(s) != MeshTopology.Triangles) continue;
                    var mat = map(s < src.Length ? src[s] : null);
                    int slot = order.IndexOf(mat);
                    if (slot < 0) { if (grow) { order.Add(mat); subs.Add(new List<int>()); slot = order.Count - 1; } else slot = 0; }
                    var tri = mesh.GetTriangles(s); var list = subs[slot];
                    for (int i = 0; i < tri.Length; i += 3)
                    {
                        list.Add(b + tri[i]);
                        if (flip) { list.Add(b + tri[i + 2]); list.Add(b + tri[i + 1]); }
                        else { list.Add(b + tri[i + 1]); list.Add(b + tri[i + 2]); }
                    }
                }
            }
            var o = new Mesh { name = Path.GetFileNameWithoutExtension(assetPath) };
            o.indexFormat = pos.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            o.SetVertices(pos); o.SetNormals(nrm); o.SetTangents(tan); o.SetUVs(0, uv);
            o.subMeshCount = subs.Count;
            for (int i = 0; i < subs.Count; i++) o.SetTriangles(subs[i], i, false);
            o.RecalculateBounds();
            if (noTangents) o.RecalculateTangents();   // normal mapped props need them
            MeshUtility.Optimize(o);
            return Save(o, assetPath, compress: compress);
        }

        /// <summary>Saves a mesh (or a copy of an imported one) as a compressed asset, replacing any old one.</summary>
        public static Mesh Save(Mesh mesh, string assetPath, bool copy = false, bool compress = true)
        {
            if (copy) { var name = Path.GetFileNameWithoutExtension(assetPath); mesh = Object.Instantiate(mesh); mesh.name = name; }
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            MeshUtility.SetMeshCompression(mesh, compress ? ModelImporterMeshCompression.Low : ModelImporterMeshCompression.Off);
            if (AssetDatabase.LoadAssetAtPath<Mesh>(assetPath) != null) AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(mesh, assetPath);
            return mesh;
        }

        public static int Triangles(Mesh m)
        {
            if (m == null) return 0;
            long n = 0; for (int s = 0; s < m.subMeshCount; s++) n += m.GetIndexCount(s);
            return (int)(n / 3);
        }
    }
}
