// Lights and damage for every car that drives (the player's, the cruisers, any parked vehicle that was taken).
// Lights: two headlamp glows and a beam at night while the engine runs (someone drives it), two tail glows that
// brighten under braking. Damage: the car's hp already falls when it hits a wall; under 60% the hood smokes, under
// 30% the smoke is dark, at zero it burns, the body chars and the engine is dead (CarController.Wrecked).
// Everything is made at run time from CombatFx's soft sprite, so no new assets ship.
using StreetEmpire.City;
using StreetEmpire.Game;
using UnityEngine;

namespace StreetEmpire.Vehicles
{
    [RequireComponent(typeof(CarController))]
    public class CarFx : MonoBehaviour
    {
        public bool HeadlightsOn { get; private set; }
        public bool Smoking => smoke != null && smoke.emission.enabled;

        CarController car; BoxCollider box; DayCycle day;
        GameObject[] heads, tails; Light beam; ParticleSystem smoke, fire; bool charred;
        static Material headMat, tailMat, brakeMat; static Mesh quad;

        void Start()
        {
            car = GetComponent<CarController>(); box = GetComponent<BoxCollider>();
            day = Object.FindFirstObjectByType<DayCycle>();
        }

        // lamps sit on the ends of the body box, a little in from its sides
        bool Build()
        {
            var fx = CombatFx.Instance; if (fx == null || fx.particleMat == null) return false;
            if (headMat == null)
            {
                headMat = Glow(fx.particleMat, new Color(1f, 0.95f, 0.8f, 0.95f) * 1.6f);
                tailMat = Glow(fx.particleMat, new Color(1f, 0.08f, 0.05f, 0.55f));
                brakeMat = Glow(fx.particleMat, new Color(1f, 0.1f, 0.06f, 1f) * 2.2f);
                quad = Quad();
            }
            Vector3 c = box.center, s = box.size; float bottom = c.y - s.y * 0.5f, y = bottom + Mathf.Clamp(s.y * 0.42f, 0.55f, 0.95f), x = s.x * 0.33f;
            float front = c.z + s.z * 0.5f + 0.03f, back = c.z - s.z * 0.5f - 0.03f;
            heads = new[] { Lamp("Headlamp L", new Vector3(c.x - x, y, front), 0.5f, headMat, false), Lamp("Headlamp R", new Vector3(c.x + x, y, front), 0.5f, headMat, false) };
            tails = new[] { Lamp("Tail lamp L", new Vector3(c.x - x, y + 0.08f, back), 0.38f, tailMat, true), Lamp("Tail lamp R", new Vector3(c.x + x, y + 0.08f, back), 0.38f, tailMat, true) };
            var b = new GameObject("Headlight beam"); b.transform.SetParent(transform, false);
            b.transform.localPosition = new Vector3(c.x, y + 0.1f, front); b.transform.localRotation = Quaternion.Euler(9f, 0f, 0f);
            beam = b.AddComponent<Light>(); beam.type = LightType.Spot; beam.range = 26f; beam.spotAngle = 72f; beam.innerSpotAngle = 38f;
            beam.color = new Color(1f, 0.95f, 0.82f); beam.intensity = 9f; beam.shadows = LightShadows.None; beam.enabled = false;
            return true;
        }

        static Material Glow(Material from, Color c) { var m = new Material(from) { name = "Car lamp" }; m.SetColor("_BaseColor", c); return m; }

        static Mesh Quad()
        {
            var m = new Mesh { name = "Car lamp quad" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 }; m.RecalculateBounds(); return m;
        }

        GameObject Lamp(string name, Vector3 at, float size, Material mat, bool rear)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            go.transform.localPosition = at; go.transform.localRotation = Quaternion.Euler(0f, rear ? 180f : 0f, 0f); go.transform.localScale = Vector3.one * size;
            go.AddComponent<MeshFilter>().sharedMesh = quad;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            go.SetActive(false); return go;
        }

        void Update()
        {
            if (car == null) return;
            if (heads == null && !Build()) return;
            bool engine = (car.hasDriver || car.aiDriven) && !car.Wrecked;
            bool dark = day != null && day.Night > 0.3f;
            HeadlightsOn = engine && dark;
            foreach (var h in heads) if (h.activeSelf != HeadlightsOn) h.SetActive(HeadlightsOn);
            // one real beam, for the car the player drives: lights are dear on a phone
            bool beamOn = HeadlightsOn && car.hasDriver; if (beam.enabled != beamOn) beam.enabled = beamOn;
            bool tailOn = engine && (dark || car.Braking);
            foreach (var t in tails)
            {
                if (t.activeSelf != tailOn) t.SetActive(tailOn);
                if (tailOn) t.GetComponent<MeshRenderer>().sharedMaterial = car.Braking ? brakeMat : tailMat;
            }
            Damage();
        }

        void Damage()
        {
            float f = car.stats.hp > 0f ? car.hp / car.stats.hp : 1f;
            if (f >= 0.6f) { if (smoke != null && smoke.emission.enabled) { var e0 = smoke.emission; e0.enabled = false; } return; }
            Vector3 c = box.center, s = box.size; var hood = new Vector3(c.x, c.y + s.y * 0.1f, c.z + s.z * 0.32f);
            if (smoke == null) smoke = Puffs("Engine smoke", hood, 1.6f, 0.9f, 1.4f);
            var e = smoke.emission; e.enabled = true; e.rateOverTime = f <= 0f ? 16f : f < 0.3f ? 11f : 5f;
            var main = smoke.main; float g = f <= 0f ? 0.05f : f < 0.3f ? 0.16f : 0.55f; main.startColor = new Color(g, g, g, f < 0.3f ? 0.75f : 0.45f);
            if (f > 0f || charred) return;
            charred = true;
            fire = Puffs("Engine fire", hood, 0.55f, 0.5f, 1.8f);
            var fm = fire.main; fm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0.08f, 0.9f), new Color(1f, 0.8f, 0.2f, 0.9f));
            var fe = fire.emission; fe.enabled = true; fe.rateOverTime = 14f;
            // the body chars
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", new Color(0.22f, 0.2f, 0.19f));
            foreach (var r in GetComponentsInChildren<MeshRenderer>()) if (r.GetComponent<ParticleSystem>() == null && !r.name.Contains("lamp")) r.SetPropertyBlock(block);
        }

        ParticleSystem Puffs(string name, Vector3 at, float life, float size, float rise)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false); go.transform.localPosition = at;
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.startLifetime = life; main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(rise * 0.6f, rise); main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 60; main.loop = true; main.playOnAwake = false;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 14f; shape.radius = 0.25f; shape.rotation = new Vector3(-90f, 0f, 0f);
            var grow = ps.sizeOverLifetime; grow.enabled = true; grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.8f));
            var fade = ps.colorOverLifetime; fade.enabled = true; var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            fade.color = gr;
            var pr = go.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = CombatFx.Instance.particleMat; pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play(); return ps;
        }
    }
}
