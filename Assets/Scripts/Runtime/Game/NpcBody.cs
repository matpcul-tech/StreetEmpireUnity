// Shared on-foot body for officers and pedestrians: CharacterController movement with gravity, smooth
// turning toward where it walks, the locomotion blend matched to the real speed, and the knockdown clip.
using UnityEngine;

namespace StreetEmpire.Game
{
    [RequireComponent(typeof(CharacterController))]
    public class NpcBody : MonoBehaviour
    {
        public Animator animator;
        public Transform model;
        /// <summary>Ground speed of the CC0 UAL loops on a 1.75 m body (measured from the clips' foot contacts:
        /// walk about 1.2 m/s, jog about 3.1 m/s). The blend thresholds in Player.controller are 1.8, 5 and 9.</summary>
        public float walkClipSpeed = 1.2f, jogClipSpeed = 3.1f, sprintClipSpeed = 6f;
        /// <summary>How far the model's rest pose leans forward (degrees, measured from hips to neck when the
        /// scene is built). Humanoid retargeting treats the rest pose as straight, so the Meshy ped rigs (8 to
        /// 11 degrees bent in their rest pose) walked hunched over. Kept for reference; LateUpdate measures the
        /// posed lean every frame instead.</summary>
        public float restLean;
        public float turnRate = 300f;
        CharacterController cc; float vy, animParam; Transform spine;
        static readonly int SpeedId = Animator.StringToHash("Speed"), GroundedId = Animator.StringToHash("Grounded");
        public CharacterController Controller => cc != null ? cc : cc = GetComponent<CharacterController>();
        public float Speed { get; private set; }
        public bool IsDown { get; private set; }
        /// <summary>Upper body lean (hips to neck, degrees, positive forward) as drawn this frame.</summary>
        public float Lean { get; private set; }

        void Awake()
        {
            if (animator != null && animator.isHuman) spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        }

        public void Move(Vector3 planarVelocity, float dt)
        {
            var c = Controller;
            if (!c.enabled) return;
            if (c.isGrounded && vy < 0) vy = -2f;
            vy += -20f * dt;
            if (planarVelocity.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(planarVelocity), turnRate * dt);
            c.Move((planarVelocity + Vector3.up * vy) * dt);
            var v = c.velocity; Speed = new Vector2(v.x, v.z).magnitude;
            if (animator)
            {
                // a full walk cycle played slower or faster to match the feet, instead of a walk half blended
                // with idle (feet barely moving while the body slides)
                float target = BlendFor(Speed, out float playRate);
                animParam = Mathf.MoveTowards(animParam, target, 12f * dt);
                animator.SetFloat(SpeedId, animParam); animator.SetBool(GroundedId, true);
                animator.speed = Mathf.MoveTowards(animator.speed, playRate, 3f * dt);
            }
        }

        /// <summary>Blend parameter and playback rate for a ground speed.</summary>
        public float BlendFor(float speed, out float playRate)
        {
            playRate = 1f;
            if (speed < 0.15f) return 0f;
            if (speed <= walkClipSpeed * 1.3f) { playRate = Mathf.Clamp(speed / walkClipSpeed, 0.6f, 1.3f); return 1.8f; }
            if (speed <= jogClipSpeed) return Mathf.Lerp(1.8f, 5f, (speed - walkClipSpeed * 1.3f) / (jogClipSpeed - walkClipSpeed * 1.3f));
            return Mathf.Lerp(5f, 9f, Mathf.Clamp01((speed - jogClipSpeed) / (sprintClipSpeed - jogClipSpeed)));
        }

        /// <summary>Average forward lean the upper body is brought to (degrees). The UAL walk itself carries
        /// about 12 degrees of torso tilt on top of the rig's rest lean; the stride's own sway is kept.</summary>
        public float uprightLean = 3f;
        Quaternion corrected; bool hasCorrected, hasAvg; float avgLean;

        void LateUpdate()
        {
            // After the Animator has posed the bones, turn the spine back so the upper body's average lean
            // is uprightLean. Only on frames the Animator wrote the spine: a culled Animator
            // (CullUpdateTransforms, off screen or no camera) leaves last frame's corrected pose, and turning
            // it again would add up.
            if (spine != null && !IsDown && !(hasCorrected && spine.localRotation == corrected))
            {
                float raw = ShotDirector.Lean(animator, transform);
                avgLean = hasAvg ? Mathf.Lerp(avgLean, raw, 1f - Mathf.Exp(-Time.deltaTime / 0.5f)) : raw; hasAvg = true;
                float turn = Mathf.Clamp(avgLean - uprightLean, -5f, 35f);
                spine.rotation = Quaternion.AngleAxis(-turn, transform.right) * spine.rotation;
                corrected = spine.localRotation; hasCorrected = true;
            }
            Lean = ShotDirector.Lean(animator, transform);
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
            IsDown = down;
            CharacterAnim.SetDown(animator, down);
            if (animator) { animator.SetFloat(SpeedId, 0); animator.speed = 1f; }
            animParam = 0; hasAvg = false;
        }
    }
}
