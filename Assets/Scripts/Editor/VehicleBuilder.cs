// M3: the kit sedan, parked where createState() parks the first car (east curb of block 0), with the
// sedan numbers from Assets/Data/vehicles.json.
using System.Linq;
using StreetEmpire.Sim;
using StreetEmpire.Sim.Data;
using StreetEmpire.Vehicles;
using UnityEditor;
using UnityEngine;

namespace StreetEmpire.EditorTools
{
    public static class VehicleBuilder
    {
        const string SedanGlb = "Assets/Art/Models/Cars/sedan.glb";

        public static CarController BuildSedan(GameData data, Block b0)
        {
            var w = data.World;
            var (cx, cz) = Simulation.BlockCenter(w, b0);
            // state.js: makeCar(s, bc.x + PITCH / 2 - 5.6, bc.z - 10 + (i % 3) * 6, 0, types[0] = 'sedan')
            var (x, z) = WorldSpace.FromSource(cx + w.pitch / 2 - 5.6f, cz - 10);
            var root = new GameObject("Sedan");
            root.transform.SetPositionAndRotation(new Vector3(x, 0, z), Quaternion.identity);

            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SedanGlb));
            model.name = "Model"; model.transform.SetParent(root.transform, false);
            UrpMaterials.Apply(model, "sedan");
            // kit.json sedan: length 4.5 m, yaw 0 (nose along +Z)
            var bounds = Bounds(model);
            model.transform.localScale *= (4.5f / Mathf.Max(bounds.size.z, 1e-3f));
            bounds = Bounds(model);
            model.transform.localPosition += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            bounds = Bounds(model);

            var box = root.AddComponent<BoxCollider>();
            box.center = bounds.center - root.transform.position; box.size = bounds.size;
            var rb = root.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.None;
            var car = root.AddComponent<CarController>();
            car.stats = data.Vehicle("sedan");
            var t = new GameObject("CameraTarget").transform; t.SetParent(root.transform, false); t.localPosition = new Vector3(0, 1.4f, 0);
            car.cameraTarget = t;
            foreach (var r in model.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return car;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
