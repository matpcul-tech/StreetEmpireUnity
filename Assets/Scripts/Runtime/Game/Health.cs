// Hit points for the player, officers and pedestrians. Whoever deals the damage is passed along so the
// heat rules (hitPed, hitCop, koPed, koCop in police.json) can be applied by the session.
using System;
using UnityEngine;

namespace StreetEmpire.Game
{
    public class Health : MonoBehaviour
    {
        public float max = 100, hp = 100;
        public bool Down => hp <= 0;
        public event Action<float, GameObject> Damaged;   // amount, source
        public event Action<GameObject> KnockedOut;       // source

        public void Damage(float amount, GameObject source)
        {
            if (Down || amount <= 0) return;
            hp = Mathf.Max(0, hp - amount);
            Damaged?.Invoke(amount, source);
            if (hp <= 0) KnockedOut?.Invoke(source);
            CombatFx.Report(this, amount, source);   // rings, numbers, sparks, dust
        }

        public void Restore(float to = -1) => hp = to < 0 ? max : Mathf.Min(max, to);
    }
}
