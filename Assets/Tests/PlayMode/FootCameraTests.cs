// The on foot camera: close over the right shoulder, and it eases round behind a walk that is left alone.
using System.Collections;
using NUnit.Framework;
using StreetEmpire.Game;
using StreetEmpire.Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class FootCameraTests
    {
        [TearDown] public void Reset() { JobBoard.AutoStart = true; SideHustles.AutoStart = true; MuggerOpening.AutoStart = true; Time.timeScale = 1f; }

        [UnityTest, Timeout(300000)]
        public IEnumerator TheCameraSitsCloseAndFollowsAWalk()
        {
            Time.timeScale = 1f; MuggerOpening.AutoStart = false; JobBoard.AutoStart = false; SideHustles.AutoStart = false;
            SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var s = GameSession.Instance; var player = Object.FindFirstObjectByType<PlayerMotor>(); var rig = Object.FindFirstObjectByType<CameraRig>();
            var user = player.GetComponent<VehicleUser>(); var cm = rig.GetComponent<CinemachineCamera>();
            Assert.AreEqual(3.8f, CameraRig.FootRadius); Assert.AreEqual(CameraRig.FootRadius, rig.radius, 1e-3f, "close behind him");
            Assert.AreEqual(CameraRig.FootRadius, user.footRadius, 1e-3f, "and it comes back to that after a drive");
            Assert.AreEqual(56f, cm.Lens.FieldOfView, 1e-3f, "a longer lens than before: less stretch at the edges");
            Assert.AreEqual(CameraRig.FootShoulder, rig.composer.Composition.ScreenPosition.x, 1e-3f, "he stands left of center");
            Assert.AreEqual(1.55f, user.footCameraTarget.localPosition.y, 1e-3f, "aimed at his shoulders");
            Assert.AreSame(player.transform, rig.footFollow, "it knows who to follow");
            yield return new WaitForSeconds(0.5f);
            Assert.That(rig.orbit.Radius, Is.InRange(1.5f, CameraRig.FootRadius + 0.05f));
            Assert.Greater(Camera.main.transform.position.y, CameraRig.FloorHeight - 0.05f);

            // a walk down a clear street with the view left alone: the camera comes round behind his heading
            Assert.IsTrue(s.SpawnPoint(out var at, 10f, 80f, 3f), "a clear street");
            float heading = 90f; Vector3 dir = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
            rig.orbit.HorizontalAxis.Value = heading + 70f;
            float t = 0f; Vector3 pos = at + Vector3.up * 0.3f;
            while (t < 6f) { pos += dir * 3f * Time.deltaTime; player.Teleport(pos, heading); t += Time.deltaTime; yield return null; }
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(rig.orbit.HorizontalAxis.Value, heading)), 20f, "behind him after six seconds of walking");

            // walking back toward the camera leaves the view where the player put it
            rig.orbit.HorizontalAxis.Value = heading + 180f; float before = rig.orbit.HorizontalAxis.Value;
            t = 0f;
            while (t < 4f) { pos += dir * 3f * Time.deltaTime; player.Teleport(pos, heading); t += Time.deltaTime; yield return null; }
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(rig.orbit.HorizontalAxis.Value, before)), 2f, "it does not swing round on a walk toward it");

            // standing still, it stays put
            rig.orbit.HorizontalAxis.Value = heading + 50f;
            yield return new WaitForSeconds(3f);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(rig.orbit.HorizontalAxis.Value, heading + 50f)), 2f, "no drift while he stands");
        }
    }
}
