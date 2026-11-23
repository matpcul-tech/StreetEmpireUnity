// Shows which build is installed (for example "v0.5 d740c9c") and the frame rate in a corner of the HUD. CI writes
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
        // the frame rate rides beside it ("v0.5 d740c9c  58 fps"), averaged over half a second
        Text text; string label; float t; int frames;
        void Start() { text = GetComponent<Text>(); label = Label; text.text = label; }
        void Update()
        {
            frames++; t += Time.unscaledDeltaTime;
            if (t < 0.5f) return;
            text.text = label + "  " + Mathf.RoundToInt(frames / t) + " fps";
            frames = 0; t = 0f;
        }
    }
}
