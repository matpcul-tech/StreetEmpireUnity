// Photos of the whole town for looking at after a change to its size: one from the air and one from the crossing
// beside a block in each of the newer parts. Logged like LookShots ("[LookShot] name part/parts data").
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
    public class TownShots
    {
        [TearDown] public void Reset() { MuggerOpening.AutoStart = true; BuildingCuller.On = true; }

        [UnityTest, Timeout(600000)]
        public IEnumerator TheTownOnCamera()
        {
            MuggerOpening.AutoStart = false; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            bool wasFrozen = GameClock.Frozen; float wasHour = GameClock.FrozenHour;
            GameClock.Frozen = true; GameClock.FrozenHour = 14f; BuildingCuller.On = false;
            var hud = GameObject.Find("HUD"); if (hud) hud.SetActive(false);
            var cam = Camera.main; Assert.IsNotNull(cam, "the game camera"); var turf = TurfWar.Instance; Assert.IsNotNull(turf);
            foreach (var mb in cam.GetComponents<MonoBehaviour>()) if (mb.GetType().Name == "CinemachineBrain") mb.enabled = false;
            cam.fieldOfView = 55f; cam.farClipPlane = 1500f;
            var rt = new RenderTexture(640, 360, 24); var tex = new Texture2D(640, 360, TextureFormat.RGB24, false);
            for (int f = 0; f < 30; f++) yield return null;

            Vector3 lo = turf.centers[0], hi = turf.centers[0];
            foreach (var c in turf.centers) { lo = Vector3.Min(lo, c); hi = Vector3.Max(hi, c); }
            Vector3 mid = (lo + hi) * 0.5f; float span = Mathf.Max(hi.x - lo.x, hi.z - lo.z) + 60f;
            int shots = 0;
            var views = new System.Collections.Generic.List<(string, Vector3, Vector3)>
            {
                ("town-air", mid + new Vector3(-span * 0.55f, span * 0.75f, -span * 0.55f), mid),
                ("town-top", mid + new Vector3(0.1f, span * 1.05f, 0f), mid),
            };
            foreach (var (name, id) in new[] { ("town-midtown", 30), ("town-downtown", 50), ("town-far-corner", 60), ("town-edge", 33) })
            {
                if (turf.IndexOf(id) < 0) continue;
                var c = turf.Center(id);
                views.Add((name, c + new Vector3(29f, 5f, 29f), c + Vector3.up * 5f));
            }
            foreach (var (name, eye, look) in views)
            {
                cam.transform.position = eye; cam.transform.LookAt(look);
                for (int f = 0; f < 4; f++) yield return null;
                var snap = cam.gameObject.AddComponent<LateSnap>(); snap.cam = cam; snap.rt = rt;
                while (!snap.done) yield return null;
                Object.Destroy(snap);
                var prev = RenderTexture.active; RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply(); RenderTexture.active = prev;
                string b64 = System.Convert.ToBase64String(tex.EncodeToJPG(72));
                int parts = (b64.Length + 5999) / 6000;
                for (int i = 0; i < parts; i++)
                    Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[LookShot] {0} {1}/{2} {3}", name, i + 1, parts, b64.Substring(i * 6000, Mathf.Min(6000, b64.Length - i * 6000)));
                shots++;
            }
            GameClock.Frozen = wasFrozen; GameClock.FrozenHour = wasHour;
            Object.Destroy(rt); Object.Destroy(tex);
            Assert.GreaterOrEqual(shots, 2, "the town from the air");
        }
    }
}
