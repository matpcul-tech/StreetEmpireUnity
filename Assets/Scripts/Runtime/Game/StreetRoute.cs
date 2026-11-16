// Getting around a block: a straight line if nothing is in the way, else the street corner (or the point on
// our street level with the target) that makes the shortest way there. Officers on foot and police cruisers
// both use it, standing in for steerPoint() and findPath() in state.js.
using System.Collections.Generic;
using UnityEngine;

namespace StreetEmpire.Game
{
    public static class StreetRoute
    {
        public static bool Clear(Vector3 a, Vector3 b, float radius)
        {
            a.y = b.y = 1f; var d = b - a; float len = d.magnitude; if (len < 0.1f) return true;
            foreach (var h in Physics.SphereCastAll(a, radius, d / len, len, ~0, QueryTriggerInteraction.Ignore))
                if (h.collider.GetComponent<StreetEmpire.City.BuildingMarker>() != null) return false;
            return true;
        }

        /// <summary>minSkip: corners closer than this are ignored (one we are already standing in).</summary>
        public static Vector3 Corner(GameSession s, Vector3 here, Vector3 target, float radius, float minSkip)
        {
            Vector3 best = target; float bd = float.MaxValue; float skip2 = minSkip * minSkip;
            foreach (var x in s.streetX) foreach (var z in s.streetZ)
            {
                var c = new Vector3(x, here.y, z);
                float via = Vector3.Distance(here, c) + Vector3.Distance(c, target);
                if (via < bd && (c - here).sqrMagnitude > skip2 && Clear(here, c, radius)) { bd = via; best = c; }
            }
            foreach (var x in s.streetX) { var c = new Vector3(x, here.y, target.z); float via = Vector3.Distance(here, c) + Vector3.Distance(c, target); if (via < bd && (c - here).sqrMagnitude > skip2 && Clear(here, c, radius) && Clear(c, target, radius)) { bd = via; best = c; } }
            foreach (var z in s.streetZ) { var c = new Vector3(target.x, here.y, z); float via = Vector3.Distance(here, c) + Vector3.Distance(c, target); if (via < bd && (c - here).sqrMagnitude > skip2 && Clear(here, c, radius) && Clear(c, target, radius)) { bd = via; best = c; } }
            return best;
        }

        // ---- street graph for cars: the district's street center lines and their crossings ----

        /// <summary>The nearest street line to p: x lines (x = const) or z lines (z = const), and p projected onto it.</summary>
        static (bool xLine, int line, Vector3 on) Snap(GameSession s, Vector3 p)
        {
            int bi = 0; bool bx = true; float bd = float.MaxValue;
            for (int i = 0; i < s.streetX.Length; i++) { float d = Mathf.Abs(p.x - s.streetX[i]); if (d < bd) { bd = d; bi = i; bx = true; } }
            for (int j = 0; j < s.streetZ.Length; j++) { float d = Mathf.Abs(p.z - s.streetZ[j]); if (d < bd) { bd = d; bi = j; bx = false; } }
            var on = bx ? new Vector3(s.streetX[bi], p.y, p.z) : new Vector3(p.x, p.y, s.streetZ[bi]);
            return (bx, bi, on);
        }

        /// <summary>Way points along the streets from a car at `from` to the street point nearest `to`
        /// (Dijkstra over the crossings), ending with that street point. Stands in for findPath() in state.js.</summary>
        public static List<Vector3> Path(GameSession s, Vector3 from, Vector3 to)
        {
            int nx = s.streetX.Length, nz = s.streetZ.Length, n = nx * nz;
            Vector3 Node(int k) => new Vector3(s.streetX[k / nz], from.y, s.streetZ[k % nz]);
            var a = Snap(s, from); var b = Snap(s, to);
            var result = new List<Vector3>();
            if (a.xLine == b.xLine && a.line == b.line) { result.Add(b.on); return result; }
            // nodes on a line: x line i holds k = i * nz + j for every j; z line j holds i * nz + j for every i
            IEnumerable<int> OnLine(bool xLine, int line) { if (xLine) for (int j = 0; j < nz; j++) yield return line * nz + j; else for (int i = 0; i < nx; i++) yield return i * nz + line; }
            var dist = new float[n]; var prev = new int[n]; var done = new bool[n];
            for (int k = 0; k < n; k++) { dist[k] = float.MaxValue; prev[k] = -1; }
            // the car can reach any crossing on its own street directly (it drives along it)
            foreach (int k in OnLine(a.xLine, a.line)) dist[k] = Vector3.Distance(a.on, Node(k));
            for (int it = 0; it < n; it++)
            {
                int u = -1; for (int k = 0; k < n; k++) if (!done[k] && (u < 0 || dist[k] < dist[u])) u = k;
                if (u < 0 || dist[u] == float.MaxValue) break;
                done[u] = true;
                int ui = u / nz, uj = u % nz;
                foreach (int v in OnLine(true, ui)) Relax(u, v);
                foreach (int v in OnLine(false, uj)) Relax(u, v);
            }
            void Relax(int u, int v) { if (v == u || done[v]) return; float nd = dist[u] + Vector3.Distance(Node(u), Node(v)); if (nd < dist[v]) { dist[v] = nd; prev[v] = u; } }
            int best = -1; float bd = float.MaxValue;
            foreach (int k in OnLine(b.xLine, b.line)) { float d = dist[k] + Vector3.Distance(Node(k), b.on); if (d < bd) { bd = d; best = k; } }
            if (best < 0) { result.Add(b.on); return result; }
            for (int k = best; k >= 0; k = prev[k]) result.Insert(0, Node(k));
            result.Add(b.on);
            return result;
        }
    }
}
