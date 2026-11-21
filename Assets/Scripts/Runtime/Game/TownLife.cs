// Town life: owns the activity spots and the townies that use them. Spawns a couple of workers per shop
// and a few residents per live block from the townie templates, finds the way round a block's sidewalk
// between two spots, brings people back out of the doors they went in, and switches a block's people off
// while the player is far away (phones).
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StreetEmpire.Game
{
    public class TownLife : MonoBehaviour
    {
        public static TownLife Instance { get; private set; }

        [Header("Baked when the scene is built (TownLifeBuilder)")]
        public GameObject[] templates = new GameObject[0];
        public ActivitySpot[] spots = new ActivitySpot[0];

        [Header("How many people")]
        public int workersPerShop = 2;
        public int residentsPerBlock = 3;
        /// <summary>Hard cap on townies in the district.</summary>
        public int maxTownies = 16;
        /// <summary>A block's people switch off past sleepDistance from the player and back on inside wakeDistance.</summary>
        public float sleepDistance = 100f, wakeDistance = 88f;

        /// <summary>Townies walk the middle of the 2 m sidewalk ring, the same line the pedestrians use.</summary>
        public const float RingHalf = 21f, CornerRadius = 2.5f;

        public readonly List<Townie> townies = new();
        readonly Dictionary<int, List<ActivitySpot>> byBlock = new();
        readonly Dictionary<int, Vector3[]> loops = new();
        readonly Dictionary<int, Vector3> centers = new();
        readonly Dictionary<int, bool> asleep = new();
        static readonly List<ActivitySpot> none = new();
        float checkT; int look;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public List<ActivitySpot> SpotsOn(int blockId) => byBlock.TryGetValue(blockId, out var l) ? l : none;
        public bool Asleep(int blockId) => asleep.TryGetValue(blockId, out bool a) && a;

        void Start()
        {
            foreach (var s in spots)
            {
                if (s == null) continue;
                if (!byBlock.TryGetValue(s.blockId, out var list))
                {
                    byBlock[s.blockId] = list = new List<ActivitySpot>();
                    var c = new Vector3(s.blockCenter.x, 0, s.blockCenter.z);
                    centers[s.blockId] = c; asleep[s.blockId] = false;
                    loops[s.blockId] = Ped.BuildLoop(c, RingHalf, CornerRadius, 1f);
                }
                list.Add(s);
            }
            if (templates == null || templates.Length == 0 || templates.All(x => x == null)) return;
            float hour = GameClock.Hour;
            foreach (var kv in byBlock)
            {
                var list = kv.Value;
                // workers: out of the shop door a few seconds in, or still inside if the shop is closed
                foreach (var g in list.Where(s => s.kind == SpotKind.Work && s.building != null).GroupBy(s => s.building))
                {
                    var ws = g.ToArray();
                    var door = list.FirstOrDefault(s => s.kind == SpotKind.Door && s.building == g.Key);
                    for (int i = 0; i < Mathf.Min(workersPerShop, ws.Length) && townies.Count < maxTownies; i++)
                    {
                        var t = Spawn(kv.Key, Townie.Role.Worker, door, ws);
                        if (t == null) continue;
                        if (door != null) { t.transform.position = door.Stand; t.StartInside(door, 1f + i * 3f, !door.OpenAt(hour)); }
                        else { t.transform.position = ws[i].Stand + Vector3.up * 0.05f; t.gameObject.SetActive(true); }
                    }
                }
                // residents: out of their own doors, one after another
                var houses = list.Where(s => s.kind == SpotKind.Door && !s.shop).ToArray();
                var doors = houses.Length > 0 ? houses : list.Where(s => s.kind == SpotKind.Door).ToArray();
                for (int i = 0; i < residentsPerBlock && townies.Count < maxTownies; i++)
                {
                    var home = doors.Length > 0 ? doors[i % doors.Length] : null;
                    var t = Spawn(kv.Key, Townie.Role.Resident, home, null);
                    if (t == null) continue;
                    if (home != null) { t.transform.position = home.Stand; t.StartInside(home, 2f + i * 6f, false); }
                    else
                    {
                        var loop = loops[kv.Key];
                        t.transform.position = loop[(i * loop.Length / Mathf.Max(1, residentsPerBlock)) % loop.Length] + Vector3.up * 0.3f;
                        t.gameObject.SetActive(true);
                    }
                }
            }
        }

        Townie Spawn(int blockId, Townie.Role role, ActivitySpot home, ActivitySpot[] work)
        {
            GameObject tpl = null;
            for (int k = 0; k < templates.Length && tpl == null; k++) tpl = templates[look++ % templates.Length];
            if (tpl == null) return null;
            var go = Instantiate(tpl);   // the template is inactive, so the copy is too until it walks out
            go.name = $"Townie {townies.Count + 1} ({role})";
            var t = go.GetComponent<Townie>();
            if (t == null) t = go.AddComponent<Townie>();
            t.role = role; t.blockId = blockId; t.home = home;
            t.work = work ?? new ActivitySpot[0];
            t.speed = Random.Range(1.1f, 1.4f);
            townies.Add(t);
            return t;
        }

        void Update()
        {
            checkT -= Time.deltaTime;
            if (checkT > 0) return;
            checkT = 0.25f;
            var s = GameSession.Instance;
            bool hasPlayer = s != null && s.player != null;
            var pp = hasPlayer ? s.player.transform.position : Vector3.zero;
            foreach (var kv in centers)
            {
                bool a = false;
                if (hasPlayer)
                {
                    float d = Vector2.Distance(new Vector2(pp.x, pp.z), new Vector2(kv.Value.x, kv.Value.z));
                    a = Asleep(kv.Key) ? d > wakeDistance : d > sleepDistance;
                }
                asleep[kv.Key] = a;
            }
            float hour = GameClock.Hour;
            foreach (var t in townies)
            {
                if (t == null) continue;
                bool sleep = Asleep(t.blockId);
                var go = t.gameObject;
                if (t.State == Townie.Mode.Inside)
                {
                    bool due = Time.time >= t.InsideUntil && (!t.UntilOpen || t.home == null || t.home.OpenAt(hour));
                    if (!sleep && due) { go.SetActive(true); t.ComeOut(); }
                    else if (go.activeSelf) go.SetActive(false);
                    continue;
                }
                if (go.activeSelf == sleep) go.SetActive(!sleep);
            }
        }

        /// <summary>Way points from one place on a block to another: straight there when it is close and no
        /// building is in the way, else out to the sidewalk, round it the short way, and in to the place.</summary>
        public void Route(int blockId, Vector3 from, Vector3 to, List<Vector3> path)
        {
            path.Clear();
            var d = to - from; d.y = 0;
            if (!loops.TryGetValue(blockId, out var loop) || loop.Length < 2 || (d.magnitude < 7f && StreetRoute.Clear(from, to, 0.35f)))
            { path.Add(to); return; }
            int n = loop.Length, a = Nearest(loop, from), b = Nearest(loop, to);
            int fwd = ((b - a) % n + n) % n;
            int dir = fwd <= n - fwd ? 1 : -1, count = dir > 0 ? fwd : n - fwd;
            for (int i = 0; i < count; i += 2) path.Add(loop[((a + dir * i) % n + n) % n]);
            path.Add(loop[b]);
            path.Add(to);
        }

        static int Nearest(Vector3[] loop, Vector3 p)
        {
            int best = 0; float bd = float.MaxValue;
            for (int i = 0; i < loop.Length; i++)
            {
                var d = loop[i] - p; d.y = 0;
                float m = d.sqrMagnitude;
                if (m < bd) { bd = m; best = i; }
            }
            return best;
        }

        /// <summary>Another townie or the player close in front of this one.</summary>
        public bool SomeoneAhead(Townie self, Vector3 pos, Vector3 forward)
        {
            foreach (var t in townies)
            {
                if (t == null || t == self || !t.gameObject.activeSelf) continue;
                if (Ahead(t.transform.position - pos, forward)) return true;
            }
            var s = GameSession.Instance;
            return s != null && s.player != null && Ahead(s.player.transform.position - pos, forward);
        }

        static bool Ahead(Vector3 d, Vector3 forward)
        {
            d.y = 0;
            float m = d.magnitude;
            return m > 0.01f && m < 1.8f && Vector3.Dot(d / m, forward) > 0.5f;
        }
    }
}
