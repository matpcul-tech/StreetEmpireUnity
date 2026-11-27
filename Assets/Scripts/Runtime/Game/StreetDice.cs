// Street dice on the corner (rules in Assets/Scripts/Sim/Dice.cs, from the web build's state.js and hustle.js): a
// block you hold whose corner runs the dice game gets a chalk ring, two dice, a few bills and a cardboard backstop
// on the sidewalk beside the people standing there. Stand at it and bet 5, 10, 25 or 50 against the corner's pot: the bet
// buttons on screen, or USE for the last bet. With a point on, every tap is the next roll. Walk off a live bet, get
// in a car, go down or lose the block and the pot keeps it. The dice land showing what was thrown.
using System.Collections.Generic;
using System.Linq;
using StreetEmpire.Player;
using StreetEmpire.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace StreetEmpire.Game
{
    public class StreetDice : MonoBehaviour
    {
        public static StreetDice Instance { get; private set; }
#if UNITY_EDITOR
        /// <summary>Off in the editor so the other tests' prompts and corners stay as they expect; the builds run it.</summary>
        public static bool AutoStart = false;
#else
        public static bool AutoStart = true;
#endif
        /// <summary>The props, built by SceneBuilder.DiceTemplate and kept inactive under the map.</summary>
        public GameObject template;
        /// <summary>The bet USE puts down: the last one tapped.</summary>
        public int bet = 10;

        /// <summary>The block whose game the player stands at, or -1.</summary>
        public int Table { get; private set; } = -1;
        public DiceBet Live { get; private set; }
        public DiceThrow Last { get; private set; }
        public int Throws { get; private set; }
        public string LastEnd { get; private set; } = "";
        /// <summary>At a game, or with a bet still down: the turf buy prompt stands aside.</summary>
        public bool Near => Table >= 0 || Live != null;
        public string Prompt { get; private set; } = "";
        public IEnumerable<GameObject> Games => games.Values.Where(g => g != null && g.root != null).Select(g => g.root);

        class Game { public GameObject root; public Transform[] dice = new Transform[2]; public Vector3[] from = new Vector3[2], rest = new Vector3[2]; public Quaternion[] end = new Quaternion[2]; public float t = 9f; }
        readonly Dictionary<int, Game> games = new();
        readonly Dictionary<int, Vector3> spots = new();
        readonly Dictionary<int, float> yaws = new();
        float propT; int tapped;
        GameObject panel, rollButton; readonly List<GameObject> betButtons = new();

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        /// <summary>A new game: no bet down.</summary>
        public void NewGame() { Live = null; LastEnd = ""; Throws = 0; }

        /// <summary>A tap on a bet button (5, 10, 25, 50), or 0 for "roll again". Tests press it too.</summary>
        public void Tap(int amount) => tapped = amount > 0 ? amount : -1;

        /// <summary>Where a block's game is: the middle of its corner group.</summary>
        public bool Spot(int blockId, out Vector3 at)
        {
            if (spots.TryGetValue(blockId, out at)) return true;
            var turf = TurfWar.Instance; if (turf == null || turf.IndexOf(blockId) < 0) return false;
            var life = BlockLife.Instance;
            if (BlockLife.AutoStart && life != null)
            {
                if (!life.Built) return false;   // the corner people pick the corner first; the game goes where they stand
                var g = life.groups.FirstOrDefault(x => x.blockId == blockId); if (g == null) return false;
                at = g.at;
            }
            else if (!BlockLife.Corner(turf.Center(blockId), out at)) return false;
            // the group stands on the lot's corner, half on the grass: the game is chalked on the sidewalk beside them,
            // two steps out from the block, if nothing but people stands there
            Vector3 outward = at - turf.Center(blockId); outward.y = 0f;
            if (outward.sqrMagnitude > 0.01f)
            {
                outward.Normalize();
                Vector3 beside = at + outward * 2.0f; bool clear = true;
                foreach (var c in Physics.OverlapSphere(beside + Vector3.up * 1.0f, 0.8f, ~0, QueryTriggerInteraction.Ignore))
                    if (!Person(c)) { clear = false; break; }
                if (clear) at = beside;
                // the backstop along the sidewalk, not toward the people or the street
                yaws[blockId] = Quaternion.LookRotation(Vector3.Cross(Vector3.up, outward)).eulerAngles.y;
            }
            // down onto the sidewalk: the first thing under the spot that is not a person
            float y = at.y - 0.1f, best = float.MaxValue;
            foreach (var h in Physics.RaycastAll(at + Vector3.up * 1.5f, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore))
                if (h.distance < best && !Person(h.collider)) { best = h.distance; y = h.point.y; }
            at.y = y; spots[blockId] = at;
            return true;
        }

        /// <summary>A body, not a thing: the corner people, walkers, fighters and the player carry their capsule on the
        /// root and their Animator on a child, so the Animator alone does not find them.</summary>
        static bool Person(Collider c) => c.GetComponentInParent<Loiterer>() != null || c.GetComponentInParent<Health>() != null || c.GetComponentInParent<Townie>() != null
            || c.GetComponentInParent<Ped>() != null || c.GetComponentInParent<PlayerMotor>() != null || c.GetComponentInParent<Animator>() != null;

        void Update()
        {
            var s = GameSession.Instance; var turf = TurfWar.Instance;
            if (!AutoStart || s == null || s.player == null || turf == null || turf.State == null) { Table = -1; Prompt = ""; Show(false); return; }
            float dt = Time.deltaTime;
            Dice.Step(turf.State, turf.world, dt);
            var p = s.player.transform.position;
            var user = s.player.GetComponent<VehicleUser>(); bool inCar = user != null && user.Car != null;
            if ((propT -= dt) <= 0f) { propT = 0.5f; Props(turf, p); }
            foreach (var g in games.Values) Animate(g, dt);

            Table = -1;
            if (!inCar && !s.IsDown)
                foreach (var b in turf.State.blocks)
                    if (Dice.Runs(b) && b.owner == Turf.You && Spot(b.id, out var at) && Flat(p, at) <= Dice.Reach) { Table = b.id; break; }

            if (Live != null)
            {
                var b = turf.State.Block(Live.block);
                bool gone = b == null || b.owner != Turf.You || inCar || s.IsDown || !Spot(Live.block, out var at) || Flat(p, at) > Dice.Leave;
                if (gone)
                {
                    Dice.Forfeit(turf.State, turf.world, Live);
                    s.Say($"You walked off a live bet. -${Live.bet}.", 3f);
                    Live = null; LastEnd = "forfeit";
                }
            }

            bool use = PlayerControls.Instance != null && PlayerControls.Instance.UsePressed && !CarInReach(s, user, p);
            if (Table >= 0 && (use || tapped != 0)) Shoot(s, turf, tapped > 0 ? tapped : Live != null ? Live.bet : bet);
            tapped = 0;

            string key = ArmGuards.Touch ? "USE" : "E";
            if (Live != null) Prompt = Table == Live.block ? $"{key}  Roll   Point {Live.point}   ${Live.bet} down" : $"${Live.bet} still down on point {Live.point}. Get back to the game.";
            else if (Table >= 0) Prompt = $"{key}  Shoot dice (${bet})   Pot ${Mathf.FloorToInt(Dice.Pot(turf.world, turf.State.Block(Table)))}";
            else Prompt = "";
            Show(Table >= 0, turf);
        }

        static bool CarInReach(GameSession s, VehicleUser user, Vector3 p)
        {
            if (user == null) return false;
            foreach (var c in FindObjectsByType<StreetEmpire.Vehicles.CarController>(FindObjectsSortMode.None))
                if (!c.hasDriver && (c.transform.position - p).sqrMagnitude < user.enterRange * user.enterRange) return true;
            return false;
        }

        void Shoot(GameSession s, TurfWar turf, int amount)
        {
            var st = turf.State; var you = st.Crew(Turf.You); you.cash = s.cash; you.rep = s.rep;
            if (Live == null)
            {
                var b = st.Block(Table);
                string why = Dice.Refuse(st, turf.world, b, amount);
                if (why.Length > 0) { s.Say(why, 2.5f); return; }
                bet = amount; Live = Dice.Open(st, turf.world, b, amount);
            }
            else if (Live.block != Table) return;
            bool onPoint = Live.point != 0;
            var t = Dice.Roll(st, turf.world, Live); Last = t; Throws++;
            s.cash = you.cash;
            switch (t.result)
            {
                case "win": s.Say($"{t.a} and {t.b}: {t.sum}. You take the pot: +${t.bet}.", 3.5f); break;
                case "lose": s.Say(onPoint ? $"{t.a} and {t.b}. Seven out. -${t.bet}." : $"{t.a} and {t.b}: {t.sum}. Craps. -${t.bet}.", 3.5f); break;
                default: s.Say($"{t.a} and {t.b}: {t.sum}. Point is {t.point}. Roll again.", 3f); break;
            }
            if (t.result == "win" || t.result == "lose") { Live = null; LastEnd = t.result; }
            if (games.TryGetValue(Table, out var g)) Toss(g, t.a, t.b);
        }

        // ---- the props ----

        void Props(TurfWar turf, Vector3 p)
        {
            foreach (var b in turf.State.blocks)
            {
                bool want = template != null && Dice.Runs(b) && b.owner == Turf.You && Spot(b.id, out var at) && Flat(p, at) < 70f;
                bool have = games.TryGetValue(b.id, out var g) && g.root != null;
                if (want && !have)
                {
                    Spot(b.id, out var at2);
                    var root = Instantiate(template, at2, Quaternion.Euler(0f, yaws.TryGetValue(b.id, out float yaw) ? yaw : b.id * 40f, 0f), transform);
                    root.name = $"Dice Game {b.id}"; root.SetActive(true);
                    g = new Game { root = root };
                    for (int i = 0; i < 2; i++) { g.dice[i] = root.transform.Find($"Die {i + 1}"); if (g.dice[i] != null) { g.rest[i] = g.dice[i].localPosition; g.end[i] = g.dice[i].localRotation; } }
                    games[b.id] = g;
                }
                else if (!want && have) { Destroy(g.root); games.Remove(b.id); }
            }
        }

        /// <summary>Which way a die's face points in its own frame (1 up, 6 down, 2 and 5 front and back, 3 and 4 the sides).</summary>
        public static Vector3 Face(int n) => n == 1 ? Vector3.up : n == 6 ? Vector3.down : n == 2 ? Vector3.forward : n == 5 ? Vector3.back : n == 3 ? Vector3.right : Vector3.left;

        void Toss(Game g, int a, int b)
        {
            int[] show = { a, b };
            for (int i = 0; i < 2; i++)
            {
                if (g.dice[i] == null) continue;
                // thrown in low from the open side toward the backstop; they land a little differently every time
                g.from[i] = new Vector3(-0.25f + i * 0.5f, 0.55f, 1.25f);
                g.rest[i] = new Vector3(Random.Range(-0.3f, 0.3f), g.rest[i].y, Random.Range(-0.45f, -0.1f));
                if (i == 1 && Vector3.Distance(g.rest[1], g.rest[0]) < 0.18f) g.rest[1].x += 0.22f;
                g.end[i] = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Quaternion.FromToRotation(Face(show[i]), Vector3.up);
            }
            g.t = 0f;
        }

        const float TossTime = 0.55f;
        static void Animate(Game g, float dt)
        {
            if (g.root == null || g.t >= TossTime) return;
            g.t += dt; float k = Mathf.Clamp01(g.t / TossTime);
            for (int i = 0; i < 2; i++)
            {
                var d = g.dice[i]; if (d == null) continue;
                var at = Vector3.Lerp(g.from[i], g.rest[i], k); at.y += Mathf.Sin(k * Mathf.PI) * 0.3f;
                d.localPosition = at;
                d.localRotation = k >= 1f ? g.end[i] : g.end[i] * Quaternion.Euler((1f - k) * 900f, (1f - k) * 500f * (i == 0 ? 1f : -1f), 0f);
            }
        }

        // ---- the bet buttons ----

        void Show(bool on, TurfWar turf = null)
        {
            if (on && panel == null && turf != null && turf.prompt != null) BuildPanel(turf.prompt);
            if (panel == null) return;
            if (panel.activeSelf != on) panel.SetActive(on);
            if (!on) return;
            bool live = Live != null;
            foreach (var b in betButtons) if (b.activeSelf == live) b.SetActive(!live);
            if (rollButton.activeSelf != live) rollButton.SetActive(live);
        }

        void BuildPanel(Text like)
        {
            panel = new GameObject("Dice Bets", typeof(RectTransform)); panel.transform.SetParent(like.transform.parent, false);
            var pr = (RectTransform)panel.transform; pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0f); pr.anchoredPosition = new Vector2(0f, 330f); pr.sizeDelta = new Vector2(640f, 76f);
            for (int i = 0; i < Dice.Bets.Length; i++)
            {
                int amount = Dice.Bets[i];
                betButtons.Add(MakeButton(like, "$" + amount, new Vector2((i - 1.5f) * 156f, 0f), new Vector2(140f, 72f), () => Tap(amount)));
            }
            rollButton = MakeButton(like, "ROLL", Vector2.zero, new Vector2(260f, 76f), () => Tap(0));
            rollButton.SetActive(false);
        }

        GameObject MakeButton(Text like, string label, Vector2 at, Vector2 size, UnityEngine.Events.UnityAction tap)
        {
            var go = new GameObject("Bet " + label, typeof(RectTransform)); go.transform.SetParent(panel.transform, false);
            var rt = (RectTransform)go.transform; rt.anchoredPosition = at; rt.sizeDelta = size;
            var img = go.AddComponent<Image>(); img.color = new Color(0.07f, 0.06f, 0.05f, 0.82f);
            var btn = go.AddComponent<Button>(); btn.targetGraphic = img; btn.onClick.AddListener(tap);
            var tg = new GameObject("Label", typeof(RectTransform)); tg.transform.SetParent(go.transform, false);
            var tr = (RectTransform)tg.transform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
            var t = tg.AddComponent<Text>(); t.font = like.font; t.fontSize = 30; t.fontStyle = FontStyle.Bold; t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color32(0xff, 0xcc, 0x66, 255); t.raycastTarget = false; t.text = label;
            return go;
        }
    }
}
