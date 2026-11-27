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
            MuggerOpening.AutoStart = false; BlockLife.AutoStart = true; TrafficFlow.AutoStart = true; SceneManager.LoadScene("Eastside");
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
                    if (hour != 14f && kind != "coupe" && kind != "limo" && kind != "suv" && kind != "school-bus" && kind != "city-bus") continue;
                    var m = marks.FirstOrDefault(x => x.model == kind); if (m == null) continue;
                    var box = m.GetComponent<BoxCollider>(); var t = m.transform;
                    Vector3 c = t.TransformPoint(box.center); float len = Mathf.Max(box.size.z, box.size.x);
                    // the street side, three quarters from the front
                    Vector3 side = Vector3.Dot(t.right, -c) > Vector3.Dot(-t.right, -c) ? t.right : -t.right;
                    // the buses also from the far side and the back: their glass wraps the whole body
                    bool bus = kind == "city-bus" || kind == "school-bus";
                    var views = bus && hour == 14f ? new[] { side, -side, -t.forward } : new[] { side };
                    for (int vi = 0; vi < views.Length; vi++)
                    foreach (var p in vi < 2 ? FindSide(c, t, views[vi], len) : new[] { c - t.forward * Mathf.Max(5.5f, len * 0.95f) + t.right * 2f + Vector3.up * 1.5f })
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
                            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[LookShot] {0}-h{1}{5} {2}/{3} {4}", kind, hour, i + 1, parts, b64.Substring(i * 6000, Mathf.Min(6000, b64.Length - i * 6000)), vi == 0 ? "" : vi == 1 ? "-far" : "-back");
                        shots++;
                        break;
                    }
                }
            }
            // the neon at night: the first few different signs, from the street in front of each
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var sign in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("Neon ")).OrderBy(r => r.name))
            {
                if (seen.Count >= 6 || !seen.Add(sign.name)) continue;
                var st = sign.transform; Vector3 at = st.position;
                foreach (float back in new[] { 16f, 24f })
                {
                    Vector3 p = at + st.forward * back; p.y = 1.7f;
                    cam.transform.position = p; cam.transform.LookAt(Vector3.Lerp(at, new Vector3(at.x, 2f, at.z), 0.45f));
                    yield return null;
                    var snap = cam.gameObject.AddComponent<LateSnap>(); snap.cam = cam; snap.rt = rt;
                    while (!snap.done) yield return null;
                    Object.Destroy(snap);
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply(); RenderTexture.active = prev;
                    string b64 = System.Convert.ToBase64String(tex.EncodeToJPG(72));
                    int parts = (b64.Length + 5999) / 6000;
                    for (int i = 0; i < parts; i++)
                        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[LookShot] neon-{0}-{1}m {2}/{3} {4}", sign.name.Substring(5), back, i + 1, parts, b64.Substring(i * 6000, Mathf.Min(6000, b64.Length - i * 6000)));
                    shots++;
                }
            }
            // a traffic car's lights at night: from ahead, from behind and above (where the headlamps must not show
            // as discs past the hood), and from the side
            GameClock.FrozenHour = 22f;
            for (int f = 0; f < 30; f++) yield return null;
            var lit = JobCar.All.FirstOrDefault(c => c != null && c.GetComponent<StreetEmpire.Vehicles.CarLamps>() != null && c.GetComponent<StreetEmpire.Vehicles.CarLamps>().On);
            if (lit == null) Debug.Log("[LookShot] no traffic car with its lights on to photograph");
            else
                foreach (var view in new[] { "ahead", "behind", "side" })
                {
                    var ct = lit.transform; Vector3 mid = ct.position + Vector3.up * 0.8f;
                    cam.transform.position = view == "ahead" ? mid + ct.forward * 8f + Vector3.up * 0.6f : view == "behind" ? mid - ct.forward * 7f + Vector3.up * 4f : mid + ct.right * 7f + Vector3.up * 1f;
                    cam.transform.LookAt(mid);
                    yield return null;
                    var snap = cam.gameObject.AddComponent<LateSnap>(); snap.cam = cam; snap.rt = rt;
                    while (!snap.done) yield return null;
                    Object.Destroy(snap);
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply(); RenderTexture.active = prev;
                    string b64 = System.Convert.ToBase64String(tex.EncodeToJPG(72));
                    int parts = (b64.Length + 5999) / 6000;
                    for (int i = 0; i < parts; i++)
                        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[LookShot] lamps-{0} {1}/{2} {3}", view, i + 1, parts, b64.Substring(i * 6000, Mathf.Min(6000, b64.Length - i * 6000)));
                    shots++;
                }
            TrafficFlow.AutoStart = false;
            // the people on the corners by day, from the front and the side: how they hold their heads
            GameClock.FrozenHour = 14f;
            for (int f = 0; f < 30; f++) yield return null;
            int people = 0;
            foreach (var who in Object.FindObjectsByType<Loiterer>(FindObjectsSortMode.None).OrderBy(l => l.name))
            {
                if (people >= 6) break;
                var wt = who.transform; Vector3 eye = wt.position + Vector3.up * 1.45f;
                foreach (var side in new[] { "front", "side" })
                {
                    Vector3 dir = side == "front" ? wt.forward : wt.right;
                    cam.transform.position = eye + dir * 2.4f; cam.transform.LookAt(eye - Vector3.up * 0.25f);
                    yield return null;
                    var snap = cam.gameObject.AddComponent<LateSnap>(); snap.cam = cam; snap.rt = rt;
                    while (!snap.done) yield return null;
                    Object.Destroy(snap);
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply(); RenderTexture.active = prev;
                    string b64 = System.Convert.ToBase64String(tex.EncodeToJPG(72));
                    int parts = (b64.Length + 5999) / 6000;
                    for (int i = 0; i < parts; i++)
                        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[LookShot] person-{0}-{1} {2}/{3} {4}", who.name.Replace(' ', '_'), side, i + 1, parts, b64.Substring(i * 6000, Mathf.Min(6000, b64.Length - i * 6000)));
                    shots++;
                }
                people++;
            }
            // the dice game on the home corner, with a throw on the ground
            StreetDice.AutoStart = true;
            var sd = StreetDice.Instance; var pm = Object.FindFirstObjectByType<StreetEmpire.Player.PlayerMotor>();
            float diceUntil = Time.time + 5f; Vector3 dspot = default;
            while (sd != null && !sd.Spot(0, out dspot) && Time.time < diceUntil) yield return null;
            if (sd != null && pm != null && sd.Spot(0, out dspot))
            {
                Vector3 dout = dspot - TurfWar.Instance.Center(0); dout.y = 0f; dout.Normalize();
                Vector3 dside = Vector3.Cross(Vector3.up, dout);
                pm.Teleport(dspot + dout * 2.4f + Vector3.up * 0.3f, 180f);
                for (int f = 0; f < 50; f++) yield return null;
                sd.Tap(5);
                for (int f = 0; f < 90; f++) yield return null;
                foreach (var (shotName, eyeOff) in new[] { ("dice-corner", dout * 4.4f + dside * 1.6f + Vector3.up * 2.3f), ("dice-close", dout * 1.0f + dside * 0.4f + Vector3.up * 1.9f) })
                {
                    cam.transform.position = dspot + eyeOff; cam.transform.LookAt(dspot + Vector3.up * 0.1f);
                    yield return null;
                    var snap = cam.gameObject.AddComponent<LateSnap>(); snap.cam = cam; snap.rt = rt;
                    while (!snap.done) yield return null;
                    Object.Destroy(snap);
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply(); RenderTexture.active = prev;
                    string b64 = System.Convert.ToBase64String(tex.EncodeToJPG(72));
                    int parts = (b64.Length + 5999) / 6000;
                    for (int i = 0; i < parts; i++)
                        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[LookShot] {0} {1}/{2} {3}", shotName, i + 1, parts, b64.Substring(i * 6000, Mathf.Min(6000, b64.Length - i * 6000)));
                    shots++;
                }
            }
            StreetDice.AutoStart = false;
            BlockLife.AutoStart = false;
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
