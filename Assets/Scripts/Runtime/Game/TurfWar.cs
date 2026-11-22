// The turf loop in the town: runs the Turf sim (Assets/Scripts/Sim/Turf.cs) for the district's blocks and puts
// bodies on it where the player is. Guards stand in the middle of their block and come for you when you walk in;
// knock them all down and hold the ring for captureTime to take the block. Your blocks pile up a stash you
// pocket by standing in the ring, rivals raid them (a warning first), and USE on a block you hold buys the next
// thing it needs: a guard (100), bats for the guards (80), the build up (200).
// Bodies only exist on the block the player is on. Everything else is the sim's counts.
using System.Collections.Generic;
using System.Linq;
using StreetEmpire.Player;
using StreetEmpire.Sim;
using StreetEmpire.Sim.Data;
using StreetEmpire.UI;
using UnityEngine;
using UnityEngine.UI;

namespace StreetEmpire.Game
{
    /// <summary>One of your own guards: stands at his post on a block you hold. He does not fight as a body (the sim rolls for him).</summary>
    public class TurfGuard : MonoBehaviour
    {
        public Vector3 post; public Vector3 look;
        NpcBody body; Health health; float downT;
        void Awake() { body = GetComponent<NpcBody>(); health = GetComponent<Health>(); }
        void Update()
        {
            float dt = Time.deltaTime;
            if (health != null && health.Down) { downT += dt; if (downT > 6f) Destroy(gameObject); return; }
            if (body == null) return;
            var back = post - transform.position; back.y = 0;
            if (back.magnitude > 0.7f) body.Move(back.normalized * 2.6f, dt);
            else { body.Face(transform.position + look); body.Move(Vector3.zero, dt); }
        }
    }

    public class TurfWar : MonoBehaviour
    {
        public static TurfWar Instance { get; private set; }
        /// <summary>Keep the turf between launches (PlayerPrefs). Off in the Editor so tests and scene builds always start clean.</summary>
#if UNITY_EDITOR
        public static bool Persist = false;
#else
        public static bool Persist = true;
#endif
        /// <summary>Tests that check exact cash or the message line (the job, the scuffle) turn the turf loop off before the scene loads.</summary>
        public static bool AutoStart = true;
        const string SaveKey = "turf.v1";

        [Header("Baked by GameBuilder")]
        public WorldFile world;
        public int[] blockIds = new int[0];
        public Vector3[] centers = new Vector3[0];         // Unity space, same order as blockIds
        public string[] crewIds = new string[0], crewNames = new string[0];
        public Color[] crewColors = new Color[0];
        public GameObject[] templates = new GameObject[0]; // inactive brawler bodies; the last one holds a bat
        public Text prompt;
        public GameHud hud; public Minimap radar; public MissionRunner job; public MuggerOpening opening; public ArmGuards arm;
        public long seed = 1;

        public TurfState State { get; private set; }
        public int ZoneBlock { get; private set; } = -1;
        public int NearBlock { get; private set; } = -1;
        public int GuardBodies => bodies.Count(b => !b.raider && !b.friendly);
        public int RaiderBodies => bodies.Count(b => b.raider);
        public int FriendlyBodies => bodies.Count(b => b.friendly);

        class Body { public GameObject go; public Health hp; public int block, slot; public string crew; public bool raider, friendly, gone; }
        readonly List<Body> bodies = new();
        readonly List<Renderer> rings = new();
        MaterialPropertyBlock mpb; Mesh ringMesh, footMesh;
        float syncT, saveT; int look; bool ringsMade;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            Instance = this; mpb = new MaterialPropertyBlock();
            if (arm != null) arm.enabled = false;   // its one purchase is part of the buy prompt here
            if (prompt) prompt.gameObject.SetActive(false);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            var s = GameSession.Instance;
            State = Persist ? Load() : null;
            if (State == null) State = Turf.Create(world, blockIds, RivalStarts(), seed, s != null ? s.cash : 150);
            else if (s != null) { s.cash = State.Crew(Turf.You).cash; s.rep = State.Crew(Turf.You).rep; }
            Paint();
        }

        /// <summary>A rival on each of the other three corners of the district (the first block is yours).</summary>
        Dictionary<string, int> RivalStarts()
        {
            var d = new Dictionary<string, int>();
            if (blockIds.Length < 4) return d;
            int g = world.grid; int minX = blockIds.Min(i => i % g), maxX = blockIds.Max(i => i % g), minZ = blockIds.Min(i => i / g), maxZ = blockIds.Max(i => i / g);
            int Id(int x, int z) => z * g + x;
            var corners = new[] { Id(maxX, maxZ), Id(maxX, minZ), Id(minX, maxZ), Id(minX, minZ) }.Where(i => i != blockIds[0] && blockIds.Contains(i)).ToArray();
            for (int i = 0; i < corners.Length && i < Turf.Rivals.Length; i++) d[Turf.Rivals[i]] = corners[i];
            return d;
        }

        /// <summary>Start the turf over (a new game).</summary>
        public void NewGame()
        {
            foreach (var b in bodies) if (b.go != null) Destroy(b.go);
            bodies.Clear();
            var s = GameSession.Instance;
            State = Turf.Create(world, blockIds, RivalStarts(), seed, s != null ? s.cash : 150);
            PlayerPrefs.DeleteKey(SaveKey); Paint();
        }

        TurfState Load()
        {
            string j = PlayerPrefs.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(j)) return null;
            try
            {
                var st = JsonUtility.FromJson<TurfState>(j);
                if (st == null || st.blocks == null || st.blocks.Count != blockIds.Length || st.over == "win") return null;
                foreach (int id in blockIds) if (st.Block(id) == null) return null;
                if (st.Crew(Turf.You) == null) return null;
                st.events = new List<TurfEvent>();
                foreach (var b in st.blocks) if (string.IsNullOrEmpty(b.capBy)) b.capBy = null;   // JSON keeps no nulls
                return st;
            }
            catch (System.Exception) { return null; }
        }

        public void Save()
        {
            if (!Persist || State == null) return;
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(State)); PlayerPrefs.Save();
        }

        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() => Save();

        public int IndexOf(int blockId) => System.Array.IndexOf(blockIds, blockId);
        public Vector3 Center(int blockId) { int i = IndexOf(blockId); return i >= 0 ? centers[i] : Vector3.zero; }
        public string CrewName(string id) { int i = System.Array.IndexOf(crewIds, id); return i >= 0 ? crewNames[i] : id; }
        public Color CrewColor(string id) { int i = System.Array.IndexOf(crewIds, id); return i >= 0 ? crewColors[i] : Color.gray; }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        void Update()
        {
            var s = GameSession.Instance; if (s == null || s.player == null || State == null || !AutoStart) return;
            float dt = Time.deltaTime;
            var p = s.player.transform.position;
            var user = s.player.GetComponent<VehicleUser>();
            bool inCar = user != null && user.Car != null;

            // where the player is: on which block, and in whose ring (not from a car, and not while down)
            float half = world.block / 2f + 4f; int near = -1, zone = -1;
            for (int i = 0; i < centers.Length; i++)
            {
                if (Mathf.Abs(p.x - centers[i].x) < half && Mathf.Abs(p.z - centers[i].z) < half) near = blockIds[i];
                if (!inCar && !s.IsDown && Flat(p, centers[i]) < world.zoneRadius) zone = blockIds[i];
            }
            NearBlock = near; ZoneBlock = zone;

            // the session holds the player's cash and rep; the sim reads and writes them through its crew
            var you = State.Crew(Turf.You);
            you.cash = s.cash; you.rep = s.rep;
            Turf.Step(State, world, new TurfPresence { zoneBlock = zone, nearBlock = s.IsDown ? -1 : near }, dt);
            Buy(s, p, inCar);
            s.cash = you.cash; s.rep = you.rep;

            bool changed = false;
            foreach (var e in State.events) changed |= Tell(s, e);
            State.events.Clear();
            if (changed) { Paint(); Save(); }
            if ((saveT += dt) > 20f) { saveT = 0; Save(); }

            if ((syncT -= dt) <= 0) { syncT = 0.25f; Sync(s, p); }
            Rings();
            if (hud != null) { hud.turfOwned = State.Owned(Turf.You); hud.turfTotal = State.blocks.Count; }
        }

        /// <summary>One event from the sim as a line on screen. True when block ownership changed.</summary>
        bool Tell(GameSession s, TurfEvent e)
        {
            var b = e.block >= 0 ? State.Block(e.block) : null; string hustle = b != null ? b.hustle : "";
            switch (e.type)
            {
                case "stash": if (e.amount >= 10) s.Say($"Collected ${e.amount} from {hustle}.", 2.5f); return false;
                case "cleared": if (e.crew != Turf.You) s.Say($"{hustle} is clear. Hold the ring to take it.", 3f); return false;
                case "capture":
                    if (e.crew == Turf.You) { s.Say($"{hustle} is yours." + (e.amount > 0 ? $" +${e.amount} from the stash." : ""), 4f); if (opening != null) opening.TookBlock(); }
                    else if (e.from == Turf.You) s.Say($"{CrewName(e.crew)} took {hustle} from you.", 4f);
                    else if (e.block == NearBlock) s.Say($"{CrewName(e.crew)} took {hustle}.", 3f);
                    return true;
                case "raid": s.Say($"{CrewName(e.crew)} are gathering to hit {hustle}. {Mathf.RoundToInt(world.raidGather)}s out.", 4f); return false;
                case "raidGo": s.Say($"{CrewName(e.crew)} are hitting {hustle} now.", 3.5f); return false;
                case "raidBeaten": if (e.from == Turf.You) s.Say($"{hustle} held. {CrewName(e.crew)} are gone.", 3.5f); return false;
                case "recruit": s.Say($"A guard is on {hustle}. -${e.amount}", 2.5f); return false;
                case "arm": s.Say($"Guards on {hustle} got bats and more health. -${e.amount}", 3f); return false;
                case "build": s.Say($"{hustle} is built up. It earns double. -${e.amount}", 3f); return false;
                case "win": s.Say("Eastside is yours. Every block.", 8f); return false;
            }
            return false;
        }

        /// <summary>The buy prompt on a block you hold, and the capture count while you hold a ring.</summary>
        void Buy(GameSession s, Vector3 p, bool inCar)
        {
            string text = "";
            var zb = ZoneBlock >= 0 ? State.Block(ZoneBlock) : null;
            if (zb != null && zb.owner != Turf.You)
            {
                bool raiders = zb.hasRaid && zb.raid.eta <= 0 && zb.raid.n > 0;
                if (zb.guards > 0) text = $"{zb.guards} guard{(zb.guards > 1 ? "s" : "")} still on {zb.hustle}";
                else if (raiders) text = $"{CrewName(zb.raid.crew)} are fighting over {zb.hustle}";
                else text = $"Taking {zb.hustle}   {Mathf.Max(0f, world.captureTime - zb.capT):0.0}s";
            }
            var nb = NearBlock >= 0 ? State.Block(NearBlock) : null;
            string buy = text.Length == 0 && !inCar && !s.IsDown ? Turf.NextBuy(nb) : null;
            if (buy != null)
            {
                bool nearUnc = job != null && job.Stage == MissionRunner.State.Offered && job.giver != null && Flat(p, job.giver.position) < job.talkRange;
                bool nearCar = false;
                var user = s.player.GetComponent<VehicleUser>();
                if (user != null) foreach (var c in FindObjectsByType<StreetEmpire.Vehicles.CarController>(FindObjectsSortMode.None)) if (!c.hasDriver && (c.transform.position - p).sqrMagnitude < user.enterRange * user.enterRange) { nearCar = true; break; }
                if (nearUnc || nearCar) buy = null;
            }
            if (buy != null)
            {
                int cost = Turf.Cost(world, buy);
                string what = buy == "recruit" ? "Hire a guard" : buy == "arm" ? "Arm guards" : "Build up " + nb.hustle;
                text = $"{(ArmGuards.Touch ? "USE" : "E")}  {what} (${cost})";
                if (PlayerControls.Instance != null && PlayerControls.Instance.UsePressed)
                {
                    bool ok = buy == "recruit" ? Turf.Recruit(State, world, nb.id) : buy == "arm" ? Turf.Arm(State, world, nb.id) : Turf.Build(State, world, nb.id);
                    if (!ok) s.Say($"Need ${cost}.", 2.5f);
                }
            }
            if (prompt) { prompt.gameObject.SetActive(text.Length > 0); if (text.Length > 0) prompt.text = text; }
        }

        // ---- bodies ----

        void Sync(GameSession s, Vector3 p)
        {
            for (int i = bodies.Count - 1; i >= 0; i--) if (bodies[i].go == null || bodies[i].gone) bodies.RemoveAt(i);
            foreach (var b in State.blocks)
            {
                var c = Center(b.id);
                bool here = NearBlock == b.id, far = Flat(p, c) > 60f;
                var mine = bodies.Where(x => x.block == b.id).ToList();
                // a block that changed hands, or that the player has left behind: its bodies go
                foreach (var x in mine)
                    if (far || (!x.raider && x.crew != b.owner)) Dismiss(x, far);
                if (far) continue;

                var guards = mine.Where(x => !x.raider && !x.gone).ToList();
                if (here && guards.Count < b.guards)
                {
                    int free = 0; while (free < Posts.Length - 1 && guards.Any(g => g.slot == free)) free++;   // the first post nobody stands at
                    SpawnGuard(b, c, free);
                }
                else if (guards.Count > b.guards) Dismiss(guards[guards.Count - 1], false);

                var raiders = mine.Where(x => x.raider && !x.gone).ToList();
                bool fight = b.owner == Turf.You && b.hasRaid && b.raid.eta <= 0 && here && !s.IsDown;
                int want = fight ? b.raid.n : 0;
                if (raiders.Count < want) SpawnRaider(b, c, raiders.Count);
                else if (raiders.Count > want) Dismiss(raiders[raiders.Count - 1], false);
            }
        }

        void Dismiss(Body x, bool quiet)
        {
            if (x.gone) return;
            x.gone = true;
            if (x.go == null) return;
            var br = x.go.GetComponent<Brawler>();
            if (quiet || br == null) Destroy(x.go); else br.leave = true;   // a brawler runs off, then removes himself
        }

        GameObject Make(Vector3 at, Vector3 face, float hp, bool bat, string name, Color crew)
        {
            if (templates.Length == 0) return null;
            var pick = bat || templates.Length == 1 ? templates[templates.Length - 1] : templates[look++ % (templates.Length - 1)];
            face.y = 0;
            var go = Instantiate(pick, new Vector3(at.x, 0.3f, at.z), face.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(face) : Quaternion.identity);
            go.name = name;
            var h = go.GetComponent<Health>(); h.max = h.hp = hp;
            // a ring in the crew's color at his feet says whose he is
            var fx = CombatFx.Instance;
            if (fx != null && fx.ringMat != null)
            {
                if (footMesh == null) footMesh = Ring(0.42f, 0.58f, 20);
                var ring = new GameObject("Crew Ring"); ring.transform.SetParent(go.transform, false); ring.transform.localPosition = new Vector3(0, 0.06f, 0);
                ring.AddComponent<MeshFilter>().sharedMesh = footMesh;
                var r = ring.AddComponent<MeshRenderer>(); r.sharedMaterial = fx.ringMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                mpb.SetColor(BaseColor, new Color(crew.r, crew.g, crew.b, 0.9f)); r.SetPropertyBlock(mpb);
            }
            return go;
        }

        static readonly Vector3[] Posts = { new Vector3(2.6f, 0, 2f), new Vector3(-2.8f, 0, -1.6f), new Vector3(-2.8f, 0, 6f) };

        void SpawnGuard(TurfBlock b, Vector3 c, int slot)
        {
            // posts in the backyard, clear of what PropBuilder.Backyards puts there (the fence down the middle,
            // the two palms, the barrel and the air conditioner)
            var off = Posts[slot % Posts.Length];
            var dir = off.normalized;
            var post = c + off;
            bool friendly = b.owner == Turf.You, bat = b.armed;
            var go = Make(post, dir, 60f + (b.armed ? world.upgradeHp : 0f), bat, $"{CrewName(b.owner)} guard ({b.hustle})", CrewColor(b.owner));
            if (go == null) return;
            var body = new Body { go = go, hp = go.GetComponent<Health>(), block = b.id, slot = slot, crew = b.owner, friendly = friendly };
            var br = go.GetComponent<Brawler>();
            if (friendly)
            {
                // not a brawler: he never comes for the player
                if (br != null) DestroyImmediate(br);
                var g = go.AddComponent<TurfGuard>(); g.post = post; g.look = dir;
            }
            else if (br != null) { br.posted = true; br.post = post; br.bat = bat; br.style = slot % 2 == 0 ? Brawler.Style.Rush : Brawler.Style.Flank; }
            go.SetActive(true);
            body.hp.KnockedOut += _ => Down(body);
            bodies.Add(body);
        }

        void SpawnRaider(TurfBlock b, Vector3 c, int slot)
        {
            // they come in off the street on the side they set out from
            var from = Center(b.raid.from) - c; from.y = 0;
            var dir = from.sqrMagnitude > 1f ? from.normalized : Vector3.forward;
            var side = Vector3.Cross(Vector3.up, dir);
            var at = c + dir * (world.block / 2f + 1f) + side * ((slot - 1.5f) * 1.8f);
            var go = Make(at, -dir, 60f, slot == 3, $"{CrewName(b.raid.crew)} raider ({b.hustle})", CrewColor(b.raid.crew));
            if (go == null) return;
            var body = new Body { go = go, hp = go.GetComponent<Health>(), block = b.id, crew = b.raid.crew, raider = true };
            var br = go.GetComponent<Brawler>();
            if (br != null) { br.bat = slot == 3; br.style = slot % 2 == 0 ? Brawler.Style.Rush : Brawler.Style.Flank; }
            go.SetActive(true);
            body.hp.KnockedOut += _ => Down(body);
            bodies.Add(body);
        }

        void Down(Body x)
        {
            if (x.gone || State == null) return;
            x.gone = true;   // he is on the ground now; the sim hears about it once
            if (x.raider) Turf.RaiderDown(State, x.block);
            else if (x.friendly) { var b = State.Block(x.block); if (b != null && b.guards > 0) b.guards--; }
            else Turf.GuardDown(State, x.block);
        }

        // ---- what you see ----

        static Mesh Ring(float r0, float r1, int seg)
        {
            var v = new Vector3[seg * 2]; var tri = new int[seg * 6];
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v[i * 2] = d * r0; v[i * 2 + 1] = d * r1;
                int n = (i + 1) % seg;
                tri[i * 6] = i * 2; tri[i * 6 + 1] = n * 2 + 1; tri[i * 6 + 2] = i * 2 + 1;
                tri[i * 6 + 3] = i * 2; tri[i * 6 + 4] = n * 2; tri[i * 6 + 5] = n * 2 + 1;
            }
            var m = new Mesh { name = "Turf Ring" }; m.vertices = v; m.triangles = tri; m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        /// <summary>The capture ring on the ground in the middle of each block, in the owner's color; it pulses while being taken.</summary>
        void Rings()
        {
            var fx = CombatFx.Instance;
            if (!ringsMade)
            {
                if (fx == null || fx.ringMat == null) return;
                ringsMade = true; ringMesh = Ring(world.zoneRadius - 0.35f, world.zoneRadius, 48);
                for (int i = 0; i < centers.Length; i++)
                {
                    var go = new GameObject($"Turf Ring {blockIds[i]}"); go.transform.SetParent(transform, false);
                    go.transform.position = new Vector3(centers[i].x, 0.07f, centers[i].z);
                    go.AddComponent<MeshFilter>().sharedMesh = ringMesh;
                    var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = fx.ringMat;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                    rings.Add(r);
                }
            }
            for (int i = 0; i < rings.Count; i++)
            {
                var b = State.Block(blockIds[i]); if (b == null || rings[i] == null) continue;
                var c = CrewColor(b.owner); float a = 0.75f;
                if (b.capT > 0 && b.capBy != null)
                {
                    c = Color.Lerp(c, CrewColor(b.capBy), Mathf.Clamp01(b.capT / world.captureTime));
                    a = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 6f));
                }
                else if (b.hasRaid && b.owner == Turf.You) { c = Color.Lerp(c, CrewColor(b.raid.crew), 0.5f + 0.5f * Mathf.Sin(Time.time * 8f)); a = 1f; }
                mpb.SetColor(BaseColor, new Color(c.r, c.g, c.b, a)); rings[i].SetPropertyBlock(mpb);
            }
        }

        /// <summary>Block owners on the radar: a dot in the crew's color on each block.</summary>
        void Paint()
        {
            if (radar == null || State == null) return;
            radar.turfAt = centers;
            radar.turfColor = blockIds.Select(id => CrewColor(State.Block(id).owner)).ToArray();
        }
    }
}
