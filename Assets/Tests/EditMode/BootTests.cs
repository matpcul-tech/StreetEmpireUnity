using System.Text;
using NUnit.Framework;
using StreetEmpire.Boot;

namespace StreetEmpire.Tests
{
    public class BootTests
    {
        [Test]
        public void TombstoneStringsAreExtracted()
        {
            var data = new byte[] { 0x0a, 0x03 }; // protobuf noise around readable text
            var bytes = Encoding.ASCII.GetBytes("abort message: Out of memory");
            var all = new byte[data.Length + bytes.Length + 4];
            data.CopyTo(all, 0); bytes.CopyTo(all, 2); all[^2] = 0x12; all[^1] = 0x41;
            var s = BootDiagnostics.ReadableStrings(all);
            StringAssert.Contains("abort message: Out of memory", s);
            Assert.AreEqual("(empty)", BootDiagnostics.ReadableStrings(new byte[0]));
        }
    }
}

namespace StreetEmpire.Tests
{
    public class ScriptFileTests
    {
        /// <summary>A MonoBehaviour whose class name differs from its file name gets no proper script asset, and
        /// the build then saved HitReaction into the scene inconsistently, which crashed the Android player.</summary>
        [Test]
        public void EveryGameComponentHasItsOwnScriptFile()
        {
            var withScript = new System.Collections.Generic.HashSet<System.Type>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Scripts" }))
            {
                var ms = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                var c = ms != null ? ms.GetClass() : null;
                if (c != null) withScript.Add(c);
            }
            var missing = new System.Collections.Generic.List<string>();
            foreach (var t in typeof(StreetEmpire.Game.HitReaction).Assembly.GetTypes())
                if (typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(t) && !t.IsAbstract && !t.IsNested && !withScript.Contains(t)) missing.Add(t.FullName);
            Assert.IsEmpty(missing, "MonoBehaviours without a matching file: " + string.Join(", ", missing));
        }
    }
}
