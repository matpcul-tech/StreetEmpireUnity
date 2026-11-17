// Stands the player's upper body up while it walks and idles, like NpcBody does for the peds: the CC0 UAL idle
// and walk carry a forward hunch (about 9 and 12 deg hips to neck) that read as a stoop on the Meshy player.
// After the Animator poses the bones, the spine is turned back so the average lean is `uprightLean` at a walk,
// easing toward `runLean` at a sprint (a run leans forward). Only in the Locomotion state; jumps, punches and
// the knockdown play as authored.
using StreetEmpire.Game;
using UnityEngine;

namespace StreetEmpire.Player
{
    public class UprightBody : MonoBehaviour
    {
        public Animator animator;
        public float uprightLean = 3f, runLean = 14f;
        /// <summary>Ground speed, set by the owner each frame.</summary>
        public float speed;
        public float Lean { get; private set; }
        Transform spine; Quaternion corrected; bool hasCorrected, hasAvg; float avgLean, weight;
        static readonly int Locomotion = Animator.StringToHash("Locomotion");

        void Start()
        {
            if (animator != null && animator.isHuman) spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        }

        void LateUpdate()
        {
            if (spine == null) return;
            // a culled Animator leaves last frame's corrected pose; turning it again would add up
            if (hasCorrected && spine.localRotation == corrected) return;
            var st = animator.GetCurrentAnimatorStateInfo(0);
            bool on = st.shortNameHash == Locomotion && !animator.IsInTransition(0);
            weight = Mathf.MoveTowards(weight, on ? 1f : 0f, Time.deltaTime / 0.2f);
            float raw = ShotDirector.Lean(animator, transform);
            avgLean = hasAvg ? Mathf.Lerp(avgLean, raw, 1f - Mathf.Exp(-Time.deltaTime / 0.5f)) : raw; hasAvg = true;
            float target = Mathf.Lerp(uprightLean, runLean, Mathf.InverseLerp(2.5f, 9f, speed));
            float turn = Mathf.Clamp(avgLean - target, -5f, 35f) * weight;
            if (Mathf.Abs(turn) > 0.1f)
            {
                spine.rotation = Quaternion.AngleAxis(-turn, transform.right) * spine.rotation;
                // the hips to spine segment keeps part of the lean: turn the rest of the way
                float r = (raw - ShotDirector.Lean(animator, transform)) / turn;
                if (Mathf.Abs(turn) > 1f && r > 0.3f && r < 1f)
                    spine.rotation = Quaternion.AngleAxis(-Mathf.Min(turn * (1f / r - 1f), 20f), transform.right) * spine.rotation;
            }
            corrected = spine.localRotation; hasCorrected = true;
            Lean = ShotDirector.Lean(animator, transform);
        }
    }
}
