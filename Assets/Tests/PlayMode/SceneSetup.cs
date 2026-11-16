using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    /// <summary>Runs in the Editor before play mode tests: rebuilds the scenes from code (ProjectBootstrap.Ensure).</summary>
    public class SceneSetup : IPrebuildSetup
    {
        public void Setup()
        {
#if UNITY_EDITOR
            var t = System.Type.GetType("StreetEmpire.EditorTools.ProjectBootstrap, StreetEmpire.Editor");
            t.GetMethod("Ensure").Invoke(null, null);
#endif
        }
    }
}
