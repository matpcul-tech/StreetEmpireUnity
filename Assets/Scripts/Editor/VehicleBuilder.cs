// M3: the kit sedan (M5: painted like makeKitCar), parked where createState() parks the first car (east curb of block 0), with the
// sedan numbers from Assets/Data/vehicles.json.
using System.Collections.Generic;
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
        // Vehicles wave C (release meshy-vehicles-wave-c-2026-11-23, tools/car_wavec.mjs): sixteen more, from the hatchback
        // to the semi trailer. Parked only. The box truck is held back (its cab is still torn).
        public static readonly string[] V2Kinds =
        {
            "sedan-clean", "sedan", "police", "coupe", "lowrider", "pickup", "suv", "taxi", "van", "cruiser", "dirt-bike", "sport-bike",
            "hatchback", "minivan", "luxury-sedan", "limo", "tow-truck", "ambulance", "ice-cream-truck", "armored-truck", "swat-van",
            "school-bus", "city-bus", "fire-engine", "garbage-truck", "dump-truck", "semi-tractor", "semi-trailer",
        };
        static readonly float[] V2Lengths =
        {
            4.75f, 5.00f, 5.25f, 4.50f, 5.35f, 5.30f, 4.85f, 5.20f, 5.30f, 2.40f, 2.18f, 2.10f,
            4.05f, 4.85f, 5.25f, 9.40f, 8.80f, 6.70f, 6.70f, 6.80f, 6.90f,
            11.80f, 12.20f, 10.00f, 9.80f, 8.60f, 6.50f, 16.15f,
        };
        // wave C: the ones too long for a car's spot at the curb; they take a side of their own
        public static readonly string[] V2Big = { "city-bus", "tow-truck", "ambulance", "garbage-truck", "school-bus", "limo", "armored-truck", "dump-truck", "fire-engine", "swat-van", "semi-tractor", "semi-trailer" };
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
                    // wave C names its painted parts paint, trim, marking and lights; all four read the one body map
                    bool waveC = m.name.EndsWith("_paint") || m.name.EndsWith("_trim") || m.name.EndsWith("_marking") || m.name.EndsWith("_lights");
                    string part = m.name.EndsWith("_body") || waveC ? "body" : m.name.EndsWith("_wheel") ? "wheel" : m.name.EndsWith("_glass") ? "glass" : m.name.EndsWith("_decal") ? "decal" : null;
                    if (part == null) continue;
                    // v75u: every car's glass shares one map; the cruiser and the taxi carry their lettering on a cut-out panel
                    string file = part == "glass" ? "glass.png" : part == "decal" ? $"{kind}_decal.png" : $"{kind}_{part}.jpg";
                    var t = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Textures/Cars/V2/{file}");
                    if (t == null) { Debug.LogError($"[VehicleBuilder] no color map for {kind} {part}"); continue; }
                    m.SetTexture("_BaseMap", t); m.SetTexture("_MainTex", t);
                    m.SetTexture("_BumpMap", null); m.DisableKeyword("_NORMALMAP");
                    if (part == "glass")
                    {
                        // dark tinted glass: the map carries the tint and nothing glows, so a window is not a lit
                        // panel at night (the limo, build 7d05e48). A sharp sun glint only, no sky reflection: on the
                        // phone that showed as gray blotches on the lowrider and the SUV (build 14652fa).
                        // No sun glint either: the panes are not flat, and a glint lit them facet by facet (garbage truck cab).
                        m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.5f);
                        m.SetFloat("_SpecularHighlights", 0f); m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                        m.SetTexture("_EmissionMap", null); m.SetColor("_EmissionColor", Color.black);
                        m.DisableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                        m.SetFloat("_EnvironmentReflections", 0f); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                    }
                    // paint with a shine: the cars read as matte primer on the phone (build 7d05e48). Tires stay dull.
                    if (part == "body") { m.SetFloat("_Smoothness", 0.74f); m.SetFloat("_Metallic", 0f); m.SetFloat("_SpecularHighlights", 1f); m.DisableKeyword("_SPECULARHIGHLIGHTS_OFF"); m.SetFloat("_EnvironmentReflections", 1f); m.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); }
                    if (part == "wheel") m.SetFloat("_Smoothness", 0.3f);
                    if (part == "decal")
                    {
                        m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.4f);
                        m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON");
                        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    }
                    EditorUtility.SetDirty(m);
                }
        }

        // v75u paint colors (tools/car_paint.mjs): four more for each of six cars. The van, the taxi and the cruiser
        // keep their one paint. Paint 0 is the car's own color.
        static readonly Dictionary<string, string[]> V2Paints = new()
        {
            ["sedan-clean"] = new[] { "black", "silver", "maroon", "white" },
            ["sedan"] = new[] { "navy", "tan", "green", "red" },
            ["coupe"] = new[] { "red", "silver", "white", "green" },
            ["lowrider"] = new[] { "purple", "teal", "black", "gold" },
            ["pickup"] = new[] { "blue", "white", "green", "tan" },
            ["suv"] = new[] { "black", "navy", "maroon", "silver" },
        };
        /// <summary>The name of a car's paint: null for its own color (paint 0, or a car with one paint).</summary>
        public static string PaintName(string kind, int paint)
        {
            if (!V2Paints.TryGetValue(kind, out var p)) return null;
            int i = ((paint % (p.Length + 1)) + p.Length + 1) % (p.Length + 1);
            return i == 0 ? null : p[i - 1];
        }
        /// <summary>A copy of a car's body material wearing another paint (its own recolored body map).</summary>
        public static Material Painted(Material body, string kind, string color)
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Textures/Cars/V2/{kind}_body_{color}.jpg");
            if (t == null) { Debug.LogError($"[VehicleBuilder] no {color} paint for {kind}"); return body; }
            string own = AssetDatabase.GetAssetPath(body);
            if (string.IsNullOrEmpty(own)) return body;
            string path = own.Substring(0, own.LastIndexOf('/') + 1) + $"{body.name}-{color}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(body); AssetDatabase.CreateAsset(m, path); }
            else { m.shader = body.shader; m.CopyPropertiesFromMaterial(body); }
            m.SetTexture("_BaseMap", t); m.SetTexture("_MainTex", t); m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
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
