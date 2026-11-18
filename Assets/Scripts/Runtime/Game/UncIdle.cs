// Unc stands at his spot: upright idle (NpcBody straightens the Meshy rig's forward lean, which had him folded
// over), turns to face the player when they come close, and his wider capsule stops the player about a meter
// off instead of letting them walk into his chest.
using UnityEngine;

namespace StreetEmpire.Game
{
    [RequireComponent(typeof(NpcBody))]
    public class UncIdle : MonoBehaviour
    {
        public float faceRange = 6f, turnRate = 200f;
        NpcBody body; Quaternion home;

        void Awake() { body = GetComponent<NpcBody>(); home = transform.rotation; }

        void Update()
        {
            var s = GameSession.Instance;
            body.Move(Vector3.zero, Time.deltaTime);   // gravity and the idle blend
            Quaternion want = home;
            if (s != null && s.player != null)
            {
                var to = s.player.transform.position - transform.position; to.y = 0;
                if (to.magnitude < faceRange && to.sqrMagnitude > 0.01f) want = Quaternion.LookRotation(to);
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnRate * Time.deltaTime);
        }
    }
}
