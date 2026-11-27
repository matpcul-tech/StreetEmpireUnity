// A car that drives itself around the town's streets for a job: the one you tail ("Shadow") and the one you stop
// ("Repo"). It keeps to the street center lines the session knows (a lane to the right of the line), turns at some
// crossings and turns round at the edge of town. No physics: it is a marked car to follow or ram, and the player's
// car treats it as solid (CarController).
using System.Linq;
using StreetEmpire.City;
using UnityEngine;

namespace StreetEmpire.Game
{
    public class JobCar : MonoBehaviour
    {
        public float speed = 8f;
        public bool stopped;
        public Vector3 Position => alongZ ? new Vector3(line, 0, along) : new Vector3(along, 0, line);

        /// <summary>Traffic (TrafficFlow) keeps the rules of the road; a job's marked car does not.</summary>
        public bool obeyLights;
        /// <summary>Somebody's car, there to be taken (traffic). A job's own car is not.</summary>
        public bool jackable;
        public float Speed => v;
        public static readonly System.Collections.Generic.List<JobCar> All = new();

        bool alongZ; int dir = 1; float line, along, slow, v, signAt = float.NaN, signT, waitT, pushT;
        const float Lane = 2.2f, StopNear = 11.5f, StopFar = 14.5f;
        static TrafficSignalCycle lights;

        /// <summary>A marked car on the street nearest to a point, wearing a parked sedan's body.</summary>
        public static JobCar Spawn(Vector3 at, float speed, PropBatch body = null)
        {
            var s = GameSession.Instance; if (s == null || s.streetX == null || s.streetX.Length == 0 || s.streetZ.Length == 0) return null;
            var go = new GameObject("Job car");
            Mesh mesh = null; Material[] mats = null;
            if (body != null && body.lod0 != null) { mesh = body.lod0; mats = body.materials; }
            var inst = mesh == null ? Object.FindFirstObjectByType<PropInstancer>() : null;
            if (inst != null && inst.batches != null)
            {
                PropBatch pick = null;
                foreach (var b in inst.batches)
                {
                    if (b == null || b.name == null || b.lod0 == null || !b.name.StartsWith("car-")) continue;
                    if (b.name.StartsWith("car-sedan")) { pick = b; break; }
                    if (pick == null && (b.name.StartsWith("car-coupe") || b.name.StartsWith("car-hatchback") || b.name.StartsWith("car-luxury"))) pick = b;
                }
                if (pick != null) { mesh = pick.lod0; mats = pick.materials; }
            }
            var box = go.AddComponent<BoxCollider>();
            if (mesh != null)
            {
                var model = new GameObject("Model"); model.transform.SetParent(go.transform, false);
                model.AddComponent<MeshFilter>().sharedMesh = mesh; model.AddComponent<MeshRenderer>().sharedMaterials = mats;
                box.center = mesh.bounds.center; box.size = mesh.bounds.size;
            }
            else
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(cube.GetComponent<Collider>());
                cube.transform.SetParent(go.transform, false); cube.transform.localPosition = new Vector3(0, 0.75f, 0); cube.transform.localScale = new Vector3(1.8f, 1.4f, 4.4f);
                box.center = new Vector3(0, 0.75f, 0); box.size = new Vector3(1.8f, 1.4f, 4.4f);
            }
            var car = go.AddComponent<JobCar>(); car.speed = speed;
            go.AddComponent<StreetEmpire.Vehicles.CarLamps>();
            float nx = Nearest(s.streetX, at.x), nz = Nearest(s.streetZ, at.z);
            car.alongZ = Mathf.Abs(at.x - nx) <= Mathf.Abs(at.z - nz);
            car.line = car.alongZ ? nx : nz; car.along = car.alongZ ? at.z : at.x;
            car.dir = Random.value < 0.5f ? 1 : -1;
            car.Place(true);
            return car;
        }

        static float Nearest(float[] lines, float v) { float best = lines[0]; foreach (var l in lines) if (Mathf.Abs(l - v) < Mathf.Abs(best - v)) best = l; return best; }

        /// <summary>Rammed: it crawls for a moment.</summary>
        public void Bump() => slow = 1.6f;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            var s = GameSession.Instance; if (s == null) return;
            float dt = Time.deltaTime; if (slow > 0) slow -= dt;
            float want = stopped ? 0f : speed * (slow > 0 ? 0.3f : 1f);
            if (obeyLights)
            {
                // traffic waits at lights and stop signs and behind whatever is in its lane, and eases on and off
                if (!stopped && (Held(s, dt) || Blocked(s, dt))) want = 0f;
                v = Mathf.MoveTowards(v, want, (want < v ? 14f : 5f) * dt);
            }
            else v = want;
            if (v <= 0.001f) { Place(false); return; }
            var span = alongZ ? s.spanZ : s.spanX; float lo = Mathf.Min(span.x, span.y) + 3f, hi = Mathf.Max(span.x, span.y) - 3f;
            float next = along + dir * v * dt;
            if (next > hi || next < lo) { dir = -dir; next = Mathf.Clamp(next, lo, hi); }
            else
            {
                // at a crossing it may turn onto the other street
                var cross = alongZ ? s.streetZ : s.streetX;
                foreach (var c in cross)
                {
                    if ((along - c) * (next - c) > 0f || Mathf.Approximately(along, c)) continue;
                    if (Random.value < 0.4f) { float was = line; line = c; alongZ = !alongZ; next = was; dir = Random.value < 0.5f ? 1 : -1; }
                    break;
                }
            }
            along = next;
            Place(false);
        }

        /// <summary>Stopped short of the next crossing: a light that is not green for this street, or a stop sign's pause.</summary>
        bool Held(GameSession s, float dt)
        {
            var cross = alongZ ? s.streetZ : s.streetX; var own = alongZ ? s.streetX : s.streetZ;
            float c = 0f, d = float.MaxValue;
            foreach (var x in cross) { float dx = (x - along) * dir; if (dx > 0f && dx < d) { d = dx; c = x; } }
            if (d < StopNear || d > StopFar) return false;
            // lights hang over the crossings inside the town; the ones round its edge have stop signs (PropBuilder)
            bool Inner(float[] lines, float val) => Mathf.Abs(val - lines.Min()) > 1f && Mathf.Abs(val - lines.Max()) > 1f;
            if (lights == null) lights = Object.FindFirstObjectByType<TrafficSignalCycle>();
            if (lights != null && Inner(cross, c) && Inner(own, line)) return lights.Phase(Time.time) != (alongZ ? 0 : 3);
            if (!Mathf.Approximately(signAt, c)) { signAt = c; signT = 1.2f; }
            if (signT > 0f) { signT -= dt; return true; }
            return false;
        }

        /// <summary>Something in the lane just ahead: another marked or traffic car, or the player. It will not wait for ever.</summary>
        bool Blocked(GameSession s, float dt)
        {
            if (pushT > 0f) { pushT -= dt; return false; }
            Vector3 h = alongZ ? new Vector3(0, 0, dir) : new Vector3(dir, 0, 0), right = Vector3.Cross(Vector3.up, h), at = transform.position;
            bool Ahead(Vector3 q, float reach) { var rel = q - at; float f = Vector3.Dot(rel, h); return f > 0.5f && f < reach && Mathf.Abs(Vector3.Dot(rel, right)) < 2.1f; }
            bool hit = s.player != null && Ahead(s.player.transform.position, 8f);
            for (int i = 0; i < All.Count && !hit; i++) if (All[i] != this && Ahead(All[i].transform.position, 9f)) hit = true;
            if (!hit) { waitT = 0f; return false; }
            waitT += dt; if (waitT > 6f) { waitT = 0f; pushT = 2.5f; return false; }
            return true;
        }

        void Place(bool snap)
        {
            // heading, and a lane to the right of the center line
            Vector3 h = alongZ ? new Vector3(0, 0, dir) : new Vector3(dir, 0, 0);
            Vector3 right = Vector3.Cross(Vector3.up, h);
            // the body glides to its lane, so a turn at a crossing is a swing and not a jump
            var to = Position + right * Lane;
            transform.position = snap ? to : Vector3.MoveTowards(transform.position, to, Mathf.Max(v, 3f) * 1.8f * Time.deltaTime);
            var want = Quaternion.LookRotation(h);
            transform.rotation = snap ? want : Quaternion.RotateTowards(transform.rotation, want, 240f * Time.deltaTime);
        }
    }
}
