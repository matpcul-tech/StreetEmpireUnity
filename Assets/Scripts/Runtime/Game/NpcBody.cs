// Shared on-foot body for officers and pedestrians: CharacterController movement with gravity,
// turning toward where it walks, the locomotion blend driven by speed, and the knockdown clip.
using UnityEngine;

namespace StreetEmpire.Game
{
    [RequireComponent(typeof(CharacterController))]
    public class NpcBody : MonoBehaviour
    {
        public Animator animator;
        public Transform model;
        CharacterController cc; float vy, animSpeed;
        static readonly int SpeedId = Animator.StringToHash("Speed"), GroundedId = Animator.StringToHash("Grounded");
        public CharacterController Controller => cc != null ? cc : cc = GetComponent<CharacterController>();
        public float Speed { get; private set; }

        public void Move(Vector3 planarVelocity, float dt)
        {
            var c = Controller;
            if (!c.enabled) return;
            if (c.isGrounded && vy < 0) vy = -2f;
            vy += -20f * dt;
            if (planarVelocity.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(planarVelocity), 600f * dt);
            c.Move((planarVelocity + Vector3.up * vy) * dt);
            var v = c.velocity; Speed = new Vector2(v.x, v.z).magnitude;
            if (animator)
            {
                animSpeed = Mathf.MoveTowards(animSpeed, Speed, 30f * dt);
                animator.SetFloat(SpeedId, animSpeed); animator.SetBool(GroundedId, true);
            }
        }

        public void Face(Vector3 at)
        {
            var d = at - transform.position; d.y = 0;
            if (d.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(d);
        }

        public void Teleport(Vector3 p) { var c = Controller; c.enabled = false; transform.position = p; c.enabled = true; vy = 0; }

        /// <summary>Knocked down: the Death01 fall clip (CharacterAnim Down), stand back up after.</summary>
        public void SetDown(bool down)
        {
            CharacterAnim.SetDown(animator, down);
            if (animator) animator.SetFloat(SpeedId, 0);
            animSpeed = 0;
        }
    }
}
