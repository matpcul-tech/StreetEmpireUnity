// The web build's "E  Arm guards ($80)" (main.js ctxName UPGRADE, state.js upgrade): standing on a block you hold
// whose guards are not armed yet, the prompt shows; Use pays UPGRADE_COST (80) and arms them (bats and more health,
// level 1 in the sim's terms). Talking to Unc and getting into a car come first when they are in reach.
using StreetEmpire.Player;
using UnityEngine;
using UnityEngine.UI;

namespace StreetEmpire.Game
{
    public class ArmGuards : MonoBehaviour
    {
        public Vector3[] ownedBlocks = new Vector3[0];   // centers of the blocks the player holds
        public float blockHalf = 21f; public int cost = 80;
        public Text prompt; public MissionRunner job;
        bool[] armed;
        public bool Showing { get; private set; }
        public int Armed { get { int n = 0; if (armed != null) foreach (var a in armed) if (a) n++; return n; } }

        void Start() { armed = new bool[ownedBlocks.Length]; if (prompt) prompt.gameObject.SetActive(false); }

        int Here(Vector3 p)
        {
            for (int i = 0; i < ownedBlocks.Length; i++)
                if (Mathf.Abs(p.x - ownedBlocks[i].x) < blockHalf && Mathf.Abs(p.z - ownedBlocks[i].z) < blockHalf && !armed[i]) return i;
            return -1;
        }

        void Update()
        {
            var s = GameSession.Instance; int i = -1;
            if (s != null && s.player != null && !s.IsDown && armed != null)
            {
                var p = s.player.transform.position; var user = s.player.GetComponent<VehicleUser>();
                bool inCar = user != null && user.Car != null;
                bool nearUnc = job != null && job.Stage == MissionRunner.State.Offered && job.giver != null && Vector2.Distance(new Vector2(p.x, p.z), new Vector2(job.giver.position.x, job.giver.position.z)) < job.talkRange;
                bool nearCar = false;
                if (user != null) foreach (var c in FindObjectsByType<StreetEmpire.Vehicles.CarController>(FindObjectsSortMode.None)) if (!c.hasDriver && (c.transform.position - p).sqrMagnitude < user.enterRange * user.enterRange) { nearCar = true; break; }
                if (!inCar && !nearUnc && !nearCar) i = Here(p);
            }
            Showing = i >= 0;
            if (prompt) { prompt.gameObject.SetActive(Showing); if (Showing) prompt.text = $"E  Arm guards (${cost})"; }
            if (!Showing || PlayerControls.Instance == null || !PlayerControls.Instance.UsePressed) return;
            if (s.cash < cost) { s.Say($"Need ${cost} to arm the guards.", 2.5f); return; }
            s.cash -= cost; armed[i] = true;
            s.Say($"Guards on your block got bats and more health. -${cost}", 3f);
        }
    }
}
