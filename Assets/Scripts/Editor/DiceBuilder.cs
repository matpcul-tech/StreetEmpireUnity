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

        /// <summary>A folding card table with a hand dealt and chips out, two crates for seats.</summary>
        public static GameObject CardsTemplate()
        {
            var root = new GameObject("Card Game Template"); root.transform.position = new Vector3(28f, -60f, 0f);
            var felt = Mat("CardFelt", new Color(0.12f, 0.33f, 0.2f), 0.05f);
            var leg = Mat("CardLeg", new Color(0.2f, 0.2f, 0.22f), 0.5f);
            var card = Mat("CardFace", new Color(0.93f, 0.92f, 0.88f), 0.3f);
            var red = Mat("ChipRed", new Color(0.62f, 0.13f, 0.12f), 0.4f);
            var blue = Mat("ChipBlue", new Color(0.14f, 0.24f, 0.55f), 0.4f);
            DicePart(root, "Table Top", felt, new Vector3(0f, 0.74f, 0f), new Vector3(0.95f, 0.04f, 0.95f), true);
            int n = 0;
            foreach (var (x, z) in new[] { (-0.42f, -0.42f), (0.42f, -0.42f), (-0.42f, 0.42f), (0.42f, 0.42f) })
                DicePart(root, $"Leg {++n}", leg, new Vector3(x, 0.36f, z), new Vector3(0.04f, 0.72f, 0.04f), true);
            for (int k = 0; k < 6; k++)
            {
                var c = DicePart(root, $"Card {k + 1}", card, new Vector3(-0.2f + SceneRnd(k) * 0.4f, 0.764f + k * 0.001f, -0.15f + SceneRnd(10 + k) * 0.3f), new Vector3(0.065f, 0.004f, 0.09f), false);
                c.transform.localRotation = Quaternion.Euler(0f, SceneRnd(20 + k) * 340f, 0f);
            }
            for (int k = 0; k < 7; k++)
                DicePart(root, $"Chip {k + 1}", k % 2 == 1 ? red : blue, new Vector3(0.22f + SceneRnd(30 + k) * 0.14f, 0.767f + (k % 3) * 0.012f, 0.2f + SceneRnd(40 + k) * 0.14f), new Vector3(0.044f, 0.006f, 0.044f), false, PrimitiveType.Cylinder);
            DicePart(root, "Crate 1", Mat("CrateBlue", new Color(0.16f, 0.27f, 0.45f), 0.2f), new Vector3(-0.2f, 0.15f, 0.95f), new Vector3(0.38f, 0.3f, 0.38f), true).transform.localRotation = Quaternion.Euler(0f, 11f, 0f);
            DicePart(root, "Crate 2", Mat("CrateRed", new Color(0.5f, 0.15f, 0.13f), 0.2f), new Vector3(1.0f, 0.15f, 0.7f), new Vector3(0.38f, 0.3f, 0.38f), true).transform.localRotation = Quaternion.Euler(0f, -23f, 0f);
            root.SetActive(false);
            return root;
        }

        /// <summary>A boombox on a milk crate and a cooler beside it. CornerScenes makes the two "Speaker" cones thump.</summary>
        public static GameObject StoopTemplate()
        {
            var root = new GameObject("Stoop Template"); root.transform.position = new Vector3(32f, -60f, 0f);
            DicePart(root, "Crate", Mat("CrateRed", new Color(0.5f, 0.15f, 0.13f), 0.2f), new Vector3(0f, 0.15f, -0.55f), new Vector3(0.38f, 0.3f, 0.38f), true).transform.localRotation = Quaternion.Euler(0f, 9f, 0f);
            var box = new GameObject("Boombox"); box.transform.SetParent(root.transform, false);
            box.transform.localPosition = new Vector3(0f, 0.45f, -0.55f); box.transform.localRotation = Quaternion.Euler(0f, 9f, 0f);
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
