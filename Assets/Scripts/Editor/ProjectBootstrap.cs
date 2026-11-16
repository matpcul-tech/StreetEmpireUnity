// This repo was scaffolded by hand, without opening the Editor. On first open (or first CI run) this
// creates what a normal "New Project" would have: the URP pipeline asset, the Boot scene and the database asset.
// Safe to run again: it only creates what is missing.
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StreetEmpire.EditorTools
{
    [InitializeOnLoad]
    public static class ProjectBootstrap
    {
        public const string BootScene = "Assets/Scenes/Boot.unity";
        const string PipelinePath = "Assets/Settings/URP-Default.asset";
        const string RendererPath = "Assets/Settings/URP-Default-Renderer.asset";
        const string DatabasePath = "Assets/Data/GameDatabase.asset";

        static ProjectBootstrap() => EditorApplication.delayCall += () => { if (!Application.isBatchMode) Ensure(); };

        [MenuItem("Street Empire/Run Project Bootstrap")]
        public static void Ensure()
        {
            EnsurePlayerSettings();
            EnsurePipeline();
            var db = EnsureDatabase();
            EnsureBootScene(db);
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
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true; // works on hosts that do not send Content-Encoding
            // Active Input Handling = Input System Package (1). Takes effect on the next Editor launch.
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
            if (GraphicsSettings.defaultRenderPipeline != null) return;
            Directory.CreateDirectory("Assets/Settings");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int keep = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = pipeline; }
            QualitySettings.SetQualityLevel(keep, false);
        }

        static GameDatabase EnsureDatabase()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (db != null) return db;
            db = ScriptableObject.CreateInstance<GameDatabase>();
            TextAsset T(string n) => AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/Data/{n}.json");
            db.crews = T("crews"); db.weapons = T("weapons"); db.vehicles = T("vehicles");
            db.jobs = T("jobs"); db.police = T("police"); db.world = T("world");
            AssetDatabase.CreateAsset(db, DatabasePath);
            return db;
        }

        static void EnsureBootScene(GameDatabase db)
        {
            if (!File.Exists(BootScene))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                var cam = Camera.main;
                if (cam != null) { cam.transform.position = new Vector3(0, 420, -260); cam.transform.rotation = Quaternion.Euler(60, 0, 0); cam.farClipPlane = 2000; }
                var boot = new GameObject("Boot").AddComponent<Boot>();
                boot.database = db;
                Directory.CreateDirectory("Assets/Scenes");
                EditorSceneManager.SaveScene(scene, BootScene);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootScene, true) };
        }
    }
}
