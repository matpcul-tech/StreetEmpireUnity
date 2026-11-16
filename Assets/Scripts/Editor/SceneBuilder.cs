// Builds Assets/Scenes/Eastside.unity from the city grid data and the kit GLBs (imported by glTFast).
// The scene is code: CI rebuilds it before every build and play mode test, so the data stays the source of truth.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StreetEmpire.City;
using StreetEmpire.Sim;
using StreetEmpire.Sim.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace StreetEmpire.EditorTools
{
    public static class SceneBuilder
    {
        public const string EastsideScene = "Assets/Scenes/Eastside.unity";
        const string Mats = "Assets/Art/Materials";
        const string Models = "Assets/Art/Models/Buildings";
        const string BuildingTextures = "Assets/Art/Textures/Buildings";

        // kit.json "buildings" for the Eastside kinds, plus (M5) the 14 new Eastside buildings v2. Eastside has
        // 9 stores, 4 apartments and 23 houses: one store and one apartment model each, and the 7 houses dealt
        // in turn so no block repeats a model. Every model faces +Z, which the lot turns to its street; the backs
        // face the middle of the block (store-barber's back wall is the rough one).
        static readonly Dictionary<string, string[]> Kit = new()
        {
            ["store"] = new[] { "store-liquor-blank", "store-bodega", "store-laundromat", "store-barber", "store-pawn", "store-takeout", "shop-auto-garage", "shop-gas-station", "civic-church" },
            ["house"] = new[] { "house-01-bungalow", "house-rowhouse-pair", "house-02-two-story", "house-wood-duplex", "house-03-duplex", "house-shotgun", "house-boarded" },
            ["apt"] = new[] { "apt-01-stucco", "apt-mixed-use", "apt-02-brick", "apt-motel-style" },
        };
        static readonly Dictionary<string, int> dealt = new();

        public static readonly int[] EastsideBlocks = { 0, 1, 2, 9, 10, 11, 18, 19, 20 };

        [MenuItem("Street Empire/Build Eastside Scene")]
        public static void BuildEastside()
        {
            var data = LoadData();
            var s = Simulation.CreateState(data, 1);
            var w = data.World;

            // build in a scene of its own so a caller's open scene (or the test runner's) is left alone
            var prev = SceneManager.GetActiveScene();
            bool additive = prev.IsValid() && !string.IsNullOrEmpty(prev.path);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, additive ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            var asphalt = Mat("Asphalt", new Color32(0x2e, 0x2e, 0x31, 255), 0.15f);
            var sidewalk = Mat("Sidewalk", new Color32(0x8e, 0x8a, 0x84, 255), 0.1f);
            var yard = Mat("Yard", new Color32(0x4f, 0x5c, 0x3a, 255), 0.05f);
            var line = Mat("LaneYellow", new Color32(0xd8, 0xc0, 0x4a, 255), 0.3f);
            var curb = Mat("Curb", new Color32(0x6e, 0x6b, 0x66, 255), 0.1f);

            var root = new GameObject("Eastside");
            // asphalt under the whole district plus a margin: the streets are what the blocks leave open
            float min = CityLayout.LineCoord(w, 0) - 30, max = CityLayout.LineCoord(w, 3) + 30;
            var (gx0, gz0) = WorldSpace.FromSource(min, min); var (gx1, gz1) = WorldSpace.FromSource(max, max);
            var ground = Box("Ground", root.transform, asphalt, new Vector3((gx0 + gx1) / 2, -0.05f, (gz0 + gz1) / 2), new Vector3(Mathf.Abs(gx1 - gx0), 0.1f, Mathf.Abs(gz1 - gz0)));
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.BatchingStatic);

            var streets = new GameObject("Streets").transform; streets.SetParent(root.transform);
            // double yellow center lines, broken at every crossing so they do not run through the intersections
            float half = w.street / 2f;
            var spans = new System.Collections.Generic.List<(float a, float b)>();
            float from = min;
            for (int j = 0; j <= 3; j++) { float cj = CityLayout.LineCoord(w, j); spans.Add((from, cj - half)); from = cj + half; }
            spans.Add((from, max));
            for (int i = 0; i <= 3; i++)
            {
                float c = CityLayout.LineCoord(w, i);
                foreach (float off in new[] { -0.18f, 0.18f })
                    for (int k = 0; k < spans.Count; k++)
                    {
                        var (a, b) = spans[k]; float mid = (a + b) / 2, len = b - a;
                        if (len < 0.5f) continue;
                        var (ax, az) = WorldSpace.FromSource(c + off, mid);
                        Box($"Line X{i}.{k}", streets, line, new Vector3(ax, 0.01f, az), new Vector3(0.12f, 0.02f, len), false);
                        var (bx, bz) = WorldSpace.FromSource(mid, c + off);
                        Box($"Line Z{i}.{k}", streets, line, new Vector3(bx, 0.01f, bz), new Vector3(len, 0.02f, 0.12f), false);
                    }
            }

            int placed = 0; dealt.Clear();
            foreach (int id in EastsideBlocks)
            {
                var b = s.blocks[id];
                var (scx, scz) = Simulation.BlockCenter(w, b);
                var (ux, uz) = WorldSpace.FromSource(scx, scz);
                var bgo = new GameObject($"Block {b.id} {b.hustle}");
                bgo.transform.SetParent(root.transform);
                bgo.transform.position = new Vector3(ux, 0, uz);
                var bm = bgo.AddComponent<BlockMarker>();
                bm.blockId = b.id; bm.gx = b.gx; bm.gz = b.gz; bm.owner = b.owner; bm.hustle = b.hustle;
                bm.district = CityLayout.DistrictOf(b.gx, b.gz, w.grid);
                // sidewalk slab with a curb edge, then the yard inside (city.js buildBlock)
                Box("Sidewalk", bgo.transform, sidewalk, new Vector3(ux, 0.11f, uz), new Vector3(w.block + 4, 0.22f, w.block + 4));
                Box("Curb", bgo.transform, curb, new Vector3(ux, 0.1f, uz), new Vector3(w.block + 4.3f, 0.2f, w.block + 4.3f), false);
                Box("Yard", bgo.transform, yard, new Vector3(ux, 0.12f, uz), new Vector3(w.block - 2, 0.24f, w.block - 2), false);
                foreach (var spot in CityLayout.BuildingsFor(w, b)) if (PlaceBuilding(spot, bgo.transform)) placed++;
            }

            Lighting(root.transform);
            var player = PlayerBuilder.Build(w, s.blocks[0]);
            VehicleBuilder.BuildSedan(data, s.blocks[0]);
            GameBuilder.Build(data, s, player);

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, EastsideScene);
            if (additive) { SceneManager.SetActiveScene(prev); EditorSceneManager.CloseScene(scene, true); }
            Debug.Log($"[SceneBuilder] Eastside: {EastsideBlocks.Length} blocks, {placed} kit buildings");
        }

        static bool PlaceBuilding(BuildingSpot spot, Transform parent)
        {
            var variants = Kit[spot.kind];
            dealt.TryGetValue(spot.kind, out int n); dealt[spot.kind] = n + 1;
            string name = variants[n % variants.Length];
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}/{name}.glb");
            var holder = new GameObject($"{spot.kind} {spot.index} ({name})");
            var marker = holder.AddComponent<BuildingMarker>();
            marker.blockId = spot.blockId; marker.index = spot.index; marker.kind = spot.kind; marker.model = name;
            if (model == null) { Debug.LogError($"[SceneBuilder] missing kit model {name}"); Object.DestroyImmediate(holder); return false; }

            // kit.js prepBuilding(): fit the widest side to the 13 m lot, feet on the ground, centered
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            inst.transform.SetParent(holder.transform, false);
            UrpMaterials.Apply(inst, name);
            var bounds = WorldBounds(inst);
            float k = spot.w / Mathf.Max(bounds.size.x, bounds.size.z, 1e-4f);
            // multiply, not set: glTFast folds a lone root node (and its scale) into the prefab root
            inst.transform.localScale *= k;
            bounds = WorldBounds(inst);
            // shift, not set: the imported root may already carry an offset of its own
            inst.transform.localPosition += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            bounds = WorldBounds(inst);
            var col = holder.AddComponent<BoxCollider>(); col.center = bounds.center; col.size = bounds.size;
            BindTextures(inst, name);
            var near = inst.GetComponentsInChildren<Renderer>();
            foreach (var r in near) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
            AddFar(holder, inst, name, bounds, near);
            // not batching static: with 20 models of about 58k triangles, static batching would copy every placed
            // mesh (36 of them) into combined buffers, roughly doubling mesh memory on phones and WebGL. The SRP
            // batcher keeps the draw calls cheap without it.

            var (x, z) = WorldSpace.FromSource(spot.x, spot.z);
            holder.transform.SetParent(parent, true);
            holder.transform.SetPositionAndRotation(new Vector3(x, 0.24f, z), Quaternion.Euler(0, WorldSpace.YawFromSource(spot.yaw) * Mathf.Rad2Deg, 0));
            return true;
        }

        /// <summary>Point the building's URP material at the extracted, GPU compressed textures
        /// (tools/extract_building_textures.mjs) instead of glTFast's uncompressed ones.</summary>
        static void BindTextures(GameObject inst, string name)
        {
            var bc = AssetDatabase.LoadAssetAtPath<Texture2D>($"{BuildingTextures}/{name}_basecolor.jpg");
            var nm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{BuildingTextures}/{name}_normal.jpg");
            var mats = inst.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToArray();
            // buildings are never metal. UrpMaterials takes glTF metallicFactor alone, but some kit files say 1 and
            // leave the real (low) value to a metallic-roughness texture URP Lit does not read, which made them
            // render as shiny metal (the original house-01 did).
            foreach (var m in mats) { m.SetFloat("_Metallic", 0f); m.SetFloat("_Smoothness", 0.12f); m.SetTexture("_MetallicGlossMap", null); m.DisableKeyword("_METALLICSPECGLOSSMAP"); EditorUtility.SetDirty(m); }
            if (bc == null) { Debug.LogWarning($"[SceneBuilder] no extracted texture for {name}"); return; }
            foreach (var m in mats)
            {
                m.SetTexture("_BaseMap", bc);
                m.SetTexture("_BumpMap", nm); if (nm != null) m.EnableKeyword("_NORMALMAP"); else m.DisableKeyword("_NORMALMAP");
                EditorUtility.SetDirty(m);
            }
        }

        /// <summary>LOD1: the Blender decimated far version (about 5.8k triangles, tools/decimate_far.py) with the
        /// near model's material, fitted to the near bounds. Phones draw the 58k triangle model only up close.</summary>
        static void AddFar(GameObject holder, GameObject inst, string name, Bounds nearBounds, Renderer[] near)
        {
            var farAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}/Far/{name}.glb");
            if (farAsset == null) return;
            var far = (GameObject)PrefabUtility.InstantiatePrefab(farAsset);
            far.name = "Far"; far.transform.SetParent(holder.transform, false);
            var mat = near[0].sharedMaterial;
            var farRs = far.GetComponentsInChildren<Renderer>();
            foreach (var r in farRs) { r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray(); r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
            var b = WorldBounds(far);
            far.transform.localScale *= Mathf.Max(nearBounds.size.x, nearBounds.size.z) / Mathf.Max(b.size.x, b.size.z, 1e-4f);
            b = WorldBounds(far);
            far.transform.localPosition += new Vector3(nearBounds.center.x - b.center.x, nearBounds.min.y - b.min.y, nearBounds.center.z - b.center.z);
            var lod = holder.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(0.2f, near), new LOD(0.01f, farRs) });
            lod.RecalculateBounds();
        }

        static void Lighting(Transform root)
        {
            // one shadowed sun, trilight ambient, light fog: cheap on phones and WebGL
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1f, 0.95f, 0.86f); sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.8f;
            sun.transform.rotation = Quaternion.Euler(48, -35, 0);
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.56f, 0.63f, 0.74f);
            RenderSettings.ambientEquatorColor = new Color(0.44f, 0.43f, 0.41f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.17f, 0.16f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 140; RenderSettings.fogEndDistance = 420;
            RenderSettings.fogColor = new Color(0.66f, 0.71f, 0.78f);
            var sky = AssetDatabase.LoadAssetAtPath<Material>($"{Mats}/Sky.mat");
            if (sky == null) { sky = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(sky, $"{Mats}/Sky.mat"); }
            sky.SetFloat("_SunSize", 0.03f); sky.SetFloat("_AtmosphereThickness", 0.9f); sky.SetFloat("_Exposure", 1.15f);
            RenderSettings.skybox = sky;
        }

        static void OverviewCamera(WorldFile w)
        {
            // M1 view over the district from the south. M2 replaces it with the follow camera.
            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.gameObject.AddComponent<AudioListener>();
            var (cx, cz) = WorldSpace.FromSource((CityLayout.LineCoord(w, 0) + CityLayout.LineCoord(w, 3)) / 2, (CityLayout.LineCoord(w, 0) + CityLayout.LineCoord(w, 3)) / 2);
            cam.transform.position = new Vector3(cx + 40, 60, cz - 150);
            cam.transform.LookAt(new Vector3(cx, 0, cz - 10));
            cam.fieldOfView = 55; cam.nearClipPlane = 0.3f; cam.farClipPlane = 600;
            cam.clearFlags = CameraClearFlags.Skybox;
        }

        static GameObject Box(string name, Transform parent, Material m, Vector3 pos, Vector3 size, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, true);
            go.transform.position = pos; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        static Material Mat(string name, Color c, float smooth)
        {
            Directory.CreateDirectory(Mats);
            string p = $"{Mats}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
            return b;
        }

        public static GameData LoadData()
        {
            string R(string n) => File.ReadAllText($"Assets/Data/{n}.json");
            return GameDataLoader.FromJson(R("crews"), R("weapons"), R("vehicles"), R("jobs"), R("police"), R("world"));
        }
    }
}
