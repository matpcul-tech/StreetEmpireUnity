// Legs of every Humanoid in the street: straight at idle (no squat), feet on the ground, and a walk whose
// knees fold like a walk, not a lunge.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StreetEmpire.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class LegTests
    {
        /// <summary>Angle at the knee, upper leg to foot, in degrees (180 is straight).</summary>
        public static float Knee(Animator a, bool left)
        {
            var up = a.GetBoneTransform(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg).position;
            var kn = a.GetBoneTransform(left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg).position;
            var ft = a.GetBoneTransform(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot).position;
            return Vector3.Angle(up - kn, ft - kn);
        }

        static float Ground(Vector3 at) =>
            Physics.Raycast(at + Vector3.up * 1.0f, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : at.y;

        [UnityTest]
        public IEnumerator EveryoneStandsStraightAndWalksLikeAWalk()
        {
            StreetEmpire.Game.MuggerOpening.AutoStart = false; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var anims = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).Where(a => a.isHuman && a.isActiveAndEnabled).ToArray();
            foreach (var a in anims) a.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // no camera draws in batch mode
            var player = GameObject.FindWithTag("Player").GetComponentInChildren<Animator>();
            var unc = GameObject.Find("Unc")?.GetComponentInChildren<Animator>();
            var peds = Object.FindObjectsByType<Ped>(FindObjectsSortMode.None).Select(p => p.GetComponent<NpcBody>().animator).ToArray();
            Assert.IsNotNull(player); Assert.AreEqual(4, peds.Length);
            yield return new WaitForSeconds(2.5f);

            // idle: the player (no input) and Unc
            var idlers = new List<Animator> { player }; if (unc != null) idlers.Add(unc);
            var walk = new List<float>();
            float maxPlayerLean = -90, minIdleKnee = 180, minSole = 9, maxSole = -9, maxWidth = 0;
            for (int k = 0; k < 12; k++)
            {
                yield return new WaitForSeconds(0.25f);
                foreach (var a in idlers)
                {
                    minIdleKnee = Mathf.Min(minIdleKnee, Knee(a, true), Knee(a, false));
                    var l = a.GetBoneTransform(HumanBodyBones.LeftFoot).position; var r = a.GetBoneTransform(HumanBodyBones.RightFoot).position;
                    var w = l - r; w.y = 0; maxWidth = Mathf.Max(maxWidth, w.magnitude);
                    float g = Ground(a.transform.position);
                    foreach (var toe in new[] { HumanBodyBones.LeftToes, HumanBodyBones.RightToes, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
                    {
                        var t = a.GetBoneTransform(toe); if (t == null) continue;
                        float y = t.position.y - g;
                        if (toe == HumanBodyBones.LeftToes || toe == HumanBodyBones.RightToes || a.GetBoneTransform(HumanBodyBones.LeftToes) == null)
                        { minSole = Mathf.Min(minSole, y); maxSole = Mathf.Max(maxSole, y); }
                    }
                }
                maxPlayerLean = Mathf.Max(maxPlayerLean, ShotDirector.Lean(player, player.transform));
                foreach (var a in peds) { walk.Add(Knee(a, true)); walk.Add(Knee(a, false)); }
            }
            // feet side by side, never crossed: the right foot stays on the body's right of the left one, every frame of
            // two seconds of walking and of standing (Matthew, Nov 28: feet crossed on every character)
            float walkGapMin = 9f, walkGapSum = 0f, idleGapMin = 9f, hipGap = 0f; int walkGapN = 0; string crossedWho = "";
            float gapUntil = Time.time + 2f;
            while (Time.time < gapUntil)
            {
                foreach (var a in peds.Concat(idlers))
                {
                    Vector3 lf = a.GetBoneTransform(HumanBodyBones.LeftFoot).position, rf = a.GetBoneTransform(HumanBodyBones.RightFoot).position;
                    var hips = a.GetBoneTransform(HumanBodyBones.Hips);
                    // across the body, by the body itself: from the left hip joint to the right one, flat on the ground
                    // (the object's own forward is not the way every one of them faces)
                    Vector3 across = a.GetBoneTransform(HumanBodyBones.RightUpperLeg).position - a.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position;
                    across.y = 0f; across.Normalize();
                    float gap = Vector3.Dot(rf - lf, across);
                    if (idlers.Contains(a)) idleGapMin = Mathf.Min(idleGapMin, gap);
                    else { if (gap < walkGapMin) { walkGapMin = gap; crossedWho = a.transform.root.name; } walkGapSum += gap; walkGapN++; }
                    hipGap = Vector3.Dot(a.GetBoneTransform(HumanBodyBones.RightUpperLeg).position - a.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position, across);
                }
                yield return null;
            }
            Debug.Log($"[LegTests] feet, right of left: walking min {walkGapMin:0.000} m ({crossedWho}) mean {walkGapSum / Mathf.Max(1, walkGapN):0.000} m, standing min {idleGapMin:0.000} m, hip joints {hipGap:0.000} m apart");
            float walkMean = walk.Average(), walkMin = walk.Min();
            Debug.Log($"[LegTests] idle knee min {minIdleKnee:0.0} deg, idle feet apart max {maxWidth:0.00} m, idle toes {minSole:0.000} to {maxSole:0.000} m above the ground, walk knee mean {walkMean:0.0} min {walkMin:0.0} deg, player idle lean max {maxPlayerLean:0.0} deg ({idlers.Count} idle, {peds.Length} walking)");
            Assert.Greater(minIdleKnee, 165f, "legs straight at idle, not a squat");
            Assert.Less(maxWidth, 0.45f, "feet under the hips at idle, not a wide stance");
            Assert.That(minSole, Is.GreaterThan(-0.05f), "toes not sunk into the ground");
            Assert.That(maxSole, Is.LessThan(0.12f), "toes on the ground, not floating");
            Assert.Less(maxPlayerLean, 10f, "player stands up straight at idle, no stoop");
            Assert.Greater(walkMean, 135f, "walking knees fold like a walk, not a lunge");
            Assert.Greater(idleGapMin, 0.1f, "standing feet are side by side, not crossed");
            Assert.Greater(walkGapMin, -0.02f, "walking feet never cross");
            Assert.Greater(walkMin, 95f, "no deep knee fold in the walk");
        }
    }
}
