// Fists, from weapons.json: 2.7 m reach, 18 damage, 0.34 s between punches. Hits the nearest body in
// front of the player. Hitting people raises heat the way damage() does in state.js.
using StreetEmpire.Game;
using UnityEngine;

namespace StreetEmpire.Player
{
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerCombat : MonoBehaviour
    {
        public float range = 2.7f, damage = 18f, cooldown = 0.34f;
        public bool scriptAttack;
        public int Punches { get; private set; }
        float cd; VehicleUser user;

        void Awake() => user = GetComponent<VehicleUser>();

        void Update()
        {
            cd -= Time.deltaTime;
            bool attack = scriptAttack || PlayerControls.Instance.AttackPressed; scriptAttack = false;
            var s = GameSession.Instance;
            if (!attack || cd > 0 || (user != null && user.Car != null) || (s != null && s.IsDown)) return;
            cd = cooldown; Punches++;
            Health best = null; float bd = range * range;
            foreach (var h in FindObjectsByType<Health>(FindObjectsSortMode.None))
            {
                if (h.gameObject == gameObject || h.Down) continue;
                var to = h.transform.position - transform.position; to.y = 0;
                if (to.sqrMagnitude > bd) continue;
                if (to.sqrMagnitude > 0.5f && Vector3.Dot(transform.forward, to.normalized) < 0.2f) continue;
                bd = to.sqrMagnitude; best = h;
            }
            if (best == null) return;
            var face = best.transform.position - transform.position; face.y = 0;
            if (face.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(face);
            bool cop = best.GetComponent<Cop>() != null, ped = best.GetComponent<Ped>() != null;
            best.Damage(damage, gameObject);
            if (s == null) return;
            if (cop) { s.AddHeat("hitCop"); if (best.Down) s.AddHeat("koCop"); }
            else if (ped) { s.AddHeat("hitPed"); if (best.Down) s.AddHeat("koPed"); }
        }
    }
}
