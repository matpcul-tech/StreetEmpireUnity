// Every job after Unc's first, in the order of jobs.json (state.js startMission and missionStep in the web game):
// Unc, then Dee, Rook and Sal, each standing in the town, each handing you on to the next. One job is on offer at a
// time; talk to its contact to start it. The board speaks through MissionRunner (Present), so the HUD line, the
// yellow marker, the arrow and the radar that already follow Bag Back follow these too.
//
// What each kind asks, as this town can run it (the web game spread these over four districts; this is one):
//   collect  pick up N drops on the streets, back to the contact, on a clock
//   fetch    one far drop and back, on a clock
//   chop     bring a car of the kind to the chop shop
//   deliver  drive a car of the kind to the marker (it may have to stay in shape, and may bring heat)
//   take     take the marked block          own   hold N blocks at once
//   tail     follow a marked car for N seconds, not too close, not too far
//   chase    ram a marked car three times before it gets away
//   race     hit every marker round the loop in a car, on a clock
//   beat     knock out N of a crew          hit   knock out one named man
//   defend   get to your block and knock out the N who come for it
//   escape   you are wanted: lose the police
using System.Collections.Generic;
using System.Linq;
using StreetEmpire.City;
using StreetEmpire.Player;
using StreetEmpire.Sim;
using StreetEmpire.Sim.Data;
using StreetEmpire.Vehicles;
using UnityEngine;

namespace StreetEmpire.Game
{
    public class JobBoard : MonoBehaviour
    {
        public const string SaveKey = "jobs.next";
        public static JobBoard Instance { get; private set; }
        /// <summary>Tests that check Bag Back on its own turn the board off before the scene loads.</summary>
        public static bool AutoStart = true;

        [Header("Baked from jobs.json")]
        public List<MissionData> missions = new();
        public List<ContactData> contacts = new();
        [Header("Scene")]
        public MissionRunner runner;
        public Transform[] contactNodes = new Transform[0];   // same order as contacts
        public float talkRange = 3f, reach = 5f, carReach = 9f;

        /// <summary>Index in missions of the job on offer or running. Bag Back is 0 and belongs to MissionRunner.</summary>
        public int Next { get; private set; } = 1;
        public bool Running { get; private set; }
        /// <summary>Bag Back is done and the board has the HUD line, marker and arrow.</summary>
        public bool Owns => owns;
        public int Done { get; private set; }
        public int Fails { get; private set; }
        public bool scriptTalk;
        public MissionData Current => Next >= 0 && Next < missions.Count ? missions[Next] : null;
        public Vector3 Target => target;
        public JobCar Mark => car;
        public IReadOnlyList<Health> Hostiles => hostiles;

        bool owns; float timeLeft; int downs0, count, step, blockId = -1;
        Vector3 target; string targetName = "";
        float followed, lost, close, hitCool, alive; int hits;
        JobCar car; readonly List<Health> hostiles = new(); readonly List<Vector3> points = new();
        string[] carKinds; bool anyCar;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; Unhook(); }

        void Start()
        {
            if (!AutoStart) { enabled = false; return; }
            if (TurfWar.Persist)
            {
                int n = PlayerPrefs.GetInt(SaveKey, 0);
                if (n >= 1 && runner != null) { Next = Mathf.Min(n, missions.Count); runner.ForcePassed(); }
            }
        }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        Transform Node(string name) { int i = contacts.FindIndex(c => c.name == name); return i >= 0 && i < contactNodes.Length ? contactNodes[i] : null; }
        ContactData Contact(string name) => contacts.FirstOrDefault(c => c.name == name);

        void Update()
        {
            var s = GameSession.Instance; if (s == null || s.player == null || runner == null) return;
            if (!owns) { if (runner.Stage != MissionRunner.State.Passed) return; owns = true; Offer(); return; }
            var m = Current; if (m == null) return;
            var p = s.player.transform.position; var user = s.player.GetComponent<VehicleUser>();
            // a race, a fare or a courier run has the marker for now
            if (!Running && SideHustles.Instance != null && SideHustles.Instance.Busy) return;
            if (!Running)
            {
                var giver = runner.giver; if (giver == null) return;
                bool near = Flat(p, giver.position) < talkRange && (user == null || user.Car == null);
                runner.Present(MissionRunner.State.Offered, giver.position, near ? $"Press Use to talk to {m.by}" : $"Talk to {m.by}", m.by, false);
                if (scriptTalk || (near && PlayerControls.Instance.UsePressed)) Begin();
                scriptTalk = false;
                return;
            }
            if (s.Busts + s.Wasted != downs0) { Fail("You went down. Job failed."); return; }
            if (m.time > 0) { timeLeft -= Time.deltaTime; if (timeLeft <= 0) { Fail("Out of time. Job failed."); return; } }
            alive += Time.deltaTime;
            Tick(s, m, p, user != null ? user.Car : null);
        }

        void Offer()
        {
            Running = false;
            var m = Current;
            if (m == null) { runner.ForcePassed(); return; }
            var node = Node(m.by);
            if (node != null) { runner.giver = node; runner.contact = Contact(m.by) ?? runner.contact; }
            runner.Present(MissionRunner.State.Offered, runner.giver != null ? runner.giver.position : Vector3.zero, $"Talk to {m.by}", m.by, false);
        }

        public void Begin()
        {
            var s = GameSession.Instance; var m = Current; if (s == null || m == null || Running) return;
            Running = true; timeLeft = m.time; downs0 = s.Busts + s.Wasted;
            count = step = hits = 0; followed = lost = close = hitCool = alive = 0; blockId = -1; points.Clear(); hostiles.Clear(); carKinds = null; anyCar = false;
            s.Say(string.Join("\n", m.brief.Append(m.intro)), 6f);
            if (m.heat > 0) s.AddHeat(m.heat);
            var p = s.player.transform.position;
            switch (m.kind)
            {
                case "collect": target = StreetPoint(35, 90); break;
                case "fetch": target = StreetPoint(110, 400); break;
                case "chop": case "deliver":
                    carKinds = Kinds(m.car); anyCar = carKinds == null || !Exists(carKinds);
                    target = m.kind == "chop" ? ChopShop() : m.home && TurfWar.Instance != null && TurfWar.Instance.centers.Length > 0 ? NearestStreet(TurfWar.Instance.centers[0]) : StreetPoint(120, 400);
                    break;
                case "take": blockId = PickBlock(p, m.rival, false); break;
                case "tail": car = JobCar.Spawn(StreetPoint(25, 60), 8f); break;
                case "chase": car = JobCar.Spawn(StreetPoint(30, 70), 15f); break;
                case "race": Loop(p); break;
                case "beat": Hook(); blockId = PickBlock(p, false, true, m.crew); break;
                case "defend": blockId = OwnBlock(p); break;
                case "hit":
                    Hook(); blockId = PickBlock(p, false, true, m.crew);
                    target = blockId >= 0 ? NearestStreet(TurfWar.Instance.Center(blockId)) : StreetPoint(60, 140);
                    if (TurfWar.Instance != null) { var h = TurfWar.Instance.SpawnHostile(m.crew, target, true, string.IsNullOrEmpty(m.who) ? "The mark" : m.who, true); if (h != null) hostiles.Add(h); }
                    break;
            }
            if ((m.kind == "tail" || m.kind == "chase") && car == null) { Fail("No car to follow."); return; }
        }

        void Tick(GameSession s, MissionData m, Vector3 p, CarController driving)
        {
            var turf = TurfWar.Instance; string clock = m.time > 0 ? $"   {Mathf.CeilToInt(timeLeft)}s" : "";
            float near = driving != null ? carReach : reach;
            switch (m.kind)
            {
                case "collect":
                case "fetch":
                {
                    int need = m.kind == "fetch" ? 1 : Mathf.Max(1, m.need);
                    if (step == 0)
                    {
                        if (Flat(p, target) < near)
                        {
                            count++;
                            if (count >= need) { step = 1; s.Say($"Got it. Back to {m.by}.", 3f); }
                            else { target = StreetPoint(35, 90); s.Say($"{count} of {need}.", 2f); }
                        }
                        if (step == 0) { Go(target, "DROP", (need > 1 ? $"Pick up the drop. {count} of {need}." : "Pick up the drop at the yellow marker.") + clock); break; }
                    }
                    if (Flat(p, runner.giver.position) < reach) { Pass(); break; }
                    runner.Present(MissionRunner.State.ReturnBag, runner.giver.position, $"Bring it back to {m.by}." + clock, m.by, false);
                    break;
                }
                case "chop":
                case "deliver":
                {
                    bool right = driving != null && (anyCar || carKinds.Contains(driving.stats.id));
                    string label = anyCar ? "car" : m.car == "muscle" ? "muscle car (a coupe or a lowrider)" : m.car;
                    if (!right)
                    {
                        var parked = anyCar ? null : NearestParked(p, carKinds);
                        if (parked != null) Go(parked.transform.position, "CAR", $"Get in the {label} on the marker.");
                        else Go(p, "", driving != null ? $"Wrong car. Find a {label}." : $"Find a {label} and get in.");
                        break;
                    }
                    if (m.minHp > 0 && driving.stats.hp > 0 && driving.hp / driving.stats.hp < m.minHp) { Fail("The car is too beat up."); break; }
                    if (Flat(p, target) < carReach)
                    {
                        if (m.kind == "chop") { var user = s.player.GetComponent<VehicleUser>(); if (user != null) user.Exit(); Destroy(driving.gameObject); }
                        Pass(); break;
                    }
                    Go(target, m.kind == "chop" ? "CHOP SHOP" : "DROP", m.kind == "chop" ? "Drive it to the chop shop." : m.minHp > 0 ? "Drive it to the marker. Keep it clean." : "Drive it to the marker.");
                    break;
                }
                case "take":
                case "own":
                {
                    if (turf == null || turf.State == null) { Go(p, "", "Take a block."); break; }
                    int mine = turf.State.Owned(Turf.You);
                    if (m.kind == "own")
                    {
                        if (mine >= m.need) { Pass(); break; }
                        int nb = PickBlock(p, false, false);
                        Go(nb >= 0 ? turf.Center(nb) : p, "BLOCK", $"Hold {m.need} blocks at once. You hold {mine}.");
                        break;
                    }
                    var b = blockId >= 0 ? turf.State.Block(blockId) : null;
                    if (b == null || b.owner == Turf.You) { if (b != null || alive > 1f) Pass(); break; }
                    Go(turf.Center(blockId), "BLOCK", "Take the block on the marker.");
                    break;
                }
                case "tail":
                {
                    float d = Flat(p, car.Position);
                    if (d > 75f) { lost += Time.deltaTime; if (lost > 6f) { Fail("You lost the car."); break; } }
                    else if (d < 7f) { close += Time.deltaTime; if (close > 3.5f) { Fail("Too close. They made you."); break; } }
                    else { lost = Mathf.Max(0, lost - Time.deltaTime); close = Mathf.Max(0, close - Time.deltaTime); followed += Time.deltaTime; }
                    if (followed >= m.need) { Pass(); break; }
                    Go(car.Position, "MARK", (d > 75f ? "Too far. Catch up." : d < 7f ? "Too close. Drop back." : "Follow the marked car. Stay back.") + $"   {Mathf.FloorToInt(followed)} of {m.need}s");
                    break;
                }
                case "chase":
                {
                    float d = Flat(p, car.Position); hitCool -= Time.deltaTime;
                    if (driving != null && d < 5.4f && hitCool <= 0f) { hits++; hitCool = 1.2f; car.Bump(); s.Say(hits >= 3 ? "Stopped." : $"Hit {hits} of 3.", 1.5f); }
                    if (hits >= 3) { car.stopped = true; Pass(); break; }
                    if (d > 150f) { lost += Time.deltaTime; if (lost > 6f) { Fail("It got away."); break; } } else lost = 0;
                    if (alive > 150f) { Fail("It got away."); break; }
                    Go(car.Position, "MARK", driving == null ? "Get a car and stop the marked car." : $"Ram the marked car. {hits} of 3.");
                    break;
                }
                case "race":
                {
                    if (step >= points.Count) { Pass(); break; }
                    if (driving != null && Flat(p, points[step]) < 10f) { step++; if (step >= points.Count) { Pass(); break; } }
                    Go(points[step], "MARKER", (driving == null ? "Get a car. " : "") + $"Hit every marker. {step} of {points.Count}." + clock);
                    break;
                }
                case "beat":
                {
                    if (count >= m.need) { Pass(); break; }
                    string who = turf != null ? turf.CrewName(m.crew) : m.crew;
                    Go(blockId >= 0 && turf != null ? turf.Center(blockId) : p, who.ToUpperInvariant(), $"Knock out {m.need} {who}. {count} of {m.need}.");
                    break;
                }
                case "defend":
                {
                    Vector3 c = blockId >= 0 && turf != null ? turf.Center(blockId) : s.spawn;
                    if (step == 0)
                    {
                        if (Flat(p, c) < 14f)
                        {
                            step = 1;
                            for (int i = 0; i < Mathf.Max(1, m.need) && turf != null; i++)
                            {
                                float a = i * Mathf.PI * 2f / Mathf.Max(1, m.need) + 0.6f;
                                var h = turf.SpawnHostile(m.crew, p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 11f, false, $"{turf.CrewName(m.crew)} raider (job)", false);
                                if (h != null) hostiles.Add(h);
                            }
                            s.Say("Here they come.", 2.5f);
                            if (hostiles.Count == 0) { Pass(); break; }
                        }
                        else { Go(c, "YOUR BLOCK", "Get to your block on the marker."); break; }
                    }
                    int left = hostiles.Count(h => h != null && !h.Down);
                    if (left == 0) { Pass(); break; }
                    Go(c, "HOLD", $"Hold the block. {left} left.");
                    break;
                }
                case "escape":
                    if (s.heat <= 0.01f && alive > 1f) { Pass(); break; }
                    Go(p, "", $"You are wanted. Lose the police.   {s.Stars} star" + (s.Stars == 1 ? "" : "s"));
                    break;
                case "hit":
                {
                    var h = hostiles.Count > 0 ? hostiles[0] : null;
                    if (h == null || h.Down) { Pass(); break; }
                    Go(h.transform.position, "MARK", $"Knock out the {(string.IsNullOrEmpty(m.who) ? "mark" : m.who)}.");
                    break;
                }
                default:
                    Pass(); break;   // a kind this town cannot run is not a wall in front of the rest
            }
        }

        void Go(Vector3 at, string name, string text)
        {
            target = at; targetName = name;
            runner.Present(MissionRunner.State.GetBag, at, text, name, name.Length > 0);
        }

        void Pass()
        {
            var s = GameSession.Instance; var m = Current; var c = Contact(m.by);
            s.cash += m.reward; s.rep += m.rep; Done++;
            Cleanup();
            string said = c != null && c.pass.Count > 0 ? $"\n{c.name}: \"{c.pass[m.index % c.pass.Count]}\"" : "";
            Next++;
            if (TurfWar.Persist) { PlayerPrefs.SetInt(SaveKey, Next); PlayerPrefs.Save(); }
            var n = Current;
            string hand = n != null && !string.IsNullOrEmpty(n.handoff) ? "\n" + n.handoff : n == null ? "\nEvery job is done." : "";
            s.Say($"Job done: {m.title}. +${m.reward}{said}{hand}", 7f);
            Offer();
        }

        void Fail(string why)
        {
            var s = GameSession.Instance; var m = Current; var c = m != null ? Contact(m.by) : null;
            Cleanup(); Fails++;
            string said = c != null && c.fail.Count > 0 ? $" {c.name}: \"{c.fail[m.index % c.fail.Count]}\"" : "";
            s.Say(why + said, 5f);
            Offer();
        }

        void Cleanup()
        {
            Running = false; Unhook();
            if (car != null) Destroy(car.gameObject, car.stopped ? 12f : 0f); car = null;
            foreach (var h in hostiles) if (h != null) Destroy(h.gameObject, 20f);
            hostiles.Clear();
        }

        /// <summary>Put a given job on offer (tests, and trying one job without playing up to it).</summary>
        public void SkipTo(int index) { Cleanup(); if (runner != null) runner.ForcePassed(); Next = Mathf.Clamp(index, 1, missions.Count); owns = true; Offer(); }

        /// <summary>Start the jobs over (a new game).</summary>
        public void NewGame() { Cleanup(); Next = 1; Done = 0; PlayerPrefs.DeleteKey(SaveKey); if (owns) Offer(); }

        // ---- knockouts ----
        bool hooked;
        void Hook() { if (!hooked && TurfWar.Instance != null) { TurfWar.Instance.CrewDown += OnCrewDown; hooked = true; } }
        void Unhook() { if (hooked && TurfWar.Instance != null) TurfWar.Instance.CrewDown -= OnCrewDown; hooked = false; }
        void OnCrewDown(string crew, GameObject who)
        {
            var m = Current; if (!Running || m == null || m.kind != "beat") return;
            if (string.IsNullOrEmpty(m.crew) || crew == m.crew || blockId < 0) count++;
        }

        // ---- places ----
        Vector3 StreetPoint(float min, float max)
        {
            var s = GameSession.Instance;
            return s.SpawnPoint(out var at, min, max, 2.6f) ? at : s.player.transform.position;
        }

        /// <summary>The spot on a street center line nearest to a place (a block's middle, a building).</summary>
        public static Vector3 NearestStreet(Vector3 q)
        {
            var s = GameSession.Instance; if (s == null || s.streetX == null || s.streetX.Length == 0 || s.streetZ.Length == 0) return q;
            float nx = s.streetX.OrderBy(x => Mathf.Abs(x - q.x)).First(), nz = s.streetZ.OrderBy(z => Mathf.Abs(z - q.z)).First();
            return Mathf.Abs(nx - q.x) <= Mathf.Abs(nz - q.z) ? new Vector3(nx, 0, q.z) : new Vector3(q.x, 0, nz);
        }

        Vector3 ChopShop()
        {
            var shop = FindObjectsByType<BuildingMarker>(FindObjectsSortMode.None).FirstOrDefault(b => b.model != null && b.model.Contains("chop"));
            return shop != null ? NearestStreet(shop.transform.position) : StreetPoint(110, 400);
        }

        /// <summary>The nearest block that is not yours: a rival's (of one crew, if named), or anyone's.</summary>
        int PickBlock(Vector3 p, bool rival, bool crewOnly, string crew = null)
        {
            var turf = TurfWar.Instance; if (turf == null || turf.State == null) return -1;
            int best = -1; float bd = float.MaxValue;
            foreach (int id in turf.blockIds)
            {
                var b = turf.State.Block(id); if (b == null || b.owner == Turf.You) continue;
                if (crewOnly && b.owner != crew) continue;
                if (rival && System.Array.IndexOf(Turf.Rivals, b.owner) < 0) continue;
                float d = Flat(p, turf.Center(id)); if (d < bd) { bd = d; best = id; }
            }
            // no rival left to take: any block that is not yours
            return best < 0 && rival ? PickBlock(p, false, false) : best;
        }

        int OwnBlock(Vector3 p)
        {
            var turf = TurfWar.Instance; if (turf == null || turf.State == null) return -1;
            int best = -1; float bd = float.MaxValue;
            foreach (int id in turf.blockIds)
            {
                var b = turf.State.Block(id); if (b == null || b.owner != Turf.You) continue;
                float d = Flat(p, turf.Center(id)); if (d < bd) { bd = d; best = id; }
            }
            return best >= 0 ? best : turf.blockIds.Length > 0 ? turf.blockIds[0] : -1;
        }

        /// <summary>Six markers round a loop of streets about the middle of town, starting from the corner nearest the player.</summary>
        void Loop(Vector3 p)
        {
            var s = GameSession.Instance; points.Clear();
            var xs = s.streetX.OrderBy(v => v).ToArray(); var zs = s.streetZ.OrderBy(v => v).ToArray();
            if (xs.Length < 2 || zs.Length < 2) return;
            float x1 = xs[Mathf.Max(0, xs.Length / 2 - 1)], x2 = xs[Mathf.Min(xs.Length - 1, xs.Length / 2 + 1)];
            float z1 = zs[Mathf.Max(0, zs.Length / 2 - 1)], z2 = zs[Mathf.Min(zs.Length - 1, zs.Length / 2 + 1)], zm = (z1 + z2) / 2f;
            var ring = new List<Vector3> { new(x1, 0, z1), new(x1, 0, zm), new(x1, 0, z2), new(x2, 0, z2), new(x2, 0, zm), new(x2, 0, z1) };
            int start = 0; for (int i = 1; i < ring.Count; i++) if (Flat(p, ring[i]) < Flat(p, ring[start])) start = i;
            for (int i = 0; i < ring.Count; i++) points.Add(ring[(start + i) % ring.Count]);
        }

        // ---- cars ----
        static string[] Kinds(string car)
        {
            switch (car)
            {
                case "muscle": return new[] { "coupe", "lowrider" };
                case "lowrider": return new[] { "lowrider" };
                case "van": return new[] { "van", "minivan" };
                default: return null;
            }
        }

        static bool Exists(string[] kinds) =>
            FindObjectsByType<PropMarker>(FindObjectsSortMode.None).Any(m => m.kind == "parked car" && kinds.Contains(m.model))
            || FindObjectsByType<CarController>(FindObjectsSortMode.None).Any(c => c.stats != null && kinds.Contains(c.stats.id));

        static PropMarker NearestParked(Vector3 p, string[] kinds)
        {
            PropMarker best = null; float bd = float.MaxValue;
            foreach (var m in FindObjectsByType<PropMarker>(FindObjectsSortMode.None))
            {
                if (m.kind != "parked car" || !kinds.Contains(m.model)) continue;
                float d = Flat(p, m.transform.position); if (d < bd) { bd = d; best = m; }
            }
            return best;
        }
    }
}
