// M2: Humanoid player in Eastside, moves with scripted and gamepad input, jumps, camera follows,
// phone controls exist.
using System.Collections;
using NUnit.Framework;
using StreetEmpire.Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class PlayerTests
    {
        PlayerMotor player;

        IEnumerator Load()
        {
            StreetEmpire.Game.MuggerOpening.AutoStart = false; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            player = Object.FindFirstObjectByType<PlayerMotor>();
            Assert.IsNotNull(player, "player in scene");
            for (int i = 0; i < 20; i++) yield return null; // settle on the ground
        }

        [UnityTest]
        public IEnumerator PlayerIsHumanoidAndSized()
        {
            yield return Load();
            var anim = player.animator;
            Assert.IsNotNull(anim, "animator");
            Assert.IsNotNull(anim.avatar, "avatar");
            Assert.IsTrue(anim.avatar.isValid && anim.avatar.isHuman, "Humanoid avatar");
            Assert.IsNotNull(anim.GetBoneTransform(HumanBodyBones.Hips), "hips mapped");
            Assert.IsNotNull(anim.GetBoneTransform(HumanBodyBones.LeftFoot), "foot mapped");
            var r = player.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.IsNotNull(r, "skinned mesh");
            Assert.That(r.bounds.size.y, Is.InRange(1.4f, 2.2f), "about 1.8 m tall");
            Assert.IsTrue(player.Grounded, "standing on the street");
            Assert.IsNotNull(Object.FindFirstObjectByType<CinemachineBrain>(), "cinemachine brain");
            Assert.IsNotNull(Object.FindFirstObjectByType<OnScreenStick>(FindObjectsInactive.Include), "phone stick");
            // the thumb zone drives the stick; the ring and knob are pictures and must not swallow the touch
            var zone = Object.FindFirstObjectByType<StreetEmpire.UI.StickZone>(FindObjectsInactive.Include);
            Assert.IsNotNull(zone, "stick thumb zone"); Assert.IsNotNull(zone.stick, "zone drives the stick"); Assert.IsNotNull(zone.stickBase, "zone moves the ring");
            Assert.IsFalse(zone.stick.GetComponent<UnityEngine.UI.Image>().raycastTarget, "the knob does not take touches");
            Assert.IsFalse(zone.stickBase.GetComponent<UnityEngine.UI.Image>().raycastTarget, "the ring does not block touches");
            var zr = (RectTransform)zone.transform;
            Assert.Greater(zr.anchorMax.x, 0.4f, "the zone covers the lower left"); Assert.Greater(zr.anchorMax.y, 0.5f);
            Assert.GreaterOrEqual(Object.FindObjectsByType<OnScreenButton>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, 3, "phone buttons");
        }

        [UnityTest]
        public IEnumerator JogSprintAndJump()
        {
            yield return Load();
            var start = player.transform.position;
            player.scriptMove = Vector2.up;
            yield return new WaitForSeconds(1.5f);
            float jog = Flat(player.transform.position - start);
            Assert.That(jog, Is.InRange(4f, 9f), $"jogged {jog} m in 1.5 s");
            Assert.That(player.animator.GetFloat("Speed"), Is.GreaterThan(3f), "animator speed follows");

            var mid = player.transform.position;
            player.scriptSprint = true;
            yield return new WaitForSeconds(1f);
            float sprint = Flat(player.transform.position - mid);
            Assert.Greater(sprint, jog / 1.5f * 1.3f, $"sprint {sprint} m/s faster than jog");
            player.scriptSprint = false; player.scriptMove = Vector2.zero;
            yield return new WaitForSeconds(0.4f);

            float y0 = player.transform.position.y; float top = y0;
            player.scriptJump = true;
            for (float t = 0; t < 1.2f; t += Time.deltaTime) { top = Mathf.Max(top, player.transform.position.y); yield return null; }
            Assert.Greater(top - y0, 0.6f, "jumped");
            Assert.IsTrue(player.Grounded, "landed");

            var cam = Camera.main.transform.position;
            float d = Vector3.Distance(cam, player.transform.position);
            Assert.That(d, Is.InRange(2.5f, 9f), $"camera follows at {d} m");
            player.scriptMove = null;
        }

        [UnityTest]
        public IEnumerator GamepadStickMovesPlayer()
        {
            yield return Load();
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var start = player.transform.position;
                for (float t = 0; t < 1f; t += Time.deltaTime)
                {
                    InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(0, 1) });
                    yield return null;
                }
                Assert.Greater(Flat(player.transform.position - start), 2f, "the stick drives the player (same path as the phone stick)");
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
