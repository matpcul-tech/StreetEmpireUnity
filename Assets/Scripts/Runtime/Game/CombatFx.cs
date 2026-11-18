// Fight feedback from the web build (main.js: lockRing, tellRings, floatHit, parts.burst): a red ring under the one
// you would hit, a warning ring under anyone winding up a swing (red for a rush, blue for a flank), the damage as a
// number rising off the body, sparks and dust on every hit, and a ring of dust running out over the ground on a
// heavy hit or a knockout. Health.Damage reports every hit here.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StreetEmpire.Game
{
    public class CombatFx : MonoBehaviour
    {
        public static CombatFx Instance { get; private set; }
        public RectTransform canvas; public Font font;
        public Material ringMat, particleMat;
        public float lockRange = 4f;

        class Num { public Text t; public Vector3 at; public float life; }
        class Wave { public Transform tr; public float t; public Color c; public float size; }
        readonly List<Num> nums = new(); readonly List<Wave> waves = new();
        readonly List<Transform> tells = new(); Transform lockRing;
        ParticleSystem sparks, dust; MaterialPropertyBlock mpb; Mesh ring;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            Instance = this; mpb = new MaterialPropertyBlock(); ring = Ring(0.62f, 0.84f, 28);
            lockRing = MakeRing("Lock Ring");
            for (int i = 0; i < 6; i++) tells.Add(MakeRing("Tell Ring " + i));
            for (int i = 0; i < 4; i++) waves.Add(new Wave { tr = MakeRing("Shockwave " + i), t = 9f });
            sparks = MakeParticles("Hit Sparks", 0.25f, 6f, 0.07f);
            dust = MakeParticles("Hit Dust", 0.7f, 1.5f, 0.22f);
            if (canvas && font)
                for (int i = 0; i < 8; i++)
                {
                    var go = new GameObject("Hit Number " + i, typeof(RectTransform)); go.transform.SetParent(canvas, false);
                    var t = go.AddComponent<Text>(); t.font = font; t.fontSize = 30; t.fontStyle = FontStyle.Bold; t.alignment = TextAnchor.MiddleCenter;
                    t.raycastTarget = false; ((RectTransform)go.transform).sizeDelta = new Vector2(220, 44);
                    var sh = go.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.85f); sh.effectDistance = new Vector2(2, -2);
                    go.SetActive(false); nums.Add(new Num { t = t, life = 0 });
                }
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        static Mesh Ring(float r0, float r1, int seg)
        {
            var v = new Vector3[seg * 2]; var tri = new int[seg * 6];
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v[i * 2] = d * r0; v[i * 2 + 1] = d * r1;
                int n = (i + 1) % seg;
                tri[i * 6] = i * 2; tri[i * 6 + 1] = n * 2 + 1; tri[i * 6 + 2] = i * 2 + 1;
                tri[i * 6 + 3] = i * 2; tri[i * 6 + 4] = n * 2; tri[i * 6 + 5] = n * 2 + 1;
            }
            var m = new Mesh { name = "Fx Ring" }; m.vertices = v; m.triangles = tri; m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        Transform MakeRing(string name)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = ring;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = ringMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            go.SetActive(false); return go.transform;
        }

        ParticleSystem MakeParticles(string name, float life, float gravity, float size)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.playOnAwake = false; main.loop = true;   // emission is off: it only simulates what Burst emits
            main.startLifetime = life; main.startSize = size;
            main.gravityModifier = gravity / 9.81f; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 200;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col.color = g;
            var pr = go.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = particleMat; pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        void Burst(ParticleSystem ps, Vector3 at, int n, float speed, Color c, Vector3 dir, float up)
        {
            if (!ps) return;
            for (int i = 0; i < n; i++)
            {
                var v = Random.insideUnitSphere * speed + dir * speed * 0.8f; v.y = Mathf.Abs(v.y) * 0.6f + up * speed;
                ps.Emit(new ParticleSystem.EmitParams { position = at + Random.insideUnitSphere * 0.08f, velocity = v, startColor = c }, 1);
            }
        }

        /// <summary>Health.Damage calls this for every hit that lands.</summary>
        public static void Report(Health target, float amount, GameObject source)
        {
            if (Instance == null || target == null) return;
            Instance.OnHit(target, amount, source);
        }

        void OnHit(Health target, float amount, GameObject source)
        {
            var at = target.transform.position;
            var s = GameSession.Instance; bool onPlayer = s != null && s.player != null && target.gameObject == s.player.gameObject;
            var dir = source ? Vector3.ProjectOnPlane(at - source.transform.position, Vector3.up).normalized : Vector3.zero;
            bool heavy = amount >= 14f, ko = target.Down;
            Burst(sparks, at + Vector3.up * 1.25f, heavy ? 10 : 6, 4f, new Color(1f, 0.82f, 0.48f), dir, 0.2f);
            Burst(dust, at + Vector3.up * 0.25f, ko ? 12 : 5, 2f, new Color(0.6f, 0.5f, 0.39f), Vector3.zero, 0.4f);
            if (heavy || ko) Shockwave(at, ko ? 3.2f : 2.2f, ko ? new Color(1f, 0.85f, 0.6f, 0.75f) : new Color(1f, 0.92f, 0.75f, 0.55f));
            foreach (var n in nums)
            {
                if (n.life > 0) continue;
                n.life = 0.75f; n.at = at + Vector3.up * 1.6f + Random.insideUnitSphere * 0.15f;
                n.t.text = Mathf.RoundToInt(amount).ToString(); n.t.color = onPlayer ? new Color(1f, 0.47f, 0.47f) : heavy ? new Color(1f, 0.82f, 0.48f) : new Color(1f, 0.95f, 0.77f);
                n.t.fontSize = heavy ? 36 : 30; n.t.gameObject.SetActive(true); break;
            }
        }

        void Shockwave(Vector3 at, float size, Color c)
        {
            Wave w = waves[0]; foreach (var x in waves) if (x.t > w.t) w = x;
            w.t = 0; w.c = c; w.size = size; w.tr.position = new Vector3(at.x, at.y + 0.06f, at.z); w.tr.gameObject.SetActive(true);
        }

        void SetColor(Transform tr, Color c) { var r = tr.GetComponent<MeshRenderer>(); mpb.SetColor(BaseColor, c); r.SetPropertyBlock(mpb); }

        void LateUpdate()
        {
            float dt = Time.deltaTime; var s = GameSession.Instance; var cam = Camera.main;
            // the one you would hit next
            Health target = null;
            if (s != null && s.player != null && !s.IsDown)
            {
                var p = s.player.transform; float bd = lockRange * lockRange;
                foreach (var b in Brawler.All)
                {
                    if (b == null) continue; var h = b.GetComponent<Health>(); if (h == null || h.Down) continue;
                    var to = b.transform.position - p.position; to.y = 0;
                    if (to.sqrMagnitude < bd && (to.sqrMagnitude < 0.5f || Vector3.Dot(p.forward, to.normalized) > 0.2f)) { bd = to.sqrMagnitude; target = h; }
                }
            }
            lockRing.gameObject.SetActive(target != null);
            if (target != null)
            {
                lockRing.position = target.transform.position + Vector3.up * 0.07f; lockRing.rotation = Quaternion.Euler(0, Time.time * 140f, 0);
                lockRing.localScale = Vector3.one * 0.95f; SetColor(lockRing, new Color(1f, 0.25f, 0.19f, 0.85f));
            }
            // warning rings under anyone winding up
            int ti = 0;
            foreach (var b in Brawler.All)
            {
                if (ti >= tells.Count) break;
                if (b == null || b.Windup <= 0) continue;
                var tr = tells[ti++]; tr.gameObject.SetActive(true);
                tr.position = b.transform.position + Vector3.up * 0.06f;
                float k = Mathf.Min(1f, b.Windup / 0.5f); tr.localScale = Vector3.one * (0.85f + (1f - k) * 0.5f);
                var c = b.style == Brawler.Style.Flank ? new Color(0.27f, 0.87f, 1f) : new Color(1f, 0.33f, 0.19f);
                c.a = 0.55f + 0.35f * Mathf.Min(1f, b.Windup / 0.36f); SetColor(tr, c);
            }
            for (; ti < tells.Count; ti++) tells[ti].gameObject.SetActive(false);
            // shockwaves run out and fade
            foreach (var w in waves)
            {
                if (w.t > 0.45f) { w.tr.gameObject.SetActive(false); continue; }
                w.t += dt; float k = Mathf.Clamp01(w.t / 0.45f);
                w.tr.localScale = Vector3.one * Mathf.Lerp(0.6f, w.size, 1f - (1f - k) * (1f - k));
                var c = w.c; c.a *= 1f - k; SetColor(w.tr, c);
            }
            // hit numbers rise and fade
            foreach (var n in nums)
            {
                if (n.life <= 0) continue;
                n.life -= dt; n.at += Vector3.up * dt * 0.9f;
                if (n.life <= 0 || cam == null || canvas == null) { n.t.gameObject.SetActive(false); n.life = 0; continue; }
                var sp = cam.WorldToScreenPoint(n.at);
                if (sp.z < 0) { n.t.gameObject.SetActive(false); continue; }
                n.t.gameObject.SetActive(true);
                float sx = canvas.rect.width / Screen.width, sy = canvas.rect.height / Screen.height;
                ((RectTransform)n.t.transform).anchoredPosition = new Vector2(sp.x * sx - canvas.rect.width / 2f, sp.y * sy - canvas.rect.height / 2f);
                var c = n.t.color; c.a = Mathf.Clamp01(n.life / 0.3f); n.t.color = c;
            }
        }
    }
}
