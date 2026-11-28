// Ground shades: a soft dark patch on the road under every car and person near the camera. The sun's shadow map
// only reaches so far and is gone at night, and screen space occlusion is too faint under the night ambient, so
// cars and people looked set on top of the street (phone, builds de79b58 to 001608c). A patch per body is what
// holds them down. A small pool of quads is moved each frame to whoever is nearest; nothing ships as an asset
// (the soft textures are made here, the material is a copy of CombatFx's alpha blended particle material).
using System.Collections.Generic;
using StreetEmpire.Game;
using StreetEmpire.Vehicles;
using UnityEngine;

namespace StreetEmpire.City
{
    [DefaultExecutionOrder(900)]
    public class GroundShades : MonoBehaviour
    {
        public static GroundShades Instance { get; private set; }
        public static bool On = true;
        /// <summary>Bodies farther than this from the camera get no patch.</summary>
        public const float Range = 60f;
        public const int Max = 56;
        public const float CarShade = 0.62f, PersonShade = 0.5f, Lift = 0.035f;
        /// <summary>How many patches are out this frame, and how many of them lie under cars.</summary>
        public int Drawn { get; private set; }
        public int UnderCars { get; private set; }

        struct Body { public Transform t; public Collider c; public bool car; public Vector3 center, size; }
        readonly List<Body> bodies = new();
        readonly List<Transform> pool = new(); readonly List<MeshRenderer> poolR = new();
        readonly HashSet<Transform> seen = new();
        static readonly Collider[] found = new Collider[768];
        static readonly RaycastHit[] hits = new RaycastHit[8];
        Material carMat, personMat; Mesh quad; float next; Camera cam; Vector3 lastEye;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; if (carMat) Destroy(carMat); if (personMat) Destroy(personMat); }

        bool Ready()
        {
            if (carMat != null) return true;
            var fx = CombatFx.Instance; if (fx == null || fx.particleMat == null) return false;
            carMat = Shade(fx.particleMat, Soft(true), CarShade); personMat = Shade(fx.particleMat, Soft(false), PersonShade);
            quad = new Mesh { name = "Ground shade" };
            quad.vertices = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, 0.5f) };
            quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            quad.triangles = new[] { 0, 2, 1, 0, 3, 2 }; quad.RecalculateNormals(); quad.RecalculateBounds();
            return true;
        }

        static Material Shade(Material from, Texture2D tex, float alpha)
        {
            var m = new Material(from) { name = "Ground shade" };
            m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", new Color(0f, 0f, 0f, alpha));
            m.SetFloat("_Cull", 0f); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 20;   // under glows and dust
            return m;
        }

        /// <summary>White with a soft edge: a rounded box for a car's footprint, a round blob for a person.</summary>
        static Texture2D Soft(bool box)
        {
            const int n = 64; var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = box ? "Car shade" : "Person shade", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Abs((x + 0.5f) / n * 2f - 1f), dy = Mathf.Abs((y + 0.5f) / n * 2f - 1f), a;
                    if (box)
                    {
                        // distance in from the edge of a box with rounded corners, full strength 40% of the way in
                        float qx = Mathf.Max(dx - 0.55f, 0f), qy = Mathf.Max(dy - 0.55f, 0f);
                        a = Mathf.Clamp01((0.45f - Mathf.Sqrt(qx * qx + qy * qy)) / 0.4f);
                    }
                    else a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    px[y * n + x] = new Color(1f, 1f, 1f, a * a * (3f - 2f * a));
                }
            t.SetPixels32(px); t.Apply(true, true); return t;
        }

        void Gather(Vector3 eye)
        {
            bodies.Clear(); seen.Clear();
            int n = Physics.OverlapSphereNonAlloc(eye, Range, found, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n && bodies.Count < Max; i++)
            {
                var c = found[i]; if (c == null) continue;
                if (c is CharacterController cc)
                {
                    if (seen.Add(c.transform)) bodies.Add(new Body { t = c.transform, c = c, car = false, center = cc.center, size = new Vector3(cc.radius, cc.height, cc.radius) });
                    continue;
                }
                if (c is not BoxCollider b) continue;
                var t = c.transform;
                bool car = t.GetComponent<CarController>() != null || t.GetComponent<JobCar>() != null;
                if (!car) { var mk = t.GetComponent<PropMarker>(); car = mk != null && mk.kind == "parked car"; }
                if (car && seen.Add(t)) bodies.Add(new Body { t = t, c = c, car = true, center = b.center, size = b.size });
            }
        }

        /// <summary>The ground under a body: the first thing below its middle that is not the body itself.</summary>
        static bool Ground(Body b, Vector3 from, out float y)
        {
            int n = Physics.RaycastNonAlloc(from, Vector3.down, hits, 5f, ~0, QueryTriggerInteraction.Ignore);
            y = 0f; float best = float.MaxValue; bool any = false;
            for (int i = 0; i < n; i++)
            {
                var h = hits[i]; if (h.collider == null || h.collider.transform.IsChildOf(b.t)) continue;
                if (h.collider is CharacterController || h.collider.GetComponent<CarController>() != null) continue;   // not onto a rider's bike or a man below
                if (h.distance < best) { best = h.distance; y = h.point.y; any = true; }
            }
            return any;
        }

        void Update()
        {
            if (!On || !Ready()) { Hide(0); Drawn = UnderCars = 0; return; }
            if (cam == null) cam = Camera.main;
            if (cam == null) { Hide(0); return; }
            // look again twice a second, and at once when the camera has jumped (a cut, a respawn, a ride across town)
            Vector3 eye = cam.transform.position;
            if (Time.unscaledTime >= next || (eye - lastEye).sqrMagnitude > 64f) { next = Time.unscaledTime + 0.5f; lastEye = eye; Gather(eye); }
            int used = 0, cars = 0;
            foreach (var b in bodies)
            {
                if (b.t == null || b.c == null || !b.c.enabled || !b.t.gameObject.activeInHierarchy) continue;
                Vector3 mid = b.t.TransformPoint(b.center);
                if (!Ground(b, mid, out float y)) continue;
                var q = Take(used++);
                if (b.car)
                {
                    // a touch wider and longer than the body, fading as the car leaves the ground (a jump, a roll)
                    float up = Mathf.Max(0f, mid.y - b.size.y * 0.5f - y);
                    q.SetPositionAndRotation(new Vector3(mid.x, y + Lift, mid.z), Quaternion.Euler(0f, b.t.eulerAngles.y, 0f));
                    float grow = 1f + Mathf.Clamp01(up / 2f) * 0.5f;
                    q.localScale = new Vector3((b.size.x + 0.5f) * grow, 1f, (b.size.z + 0.45f) * grow);
                    poolR[used - 1].sharedMaterial = carMat; cars++;
                }
                else
                {
                    q.SetPositionAndRotation(new Vector3(mid.x, y + Lift, mid.z), Quaternion.identity);
                    float w = Mathf.Max(0.9f, b.size.x * 3.2f);
                    q.localScale = new Vector3(w, 1f, w);
                    poolR[used - 1].sharedMaterial = personMat;
                }
            }
            Hide(used); Drawn = used; UnderCars = cars;
        }

        Transform Take(int i)
        {
            while (pool.Count <= i)
            {
                var go = new GameObject("Shade"); go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = quad;
                var r = go.AddComponent<MeshRenderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                pool.Add(go.transform); poolR.Add(r);
            }
            if (!pool[i].gameObject.activeSelf) pool[i].gameObject.SetActive(true);
            return pool[i];
        }

        void Hide(int from) { for (int i = from; i < pool.Count; i++) if (pool[i].gameObject.activeSelf) pool[i].gameObject.SetActive(false); }
    }
}
