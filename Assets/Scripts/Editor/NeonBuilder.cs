// Neon for the town's night (part of SceneBuilder): tube signs from one atlas (tools/neon_signs.py, invented names)
// hung on the towers, the clubs, the pool hall, the car wash and the stores and apartments past the Eastside, with
// a few small ones on Eastside stores. Each is one additive quad (no mesh edit, like the lettered boards), dim by
// day and full from dusk (NeonGlow), and throws a spill of its own color on the sidewalk through the storefront
// lights (DayCycle lights the nearest few).
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace StreetEmpire.EditorTools
{
    public static partial class SceneBuilder
    {
        public const string NeonAtlas = "Assets/Art/Textures/Neon/neon-atlas.png";
        // the atlas cells, in tools/neon_signs.py order
        enum Wide { CalderGrand, Lucky9, Nightowl, PoolHall, Open24, Cocktails, Arcade, Diner, HarborRoom, Tattoo, CarWash, Eats }
        enum Tall { Hotel, Bar, Jazz, Rooms, Club, Loans, Pawn, Eats }
        static readonly Color NPink = new(1f, 0.16f, 0.59f), NCyan = new(0.16f, 0.86f, 1f), NAmber = new(1f, 0.67f, 0.12f), NRed = new(1f, 0.16f, 0.16f),
            NGreen = new(0.24f, 1f, 0.43f), NViolet = new(0.67f, 0.35f, 1f), NBlue = new(0.24f, 0.43f, 1f);
        static readonly Color[] WideColor = { NAmber, NPink, NCyan, NGreen, NRed, NPink, NViolet, NRed, NBlue, NRed, NCyan, NAmber };
        static readonly Color[] TallColor = { NRed, NPink, NBlue, NAmber, NViolet, NGreen, NAmber, NCyan };
        static readonly Wide[] StoreRoof = { Wide.Open24, Wide.Eats, Wide.Diner, Wide.Arcade, Wide.Tattoo, Wide.Cocktails };
        static readonly Tall[] AptBlade = { Tall.Rooms, Tall.Hotel, Tall.Bar, Tall.Jazz };
        static int neonCount, neonStores, neonApts;
        static Material neonSteady, neonFlicker;
        static readonly Dictionary<int, Mesh> neonMeshes = new();

        public static Material NeonSteady => neonSteady;
        public static Material NeonFlicker => neonFlicker;

        static void NeonReset() { neonCount = neonStores = neonApts = 0; neonMeshes.Clear(); neonSteady = NeonMat("NeonSteady"); neonFlicker = NeonMat("NeonFlicker"); }

        static Material NeonMat(string name)
        {
            string path = $"Assets/Art/Materials/{name}.mat";
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(NeonAtlas);
            if (tex == null) Debug.LogError("[SceneBuilder] missing the neon atlas");
            m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", Color.white * StreetEmpire.City.NeonGlow.DayLevel);
            EditorUtility.SetDirty(m); return m;
        }

        /// <summary>The neon a building takes, if any. Same frame as HangBoard: the model centered on its lot, feet
        /// on the ground, front toward +Z.</summary>
        static void Neon(GameObject inst, GameObject holder, string name, string kind, bool mid, bool landmark)
        {
            if (neonSteady == null) NeonReset();
            var b = WorldBounds(inst);
            float edge = b.extents.x * 0.72f;
            if (name.StartsWith("tower-"))
            {
                var (w, t) = name == "tower-terracotta" ? (Wide.CalderGrand, Tall.Hotel) : name == "tower-deco-setback" ? (Wide.Lucky9, Tall.Club)
                    : name == "tower-copper-mansard" ? (Wide.HarborRoom, Tall.Jazz) : (Wide.Nightowl, Tall.Bar);
                float width = Mathf.Min(b.size.x * 0.6f, 8f);   // 11 m wide was a wall of light on the phone
                NeonFace(inst, holder, (int)w, false, 0f, 5.6f, width, width / 4f, true);
                // the blade low and in from the corner: the towers step back above their base
                NeonFace(inst, holder, (int)t, true, b.extents.x * 0.5f, 10.5f, 2.6f, 5.2f, false);
            }
            else if (name == "job-pool-hall") NeonFace(inst, holder, (int)Wide.PoolHall, false, 0f, Mathf.Min(b.size.y - 0.9f, 4.4f), Mathf.Min(b.size.x * 0.6f, 7f), Mathf.Min(b.size.x * 0.6f, 7f) / 4f, true);
            else if (name == "job-car-wash") NeonRoof(inst, holder, (int)Wide.CarWash, Mathf.Min(b.size.x * 0.6f, 7f));
            else if (name.StartsWith("crew-")) NeonFace(inst, holder, (int)(name.Contains("goldline") ? Tall.Bar : Tall.Club), true, edge, Mathf.Min(b.size.y * 0.6f, 5.5f), 1.7f, 3.4f, true);
            else if (landmark) return;   // the police station, bank, school, clinic, docks and warehouses stay plain
            else if (mid && kind == "apt") { var t = AptBlade[neonApts++ % AptBlade.Length]; float h = Mathf.Clamp(b.size.y * 0.42f, 2.6f, 4.6f); NeonFace(inst, holder, (int)t, true, edge, Mathf.Min(b.size.y * 0.62f, 6.2f), h / 2f, h, true); }
            else if (mid && kind == "store") NeonRoof(inst, holder, (int)StoreRoof[neonStores++ % StoreRoof.Length], Mathf.Min(b.size.x * 0.66f, 6.5f));
            // no blades on the Eastside stores: on the phone the pawn one sat across the lettered board
        }

        /// <summary>A sign flat on the front, on whatever part of the wall is nearest the street in its band.</summary>
        static void NeonFace(GameObject inst, GameObject holder, int cell, bool tall, float x, float y, float w, float h, bool light)
        {
            float z = BandFrontZ(inst, x, y, w, h);
            if (float.IsNegativeInfinity(z)) z = WorldBounds(inst).max.z;
            NeonQuad(holder, cell, tall, new Vector3(x, y, z + 0.14f), w, h, light);
        }

        /// <summary>A wide sign standing on the roof's street edge.</summary>
        static void NeonRoof(GameObject inst, GameObject holder, int cell, float w)
        {
            var b = WorldBounds(inst); float h = w / 4f;
            NeonQuad(holder, cell, false, new Vector3(0f, b.max.y + h * 0.5f + 0.1f, b.max.z - 0.3f), w, h, true);
        }

        static void NeonQuad(GameObject holder, int cell, bool tall, Vector3 at, float w, float h, bool light)
        {
            var go = new GameObject($"Neon {(tall ? ((Tall)cell).ToString() : ((Wide)cell).ToString())}");
            go.transform.SetParent(holder.transform, false); go.transform.position = at; go.transform.localScale = new Vector3(w, h, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = NeonMesh(cell, tall);
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = neonCount % 5 == 3 ? neonFlicker : neonSteady;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            neonCount++;
            if (!light) return;
            var l = new GameObject("Storefront Light").AddComponent<Light>();
            l.transform.SetParent(holder.transform, false);
            l.transform.position = new Vector3(at.x, Mathf.Min(at.y, 3.2f), at.z + 1.6f);
            l.type = LightType.Point; l.color = tall ? TallColor[cell] : WideColor[cell]; l.range = 9f; l.intensity = 0f;
            l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForcePixel; l.enabled = false;
        }

        /// <summary>A unit quad with one atlas cell on it (wide: 1024 x 256, two columns, six rows from the top;
        /// tall: 256 x 512, eight along the bottom).</summary>
        static Mesh NeonMesh(int cell, bool tall)
        {
            int key = tall ? 100 + cell : cell;
            if (neonMeshes.TryGetValue(key, out var have) && have != null) return have;
            float u0, u1, v0, v1;
            if (tall) { u0 = cell * 0.125f; u1 = u0 + 0.125f; v0 = 0f; v1 = 0.25f; }
            else { u0 = (cell % 2) * 0.5f; u1 = u0 + 0.5f; v1 = 1f - (cell / 2) * 0.125f; v0 = v1 - 0.125f; }
            var m = new Mesh { name = $"Neon cell {key}" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            // read from the street (looking back along -Z), the quad's +X is on the left, so U runs the other way
            m.uv = new[] { new Vector2(u1, v0), new Vector2(u0, v0), new Vector2(u0, v1), new Vector2(u1, v1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 }; m.RecalculateBounds();
            m = MeshMerge.Save(m, $"Assets/Art/Meshes/Neon/Neon-{key}.asset", compress: false);
            neonMeshes[key] = m; return m;
        }
    }
}
