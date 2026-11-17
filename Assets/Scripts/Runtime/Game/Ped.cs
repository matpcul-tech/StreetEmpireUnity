// A pedestrian on the Eastside sidewalks (pedStep in state.js, cut down): walks laps of its block's sidewalk
// on a rounded loop (smooth corners, a meter clear of the building walls), steps to its right to pass
// people in its way, runs from the player when hit, goes down at 0 hp and gets back up with 30 hp after 5 s.
using System.Collections.Generic;
using UnityEngine;

namespace StreetEmpire.Game
{
    [RequireComponent(typeof(NpcBody), typeof(Health))]
    public class Ped : MonoBehaviour
    {
        public float speed = 1.25f, fleeSpeed = 4.5f;
        /// <summary>Center of the block whose sidewalk it walks, the loop's half size (middle of the sidewalk)
        /// and corner radius, and which way round it goes (+1 or -1).</summary>
        public Vector3 blockCenter; public float half = 21f, cornerRadius = 2.5f; public int lap = 1;
        public Vector3 along = Vector3.forward;   // kept for older scenes: the first heading
        public const float SidewalkInner = 20f, SidewalkOuter = 22f;

        static readonly List<Ped> all = new();
        NpcBody body; Health health; float flee, downT, sidestep; Transform from;
        Vector3[] loop; Vector3 vel;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Awake()
        {
            body = GetComponent<NpcBody>(); health = GetComponent<Health>();
            health.Damaged += (_, src) => { flee = 8; from = src != null ? src.transform : null; };
            if (blockCenter == Vector3.zero) { blockCenter = transform.position; half = 0; }
            loop = BuildLoop(blockCenter, half, cornerRadius, 0.5f);
        }

        /// <summary>Points every ~step meters round a rounded square centered on c.</summary>
        public static Vector3[] BuildLoop(Vector3 c, float half, float r, float step)
        {
            var pts = new List<Vector3>();
            if (half <= r) { pts.Add(c); return pts.ToArray(); }
            float s = half - r;
            // corners counter-clockwise seen from above (+x, +z), each a quarter arc, joined by straight sides
            var corners = new[] { new Vector2(s, s), new Vector2(-s, s), new Vector2(-s, -s), new Vector2(s, -s) };
            for (int k = 0; k < 4; k++)
            {
                float a0 = k * 90f * Mathf.Deg2Rad;
                int arcN = Mathf.Max(2, Mathf.CeilToInt(r * Mathf.PI / 2 / step));
                for (int i = 0; i < arcN; i++)
                {
                    float a = a0 + i / (float)arcN * Mathf.PI / 2;
                    pts.Add(c + new Vector3(corners[k].x + Mathf.Cos(a) * r, 0, corners[k].y + Mathf.Sin(a) * r));
                }
                var endArc = c + new Vector3(corners[k].x + Mathf.Cos(a0 + Mathf.PI / 2) * r, 0, corners[k].y + Mathf.Sin(a0 + Mathf.PI / 2) * r);
                var next = corners[(k + 1) % 4]; float a1 = (k + 1) * 90f * Mathf.Deg2Rad;
                var startNext = c + new Vector3(next.x + Mathf.Cos(a1) * r, 0, next.y + Mathf.Sin(a1) * r);
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(endArc, startNext) / step));
                for (int i = 0; i < n; i++) pts.Add(Vector3.Lerp(endArc, startNext, i / (float)n));
            }
            return pts.ToArray();
        }

        int Nearest(Vector3 p)
        {
            int best = 0; float bd = float.MaxValue;
            for (int i = 0; i < loop.Length; i++) { var d = loop[i] - p; d.y = 0; float m = d.sqrMagnitude; if (m < bd) { bd = m; best = i; } }
            return best;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (health.Down)
            {
                downT += dt; body.SetDown(true);
                if (downT > 5) { downT = 0; health.Restore(30); body.SetDown(false); flee = 8; }
                return;
            }
            Vector3 want;
            var pos = transform.position;
            if (flee > 0 && from != null)
            {
                flee -= dt;
                want = pos - from.position; want.y = 0; want = want.normalized * fleeSpeed;
            }
            else if (loop.Length > 1)
            {
                int i = Nearest(pos), n = loop.Length;
                var target = loop[((i + lap * 4) % n + n) % n];
                var outward = target - blockCenter; outward.y = 0; outward = outward.normalized;
                // someone close ahead (another ped or the player): step aside to pass
                bool blocked = false; float closest = 99f;
                var fwd = transform.forward;
                foreach (var nb in FindNearby(pos))
                {
                    var d = nb - pos; d.y = 0;
                    if (d.magnitude < 3.5f && Vector3.Dot(d.normalized, fwd) > 0.4f) { blocked = true; closest = Mathf.Min(closest, d.magnitude); }
                }
                // pass on the right when that is the curb side; going the other way round (right hand to the
                // walls, a meter off) hold the line and slow down while the other one steps out
                bool rightIsCurb = Vector3.Dot(Vector3.Cross(Vector3.up, fwd), outward) >= 0;
                sidestep = Mathf.MoveTowards(sidestep, blocked && rightIsCurb ? 0.85f : 0f, dt * 1.2f);
                // the one holding the line stops short instead of shouldering past (the capsules would slide it
                // off the loop toward the walls)
                float pace = blocked && !rightIsCurb ? (closest < 1.4f ? 0f : 0.4f) : 1f;
                var aim = target + outward * sidestep - pos; aim.y = 0;
                want = aim.normalized * speed * pace;
            }
            else want = Vector3.zero;
            // ease into the new direction so it walks round corners instead of snapping
            vel = Vector3.MoveTowards(vel, want, (flee > 0 ? 12f : 3f) * dt);
            body.Move(vel, dt);
        }

        static readonly List<Vector3> near = new();
        List<Vector3> FindNearby(Vector3 p)
        {
            near.Clear();
            foreach (var o in all) if (o != this) near.Add(o.transform.position);
            var s = GameSession.Instance;
            if (s != null && s.player != null) near.Add(s.player.transform.position);
            return near;
        }
    }
}
