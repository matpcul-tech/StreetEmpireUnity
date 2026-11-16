// The simulation entry points. Mirrors state.js: CreateState() is createState(), Step() is step().
// Only the parts with no physics or navigation yet are filled in. Every TODO names the JS function to port.
using System;
using System.Linq;
using StreetEmpire.Sim.Data;

namespace StreetEmpire.Sim
{
    public static class Simulation
    {
        public static readonly string[] PlayableCrews = { "neutral", "player", "reds", "blues", "golds" };

        public static double R(SimState s) => SimRng.Next(s);

        /// <summary>Port of createState(): city grid, crew corners, starting cash. Units, cars and pickups are TODO.</summary>
        public static SimState CreateState(GameData data, long seed = 1, SimConfig cfg = null)
        {
            var w = data.World;
            var s = new SimState { seed = seed, rng = SimRng.Seed(seed), cfg = cfg ?? new SimConfig() };
            s.calls.cd = data.Jobs.streetCalls.first;

            // you start with one corner block; each rival crew holds a 2 x 2 corner of its own
            int L = w.grid - 1;
            var starts = new System.Collections.Generic.Dictionary<(int, int), string> { [(0, 0)] = "player" };
            foreach (var (crew, cx, cz) in new[] { ("reds", L, 0), ("blues", 0, L), ("golds", L, L) })
                for (int ox = 0; ox <= 1; ox++) for (int oz = 0; oz <= 1; oz++) starts[(Math.Abs(cx - ox), Math.Abs(cz - oz))] = crew;

            for (int gz = 0; gz < w.grid; gz++)
                for (int gx = 0; gx < w.grid; gx++)
                {
                    int id = gz * w.grid + gx;
                    s.blocks.Add(new Block
                    {
                        id = id, gx = gx, gz = gz,
                        owner = starts.TryGetValue((gx, gz), out var o) ? o : "neutral",
                        hustle = w.hustles[id % w.hustles.Count],
                        rate = 3 + ((id * 7) % 4),
                    });
                }

            foreach (var k in PlayableCrews)
                s.crews[k] = new CrewState { id = k, cash = k == "player" ? 150 : 140, rep = 0, aiT = 10 + (float)R(s) * 10, alive = true };

            // TODO port: spawnUnit defenders per block, the player entity, ensureContacts,
            // parked cars, addBikes, alley pickups, spawnTraffic, spawnPed (same rng order as JS).
            return s;
        }

        /// <summary>Port target of step(s, input, dt). Order matters for rng parity with the JS.</summary>
        public static void Step(SimState s, GameData data, PlayerInput input, float dt)
        {
            if (s.over != null) return;
            s.t += dt;
            // TODO port in this order: playerStep, carsStep, aiStep, captureStep, economyStep,
            // policeStep (with heliStep, roadblockStep), missionStep, sideStep, callsStep, cleanup.
            var p = Player(s);
            if (p != null) DecayHeat(s, data, p, dt);
        }

        public static Entity Player(SimState s) => s.ents.FirstOrDefault(e => e.kind == EntKind.Player);

        /// <summary>stars(p) in state.js.</summary>
        public static int Stars(float heat) => heat < 10 ? 0 : Math.Min(5, 1 + (int)Math.Floor((heat - 10) / 20f));

        /// <summary>addHeat(s, p, n) in state.js.</summary>
        public static void AddHeat(Entity p, float n, PoliceFile police)
        {
            if (p == null || p.kind != EntKind.Player) return;
            p.heat = Math.Min(police.maxHeat, p.heat + n); p.calmT = 0;
        }

        public static float HeatFor(PoliceFile police, string sourceId) => police.heatSources.First(h => h.id == sourceId).heat;

        public static PoliceLevelData Level(PoliceFile police, float heat)
        {
            int st = Stars(heat);
            return st == 0 ? null : police.levels.First(l => l.stars == st);
        }

        /// <summary>The calm decay at the end of the JS player update.</summary>
        static void DecayHeat(SimState s, GameData data, Entity p, float dt)
        {
            var d = data.Police.decay; float r2 = d.copNearRadius * d.copNearRadius;
            p.calmT += dt;
            bool copNear = s.ents.Any(e => e.kind == EntKind.Cop && e.hp > 0 && (e.x - p.x) * (e.x - p.x) + (e.z - p.z) * (e.z - p.z) < r2);
            if (p.heat > 0 && p.calmT > d.calmAfter && !copNear) p.heat = Math.Max(0, p.heat - d.calmRate * dt);
            if (p.heat > 0 && p.calmT > d.longCalmAfter) p.heat = Math.Max(0, p.heat - d.longCalmRate * dt);
        }

        /// <summary>onPlayerDown(): busted with heat, wasted without. Returns cash lost.</summary>
        public static int CashLostOnDown(SimState s, PoliceFile police, Entity p)
        {
            bool busted = p.heat >= police.bustedHeat;
            var cr = s.crews[p.crew];
            int lost = (int)Math.Floor(cr.cash * (busted ? police.bailBusted : police.bailWasted));
            cr.cash -= lost; return lost;
        }

        /// <summary>blockCenter(b) in source space.</summary>
        public static (float x, float z) BlockCenter(WorldFile w, Block b)
        {
            float off = (w.grid - 1) / 2f;
            return ((b.gx - off) * w.pitch, (b.gz - off) * w.pitch);
        }
    }

    /// <summary>One tick of player intent. Same shape for local play and for the network later.</summary>
    public struct PlayerInput
    {
        public float moveX, moveZ, aimYaw;
        public bool attack, block, dodge, jump, use, enterCar, sprint;
    }

    /// <summary>The seam from net.js LocalSession: the client only talks to a session, never to SimState directly.</summary>
    public interface ISession
    {
        SimState State { get; }
        void Send(PlayerInput input);
        void Tick(float dt);
    }

    public class LocalSession : ISession
    {
        readonly GameData data; PlayerInput last;
        public SimState State { get; }
        public LocalSession(GameData data, long seed) { this.data = data; State = Simulation.CreateState(data, seed); }
        public void Send(PlayerInput input) => last = input;
        public void Tick(float dt) => Simulation.Step(State, data, last, dt);
    }
}
