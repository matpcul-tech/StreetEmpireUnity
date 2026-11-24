// One of the people on a corner (BlockLife): a townie body that stays where it is.
using UnityEngine;

namespace StreetEmpire.Game
{
    /// <summary>Stands in a group: faces its middle, and turns to look at the player when they come close.</summary>
    [RequireComponent(typeof(NpcBody))]
    public class Loiterer : MonoBehaviour
    {
        public Vector3 center; public float noticeRange = 4.5f, turnRate = 160f;
        NpcBody body; Health hp;

        void Awake() { body = GetComponent<NpcBody>(); hp = GetComponent<Health>(); }

        void Update()
        {
            if (hp != null && hp.Down) return;
            body.Move(Vector3.zero, Time.deltaTime);   // gravity and the idle blend
            var s = GameSession.Instance; var to = center - transform.position;
            if (s != null && s.player != null) { var tp = s.player.transform.position - transform.position; if (tp.magnitude < noticeRange) to = tp; }
            to.y = 0; if (to.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), turnRate * Time.deltaTime);
        }
    }
}
