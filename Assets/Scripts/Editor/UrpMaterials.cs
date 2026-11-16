// glTFast picks its shader when a .glb is imported, and caches the render pipeline it detected. On a
// fresh CI checkout the models import before URP is assigned, so they come in on the Built-in glTF shader,
// which URP players skip (the buildings were invisible in the APK and WebGL). Instead of depending on
// import order, the scene builders swap each kit material for a URP Lit copy saved under Assets/Art/Materials/Kit.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StreetEmpire.EditorTools
{
    public static class UrpMaterials
    {
        const string Dir = "Assets/Art/Materials/Kit";
        static readonly Dictionary<Material, Material> cache = new();

        public static bool IsUrp(Shader sh) => sh != null && (sh.name.StartsWith("Universal Render Pipeline/") || sh.name.StartsWith("Shader Graphs/"));

        public static void Apply(GameObject root, string modelName)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>())
                r.sharedMaterials = r.sharedMaterials.Select(m => Convert(m, modelName)).ToArray();
        }

        static Material Convert(Material src, string modelName)
        {
            if (src == null || IsUrp(src.shader)) return src;
            if (cache.TryGetValue(src, out var done) && done != null) return done;
            Directory.CreateDirectory(Dir);
            string path = $"{Dir}/{Safe(modelName)}_{Safe(src.name)}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            else m.shader = Shader.Find("Universal Render Pipeline/Lit");

            var tex = src.GetTexturePropertyNames();
            Texture T(string key) => tex.Where(n => n.ToLowerInvariant().Contains(key)).Select(src.GetTexture).FirstOrDefault(t => t != null);
            Color C(string name, Color d) => src.HasProperty(name) ? src.GetColor(name) : d;
            float F(string name, float d) => src.HasProperty(name) ? src.GetFloat(name) : d;

            m.SetTexture("_BaseMap", T("basecolor") ?? T("maintex"));
            m.SetColor("_BaseColor", C("baseColorFactor", C("_Color", Color.white)));
            m.SetFloat("_Metallic", F("metallicFactor", 0f));
            m.SetFloat("_Smoothness", 1f - F("roughnessFactor", 1f) * 0.9f);
            var normal = T("normal");
            m.SetTexture("_BumpMap", normal);
            if (normal != null) m.EnableKeyword("_NORMALMAP"); else m.DisableKeyword("_NORMALMAP");
            var emissive = T("emissive");
            var ecol = C("emissiveFactor", Color.black);
            if (emissive != null || ecol.maxColorComponent > 0.01f)
            {
                m.SetTexture("_EmissionMap", emissive); m.SetColor("_EmissionColor", emissive != null && ecol.maxColorComponent < 0.01f ? Color.white : ecol);
                m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            if (src.renderQueue >= 3000)
            {
                // glass: URP Lit transparent
                m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                m.SetOverrideTag("RenderType", "Transparent");
            }
            else if (src.IsKeywordEnabled("_ALPHATEST_ON") || src.renderQueue == 2450)
            {
                m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", F("alphaCutoff", 0.5f)); m.EnableKeyword("_ALPHATEST_ON");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            cache[src] = m;
            return m;
        }

        static string Safe(string s) => new string((s ?? "mat").Select(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '_').ToArray());
    }
}
