// Shows which build is installed (for example "v0.5 d740c9c") in a corner of the HUD. CI writes
// Assets/Resources/build-label.txt with the commit before it builds; editor and local builds say "dev".
using UnityEngine;
using UnityEngine.UI;

namespace StreetEmpire.UI
{
    [RequireComponent(typeof(Text))]
    public class BuildLabel : MonoBehaviour
    {
        public const string Fallback = "v0.5 dev";
        public static string Label { get { var t = Resources.Load<TextAsset>("build-label"); return t != null && t.text.Trim().Length > 0 ? t.text.Trim() : Fallback; } }
        void Start() => GetComponent<Text>().text = Label;
    }
}
