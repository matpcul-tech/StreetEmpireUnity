// Shared on-foot body for officers and pedestrians: CharacterController movement with gravity,
// turning toward where it walks, the locomotion blend driven by speed, and a knocked-down pose.
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

        /// <summary>Lie flat while knocked down (no fall clip yet), stand back up after.</summary>
        public void SetDown(bool down)
        {
            if (model) model.localRotation = down ? Quaternion.Euler(-90, 0, 0) : Quaternion.identity;
            if (model) model.localPosition = down ? new Vector3(0, 0.25f, 0) : Vector3.zero;
            if (animator) animator.SetFloat(SpeedId, 0);
        }
    }
}
