// On-foot movement: camera relative, jog 5 m/s and sprint 9 m/s like movePlayer() in state.js.
using UnityEngine;

namespace StreetEmpire.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        public float walkSpeed = 1.8f, jogSpeed = 5f, sprintSpeed = 9f;
        public float turnSpeed = 720f, jumpHeight = 1.1f, gravity = -20f;
        public Animator animator;

        // scripted input for tests and cut scenes; null means read the player's controls
        public Vector2? scriptMove;
        public bool scriptSprint, scriptJump;
        public bool inputEnabled = true;

        public bool Grounded => cc != null && cc.isGrounded;
        public float PlanarSpeed { get; private set; }
        public int Jumps { get; private set; }

        CharacterController cc;
        float vy, animSpeed;
        static readonly int SpeedId = Animator.StringToHash("Speed"), GroundedId = Animator.StringToHash("Grounded"), JumpId = Animator.StringToHash("Jump");

        UprightBody upright;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            // added at load, not saved in the scene
            if (animator != null) { upright = GetComponent<UprightBody>() ?? gameObject.AddComponent<UprightBody>(); upright.animator = animator; }
        }

        void Update()
        {
            var c = PlayerControls.Instance;
            Vector2 m = scriptMove ?? (inputEnabled ? c.Move : Vector2.zero);
            bool sprint = scriptSprint || (inputEnabled && c.Sprint);
            bool jump = scriptJump || (inputEnabled && c.JumpPressed);
            scriptJump = false;

            var cam = Camera.main != null ? Camera.main.transform : transform;
            var f = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
            f = f.sqrMagnitude < 1e-4f ? transform.forward : f.normalized;
            var r = Vector3.Cross(Vector3.up, f);
            var dir = f * m.y + r * m.x;
            float mag = Mathf.Clamp01(dir.magnitude);
            float target = mag < 0.08f ? 0f : sprint ? sprintSpeed : Mathf.Lerp(walkSpeed, jogSpeed, Mathf.InverseLerp(0.2f, 0.85f, mag));
            var planar = mag < 0.08f ? Vector3.zero : dir.normalized * target;
            if (planar.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(planar), turnSpeed * Time.deltaTime);

            if (cc.isGrounded)
            {
                if (vy < 0) vy = -2f;
                if (jump) { vy = Mathf.Sqrt(2f * -gravity * jumpHeight); Jumps++; if (animator) animator.SetTrigger(JumpId); }
            }
            vy += gravity * Time.deltaTime;
            cc.Move((planar + Vector3.up * vy) * Time.deltaTime);
            var v = cc.velocity; PlanarSpeed = new Vector2(v.x, v.z).magnitude;

            if (animator)
            {
                animSpeed = Mathf.MoveTowards(animSpeed, PlanarSpeed, 30f * Time.deltaTime);
                animator.SetFloat(SpeedId, animSpeed);
                if (upright) upright.speed = PlanarSpeed;
                animator.SetBool(GroundedId, cc.isGrounded);
            }
        }

        /// <summary>Move the player somewhere (respawn, leave a car) without the controller fighting it.</summary>
        public void Teleport(Vector3 pos, float yaw)
        {
            cc.enabled = false; transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0)); cc.enabled = true; vy = 0;
        }
    }
}
