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
                scenes = new[] { ProjectBootstrap.BootScene },
                target = target,
                locationPathName = path,
                options = BuildOptions.None,
            };
            if (target == BuildTarget.Android) EditorUserBuildSettings.buildAppBundle = false; // APK for now, AAB with signing later
            var report = BuildPipeline.BuildPlayer(options);
            var r = report.summary;
            Debug.Log($"[CIBuild] {target} {r.result} size {r.totalSize} errors {r.totalErrors} at {path}");
            if (Application.isBatchMode) EditorApplication.Exit(r.result == BuildResult.Succeeded ? 0 : 1);
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
    }
}
