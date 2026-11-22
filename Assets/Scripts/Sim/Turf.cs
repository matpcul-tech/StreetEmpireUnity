// The turf loop for one district: take a block, earn from it, get raided, defend it, spend the cash.
// Rules and numbers are the web game's (state.js): the capture zone and timer (ZONE_R 7 m, CAPTURE_TIME 6 s),
// stashRate (rate x (0.4 + 0.2 per guard, up to 3) x 2 when built up), the pocketed stash, recruit 100,
// arm 80, build 200, and aiThink (a think every 9 to 15 s: bank the stash, pay 60 a guard up to two a block,
// then with 150 or more and a 70% roll send 2 to 4 at a neighbor for 50 each; your blocks get a RAID_GATHER
// warning of 10 s first). The pace of the rivals is set further down (see the note on the constants).
// What differs from the web: the web walks every guard and raider as a body all over the city. Here a block
// keeps a guard count, and the bodies only exist where the player is (TurfWar spawns them). A fight nobody is
// standing at is settled by rolls, one exchange every few seconds.
// Pure C#, no UnityEngine: the same code runs in the game, on a server and in EditMode tests.
using System;
using System.Collections.Generic;
using System.Linq;
using StreetEmpire.Sim.Data;

namespace StreetEmpire.Sim
{
    [Serializable]
    public class TurfRaid
    {
        public string crew;       // who is coming
        public int from;          // the block they set out from
        public int n;             // raiders still standing
        public float eta;         // seconds until they hit (your blocks get a warning; others are hit at once)
        public float fightT;      // time since the last exchange
        public bool hit;          // the "hitting now" event has gone out
    }

    [Serializable]
    public class TurfBlock
    {
        public int id, gx, gz;
        public string owner;      // crew id
        public string hustle;
        public int rate;
        public int guards;        // owner's guards on post, 0 to 3
        public bool armed;        // bats and more health for this block's guards (upgrade)
        public bool built;        // built up once: earns buildBoost times as much, lost when the block falls
        public float stash;       // hustle cash piled up on the block
        public string capBy;      // crew capturing it, or null
        public float capT;
        public float regrowT;     // locals drifting back onto an empty neutral block
        public bool hasRaid;      // JsonUtility cannot keep a null object, so the flag says whether raid counts
        public TurfRaid raid = new TurfRaid();
    }

    [Serializable]
    public class TurfCrew { public string id; public float cash; public int rep; public float aiT; public float raidT; public bool alive = true; }

    [Serializable]
    public class TurfEvent { public string type; public int block; public string crew; public string from; public int amount; }

    [Serializable]
    public class TurfState
    {
        public float t;
        public int grid;
        public ulong rng;
        public List<TurfBlock> blocks = new List<TurfBlock>();
        public List<TurfCrew> crews = new List<TurfCrew>();
        public string over = "";               // "" while playing, "win" once every block is yours
        public int taken, lost, defended;      // the player's tally
        [NonSerialized] public List<TurfEvent> events = new List<TurfEvent>();   // read and cleared by the client

        public TurfBlock Block(int id) { foreach (var b in blocks) if (b.id == id) return b; return null; }
        public TurfCrew Crew(string id) { foreach (var c in crews) if (c.id == id) return c; return null; }
        public int Owned(string crew) { int n = 0; foreach (var b in blocks) if (b.owner == crew) n++; return n; }
    }

    /// <summary>Where the player is this tick: the block whose capture zone he stands in, and the block he is on (each -1 for none).</summary>
    public struct TurfPresence { public int zoneBlock; public int nearBlock; }

    public static class Turf
    {
        public const string You = "player", Locals = "neutral";
        public const int MaxGuards = 3;
        public const float GuardCost = 60, RaiderCost = 50, RaidCash = 150, RaidChance = 0.7f;
        // Pace, set by running 300 seeded games of the rules with the player away: a rival crew rests 75 to 120 s
        // between raids, a raid on someone else's block takes 20 s to walk there, a fight nobody is at trades one
        // blow every 6 s and the defenders win 60% of them (70% with bats). With these a block left on one guard
        // falls to the first raid after the grace, and one with three armed guards holds about half the time
        // over twenty minutes with nobody home.
        public const float AwayExchange = 6f, HelpExchange = 5f, RegrowSeconds = 50f, GraceSeconds = 300f;
        public const float RaidRest = 75f, RaidRestSpread = 45f, TravelSeconds = 20f, DefenderEdge = 0.6f, BatEdge = 0.1f;
        public static readonly string[] Rivals = { "reds", "blues", "golds" };

        static double R(TurfState s)
        {
            s.rng = (s.rng * 1664525UL + 1013904223UL) % 4294967296UL;
            return s.rng / 4294967296.0;
        }

        static void Emit(TurfState s, string type, TurfBlock b, string crew, string from = null, int amount = 0)
        {
            if (s.events == null) s.events = new List<TurfEvent>();
            s.events.Add(new TurfEvent { type = type, block = b != null ? b.id : -1, crew = crew, from = from, amount = amount });
        }

        /// <summary>
        /// A district of the city grid as its own turf map. The player starts on the first id; each rival starts on one
        /// block of its own (rivalStarts, crew id to block id); the rest belong to the Locals.
        /// </summary>
        public static TurfState Create(WorldFile w, IList<int> blockIds, IDictionary<string, int> rivalStarts, long seed = 1, float playerCash = 150)
        {
            var s = new TurfState { grid = w.grid, rng = SimRng.Seed(seed) };
            foreach (int id in blockIds)
            {
                string owner = Locals;
                if (id == blockIds[0]) owner = You;
                else if (rivalStarts != null) foreach (var kv in rivalStarts) if (kv.Value == id) owner = kv.Key;
                s.blocks.Add(new TurfBlock
                {
                    id = id, gx = id % w.grid, gz = id / w.grid, owner = owner,
                    hustle = w.hustles[id % w.hustles.Count], rate = 3 + ((id * 7) % 4),
                    guards = owner == You ? 1 : 2,
                });
            }
            s.crews.Add(new TurfCrew { id = Locals });
            s.crews.Add(new TurfCrew { id = You, cash = playerCash });
            foreach (var k in Rivals)
                s.crews.Add(new TurfCrew { id = k, cash = 140, aiT = 10 + (float)R(s) * 10, alive = s.Owned(k) > 0 });
            return s;
        }

        /// <summary>The Eastside slice: nine blocks, you on block 0, a rival on each of the other three corners.</summary>
        public static TurfState CreateEastside(WorldFile w, long seed = 1, float playerCash = 150) =>
            Create(w, new[] { 0, 1, 2, 9, 10, 11, 18, 19, 20 }, new Dictionary<string, int> { ["reds"] = 20, ["blues"] = 2, ["golds"] = 18 }, seed, playerCash);

        /// <summary>stashRate(s, b): dollars a second.</summary>
        public static float StashRate(WorldFile w, TurfBlock b) =>
            b.rate * (0.4f + 0.2f * Math.Min(MaxGuards, b.guards)) * (b.built ? w.buildBoost : 1f);

        public static IEnumerable<TurfBlock> Neighbors(TurfState s, TurfBlock b) =>
            s.blocks.Where(n => Math.Abs(n.gx - b.gx) + Math.Abs(n.gz - b.gz) == 1);

        public static void Step(TurfState s, WorldFile w, TurfPresence at, float dt)
        {
            if (s.events == null) s.events = new List<TurfEvent>();
            s.t += dt;

            // the hustle pays onto the block; standing in the ring of your own block pockets it
            foreach (var b in s.blocks) if (b.owner != Locals) b.stash += StashRate(w, b) * dt;
            var here = at.zoneBlock >= 0 ? s.Block(at.zoneBlock) : null;
            if (here != null && here.owner == You && here.stash >= 1f)
            {
                int amt = (int)Math.Floor(here.stash);
                s.Crew(You).cash += here.stash; here.stash = 0;
                Emit(s, "stash", here, You, null, amt);
            }

            // the player takes a block: nobody of the owner's left on it, and he holds the ring for captureTime
            foreach (var b in s.blocks)
            {
                bool raidersOnIt = b.hasRaid && b.raid.eta <= 0 && b.raid.n > 0;
                bool claiming = here == b && b.owner != You && b.guards <= 0 && !raidersOnIt;
                if (claiming)
                {
                    if (b.capBy != You) { b.capBy = You; b.capT = 0; }
                    b.capT += dt;
                    if (b.capT >= w.captureTime) Capture(s, b, You, 1);
                }
                else if (b.capBy == You && b.capT > 0)
                {
                    b.capT = Math.Max(0, b.capT - dt * 2);
                    if (b.capT <= 0) b.capBy = null;
                }
            }

            foreach (var k in Rivals) Think(s, w, k, dt);
            foreach (var b in s.blocks) if (b.hasRaid) RaidStep(s, w, b, at, dt);

            // locals drift back onto a neutral block that has been cleared and left
            foreach (var b in s.blocks)
            {
                if (b.owner != Locals || b.guards >= 2 || here == b || b.hasRaid) { b.regrowT = 0; continue; }
                b.regrowT += dt;
                if (b.regrowT >= RegrowSeconds) { b.regrowT = 0; b.guards++; }
            }

            foreach (var c in s.crews) if (c.id != Locals) c.alive = s.Owned(c.id) > 0;
            if (s.over == "" && s.Owned(You) == s.blocks.Count) { s.over = "win"; Emit(s, "win", null, You); }
        }

        static void Capture(TurfState s, TurfBlock b, string crew, int holdWith)
        {
            string prev = b.owner;
            b.owner = crew; b.capBy = null; b.capT = 0; b.hasRaid = false; b.raid = new TurfRaid();
            b.armed = false; b.built = false; b.regrowT = 0;
            b.guards = Math.Max(1, Math.Min(MaxGuards, holdWith));
            s.Crew(crew).rep += 25;
            int loot = (int)Math.Floor(b.stash);
            if (b.stash > 0) { s.Crew(crew).cash += b.stash; b.stash = 0; }
            if (crew == You) s.taken++;
            if (prev == You) s.lost++;
            Emit(s, "capture", b, crew, prev, loot);
        }

        /// <summary>aiThink(s, k, dt).</summary>
        static void Think(TurfState s, WorldFile w, string k, float dt)
        {
            var cr = s.Crew(k);
            var own = s.blocks.Where(b => b.owner == k).ToList();
            cr.alive = own.Count > 0;
            if (!cr.alive) return;
            cr.aiT -= dt;
            if (cr.aiT > 0) return;
            cr.aiT = 9 + (float)R(s) * 6;
            foreach (var b in own) if (b.stash > 0) { cr.cash += b.stash; b.stash = 0; }
            foreach (var b in own)
                if (b.guards < 2 && cr.cash >= GuardCost) { cr.cash -= GuardCost; b.guards++; }
            // one raid party out at a time
            if (s.blocks.Any(b => b.hasRaid && b.raid.crew == k)) return;
            if (cr.cash < RaidCash || s.t < cr.raidT || R(s) >= RaidChance) return;
            TurfBlock bestFrom = null, bestTo = null; double best = 1e9;
            foreach (var b in own)
                foreach (var n in Neighbors(s, b))
                {
                    if (n.owner == k || n.hasRaid) continue;
                    // a new player is left alone until he has taken a block of his own, or five minutes in
                    if (n.owner == You && s.taken == 0 && s.t < GraceSeconds) continue;
                    // fewest defenders first; your blocks and built-up blocks pull a little harder
                    double score = n.guards + (n.owner == You ? -0.5 : 0) + (n.built ? -1.5 : 0) + R(s);
                    if (score < best) { best = score; bestFrom = b; bestTo = n; }
                }
            if (bestTo == null) return;
            int count = Math.Min(4, (int)Math.Floor(cr.cash / RaiderCost));
            if (count < 2) return;
            cr.cash -= count * RaiderCost;
            cr.raidT = s.t + RaidRest + (float)R(s) * RaidRestSpread;
            bool onYou = bestTo.owner == You;
            bestTo.hasRaid = true;
            bestTo.raid = new TurfRaid { crew = k, from = bestFrom.id, n = count, eta = onYou ? w.raidGather : TravelSeconds };
            Emit(s, onYou ? "raid" : "move", bestTo, k, bestTo.owner, count);
        }

        static void RaidStep(TurfState s, WorldFile w, TurfBlock b, TurfPresence at, float dt)
        {
            var r = b.raid;
            if (b.owner == r.crew || r.n <= 0) { EndRaid(s, b, r.n <= 0); return; }
            if (r.eta > 0) { r.eta -= dt; if (r.eta > 0) return; }
            if (!r.hit) { r.hit = true; if (b.owner == You) Emit(s, "raidGo", b, r.crew, b.owner, r.n); }

            // the player is standing on his own block: the raiders are bodies he fights (TurfWar reports each one
            // that goes down); his guards drop one every few seconds and now and then lose one of their own
            bool playerDefends = b.owner == You && at.nearBlock == b.id;
            r.fightT += dt;
            float every = playerDefends ? HelpExchange : AwayExchange;
            if (r.fightT >= every && b.guards > 0)
            {
                r.fightT = 0;
                double edge = DefenderEdge + (b.armed ? BatEdge : 0f);
                if (R(s) < edge) r.n--;
                else if (!playerDefends || R(s) < 0.5) b.guards--;
                if (r.n <= 0) { EndRaid(s, b, true); return; }
            }
            if (b.guards > 0 || playerDefends) { if (b.capBy == r.crew) { b.capBy = null; b.capT = 0; } return; }
            // nobody left on it: the raiders hold the ring
            if (b.capBy != r.crew) { b.capBy = r.crew; b.capT = 0; }
            b.capT += dt;
            if (b.capT >= w.captureTime) Capture(s, b, r.crew, r.n);
        }

        static void EndRaid(TurfState s, TurfBlock b, bool beaten)
        {
            var r = b.raid; string crew = r.crew;
            b.hasRaid = false; b.raid = new TurfRaid();
            if (b.capBy == crew) { b.capBy = null; b.capT = 0; }
            if (!beaten) return;
            if (b.owner == You) { s.defended++; s.Crew(You).rep += 10; }
            Emit(s, "raidBeaten", b, crew, b.owner);
        }

        // ---- what the client reports ----

        /// <summary>One of a block's guards went down to the player.</summary>
        public static void GuardDown(TurfState s, int blockId)
        {
            var b = s.Block(blockId); if (b == null || b.guards <= 0) return;
            b.guards--;
            if (b.guards == 0) Emit(s, "cleared", b, b.owner);
        }

        /// <summary>One of the raiders on a block went down to the player.</summary>
        public static void RaiderDown(TurfState s, int blockId)
        {
            var b = s.Block(blockId); if (b == null || !b.hasRaid || b.raid.n <= 0) return;
            b.raid.n--;
            if (b.raid.n <= 0) EndRaid(s, b, true);
        }

        // ---- what the player buys, standing on a block he holds ----

        /// <summary>recruit: one more guard on this block, up to three.</summary>
        public static bool Recruit(TurfState s, WorldFile w, int blockId)
        {
            var b = s.Block(blockId); var c = s.Crew(You);
            if (b == null || b.owner != You || b.guards >= MaxGuards || c.cash < w.recruitCost) return false;
            c.cash -= w.recruitCost; b.guards++; Emit(s, "recruit", b, You, null, w.recruitCost); return true;
        }

        /// <summary>upgrade: bats and more health for this block's guards.</summary>
        public static bool Arm(TurfState s, WorldFile w, int blockId)
        {
            var b = s.Block(blockId); var c = s.Crew(You);
            if (b == null || b.owner != You || b.armed || c.cash < w.upgradeCost) return false;
            c.cash -= w.upgradeCost; b.armed = true; Emit(s, "arm", b, You, null, w.upgradeCost); return true;
        }

        /// <summary>build: the hustle earns buildBoost times as much until the block falls.</summary>
        public static bool Build(TurfState s, WorldFile w, int blockId)
        {
            var b = s.Block(blockId); var c = s.Crew(You);
            if (b == null || b.owner != You || b.built || c.cash < w.buildCost) return false;
            c.cash -= w.buildCost; b.built = true; Emit(s, "build", b, You, null, w.buildCost); return true;
        }

        /// <summary>The next thing worth buying on a block you hold: guards first, then bats, then the build up. Null when it has everything.</summary>
        public static string NextBuy(TurfBlock b)
        {
            if (b == null || b.owner != You) return null;
            if (b.guards < 2) return "recruit";
            if (!b.armed) return "arm";
            if (b.guards < MaxGuards) return "recruit";
            if (!b.built) return "build";
            return null;
        }

        public static int Cost(WorldFile w, string buy) => buy == "recruit" ? w.recruitCost : buy == "arm" ? w.upgradeCost : buy == "build" ? w.buildCost : 0;
    }
}
