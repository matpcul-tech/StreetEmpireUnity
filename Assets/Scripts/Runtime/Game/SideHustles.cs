// The side money from the web game (jobs.json: stashes, races, fare, courier), once Bag Back is done and while no
// contact's job is running:
//   stashes  hidden bags around the town: walk or drive onto one for stashPay, and a bonus for finding them all
//   races    drive a car into a race's start marker: hit its markers in order, paid by medal time
//   fares    driving a taxi, a fare waits at a marker: stop there, then stop at the drop; paid by distance plus a tip for speed
//   courier  the same in a van, a minivan or a pickup, with an order to carry and a bonus for speed
// A run shows through MissionRunner.Present like a job does; JobBoard stands back while one is on.
using System.Linq;
using StreetEmpire.City;
using StreetEmpire.Player;
using StreetEmpire.Sim.Data;
using StreetEmpire.Vehicles;
using UnityEngine;

namespace StreetEmpire.Game
{
    public class SideHustles : MonoBehaviour
    {
        public const string StashKey = "stash.found";
        public static SideHustles Instance { get; private set; }
        public static bool AutoStart = true;

        [System.Serializable] public class Race { public string name; public Vector3 start; public Vector3[] points = new Vector3[0]; public float gold, silver, bronze; }

        [Header("Baked from jobs.json")]
        public FareData fare; public CourierData courier; public RacePayData racePay;
        public int stashPay, stashAll;
        public Race[] races = new Race[0];
        [Header("Scene")]
        public MissionRunner runner;
        public Transform[] raceStarts = new Transform[0];   // same order as races
        public Transform[] stashes = new Transform[0];

        public enum Mode { None, Race, Fare, Courier }
        public Mode Current { get; private set; }
        public bool Busy => Current != Mode.None;
        public int StashesFound { get; private set; }
        public int RacesRun { get; private set; }
        public int RunsPaid { get; private set; }
        public string LastMedal { get; private set; } = "";
        public Vector3 Target { get; private set; }

        static readonly string[] Haulers = { "van", "minivan", "pickup" };
        int race = -1, step; float clock, gapT, waitT, lostT, raceCool, dist; Vector3 pick, drop;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            if (!AutoStart) { foreach (var t in stashes) if (t) t.gameObject.SetActive(false); foreach (var t in raceStarts) if (t) t.gameObject.SetActive(false); enabled = false; return; }
            int found = TurfWar.Persist ? PlayerPrefs.GetInt(StashKey, 0) : 0;
            for (int i = 0; i < stashes.Length; i++)
            {
                var t = stashes[i]; if (t == null) continue;
                // a stash spot from the web map can land inside one of this town's buildings: then it sits on the sidewalk nearest to it
                if (Physics.OverlapSphere(t.position + Vector3.up, 1.2f, ~0, QueryTriggerInteraction.Ignore).Any(c => c.GetComponent<BuildingMarker>() != null))
                {
                    var st = JobBoard.NearestStreet(t.position); var to = t.position - st; to.y = 0;
                    t.position = st + (to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward) * 10f + Vector3.up * 0.25f;
                }
                bool got = (found & (1 << i)) != 0; t.gameObject.SetActive(!got); if (got) StashesFound++;
            }
            foreach (var t in raceStarts) if (t) t.gameObject.SetActive(false);
            gapT = fare != null ? fare.gap : 14f;
        }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        void Update()
        {
            var s = GameSession.Instance; if (s == null || s.player == null) return;
            var p = s.player.transform.position; var user = s.player.GetComponent<VehicleUser>(); var car = user != null ? user.Car : null;
            float dt = Time.deltaTime;
            Stashes(s, p);

            var board = JobBoard.Instance;
            bool open = board != null && board.Owns && !board.Running && runner != null && !s.IsDown;
            if (!open) { if (Busy) End(); ShowStarts(false); return; }
            if (raceCool > 0) raceCool -= dt;

            switch (Current)
            {
                case Mode.None:
                {
                    ShowStarts(true);
                    if (car == null) break;
                    for (int i = 0; i < races.Length && raceCool <= 0; i++)
                        if (races[i].points.Length > 0 && Flat(p, races[i].start) < 6f) { race = i; step = 0; clock = 0; lostT = 0; Current = Mode.Race; s.Say($"{races[i].name}. Go!", 2.5f); break; }
                    if (Busy) break;
                    string id = car.stats != null ? car.stats.id : "";
                    bool taxi = id == "taxi" && fare != null, haul = System.Array.IndexOf(Haulers, id) >= 0 && courier != null;
                    if (!taxi && !haul) { gapT = Mathf.Max(gapT, 2f); break; }
                    gapT -= dt; if (gapT > 0) break;
                    if (!s.SpawnPoint(out pick, 40f, 120f, 2.6f)) break;
                    Current = taxi ? Mode.Fare : Mode.Courier; step = 0; lostT = 0; waitT = taxi ? fare.wait : courier.wait;
                    s.Say(taxi ? "A fare is waiting. Pick them up at the marker." : "An order is ready. Pick it up at the marker.", 3f);
                    break;
                }

                case Mode.Race:
                {
                    var r = races[race]; clock += dt;
                    if (car == null) { lostT += dt; if (lostT > 8f) { s.Say("Race off.", 2f); End(); break; } } else lostT = 0;
                    if (car != null && Flat(p, r.points[step]) < 10f)
                    {
                        step++;
                        if (step >= r.points.Length)
                        {
                            LastMedal = clock <= r.gold ? "gold" : clock <= r.silver ? "silver" : clock <= r.bronze ? "bronze" : "none";
                            int pay = LastMedal == "gold" ? racePay.gold : LastMedal == "silver" ? racePay.silver : LastMedal == "bronze" ? racePay.bronze : racePay.none;
                            s.cash += pay; RacesRun++; raceCool = 8f;
                            s.Say($"{r.name}: {clock:0.0}s. {(LastMedal == "none" ? "No medal" : char.ToUpper(LastMedal[0]) + LastMedal.Substring(1))}. +${pay}", 5f);
                            End(); break;
                        }
                    }
                    Show(r.points[step], "MARKER", $"{r.name}   {clock:0.0}s   marker {step + 1} of {r.points.Length}   gold {r.gold:0}s");
                    break;
                }

                case Mode.Fare:
                case Mode.Courier:
                {
                    bool taxi = Current == Mode.Fare; string id = car != null && car.stats != null ? car.stats.id : "";
                    bool right = car != null && (taxi ? id == "taxi" : System.Array.IndexOf(Haulers, id) >= 0);
                    if (!right) { lostT += dt; if (lostT > 6f) { s.Say(taxi ? "The fare is gone." : "The order went to someone else.", 2.5f); End(); break; } } else lostT = 0;
                    bool stoppedAt(Vector3 at) => right && Flat(p, at) < 7f && Mathf.Abs(car.Speed) < 2.5f;
                    if (step == 0)
                    {
                        waitT -= dt;
                        if (waitT <= 0) { s.Say(taxi ? "The fare got another ride." : "The order was pulled.", 2.5f); End(); break; }
                        if (stoppedAt(pick))
                        {
                            if (!s.SpawnPoint(out drop, 110f, 400f, 2.6f)) { End(); break; }
                            step = 1; clock = 0; dist = Flat(pick, drop);
                            s.Say(taxi ? "They are in. Take them to the marker." : "Loaded. Take it to the marker.", 3f);
                        }
                        else { Show(pick, taxi ? "FARE" : "PICKUP", (taxi ? "Stop at the fare." : "Stop at the pickup.") + $"   {Mathf.CeilToInt(waitT)}s"); break; }
                    }
                    clock += dt;
                    if (stoppedAt(drop))
                    {
                        // a tip or bonus for a quick run: all of it at a steady 11 m/s, none at half that
                        float par = dist / 11f + 6f, quick = Mathf.Clamp01((2f * par - clock) / par);
                        int extra = Mathf.RoundToInt((taxi ? fare.tipMax : courier.bonusMax) * quick);
                        int pay = Mathf.RoundToInt((taxi ? fare.basePay : courier.basePay) + (taxi ? fare.perMeter : courier.perMeter) * dist) + extra;
                        s.cash += pay; RunsPaid++;
                        s.Say(taxi ? $"Fare paid. +${pay} (tip ${extra})" : $"Delivered. +${pay} (bonus ${extra})", 4f);
                        End(); break;
                    }
                    Show(drop, "DROP", taxi ? "Take the fare to the marker and stop." : "Take the order to the marker and stop.");
                    break;
                }
            }
        }

        void Show(Vector3 at, string name, string text) { Target = at; runner.Present(MissionRunner.State.GetBag, at, text, name, true); }

        void End()
        {
            Current = Mode.None; race = -1; step = 0; lostT = 0;
            gapT = fare != null ? fare.gap : 14f;
        }

        void ShowStarts(bool on) { foreach (var t in raceStarts) if (t != null && t.gameObject.activeSelf != on) t.gameObject.SetActive(on); }

        void Stashes(GameSession s, Vector3 p)
        {
            for (int i = 0; i < stashes.Length; i++)
            {
                var t = stashes[i]; if (t == null || !t.gameObject.activeSelf || Flat(p, t.position) > 3f) continue;
                t.gameObject.SetActive(false); StashesFound++; s.cash += stashPay;
                bool all = StashesFound >= stashes.Length; if (all) s.cash += stashAll;
                s.Say(all ? $"Stash {StashesFound} of {stashes.Length}. +${stashPay}\nEvery stash found. +${stashAll}" : $"Stash {StashesFound} of {stashes.Length}. +${stashPay}", 4f);
                if (TurfWar.Persist) { PlayerPrefs.SetInt(StashKey, PlayerPrefs.GetInt(StashKey, 0) | (1 << i)); PlayerPrefs.Save(); }
            }
        }

        /// <summary>Start the side money over (a new game): every stash back.</summary>
        public void NewGame()
        {
            End(); StashesFound = 0; RacesRun = 0; RunsPaid = 0; PlayerPrefs.DeleteKey(StashKey);
            foreach (var t in stashes) if (t) t.gameObject.SetActive(true);
        }
    }
}
