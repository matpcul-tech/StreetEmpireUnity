// An officer on foot, from the "officers on foot" part of policeStep() in state.js: runs at 6 m/s,
// winds up 0.32 s and hits for 9 when within 2.25 m, waits 1.15 s between swings, drags you out of a car
// that sits still within 3 m, and walks off when the stars are gone or after a bust.
using StreetEmpire.Player;
using UnityEngine;

namespace StreetEmpire.Game
{
    [RequireComponent(typeof(NpcBody), typeof(Health))]
    public class Cop : MonoBehaviour
    {
        public float runSpeed = 6f, reach = 1.9f, hitRange = 2.25f, windupTime = 0.32f, cooldown = 1.15f, damage = 9f;
        public bool leave;
        public int Swings { get; private set; }

        NpcBody body; Health health; float windup, cd, leaveT, stuckT, sideT; Vector3 lastPos; float sideSign = 1;

        void Awake() { body = GetComponent<NpcBody>(); health = GetComponent<Health>(); lastPos = transform.position; }

        void Update()
        {
            float dt = Time.deltaTime;
            var s = GameSession.Instance;
            if (s == null || s.player == null) return;
            if (health.Down) { body.SetDown(true); leaveT += dt; if (leaveT > 6) Destroy(gameObject); return; }
            cd -= dt;
            var p = s.player.transform;
            if (leave || s.Stars == 0)
            {
                windup = 0; leaveT += dt;
                var away = transform.position - p.position; away.y = 0;
                body.Move(away.normalized * 4f, dt);
                if (leaveT > 3f || away.magnitude > 70f) Destroy(gameObject);
                return;
            }
            leaveT = 0;
            var user = s.player.GetComponent<VehicleUser>();
            var car = user != null ? user.Car : null;
            Vector3 target = car != null ? car.transform.position : p.position;
            var to = target - transform.position; to.y = 0; float d = to.magnitude;

            if (car != null && d < 3f && Mathf.Abs(car.Speed) < 2f) { user.Exit(); s.Say("The cops dragged you out of the car.", 2.5f); return; }
            if (windup > 0)
            {
                windup -= dt; body.Face(target); body.Move(Vector3.zero, dt);
                if (windup <= 0)
                {
                    Swings++;
                    var pp = p.position - transform.position; pp.y = 0;
                    if (car == null && pp.magnitude < hitRange) s.playerHealth.Damage(damage, gameObject);
                }
                return;
            }
            if (d < reach && car == null)
            {
                body.Face(target); body.Move(Vector3.zero, dt);
                if (cd <= 0) { windup = windupTime; cd = cooldown; }
                return;
            }
            // run at the player; when a wall holds it up, step to one side for a moment
            var dir = to / Mathf.Max(d, 0.01f);
            if (sideT > 0) { sideT -= dt; dir = (dir + Vector3.Cross(Vector3.up, dir) * sideSign * 1.6f).normalized; }
            body.Move(dir * runSpeed, dt);
            float moved = (transform.position - lastPos).magnitude; lastPos = transform.position;
            stuckT = moved < runSpeed * dt * 0.3f ? stuckT + dt : 0;
            if (stuckT > 0.35f) { stuckT = 0; sideT = 0.9f; sideSign = -sideSign; }
        }
    }
}
