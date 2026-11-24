// A car that drives itself around the town's streets for a job: the one you tail ("Shadow") and the one you stop
// ("Repo"). It keeps to the street center lines the session knows (a lane to the right of the line), turns at some
// crossings and turns round at the edge of town. No physics: it is a marked car to follow or ram, and the player's
// car treats it as solid (CarController).
using StreetEmpire.City;
using UnityEngine;

namespace StreetEmpire.Game
{
    public class JobCar : MonoBehaviour
    {
        public float speed = 8f;
        public bool stopped;
        public Vector3 Position => alongZ ? new Vector3(line, 0, along) : new Vector3(along, 0, line);

        bool alongZ; int dir = 1; float line, along, slow;
        const float Lane = 2.2f;

        /// <summary>A marked car on the street nearest to a point, wearing a parked sedan's body.</summary>
        public static JobCar Spawn(Vector3 at, float speed)
        {
            var s = GameSession.Instance; if (s == null || s.streetX == null || s.streetX.Length == 0 || s.streetZ.Length == 0) return null;
            var go = new GameObject("Job car");
            Mesh mesh = null; Material[] mats = null;
            var inst = Object.FindFirstObjectByType<PropInstancer>();
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

        void Update()
        {
            var s = GameSession.Instance; if (s == null || stopped) return;
            float dt = Time.deltaTime; if (slow > 0) slow -= dt;
            float v = speed * (slow > 0 ? 0.3f : 1f);
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

        void Place(bool snap)
        {
            // heading, and a lane to the right of the center line
            Vector3 h = alongZ ? new Vector3(0, 0, dir) : new Vector3(dir, 0, 0);
            Vector3 right = Vector3.Cross(Vector3.up, h);
            transform.position = Position + right * Lane;
            var want = Quaternion.LookRotation(h);
            transform.rotation = snap ? want : Quaternion.RotateTowards(transform.rotation, want, 240f * Time.deltaTime);
        }
    }
}
