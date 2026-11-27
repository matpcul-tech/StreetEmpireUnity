// Street calls, from the web build (matpcul-tech/StreetEmpire src/state.js, CALLS and callsStep): one optional
// corner event at a time. A rival crew pushes a corner (incursion), two lookouts are posted up the block (ambush),
// or a deal breaks (deal). The player steps in or walks off. First one after 36 s, 46 s between them, five a
// session, 14 s to decide, 28 s to finish. Nothing starts during the opening, a job, a side hustle, a bust, while
// driving, at four stars or more, or with police already close. Numbers come from Assets/Data/jobs.json.
using System.Collections.Generic;
using StreetEmpire.Player;
using StreetEmpire.Sim;
using UnityEngine;

namespace StreetEmpire.Game
{
    public class StreetCalls : MonoBehaviour
    {
        public static StreetCalls Instance { get; private set; }
#if UNITY_EDITOR
        /// <summary>Off in the editor so the other tests' streets stay as they expect; the builds run it.</summary>
        public static bool AutoStart = false;
#else
        public static bool AutoStart = true;
#endif
        // CALLS in state.js (jobs.json "streetCalls")
        public float first = 36f, cooldown = 46f, offer = 14f, fight = 28f, minDist = 14f;
        public int cap = 5;
        /// <summary>How close is stepping in, and how far is walking off (offer) or leaving the fight.</summary>
        public const float StepIn = 13f, WalkOff = 34f, Leave = 42f;

        public enum Kind { Incursion, Ambush, Deal }
        public enum Phase { None, Offer, Fight }
        public Phase Now { get; private set; }
        public Kind Current { get; private set; }
        public Vector3 Spot { get; private set; }
        public int Offered { get; private set; }
        public int Won { get; private set; }
        public string LastEnd { get; private set; } = "";
        public IReadOnlyList<Health> Men => men;
        /// <summary>Seconds until the next call may start (tests shorten it).</summary>
        public float wait;

        readonly List<Health> men = new();
        float t, age; bool capped;
        string crewName, leaveText, skimText, winText, fightText;
        int reward, repGain; float skim, heatGain;

        void Awake() { Instance = this; wait = first; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>A new game: no call up, the first one a little way off.</summary>
        public void NewGame() { End("new game"); wait = first; Offered = 0; Won = 0; capped = false; age = 0f; }

        void Update()
        {
            if (!AutoStart) return;
            var s = GameSession.Instance; if (s == null || s.player == null) return;
            age += Time.deltaTime;
            Vector3 p = s.player.transform.position;
            if (Now != Phase.None) { Step(s, p); return; }
            if (Blocked(s, p)) return;
            if (Offered >= cap) { if (!capped) { capped = true; s.Say("The corner stays quiet."); } return; }
            if (wait > 0f) { wait -= Time.deltaTime; return; }
            Offer(s, p);
        }

        bool Blocked(GameSession s, Vector3 p)
        {
            if (age < 8f || s.IsDown || s.Stars >= 4) return true;
            // the first job under way (the bag is out), like any job from the board
            var mr = MissionRunner.Instance;
            if (mr != null && !mr.External && (mr.Stage == MissionRunner.State.GetBag || mr.Stage == MissionRunner.State.ReturnBag)) return true;
            if (JobBoard.Instance != null && JobBoard.Instance.Running) return true;
            if (SideHustles.Instance != null && SideHustles.Instance.Busy) return true;
            var user = s.player.GetComponent<VehicleUser>(); if (user != null && user.Car != null) return true;
            foreach (var c in s.cops) if (c != null && (c.transform.position - p).sqrMagnitude < 45f * 45f) return true;
            return false;
        }

        void Offer(GameSession s, Vector3 p)
        {
            var turf = TurfWar.Instance; if (turf == null || turf.State == null) { wait = 8f; return; }
            string crew = RivalNear(turf, p); if (crew == null) { wait = 8f; return; }
            if (!s.SpawnPoint(out var at, minDist + 2f, minDist + 10f, 1.1f)) { wait = 3f; return; }
            Current = (Kind)(Offered % 3); Spot = at; crewName = turf.CrewName(crew);
            string text;
            switch (Current)
            {
                case Kind.Incursion:
                    reward = 80; repGain = 8; skim = 0.25f; heatGain = 0f;
                    text = $"{crewName} are pushing this corner. Step in, or let them shake it down.";
                    fightText = $"{crewName} rush the corner. Drop them or leave.";
                    leaveText = $"{crewName} rough up the corner and move on."; skimText = $"{crewName} shook $N out of the corner and left.";
                    winText = $"You broke the {crewName} push.";
                    break;
                case Kind.Ambush:
                    reward = 60; repGain = 6; skim = 0f; heatGain = 0f;
                    text = $"Two {crewName} are posted up the block. Face them, or go around.";
                    fightText = $"The {crewName} lookouts come at you.";
                    leaveText = skimText = "You went around. They pack it in."; winText = "The lookouts are down.";
                    break;
                default:
                    reward = 100; repGain = 4; skim = 0.12f; heatGain = 6f;
                    text = "A deal on the corner just broke. Break it up, or leave it.";
                    fightText = "Both sides turn on you.";
                    leaveText = "They settle it and scatter."; skimText = "They settle it and skim $N off the block."; winText = "You took the purse.";
                    break;
            }
            // who stands where, with what and how tough: the specs in state.js (x, z, hp, bat). The second man of a push
            // or an ambush carries a bat and comes round the side. (The web game's retreat style has no brawler here yet.)
            (float x, float z, float hp, bool bat)[] specs = Current == Kind.Incursion ? new[] { (0f, 0f, 56f, false), (1.5f, 0.8f, 60f, true), (-1.5f, 0.6f, 50f, false) }
                : Current == Kind.Ambush ? new[] { (0f, 0f, 58f, false), (1.6f, -0.4f, 60f, true) } : new[] { (0f, 0f, 52f, false), (1.5f, 0.2f, 48f, false) };
            men.Clear();
            for (int i = 0; i < specs.Length; i++)
            {
                var h = turf.SpawnHostile(crew, at + new Vector3(specs[i].x, 0f, specs[i].z), false, $"{crewName} ({Current.ToString().ToLowerInvariant()})", true, specs[i].bat);
                if (h == null) continue;
                h.max = h.hp = specs[i].hp;
                var br = h.GetComponent<Brawler>(); if (br != null && i == 1) br.style = Brawler.Style.Flank;
                men.Add(h);
            }
            if (men.Count == 0) { wait = 8f; return; }
            Offered++; Now = Phase.Offer; t = offer;
            s.Say(text, 6f);
        }

        void Step(GameSession s, Vector3 p)
        {
            if (s.IsDown || (JobBoard.Instance != null && JobBoard.Instance.Running))
            {
                if (!s.IsDown) s.Say("The corner call clears off. Finish the job first.");
                End(s.IsDown ? "down" : "mission"); return;
            }
            float away = Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(Spot.x, 0, Spot.z));
            if (Now == Phase.Offer)
            {
                t -= Time.deltaTime;
                bool hurt = false; foreach (var m in men) if (m != null && m.hp < m.max) hurt = true;
                if (away <= StepIn || hurt) { Accept(s); return; }
                if (away > WalkOff || t <= 0f) { Decline(s, t <= 0f ? "timeout" : "off"); return; }
                return;
            }
            bool live = false; foreach (var m in men) if (m != null && !m.Down) live = true;
            if (!live)
            {
                s.cash += reward; s.rep += repGain; Won++;
                s.Say($"{winText} +${reward}", 4f);
                End("win"); return;
            }
            t -= Time.deltaTime;
            if (t <= 0f || away > Leave)
            {
                s.Say(t <= 0f ? "They scatter before you finish it." : "You left the corner. They scatter.");
                End(t <= 0f ? "timeout" : "leave");
            }
        }

        void Accept(GameSession s)
        {
            Now = Phase.Fight; t = fight;
            foreach (var m in men) { var br = m != null ? m.GetComponent<Brawler>() : null; if (br != null) br.posted = false; }
            if (heatGain > 0f) s.AddHeat(heatGain);
            s.Say(fightText, 4f);
        }

        void Decline(GameSession s, string why)
        {
            string note = leaveText;
            var turf = TurfWar.Instance;
            if (skim > 0f && turf != null && turf.State != null && turf.ZoneBlock >= 0)
            {
                // skimStash: a share of the stash on the player's own block where he stands
                var b = turf.State.Block(turf.ZoneBlock);
                if (b != null && b.owner == Turf.You)
                {
                    int took = Mathf.FloorToInt(b.stash * skim);
                    if (took >= 1) { b.stash -= took; note = skimText.Replace("$N", "$" + took); }
                }
            }
            s.Say(note, 4f);
            End(why);
        }

        /// <summary>The call is over: the men still standing leave, the ones on the ground after a win stay down.</summary>
        void End(string why)
        {
            foreach (var m in men) if (m != null && !(why == "win" && m.Down)) Destroy(m.gameObject);
            men.Clear();
            if (Now != Phase.None) { LastEnd = why; wait = cooldown; }
            Now = Phase.None;
        }

        static string RivalNear(TurfWar turf, Vector3 p)
        {
            string best = null; float bd = float.MaxValue;
            foreach (int id in turf.blockIds)
            {
                var b = turf.State.Block(id); if (b == null || System.Array.IndexOf(Turf.Rivals, b.owner) < 0) continue;
                float d = (turf.Center(id) - p).sqrMagnitude;
                if (d < bd) { bd = d; best = b.owner; }
            }
            return best;
        }
    }
}
