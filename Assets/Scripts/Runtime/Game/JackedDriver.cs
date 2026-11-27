// The driver of a car the player took out of traffic (Carjack): put out on the street, he runs.
using UnityEngine;

namespace StreetEmpire.Game
{
    /// <summary>Runs from whoever took the car, then is gone once nobody is near to see.</summary>
    public class JackedDriver : MonoBehaviour
    {
        public Transform from;
        public float runSpeed = 4.5f, runFor = 9f;
        NpcBody body; Vector3 vel; float t;

        void Start() => body = GetComponent<NpcBody>();

        void Update()
        {
            float dt = Time.deltaTime; t += dt;
            if (from == null) { Destroy(gameObject); return; }
            Vector3 away = transform.position - from.position; away.y = 0f;
            if (t > 30f || (t > runFor && away.sqrMagnitude > 55f * 55f)) { Destroy(gameObject); return; }
            Vector3 want = away.sqrMagnitude > 0.01f ? away.normalized * (t < runFor ? runSpeed : 1.3f) : Vector3.zero;
            vel = Vector3.MoveTowards(vel, want, 12f * dt);
            if (body != null) body.Move(vel, dt);
        }
    }
}
