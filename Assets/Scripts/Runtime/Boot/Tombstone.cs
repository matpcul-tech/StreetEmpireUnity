// Minimal reader for Android's protobuf tombstone (system/core/debuggerd/proto/tombstone.proto), so the boot
// report can show the crashing thread's frames (library, offset, function) and the last log lines Android
// attached, instead of loose strings. Unknown fields are skipped.
using System;
using System.Collections.Generic;
using System.Text;

namespace StreetEmpire.Boot
{
    public static class Tombstone
    {
        struct Field { public int num, wire; public ulong v; public int start, len; }

        static IEnumerable<Field> Fields(byte[] b, int start, int end)
        {
            int p = start;
            while (p < end)
            {
                ulong key = Varint(b, ref p); var f = new Field { num = (int)(key >> 3), wire = (int)(key & 7) };
                switch (f.wire)
                {
                    case 0: f.v = Varint(b, ref p); break;
                    case 1: p += 8; break;
                    case 2: int len = (int)Varint(b, ref p); f.start = p; f.len = len; p += len; break;
                    case 5: p += 4; break;
                    default: yield break;
                }
                if (p > end) yield break;
                yield return f;
            }
        }

        static ulong Varint(byte[] b, ref int p)
        {
            ulong r = 0; int s = 0;
            while (p < b.Length) { byte x = b[p++]; r |= (ulong)(x & 0x7f) << s; if ((x & 0x80) == 0) break; s += 7; }
            return r;
        }

        static string Str(byte[] b, Field f) => Encoding.UTF8.GetString(b, f.start, f.len);

        /// <summary>Readable summary, or null when the bytes do not parse as a tombstone.</summary>
        public static string Summarize(byte[] b)
        {
            try
            {
                uint tid = 0; string abort = "", signal = ""; var threads = new Dictionary<uint, (string name, List<string> frames)>();
                var logs = new List<string>();
                foreach (var f in Fields(b, 0, b.Length))
                {
                    if (f.num == 6 && f.wire == 0) tid = (uint)f.v;
                    else if (f.num == 14 && f.wire == 2) abort = Str(b, f);
                    else if (f.num == 10 && f.wire == 2) foreach (var s in Fields(b, f.start, f.start + f.len)) { if (s.num == 2 && s.wire == 2) signal += Str(b, s) + " "; if (s.num == 4 && s.wire == 2) signal += Str(b, s) + " "; }
                    else if (f.num == 16 && f.wire == 2)
                    {
                        uint key = 0; Field val = default; bool has = false;
                        foreach (var e in Fields(b, f.start, f.start + f.len)) { if (e.num == 1) key = (uint)e.v; if (e.num == 2 && e.wire == 2) { val = e; has = true; } }
                        if (!has) continue;
                        string name = ""; var frames = new List<string>();
                        foreach (var t in Fields(b, val.start, val.start + val.len))
                        {
                            if (t.num == 2 && t.wire == 2) name = Str(b, t);
                            if (t.num == 4 && t.wire == 2)
                            {
                                ulong rel = 0, off = 0; string fn = "", file = "";
                                foreach (var fr in Fields(b, t.start, t.start + t.len))
                                {
                                    if (fr.num == 1) rel = fr.v; else if (fr.num == 5) off = fr.v;
                                    else if (fr.num == 4 && fr.wire == 2) fn = Str(b, fr); else if (fr.num == 6 && fr.wire == 2) file = Str(b, fr);
                                }
                                int slash = file.LastIndexOf('/');
                                frames.Add($"#{frames.Count:00} pc {rel:x8} {(slash >= 0 ? file.Substring(slash + 1) : file)} {(fn.Length > 0 ? fn + "+" + off : "")}");
                            }
                        }
                        threads[key] = (name, frames);
                    }
                    else if (f.num == 18 && f.wire == 2)
                        foreach (var lb in Fields(b, f.start, f.start + f.len))
                            if (lb.num == 2 && lb.wire == 2)
                            {
                                string tag = "", msg = "";
                                foreach (var m in Fields(b, lb.start, lb.start + lb.len)) { if (m.num == 5 && m.wire == 2) tag = Str(b, m); if (m.num == 6 && m.wire == 2) msg = Str(b, m); }
                                logs.Add($"{tag}: {msg}".Trim());
                            }
                }
                if (threads.Count == 0) return null;
                var sb = new StringBuilder();
                sb.AppendLine($"signal: {signal.Trim()}");
                if (abort.Length > 0) sb.AppendLine($"abort: {abort}");
                if (threads.TryGetValue(tid, out var ct))
                {
                    sb.AppendLine($"crashing thread {tid} {ct.name}:");
                    for (int i = 0; i < ct.frames.Count && i < 48; i++) sb.AppendLine(ct.frames[i]);
                }
                if (logs.Count > 0)
                {
                    sb.AppendLine("last log lines:");
                    for (int i = Math.Max(0, logs.Count - 40); i < logs.Count; i++) sb.AppendLine(logs[i]);
                }
                return sb.ToString();
            }
            catch { return null; }
        }
    }
}
