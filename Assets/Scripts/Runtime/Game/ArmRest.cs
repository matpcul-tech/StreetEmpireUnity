// Arms at rest. Standing still, the pack's idle read through Unity's humanoid sweeps the upper arms back behind the
// body (measured on the player: 14 and 35 degrees back, 25 out to the side) with the elbows bent 28 and 43 degrees,
// so everyone stood like a man about to draw (phone, build 69ec286: "the arms still backwards"). After the Animator
// has posed the bones, each upper arm is brought back to hang beside the body and the elbow is eased open. Only
// standing or near it: the walk and run swing as authored. Every step moves toward a limit and stops there, so a
// frame the Animator did not write (a culled body) cannot add up.
using UnityEngine;

namespace StreetEmpire.Game
{
    public static class ArmRest
    {
        public static bool On = true;
        /// <summary>The most an upper arm may stand back of straight down and out to the side at rest, in degrees,
        /// and the most the elbow stays bent.</summary>
        public const float MaxBack = 5f, MaxOut = 11f, MaxElbow = 16f;
        /// <summary>Full effect standing, none from this ground speed up.</summary>
        public const float FadeSpeed = 1.2f;
        static readonly int Locomotion = Animator.StringToHash("Locomotion"), Speed = Animator.StringToHash("Speed");

        public static void Apply(Animator a)
        {
            if (!On || a == null || !a.isHuman || !a.isActiveAndEnabled || a.runtimeAnimatorController == null) return;
            if (a.GetCurrentAnimatorStateInfo(0).shortNameHash != Locomotion || a.IsInTransition(0)) return;
            float w = 1f - Mathf.Clamp01(a.GetFloat(Speed) / FadeSpeed);
            if (w <= 0.01f) return;
            Side(a, true, w); Side(a, false, w);
        }

        static void Side(Animator a, bool left, float w)
        {
            var sh = a.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
            var el = a.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            var hand = a.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            if (sh == null || el == null || hand == null) return;
            var t = a.transform; Vector3 outward = left ? -t.right : t.right;
            Vector3 upper = (el.position - sh.position).normalized;
            float o = Vector3.Dot(upper, outward), f = Vector3.Dot(upper, t.forward), d = Vector3.Dot(upper, -t.up);
            if (d > 0.2f)   // hanging, not raised: a raised arm is doing something
            {
                float o2 = Mathf.Min(o, Mathf.Sin(MaxOut * Mathf.Deg2Rad)), f2 = Mathf.Max(f, -Mathf.Sin(MaxBack * Mathf.Deg2Rad));
                if (o2 != o || f2 != f)
                {
                    float d2 = Mathf.Sqrt(Mathf.Max(0.01f, 1f - o2 * o2 - f2 * f2));
                    Vector3 want = outward * o2 + t.forward * f2 - t.up * d2;
                    sh.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(upper, want), w) * sh.rotation;
                }
            }
            upper = (el.position - sh.position).normalized;
            Vector3 fore = (hand.position - el.position).normalized;
            float bend = Vector3.Angle(upper, fore);
            if (bend > MaxElbow)
            {
                var axis = Vector3.Cross(fore, upper);
                if (axis.sqrMagnitude > 1e-6f) el.rotation = Quaternion.AngleAxis((bend - MaxElbow) * w, axis.normalized) * el.rotation;
            }
        }
    }
}
