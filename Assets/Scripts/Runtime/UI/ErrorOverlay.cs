// Crash-safe startup aid: any exception or error logged by the game is shown on screen (top of the screen,
// red text), so a phone screenshot shows the real error instead of nothing. It cannot catch native crashes.
using System.Collections.Generic;
using UnityEngine;

namespace StreetEmpire.UI
{
    public class ErrorOverlay : MonoBehaviour
    {
        static ErrorOverlay instance;
        readonly List<string> lines = new();
        GUIStyle style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (instance != null) return;
            var go = new GameObject("ErrorOverlay"); DontDestroyOnLoad(go);
            instance = go.AddComponent<ErrorOverlay>();
            Application.logMessageReceived += instance.OnLog;
        }

        void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            if (lines.Count >= 6) return;
            var first = (stack ?? "").Split('\n');
            lines.Add($"{type}: {message}\n  {(first.Length > 0 ? first[0] : "")}");
        }

        void OnDestroy() { if (instance == this) { Application.logMessageReceived -= OnLog; instance = null; } }

        void OnGUI()
        {
            if (lines.Count == 0) return;
            style ??= new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(16, Screen.height / 45), wordWrap = true, normal = { textColor = new Color(1f, 0.35f, 0.3f) } };
            var r = new Rect(10, Screen.height * 0.12f, Screen.width - 20, Screen.height * 0.5f);
            GUI.color = new Color(0, 0, 0, 0.75f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(r.x + 8, r.y + 6, r.width - 16, r.height - 12), $"Street Empire {BuildLabel.Label} error:\n" + string.Join("\n", lines), style);
        }
    }
}
