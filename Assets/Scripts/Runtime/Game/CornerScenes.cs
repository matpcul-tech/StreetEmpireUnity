// The other two corner scenes from the web build (hustle.js): every block's corner is a dice game, a card table or
// a boombox on a crate, by block id. The dice game is StreetDice's; this lays out the other two beside each block's
// corner group. Props only, as on the web: nobody deals you in. The boombox speakers thump.
using System.Collections.Generic;
using StreetEmpire.Sim;
using UnityEngine;

namespace StreetEmpire.Game
{
    public class CornerScenes : MonoBehaviour
    {
        public static CornerScenes Instance { get; private set; }
        /// <summary>Built by SceneBuilder.CardsTemplate and StoopTemplate, kept inactive under the map.</summary>
        public GameObject cards, stoop;
        /// <summary>How near the player has to be for a corner's props to exist (meters).</summary>
        public float near = 70f;

        readonly Dictionary<int, GameObject> live = new();
        readonly List<Transform> speakers = new();
        float scanT;

        public IEnumerable<GameObject> Scenes => live.Values;
        /// <summary>'dice', 'cards' or 'stoop': sceneOf(b) on the web.</summary>
        public static string Kind(int blockId) => (blockId % 3) switch { 0 => "dice", 1 => "cards", _ => "stoop" };

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            var s = GameSession.Instance; var turf = TurfWar.Instance; var dice = StreetDice.Instance;
            if (!StreetDice.AutoStart || s == null || s.player == null || turf == null || turf.State == null || dice == null) return;
            if ((scanT -= Time.deltaTime) <= 0f)
            {
                scanT = 0.5f; var p = s.player.transform.position; bool changed = false;
                foreach (var b in turf.State.blocks)
                {
                    string kind = Kind(b.id); var template = kind == "cards" ? cards : kind == "stoop" ? stoop : null;
                    bool want = template != null && dice.Spot(b.id, out var at) && new Vector2(p.x - at.x, p.z - at.z).magnitude < near;
                    bool have = live.TryGetValue(b.id, out var go) && go != null;
                    if (want && !have)
                    {
                        dice.Spot(b.id, out var here);
                        go = Instantiate(template, here, Quaternion.Euler(0f, dice.Yaw(b.id), 0f), transform);
                        go.name = (kind == "cards" ? "Card Game " : "Stoop ") + b.id; go.SetActive(true);
                        live[b.id] = go; changed = true;
                    }
                    else if (!want && have) { Destroy(go); live.Remove(b.id); changed = true; }
                }
                if (changed)
                {
                    speakers.Clear();
                    foreach (var go in live.Values)
                        if (go != null) foreach (var t in go.GetComponentsInChildren<Transform>()) if (t.name.StartsWith("Speaker")) speakers.Add(t);
                }
            }
            // the speakers thump, about two beats a second
            float k = 1f + 0.12f * Mathf.Max(0f, Mathf.Sin(Time.time * 12.6f));
            foreach (var t in speakers) if (t != null) t.localScale = new Vector3(SpeakerSize * k, t.localScale.y, SpeakerSize * k);
        }

        /// <summary>A speaker cone's width (the template's cylinders, lying on their side).</summary>
        public const float SpeakerSize = 0.2f;
    }
}
