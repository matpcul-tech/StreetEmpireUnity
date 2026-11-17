// Screenshot set-ups for the headless WebGL capture (tools/capture_webgl.mjs --query=shot=...).
// Only acts when the page URL asks for it, so normal play is untouched:
//   shot=job     next to Unc with Bag Back started (brief, marker, timer)
//   shot=bag     Bag Back running, standing by the bag
//   shot=police  two stars, officers on the way
//   shot=car     in the sedan
//   shot=overview high view over the whole district
//   shot=cruiser two stars, standing still while a cruiser drives in
//   shot=peds    side view following a walking pedestrian (posture, feet, corners); logs each ped's lean
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
                cam.transform.position = tr.position + tr.right * 4.2f + Vector3.up * 1.2f;
                cam.transform.LookAt(tr.position + Vector3.up * 0.9f);
                if (t > nextLog)
                {
                    nextLog += 1;
                    foreach (var p in peds)
                    {
                        var b = p.GetComponent<NpcBody>();
                        Debug.Log($"[ShotDirector] ped {p.name} lean {b.Lean:0.0} speed {b.Speed:0.00} y {p.transform.position.y:0.00}");
                    }
                }
                yield return null;
            }
        }
    }
}
