// Build entry point for GameCI (buildMethod: StreetEmpire.EditorTools.CIBuild.Build).
// GameCI passes -customBuildTarget and -customBuildPath on the command line.
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace StreetEmpire.EditorTools
{
    public static class CIBuild
    {
        public static void Build()
        {
            ProjectBootstrap.Ensure();
            var target = Enum.TryParse(Arg("-customBuildTarget") ?? "", out BuildTarget t) ? t : EditorUserBuildSettings.activeBuildTarget;
            var path = Arg("-customBuildPath") ?? $"build/{target}/StreetEmpire";
            var options = new BuildPlayerOptions
            {
                scenes = ProjectBootstrap.BuildScenes,
                target = target,
                locationPathName = path,
                options = BuildOptions.None,
            };
            if (target == BuildTarget.Android) EditorUserBuildSettings.buildAppBundle = false; // APK for now, AAB with signing later
            if (target == BuildTarget.Android) AndroidSigning();
            // 47c3075 changed three things and its APK crashed at launch on a Samsung: medium managed stripping,
            // mesh channel stripping and LZ4HC. d1b55f0 reverted the first two and still crashed, so LZ4HC (which
            // packs all player data into one data.unity3d archive instead of the split files the M5 APK had)
            // is off too. Every packaging setting now matches the M5 APK that ran; only the release key differs.
            PlayerSettings.stripUnusedMeshComponents = false;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
            // bisect variants (workflow_dispatch input "variant", passed as -seVariant)
            var variant = Arg("-seVariant") ?? "";
            if (target == BuildTarget.Android && variant.Contains("gles3"))
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            }
            if (target == BuildTarget.Android && variant.Contains("lz4hc")) options.options |= BuildOptions.CompressWithLz4HC;
            if (target == BuildTarget.Android)
                Debug.Log($"[CIBuild] Android variant '{variant}', graphics {string.Join(",", PlayerSettings.GetGraphicsAPIs(BuildTarget.Android))}, options {options.options}, stripping {PlayerSettings.GetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Android)}");
            var report = BuildPipeline.BuildPlayer(options);
            var r = report.summary;
            Debug.Log($"[CIBuild] {target} {r.result} size {r.totalSize} errors {r.totalErrors} at {path}");
            // game-ci/cli validates custom build methods by scraping the log: it needs the literal
            // "Build succeeded!" or its "Build results" block (see UnityBuildValidation in game-ci/cli).
            Console.WriteLine("###########################");
            Console.WriteLine("#      Build results      #");
            Console.WriteLine("###########################");
            Console.WriteLine($"Duration: {r.totalTime}");
            Console.WriteLine($"Warnings: {r.totalWarnings}");
            Console.WriteLine($"Errors: {(r.result == BuildResult.Succeeded ? 0 : Math.Max(1, r.totalErrors))}");
            Console.WriteLine($"Size: {r.totalSize} bytes");
            if (r.result == BuildResult.Succeeded) Console.WriteLine("Build succeeded!");
            if (Application.isBatchMode) EditorApplication.Exit(r.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>GameCI passes the version code and the release key (decoded into the project folder) on the
        /// command line; its own build script would apply them, a custom build method has to. Nothing secret is logged.</summary>
        static void AndroidSigning()
        {
            if (int.TryParse(Arg("-androidVersionCode") ?? "", out int code) && code > 0) PlayerSettings.Android.bundleVersionCode = code;
            var ks = Arg("-androidKeystoreName");
            string pass = Arg("-androidKeystorePass"), alias = Arg("-androidKeyaliasName"), aliasPass = Arg("-androidKeyaliasPass");
            string path = string.IsNullOrEmpty(ks) ? null : System.IO.Path.GetFullPath(ks);
            bool signed = path != null && System.IO.File.Exists(path) && !string.IsNullOrEmpty(pass) && !string.IsNullOrEmpty(alias);
            PlayerSettings.Android.useCustomKeystore = signed;
            if (signed)
            {
                PlayerSettings.Android.keystoreName = path; PlayerSettings.Android.keystorePass = pass;
                PlayerSettings.Android.keyaliasName = alias; PlayerSettings.Android.keyaliasPass = aliasPass;
            }
            Debug.Log($"[CIBuild] Android version code {PlayerSettings.Android.bundleVersionCode}, {(signed ? "signed with the release key" : "debug signed (no release key given)")}");
            // CI always passes the key. A debug signed APK has a different certificate every build, so it can never
            // update over an installed copy ("App not installed"). Stop instead of shipping one.
            if (!signed && ks != null && Application.isBatchMode)
            {
                Debug.LogError("[CIBuild] release key missing or incomplete, refusing to build a debug signed APK");
                EditorApplication.Exit(1);
            }
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
    }
}
