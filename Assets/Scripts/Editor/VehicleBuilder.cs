// M3: the kit sedan (M5: painted like makeKitCar), parked where createState() parks the first car (east curb of block 0), with the
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

        const string PoliceGlb = "Assets/Art/Models/Cars/police.glb";
        // the first PAINT color in vehicles.js (deep red), baked into the atlas by tools/paint_car.mjs
        const string SedanPaint = "Assets/Art/Models/Cars/sedan_paint_6a2420.jpg";

        public static CarController BuildSedan(GameData data, Block b0)
        {
            var w = data.World;
            var (cx, cz) = Simulation.BlockCenter(w, b0);
            // state.js: makeCar(s, bc.x + PITCH / 2 - 5.6, bc.z - 10 + (i % 3) * 6, 0, types[0] = 'sedan')
            var (x, z) = WorldSpace.FromSource(cx + w.pitch / 2 - 5.6f, cz - 10);
            // kit.json sedan: length 4.5 m, yaw 0 (nose along +Z)
            var car = BuildCar("Sedan", SedanGlb, "sedan", 4.5f, data.Vehicle("sedan"));
            Paint(car.gameObject, "sedan", SedanPaint);
            car.transform.SetPositionAndRotation(new Vector3(x, 0, z), Quaternion.identity);
            return car;
        }

        /// <summary>M5: the kit police cruiser (police-v7, made from the Meshy police-01 model), kit.json length 5.2 m,
        /// as an inactive template the police dispatch copies. Police cars keep their own paint (makeKitCar).</summary>
        public static CarController BuildPoliceTemplate(GameData data)
        {
            var car = BuildCar("Cruiser Template", PoliceGlb, "police", 5.2f, data.Vehicle("police"));
            car.transform.position = new Vector3(0, -60, 0);
            car.gameObject.AddComponent<StreetEmpire.Game.PoliceCruiser>();
            car.gameObject.SetActive(false);
            return car;
        }

        /// <summary>A parked, static copy of a kit car for the curbs (no controller, no rigidbody): the stock
        /// or painted sedan, or the police cruiser. Root at the ground center, nose along +Z, box collider.</summary>
        public static GameObject ParkedCar(string kind, bool painted)
        {
            bool police = kind == "police";
            var root = new GameObject(police ? "Parked cruiser" : painted ? "Parked sedan (red)" : "Parked sedan");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(police ? PoliceGlb : SedanGlb));
            model.name = "Model"; model.transform.SetParent(root.transform, false);
            string name = police ? "police" : "sedan";
            UrpMaterials.Apply(model, name);
            BindTextures(model, name);
            if (painted && !police) Paint(model, "sedan", SedanPaint);
            var bounds = Bounds(model);
            model.transform.localScale *= (police ? 5.2f : 4.5f) / Mathf.Max(bounds.size.z, 1e-3f);
            bounds = Bounds(model);
            model.transform.localPosition += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            bounds = Bounds(model);
            var box = root.AddComponent<BoxCollider>(); box.center = bounds.center; box.size = bounds.size;
            foreach (var r in model.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; }
            return root;
        }

        static CarController BuildCar(string name, string glb, string modelName, float length, VehicleData stats)
        {
            // fit the model with the root at the origin, then place the root (bounds are world space)
            var root = new GameObject(name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(glb));
            model.name = "Model"; model.transform.SetParent(root.transform, false);
            UrpMaterials.Apply(model, modelName);
            BindTextures(model, modelName);
            var bounds = Bounds(model);
            model.transform.localScale *= (length / Mathf.Max(bounds.size.z, 1e-3f));
            bounds = Bounds(model);
            model.transform.localPosition += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            bounds = Bounds(model);

            var box = root.AddComponent<BoxCollider>();
            box.center = bounds.center; box.size = bounds.size;
            var rb = root.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.None;
            var car = root.AddComponent<CarController>();
            car.stats = stats;
            var t = new GameObject("CameraTarget").transform; t.SetParent(root.transform, false); t.localPosition = new Vector3(0, 1.4f, 0);
            car.cameraTarget = t;
            foreach (var r in model.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return car;
        }

        /// <summary>Use the extracted, GPU compressed textures (tools/extract_car_textures.mjs) when there are any,
        /// including the metallic-smoothness map: without it the police car's metallic factor of 1 made the
        /// whole car chrome.</summary>
        static void BindTextures(GameObject model, string modelName)
        {
            const string dir = "Assets/Art/Textures/Cars";
            var bc = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{modelName}_basecolor.jpg");
            if (bc == null) return;
            var em = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{modelName}_emissive.jpg");
            var ms = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{modelName}_metalsmooth.png");
            foreach (var m in model.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct())
            {
                if (m.GetTexture("_BaseMap") == null) continue;
                m.SetTexture("_BaseMap", bc);
                if (m.IsKeywordEnabled("_EMISSION")) m.SetTexture("_EmissionMap", em);
                if (ms != null)
                {
                    m.SetTexture("_MetallicGlossMap", ms); m.EnableKeyword("_METALLICSPECGLOSSMAP");
                    m.SetFloat("_Metallic", 1f); m.SetFloat("_Smoothness", 1f); m.SetFloat("_SmoothnessTextureChannel", 0f);
                }
                EditorUtility.SetDirty(m);
            }
        }

        /// <summary>Swap each textured material for a copy that uses the painted atlas.</summary>
        static void Paint(GameObject car, string modelName, string texPath)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null) { Debug.LogWarning($"[VehicleBuilder] no paint texture {texPath}"); return; }
            foreach (var r in car.GetComponentsInChildren<Renderer>())
                r.sharedMaterials = r.sharedMaterials.Select(m =>
                {
                    if (m == null || m.GetTexture("_BaseMap") == null) return m;
                    string p = $"Assets/Art/Materials/Kit/{modelName}_{m.name}_painted.mat";
                    var pm = AssetDatabase.LoadAssetAtPath<Material>(p);
                    if (pm == null) { pm = new Material(m); AssetDatabase.CreateAsset(pm, p); } else pm.CopyPropertiesFromMaterial(m);
                    pm.SetTexture("_BaseMap", tex);
                    pm.SetFloat("_Smoothness", Mathf.Max(pm.GetFloat("_Smoothness"), 0.55f));
                    EditorUtility.SetDirty(pm);
                    return pm;
                }).ToArray();
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
