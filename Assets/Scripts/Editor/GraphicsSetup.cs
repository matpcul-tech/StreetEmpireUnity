// Two URP quality tiers for the Eastside, both made from code like the rest of the project:
//   v75 (Matthew: Ultra did not earn its cost, and High had the crisper palm shadow):
//   Ultra (default): HDR, 4x MSAA, full render scale, medium soft shadows over 120 m in 4 cascades on a 4096 map
//   (the first cascade only 6 m deep, so the shadows at the player's feet are the sharpest in the game), full
//   resolution SSAO, up to 6 lamp lights per object, props and building detail held 30% farther (GraphicsQuality).
//   High: HDR, 4x MSAA (the palm fronds need it), 70% render scale, low soft shadows over 40 m in 2 cascades on a
//   2048 map (10 m first cascade: softer, blurrier edges), no SSAO, up to 3 lamp lights per object, props and
//   building detail drop 20% sooner.
// Both feed the same global post volume (ACES tonemapping, a little bloom, color grading, vignette).
// The player switches tiers at run time with the HUD button (GraphicsQuality); nothing here runs in the player.
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StreetEmpire.EditorTools
{
    public static class GraphicsSetup
    {
        public const string UltraPath = "Assets/Settings/URP-Default.asset";     // the original asset is the Ultra tier
        public const string UltraRendererPath = "Assets/Settings/URP-Default-Renderer.asset";
        public const string HighPath = "Assets/Settings/URP-High.asset";
        public const string HighRendererPath = "Assets/Settings/URP-High-Renderer.asset";
        public const string VolumePath = "Assets/Settings/Eastside-Post.asset";
        public const string NightVolumePath = "Assets/Settings/Eastside-Night.asset";

        public static UniversalRenderPipelineAsset Ultra => AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UltraPath);
        public static UniversalRenderPipelineAsset High => AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(HighPath);

        public static UniversalRenderPipelineAsset Ensure()
        {
            var ultra = Pipeline(UltraPath, UltraRendererPath, out var ultraRenderer);
            Tier(ultra, msaa: 4, distance: 120f, cascades: 4, map: 4096, softQuality: 2, scale: 1f, lights: 6);
            Ssao(ultraRenderer, true);
            var high = Pipeline(HighPath, HighRendererPath, out var highRenderer);
            Tier(high, msaa: 4, distance: 40f, cascades: 2, map: 2048, softQuality: 1, scale: 0.7f, lights: 3);
            Ssao(highRenderer, false);
            AssetDatabase.SaveAssets();
            return ultra;
        }

        static UniversalRenderPipelineAsset Pipeline(string path, string rendererPath, out UniversalRendererData renderer)
        {
            var p = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            if (p == null)
            {
                p = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(p, path);
            }
            return p;
        }

        static void Tier(UniversalRenderPipelineAsset p, int msaa, float distance, int cascades, int map, int softQuality, float scale, int lights)
        {
            p.supportsHDR = true;               // tonemapping and bloom need HDR
            p.msaaSampleCount = msaa;
            p.renderScale = scale;
            p.shadowDistance = distance;
            p.shadowCascadeCount = cascades;
            if (cascades == 2) p.cascade2Split = 0.25f;
            if (cascades == 3) p.cascade3Split = new Vector2(0.1f, 0.3f);
            if (cascades == 4) p.cascade4Split = new Vector3(0.05f, 0.15f, 0.4f);
            p.supportsCameraDepthTexture = true;
            var so = new SerializedObject(p);
            Set(so, "m_MainLightShadowsSupported", true);
            Set(so, "m_MainLightShadowmapResolution", map);
            Set(so, "m_SoftShadowsSupported", softQuality > 0);   // High: hard shadow edges (0)
            if (softQuality > 0) Set(so, "m_SoftShadowQuality", softQuality); // 1 low, 2 medium, 3 high
            Set(so, "m_ShadowDepthBias", 1f);
            Set(so, "m_ShadowNormalBias", 1.0f);          // 0.6 left shadow acne on the hoodie (dark blotch on the chest and side)
            Set(so, "m_ColorGradingMode", 1);            // HDR grading
            Set(so, "m_UseSRPBatcher", true);
            Set(so, "m_EnableLODCrossFade", false);       // no dithered LOD cross fade (it read as stripes on the palms)
            Set(so, "m_SupportsLightLayers", true);         // rendering layers: the character fill light lights people only
            // per pixel (the street lamp lights near the camera at night). v75c: set by value: the enum is declared
            // Disabled 0, PerVertex 2, PerPixel 1, and the old enumValueIndex 2 was not per pixel: the lamps lit
            // only dense meshes (the player, the sidewalk joint strips) and never the big ground and wall faces
            { var sp = so.FindProperty("m_AdditionalLightsRenderingMode"); if (sp != null) sp.intValue = (int)UnityEngine.Rendering.Universal.LightRenderingMode.PerPixel; }
            Set(so, "m_AdditionalLightsPerObjectLimit", lights);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(p);
            if (p.additionalLightsRenderingMode != UnityEngine.Rendering.Universal.LightRenderingMode.PerPixel) throw new Exception($"[GraphicsSetup] {p.name}: additional lights are {p.additionalLightsRenderingMode}, not per pixel");
        }

        static void Set(SerializedObject so, string name, object v)
        {
            var sp = so.FindProperty(name);
            if (sp == null) { Debug.LogWarning($"[GraphicsSetup] no property {name}"); return; }
            switch (v)
            {
                case bool b: sp.boolValue = b; break;
                // shadow map size is an enum whose values are the sizes; the others are small enums whose index is the value
                case int i: if (sp.propertyType == SerializedPropertyType.Enum && i < 16) sp.enumValueIndex = i; else sp.intValue = i; break;
                case float f: sp.floatValue = f; break;
            }
        }

        /// <summary>Adds (or removes) URP's SSAO renderer feature. The type is looked up by name so this keeps
        /// compiling if URP changes its visibility.</summary>
        static void Ssao(UniversalRendererData r, bool on)
        {
            var type = Type.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime");
            if (type == null) { Debug.LogWarning("[GraphicsSetup] SSAO feature type not found"); return; }
            var existing = r.rendererFeatures.FirstOrDefault(f => f != null && f.GetType() == type);
            if (!on)
            {
                if (existing != null) { r.rendererFeatures.Remove(existing); AssetDatabase.RemoveObjectFromAsset(existing); UnityEngine.Object.DestroyImmediate(existing, true); }
                SyncMap(r); EditorUtility.SetDirty(r); return;
            }
            var feature = existing ?? (ScriptableRendererFeature)ScriptableObject.CreateInstance(type);
            if (existing == null)
            {
                feature.name = "SSAO";
                AssetDatabase.AddObjectToAsset(feature, r);
                r.rendererFeatures.Add(feature);
            }
            var so = new SerializedObject(feature);
            var s = so.FindProperty("m_Settings");
            if (s != null)
            {
                void F(string n, float v) { var p = s.FindPropertyRelative(n); if (p != null) p.floatValue = v; }
                void B(string n, bool v) { var p = s.FindPropertyRelative(n); if (p != null) p.boolValue = v; }
                F("Intensity", 2.2f); F("Radius", 0.55f); F("DirectLightingStrength", 0.3f);
                B("Downsample", false);       // v75: full resolution, so the contact shadows read on Ultra
                B("AfterOpaque", false);
                // depth only, normals rebuilt from it: the depth-normals source draws the whole scene once more
                void E(string n, int v) { var p = s.FindPropertyRelative(n); if (p != null) p.enumValueIndex = v; else Debug.LogWarning($"[GraphicsSetup] SSAO has no {n}"); }
                E("Source", 0); E("NormalSamples", 1);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            feature.SetActive(true);
            SyncMap(r);
            EditorUtility.SetDirty(r);
        }

        /// <summary>The renderer keeps a parallel list of the features' local file ids; keep it matching.</summary>
        static void SyncMap(UniversalRendererData r)
        {
            var so = new SerializedObject(r);
            var map = so.FindProperty("m_RendererFeatureMap");
            if (map == null) return;
            map.arraySize = r.rendererFeatures.Count;
            for (int i = 0; i < r.rendererFeatures.Count; i++)
            {
                long id = 0;
                if (r.rendererFeatures[i] != null) AssetDatabase.TryGetGUIDAndLocalFileIdentifier(r.rendererFeatures[i], out _, out id);
                map.GetArrayElementAtIndex(i).longValue = id;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>The one global post processing profile, rebuilt each time so the numbers here stay the truth. It is
        /// locked: the scene has this single global Volume, nothing changes it at run time, and the exposure is fixed
        /// (URP has no auto exposure; post exposure is a constant), so every view and both tiers grade the same.</summary>
        public static VolumeProfile PostProfile()
        {
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath) != null) AssetDatabase.DeleteAsset(VolumePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumePath);
            T Add<T>() where T : VolumeComponent { var c = profile.Add<T>(true); c.name = typeof(T).Name; AssetDatabase.AddObjectToAsset(c, profile); return c; }
            var tone = Add<Tonemapping>(); tone.mode.Override(TonemappingMode.ACES);
            // the original web game's look (src/grade.js on top of ACES): a strong S curve, a little less saturation,
            // golden highlights, deep brown lifted shadows, a heavy vignette and a soft bloom on the lamps and sun
            // v73: a little bloom (lamps, lit windows, the sun disk), contrast and saturation up a step
            var bloom = Add<Bloom>(); bloom.threshold.Override(1.0f); bloom.intensity.Override(0.3f); bloom.scatter.Override(0.6f);
            bloom.tint.Override(new Color(1f, 0.86f, 0.7f));
            bloom.highQualityFiltering.Override(false);
            var grade = Add<ColorAdjustments>(); grade.postExposure.Override(0f); grade.contrast.Override(28f); grade.saturation.Override(8f);
            var wb = Add<WhiteBalance>(); wb.temperature.Override(10f); wb.tint.Override(3f);
            var split = Add<SplitToning>(); split.highlights.Override(new Color(1f, 0.84f, 0.62f)); split.shadows.Override(new Color(0.5f, 0.38f, 0.32f));
            split.balance.Override(-10f);
            var lift = Add<LiftGammaGain>(); lift.lift.Override(new Vector4(1f, 0.96f, 0.92f, 0.015f)); lift.gain.Override(new Vector4(1.02f, 1f, 0.95f, 0f));
            var vig = Add<Vignette>(); vig.intensity.Override(0.36f); vig.smoothness.Override(0.55f); vig.color.Override(new Color(0.12f, 0.06f, 0.03f));
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        /// <summary>Night look: what the night adds on top of PostProfile, blended in by NightLook with the clock.
        /// More bloom from a lower threshold (lamps, signs, headlights and windows halo), deeper color, cool blue
        /// shadows under warm lights, a darker cooler vignette.</summary>
        public static VolumeProfile NightProfile()
        {
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(NightVolumePath) != null) AssetDatabase.DeleteAsset(NightVolumePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, NightVolumePath);
            T Add<T>() where T : VolumeComponent { var c = profile.Add<T>(true); c.name = typeof(T).Name; AssetDatabase.AddObjectToAsset(c, profile); return c; }
            var bloom = Add<Bloom>(); bloom.threshold.Override(0.8f); bloom.intensity.Override(0.9f); bloom.scatter.Override(0.72f);
            bloom.tint.Override(new Color(1f, 0.95f, 0.95f));
            var grade = Add<ColorAdjustments>(); grade.postExposure.Override(0.15f); grade.contrast.Override(24f); grade.saturation.Override(24f);
            var wb = Add<WhiteBalance>(); wb.temperature.Override(-4f); wb.tint.Override(3f);
            var split = Add<SplitToning>(); split.highlights.Override(new Color(1f, 0.86f, 0.75f)); split.shadows.Override(new Color(0.42f, 0.42f, 0.62f));
            split.balance.Override(-20f);
            var vig = Add<Vignette>(); vig.intensity.Override(0.42f); vig.smoothness.Override(0.55f); vig.color.Override(new Color(0.03f, 0.03f, 0.08f));
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }
    }
}
