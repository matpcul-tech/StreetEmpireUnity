// Block life: people hanging out on the corners. Every block in the town gets a small group standing together
// just inside one of its corners, on the first corner that is clear of buildings and street furniture. They are
// the townie bodies with the walking-about taken out (Loiterer): they stand in a loose ring facing each other and
// turn to look when the player comes close. A block's group is only switched on near the player, and at most the
// four nearest groups are on at once, so a 25 block town costs a dozen bodies.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StreetEmpire.Game
{
    public class BlockLife : MonoBehaviour
    {
        public static BlockLife Instance { get; private set; }
        /// <summary>Off in the Editor, where the tests run: a test that wants the corners alive turns it on before the scene loads.</summary>
#if UNITY_EDITOR
        public static bool AutoStart = false;
#else
        public static bool AutoStart = true;
#endif
        public int perGroup = 3, maxAwake = 4;
        public float wake = 85f, sleep = 100f, ring = 0.95f;

        public class Group { public int blockId; public Vector3 at; public bool on; public readonly List<GameObject> people = new(); }
        public readonly List<Group> groups = new();
        public int AwakeGroups => groups.Count(g => g.on);

        bool built; int look; float t;
        /// <summary>The corners have been picked (StreetDice puts its game where a block's group stands).</summary>
        public bool Built => built;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        void Update()
        {
            var s = GameSession.Instance; var turf = TurfWar.Instance; var life = TownLife.Instance;
            if (!AutoStart || s == null || s.player == null || turf == null || life == null || life.templates.Length == 0 || turf.centers.Length == 0) return;
            if (!built) { built = true; for (int i = 0; i < turf.centers.Length; i++) if (Corner(turf.centers[i], out var at)) groups.Add(new Group { blockId = turf.blockIds[i], at = at }); }
            t -= Time.deltaTime; if (t > 0f) return; t = 0.4f;
            var p = s.player.transform.position;
            // the nearest few inside the wake ring are on; one that is on stays on out to the sleep ring
            var want = groups.Where(g => Flat(p, g.at) < (g.on ? sleep : wake)).OrderBy(g => Flat(p, g.at)).Take(maxAwake).ToList();
            foreach (var g in groups)
            {
                bool on = want.Contains(g); if (on == g.on) continue;
                g.on = on;
                if (on && g.people.Count == 0) Spawn(g, life);
                foreach (var who in g.people) if (who != null) who.SetActive(on);
            }
        }

        /// <summary>A spot just inside a corner of the block that nothing stands on.</summary>
        public static bool Corner(Vector3 c, out Vector3 at)
        {
            foreach (var (x, z) in new[] { (18.6f, 18.6f), (-18.6f, -18.6f), (18.6f, -18.6f), (-18.6f, 18.6f), (0f, 19.2f), (19.2f, 0f), (0f, -19.2f), (-19.2f, 0f) })
            {
                at = new Vector3(c.x + x, 0.3f, c.z + z);
                // clear from knee height up: the ground and the sidewalk slab do not count
                if (Physics.OverlapSphere(at + Vector3.up * 1.6f, 1.4f, ~0, QueryTriggerInteraction.Ignore).Length == 0) return true;
            }
            at = default; return false;
        }

        /// <summary>Models that must not stand on a corner yet. Template 6 is ped-f-3, the woman in teal: standing
        /// still her face points at the sky (LookShots photographs, build de79b58). Her head bone reads level while
        /// it does, so the fault is in how her head is rigged to the skeleton and no guard on the bones reaches it.
        /// She stays out of the corner groups until the model is re-rigged; she still walks the blocks.</summary>
        public static readonly string[] BadStanders = { "Townie Template 6" };
        public static bool StandsBadly(GameObject template) => template != null && System.Array.Exists(BadStanders, n => template.name.StartsWith(n));

        void Spawn(Group g, TownLife life)
        {
            for (int i = 0; i < perGroup; i++)
            {
                float a = i * Mathf.PI * 2f / perGroup + g.blockId * 0.7f;
                var off = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * ring;
                var tpl = life.templates[look++ % life.templates.Length];
                for (int k = 0; k < life.templates.Length && StandsBadly(tpl); k++) tpl = life.templates[look++ % life.templates.Length];
                var who = Instantiate(tpl, g.at + off, Quaternion.LookRotation(-off), transform);
                who.name = $"Corner {g.blockId}.{i + 1}";
                // a townie that stays where it is: no errands
                var townie = who.GetComponent<Townie>(); if (townie != null) DestroyImmediate(townie);
                who.AddComponent<Loiterer>().center = g.at;
                g.people.Add(who);
            }
        }
    }
}
