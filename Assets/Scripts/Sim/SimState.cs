// C# skeleton of the simulation state built by createState() in state.js.
// Pure data, no UnityEngine, so the same code runs in the client, a headless server and tests.
using System.Collections.Generic;

namespace StreetEmpire.Sim
{
    public class SimConfig { public int traffic = 10; public int peds = 14; }

    public class Block
    {
        public int id, gx, gz;
        public string owner;      // crew id
        public string hustle;
        public int rate;
        public string capBy;      // crew capturing it, or null
        public float capT;
        public float stash;
        public int level;         // upgrades, see UPGRADE_COST / UPGRADE_HP
        public bool built;        // BUILD_COST front business, BUILD_BOOST
    }

    public class CrewState { public string id; public float cash; public int rep; public float aiT; public bool alive = true; }

    public enum EntKind { Player, Npc, Ped, Cop, Giver }

    public class Entity
    {
        public int id; public EntKind kind; public string crew;
        public float x, z, ry;
        public float hp, max;
        public string weapon = "fists"; public int ammo; public int shells;
        public bool hasBat, hasKnux, blocking;
        public float cd, hitT, stun, swing, windup, kx, kz, jumpT;
        public int combo;
        public int? car;          // car id while driving or riding
        public string role;       // defender, raider, follower, contact slot
        public int? homeBlock;
        // player only
        public float heat, calmT, armor, downT, dodgeT, dodgeCd;
        public int busted;
    }

    public class Car
    {
        public int id; public string type;    // CAR_TYPES or BIKE_MODELS id
        public float x, z, ry, v, hp, max;
        public bool ai, cops, siren, wrecked, exploded, byP;
        public float burn; public int paint;
        public int? driver;
    }

    public class Pickup { public string id; public string type; public float x, z, respawnT; }

    public class MissionState
    {
        public int index; public string kind; public float t; public string stage;
        public int? carId; public string carType; public float minHp; public int heat;
    }

    public class CallsState { public float cd; public int n; public object active; public float grace; }

    public class SimEvent { public string type; public float x, z; public int by; public string text; }

    public class SimState
    {
        public float t;
        public int nextId = 1;
        public long seed;
        public ulong rng;
        public SimConfig cfg = new();
        public List<Block> blocks = new();
        public Dictionary<string, CrewState> crews = new();
        public List<Entity> ents = new();
        public List<Car> cars = new();
        public List<Pickup> pickups = new();
        public List<SimEvent> events = new();   // gameplay log, read by UI
        public List<SimEvent> fx = new();       // one frame effects, read by the client then cleared
        public string over;                      // null while playing, else "win" or "lose"
        public MissionState mission;
        public int missionIdx;
        public Dictionary<string, float> stats = new();
        public CallsState calls = new();
        // police
        public float policeT; public int starsSeen; public float heliCd;
        // owned safehouses (HOUSES indexes)
        public List<int> houses = new();
    }
}
