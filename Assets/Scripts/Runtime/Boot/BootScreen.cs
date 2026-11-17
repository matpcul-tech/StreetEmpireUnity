// First scene. Normally it just loads Eastside. If the last run crashed (on Android) it shows what the
// previous run logged and how Android says it died, with Copy and Continue buttons, before loading anything heavy.
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StreetEmpire.Boot
{
    public class BootScreen : MonoBehaviour
    {
        public const string NextScene = "Eastside";
        bool showReport, loading;
        string report;
        Vector2 scroll;
        GUIStyle text, button;

        void Start()
        {
            BootDiagnostics.Write("boot scene up");
            showReport = Application.platform == RuntimePlatform.Android && BootDiagnostics.PreviousCrashed;
            if (showReport)
            {
                var prev = BootDiagnostics.PreviousLog.Split('\n');
                int from = Mathf.Max(0, prev.Length - 45);
                report = $"Street Empire {Label()} | last run did not finish starting\n{BootDiagnostics.PreviousExit}\n\nprevious run log (last lines):\n{string.Join("\n", prev, from, prev.Length - from)}";
            }
            else Load();
        }

        void Load()
        {
            if (loading) return;
            loading = true;
            BootDiagnostics.Write("loading " + NextScene);
            SceneManager.LoadSceneAsync(NextScene);
        }

        static string Label() { var t = Resources.Load<TextAsset>("build-label"); return t != null ? t.text.Trim() : "dev"; }

        void OnGUI()
        {
            int fs = Mathf.Max(14, Screen.height / 40);
            text ??= new GUIStyle(GUI.skin.label) { fontSize = fs, wordWrap = true, normal = { textColor = Color.white } };
            button ??= new GUIStyle(GUI.skin.button) { fontSize = fs + 4 };
            if (!showReport || loading)
            {
                GUI.Label(new Rect(20, Screen.height - fs * 3, Screen.width - 40, fs * 2), $"Loading Eastside...  {Label()}", text);
                return;
            }
            float bh = fs * 3f, w = Screen.width - 40;
            GUILayout.BeginArea(new Rect(20, 20, w, Screen.height - bh - 50));
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label(report, text);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            if (GUI.Button(new Rect(20, Screen.height - bh - 20, w / 2 - 10, bh), "Copy report", button)) GUIUtility.systemCopyBuffer = report;
            if (GUI.Button(new Rect(20 + w / 2 + 10, Screen.height - bh - 20, w / 2 - 10, bh), "Continue", button)) Load();
        }

        void Update()
        {
            // touch scrolling for the report
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (showReport && !loading && touch != null && touch.primaryTouch.press.isPressed) scroll.y += touch.primaryTouch.delta.ReadValue().y;
        }
    }
}
