// The corner dice game's props (StreetDice copies them onto a held block's corner): a chalk ring on the sidewalk,
// two dice with real pips, a few bills and a flattened box stood up as the backstop the dice are thrown against.
// From the web build's hustle.js dice scene.
using UnityEngine;
using UnityEngine.Rendering;

namespace StreetEmpire.EditorTools
{
    public static partial class SceneBuilder
    {
        /// <summary>The inactive template, parked under the map.</summary>
        public static GameObject DiceTemplate()
        {
            var root = new GameObject("Dice Game Template"); root.transform.position = new Vector3(24f, -60f, 0f);
            var chalk = Mat("DiceChalk", new Color(0.86f, 0.85f, 0.8f), 0.05f); chalk.SetFloat("_Cull", 0f);
            var ivory = Mat("DiceIvory", new Color(0.93f, 0.9f, 0.82f), 0.5f);
            var pip = Mat("DicePip", new Color(0.06f, 0.05f, 0.05f), 0.3f);
            var bill = Mat("DiceBill", new Color(0.33f, 0.5f, 0.3f), 0.1f);
            var card = Mat("DiceCardboard", new Color(0.55f, 0.42f, 0.27f), 0.05f);

            var ring = new GameObject("Chalk Ring"); ring.transform.SetParent(root.transform, false); ring.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            ring.AddComponent<MeshFilter>().sharedMesh = DiceRing(0.7f, 0.76f, 40);
            var rr = ring.AddComponent<MeshRenderer>(); rr.sharedMaterial = chalk; rr.shadowCastingMode = ShadowCastingMode.Off;

            for (int i = 0; i < 2; i++)
            {
                var die = DicePart(root, $"Die {i + 1}", ivory, new Vector3(-0.12f + i * 0.26f, 0.065f, -0.3f - i * 0.12f), new Vector3(0.13f, 0.13f, 0.13f), true);
                die.transform.localRotation = Quaternion.Euler(0f, 25f + i * 50f, 0f);
                for (int n = 1; n <= 6; n++)
                {
                    Vector3 face = DiceFace(n), u = Mathf.Abs(face.y) > 0.5f ? Vector3.right : Vector3.up, v = Vector3.Cross(face, u);
                    foreach (var (x, y) in DicePips(n))
                    {
                        var dot = DicePart(die, "Pip", pip, face * 0.5f + (u * x + v * y) * 0.27f, new Vector3(0.17f, 0.17f, 0.02f), false);
                        dot.transform.localRotation = Quaternion.LookRotation(face, u);
                    }
                }
            }
            for (int i = 0; i < 3; i++)
            {
                var b = DicePart(root, $"Bill {i + 1}", bill, new Vector3(0.42f + i * 0.05f, 0.006f + i * 0.004f, 0.28f - i * 0.07f), new Vector3(0.16f, 0.004f, 0.07f), false);
                b.transform.localRotation = Quaternion.Euler(0f, 20f + i * 35f, 0f);
            }
            var stop = DicePart(root, "Backstop", card, new Vector3(0f, 0.16f, -0.74f), new Vector3(0.62f, 0.34f, 0.03f), true);
            stop.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            root.SetActive(false);
            return root;
        }

        /// <summary>A folding card table with a hand dealt and chips out, two milk crates for seats.</summary>
        public static GameObject CardsTemplate()
        {
            var root = new GameObject("Card Game Template"); root.transform.position = new Vector3(28f, -60f, 0f);
            var leg = Mat("CardLeg", new Color(0.27f, 0.26f, 0.25f), 0.42f);
            var card = Mat("CardFace", new Color(0.93f, 0.92f, 0.88f), 0.3f);
            var red = Mat("ChipRed", new Color(0.62f, 0.13f, 0.12f), 0.4f);
            var blue = Mat("ChipBlue", new Color(0.14f, 0.24f, 0.55f), 0.4f);
            // block pass: a folding card table (a worn vinyl top in a steel edge, an apron, four tube legs splayed a
            // little, each on its folding brace), in place of the flat green slab on four sticks. Top 0.72 m up.
            var top = Mat("CardTableTop", Color.white, 0.22f);
            top.SetTexture("_BaseMap", WornTableTop()); UnityEditor.EditorUtility.SetDirty(top);
            DicePart(root, "Table Top", top, new Vector3(0f, 0.709f, 0f), new Vector3(0.86f, 0.022f, 0.86f), true);
            DiceMerged(root, "Table Frame", leg, Vector3.zero, "Assets/Art/Meshes/Props/card-table-frame.asset", f =>
            {
                foreach (int sgn in new[] { -1, 1 })
                {
                    DicePart(f, "Edge", leg, new Vector3(0f, 0.709f, sgn * 0.438f), new Vector3(0.894f, 0.034f, 0.018f), true);
                    DicePart(f, "Edge", leg, new Vector3(sgn * 0.438f, 0.709f, 0f), new Vector3(0.018f, 0.034f, 0.894f), true);
                    DicePart(f, "Apron", leg, new Vector3(0f, 0.683f, sgn * 0.36f), new Vector3(0.74f, 0.03f, 0.02f), true);
                    DicePart(f, "Apron", leg, new Vector3(sgn * 0.36f, 0.683f, 0f), new Vector3(0.02f, 0.03f, 0.74f), true);
                }
                foreach (var (x, z) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
                {
                    DicePart(f, "Leg", leg, new Vector3(x * 0.375f, 0.349f, z * 0.375f), new Vector3(0.026f, 0.7f, 0.026f), true).transform.localRotation = Quaternion.Euler(z * -2.4f, 0f, x * 2.4f);
                    DicePart(f, "Brace", leg, new Vector3(x * 0.255f, 0.565f, z * 0.352f), new Vector3(0.34f, 0.016f, 0.012f), true).transform.localRotation = Quaternion.Euler(0f, 0f, x * -44f);
                    DicePart(f, "Foot", leg, new Vector3(x * 0.39f, 0.012f, z * 0.39f), new Vector3(0.034f, 0.024f, 0.034f), true);
                }
            });
            for (int k = 0; k < 6; k++)
            {
                var c = DicePart(root, $"Card {k + 1}", card, new Vector3(-0.2f + SceneRnd(k) * 0.4f, 0.724f + k * 0.001f, -0.15f + SceneRnd(10 + k) * 0.3f), new Vector3(0.065f, 0.004f, 0.09f), false);
                c.transform.localRotation = Quaternion.Euler(0f, SceneRnd(20 + k) * 340f, 0f);
            }
            for (int k = 0; k < 7; k++)
                DicePart(root, $"Chip {k + 1}", k % 2 == 1 ? red : blue, new Vector3(0.22f + SceneRnd(30 + k) * 0.14f, 0.727f + (k % 3) * 0.012f, 0.2f + SceneRnd(40 + k) * 0.14f), new Vector3(0.044f, 0.006f, 0.044f), false, PrimitiveType.Cylinder);
            // the seats: two milk crates turned over (open lattice sides, MilkCrate), not plain cubes
            MilkCrate(root, "Crate 1", Mat("CrateBlue", new Color(0.13f, 0.25f, 0.44f), 0.32f), new Vector3(-0.2f, MilkCrateHeight / 2f, 0.95f), Quaternion.Euler(180f, 11f, 0f));
            MilkCrate(root, "Crate 2", Mat("CrateRed", new Color(0.52f, 0.13f, 0.1f), 0.32f), new Vector3(1.0f, MilkCrateHeight / 2f, 0.7f), Quaternion.Euler(180f, -23f, 0f));
            root.SetActive(false);
            return root;
        }

        /// <summary>A boombox on a milk crate and a cooler beside it. CornerScenes makes the two "Speaker" cones thump.</summary>
        public static GameObject StoopTemplate()
        {
            var root = new GameObject("Stoop Template"); root.transform.position = new Vector3(32f, -60f, 0f);
            MilkCrate(root, "Crate", Mat("CrateRed", new Color(0.52f, 0.13f, 0.1f), 0.32f), new Vector3(0f, MilkCrateHeight / 2f, -0.55f), Quaternion.Euler(180f, 9f, 0f));
            var box = new GameObject("Boombox"); box.transform.SetParent(root.transform, false);
            box.transform.localPosition = new Vector3(0f, MilkCrateHeight + 0.15f, -0.55f); box.transform.localRotation = Quaternion.Euler(0f, 9f, 0f);
            DicePart(box, "Body", Mat("BoomBody", new Color(0.11f, 0.11f, 0.12f), 0.45f), Vector3.zero, new Vector3(0.62f, 0.3f, 0.16f), true);
            var cone = Mat("BoomCone", new Color(0.32f, 0.33f, 0.35f), 0.6f);
            foreach (float x in new[] { -0.19f, 0.19f })
            {
                // a cylinder on its side, flat face to the street
                var sp = DicePart(box, "Speaker", cone, new Vector3(x, 0f, 0.085f), new Vector3(StreetEmpire.Game.CornerScenes.SpeakerSize, 0.01f, StreetEmpire.Game.CornerScenes.SpeakerSize), false, PrimitiveType.Cylinder);
                sp.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            DicePart(box, "Handle", Mat("BoomTrim", new Color(0.6f, 0.6f, 0.62f), 0.7f), new Vector3(0f, 0.19f, 0f), new Vector3(0.4f, 0.025f, 0.03f), false);
            DicePart(root, "Cooler", Mat("CoolerBlue", new Color(0.15f, 0.4f, 0.62f), 0.4f), new Vector3(1.05f, 0.15f, 0.25f), new Vector3(0.5f, 0.3f, 0.32f), true).transform.localRotation = Quaternion.Euler(0f, -29f, 0f);
            DicePart(root, "Cooler Lid", Mat("CoolerLid", new Color(0.9f, 0.9f, 0.88f), 0.4f), new Vector3(1.05f, 0.325f, 0.25f), new Vector3(0.52f, 0.05f, 0.34f), false).transform.localRotation = Quaternion.Euler(0f, -29f, 0f);
            root.SetActive(false);
            return root;
        }

        public const float MilkCrateSize = 0.34f, MilkCrateHeight = 0.28f;
        static Mesh milkCrateMesh;

        /// <summary>A milk crate as one mesh, built from boxes: four corner posts, a rim top and bottom, two slats
        /// and four uprights a side (the open lattice) and a grid floor. Open at the top; centered on its middle.</summary>
        public static Mesh MilkCrateMesh()
        {
            if (milkCrateMesh != null) return milkCrateMesh;
            var mat = Mat("CrateBlue", new Color(0.13f, 0.25f, 0.44f), 0.32f);
            var temp = new GameObject("Milk Crate parts");
            const float w = MilkCrateSize, h = MilkCrateHeight, e = w / 2f - 0.011f;
            foreach (var (x, z) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
                DicePart(temp, "Post", mat, new Vector3(x * e, 0f, z * e), new Vector3(0.03f, h, 0.03f), true);
            foreach (int sgn in new[] { -1, 1 })
                for (int axis = 0; axis < 2; axis++)
                {
                    // one wall: along x (axis 0) or along z (axis 1)
                    Vector3 P(float along, float y) => axis == 0 ? new Vector3(along, y, sgn * e) : new Vector3(sgn * e, y, along);
                    Vector3 S(float along, float tall, float thick) => axis == 0 ? new Vector3(along, tall, thick) : new Vector3(thick, tall, along);
                    DicePart(temp, "Rim", mat, P(0f, h / 2f - 0.02f), S(w - 0.03f, 0.04f, 0.022f), true);
                    DicePart(temp, "Rim", mat, P(0f, -h / 2f + 0.013f), S(w - 0.03f, 0.026f, 0.022f), true);
                    foreach (float y in new[] { -0.045f, 0.04f }) DicePart(temp, "Slat", mat, P(0f, y), S(w - 0.03f, 0.016f, 0.012f), true);
                    for (int i = 0; i < 4; i++) DicePart(temp, "Upright", mat, P(-0.102f + i * 0.068f, 0f), S(0.014f, h - 0.03f, 0.012f), true);
                }
            for (int i = 0; i < 4; i++)
            {
                DicePart(temp, "Floor", mat, new Vector3(-0.102f + i * 0.068f, -h / 2f + 0.007f, 0f), new Vector3(0.018f, 0.014f, w - 0.03f), true);
                DicePart(temp, "Floor", mat, new Vector3(0f, -h / 2f + 0.007f, -0.102f + i * 0.068f), new Vector3(w - 0.03f, 0.014f, 0.018f), true);
            }
            milkCrateMesh = MeshMerge.Merge(temp, m => m, new System.Collections.Generic.List<Material>(), Vector3.one, "Assets/Art/Meshes/Props/milk-crate.asset");
            Object.DestroyImmediate(temp);
            return milkCrateMesh;
        }

        static GameObject MilkCrate(GameObject parent, string name, Material mat, Vector3 at, Quaternion rot)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = at; go.transform.localRotation = rot;
            go.AddComponent<MeshFilter>().sharedMesh = MilkCrateMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Box parts built under a holder by <paramref name="build"/>, kept as one mesh and one renderer.</summary>
        static GameObject DiceMerged(GameObject parent, string name, Material mat, Vector3 at, string meshPath, System.Action<GameObject> build)
        {
            var temp = new GameObject(name + " parts");
            build(temp);
            var mesh = MeshMerge.Merge(temp, m => m, new System.Collections.Generic.List<Material>(), Vector3.one, meshPath);
            Object.DestroyImmediate(temp);
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false); go.transform.localPosition = at;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>The card table's top, drawn here: faded green vinyl gone pale where hands and cards rub, darker
        /// toward the edge, with scuffs, two cup rings and a burn. 128 px, the same every build.</summary>
        static Texture2D WornTableTop()
        {
            const int n = 128; const string path = "Assets/Art/Textures/Generated/card-table-top.asset";
            var rnd = new System.Random(555);
            float R() => (float)rnd.NextDouble();
            var px = new Color[n * n];
            Color vinyl = new Color(0.15f, 0.27f, 0.2f), pale = new Color(0.42f, 0.46f, 0.39f), dirt = new Color(0.12f, 0.11f, 0.09f);
            float Noise(float x, float y) => Mathf.PerlinNoise(x + 13.7f, y + 4.1f);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n - 0.5f, v = (y + 0.5f) / n - 0.5f, r = Mathf.Sqrt(u * u + v * v);
                float wear = Mathf.Clamp01(1.15f - r * 2.6f) * (0.35f + 0.65f * Noise(u * 5f, v * 5f)) * 0.75f;
                float grime = Mathf.Clamp01((Mathf.Max(Mathf.Abs(u), Mathf.Abs(v)) - 0.36f) * 7f) * 0.6f + (Noise(u * 11f, v * 11f) - 0.5f) * 0.25f;
                var c = Color.Lerp(vinyl, pale, wear); c = Color.Lerp(c, dirt, Mathf.Clamp01(grime));
                c *= 0.9f + 0.2f * Noise(u * 40f, v * 40f); c.a = 1f;
                px[y * n + x] = c;
            }
            void Dot(float fx, float fy, Color c, float a)
            {
                int x = Mathf.RoundToInt(fx), y = Mathf.RoundToInt(fy); if (x < 0 || y < 0 || x >= n || y >= n) return;
                var was = px[y * n + x]; var now = Color.Lerp(was, c, a); now.a = 1f; px[y * n + x] = now;
            }
            for (int k = 0; k < 16; k++)   // scuffs: short pale scratches
            {
                float x = 12 + R() * 104, y = 12 + R() * 104, a = R() * Mathf.PI, len = 6 + R() * 22;
                for (float t = 0; t < len; t += 0.5f) Dot(x + Mathf.Cos(a) * t, y + Mathf.Sin(a) * t, new Color(0.6f, 0.62f, 0.55f), 0.55f);
            }
            foreach (var (cx, cy, rad) in new[] { (38f, 86f, 8.5f), (92f, 40f, 7.5f) })   // cup rings
                for (float a = 0; a < Mathf.PI * 2f; a += 0.02f)
                    if (Mathf.Sin(a * 3f + cx) > -0.7f) { Dot(cx + Mathf.Cos(a) * rad, cy + Mathf.Sin(a) * rad, dirt, 0.6f); Dot(cx + Mathf.Cos(a) * (rad + 1f), cy + Mathf.Sin(a) * (rad + 1f), dirt, 0.3f); }
            for (int dy = -2; dy <= 2; dy++) for (int dx = -2; dx <= 2; dx++) if (dx * dx + dy * dy <= 5) Dot(80 + dx, 98 + dy, new Color(0.04f, 0.03f, 0.03f), 0.9f);   // a burn
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "card-table-top", wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(px); tex.Apply(true);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            if (UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null) UnityEditor.AssetDatabase.DeleteAsset(path);
            UnityEditor.AssetDatabase.CreateAsset(tex, path);
            return tex;
        }

        /// <summary>The web build's rnd(id, k): the same scatter every build.</summary>
        static float SceneRnd(int k) { float v = Mathf.Sin(127.1f + k * 311.7f) * 43758.5453f; return v - Mathf.Floor(v); }

        /// <summary>Same faces as StreetDice.Face: 1 up, 6 down, 2 and 5 front and back, 3 and 4 the sides.</summary>
        static Vector3 DiceFace(int n) => n == 1 ? Vector3.up : n == 6 ? Vector3.down : n == 2 ? Vector3.forward : n == 5 ? Vector3.back : n == 3 ? Vector3.right : Vector3.left;

        static (float, float)[] DicePips(int n)
        {
            switch (n)
            {
                case 1: return new[] { (0f, 0f) };
                case 2: return new[] { (-1f, -1f), (1f, 1f) };
                case 3: return new[] { (-1f, -1f), (0f, 0f), (1f, 1f) };
                case 4: return new[] { (-1f, -1f), (1f, 1f), (-1f, 1f), (1f, -1f) };
                case 5: return new[] { (-1f, -1f), (1f, 1f), (-1f, 1f), (1f, -1f), (0f, 0f) };
                default: return new[] { (-1f, -1f), (1f, 1f), (-1f, 1f), (1f, -1f), (-1f, 0f), (1f, 0f) };
            }
        }

        static GameObject DicePart(GameObject parent, string name, Material mat, Vector3 at, Vector3 size, bool shadow, PrimitiveType shape = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(shape); go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent.transform, false); go.transform.localPosition = at; go.transform.localScale = size;
            var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = shadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        /// <summary>A flat ring facing up.</summary>
        static Mesh DiceRing(float r0, float r1, int seg)
        {
            var v = new Vector3[seg * 2]; var tri = new int[seg * 6];
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v[i * 2] = d * r0; v[i * 2 + 1] = d * r1;
                int n = (i + 1) % seg;
                tri[i * 6] = i * 2; tri[i * 6 + 1] = n * 2 + 1; tri[i * 6 + 2] = i * 2 + 1;
                tri[i * 6 + 3] = i * 2; tri[i * 6 + 4] = n * 2; tri[i * 6 + 5] = n * 2 + 1;
            }
            var m = new Mesh { name = "Chalk Ring" }; m.vertices = v; m.triangles = tri; m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
    }
}
