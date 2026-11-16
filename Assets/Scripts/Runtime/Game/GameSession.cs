// The single-player session for the Eastside slice: cash and rep, the player's heat and stars,
// police dispatch, and what happens when the player goes down (busted with heat, wasted without).
// Rules and numbers come from police.json and state.js (addHeat, stars, policeStep, onPlayerDown, respawnPlayer).
using System.Collections.Generic;
using System.Linq;
using StreetEmpire.City;
using StreetEmpire.Player;
using StreetEmpire.Sim;
using StreetEmpire.Sim.Data;
using UnityEngine;

namespace StreetEmpire.Game
{
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [Header("Baked from Assets/Data when the scene is built")]
        public PoliceFile police;
        public float cash = 150; public int rep;
        public Vector3 spawn; public float spawnYaw;
        public float[] streetX, streetZ;      // Unity-space street center lines of the district (x = const, z = const)
        public Vector2 spanZ, spanX;          // how far the x lines run in z, and the z lines run in x
        public GameObject[] copTemplates;     // one per cop model, taken in turn
        // M5: officers arrive in a cruiser that spawns spawnRingMin to spawnRingMax (60 to 110 m) out, like the JS
        public GameObject cruiserTemplate;

        [Header("Scene")]
        public PlayerMotor player; public Health playerHealth;

        public float heat, calmT;
        public int Stars => Simulation.Stars(heat);
        public bool IsDown => downT > 0;
        public int Busts { get; private set; }
        public int Wasted { get; private set; }
        public float LastBail { get; private set; }
        public string Message { get; private set; } = "";
        public readonly List<Cop> cops = new();
        public readonly List<PoliceCruiser> cruisers = new();
        public int CruisersSent { get; private set; }

        float downT, policeT, msgT; bool downBusted; int copLook;

        void Awake()
        {
            Instance = this;
            if (playerHealth != null) playerHealth.KnockedOut += _ => OnPlayerDown();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public float HeatFor(string id) => police.heatSources.First(h => h.id == id).heat;

        /// <summary>addHeat(s, p, n): capped at maxHeat, and any new heat resets the calm timer.</summary>
        public void AddHeat(string sourceId) => AddHeat(HeatFor(sourceId));
        public void AddHeat(float n) { heat = Mathf.Min(police.maxHeat, heat + n); calmT = 0; }

        public void Say(string text, float seconds = 3f) { Message = text; msgT = seconds; }

        void Update()
        {
            float dt = Time.deltaTime;
            if (msgT > 0) { msgT -= dt; if (msgT <= 0) Message = ""; }
            cops.RemoveAll(c => c == null); cruisers.RemoveAll(c => c == null);
            if (downT > 0) { downT -= dt; if (downT <= 0) Respawn(); return; }

            // heat decays when nobody is chasing you
            var d = police.decay; calmT += dt;
            var pp = player.transform.position;
            float r2 = d.copNearRadius * d.copNearRadius;
            bool copNear = cops.Any(c => !c.GetComponent<Health>().Down && (c.transform.position - pp).sqrMagnitude < r2)
                || cruisers.Any(c => c.riders > 0 && (c.transform.position - pp).sqrMagnitude < r2);
            if (heat > 0 && calmT > d.calmAfter && !copNear) heat = Mathf.Max(0, heat - d.calmRate * dt);
            if (heat > 0 && calmT > d.longCalmAfter) heat = Mathf.Max(0, heat - d.longCalmRate * dt);

            Dispatch(dt);
        }

        void Dispatch(float dt)
        {
            int st = Stars;
            policeT -= dt;
            if (st == 0) return;
            var level = police.levels.First(l => l.stars == st);
            int active = cops.Count(c => !c.leave && !c.GetComponent<Health>().Down) + cruisers.Where(c => !c.leave).Sum(c => c.riders);
            if (active >= level.maxUnits || policeT > 0) return;
            policeT = level.spawnInterval;
            if (!SpawnPoint(out var at, police.spawnRingMin, police.spawnRingMax, 2.6f)) return;
            int n = Mathf.Min(level.officersPerCar, level.maxUnits - active);
            var p = player.transform.position; var face = p - at; face.y = 0;
            var go = Instantiate(cruiserTemplate, at, Quaternion.LookRotation(face.sqrMagnitude > 0.01f ? face : Vector3.forward));
            go.name = $"Cruiser {++CruisersSent}"; go.SetActive(true);
            var car = go.GetComponent<StreetEmpire.Vehicles.CarController>(); car.hp = level.cruiserHp;
            var cr = go.GetComponent<PoliceCruiser>(); cr.riders = n; cr.riderHp = level.officerHp;
            cr.SpawnDistance = Vector2.Distance(new Vector2(at.x, at.z), new Vector2(p.x, p.z));
            cruisers.Add(cr);
            Say(st >= 2 ? "More units on the way." : "Police are coming.", 2.5f);
        }

        /// <summary>An officer on foot at a spot (out of a cruiser), the cop models taken in turn.</summary>
        public Cop SpawnOfficer(Vector3 at, float hp)
        {
            var t = copTemplates[copLook++ % copTemplates.Length];
            var cop = Instantiate(t, at, Quaternion.identity);
            cop.name = $"Officer {cops.Count + 1}"; cop.SetActive(true);
            var h = cop.GetComponent<Health>(); h.max = h.hp = hp;
            var c = cop.GetComponent<Cop>(); cops.Add(c);
            return c;
        }

        /// <summary>A point on a district street center line, min to max m from the player, clear of buildings.</summary>
        public bool SpawnPoint(out Vector3 at, float min, float max, float clearRadius = 1f)
        {
            var p = player.transform.position; var opts = new List<Vector3>();
            foreach (var x in streetX) for (float t = spanZ.x; t <= spanZ.y; t += 4f) opts.Add(new Vector3(x, 0, t));
            foreach (var z in streetZ) for (float t = spanX.x; t <= spanX.y; t += 4f) opts.Add(new Vector3(t, 0, z));
            bool Clear(Vector3 q) => !Physics.OverlapSphere(q + Vector3.up, clearRadius, ~0, QueryTriggerInteraction.Ignore)
                .Any(c => c.GetComponent<BuildingMarker>() != null || c.GetComponentInParent<StreetEmpire.Vehicles.CarController>() != null);
            float D(Vector3 q) => Vector2.Distance(new Vector2(q.x, q.z), new Vector2(p.x, p.z));
            var ring = opts.Where(q => D(q) > min && D(q) < max && Clear(q)).ToList();
            // a small district may not reach the full ring: then the farthest clear street point
            if (ring.Count == 0) ring = opts.Where(Clear).OrderByDescending(D).Take(8).ToList();
            if (ring.Count == 0) { at = default; return false; }
            at = ring[Random.Range(0, ring.Count)];
            return true;
        }

        void OnPlayerDown()
        {
            if (downT > 0) return;
            var user = player.GetComponent<VehicleUser>();
            if (user != null && user.Car != null) user.Exit();
            downBusted = heat >= police.bustedHeat;
            float lost = Mathf.Floor(cash * (downBusted ? police.bailBusted : police.bailWasted));
            cash -= lost; LastBail = lost;
            if (downBusted) Busts++; else Wasted++;
            Say(downBusted ? $"BUSTED   Bail: -${lost}" : $"WASTED   Lost ${lost}. Back at your turf.", 4f);
            player.inputEnabled = false;
            downT = police.downTime;
        }

        void Respawn()
        {
            // back at the start of your first block (no safehouses in this slice)
            player.Teleport(spawn, spawnYaw);
            playerHealth.Restore();
            player.inputEnabled = true;
            heat = 0; calmT = 0;
            foreach (var c in cops) if (c != null) c.leave = true;
            foreach (var c in cruisers) if (c != null) c.leave = true;
        }
    }
}
