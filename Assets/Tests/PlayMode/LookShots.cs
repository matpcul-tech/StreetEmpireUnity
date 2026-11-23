// Eyes on the build before it goes to a phone: renders parked cars with the game's own camera and pipeline and
// writes each frame to the test log as base64 JPEG ("[LookShot] name part/parts data"). The log is read back from
// the CI job, so a look change is checked on real frames, not guessed.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StreetEmpire.City;
using StreetEmpire.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [DefaultExecutionOrder(20000)]
    public class LateSnap : MonoBehaviour
    {
        public Camera cam; public RenderTexture rt; public bool done;
        void LateUpdate() { if (done) return; cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; done = true; }
    }

    [PrebuildSetup(typeof(SceneSetup))]
    public class LookShots
    {
        static readonly string[] Kinds = { "sedan-clean", "sedan", "coupe", "lowrider", "suv", "pickup", "van", "taxi", "police", "hatchback", "minivan",
            "luxury-sedan", "limo", "school-bus", "city-bus", "ambulance", "armored-truck", "tow-truck", "fire-engine", "garbage-truck", "swat-van", "dump-truck" };

        [UnityTest, Timeout(900000)]
        public IEnumerator ParkedCarsOnCamera()
        {
            MuggerOpening.AutoStart = false; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            bool wasFrozen = GameClock.Frozen; float wasHour = GameClock.FrozenHour;
            GameClock.Frozen = true; GameClock.FrozenHour = 14f;
            BuildingCuller.On = false;
            var hud = GameObject.Find("HUD"); if (hud) hud.SetActive(false);
            var cam = Camera.main; Assert.IsNotNull(cam, "the game camera");
            foreach (var mb in cam.GetComponents<MonoBehaviour>()) if (mb.GetType().Name == "CinemachineBrain") mb.enabled = false;
            cam.fieldOfView = 50f; cam.farClipPlane = 600f;
            var rt = new RenderTexture(640, 360, 24); var tex = new Texture2D(640, 360, TextureFormat.RGB24, false);
            var marks = Object.FindObjectsByType<PropMarker>(FindObjectsSortMode.None).Where(m => m.kind == "parked car").ToArray();
            int shots = 0;
            foreach (float hour in new[] { 14f, 7.5f, 22f })
            {
                GameClock.FrozenHour = hour;
                yield return null; yield return null; yield return null;
                foreach (string kind in Kinds)
                {
                    if (hour != 14f && kind != "coupe" && kind != "limo" && kind != "suv" && kind != "school-bus") continue;
                    var m = marks.FirstOrDefault(x => x.model == kind); if (m == null) continue;
                    var box = m.GetComponent<BoxCollider>(); var t = m.transform;
                    Vector3 c = t.TransformPoint(box.center); float len = Mathf.Max(box.size.z, box.size.x);
                    // the street side, three quarters from the front
                    Vector3 side = Vector3.Dot(t.right, -c) > Vector3.Dot(-t.right, -c) ? t.right : -t.right;
                    foreach (var p in FindSide(c, t, side, len))
                    {
                        cam.transform.position = p; cam.transform.LookAt(c + Vector3.up * 0.1f);
                        // the instanced props (cars included) are queued in PropInstancer.LateUpdate for that frame only,
                        // so the frame is taken by a late LateUpdate of the same frame
                        yield return null;
                        var snap = cam.gameObject.AddComponent<LateSnap>(); snap.cam = cam; snap.rt = rt;
                        while (!snap.done) yield return null;
                        Object.Destroy(snap);
                        var prev = RenderTexture.active; RenderTexture.active = rt;
                        tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply(); RenderTexture.active = prev;
                        string b64 = System.Convert.ToBase64String(tex.EncodeToJPG(72));
                        int parts = (b64.Length + 5999) / 6000;
                        for (int i = 0; i < parts; i++)
                            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[LookShot] {0}-h{1} {2}/{3} {4}", kind, hour, i + 1, parts, b64.Substring(i * 6000, Mathf.Min(6000, b64.Length - i * 6000)));
                        shots++;
                        break;
                    }
                }
            }
            GameClock.Frozen = wasFrozen; GameClock.FrozenHour = wasHour; BuildingCuller.On = true;
            Object.Destroy(rt); Object.Destroy(tex);
            Assert.Greater(shots, 8, "frames of parked cars");
        }

        // camera spots around the car, the clearest first: nothing between the camera and the car
        static System.Collections.Generic.IEnumerable<Vector3> FindSide(Vector3 c, Transform t, Vector3 side, float len)
        {
            float back = Mathf.Max(5.5f, len * 0.95f);
            var spots = new[] { side * 0.8f + t.forward * 0.6f, side * 0.8f - t.forward * 0.6f, -side * 0.8f + t.forward * 0.6f, -side * 0.8f - t.forward * 0.6f };
            foreach (var d in spots)
            {
                Vector3 p = c + d.normalized * back + Vector3.up * 1.5f;
                if (!Physics.CheckSphere(p, 0.6f) && (!Physics.Linecast(p, c + Vector3.up * 0.6f, out var hit) || hit.collider.transform == t)) { yield return p; yield break; }
            }
            yield return c + spots[0].normalized * back + Vector3.up * 1.5f;
        }
    }
}
