// Builds Assets/Scenes/Boot.unity: a camera and the BootScreen, nothing heavy, so it can show the previous
// run's crash report before Eastside (all the buildings, cars and characters) is loaded.
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StreetEmpire.EditorTools
{
    public static class BootSceneBuilder
    {
        public const string BootScene = "Assets/Scenes/Boot.unity";

        public static void Build()
        {
            var prev = SceneManager.GetActiveScene();
            bool additive = prev.IsValid() && !string.IsNullOrEmpty(prev.path);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, additive ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            var cam = new GameObject("Boot Camera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.07f, 0.07f, 0.08f);
            new GameObject("Boot").AddComponent<StreetEmpire.Boot.BootScreen>();
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, BootScene);
            if (additive) { SceneManager.SetActiveScene(prev); EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
