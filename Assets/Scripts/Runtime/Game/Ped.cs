// A pedestrian on the Eastside sidewalks (pedStep in state.js, cut down): strolls back and forth,
// runs from the player when hit, goes down at 0 hp and gets back up with 30 hp after 5 s.
using UnityEngine;

namespace StreetEmpire.Game
{
    [RequireComponent(typeof(NpcBody), typeof(Health))]
    public class Ped : MonoBehaviour
    {
        public float speed = 1.3f, fleeSpeed = 4.5f, strollRange = 10f;
        public Vector3 along = Vector3.forward;   // the sidewalk direction it strolls on
        NpcBody body; Health health; Vector3 home; float dir = 1, flee, downT; Transform from;

        void Awake()
        {
            body = GetComponent<NpcBody>(); health = GetComponent<Health>(); home = transform.position;
            health.Damaged += (_, src) => { flee = 8; from = src != null ? src.transform : null; };
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (health.Down)
            {
                downT += dt; body.SetDown(true);
                if (downT > 5) { downT = 0; health.Restore(30); body.SetDown(false); flee = 8; }
                return;
            }
            Vector3 v;
            if (flee > 0 && from != null)
            {
                flee -= dt;
                v = transform.position - from.position; v.y = 0; v = v.normalized * fleeSpeed;
            }
            else
            {
                // stroll along the sidewalk: the long axis of where it was placed
                if (Vector3.Dot(transform.position - home, along * dir) > strollRange) dir = -dir;
                v = along * dir * speed;
            }
            body.Move(v, dt);
        }
    }
}
