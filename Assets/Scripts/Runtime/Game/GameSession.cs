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
        public GameObject copTemplate;
        // The JS drives a cruiser in from spawnRingMin to spawnRingMax (60 to 110 m) at up to 34 m/s and the officers
        // get out near you. This slice has no police cars yet, so officers arrive on foot from about where that
        // cruiser would stop, which keeps the time until they reach you in the same range.
        public Vector2 footRing = new(30f, 45f);

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

        float downT, policeT, msgT; bool downBusted;

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
            cops.RemoveAll(c => c == null);
            if (downT > 0) { downT -= dt; if (downT <= 0) Respawn(); return; }

            // heat decays when nobody is chasing you
            var d = police.decay; calmT += dt;
            var pp = player.transform.position;
            bool copNear = cops.Any(c => !c.GetComponent<Health>().Down && (c.transform.position - pp).sqrMagnitude < d.copNearRadius * d.copNearRadius);
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
            int active = cops.Count(c => !c.leave && !c.GetComponent<Health>().Down);
            if (active >= level.maxUnits || policeT > 0) return;
            policeT = level.spawnInterval;
            if (!SpawnPoint(out var at)) return;
            for (int i = 0; i < level.officersPerCar && active + i < level.maxUnits; i++)
            {
                var cop = Instantiate(copTemplate, at + new Vector3(i * 1.6f - 0.8f, 0.1f, 0), Quaternion.identity);
                cop.name = $"Officer {cops.Count + 1}"; cop.SetActive(true);
                var h = cop.GetComponent<Health>(); h.max = h.hp = level.officerHp;
                cops.Add(cop.GetComponent<Cop>());
            }
            Say(st >= 2 ? "More units on the way." : "Police are coming.", 2.5f);
        }

        /// <summary>A point on a district street center line, footRing from the player, clear of buildings.</summary>
        public bool SpawnPoint(out Vector3 at)
        {
            var p = player.transform.position; var opts = new List<Vector3>();
            foreach (var x in streetX) for (float t = spanZ.x; t <= spanZ.y; t += 4f) opts.Add(new Vector3(x, 0, t));
            foreach (var z in streetZ) for (float t = spanX.x; t <= spanX.y; t += 4f) opts.Add(new Vector3(t, 0, z));
            bool Clear(Vector3 q) => !Physics.OverlapSphere(q + Vector3.up, 1f, ~0, QueryTriggerInteraction.Ignore).Any(c => c.GetComponent<BuildingMarker>() != null);
            float D(Vector3 q) => Vector2.Distance(new Vector2(q.x, q.z), new Vector2(p.x, p.z));
            var ring = opts.Where(q => D(q) > footRing.x && D(q) < footRing.y && Clear(q)).ToList();
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
        }
    }
}
