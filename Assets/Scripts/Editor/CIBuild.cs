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
            // 47c3075 also set medium managed stripping and stripped unused mesh channels, and that APK crashed on
            // launch on a Samsung phone. Both are back to what the M2 to M4 APKs (which ran) used: minimal
            // stripping, all mesh channels. LZ4HC only changes how the data is packed.
            PlayerSettings.stripUnusedMeshComponents = false;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
            if (target == BuildTarget.Android) options.options |= BuildOptions.CompressWithLz4HC;
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
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
    }
}
