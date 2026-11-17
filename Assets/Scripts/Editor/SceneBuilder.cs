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
using UnityEngine.Rendering.Universal;
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
            // CC0 Poly Haven ground textures (Assets/Art/Textures/Ground), tiled to the size of the box they cover
            float district = CityLayout.LineCoord(w, 3) - CityLayout.LineCoord(w, 0) + 60;
            var asphalt = TexMat("Asphalt", "worn_asphalt", district / 4.5f, 0.12f, new Color(0.78f, 0.78f, 0.8f));
            var sidewalk = TexMat("Sidewalk", "concrete_pavement", (w.block + 4) / 2.6f, 0.08f, Color.white);
            var yard = TexMat("Yard", "sparse_grass", (w.block - 2) / 3.5f, 0.04f, new Color(0.92f, 0.95f, 0.88f));
            var outer = TexMat("OuterGround", "leafy_grass", 1600f / 6f, 0.03f, new Color(0.8f, 0.85f, 0.78f));
            var line = Mat("LaneYellow", new Color32(0xd8, 0xc0, 0x4a, 255), 0.3f);
            var curb = Mat("Curb", new Color32(0x6e, 0x6b, 0x66, 255), 0.1f);

            var root = new GameObject("Eastside");
            // asphalt under the whole district plus a margin: the streets are what the blocks leave open
            float min = CityLayout.LineCoord(w, 0) - 30, max = CityLayout.LineCoord(w, 3) + 30;
            var (gx0, gz0) = WorldSpace.FromSource(min, min); var (gx1, gz1) = WorldSpace.FromSource(max, max);
            var ground = Box("Ground", root.transform, asphalt, new Vector3((gx0 + gx1) / 2, -0.05f, (gz0 + gz1) / 2), new Vector3(Mathf.Abs(gx1 - gx0), 0.1f, Mathf.Abs(gz1 - gz0)));
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.BatchingStatic);
            // grass out to the fog so the district does not end over nothing
            Box("Outer Ground", root.transform, outer, new Vector3((gx0 + gx1) / 2, -0.1f, (gz0 + gz1) / 2), new Vector3(1600, 0.1f, 1600));

            var streets = new GameObject("Streets").transform; streets.SetParent(root.transform);
            // double yellow center lines, broken at every crossing so they do not run through the intersections
            float half = w.street / 2f;
            var spans = new System.Collections.Generic.List<(float a, float b)>();
            float from = min;
            // the center lines stop short of the crosswalks and stop bars (PropBuilder), 13.5 m from each crossing
            float gap = half + 4.5f;
            for (int j = 0; j <= 3; j++) { float cj = CityLayout.LineCoord(w, j); spans.Add((from, cj - gap)); from = cj + gap; }
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

            int placed = 0; dealt.Clear(); compressed.Clear();
            var blockInfo = new List<PropBuilder.BlockInfo>();
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
                blockInfo.Add(new PropBuilder.BlockInfo { id = b.id, center = new Vector3(ux, 0, uz) });
                // sidewalk slab with a curb edge, then the yard inside (city.js buildBlock)
                Box("Sidewalk", bgo.transform, sidewalk, new Vector3(ux, 0.11f, uz), new Vector3(w.block + 4, 0.22f, w.block + 4));
                Box("Curb", bgo.transform, curb, new Vector3(ux, 0.1f, uz), new Vector3(w.block + 4.3f, 0.2f, w.block + 4.3f), false);
                Box("Yard", bgo.transform, yard, new Vector3(ux, 0.12f, uz), new Vector3(w.block - 2, 0.24f, w.block - 2), false);
                foreach (var spot in CityLayout.BuildingsFor(w, b)) if (PlaceBuilding(spot, bgo.transform)) placed++;
            }

            Combine(root.transform);
            Lighting(root.transform);
            var player = PlayerBuilder.Build(w, s.blocks[0]);
            VehicleBuilder.BuildSedan(data, s.blocks[0]);
            GameBuilder.Build(data, s, player);
            // after the people and cars, so the props keep clear of where they start
            var xs = Enumerable.Range(0, 4).Select(i => WorldSpace.FromSource(CityLayout.LineCoord(w, i), 0).x).ToArray();
            var zs = Enumerable.Range(0, 4).Select(i => WorldSpace.FromSource(0, CityLayout.LineCoord(w, i)).z).ToArray();
            PropBuilder.Build(blockInfo, xs, zs, root.transform);
            PostAndQuality(root.transform);

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
            // the near model is the Sovereign Asset se-building cut of the READY source (about 26k triangles,
            // docs/sovereign-assets/buildings-lods): the 58k triangle sources were most of the APK and the phone's mesh memory
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}/Near/{name}.glb") ?? AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}/{name}.glb");
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
            Compress(inst, name, "Near");
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

        /// <summary>The lower detail levels, both with the near model's material and fitted to the near bounds:
        /// LOD1 Far (Blender decimate to 22%, about 12.8k triangles, then a Sovereign Asset check) and LOD2 Low
        /// (6%, about 3.5k). Sovereign Asset itself will not cut these Meshy meshes below about 25k ("going lower
        /// would visibly change the shape"), so the near model is its 26k cut and the two far levels are Blender's.</summary>
        static void AddFar(GameObject holder, GameObject inst, string name, Bounds nearBounds, Renderer[] near)
        {
            var far = Level(holder, "Far", name, nearBounds, near[0].sharedMaterial);
            if (far == null) return;
            var low = Level(holder, "Low", name, nearBounds, near[0].sharedMaterial);
            // shadows come from one always-on copy of the low mesh (about 3.5k triangles) instead of whichever level
            // is drawn: the near models are about 26k and the three Ultra cascades each draw the casters again
            var shadow = Level(holder, low != null ? "Low" : "Far", name, nearBounds, near[0].sharedMaterial, "Shadow");
            foreach (var r in shadow) r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            foreach (var r in near.Concat(far).Concat(low ?? new Renderer[0])) r.shadowCastingMode = ShadowCastingMode.Off;
            // the LODGroup itself is added at load time (RuntimeLod): a saved one made the phone reject the scene file.
            // A 13 m building leaves the near model at about 40 m and the far one at about 190 m.
            var lod = holder.AddComponent<StreetEmpire.City.RuntimeLod>();
            lod.near = near; lod.far = far; lod.low = low ?? new Renderer[0];
            lod.nearHeight = 0.28f; lod.farHeight = low != null ? 0.06f : 0.005f; lod.lowHeight = 0.003f;
        }

        static Renderer[] Level(GameObject holder, string level, string name, Bounds nearBounds, Material mat, string goName = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}/{level}/{name}.glb");
            if (asset == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.name = goName ?? level; go.transform.SetParent(holder.transform, false);
            var rs = go.GetComponentsInChildren<Renderer>();
            foreach (var r in rs) { r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray(); r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
            Compress(go, name, level);
            var b = WorldBounds(go);
            go.transform.localScale *= Mathf.Max(nearBounds.size.x, nearBounds.size.z) / Mathf.Max(b.size.x, b.size.z, 1e-4f);
            b = WorldBounds(go);
            go.transform.localPosition += new Vector3(nearBounds.center.x - b.center.x, nearBounds.min.y - b.min.y, nearBounds.center.z - b.center.z);
            return rs;
        }

        /// <summary>Points the building's mesh filters at compressed copies of the imported meshes (glTFast meshes
        /// go into the build uncompressed and were the bulk of the APK's mesh data). Made once per model and level.</summary>
        static void Compress(GameObject go, string name, string level)
        {
            int i = 0;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                string path = $"Assets/Art/Meshes/Buildings/{name}-{level.ToLowerInvariant()}-{i++}.asset";
                if (!compressed.TryGetValue(path, out var m)) compressed[path] = m = MeshMerge.Save(mf.sharedMesh, path, copy: true);
                mf.sharedMesh = m;
            }
        }
        static readonly Dictionary<string, Mesh> compressed = new();

        /// <summary>The global post volume, post processing on the main camera, and the Ultra / High switch
        /// with its HUD button (top right, under the wanted stars).</summary>
        static void PostAndQuality(Transform root)
        {
            var vol = new GameObject("Post Volume"); vol.transform.SetParent(root, false);
            var v = vol.AddComponent<Volume>(); v.isGlobal = true; v.priority = 0; v.sharedProfile = GraphicsSetup.PostProfile();
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!cam.CompareTag("MainCamera")) continue;
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.None; // MSAA does the edges
                data.renderShadows = true;
                cam.allowHDR = true; cam.allowMSAA = true;
            }
            var gq = new GameObject("Graphics Quality").AddComponent<StreetEmpire.UI.GraphicsQuality>();
            gq.transform.SetParent(root, false);
            gq.ultra = GraphicsSetup.Ultra; gq.high = GraphicsSetup.High;
            var canvas = GameObject.Find("HUD");
            if (canvas == null) return;
            var r = PlayerBuilder.Rect("Graphics Button", canvas.transform, Vector2.one, Vector2.one, new Vector2(-30, -92), new Vector2(190, 52));
            r.pivot = Vector2.one;
            var img = r.gameObject.AddComponent<UnityEngine.UI.Image>(); img.color = new Color(0, 0, 0, 0.45f);
            var btn = r.gameObject.AddComponent<UnityEngine.UI.Button>(); btn.targetGraphic = img;
            var lr = PlayerBuilder.Rect("Label", r, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var t = lr.gameObject.AddComponent<UnityEngine.UI.Text>(); t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 24; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false; t.text = "GFX ULTRA";
            gq.label = t;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, gq.Toggle);
        }

        static Material TexMat(string name, string tex, float tile, float smooth, Color tint)
        {
            var m = Mat(name, tint, smooth);
            const string dir = "Assets/Art/Textures/Ground";
            var bc = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{tex}_basecolor.jpg");
            var nm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{tex}_normal.jpg");
            m.SetTexture("_BaseMap", bc); m.SetTextureScale("_BaseMap", new Vector2(tile, tile));
            m.SetTexture("_BumpMap", nm); m.SetFloat("_BumpScale", 1f);
            if (nm != null) m.EnableKeyword("_NORMALMAP"); else m.DisableKeyword("_NORMALMAP");
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>A small sky and ground cube for reflections. Nothing is baked in CI, so without this the
        /// metal props (hydrants, bins, lamps) would reflect black.</summary>
        static Cubemap Reflection()
        {
            const string path = "Assets/Art/Materials/SkyReflection.cubemap";
            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if (cube == null) { cube = new Cubemap(64, TextureFormat.RGBA32, true); AssetDatabase.CreateAsset(cube, path); }
            Color sky = new(0.58f, 0.68f, 0.84f), horizon = new(0.78f, 0.8f, 0.82f), ground = new(0.24f, 0.23f, 0.22f);
            var px = new Color[64 * 64];
            foreach (CubemapFace f in new[] { CubemapFace.PositiveX, CubemapFace.NegativeX, CubemapFace.PositiveY, CubemapFace.NegativeY, CubemapFace.PositiveZ, CubemapFace.NegativeZ })
            {
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        // face rows run top to bottom; the side faces blend sky, horizon and ground by height
                        float v = 1f - (y + 0.5f) / 64f;
                        Color c = f == CubemapFace.PositiveY ? sky : f == CubemapFace.NegativeY ? ground
                            : v > 0.5f ? Color.Lerp(horizon, sky, (v - 0.5f) * 2f) : Color.Lerp(ground, horizon, Mathf.Pow(v * 2f, 3f));
                        px[y * 64 + x] = c;
                    }
                cube.SetPixels(px, f);
            }
            cube.Apply(true);
            EditorUtility.SetDirty(cube);
            return cube;
        }

        static void Lighting(Transform root)
        {
            // one shadowed sun, trilight ambient, light fog: cheap on phones and WebGL
            var sun = new GameObject("Sun").AddComponent<Light>();
            // brighter than before: ACES tonemapping (GraphicsSetup) pulls the midtones down
            sun.type = LightType.Directional; sun.color = new Color(1f, 0.94f, 0.84f); sun.intensity = 1.7f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.85f;
            sun.shadowBias = 0.05f; sun.shadowNormalBias = 0.4f;
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
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = Reflection();
            RenderSettings.reflectionIntensity = 0.8f;
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

        /// <summary>The center lines, sidewalks, curbs and yards drawn as one mesh (one draw per material) instead of
        /// about 130 boxes. The center line boxes go; the sidewalk slabs keep their colliders without a renderer.</summary>
        static void Combine(Transform root)
        {
            var parts = root.GetComponentsInChildren<MeshRenderer>()
                .Where(r => r.name.StartsWith("Line ") || r.name == "Sidewalk" || r.name == "Curb" || r.name == "Yard").ToArray();
            if (parts.Length == 0) return;
            var order = new List<Material>();
            var mesh = MeshMerge.Merge(parts, Matrix4x4.identity, m => m, order, false, "Assets/Art/Meshes/Streets-Combined.asset", compress: false);
            var go = new GameObject("Streets Combined"); go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterials = order.ToArray();
            mr.shadowCastingMode = ShadowCastingMode.On; mr.receiveShadows = true;
            foreach (var r in parts)
            {
                if (r.name.StartsWith("Line ")) { Object.DestroyImmediate(r.gameObject); continue; }
                var mf = r.GetComponent<MeshFilter>(); Object.DestroyImmediate(r); if (mf) Object.DestroyImmediate(mf);
            }
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
