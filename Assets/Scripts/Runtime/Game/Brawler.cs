// A street brawler for the opening scuffle, the way engage() in state.js runs them: rush closes at 5.3 m/s,
// flank steps 2.6 m to the side first at 4.3; a tell (wind up) of 0.36 s for rush and 0.46 s for flank (a bat
// adds 0.08) with a short lunge, then 8 damage for fists or 14 for a bat within reach (1.85 m fists, 2.55 m bat)
// plus 0.45 m. Only the two nearest swing (strikeCap); the rest hold a ring 6.6 m out. Hitting them brings no heat.
using System.Collections.Generic;
using StreetEmpire.Player;
using UnityEngine;

namespace StreetEmpire.Game
{
    [RequireComponent(typeof(NpcBody), typeof(Health), typeof(HitReaction))]
    public class Brawler : MonoBehaviour
    {
        public enum Style { Rush, Flank }
        public Style style = Style.Rush;
        public bool bat, leave;
        public int Swings { get; private set; }
        public int Hits { get; private set; }
        public float Reach => bat ? 2.55f : 1.85f;
        public static readonly List<Brawler> All = new();

        NpcBody body; Health health; float windup, cd, downT, leaveT; bool flanked;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        void Awake() { body = GetComponent<NpcBody>(); health = GetComponent<Health>(); }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        void Update()
        {
            float dt = Time.deltaTime;
            var s = GameSession.Instance; if (s == null || s.player == null) return;
            if (health.Down) { windup = 0; downT += dt;   // HitReaction plays the fall
                if (downT > 6f) Destroy(gameObject); return; }
            var p = s.player.transform.position;
            var to = Flat(p - transform.position); float d = to.magnitude;
            if (leave || s.IsDown)
            {
                leaveT += dt; body.Move(Flat(transform.position - p).normalized * 3.2f, dt);
                if (leaveT > 6f || d > 45f) Destroy(gameObject);
                return;
            }
            var user = s.player.GetComponent<VehicleUser>();
            bool inCar = user != null && user.Car != null;
            cd -= dt;
            if (windup > 0)
            {
                windup -= dt; body.Face(p);
                body.Move(d > Reach * 0.7f ? to / Mathf.Max(d, 0.01f) * (style == Style.Rush ? 2.2f : 1.05f) : Vector3.zero, dt);
                if (windup <= 0)
                {
                    Swings++;
                    if (!inCar && d <= Reach + 0.45f) { s.playerHealth.Damage(bat ? 14f : 8f, gameObject); Hits++; }
                }
                return;
            }
            if (inCar) { body.Move(Vector3.zero, dt); return; }
            if (!Aggressor(p))
            {
                // hold a ring round the fight until one of the two in front goes down
                var ring = p + Flat(transform.position - p).normalized * 6.6f;
                var go = Flat(ring - transform.position);
                body.Move(go.magnitude > 0.5f ? go.normalized * 3.6f : Vector3.zero, dt);
                if (go.magnitude <= 0.5f) body.Face(p);
                return;
            }
            var dest = p;
            if (style == Style.Flank && d > Reach + 0.15f && !flanked)
            {
                var side = Vector3.Cross(Vector3.up, to / Mathf.Max(d, 0.01f)) * (GetInstanceID() % 2 == 0 ? 1f : -1f);
                dest = p + side * 2.6f;
                if (Flat(dest - transform.position).magnitude < 1.25f) flanked = true;
            }
            if (dest == p && d <= Reach && cd <= 0)
            {
                windup = (bat ? 0.08f : 0f) + (style == Style.Rush ? 0.36f : 0.46f);
                cd = windup + (bat ? 0.85f : 0.62f);
                CharacterAnim.Punch(body.animator, Swings % 2);
                body.Move(Vector3.zero, dt);
                return;
            }
            var step = Flat(dest - transform.position);
            if (step.magnitude > 0.45f) body.Move(step.normalized * (dest == p ? (style == Style.Rush ? 5.3f : 4.3f) : 4.2f), dt);
            else { body.Face(p); body.Move(Vector3.zero, dt); }
        }

        /// <summary>One of the two nearest standing brawlers (only they close in and swing).</summary>
        bool Aggressor(Vector3 p)
        {
            float mine = Flat(transform.position - p).sqrMagnitude; int nearer = 0;
            foreach (var b in All)
            {
                if (b == this || b.leave || b.health == null || b.health.Down) continue;
                if (Flat(b.transform.position - p).sqrMagnitude < mine) nearer++;
            }
            return nearer < 2;
        }
    }
}
