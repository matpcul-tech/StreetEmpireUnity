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

        // Cars v2 (release meshy-cars-v2-2026-11-22, docs/sovereign-assets/cars-v2): nine cars in real meters, body,
        // glass and wheel materials, four wheel nodes pivoting at the hub. Prepared by tools/car_v2.mjs.
        // Motorcycles v2 (release meshy-bikes-v2-2026-11-22): the last three. Parked only for now.
        public static readonly string[] V2Kinds = { "sedan-clean", "sedan", "police", "coupe", "lowrider", "pickup", "suv", "taxi", "van", "cruiser", "dirt-bike", "sport-bike" };
        static readonly float[] V2Lengths = { 4.75f, 5.00f, 5.25f, 4.50f, 5.35f, 5.30f, 4.85f, 5.20f, 5.30f, 2.40f, 2.18f, 2.10f };
        public static readonly string[] V2Bikes = { "cruiser", "dirt-bike", "sport-bike" };
        public static string V2Glb(string kind, bool far = false) => $"Assets/Art/Models/Cars/V2/{(far ? "LOD1/" : "")}{kind}.glb";
        /// <summary>Puts a v2 vehicle's color maps on its body and wheel materials. The maps are files under
        /// Art/Textures/Cars/V2, not images inside the GLB, so they import as ordinary compressed textures and the
        /// near and far car share one copy. No normal map (see tools/car_v2.mjs for why).</summary>
        public static void BindV2(GameObject model, string kind)
        {
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    string part = m.name.EndsWith("_body") ? "body" : m.name.EndsWith("_wheel") ? "wheel" : null;
                    if (part == null) continue;
                    var t = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Textures/Cars/V2/{kind}_{part}.jpg");
                    if (t == null) { Debug.LogError($"[VehicleBuilder] no color map for {kind} {part}"); continue; }
                    m.SetTexture("_BaseMap", t); m.SetTexture("_MainTex", t);
                    m.SetTexture("_BumpMap", null); m.DisableKeyword("_NORMALMAP");
                    EditorUtility.SetDirty(m);
                }
        }

        public static float V2Length(string kind) { int i = System.Array.IndexOf(V2Kinds, kind); return i < 0 ? 4.75f : V2Lengths[i]; }

        public static CarController BuildSedan(GameData data, Block b0)
        {
            var w = data.World;
            var (cx, cz) = Simulation.BlockCenter(w, b0);
            // state.js: makeCar(s, bc.x + PITCH / 2 - 5.6, bc.z - 10 + (i % 3) * 6, 0, types[0] = 'sedan')
            var (x, z) = WorldSpace.FromSource(cx + w.pitch / 2 - 5.6f, cz - 10);
            // kit.json sedan: length 4.5 m, yaw 0 (nose along +Z)
            // cars v2: the clean sedan, in its own paint
            var car = BuildCar("Sedan", V2Glb("sedan-clean"), "v2-sedan-clean", V2Length("sedan-clean"), data.Vehicle("sedan"));
            BindV2(car.gameObject, "sedan-clean");
            car.transform.SetPositionAndRotation(new Vector3(x, 0, z), Quaternion.identity);
            return car;
        }

        /// <summary>M5: the kit police cruiser (police-v7, made from the Meshy police-01 model), kit.json length 5.2 m,
        /// as an inactive template the police dispatch copies. Police cars keep their own paint (makeKitCar).</summary>
        public static CarController BuildPoliceTemplate(GameData data)
        {
            var car = BuildCar("Cruiser Template", V2Glb("police"), "v2-police", V2Length("police"), data.Vehicle("police"));
            BindV2(car.gameObject, "police");
            car.transform.position = new Vector3(0, -60, 0);
            car.gameObject.AddComponent<StreetEmpire.Game.PoliceCruiser>();
            car.gameObject.SetActive(false);
            return car;
        }

        /// <summary>A parked, static copy of a v2 car for the curbs (no controller, no rigidbody), or its far
        /// version. Root at the ground center, nose along +Z, box collider.</summary>
        public static GameObject ParkedCar(string kind, bool far = false)
        {
            if (System.Array.IndexOf(V2Kinds, kind) < 0) { Debug.LogError($"[VehicleBuilder] no car '{kind}', using the sedan"); kind = "sedan"; }
            var root = new GameObject($"Parked {kind}");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(V2Glb(kind, far)));
            model.name = "Model"; model.transform.SetParent(root.transform, false);
            UrpMaterials.Apply(model, "v2-" + kind);
            BindV2(model, kind);
            var bounds = Bounds(model);
            model.transform.localScale *= V2Length(kind) / Mathf.Max(bounds.size.z, 1e-3f);
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
                // v75e: the v75c police rebake dropped the glTF emissive slot, so the light bar map is set and switched on here
                if (em != null) { m.SetTexture("_EmissionMap", em); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
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
