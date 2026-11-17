// This repo was scaffolded without opening the Editor. On first open (and before every CI build or
// play mode test) this creates what a normal "New Project" would have and rebuilds the scenes from code.
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StreetEmpire.EditorTools
{
    [InitializeOnLoad]
    public static class ProjectBootstrap
    {
        const string PipelinePath = "Assets/Settings/URP-Default.asset";
        const string RendererPath = "Assets/Settings/URP-Default-Renderer.asset";
        const string DatabasePath = "Assets/Data/GameDatabase.asset";

        public static string[] BuildScenes => new[] { BootSceneBuilder.BootScene, SceneBuilder.EastsideScene };

        static ProjectBootstrap() => EditorApplication.delayCall += () => { if (!Application.isBatchMode && !File.Exists(SceneBuilder.EastsideScene)) Ensure(); };

        [MenuItem("Street Empire/Run Project Bootstrap")]
        public static void Ensure()
        {
            EnsurePlayerSettings();
            EnsurePipeline();
            EnsureDatabase();
            AssetDatabase.SaveAssets();
            SceneBuilder.BuildEastside();
            BootSceneBuilder.Build();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootSceneBuilder.BootScene, true), new EditorBuildSettingsScene(SceneBuilder.EastsideScene, true) };
            AssetDatabase.SaveAssets();
        }

        static void EnsurePlayerSettings()
        {
            PlayerSettings.companyName = "matpcul-tech";
            PlayerSettings.productName = "Street Empire";
            // Placeholder id. Pick the final one before the first Play Console upload; it can never change after.
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.matpcultech.streetempire");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true; PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true; // works on hosts that do not send Content-Encoding
            PlayerSettings.WebGL.template = "PROJECT:StreetEmpire"; // shows a clear message when 3D cannot start
            // Active Input Handling = Input System Package (1). Takes effect on the next Editor launch, so never
            // flip it inside a batch build: the Editor would stay compiled for the old backend while the player
            // uses the new one ("script class layout is incompatible"). Set it on first interactive open, then commit ProjectSettings.
            if (Application.isBatchMode) return;
            var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (ps.Length > 0)
            {
                var so = new SerializedObject(ps[0]);
                var prop = so.FindProperty("activeInputHandler");
                if (prop != null && prop.intValue != 1) { prop.intValue = 1; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
        }

        static void EnsurePipeline()
        {
            Directory.CreateDirectory("Assets/Settings");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            // mobile and WebGL friendly: no HDR, 2x MSAA, one shadow cascade over the near streets
            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = 2;
            pipeline.renderScale = 1f;
            pipeline.shadowDistance = 90f;
            pipeline.shadowCascadeCount = 1;
            EditorUtility.SetDirty(pipeline);
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int keep = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = pipeline; }
            QualitySettings.SetQualityLevel(keep, false);
        }

        static void EnsureDatabase()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (db != null) return;
            db = ScriptableObject.CreateInstance<GameDatabase>();
            TextAsset T(string n) => AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/Data/{n}.json");
            db.crews = T("crews"); db.weapons = T("weapons"); db.vehicles = T("vehicles");
            db.jobs = T("jobs"); db.police = T("police"); db.world = T("world");
            AssetDatabase.CreateAsset(db, DatabasePath);
        }
    }
}
