// Getting around a block: a straight line if nothing is in the way, else the street corner (or the point on
// our street level with the target) that makes the shortest way there. Officers on foot and police cruisers
// both use it, standing in for steerPoint() and findPath() in state.js.
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
    }
}
