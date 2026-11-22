// One of your own guards on a block you hold (TurfWar puts him there). He stands at his post; the turf sim rolls
// for him when the block is raided.
using UnityEngine;

namespace StreetEmpire.Game
{
    /// <summary>One of your own guards: stands at his post on a block you hold. He does not fight as a body (the sim rolls for him).</summary>
    public class TurfGuard : MonoBehaviour
    {
        public Vector3 post; public Vector3 look;
        NpcBody body; Health health; float downT;
        void Awake() { body = GetComponent<NpcBody>(); health = GetComponent<Health>(); }
        void Update()
        {
            float dt = Time.deltaTime;
            if (health != null && health.Down) { downT += dt; if (downT > 6f) Destroy(gameObject); return; }
            if (body == null) return;
            var back = post - transform.position; back.y = 0;
            if (back.magnitude > 0.7f) body.Move(back.normalized * 2.6f, dt);
            else { body.Face(transform.position + look); body.Move(Vector3.zero, dt); }
        }
    }
}
