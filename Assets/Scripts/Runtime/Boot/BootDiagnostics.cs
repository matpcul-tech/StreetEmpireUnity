// Early, crash-tolerant diagnostics. Starts before any scene loads and writes every log line (any thread) to
// boot-log.txt in the app's data folder, flushing each line, so a native crash still leaves a trail. The next
// launch reads the previous trail plus Android's own record of how the last process died (Android 11 and up)
// and the Boot scene shows it on screen with a Copy button.
using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace StreetEmpire.Boot
{
    public static class BootDiagnostics
    {
        public const string OkMarker = "RUNNING_OK";
        static readonly object gate = new();
        static StreamWriter writer;
        static int lines;
        static float startTime;

        /// <summary>The previous run's log (empty on a first install).</summary>
        public static string PreviousLog { get; private set; } = "";
        /// <summary>How Android says the last process ended (Android 11+), or a note why it is unknown.</summary>
        public static string PreviousExit { get; private set; } = "";
        /// <summary>True when the last run never reached the OK marker or Android reports a crash, ANR or kill for memory.</summary>
        public static bool PreviousCrashed { get; private set; }
        public static string LogPath => Path.Combine(Application.persistentDataPath, "boot-log.txt");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Begin()
        {
            try
            {
                string path = LogPath, prev = path + ".prev";
                if (File.Exists(path)) { File.Copy(path, prev, true); PreviousLog = File.ReadAllText(prev); }
                bool hadLog = PreviousLog.Length > 0;
                writer = new StreamWriter(path, false, Encoding.UTF8) { AutoFlush = true };
                startTime = Time.realtimeSinceStartup;
                Write($"boot {DateTime.Now:yyyy-MM-dd HH:mm:ss} {Application.version} {LabelText()}");
                Write($"device {SystemInfo.deviceModel} | {SystemInfo.operatingSystem} | ram {SystemInfo.systemMemorySize} MB");
                Write($"gpu {SystemInfo.graphicsDeviceName} | {SystemInfo.graphicsDeviceType} | {SystemInfo.graphicsDeviceVersion} | vram {SystemInfo.graphicsMemorySize} MB | astc {SystemInfo.SupportsTextureFormat(TextureFormat.ASTC_6x6)}");
                Application.logMessageReceivedThreaded += OnLog;
                SceneManager.sceneLoaded += (s, m) => Write($"scene loaded {s.name} ({Mem()})");
                bool exitCrash = ReadExitInfo();
                PreviousCrashed = exitCrash || (hadLog && !PreviousLog.Contains(OkMarker));
                var go = new GameObject("BootHeartbeat") { hideFlags = HideFlags.HideAndDontSave };
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<BootHeartbeat>();
            }
            catch (Exception e) { Debug.LogWarning($"BootDiagnostics could not start: {e.Message}"); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void BeforeScene() => Write($"before first scene ({Mem()})");

        public static void Write(string line)
        {
            lock (gate)
            {
                if (writer == null || lines > 600) return;
                lines++;
                try { writer.WriteLine($"[{Time.realtimeSinceStartup - startTime:0.00}s] {line}"); } catch { }
            }
        }

        static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Log && lines > 300) return;
            var top = type == LogType.Exception || type == LogType.Error ? " @ " + (stack ?? "").Split('\n')[0] : "";
            Write($"{type}: {message}{top}");
        }

        internal static string Mem() => $"mem {Profiler.GetTotalAllocatedMemoryLong() / 1048576} of {Profiler.GetTotalReservedMemoryLong() / 1048576} MB, mono {Profiler.GetMonoUsedSizeLong() / 1048576} MB";

        static string LabelText() { var t = Resources.Load<TextAsset>("build-label"); return t != null ? t.text.Trim() : "dev"; }

        /// <summary>ActivityManager.getHistoricalProcessExitReasons for this app. Native crash traces are a
        /// protobuf tombstone, so only their readable strings are kept (abort message, library and function names).</summary>
        static bool ReadExitInfo()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                int sdk = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
                if (sdk < 30) { PreviousExit = $"Android API {sdk}: exit reasons need Android 11"; return false; }
                var act = UnityEngine.Android.AndroidApplication.currentActivity;
                var am = act.Call<AndroidJavaObject>("getSystemService", "activity");
                var list = am.Call<AndroidJavaObject>("getHistoricalProcessExitReasons", act.Call<string>("getPackageName"), 0, 1);
                if (list == null || list.Call<int>("size") == 0) { PreviousExit = "no earlier exit recorded"; return false; }
                var info = list.Call<AndroidJavaObject>("get", 0);
                int reason = info.Call<int>("getReason");
                string desc = info.Call<string>("getDescription");
                long when = info.Call<long>("getTimestamp");
                var sb = new StringBuilder();
                sb.Append($"last exit: {ReasonName(reason)} ({reason}) at {DateTimeOffset.FromUnixTimeMilliseconds(when).ToLocalTime():MM-dd HH:mm:ss}");
                sb.Append($", pss {info.Call<long>("getPss") / 1024} MB, rss {info.Call<long>("getRss") / 1024} MB, importance {info.Call<int>("getImportance")}");
                if (!string.IsNullOrEmpty(desc)) sb.Append($"\ndescription: {desc}");
                if (reason == 4 || reason == 5 || reason == 6)
                {
                    var stream = info.Call<AndroidJavaObject>("getTraceInputStream");
                    if (stream != null)
                    {
                        var bytes = ReadBytes(stream, sdk);
                        var parsed = reason == 5 ? Tombstone.Summarize(bytes) : null;
                        sb.Append("\ntrace:\n").Append(parsed ?? ReadableStrings(bytes));
                    }
                }
                PreviousExit = sb.ToString();
                Write(PreviousExit.Split('\n')[0]);
                return reason == 3 || reason == 4 || reason == 5 || reason == 6 || reason == 9;
            }
            catch (Exception e) { PreviousExit = "exit info unavailable: " + e.Message; return false; }
#else
            PreviousExit = "exit reasons are only read on Android";
            return false;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static byte[] ReadBytes(AndroidJavaObject stream, int sdk)
        {
            try
            {
                if (sdk >= 33) { var s = stream.Call<sbyte[]>("readAllBytes"); var b = new byte[s.Length]; Buffer.BlockCopy(s, 0, b, 0, s.Length); return b; }
                var ms = new MemoryStream();
                for (int i = 0; i < 65536; i++) { int v = stream.Call<int>("read"); if (v < 0) break; ms.WriteByte((byte)v); }
                return ms.ToArray();
            }
            catch { return Array.Empty<byte>(); }
            finally { try { stream.Call("close"); } catch { } }
        }
#endif

        /// <summary>Printable ASCII runs of 5+ characters, deduplicated, at most 120 lines.</summary>
        public static string ReadableStrings(byte[] data)
        {
            if (data == null || data.Length == 0) return "(empty)";
            var sb = new StringBuilder(); var run = new StringBuilder(); int count = 0;
            var seen = new System.Collections.Generic.HashSet<string>();
            void Flush() { if (run.Length >= 5 && count < 120 && seen.Add(run.ToString())) { sb.AppendLine(run.ToString()); count++; } run.Clear(); }
            foreach (var b in data) { if (b >= 32 && b < 127) run.Append((char)b); else if (b == 10 || b == 9) { Flush(); } else Flush(); }
            Flush();
            return sb.ToString();
        }

        static string ReasonName(int r) => r switch
        {
            1 => "exit self", 2 => "signaled", 3 => "low memory", 4 => "crash (Java)", 5 => "crash (native)",
            6 => "ANR (not responding)", 7 => "init failure", 8 => "permission change", 9 => "excessive resource use",
            10 => "user requested", 11 => "user stopped", 12 => "dependency died", 13 => "other", 14 => "freezer", 15 => "package state change", 16 => "package updated",
            _ => "unknown",
        };

        internal static float Since => Time.realtimeSinceStartup - startTime;
    }
}
