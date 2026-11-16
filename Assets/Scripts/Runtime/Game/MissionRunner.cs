// Unc's first job, "Bag Back" (kind hook in jobs.json), run the way startMission(), hookDrop() and
// missionStep() do in state.js: talk to Unc, the bag drops on the first clear spot 12 m or more from him,
// pick it up within 4 m, bring it back within 4 m of Unc before the 90 s clock runs out.
using System.Linq;
using StreetEmpire.City;
using StreetEmpire.Player;
using StreetEmpire.Sim.Data;
using UnityEngine;

namespace StreetEmpire.Game
{
    public class MissionRunner : MonoBehaviour
    {
        public enum State { Offered, GetBag, ReturnBag, Passed }

        [Header("Baked from jobs.json")]
        public MissionData mission;
        public ContactData contact;
        [Header("Scene")]
        public Transform giver;                 // Unc
        public Transform marker, bag;           // yellow beam, the bag itself
        public float talkRange = 3f, reach = 4f;

        public State Stage { get; private set; } = State.Offered;
        public int Fails { get; private set; }
        public float TimeLeft { get; private set; }
        public Vector3 Target { get; private set; }
        public bool scriptTalk;
        string objective = "";
        public string Objective => objective;

        // hookDrop() offsets in source space; x is mirrored into Unity space
        static readonly Vector2[] Spots = { new(18, -6), new(-16, 14), new(22, 10), new(8, -22), new(20, 16), new(-18, -10), new(28, -2), new(-8, 22) };

        void Start() { Show(false); objective = $"Talk to {contact.name}"; }

        void Update()
        {
            var s = GameSession.Instance; if (s == null || s.player == null) return;
            var p = s.player.transform.position;
            var user = s.player.GetComponent<VehicleUser>();
            float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

            switch (Stage)
            {
                case State.Offered:
                    bool near = Flat(p, giver.position) < talkRange && (user == null || user.Car == null);
                    objective = near ? $"Press Use to talk to {contact.name}" : $"Talk to {contact.name}";
                    if (scriptTalk || (near && PlayerControls.Instance.UsePressed)) Begin();
                    scriptTalk = false;
                    break;
                case State.GetBag:
                case State.ReturnBag:
                    TimeLeft -= Time.deltaTime;
                    if (TimeLeft <= 0) { Fail("Out of time. Job failed."); break; }
                    if (Stage == State.GetBag && Flat(p, Target) < reach)
                    {
                        Stage = State.ReturnBag; Target = giver.position; bag.gameObject.SetActive(false);
                        s.Say($"Got the bag. Bring it back to {contact.name}.", 3f);
                    }
                    else if (Stage == State.ReturnBag && Flat(p, Target) < reach) Pass();
                    if (Stage == State.GetBag || Stage == State.ReturnBag)
                    {
                        marker.position = new Vector3(Target.x, 0, Target.z);
                        objective = (Stage == State.GetBag ? "Grab the bag at the yellow marker." : $"Bring the bag back to {contact.name}.") + $"   {Mathf.CeilToInt(TimeLeft)}s";
                    }
                    break;
            }
        }

        public void Begin()
        {
            if (Stage != State.Offered) return;
            Target = HookDrop(); Stage = State.GetBag; TimeLeft = mission.time;
            bag.position = Target + Vector3.up * 0.25f; Show(true);
            var s = GameSession.Instance;
            s.Say(string.Join("\n", mission.brief.Append(mission.intro)), 6f);
        }

        Vector3 HookDrop()
        {
            var g = giver.position;
            foreach (var o in Spots)
            {
                var q = new Vector3(g.x - o.x, 0, g.z + o.y);
                if (o.magnitude < 12 || Inside(q)) continue;
                return q;
            }
            return new Vector3(g.x - 18, 0, g.z - 6);
        }

        static bool Inside(Vector3 q) =>
            Physics.OverlapSphere(q + Vector3.up, 0.8f, ~0, QueryTriggerInteraction.Ignore).Any(c => c.GetComponent<BuildingMarker>() != null);

        void Pass()
        {
            var s = GameSession.Instance;
            s.cash += mission.reward; s.rep += mission.rep;
            Stage = State.Passed; Show(false);
            string said = contact.pass.Count > 0 ? $"\n{contact.name}: \"{contact.pass[mission.index % contact.pass.Count]}\"" : "";
            s.Say($"Job done: {mission.title}. +${mission.reward}{said}", 5f);
            objective = "";
        }

        void Fail(string why)
        {
            var s = GameSession.Instance;
            // failMission(): the job is dropped and Unc offers it again
            Stage = State.Offered; Fails++; Show(false);
            string said = contact.fail.Count > 0 ? $" {contact.name}: \"{contact.fail[mission.index % contact.fail.Count]}\"" : "";
            s.Say(why + said, 5f);
        }

        void Show(bool on) { if (marker) marker.gameObject.SetActive(on); if (bag) bag.gameObject.SetActive(on); }
    }
}
