// The opening scuffle from openingScuffle() and openingStep() in state.js: 2.5 s into a new game a mugger
// (48 hp, fists, rush) stands 9 m ahead on the street and goes for your bag; within 2.3 m he grabs it and
// shoves you back; when he is hurt, has grabbed, or after 3.2 s his two friends pile in (50 hp fists, and
// 56 hp with a bat); when all three are down, or after 22 s, "The street goes quiet. Unc wants a word."
// It also keeps the onboarding steps (main.js ONB): 1/3 stop the muggers, 2/3 talk to Unc (3/3 take a block
// needs turf capture, not in this build).
// Not here yet: the Red Kings scout drive by that follows the fight in the web game.
using System.Collections.Generic;
using System.Linq;
using StreetEmpire.City;
using StreetEmpire.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StreetEmpire.Game
{
    public class MuggerOpening : MonoBehaviour
    {
        /// <summary>Tests and screenshot set ups that are not about the opening turn it off before the scene loads.</summary>
        public static bool AutoStart = true;
        public enum Beat { Waiting, Mug, Friends, Done }

        public GameObject[] templates;      // inactive brawler bodies (the Locals models), taken in turn
        public MissionRunner job;
        public float delay = 2.5f;
        public Beat Stage { get; private set; } = Beat.Waiting;
        public bool Grabbed { get; private set; }
        public readonly List<Brawler> crew = new();
        /// <summary>Onboarding step: 0 stop the muggers, 1 talk to Unc, 2 take a block, -1 none.</summary>
        public int Step { get; private set; }

        float wait, t; int look;

        void Start()
        {
            wait = delay;
            if (!AutoStart) { Stage = Beat.Done; Step = -1; }
        }

        /// <summary>No scuffle and no onboarding box (screenshot set ups that are about something else).</summary>
        public void Skip() { if (Stage == Beat.Waiting) Stage = Beat.Done; Step = -1; }

        public string StepText
        {
            get
            {
                bool touch = Application.isMobilePlatform || Touchscreen.current != null;
                return Step switch
                {
                    0 => "<color=#ffcc33>1/3</color>  Stop the muggers: " + (touch ? "HIT" : "F or click"),
                    1 => "<color=#ffcc33>2/3</color>  Talk to Unc: " + (touch ? "USE" : "E"),
                    2 => "<color=#ffcc33>3/3</color>  Take a block: clear it, stand in the ring",
                    _ => "",
                };
            }
        }

        void Update()
        {
            var s = GameSession.Instance; if (s == null || s.player == null) return;
            float dt = Time.deltaTime;
            // the talk step is done once Unc's job is running. The web's 3/3 (take a block) waits on turf capture,
            // which this build does not have yet, so the box goes away there instead of asking for it.
            if (Step == 1 && job != null && job.Stage != MissionRunner.State.Offered) Step = -1;
            if (Stage == Beat.Waiting) { if ((wait -= dt) <= 0) Begin(); return; }
            if (Stage == Beat.Done) return;
            t += dt;
            var p = s.player.transform.position;
            var mug = crew.Count > 0 ? crew[0] : null;
            bool mugUp = mug != null && !mug.GetComponent<Health>().Down;
            if (!Grabbed && mugUp && Flat(mug.transform.position, p) < 2.3f)
            {
                Grabbed = true;
                var away = p - mug.transform.position; away.y = 0;
                var cc = s.player.GetComponent<CharacterController>();
                if (cc != null && away.sqrMagnitude > 1e-4f) cc.Move(away.normalized * 0.8f);
                s.Say("He grabs at your bag. Shove him off.", 3f);
            }
            if (!Grabbed && !mugUp) Grabbed = true;
            bool hurt = mug != null && mug.GetComponent<Health>().hp < mug.GetComponent<Health>().max;
            if (Stage == Beat.Mug && (hurt || Grabbed || t > 3.2f)) Friends(s, mugUp ? mug.transform.position : p);
            if (Stage == Beat.Friends && (crew.All(b => b == null || b.GetComponent<Health>().Down) || t > 22f)) Finish(s);
        }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        /// <summary>The mugger appears 9 m ahead of the player (or the first clear spot round him) and comes in.</summary>
        public void Begin()
        {
            var s = GameSession.Instance; if (s == null || Stage != Beat.Waiting) return;
            var pl = s.player.transform;
            var f = pl.forward; f.y = 0; f = f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.forward; var r = Vector3.Cross(Vector3.up, f);
            var at = pl.position + f * 9f;
            foreach (var o in new[] { f * 9f, -f * 9f, -r * 9f, r * 9f, f * 6f })
                if (!Inside(pl.position + o)) { at = pl.position + o; break; }
            Spawn(at, 48f, Brawler.Style.Rush, false);
            Stage = Beat.Mug; t = 0; Step = 0;
            s.Say("A mugger is going for your bag.", 3f);
        }

        void Friends(GameSession s, Vector3 near)
        {
            Stage = Beat.Friends;
            var spots = new[] { new Vector3(3.2f, 0, 1.5f), new Vector3(-3.4f, 0, 1.2f), new Vector3(2.4f, 0, -2.6f), new Vector3(-2.2f, 0, -2.4f), new Vector3(4.2f, 0, 0), new Vector3(-4.2f, 0, 0.4f) };
            var clear = spots.Where(o => !Inside(near + o)).Select(o => near + o).ToList();
            while (clear.Count < 2) clear.Add(near + new Vector3(clear.Count == 0 ? 3f : -3f, 0, 0));
            Spawn(clear[0], 50f, Brawler.Style.Flank, false);
            Spawn(clear[1], 56f, Brawler.Style.Rush, true);
            s.Say("His friends jump in. One of them has a bat.", 3f);
        }

        void Finish(GameSession s)
        {
            Stage = Beat.Done;
            foreach (var b in crew) if (b != null && !b.GetComponent<Health>().Down) b.leave = true;
            if (Step == 0) Step = 1;
            s.Say("The street goes quiet. Unc wants a word.", 4f);
        }

        Brawler Spawn(Vector3 at, float hp, Brawler.Style style, bool bat)
        {
            // the bat template is the one with the bat in its hand
            var pick = bat ? templates.Last() : templates[look++ % Mathf.Max(1, templates.Length - 1)];
            var go = Instantiate(pick, new Vector3(at.x, 0.3f, at.z), Quaternion.identity);
            go.name = crew.Count == 0 ? "Mugger" : $"Mugger friend {crew.Count}";
            var s = GameSession.Instance;
            var face = s.player.transform.position - at; face.y = 0;
            if (face.sqrMagnitude > 1e-4f) go.transform.rotation = Quaternion.LookRotation(face);
            go.SetActive(true);
            var h = go.GetComponent<Health>(); h.max = h.hp = hp;
            var b = go.GetComponent<Brawler>(); b.style = style; b.bat = bat;
            crew.Add(b);
            return b;
        }

        static bool Inside(Vector3 q) =>
            Physics.OverlapSphere(q + Vector3.up, 0.8f, ~0, QueryTriggerInteraction.Ignore).Any(c => c.GetComponent<BuildingMarker>() != null);
    }
}
