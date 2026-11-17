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
//   shot=tour    fixed street views of the Eastside build-out, keys 1 to 8 pick the view (view=N in the URL
//                starts on one); logs the frame's draw call, batch and triangle counters where the player has them
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
                case "tour":
                    StartCoroutine(Tour(url));
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

        /// <summary>Views for the build-out shots, from the block markers: 1 an avenue at eye height, 2 a
        /// signal crossing, 3 into an open lot, 4 a block from above, 5 the backyards, 6 a stop sign crossing,
        /// 7 the whole district, 8 a curb with parked cars.</summary>
        IEnumerator Tour(string url)
        {
            var cam = Camera.main;
            var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>(); if (brain) brain.enabled = false;
            cam.farClipPlane = 800f;
            Vector3 B(int id) { foreach (var m in FindObjectsByType<StreetEmpire.City.BlockMarker>(FindObjectsSortMode.None)) if (m.blockId == id) return m.transform.position; return Vector3.zero; }
            var b10 = B(10); var b0 = B(0); var b20 = B(20); var b2 = B(2);
            var views = new (Vector3 pos, Vector3 at)[]
            {
                (b10 + new Vector3(28f, 1.7f, -24f), b10 + new Vector3(27f, 1.5f, 30f)),
                (b10 + new Vector3(-34f, 2.2f, 36f), b10 + new Vector3(-22f, 1.5f, 24f)),
                (b10 + new Vector3(0f, 1.8f, -27f), b10 + new Vector3(0f, 1.2f, -12f)),
                (b20 + new Vector3(-36f, 26f, -36f), b20 + new Vector3(0f, 0f, 0f)),
                (b0 + new Vector3(5.5f, 1.7f, -5.5f), b0 + new Vector3(-6f, 1.4f, 6f)),
                (b2 + new Vector3(32f, 1.8f, 12f), b2 + new Vector3(24f, 1.5f, 24f)),
                ((b0 + b20) / 2 + new Vector3(80f, 110f, -150f), (b0 + b20) / 2),
                (b20 + new Vector3(-26.5f, 1.6f, -16f), b20 + new Vector3(-24.5f, 1.0f, 12f)),
            };
            int view = 0; int q = url.IndexOf("view="); if (q >= 0 && int.TryParse(url.Substring(q + 5).Split('&')[0], out int v0)) view = Mathf.Clamp(v0 - 1, 0, views.Length - 1);
            var draws = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Draw Calls Count");
            var batches = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Batches Count");
            var setpass = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "SetPass Calls Count");
            var trisRec = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Triangles Count");
            float nextLog = 2f; int shown = -1;
            while (true)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    var keys = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key, kb.digit6Key, kb.digit7Key, kb.digit8Key };
                    for (int i = 0; i < keys.Length; i++) if (keys[i].isPressed) view = i;
                }
                cam.transform.position = views[view].pos; cam.transform.LookAt(views[view].at);
                if (view != shown) { shown = view; nextLog = Time.time + 1.5f; }
                if (Time.time > nextLog)
                {
                    nextLog = Time.time + 3f;
                    Debug.Log($"[ShotDirector] tour view {view + 1} draws {draws.LastValue} batches {batches.LastValue} setpass {setpass.LastValue} tris {trisRec.LastValue} frame {Time.smoothDeltaTime * 1000f:0} ms (software renderer here, not a phone number)");
                }
                yield return null;
            }
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
