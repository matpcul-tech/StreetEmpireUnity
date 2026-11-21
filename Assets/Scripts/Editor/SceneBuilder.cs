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
            // v75l: store-check-cashing takes store-bodega's place (Eastside has 9 store lots; the bodega file stays in the repo)
            ["store"] = new[] { "store-liquor-blank", "store-check-cashing", "store-laundromat", "store-barber", "store-pawn", "store-takeout", "shop-auto-garage", "shop-gas-station", "civic-church" },
            // Eastside houses read like the web game's: stucco walls, tile roofs, bars on the windows. The clapboard
            // and brick ones (house-wood-duplex, house-shotgun, house-boarded, house-rowhouse-pair) read as the
            // Midwest and are left out; their files stay in the repo. Each house gets a stucco wash (HouseTint).
            // v75: house-01-bungalow is back: only its roof is kept from the old file, the walls, porch, posts,
            // windows and door are rebuilt as clean geometry (tools/rebuild_house01.py), READY in Sovereign Asset.
            // v75l: the five new Eastside houses (release meshy-new-buildings-2026-11-19, finished in Sovereign Asset) join the
            // two finished ones, so the seven house slots are seven different models
            ["house"] = new[] { "house-02-two-story", "house-spanish-tile", "house-01-bungalow", "house-craftsman-porch", "house-stucco-duplex-2story", "house-boarded-stucco", "house-flat-roof", "house-03-duplex" },   // v75o: house-03-duplex back, the u75o finished file
            // v75l: apt-dingbat takes apt-motel-style's place (Eastside has 4 apartment lots; the motel file stays in the repo)
            ["apt"] = new[] { "apt-01-stucco", "apt-mixed-use", "apt-02-brick", "apt-dingbat" },
        };
        static readonly Dictionary<string, int> dealt = new();

        /// <summary>v75j: Matthew's finished Sovereign Asset files (byte for byte his v2 and v3 deliveries). Only these
        /// get scaling and their maps wired, nothing else; only their windows_lit and sign_lit glow, as their materials
        /// assign. Every other kit model is raw or an older Sovereign Asset run and glows nowhere.</summary>
        // v75k: all six are Matthew's finished-buildings-v4 delivery (Sovereign Assets branch awnings, b7a6717), byte for byte:
        // v75n: eleven files replaced by the u75n delivery (Sovereign Asset claude/asset-rooms, docs/sovereign-assets/u75n),
        // as delivered: windows_lit glows from its own emission map (windows_night), the white house redrawn on all sides.
        // v75m: house-01-bungalow and store-pawn replaced by the u75m files (Sovereign Asset claude/asset-openings,
        // docs/sovereign-assets/u75m), as delivered: lettered pawn sign_lit, redrawn bungalow front with a trim material:
        // house-01-bungalow (7,984,464 bytes, SHA-256 a7991f266152283d...): Material_0, walls, trim, door, bars, windows_lit, windows_dark
        // store-pawn (6,221,616 bytes, SHA-256 08c2603b95b1d8f5...): Material_0, walls, sign_lit, windows_lit
        // apt-01-stucco (5,718,764 bytes, SHA-256 5972b40481486552...): Material_0, door, walls, windows_lit, windows_dark
        // house-02-two-story (5,231,296 bytes, SHA-256 5f53253f1d6eb071...): Material_0, door, walls, windows_lit
        // apt-mixed-use (5,612,804 bytes, SHA-256 b75e17aaacc19d57...): Material_0, door, walls, windows_lit, windows_dark
        // store-barber (8,799,264 bytes, SHA-256 df9fd2071d4a7e9e...): Material_0, door, awning, walls, windows_lit
        // v75l: seven more, from the untouched Meshy files in release meshy-new-buildings-2026-11-19, finished with Sovereign
        // Asset main a642bef (Unity building, Size Height, about 58k triangles; docs/sovereign-assets/u75l):
        // house-spanish-tile (8,600,696 bytes, SHA-256 2a5626663236a176...): Material_0, door, walls, windows_lit, windows_dark
        // house-craftsman-porch (8,164,820 bytes, SHA-256 9dead538c2ecf2f0...): Material_0, walls, windows_lit, windows_dark
        // house-stucco-duplex-2story (8,509,276 bytes, SHA-256 bbea5573368c2111...): Material_0, door, walls, windows_lit, windows_dark
        // house-boarded-stucco (7,133,304 bytes, SHA-256 173cc622956a4af7...): Material_0, door, walls
        // house-flat-roof (7,800,828 bytes, SHA-256 9bb7bfb43bd23365...): Material_0, walls, windows_lit, windows_dark
        // apt-dingbat (7,066,688 bytes, SHA-256 6027c60d81addc3d...): Material_0, sign_lit, walls, windows_lit, windows_dark (sign_lit emission off)
        // store-check-cashing (5,966,416 bytes, SHA-256 75b476a53e0a9b59...): Material_0, door, walls
        // v75o: house-03-duplex redrawn in Sovereign Asset (claude/asset-ribbon, docs/sovereign-assets/u75o), as delivered:
        // house-03-duplex (3,209,036 bytes, SHA-256 23752832308e0ae7...): Material_0, trim, bars, windows_lit
        public static readonly HashSet<string> Finished = new()
        {
            "house-01-bungalow", "house-02-two-story", "apt-01-stucco", "apt-mixed-use", "store-pawn", "store-barber",
            "house-spanish-tile", "house-craftsman-porch", "house-stucco-duplex-2story", "house-boarded-stucco", "house-flat-roof", "apt-dingbat", "store-check-cashing",
            "house-03-duplex",   // v75o
        };

        /// <summary>Stucco washes dealt to the houses in turn (multiplied over the model's own colors): white, warm
        /// cream, peach, pale pink and sand, as along the web game's Eastside streets.</summary>
        static readonly Color[] HouseTint =
        {
            Color.white, new(1f, 0.95f, 0.86f), new(0.92f, 0.95f, 0.9f), new(1f, 0.9f, 0.88f), new(0.96f, 0.9f, 0.78f), new(0.9f, 0.93f, 0.96f), new(1f, 0.97f, 0.9f),
        };

        /// <summary>v75 wall colors (Matthew: too much of one salmon stucco box; apt-01's clean, light, warm gray stucco
        /// is the bar). The house walls are painted in turn (sRGB): cream, pale sage, blue gray, sand, off white,
        /// soft pink, pale yellow, by multiplying the wall texture by target over its own mean color.</summary>
        static readonly Color32[] HouseWall =
        {
            new(214, 204, 180, 255), new(180, 188, 160, 255), new(176, 186, 194, 255), new(206, 186, 150, 255),
            new(224, 220, 208, 255), new(214, 188, 178, 255), new(222, 208, 162, 255),
        };

        /// <summary>The mean color of each model's extracted wall texture (sRGB, measured from the jpgs) and, for the
        /// stores and apartments, the wall color it is painted toward. The dark Meshy brick and block walls
        /// (about 40 to 65 of 255) came up as black boxes at a distance; they move toward apt-01's lightness with
        /// their own hue (brick stays brick).</summary>
        static readonly Dictionary<string, (Color32 mean, Color32 target)> WallPaint = new()
        {
            ["apt-01-stucco"] = (new(200, 183, 165, 255), new(200, 183, 165, 255)),   // v75d: Matthew's v2 file, as it is
            ["apt-mixed-use"] = (new(128, 128, 128, 255), new(128, 128, 128, 255)),   // v75h: Matthew's v3 file, as it is
            ["apt-02-brick"] = (new(62, 46, 38, 255), new(128, 86, 66, 255)),
            ["apt-motel-style"] = (new(99, 89, 78, 255), new(178, 172, 154, 255)),
            ["civic-church"] = (new(53, 41, 37, 255), new(168, 152, 130, 255)),
            ["shop-gas-station"] = (new(36, 27, 24, 255), new(176, 172, 162, 255)),
            ["store-bodega"] = (new(66, 48, 44, 255), new(104, 128, 116, 255)),
            ["store-laundromat"] = (new(90, 81, 69, 255), new(152, 162, 168, 255)),
            ["store-takeout"] = (new(134, 111, 109, 255), new(188, 166, 122, 255)),
            ["store-pawn"] = (new(98, 95, 91, 255), new(98, 95, 91, 255)),   // v75d: Matthew's v2 file, as it is
            ["house-02-two-story"] = (new(150, 104, 84, 255), new(150, 104, 84, 255)),
            ["house-01-bungalow"] = (new(221, 214, 198, 255), new(221, 214, 198, 255)),
        };

        static Color Paint(Color32 mean, Color32 target)
        {
            float R(byte t, byte m) => Mathf.Clamp(t / Mathf.Max(1f, m), 0.5f, 2.6f);
            return new Color(R(target.r, mean.r), R(target.g, mean.g), R(target.b, mean.b), 1f);
        }

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
            var sidewalkStrip = TexMat("SidewalkStrips", "concrete_pavement", 1f, 0.08f, Color.white);
            var yard = TexMat("Yard", "sparse_grass", (w.block - 2) / 3.5f, 0.04f, new Color(0.74f, 0.71f, 0.52f));   // dry olive like the web yards
            YardVariation(yard, (w.block - 2) / 11f);
            var outer = TexMat("OuterGround", "sparse_grass", 1600f / 6f, 0.03f, new Color(0.62f, 0.56f, 0.42f));   // dry lots out to the haze, not green lawn
            var line = Mat("LaneYellow", new Color32(0xb5, 0x9a, 0x3a, 255), 0.3f);   // the web center line color
            var curb = Mat("Curb", new Color32(0x6e, 0x6b, 0x66, 255), 0.1f);

            var root = new GameObject("Eastside");
            // asphalt under the whole district plus a margin: the streets are what the blocks leave open
            float min = CityLayout.LineCoord(w, 0) - 30, max = CityLayout.LineCoord(w, 3) + 30;
            var (gx0, gz0) = WorldSpace.FromSource(min, min); var (gx1, gz1) = WorldSpace.FromSource(max, max);
            var ground = Box("Ground", root.transform, asphalt, new Vector3((gx0 + gx1) / 2, -0.05f, (gz0 + gz1) / 2), new Vector3(Mathf.Abs(gx1 - gx0), 0.1f, Mathf.Abs(gz1 - gz0)));
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.BatchingStatic);
            // v75c: URP's forward path lights each object with its nearest few lamps only (3 on High, 6 on Ultra),
            // so one 230 m asphalt box got almost none of the 36 lamp pools. The box keeps its collider and the
            // asphalt is drawn as about 26 m tiles (world-space UVs, same 4.5 m texture repeat), each in reach of a
            // lamp or two.
            {
                Object.DestroyImmediate(ground.GetComponent<MeshRenderer>()); Object.DestroyImmediate(ground.GetComponent<MeshFilter>());
                var tileMat = TexMat("AsphaltTiles", "worn_asphalt", 1f, 0.12f, new Color(0.78f, 0.78f, 0.8f));
                var tiles = new GameObject("Ground Tiles").transform; tiles.SetParent(root.transform, false);
                float x0 = Mathf.Min(gx0, gx1), x1 = Mathf.Max(gx0, gx1), z0 = Mathf.Min(gz0, gz1), z1 = Mathf.Max(gz0, gz1);
                int nx = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / 26f)), nz = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / 26f));
                for (int i = 0; i < nx; i++) for (int j = 0; j < nz; j++)
                {
                    float ax = Mathf.Lerp(x0, x1, i / (float)nx), bx = Mathf.Lerp(x0, x1, (i + 1) / (float)nx);
                    float az = Mathf.Lerp(z0, z1, j / (float)nz), bz = Mathf.Lerp(z0, z1, (j + 1) / (float)nz);
                    var t = Slab($"Ground {i}.{j}", tiles, tileMat, new Vector3((ax + bx) / 2, -0.05f, (az + bz) / 2), new Vector3(bx - ax, 0.1f, bz - az), 4.5f);
                    t.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    var tf = t.GetComponent<MeshFilter>(); tf.sharedMesh = MeshMerge.Save(tf.sharedMesh, $"Assets/Art/Meshes/Ground/Ground-{i}-{j}.asset", compress: false);
                }
            }
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
                // v75c: the slab keeps its collider; the sidewalk and curb are drawn as a strip per block side (see Ground Tiles)
                var slab = Box("Sidewalk", bgo.transform, sidewalk, new Vector3(ux, 0.11f, uz), new Vector3(w.block + 4, 0.22f, w.block + 4));
                Object.DestroyImmediate(slab.GetComponent<MeshRenderer>()); Object.DestroyImmediate(slab.GetComponent<MeshFilter>());
                SideStrips(bgo.transform, new Vector3(ux, 0, uz), w.block - 2, w.block + 4, 0.22f, "Sidewalk", sidewalkStrip, 2.6f);
                SideStrips(bgo.transform, new Vector3(ux, 0, uz), w.block + 4, w.block + 4.3f, 0.2f, "Curb", curb, 0f);
                Box("Yard", bgo.transform, yard, new Vector3(ux, 0.12f, uz), new Vector3(w.block - 2, 0.24f, w.block - 2), false);
                foreach (var spot in CityLayout.BuildingsFor(w, b)) if (PlaceBuilding(spot, bgo.transform)) placed++;
            }

            Combine(root.transform);
            var sunLight = Lighting(root.transform);
            // the sun and sky follow the web clock (DayCycle); it dims and lights the building windows too
            var cycle = new GameObject("Day Cycle").AddComponent<StreetEmpire.City.DayCycle>();
            cycle.transform.SetParent(root.transform, false);
            cycle.sun = sunLight; cycle.sky = RenderSettings.skybox; cycle.duskFog = RenderSettings.fogColor;
            cycle.dayFog = DayFog; cycle.nightFog = NightFog;
            var fillT = root.transform.Find("Character Fill"); if (fillT) cycle.fill = fillT.GetComponent<Light>();
            var moonT = root.transform.Find("Moon"); if (moonT) cycle.moon = moonT.GetComponent<Light>();
            // v74: only the Sovereign Asset windows_lit materials glow (a flat color, no emission map); walls never do
            cycle.windowMaterials = Object.FindObjectsByType<StreetEmpire.City.BuildingMarker>(FindObjectsSortMode.None)
                .SelectMany(b => b.GetComponentsInChildren<Renderer>(true)).SelectMany(r => r.sharedMaterials)
                .Where(m => m != null && Part(m) == "windows_lit" && m.IsKeywordEnabled("_EMISSION")).Distinct().ToArray();   // v75j: finished files only (raw windows_lit is dark glass)
            var player = PlayerBuilder.Build(w, s.blocks[0]);
            VehicleBuilder.BuildSedan(data, s.blocks[0]);
            GameBuilder.Build(data, s, player);
            // after the people and cars, so the props keep clear of where they start
            var xs = Enumerable.Range(0, 4).Select(i => WorldSpace.FromSource(CityLayout.LineCoord(w, i), 0).x).ToArray();
            var zs = Enumerable.Range(0, 4).Select(i => WorldSpace.FromSource(0, CityLayout.LineCoord(w, i)).z).ToArray();
            PropBuilder.Build(blockInfo, xs, zs, root.transform);
            cycle.skylineMaterials = PropBuilder.EmissiveMaterials.ToArray();
            cycle.signMaterials = Object.FindObjectsByType<StreetEmpire.City.BuildingMarker>(FindObjectsSortMode.None)
                .SelectMany(b => b.GetComponentsInChildren<Renderer>(true)).SelectMany(r => r.sharedMaterials)
                .Where(m => m != null && Part(m) == "sign_lit" && m.IsKeywordEnabled("_EMISSION")).Distinct().ToArray();   // v75j: a finished file's sign_lit only
            // v75j: no night dimming of the white house walls any more (its finished materials as delivered)
            cycle.nightDimMaterials = new Material[0];
            cycle.nightLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.name == "Storefront Light").ToArray();
            LampLightsBuild(root.transform, cycle);
            PostAndQuality(root.transform);

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, EastsideScene);
            if (additive) { SceneManager.SetActiveScene(prev); EditorSceneManager.CloseScene(scene, true); }
            Debug.Log($"[SceneBuilder] Eastside: {EastsideBlocks.Length} blocks, {placed} kit buildings");
        }

        /// <summary>Real heights in meters (v74 spec A.1: apartments 10, pawn shop 4.5, single story houses about 4,
        /// barbershop about 4.5; the rest read from each model's storeys). Sovereign Asset Size: Height used the same.</summary>
        public static readonly Dictionary<string, float> RealHeight = new()
        {
            ["apt-01-stucco"] = 10f /* v75n finish (u75n, 5,718,764 bytes, 5972b404) */, ["apt-02-brick"] = 10f, ["apt-mixed-use"] = 10f /* v75n finish (u75n, 5,612,804 bytes, b75e17aa) */, ["apt-motel-style"] = 7f, ["civic-church"] = 10f,
            ["house-01-bungalow"] = 4.2f /* v75n finish (u75n, 7,984,464 bytes, a7991f26) */, ["house-02-two-story"] = 6.5f /* v75n finish (u75n, 5,231,296 bytes, 5f53253f) */, ["house-03-duplex"] = 4f /* v75o finish (u75o, 3,209,036 bytes, 23752832) */, ["house-boarded"] = 7f,
            ["house-rowhouse-pair"] = 9f, ["house-shotgun"] = 4.5f, ["house-wood-duplex"] = 7.5f, ["shop-auto-garage"] = 5f,
            ["shop-gas-station"] = 5f, ["store-barber"] = 4.5f /* v75n finish (u75n, 8,799,264 bytes, df9fd207) */, ["store-bodega"] = 7f, ["store-laundromat"] = 6f,
            ["store-pawn"] = 4.5f /* v75n finish (u75n, 6,221,616 bytes, 08c2603b) */, ["store-takeout"] = 7.5f, ["store-liquor-blank"] = 4.9f,
            // v75l: the files are 4.5, 4.8, 6.5, 4.2, 3.6, 6.5 and 4.5 m; the flat roof house is drawn at 4 m so its 4.2 m front
            // clears the 4.5 m footprint floor
            ["house-spanish-tile"] = 4.5f, ["house-craftsman-porch"] = 4.8f, ["house-stucco-duplex-2story"] = 6.5f, ["house-boarded-stucco"] = 4.2f,
            ["house-flat-roof"] = 4f, ["apt-dingbat"] = 6.5f, ["store-check-cashing"] = 4.5f,
        };

        static bool PlaceBuilding(BuildingSpot spot, Transform parent)
        {
            var variants = Kit[spot.kind];
            dealt.TryGetValue(spot.kind, out int n); dealt[spot.kind] = n + 1;
            string name = variants[n % variants.Length];
            // v74: the near model is the Sovereign Asset "Unity building" finish of the source at its real height in
            // meters (docs/sovereign-assets/u74), cut to the lowest triangle target the app accepts; four materials
            // (Material_0, walls, windows_lit, windows_dark) kept apart
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}/{name}.glb");
            var holder = new GameObject($"{spot.kind} {spot.index} ({name})");
            var marker = holder.AddComponent<BuildingMarker>();
            marker.blockId = spot.blockId; marker.index = spot.index; marker.kind = spot.kind; marker.model = name;
            if (model == null) { Debug.LogError($"[SceneBuilder] missing kit model {name}"); Object.DestroyImmediate(holder); return false; }

            // v74: the files are in real meters now (Sovereign Asset Size: Height), so no fit to the 13 m lot; feet
            // on the ground, centered on the lot, same position and turn as before
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            inst.transform.SetParent(holder.transform, false);
            UrpMaterials.Apply(inst, name);
            var bounds = WorldBounds(inst);
            // Matthew's two finished files (apt-01-stucco, store-pawn) are still 1.9 m wide; every file is scaled
            // to its real height here (a no-op for the ones Sovereign Asset already sized). Multiply, not set:
            // glTFast folds a lone root node (and its scale) into the prefab root.
            if (RealHeight.TryGetValue(name, out float hReal) && bounds.size.y > 1e-4f) { inst.transform.localScale *= hReal / bounds.size.y; bounds = WorldBounds(inst); }
            // shift, not set: the imported root may already carry an offset of its own
            inst.transform.localPosition += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            bounds = WorldBounds(inst);
            var col = holder.AddComponent<BoxCollider>(); col.center = bounds.center; col.size = bounds.size;
            BindTextures(inst, name);
            // v75j: no SplitWindows (the per pane lit and dark scatter, door glass rules and warm room UVs): it changed
            // Matthew's meshes and UVs, and lit raw models' window triangles
            // v75e: the white house's roof has its own material now, so it is washed again, the walls only and lightly
            if (spot.kind == "house") { marker.wash = n % HouseTint.Length; if (!Finished.Contains(name)) Wash(inst, name, HouseTint[marker.wash], HouseWall[marker.wash % HouseWall.Length]); }
            // v75j: no lettered board over the pawn shop's own sign_lit (it covered it and glowed itself)
            if (name == "store-pawn") StorefrontLight(inst, holder);
            if (name == "store-liquor-blank") LiquorSign(inst, holder);
            var near = inst.GetComponentsInChildren<Renderer>();
            foreach (var r in near) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
            Compress(inst, name, "Near");
            // v75i (Matthew): no Far or Low levels and no LODGroup for any building: the finished GLB draws at every
            // distance and casts its own shadow. RuntimeLod stays as the list of the near renderers (graffiti uses it).
            holder.AddComponent<StreetEmpire.City.RuntimeLod>().near = near;
            // not batching static: with 20 models of about 58k triangles, static batching would copy every placed
            // mesh (36 of them) into combined buffers, roughly doubling mesh memory on phones and WebGL. The SRP
            // batcher keeps the draw calls cheap without it.

            var (x, z) = WorldSpace.FromSource(spot.x, spot.z);
            holder.transform.SetParent(parent, true);
            holder.transform.SetPositionAndRotation(new Vector3(x, 0.24f, z), Quaternion.Euler(0, WorldSpace.YawFromSource(spot.yaw) * Mathf.Rad2Deg, 0));
            return true;
        }

        /// <summary>Which of the four Sovereign Asset building materials this is: Material_0 (trim, roof, frames,
        /// doors), walls (the clean wall texture), windows_lit or windows_dark. UrpMaterials names the copies
        /// model_material, and wash copies model__material__tint.</summary>
        public static string Part(Material m)
        {
            string n = m.name;
            if (n.Contains("windows_lit")) return "windows_lit";
            if (n.Contains("windows_dark")) return "windows_dark";
            if (n.Contains("_walls") || n.EndsWith("walls")) return "walls";
            if (n.Contains("Material_0")) return "Material_0";
            // other glTF materials (store-liquor-blank's kitbash atlas parts) keep their own name: the material copies
            // are named {model}_{glTF name} and model names have no underscores
            int u = n.IndexOf('_');
            return u >= 0 && u < n.Length - 1 ? n.Substring(u + 1) : "Material_0";
        }

        /// <summary>v74: each building material gets its own extracted, GPU compressed textures
        /// (tools/extract_building_textures.mjs: name__part_basecolor, _normal, _metalsmooth). The old emission
        /// mask system is gone: no building has an emission map. windows_lit glows with a flat color only
        /// (DayCycle drives it from the clock), windows_dark is smooth dark glass, walls never emit.</summary>
        static void BindTextures(GameObject inst, string name)
        {
            var mats = inst.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToArray();
            bool finished = Finished.Contains(name);
            foreach (var m in mats)
            {
                string part = Part(m);
                // v75j: a finished file's windows_lit and sign_lit keep the emission its material assigns (UrpMaterials
                // carried the glTF emissiveFactor and emissive map over); its glass keeps its own maps too
                if (finished && (part == "windows_lit" || part == "windows_dark"))
                {
                    m.SetFloat("_Metallic", 0f);
                    EditorUtility.SetDirty(m); continue;
                }
                bool keepGlow = finished && part == "sign_lit";
                var ke = m.GetTexture("_EmissionMap"); var kc = m.GetColor("_EmissionColor");
                // buildings are never metal
                m.SetFloat("_Metallic", 0f); m.SetTexture("_MetallicGlossMap", null); m.DisableKeyword("_METALLICSPECGLOSSMAP");
                m.SetTexture("_EmissionMap", null); m.SetColor("_EmissionColor", Color.black); m.DisableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                if (part == "windows_lit" || part == "windows_dark")
                {
                    // flat glass: no base texture, so no triangle lines show in a pane
                    m.SetTexture("_BaseMap", null); m.SetTexture("_BumpMap", null); m.DisableKeyword("_NORMALMAP");
                    // v75j: a raw model's windows_lit is dark glass like windows_dark, no emission
                    m.SetColor("_BaseColor", new Color(0.06f, 0.07f, 0.08f)); m.SetFloat("_Smoothness", 0.75f);
                    EditorUtility.SetDirty(m); continue;
                }
                // v75e: the white house's roof is its own material (tools/house01_v2_fix.py) on the walls textures
                string tp = part == "roof" ? "walls" : part;
                var bc = AssetDatabase.LoadAssetAtPath<Texture2D>($"{BuildingTextures}/{name}__{tp}_basecolor.jpg");
                var nm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{BuildingTextures}/{name}__{tp}_normal.jpg");
                var ms = AssetDatabase.LoadAssetAtPath<Texture2D>($"{BuildingTextures}/{name}__{tp}_metalsmooth.png");
                if (bc == null) Debug.LogWarning($"[SceneBuilder] no extracted {part} texture for {name}");
                else m.SetTexture("_BaseMap", bc);
                m.SetTexture("_BumpMap", nm); m.SetFloat("_BumpScale", tp == "walls" || finished ? 1f : 0.55f);
                if (nm != null) m.EnableKeyword("_NORMALMAP"); else m.DisableKeyword("_NORMALMAP");
                // roughness from Sovereign Asset's surface maps, as URP smoothness in the alpha
                if (ms != null) { m.SetTexture("_MetallicGlossMap", ms); m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.SetFloat("_Smoothness", 1f); m.SetFloat("_SmoothnessTextureChannel", 0f); }
                else m.SetFloat("_Smoothness", 0.12f);
                // v75 wall paint for the stores and apartments (the houses are painted per placement in Wash)
                if (part == "walls" && !finished && WallPaint.TryGetValue(name, out var wp) && !name.StartsWith("house")) m.SetColor("_BaseColor", Paint(wp.mean, wp.target));
                // v75: the liquor store sign glows again, the sign face only, its own print as the emission map
                // (DayCycle.signMaterials: low by day, full at night)
                // v75d: the pawn shop's sign_lit (Matthew's v2 file) glows the same way
                // v75j: only a finished file's sign_lit, with its own emissive map and factor; raw signs do not glow
                if (keepGlow && kc.maxColorComponent > 0.01f)
                {
                    m.SetTexture("_EmissionMap", ke); m.SetColor("_EmissionColor", kc); m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
                EditorUtility.SetDirty(m);
            }
        }

        /// <summary>v75e: a warm spill in front of the pawn shop's lit storefront (Matthew: the front read black at
        /// 22:00). A small point light 1.3 m out from the glass, on the night ramp only (DayCycle.nightLights).</summary>
        static void StorefrontLight(GameObject inst, GameObject holder)
        {
            var b = WorldBounds(inst);
            var l = new GameObject("Storefront Light").AddComponent<Light>();
            l.transform.SetParent(holder.transform, false);
            l.transform.position = new Vector3(b.center.x, 2.1f, b.max.z + 1.3f);
            l.type = LightType.Point; l.color = new Color(1f, 0.72f, 0.45f); l.range = 6.5f; l.intensity = 0f;
            l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForcePixel; l.enabled = false;
        }

        /// <summary>v75c: the liquor store's blank main sign gets a lettered board (liquor-sign: LIQUOR, BEER, WINE,
        /// through Sovereign Asset) on its face. The board's own print is its emission map, so only the letters and
        /// their halo glow warm; DayCycle.signMaterials keeps it low by day and full at night.</summary>
        static void LiquorSign(GameObject inst, GameObject holder)
        {
            var face = inst.GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.name.Contains("MainSign"));
            if (face == null) { Debug.LogWarning("[SceneBuilder] liquor store has no MainSign"); return; }
            var sign = PropBuilder.Loose("liquor-sign");
            if (sign == null) return;
            sign.name = "Liquor Sign"; sign.transform.SetParent(holder.transform, false);
            var fb = face.bounds;   // the holder is still at the origin here, so world is the model's own frame
            sign.transform.rotation = holder.transform.rotation;
            var b = WorldBounds(sign);
            if (b.size.x > 1e-3f) sign.transform.localScale = new Vector3(fb.size.x * 0.94f / b.size.x, fb.size.y * 0.8f / b.size.y, 1f);
            b = WorldBounds(sign);
            sign.transform.position += new Vector3(fb.center.x - b.center.x, fb.center.y - b.center.y, fb.max.z + 0.02f - b.center.z);
            foreach (var r in sign.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true;
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    m.SetFloat("_Metallic", 0f);
                    // v75j: the board does not glow (the liquor store is a raw model; only finished sign_lit glows)
                    m.SetTexture("_EmissionMap", null); m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black);
                    EditorUtility.SetDirty(m);
                }
            }
        }

        /// <summary>A tinted copy of the house material (one asset per model and wash, so placements share it).</summary>
        static void Wash(GameObject inst, string name, Color tint, Color32 wall)
        {
            bool painted = WallPaint.TryGetValue(name, out var wp);
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || Part(mats[i]).StartsWith("windows")) continue;   // glass keeps its own color
                    // v75: a model with its own walls material gets the wall paint there and keeps its roof and trim
                    // v75o: the old light wash for the one atlas model (house-03-duplex) is gone; it is a finished file now
                    bool wallPart = Part(mats[i]) == "walls";
                    if (!painted || !wallPart) continue;
                    var mul = painted ? Paint(wp.mean, wall) : tint;
                    // v75e: the white house (its roof now apart) gets only a slight share of the stucco wash per lot
                    if (name == "house-01-bungalow") mul = Color.Lerp(Color.white, mul, 0.6f);
                    // v75g: no paint on the white house at all, Matthew's walls as they are (the blue gray lot paint made
                    // them read flat gray)
                    if (name == "house-01-bungalow" || name == "house-02-two-story") mul = Color.white;   // v75h: Matthew's v3 two story too
                    if (mul == Color.white) continue;
                    string path = $"{Mats}/Wash/{name}__{mats[i].name.Replace(" ", "_")}__{ColorUtility.ToHtmlStringRGB(mul * 0.5f)}.mat";
                    var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (m == null) { System.IO.Directory.CreateDirectory($"{Mats}/Wash"); m = new Material(mats[i]); AssetDatabase.CreateAsset(m, path); }
                    else m.CopyPropertiesFromMaterial(mats[i]);
                    m.SetColor("_BaseColor", painted ? mul : mats[i].GetColor("_BaseColor") * tint);
                    EditorUtility.SetDirty(m); mats[i] = m;
                }
                r.sharedMaterials = mats;
            }
        }

        // v75i: AddFar and Level (the Blender decimated Far and Low levels, a Low shadow copy and the LODGroup) are
        // gone at Matthew's request: decimating his finished files broke the walls atlas UVs and the normals.

        /// <summary>Points the building's mesh filters at compressed copies of the imported meshes (glTFast meshes
        /// go into the build uncompressed and were the bulk of the APK's mesh data). Made once per model and level.</summary>
        static void Compress(GameObject go, string name, string level)
        {
            int i = 0;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                string path = $"Assets/Art/Meshes/Buildings/{name}-{level.ToLowerInvariant()}-{i++}.asset";
                // v75i: saved uncompressed, so the finished file's UVs and normals are not quantized
                if (!compressed.TryGetValue(path, out var m)) compressed[path] = m = MeshMerge.Save(mf.sharedMesh, path, copy: true, compress: false);
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
            var r = PlayerBuilder.Rect("Graphics Button", canvas.transform, Vector2.one, Vector2.one, new Vector2(-30, -236), new Vector2(190, 52));
            r.pivot = Vector2.one;
            var img = r.gameObject.AddComponent<UnityEngine.UI.Image>(); img.color = new Color(0, 0, 0, 0.45f);
            var btn = r.gameObject.AddComponent<UnityEngine.UI.Button>(); btn.targetGraphic = img;
            var lr = PlayerBuilder.Rect("Label", r, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var t = lr.gameObject.AddComponent<UnityEngine.UI.Text>(); t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 24; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false; t.text = "GFX ULTRA";
            gq.label = t;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, gq.Toggle);
        }

        /// <summary>Big soft patches over the yard grass (greener olive, straw, bare dirt) so a yard is not one flat
        /// tile repeated: an own-work map (tools/draw_yard_variation.py, through Sovereign Asset) on the URP detail
        /// albedo slot, which multiplies by 2 so its 0.5 grey changes nothing.</summary>
        static void YardVariation(Material m, float tile)
        {
            var v = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Ground/yard_variation.png");
            if (v == null) return;
            m.SetTexture("_DetailAlbedoMap", v); m.SetTextureScale("_DetailAlbedoMap", new Vector2(tile, tile));
            m.SetFloat("_DetailAlbedoMapScale", 1f); m.EnableKeyword("_DETAIL_MULX2");
            EditorUtility.SetDirty(m);
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
        /// <summary>Direction to the sun: a fixed golden hour like the start of a new web game (17:48), the sun a few
        /// degrees up at the end of the east-west streets. The web sun direction (cos a * 0.8, sin a, 0.45) with x
        /// mirrored into Unity space (WorldSpace.FromSource), raised to 9 degrees so the streets stay readable.</summary>
        public static readonly Vector3 SunDir = SunAt(7f);   // v73: a lower sun, longer shadows
        static Vector3 SunAt(float elevation)
        {
            var h = new Vector2(0.8f, 0.45f).normalized; float e = elevation * Mathf.Deg2Rad;
            return new Vector3(h.x * Mathf.Cos(e), Mathf.Sin(e), h.y * Mathf.Cos(e));
        }

        // Golden hour palette taken from the live web game (sky, haze, fog), written as the scene values that land
        // on those screen colors after ACES (an inverse of the ACES curve on the captured sRGB colors)
        static readonly Color SkyZenith = new(0.49f, 0.50f, 0.47f), SkyUpper = new(0.72f, 0.68f, 0.56f),
            SkyHorizon = new(1.05f, 0.62f, 0.47f), SkyGlow = new(2.4f, 1.15f, 0.55f);

        // the fog color of each sky in linear (DayCycle.dayFog, duskFog, nightFog): the sky at and under the horizon is
        // exactly the fully fogged ground, so the far edge of the world melts into the haze with no line
        static readonly Color FogLinear = new Color(0.68f, 0.61f, 0.54f).linear;
        public static readonly Color DayFog = new(0.70f, 0.76f, 0.84f), NightFog = new(0.08f, 0.10f, 0.16f);

        /// <summary>v75 skies: 0 day (a blue sky, the sun high at 14:00), 1 the built golden hour, 2 night (near black
        /// blue overhead, the sodium glow of the city along the horizon, stars from the sky shader). The painted skyline strip of v74
        /// is gone: the far skyline is real towers now (BuildEdge).</summary>
        static Color SkyColor(Vector3 d, int mode)
        {
            float e = d.y; Color c;
            if (mode == 0)
            {
                var fog = DayFog.linear;
                Color zen = new(0.16f, 0.32f, 0.70f), up = new(0.38f, 0.55f, 0.88f), hor = new(0.62f, 0.70f, 0.82f);
                c = e < 0 ? fog : e < 0.06f ? Color.Lerp(fog, hor, Mathf.SmoothStep(0, 1, e / 0.06f))
                    : e < 0.35f ? Color.Lerp(hor, up, Mathf.SmoothStep(0, 1, (e - 0.06f) / 0.29f))
                    : Color.Lerp(up, zen, Mathf.SmoothStep(0, 1, (e - 0.35f) / 0.6f));
                float k = Mathf.Max(0, Vector3.Dot(d, SunAt(54f)));
                c += new Color(1f, 0.95f, 0.85f) * (Mathf.Pow(k, 24f) * 0.35f + Mathf.Pow(k, 300f) * 0.8f);
                if (k > 0.9996f) c += new Color(14f, 13f, 11f);
            }
            else if (mode == 2)
            {
                var fog = NightFog.linear;
                Color zen = new(0.004f, 0.006f, 0.016f), up = new(0.010f, 0.015f, 0.035f), glow = new(0.11f, 0.065f, 0.04f);
                c = e < 0 ? fog : e < 0.035f ? Color.Lerp(fog, glow, Mathf.SmoothStep(0, 1, e / 0.035f))
                    : e < 0.25f ? Color.Lerp(glow, up, Mathf.SmoothStep(0, 1, (e - 0.035f) / 0.215f))
                    : Color.Lerp(up, zen, Mathf.SmoothStep(0, 1, (e - 0.25f) / 0.6f));
                // v75b: no stars in the cubemap (a 256 texel face made them big dots); SkyBlend draws them per pixel
            }
            else
            {
                c = e < 0 ? FogLinear
                    : e < 0.08f ? Color.Lerp(FogLinear, SkyHorizon, Mathf.SmoothStep(0, 1, e / 0.08f))
                    : e < 0.3f ? Color.Lerp(SkyHorizon, SkyUpper, Mathf.SmoothStep(0, 1, (e - 0.08f) / 0.22f))
                    : Color.Lerp(SkyUpper, SkyZenith, Mathf.SmoothStep(0, 1, (e - 0.3f) / 0.6f));
                float k = Mathf.Max(0, Vector3.Dot(d, SunDir));
                float glow = Mathf.Pow(k, 6f) * 0.55f + Mathf.Pow(k, 48f) * 0.8f;
                c += SkyGlow * glow * Mathf.SmoothStep(0, 1, (e + 0.01f) / 0.06f) * Mathf.Clamp01(1f - Mathf.Max(0, e) * 1.6f);
                if (k > 0.9994f) c += new Color(10f, 7.5f, 5f);           // the sun disk
            }
            c.a = 1; return c;
        }

        /// <summary>A sky cubemap (half float so the glow round the sun stays HDR), used both as the skybox and,
        /// smaller, as the reflection probe for the whole district.</summary>
        static Cubemap SkyCube(string path, int n, int mode)
        {
            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if (cube != null && (cube.width != n || cube.format != TextureFormat.RGBAHalf)) { AssetDatabase.DeleteAsset(path); cube = null; }
            if (cube == null) { cube = new Cubemap(n, TextureFormat.RGBAHalf, true); AssetDatabase.CreateAsset(cube, path); }
            var px = new Color[n * n];
            foreach (CubemapFace f in new[] { CubemapFace.PositiveX, CubemapFace.NegativeX, CubemapFace.PositiveY, CubemapFace.NegativeY, CubemapFace.PositiveZ, CubemapFace.NegativeZ })
            {
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        // face rows run top to bottom (the usual cubemap face layout)
                        float u = 2f * (x + 0.5f) / n - 1f, v = 2f * (y + 0.5f) / n - 1f;
                        Vector3 d = f switch
                        {
                            CubemapFace.PositiveX => new Vector3(1, -v, -u), CubemapFace.NegativeX => new Vector3(-1, -v, u),
                            CubemapFace.PositiveY => new Vector3(u, 1, v), CubemapFace.NegativeY => new Vector3(u, -1, -v),
                            CubemapFace.PositiveZ => new Vector3(u, -v, 1), _ => new Vector3(-u, -v, -1),
                        };
                        px[y * n + x] = SkyColor(d.normalized, mode);
                    }
                cube.SetPixels(px, f);
            }
            cube.Apply(true);
            EditorUtility.SetDirty(cube);
            return cube;
        }

        /// <summary>v75 night light: one warm spot light per street lamp at its head, straight down (a 100 degree cone with
        /// a soft 50 degree inner core, range 13 m, about 3000 K, no shadows), so each lamp throws a real pool on the
        /// ground; switched on by LampLights. No ground decals.</summary>
        public static readonly Color Lamp3000K = new(1f, 0.706f, 0.42f);
        const float LampRange = 13f;
        static void LampLightsBuild(Transform root, StreetEmpire.City.DayCycle cycle)
        {
            var spots = PropBuilder.LampSpots;
            if (spots.Count == 0) return;
            var go = new GameObject("Lamp Lights"); go.transform.SetParent(root, false);
            var ll = go.AddComponent<StreetEmpire.City.LampLights>(); ll.cycle = cycle;
            ll.lights = new Light[spots.Count];
            for (int i = 0; i < spots.Count; i++)
            {
                var l = new GameObject($"Lamp Light {i + 1}").AddComponent<Light>(); l.transform.SetParent(go.transform, false);
                l.transform.position = spots[i];   // PropBuilder records the point under each lamp head
                l.type = LightType.Spot; l.range = LampRange; l.spotAngle = 100f; l.innerSpotAngle = 50f;
                l.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                l.color = Lamp3000K; l.shadows = LightShadows.None;
                l.intensity = 0f; l.renderingLayerMask = -1; l.renderMode = LightRenderMode.ForcePixel; l.enabled = false;
                ll.lights[i] = l;
            }
            // v75c: the globes glow only with the lamp level (a run-time copy of the glass, see LampLights)
            var inst = Object.FindFirstObjectByType<StreetEmpire.City.PropInstancer>();
            ll.instancer = inst;
            ll.globe = inst == null ? null : inst.batches.Where(b => b != null && b.name != null && b.name.StartsWith("street-lamp-1"))
                .SelectMany(b => b.materials).FirstOrDefault(m => m != null && m.name.Contains("glass"));
            if (ll.globe == null) Debug.LogWarning("[SceneBuilder] no street lamp globe material");
        }

        static Light Lighting(Transform root)
        {
            // one shadowed sun, trilight ambient, fog: cheap on phones and WebGL. Golden hour like the original web
            // game: a low warm sun, a strong lavender sky fill that outweighs it, warm haze. v73: the sun lower (7 deg)
            // and warmer, the haze pushed out to 90 m to 340 m so the near street is clear.
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1f, 0.70f, 0.47f); sun.intensity = 0.95f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.8f;
            sun.shadowBias = 0.08f; sun.shadowNormalBias = 0.5f;          // a grazing sun needs a little more bias
            sun.transform.rotation = Quaternion.LookRotation(-SunDir);
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.63f, 0.61f, 0.735f);
            RenderSettings.ambientEquatorColor = new Color(0.57f, 0.53f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.40f, 0.33f, 0.31f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            // v74: haze pushed out to 120 m to 460 m and no longer pink (it washed the street shots)
            RenderSettings.fogStartDistance = 120; RenderSettings.fogEndDistance = 460;
            RenderSettings.fogColor = new Color(0.68f, 0.61f, 0.54f);
            var sky = AssetDatabase.LoadAssetAtPath<Material>($"{Mats}/Sky.mat");
            var skyShader = Shader.Find("StreetEmpire/SkyBlend");
            if (sky == null) { sky = new Material(skyShader); AssetDatabase.CreateAsset(sky, $"{Mats}/Sky.mat"); }
            sky.shader = skyShader;
            // v75: three skies blended by the clock (DayCycle): day, the built golden hour, night with the city glow
            sky.SetTexture("_DayTex", SkyCube("Assets/Art/Materials/SkyDay.cubemap", 256, 0));
            sky.SetTexture("_DuskTex", SkyCube("Assets/Art/Materials/SkyGoldenHour.cubemap", 256, 1));
            sky.SetTexture("_NightTex", SkyCube("Assets/Art/Materials/SkyNight.cubemap", 256, 2));
            sky.SetVector("_Weights", new Vector4(0, 1, 0, 0)); sky.SetFloat("_Exposure", 1f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = SkyCube("Assets/Art/Materials/SkyReflection.cubemap", 64, 1);
            RenderSettings.reflectionIntensity = 0.35f;
            // a soft fill for people only (rendering layer 2, set on every character): from the side away from the sun,
            // no shadows, so the player and the people on the street stand out from the road
            var fill = new GameObject("Character Fill").AddComponent<Light>();
            fill.type = LightType.Directional; fill.color = new Color(0.86f, 0.84f, 1f); fill.intensity = 0.55f; fill.shadows = LightShadows.None;
            fill.renderingLayerMask = 2;
            fill.transform.rotation = Quaternion.LookRotation(-new Vector3(-SunDir.x, 0.55f, -SunDir.z).normalized);
            fill.transform.SetParent(root, false);
            // v74: moonlight, a blue gray directional of its own, off by day (DayCycle turns it on with the night and
            // puts it high in another quarter of the sky). When the sun is switched off it is the main light and casts the shadows.
            var moon = new GameObject("Moon").AddComponent<Light>();
            moon.type = LightType.Directional; moon.color = new Color(0.62f, 0.68f, 0.80f); moon.intensity = 0f;
            moon.shadows = LightShadows.Soft; moon.shadowStrength = 0.35f; moon.shadowBias = 0.08f; moon.shadowNormalBias = 0.5f;
            moon.enabled = false; moon.transform.SetParent(root, false);
            return sun;
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

        /// <summary>v75c: a ring (outer size <paramref name="outer"/>, hole <paramref name="inner"/>, top at
        /// <paramref name="top"/>) as four side boxes named "{name} {side}": the north and south ones run the full
        /// width, the east and west ones fill between them.</summary>
        static void SideStrips(Transform parent, Vector3 c, float inner, float outer, float top, string name, Material m, float uvMeters)
        {
            float o = outer / 2, i = inner / 2, wd = o - i, mid = (o + i) / 2;
            Slab($"{name} N", parent, m, c + new Vector3(0, top / 2, mid), new Vector3(outer, top, wd), uvMeters);
            Slab($"{name} S", parent, m, c + new Vector3(0, top / 2, -mid), new Vector3(outer, top, wd), uvMeters);
            Slab($"{name} E", parent, m, c + new Vector3(mid, top / 2, 0), new Vector3(wd, top, inner), uvMeters);
            Slab($"{name} W", parent, m, c + new Vector3(-mid, top / 2, 0), new Vector3(wd, top, inner), uvMeters);
        }

        /// <summary>v75c: a render-only box (no bottom face, no collider) with world-space planar UVs, one texture
        /// repeat per <paramref name="uvMeters"/> (0: plain 0..1 per face), so tiles and strips line up seamlessly.</summary>
        static GameObject Slab(string name, Transform parent, Material m, Vector3 center, Vector3 size, float uvMeters)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, true); go.transform.position = center;
            var h = size / 2; var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            void Face(Vector3 nrm, Vector3 a, Vector3 b, Vector3 cc, Vector3 d, System.Func<Vector3, Vector2> proj)
            {
                int k = v.Count;
                foreach (var q in new[] { a, b, cc, d })
                {
                    v.Add(q); n.Add(nrm);
                    var wp = q + center;
                    uv.Add(uvMeters > 0 ? proj(wp) / uvMeters : proj(q - (a + cc) / 2) / 1f);
                }
                tri.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
            }
            float x = h.x, y = h.y, z = h.z;
            Face(Vector3.up, new(-x, y, -z), new(-x, y, z), new(x, y, z), new(x, y, -z), p => new Vector2(p.x, p.z));
            Face(Vector3.forward, new(x, -y, z), new(x, y, z), new(-x, y, z), new(-x, -y, z), p => new Vector2(-p.x, p.y));
            Face(Vector3.back, new(-x, -y, -z), new(-x, y, -z), new(x, y, -z), new(x, -y, -z), p => new Vector2(p.x, p.y));
            Face(Vector3.right, new(x, -y, -z), new(x, y, -z), new(x, y, z), new(x, -y, z), p => new Vector2(p.z, p.y));
            Face(Vector3.left, new(-x, -y, z), new(-x, y, z), new(-x, y, -z), new(-x, -y, -z), p => new Vector2(-p.z, p.y));
            var mesh = new Mesh { name = name };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0);
            mesh.RecalculateTangents(); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = m;
            // not static batched: that would fold the chunks back into one mesh (and unreadable for the tests)
            return go;
        }

        /// <summary>The center lines, sidewalks, curbs and yards drawn as one mesh (one draw per material) instead of
        /// about 130 boxes. The center line boxes go; the sidewalk slabs keep their colliders without a renderer.</summary>
        static void Combine(Transform root)
        {
            var parts = root.GetComponentsInChildren<MeshRenderer>()
                .Where(r => r.name.StartsWith("Line ") || r.name.StartsWith("Sidewalk ") || r.name.StartsWith("Curb ") || r.name == "Yard").ToArray();
            if (parts.Length == 0) return;
            // v75c: one mesh per block side (its sidewalk and curb strips), one per block yard, and the center lines
            // by 26 m cell, so each chunk is lit by the one or two lamps that reach it (see Ground Tiles)
            string Key(MeshRenderer r)
            {
                if (r.name.StartsWith("Line ")) { var c = r.bounds.center; return $"Lines {Mathf.FloorToInt(c.x / 26f)}.{Mathf.FloorToInt(c.z / 26f)}"; }
                var block = r.transform.parent ? r.transform.parent.name : "";
                return r.name == "Yard" ? $"{block} Yard" : $"{block} Side {r.name[^1]}";
            }
            var top = new GameObject("Streets Combined"); top.transform.SetParent(root, false);
            Directory.CreateDirectory("Assets/Art/Meshes/Streets");
            int chunk = 0;
            foreach (var g in parts.GroupBy(Key).OrderBy(g => g.Key))
            {
                var order = new List<Material>();
                var mesh = MeshMerge.Merge(g, Matrix4x4.identity, m => m, order, false, $"Assets/Art/Meshes/Streets/Streets-{chunk++:000}.asset", compress: false);
                var go = new GameObject($"Streets {g.Key}"); go.transform.SetParent(top.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterials = order.ToArray();
                mr.shadowCastingMode = g.Key.Contains("Lines") ? ShadowCastingMode.Off : ShadowCastingMode.On; mr.receiveShadows = true;
            }
            foreach (var r in parts)
            {
                if (r.name.StartsWith("Line ") || r.name.StartsWith("Sidewalk ") || r.name.StartsWith("Curb ")) { Object.DestroyImmediate(r.gameObject); continue; }
                var mf = r.GetComponent<MeshFilter>(); Object.DestroyImmediate(r); if (mf) Object.DestroyImmediate(mf);
            }
            Debug.Log($"[SceneBuilder] streets in {chunk} chunks, ground in {root.Find("Ground Tiles")?.childCount ?? 0} tiles");
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
