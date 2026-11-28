// Motorcycles: a parked one can be taken, the rider sits on it in the open, it leans into a turn and he leans with
// it, and getting off puts him back on his feet. Photos of the rider are logged like LookShots.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StreetEmpire.City;
using StreetEmpire.Game;
using StreetEmpire.Player;
using StreetEmpire.Vehicles;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class BikeTests
    {
        static bool clockSet, wasFrozen; static float wasHour;
        [TearDown] public void Reset()
        {
            JobBoard.AutoStart = true; SideHustles.AutoStart = true; MuggerOpening.AutoStart = true; BuildingCuller.On = true; Time.timeScale = 1f;
            if (clockSet) { GameClock.Frozen = wasFrozen; GameClock.FrozenHour = wasHour; clockSet = false; }
        }

        [Test]
        public void TheLeanGrowsWithSpeedAndSteering()
        {
            Assert.AreEqual(0f, BikeLean.LeanFor(0.4f, 0f, 1.4f, 32f), 1e-4f, "standing still it is upright");
            Assert.Less(BikeLean.LeanFor(0.1f, 10f, 1.4f, 32f), -10f, "a right turn leans right");
            Assert.Greater(BikeLean.LeanFor(-0.1f, 10f, 1.4f, 32f), 10f, "a left turn leans left");
            Assert.Less(Mathf.Abs(BikeLean.LeanFor(0.1f, 3f, 1.4f, 32f)), Mathf.Abs(BikeLean.LeanFor(0.1f, 6f, 1.4f, 32f)), "less at a crawl");
            Assert.AreEqual(-32f, BikeLean.LeanFor(0.3f, 30f, 1.4f, 32f), "never past 32 degrees");
            Assert.Less(BikeLean.LeanFor(0.03f, 25f, 1.4f, 32f), -20f, "at speed a small turn of the bars is a deep lean");
            Assert.IsTrue(ParkedCars.IsBike("sport-bike") && ParkedCars.IsBike("dirt-bike") && ParkedCars.IsBike("cruiser")); Assert.IsFalse(ParkedCars.IsBike("sedan"));
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator AParkedMotorcycleIsRiddenInTheOpen()
        {
            Time.timeScale = 1f; MuggerOpening.AutoStart = false; JobBoard.AutoStart = false; SideHustles.AutoStart = false;
            SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            wasFrozen = GameClock.Frozen; wasHour = GameClock.FrozenHour; clockSet = true; GameClock.Frozen = true; GameClock.FrozenHour = 14f;
            var s = GameSession.Instance; var player = Object.FindFirstObjectByType<PlayerMotor>(); var user = player.GetComponent<VehicleUser>();
            var parked = Object.FindObjectsByType<PropMarker>(FindObjectsSortMode.None).Where(m => ParkedCars.IsBike(m.model) && ParkedCars.Drivable(m)).OrderBy(m => m.model == "sport-bike" ? 0 : 1).ToArray();
            Assert.Greater(parked.Length, 0, "motorcycles parked at the curb, there for the taking");
            string kind = parked[0].model;
            var bike = ParkedCars.Take(parked[0]); Assert.IsNotNull(bike, "the parked bike becomes one that drives");
            Assert.IsTrue(bike.bike); var lean = bike.GetComponent<BikeLean>(); Assert.IsNotNull(lean, "it leans");
            Assert.That(bike.stats.maxSpeed, Is.InRange(27f, 34f)); Assert.AreEqual(60f, bike.hp);

            // out on a clear stretch of street, the player beside it
            Assert.IsTrue(s.SpawnPoint(out var at, 15f, 90f, 4f), "a clear street");
            bike.Place(at, 0f);
            yield return new WaitForFixedUpdate(); yield return null;
            player.Teleport(bike.transform.position - bike.transform.right * 1.6f + Vector3.up * 0.3f, 0f);
            yield return null; yield return null;
            user.scriptUse = true;
            yield return null; yield return null;
            Assert.AreSame(bike, user.Car, "on the bike"); Assert.IsTrue(user.Riding);
            Assert.IsTrue(player.GetComponentsInChildren<Renderer>().All(r => r.enabled), "the rider is on show, not hidden as in a car");
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(player.animator.GetCurrentAnimatorStateInfo(0).IsName("Riding"), "seated, hands out front");
            Assert.Less(Vector3.Distance(player.transform.position, bike.transform.position + bike.transform.rotation * (user.bikeSeat + Vector3.forward * (user.Tuck > 0f ? user.sportForward : 0f))), 0.05f, "in the saddle");
            if (kind == "sport-bike")
            {
                Assert.Greater(user.Tuck, 10f, "tucked forward over the tank on a sport bike");
                yield return null; yield return null;
                var hipBone = player.animator.GetBoneTransform(HumanBodyBones.Hips); var headBone = player.animator.GetBoneTransform(HumanBodyBones.Head);
                float ahead = Vector3.Dot(headBone.position - hipBone.position, bike.transform.forward);
                Debug.Log($"[BikeTests] head ahead of hips: {ahead:0.00} m");
                Assert.Greater(ahead, 0.15f, "his head is out over the tank");
            }

            // photos of the rider at rest: side, behind and ahead
            var hud = GameObject.Find("HUD"); if (hud) hud.SetActive(false);
            var cam = Camera.main; BuildingCuller.On = false;
            foreach (var mb in cam.GetComponents<MonoBehaviour>()) if (mb.GetType().Name == "CinemachineBrain") mb.enabled = false;
            cam.fieldOfView = 45f;
            var rt = new RenderTexture(640, 360, 24); var tex = new Texture2D(640, 360, TextureFormat.RGB24, false);
            var bt = bike.transform; Vector3 mid = bt.position + Vector3.up * 0.95f;
            foreach (var (name, eye) in new[] { ("bike-side", mid - bt.right * 3.6f + Vector3.up * 0.2f), ("bike-side-b", mid + bt.right * 3.6f + Vector3.up * 0.2f), ("bike-behind", mid - bt.forward * 3.6f + bt.right * 1.2f + Vector3.up * 0.7f), ("bike-ahead", mid + bt.forward * 3.4f - bt.right * 1.4f + Vector3.up * 0.3f) })
            {
                cam.transform.position = eye; cam.transform.LookAt(mid);
                for (int f = 0; f < 3; f++) yield return null;
                var snap = cam.gameObject.AddComponent<LateSnap>(); snap.cam = cam; snap.rt = rt;
                while (!snap.done) yield return null;
                Object.Destroy(snap);
                var prev = RenderTexture.active; RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply(); RenderTexture.active = prev;
                string b64 = System.Convert.ToBase64String(tex.EncodeToJPG(75));
                int parts = (b64.Length + 5999) / 6000;
                for (int i = 0; i < parts; i++)
                    Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[LookShot] {0}-{1} {2}/{3} {4}", name, kind, i + 1, parts, b64.Substring(i * 6000, Mathf.Min(6000, b64.Length - i * 6000)));
            }
            Object.Destroy(rt); Object.Destroy(tex);
            // this test has twice stopped dead here for five minutes with nothing in the log. Each step now says where it
            // is, with the game clock, the frame and the time scale, so the next time shows which wait never came back.
            void Mark(string step) => Debug.Log($"[BikeTests] {step}: time {Time.time:0.00} frame {Time.frameCount} scale {Time.timeScale:0.##} bike {(bike != null ? bike.transform.position.ToString("0.0") : "gone")} v {(bike != null ? bike.Speed : 0f):0.0} hp {(bike != null ? bike.hp : 0f):0}");
            Mark("photos done");

            // it goes, and in a turn at speed the bike leans over and he leans with it
            Vector3 from = bike.transform.position;
            // a short run: at full throttle for a second and a half it is across the block and into a wall before the turn
            bike.scriptThrottle = 1f; bike.scriptSteer = 0f;
            Mark("throttle on");
            float wall = Time.realtimeSinceStartup, goUntil = Time.time + 0.7f;
            while (Time.time < goUntil) { Assert.Less(Time.realtimeSinceStartup - wall, 30f, $"0.7 s of game time takes under 30 s of real time (scale {Time.timeScale}, frame {Time.frameCount})"); yield return null; }
            Mark("straight run done");
            Assert.Greater(Vector3.Distance(bike.transform.position, from), 2.5f, "the bike goes");
            Assert.Less(Mathf.Abs(lean.Roll), 2f, "upright in a straight line");
            bike.scriptSteer = 0.5f;
            float worst = 0f, until = Time.time + 1.2f, fastest = 0f, mostSteer = 0f, riderOver = 0f, drawnOff = 0f;
            var model = bike.transform.Find("Model"); Assert.IsNotNull(model);
            wall = Time.realtimeSinceStartup;
            while (Time.time < until)
            {
                Assert.Less(Time.realtimeSinceStartup - wall, 30f, $"the turn takes under 30 s of real time (scale {Time.timeScale}, frame {Time.frameCount})");
                // read while it is leaning: by the end of the turn it may have straightened or met a wall
                worst = Mathf.Min(worst, lean.Roll); fastest = Mathf.Max(fastest, bike.Speed); mostSteer = Mathf.Max(mostSteer, bike.state.steer);
                riderOver = Mathf.Max(riderOver, Vector3.Angle(player.transform.up, Vector3.up));
                drawnOff = Mathf.Max(drawnOff, Mathf.Abs(lean.Roll - Mathf.DeltaAngle(0f, model.localEulerAngles.z)));
                yield return null;
            }
            Debug.Log($"[BikeTests] in the turn: lean {worst:0.0} deg, top speed {fastest:0.0} m/s, most steer {mostSteer * Mathf.Rad2Deg:0.0} deg");
            Assert.Less(worst, -8f, $"over to the right in a right turn (top speed {fastest:0.0} m/s, steer {mostSteer * Mathf.Rad2Deg:0.0} deg)");
            Assert.Less(drawnOff, 0.5f, "the drawn bike is the one leaning");
            Assert.Greater(riderOver, 5f, "and the rider leans with it");
            bike.scriptThrottle = 0f; bike.scriptSteer = 0f; bike.scriptHandbrake = true;
            yield return new WaitForSeconds(2.5f);
            bike.scriptHandbrake = false;

            // off again: on his feet beside it
            user.scriptUse = true;
            yield return null; yield return null;
            Assert.IsNull(user.Car); Assert.IsFalse(user.Riding);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(player.animator.GetCurrentAnimatorStateInfo(0).IsName("Riding"), "standing again");
            Assert.Less(Vector3.Angle(player.transform.up, Vector3.up), 1f, "upright");
            Assert.That(Vector3.Distance(player.transform.position, bike.transform.position), Is.InRange(1.5f, 3.5f), "beside the bike");

            // at night it carries one headlamp and one tail lamp on the bike itself, not a car's pair out at the corners
            GameClock.FrozenHour = 22f;
            yield return new WaitForSeconds(1.2f);
            var drawn = bike.transform.Find("Model");
            var head = drawn.Find("Headlamp"); var tail = drawn.Find("Tail lamp");
            Assert.IsNotNull(head, "a headlamp on the bike"); Assert.IsNotNull(tail, "a tail lamp on the bike");
            Assert.IsNull(bike.transform.Find("Headlamp L"), "no car headlamp pair"); Assert.IsNull(bike.transform.Find("Tail lamp L"), "no car tail lamp pair");
            var bb = bike.GetComponent<BoxCollider>(); float lo = bb.center.y - bb.size.y * 0.5f, fwd = bb.center.z + bb.size.z * 0.5f, aft = bb.center.z - bb.size.z * 0.5f;
            Assert.AreEqual(bb.center.x, head.localPosition.x, 0.01f, "on the centre line"); Assert.AreEqual(bb.center.x, tail.localPosition.x, 0.01f);
            Assert.That(head.localPosition.y, Is.InRange(lo + bb.size.y * 0.5f, lo + bb.size.y * 0.85f), "up in the fairing");
            Assert.Less(head.localPosition.z, fwd - 0.1f, "behind the front of the tyre"); Assert.Greater(tail.localPosition.z, aft, "inside the tail");
            Assert.Less(head.localScale.x, 0.4f, "a bike sized lamp"); Assert.Less(tail.localScale.x, 0.3f);
        }
    }
}
