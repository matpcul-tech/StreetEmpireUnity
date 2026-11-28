// Photos of the one block that has had the street pass (block 1, across the street from where the player starts):
// the curb, the sidewalk, the card table corner and the people on it, by day, at dusk and at night. Logged like
// LookShots ("[LookShot] name part/parts data"), so a change to the block's clutter, ground or night light is
// judged on frames. Also logs what the prop instancer drew for each view ("[BlockShots] ...").
using System.Collections;
using NUnit.Framework;
using StreetEmpire.City;
using StreetEmpire.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class BlockShots
    {
        /// <summary>The block with the street pass.</summary>
        public const int Block = 1;
        const int W = 960, H = 540;

        static bool clockHeld, clockWasFrozen; static float clockWasHour;
        [TearDown] public void Restore()
        {
            if (clockHeld) { GameClock.Frozen = clockWasFrozen; GameClock.FrozenHour = clockWasHour; clockHeld = false; }
            BuildingCuller.On = true; MuggerOpening.AutoStart = true; BlockLife.AutoStart = false; StreetDice.AutoStart = false;
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator TheBlockOnCamera()
        {
            MuggerOpening.AutoStart = false; BlockLife.AutoStart = true; StreetDice.AutoStart = true; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            clockWasFrozen = GameClock.Frozen; clockWasHour = GameClock.FrozenHour; clockHeld = true;
            GameClock.Frozen = true; GameClock.FrozenHour = 14f; BuildingCuller.On = false;
            var hud = GameObject.Find("HUD"); if (hud) hud.SetActive(false);
            var cam = Camera.main; Assert.IsNotNull(cam, "the game camera");
            foreach (var mb in cam.GetComponents<MonoBehaviour>()) if (mb.GetType().Name == "CinemachineBrain") mb.enabled = false;
            cam.fieldOfView = 55f; cam.farClipPlane = 600f;
            var turf = TurfWar.Instance; Assert.IsNotNull(turf); Assert.GreaterOrEqual(turf.IndexOf(Block), 0, "the block is in the town");
            var sd = StreetDice.Instance; var pm = Object.FindFirstObjectByType<StreetEmpire.Player.PlayerMotor>();
            Assert.IsNotNull(sd, "street dice"); Assert.IsNotNull(pm, "the player");
            var inst = Object.FindFirstObjectByType<PropInstancer>();
            Vector3 c = turf.Center(Block); c.y = 0f;

            // the corner people and the card table only stand near the player: he is put beside the table
            pm.Teleport(new Vector3(c.x + 24f, 0.4f, c.z + 24f), 180f);
            Vector3 spot = default; float until = Time.time + 8f;
            while (!sd.Spot(Block, out spot) && Time.time < until) yield return null;
            Assert.IsTrue(sd.Spot(Block, out spot), "the block has a corner scene");
            Vector3 outward = spot - c; outward.y = 0f; outward.Normalize(); Vector3 side = Vector3.Cross(Vector3.up, outward);
            pm.Teleport(spot + outward * 2.2f - side * 1.8f + Vector3.up * 0.3f, Quaternion.LookRotation(-outward).eulerAngles.y);
            for (int f = 0; f < 90; f++) yield return null;
            Debug.Log($"[BlockShots] block {Block} center {c}, corner scene at {spot}, facing out {outward}");

            var views = new (string name, Vector3 eye, Vector3 look)[]
            {
                // down the east curb from the street: the gutter, the walk, the lamp pool and what stands on them
                ("street", c + new Vector3(26.5f, 2.2f, -27f), c + new Vector3(21f, 0.6f, 2f)),
                // on the east sidewalk at eye height, looking north toward the corner
                ("walk", c + new Vector3(20.6f, 1.7f, -17f), c + new Vector3(20.4f, 0.9f, 8f)),
                // the north curb from across the street
                ("north", c + new Vector3(-6f, 3.2f, 33f), c + new Vector3(4f, 0.8f, 20f)),
                // the whole corner from above the crossing
                ("air", c + new Vector3(41f, 21f, 41f), c + new Vector3(13f, 0f, 13f)),
                // the card table corner from the crossing, and close
                ("corner", spot + outward * 8.5f + side * 3f + Vector3.up * 2.6f, spot + Vector3.up * 0.7f),
                ("cards", spot + outward * 2.6f + side * 1.5f + Vector3.up * 1.7f, spot + Vector3.up * 0.55f),
                // the people on the corner: faces and clothes
                ("people", spot + outward * 1.2f + side * 2.6f + Vector3.up * 1.55f, spot - outward * 2.0f + Vector3.up * 1.25f),
            };
            var rt = new RenderTexture(W, H, 24); var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            int shots = 0;
            foreach (var (hour, tag) in new[] { (14f, "day"), (18.9f, "dusk"), (22f, "night") })
            {
                GameClock.FrozenHour = hour;
                for (int f = 0; f < 10; f++) yield return null;
                foreach (var (name, eye, look) in views)
                {
                    cam.transform.position = eye; cam.transform.LookAt(look);
                    // the lamps and storefront spills nearest the camera are picked a few times a second
                    yield return new WaitForSecondsRealtime(0.4f);
                    yield return null;
                    var snap = cam.gameObject.AddComponent<LateSnap>(); snap.cam = cam; snap.rt = rt;
                    while (!snap.done) yield return null;
                    Object.Destroy(snap);
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = prev;
                    string b64 = System.Convert.ToBase64String(tex.EncodeToJPG(78));
                    int parts = (b64.Length + 5999) / 6000;
                    for (int i = 0; i < parts; i++)
                        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[LookShot] block-{0}-{1} {2}/{3} {4}", name, tag, i + 1, parts, b64.Substring(i * 6000, Mathf.Min(6000, b64.Length - i * 6000)));
                    if (inst != null) Debug.Log($"[BlockShots] {name} {tag}: instancer {inst.LastDraws} draws, {inst.LastInstances} props, {inst.LastTriangles} triangles");
                    shots++;
                }
            }
            Object.Destroy(rt); Object.Destroy(tex);
            Assert.AreEqual(views.Length * 3, shots, "every view by day, at dusk and at night");
        }
    }
}
