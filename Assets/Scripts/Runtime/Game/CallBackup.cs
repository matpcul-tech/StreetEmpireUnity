// Crew that shows up (state.js callBackup): when the player steps into a street call, the nearest guard of his runs
// over and fights beside him, then walks back to his post when it is over. StreetCalls puts this on the guard for
// the length of the fight; his TurfGuard is switched off meanwhile and back on at the end.
using System.Collections.Generic;
using UnityEngine;

namespace StreetEmpire.Game
{
    public class CallBackup : MonoBehaviour
    {
        /// <summary>The men of the call he came to help with.</summary>
        public IReadOnlyList<Health> men;
        public float run = 8.5f, reach = 1.9f, damage = 8f, every = 0.9f;
        public int Hits { get; private set; }
        NpcBody body; Health health; TurfGuard guard; float cd; int swings;
        Vector3 stuckAt; float stuckT, sideT, side = 1f;

        void Awake() { body = GetComponent<NpcBody>(); health = GetComponent<Health>(); guard = GetComponent<TurfGuard>(); if (guard != null) guard.enabled = false; stuckAt = transform.position; }

        /// <summary>The call is over: back to the post.</summary>
        public void Release() { if (guard != null) guard.enabled = true; Destroy(this); }

        void Update()
        {
            float dt = Time.deltaTime;
            if (body == null || health == null || health.Down) { Release(); return; }
            Health target = null; float bd = float.MaxValue;
            if (men != null) foreach (var m in men)
            {
                if (m == null || m.Down) continue;
                float d = (m.transform.position - transform.position).sqrMagnitude;
                if (d < bd) { bd = d; target = m; }
            }
            if (target == null) { body.Move(Vector3.zero, dt); return; }
            Vector3 to = target.transform.position - transform.position; to.y = 0f; float dist = to.magnitude;
            cd -= dt;
            if (dist > reach)
            {
                // he runs straight at them; held up by a wall or a fence for a second, he goes round it
                Vector3 dir = to / Mathf.Max(dist, 0.01f);
                if ((stuckT += dt) >= 1f)
                {
                    if ((transform.position - stuckAt).magnitude < 0.6f) { sideT = 1.1f; side = -side; }
                    stuckT = 0f; stuckAt = transform.position;
                }
                if (sideT > 0f) { sideT -= dt; dir = (Vector3.Cross(Vector3.up, dir) * side + dir * 0.25f).normalized; }
                body.Move(dir * (dist > 6f ? run : 4.2f), dt); return;
            }
            body.Face(target.transform.position); body.Move(Vector3.zero, dt);
            if (cd <= 0f)
            {
                cd = every; CharacterAnim.Punch(body.animator, swings++ % 2);
                target.Damage(damage, gameObject); Hits++;
            }
        }
    }
}
