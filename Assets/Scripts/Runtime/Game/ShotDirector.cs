// Screenshot set-ups for the headless WebGL capture (tools/capture_webgl.mjs --query=shot=...).
// Only acts when the page URL asks for it, so normal play is untouched:
//   shot=job     next to Unc with Bag Back started (brief, marker, timer)
//   shot=bag     Bag Back running, standing by the bag
//   shot=police  two stars, officers on the way
//   shot=car     in the sedan
//   shot=overview high view over the whole district
//   shot=cruiser two stars, standing still while a cruiser drives in
//   shot=peds    side view following a walking pedestrian (posture, feet, corners); logs each ped's lean
//   shot=legs    side view of the player: idle, then keys 2 walk, 3 sprint, 1 stand; logs the knee angles
using System.Collections;
using StreetEmpire.Player;
using StreetEmpire.Vehicles;
using UnityEngine;

namespace StreetEmpire.Game
{
    public class ShotDirector : MonoBehaviour
    {
        IEnumerator Start()
        {
            string url = Application.absoluteURL ?? "";
            int i = url.IndexOf("shot=");
            if (i < 0) yield break;
            string shot = url.Substring(i + 5).Split('&')[0];
            yield return null;
            var s = GameSession.Instance; var job = FindFirstObjectByType<MissionRunner>();
            if (s == null) yield break;
            var p = s.player;
            switch (shot)
            {
                case "job":
                    p.Teleport(job.giver.position + job.giver.forward * 2.2f + job.giver.right * 0.8f, job.giver.eulerAngles.y + 180f);
                    yield return null; job.Begin();
                    break;
                case "bag":
                    job.Begin();
                    var to = job.Target; p.Teleport(to + new Vector3(0, 0.1f, -6f), 0f);
                    break;
                case "police":
                    s.AddHeat(45f);
                    break;
                case "cruiser":
                    s.AddHeat(45f);
                    break;
                case "overview":
                    var cam = Camera.main;
                    var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>(); if (brain) brain.enabled = false;
                    float cx = (s.spanX.x + s.spanX.y) / 2, cz = (s.spanZ.x + s.spanZ.y) / 2;
                    cam.transform.position = new Vector3(cx + 70f, 120f, cz - 150f);
                    cam.transform.LookAt(new Vector3(cx, 0, cz - 5f));
                    cam.farClipPlane = 800f;
                    break;
                case "peds":
                    StartCoroutine(FollowPed());
                    break;
                case "legs":
                    StartCoroutine(PlayerLegs(p));
                    break;
                case "car":
                    var car = FindFirstObjectByType<CarController>();
                    p.Teleport(car.transform.position - car.transform.right * 2.4f, car.transform.eulerAngles.y);
                    yield return null;
                    p.GetComponent<VehicleUser>().TryEnter();
                    break;
            }
            Debug.Log($"[ShotDirector] {shot}");
        }

        /// <summary>Upper body lean of a character from hips to neck, degrees, positive forward.</summary>
        public static float Lean(Animator a, Transform root)
        {
            if (a == null || !a.isHuman || a.GetBoneTransform(HumanBodyBones.Neck) == null) return 0;
            var v = Vector3.ProjectOnPlane(a.GetBoneTransform(HumanBodyBones.Neck).position - a.GetBoneTransform(HumanBodyBones.Hips).position, root.right);
            return Vector3.SignedAngle(Vector3.up, v, root.right);
        }

        /// <summary>Angle at the knee (upper leg, knee, ankle), degrees; 180 is straight.</summary>
        public static float Knee(Animator a, bool left)
        {
            if (a == null || !a.isHuman) return 0;
            var up = a.GetBoneTransform(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
            var kn = a.GetBoneTransform(left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
            var ft = a.GetBoneTransform(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
            if (up == null || kn == null || ft == null) return 0;
            return Vector3.Angle(up.position - kn.position, ft.position - kn.position);
        }

        IEnumerator PlayerLegs(PlayerMotor motor)
        {
            var cam = Camera.main;
            var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>(); if (brain) brain.enabled = false;
            var anim = motor.animator;
            // the player starts in the street at the east edge of the block: film from further out in the street
            var side = Vector3.right;
            int phase = 1; float nextLog = 0;
            while (true)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (kb.digit1Key.isPressed) phase = 1;
                    if (kb.digit2Key.isPressed) phase = 2;
                    if (kb.digit3Key.isPressed) phase = 3;
                }
                motor.scriptMove = phase == 1 ? Vector2.zero : phase == 2 ? new Vector2(0.2f, 0) : new Vector2(1f, 0);
                motor.scriptSprint = phase == 3;
                var tr = motor.transform;
                cam.transform.position = tr.position + side * 3.6f + Vector3.up * 1.0f;
                cam.transform.LookAt(tr.position + Vector3.up * 0.85f);
                if (Time.time > nextLog)
                {
                    nextLog = Time.time + 0.5f;
                    Debug.Log($"[ShotDirector] legs {(phase == 1 ? "idle" : phase == 2 ? "walk" : "sprint")} speed {motor.PlanarSpeed:0.0} knees {Knee(anim, true):0} {Knee(anim, false):0} lean {Lean(anim, motor.transform):0.0}");
                }
                yield return null;
            }
        }

        IEnumerator FollowPed()
        {
            var cam = Camera.main;
            var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>(); if (brain) brain.enabled = false;
            var peds = FindObjectsByType<Ped>(FindObjectsSortMode.InstanceID);
            if (peds.Length == 0) yield break;
            var ped = peds[0];
            float t = 0, nextLog = 1;
            while (t < 60f)
            {
                t += Time.deltaTime;
                var tr = ped.transform;
                // from the street side of the sidewalk (never from inside the building next to it)
                var d = tr.position - ped.blockCenter; d.y = 0;
                var outward = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? new Vector3(Mathf.Sign(d.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(d.z));
                cam.transform.position = tr.position + outward * 3.8f + Vector3.up * 1.1f;
                cam.transform.LookAt(tr.position + Vector3.up * 0.9f);
                if (t > nextLog)
                {
                    nextLog += 1;
                    var b = ped.GetComponent<NpcBody>();
                    Debug.Log($"[ShotDirector] ped {ped.name} lean {b.Lean:0.0} speed {b.Speed:0.00} y {tr.position.y:0.00}");
                }
                yield return null;
            }
        }
    }
}
