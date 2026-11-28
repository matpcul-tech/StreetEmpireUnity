// Arms at rest: how far the upper arms stand out from the body and which way the palms face, for the player and
// the people standing near him. Logged, so a change to the arms can be read as numbers as well as seen in photos.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StreetEmpire.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class ArmTests
    {
        public struct Arm { public float outDeg, sideDeg, foreDeg, elbowDeg, palmIn, palmFwd, palmUp; }

        public static Arm Measure(Animator a, bool left)
        {
            Transform B(HumanBodyBones b) => a.GetBoneTransform(b);
            var t = a.transform;
            var sh = B(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm); var el = B(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            var hand = B(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            var mid = B(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
            var idx = B(left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal); var pinky = B(left ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal);
            Vector3 upper = (el.position - sh.position).normalized, fore = (hand.position - el.position).normalized;
            Vector3 outward = left ? -t.right : t.right;
            var r = new Arm { outDeg = Vector3.Angle(upper, -t.up), sideDeg = Mathf.Asin(Mathf.Clamp(Vector3.Dot(upper, outward), -1f, 1f)) * Mathf.Rad2Deg,
                foreDeg = Mathf.Asin(Mathf.Clamp(Vector3.Dot(upper, t.forward), -1f, 1f)) * Mathf.Rad2Deg, elbowDeg = Vector3.Angle(upper, fore) };
            if (mid != null && idx != null && pinky != null)
            {
                // the palm's own facing, from the knuckles: across the hand (index to little finger) and along it
                var n = Vector3.Cross(mid.position - hand.position, idx.position - pinky.position).normalized; if (left) n = -n;
                r.palmIn = Vector3.Dot(n, -outward); r.palmFwd = Vector3.Dot(n, t.forward); r.palmUp = Vector3.Dot(n, t.up);
            }
            return r;
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator ArmsAtRestAreMeasured()
        {
            MuggerOpening.AutoStart = false; BlockLife.AutoStart = true; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            yield return new WaitForSeconds(3f);
            var player = GameObject.FindGameObjectWithTag("Player"); Assert.IsNotNull(player);
            var all = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).Where(a => a.isHuman && a.isActiveAndEnabled && a.GetBoneTransform(HumanBodyBones.LeftHand) != null)
                .OrderBy(a => Vector3.Distance(a.transform.position, player.transform.position)).Take(7).ToArray();
            Assert.Greater(all.Length, 0, "people to measure");
            var pa = player.GetComponentInChildren<Animator>();
            foreach (bool left in new[] { true, false })
            {
                var m = Measure(pa, left);
                Assert.Less(m.foreDeg, 3f, "standing, his upper arm is not thrown forward"); Assert.Greater(m.foreDeg, -ArmRest.MaxBack - 3f, "nor swept back behind him");
                Assert.Less(m.sideDeg, ArmRest.MaxOut + 3f, "it hangs beside the body"); Assert.Less(m.elbowDeg, ArmRest.MaxElbow + 4f, "with the elbow eased open");
                Assert.Greater(m.palmIn, 0.5f, "palm toward the thigh");
            }
            foreach (var a in all)
                foreach (bool left in new[] { true, false })
                {
                    var m = Measure(a, left);
                    Debug.Log($"[ArmTests] {a.transform.root.name} {(left ? "L" : "R")}: upper arm {m.outDeg:0} deg off straight down (out to the side {m.sideDeg:0}, forward {m.foreDeg:0}), elbow bent {m.elbowDeg:0}, palm faces in {m.palmIn:0.00} forward {m.palmFwd:0.00} up {m.palmUp:0.00}, state {a.GetCurrentAnimatorStateInfo(0).shortNameHash} speed {(a.parameters.Any(p => p.name == "Speed") ? a.GetFloat("Speed") : -1f):0.0}");
                }
        }
    }
}
