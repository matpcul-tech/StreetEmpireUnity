// M4: the session (cash, heat, police), Unc and his first job "Bag Back", a few pedestrians, the officer
// template the police dispatch copies, and the top-of-screen HUD. Numbers are baked from Assets/Data.
using System.Linq;
using StreetEmpire.Game;
using StreetEmpire.Player;
using StreetEmpire.Sim;
using StreetEmpire.Sim.Data;
using StreetEmpire.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace StreetEmpire.EditorTools
{
    public static class GameBuilder
    {
        public static void Build(GameData data, SimState s, GameObject player)
        {
            var w = data.World;
            var go = new GameObject("Game");
            var session = go.AddComponent<GameSession>();
            go.AddComponent<ShotDirector>();
            session.police = data.Police;
            session.cash = s.crews["player"].cash;
            session.spawn = player.transform.position; session.spawnYaw = player.transform.eulerAngles.y;
            float min = CityLayout.LineCoord(w, 0) - 30, max = CityLayout.LineCoord(w, 3) + 30;
            session.streetX = Enumerable.Range(0, 4).Select(i => WorldSpace.FromSource(CityLayout.LineCoord(w, i), 0).x).ToArray();
            session.streetZ = Enumerable.Range(0, 4).Select(i => WorldSpace.FromSource(0, CityLayout.LineCoord(w, i)).z).ToArray();
            session.spanZ = new Vector2(min + 4, max - 4);
            session.spanX = new Vector2(WorldSpace.FromSource(max, 0).x + 4, WorldSpace.FromSource(min, 0).x - 4);

            session.player = player.GetComponent<PlayerMotor>();
            var ph = player.AddComponent<Health>(); ph.max = ph.hp = 100;
            session.playerHealth = ph;
            player.AddComponent<PlayerCombat>();
            player.AddComponent<HitReaction>().animator = session.player.animator;

            // Unc: on the home block, 6 m in from the center, facing back toward the street (ensureContacts)
            var b0 = s.blocks[0]; var (cx, cz) = Simulation.BlockCenter(w, b0);
            var (ux, uz) = WorldSpace.FromSource(cx + 6, cz + 6);
            var unc = Character("Unc", new Vector3(ux, 0.3f, uz), WorldSpace.YawFromSource(Mathf.PI) * Mathf.Rad2Deg, Unc, false);
            Object.DestroyImmediate(unc.GetComponent<CharacterController>());

            var job = new GameObject("Job: Bag Back");
            var runner = job.AddComponent<MissionRunner>();
            runner.mission = data.Jobs.missions.First(m => m.title == "Bag Back");
            runner.contact = data.Jobs.contacts.First(c => c.name == runner.mission.by);
            runner.giver = unc.transform;
            runner.marker = Marker(job.transform);
            runner.bag = Bag(job.transform);

            // a few people on the sidewalks around the start
            var (bx, bz) = WorldSpace.FromSource(cx, cz);
            float edge = w.block / 2 + 1f;
            Ped(new Vector3(bx - edge, 0.3f, bz + 10), Vector3.forward, PedLooks[0]);
            Ped(new Vector3(bx - edge, 0.3f, bz - 14), Vector3.forward, PedLooks[1]);
            Ped(new Vector3(bx - 8, 0.3f, bz + edge), Vector3.right, PedLooks[2]);
            Ped(new Vector3(bx + 6, 0.3f, bz - edge), Vector3.right, PedLooks[3]);

            // the officers the dispatch copies (inactive in the scene), one per cop model, taken in turn
            session.copTemplates = CopLooks.Select((look, i) =>
            {
                var cop = Character($"Officer Template {i + 1}", new Vector3(i * 3, -50, 0), 0, look, true);
                var ch = cop.AddComponent<Health>(); ch.max = ch.hp = 80;
                cop.AddComponent<HitReaction>();
                cop.AddComponent<Cop>();
                cop.SetActive(false);
                return cop;
            }).ToArray();
            session.cruiserTemplate = VehicleBuilder.BuildPoliceTemplate(data).gameObject;

            Hud(runner);
        }

        static void Ped(Vector3 at, Vector3 along, Look look)
        {
            var p = Character("Pedestrian", at, Quaternion.LookRotation(along).eulerAngles.y, look, true);
            var h = p.AddComponent<Health>(); h.max = h.hp = 30;
            p.AddComponent<HitReaction>();
            var ped = p.AddComponent<Ped>(); ped.along = along;
        }

        // M5: the finished Meshy characters already on the box, converted by tools/convert_character.sh (Mixamo
        // rigs, Humanoid on import, so they share Player.controller and its CC0 UAL clips). Heights are the
        // ones the roster files were made at.
        public readonly struct Look
        {
            public readonly string dir, prefix; public readonly float height;
            public Look(string dir, string prefix, float height) { this.dir = dir; this.prefix = prefix; this.height = height; }
            public string Fbx => $"Assets/Art/Models/Characters/{dir}/{prefix}.fbx";
        }
        public static readonly Look Unc = new("Unc", "unc", 1.80f);
        public static readonly Look[] CopLooks = { new("Cops", "cop-m-1", 1.82f), new("Cops", "cop-m-2", 1.76f) };
        public static readonly Look[] PedLooks = { new("Peds", "ped-m-1", 1.78f), new("Peds", "ped-f-1", 1.66f), new("Peds", "ped-m-2", 1.72f), new("Peds", "ped-f-2", 1.70f) };

        public static GameObject Character(string name, Vector3 at, float yaw, Look look, bool body)
        {
            var root = new GameObject(name);
            root.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0, 0.92f, 0); cc.stepOffset = 0.35f;
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(look.Fbx);
            if (fbx == null) { Debug.LogError($"[GameBuilder] missing {look.Fbx}, using the player model"); fbx = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerBuilder.PlayerFbx); }
            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            model.name = "Model"; model.transform.SetParent(root.transform, false);
            var anim = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerBuilder.ControllerPath);
            anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            var mat = LookMaterial(look);
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()) { r.sharedMaterial = mat; r.updateWhenOffscreen = true; }
            // the files are authored at their height; only rescale one that came out of the converter far off it
            float h = ModelHeight(model);
            if (h > 0.1f && Mathf.Abs(h - look.height) > 0.1f * look.height) model.transform.localScale *= look.height / h;
            if (body) { var nb = root.AddComponent<NpcBody>(); nb.animator = anim; nb.model = model.transform; }
            return root;
        }

        public static float ModelHeight(GameObject model)
        {
            var rs = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (rs.Length == 0) return 0;
            var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
            return b.size.y;
        }

        static Material LookMaterial(Look look)
        {
            string dir = "Assets/Art/Materials/Characters"; System.IO.Directory.CreateDirectory(dir);
            string p = $"{dir}/{look.prefix}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
            string tex = $"Assets/Art/Models/Characters/{look.dir}/{look.prefix}";
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "_basecolor.jpg"));
            m.SetColor("_BaseColor", Color.white);
            var n = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "_normal.jpg");
            m.SetTexture("_BumpMap", n); if (n != null) m.EnableKeyword("_NORMALMAP"); else m.DisableKeyword("_NORMALMAP");
            m.SetFloat("_Smoothness", 0.3f); m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Transform Marker(Transform parent)
        {
            var m = GameObject.CreatePrimitive(PrimitiveType.Cylinder); m.name = "Marker";
            Object.DestroyImmediate(m.GetComponent<Collider>());
            m.transform.SetParent(parent); m.transform.localScale = new Vector3(2.2f, 3f, 2.2f);
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Marker.mat");
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(mat, "Assets/Art/Materials/Marker.mat"); }
            mat.SetFloat("_Surface", 1); mat.SetFloat("_Blend", 0);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetColor("_BaseColor", new Color(1f, 0.85f, 0.15f, 0.35f));
            EditorUtility.SetDirty(mat);
            var r = m.GetComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // the cylinder pivot is its middle; a child keeps the base on the ground
            var holder = new GameObject("Marker").transform; holder.SetParent(parent);
            m.transform.SetParent(holder); m.transform.localPosition = new Vector3(0, 3f, 0);
            return holder;
        }

        static Transform Bag(Transform parent)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube); b.name = "Bag";
            Object.DestroyImmediate(b.GetComponent<Collider>());
            b.transform.SetParent(parent); b.transform.localScale = new Vector3(0.55f, 0.32f, 0.36f);
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Bag.mat");
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, "Assets/Art/Materials/Bag.mat"); }
            mat.SetColor("_BaseColor", new Color(0.32f, 0.2f, 0.12f)); EditorUtility.SetDirty(mat);
            b.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return b.transform;
        }

        static void Hud(MissionRunner runner)
        {
            var canvas = GameObject.Find("HUD");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var hud = canvas.AddComponent<GameHud>(); hud.mission = runner;

            Text T(string name, Vector2 anchor, Vector2 pos, Vector2 size, int fs, TextAnchor align)
            {
                var r = PlayerBuilder.Rect(name, canvas.transform, anchor, anchor, pos, size); r.pivot = anchor;
                var t = r.gameObject.AddComponent<Text>(); t.font = font; t.fontSize = fs; t.alignment = align; t.color = Color.white; t.raycastTarget = false;
                var o = r.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, 0.8f); o.effectDistance = new Vector2(2, -2);
                return t;
            }
            hud.cash = T("Cash", new Vector2(0, 1), new Vector2(30, -24), new Vector2(400, 60), 44, TextAnchor.UpperLeft);
            hud.cash.color = new Color(0.55f, 0.95f, 0.55f);
            var ver = T("BuildLabel", new Vector2(0, 1), new Vector2(32, -76), new Vector2(400, 30), 20, TextAnchor.UpperLeft);
            ver.color = new Color(1, 1, 1, 0.7f); ver.text = BuildLabel.Fallback; ver.gameObject.AddComponent<BuildLabel>();
            hud.objective = T("Objective", new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(1100, 60), 34, TextAnchor.UpperCenter);
            hud.message = T("Message", new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(1400, 260), 36, TextAnchor.MiddleCenter);

            // five wanted stars, top right (diamonds: the built-in font has no star glyph)
            hud.stars = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var r = PlayerBuilder.Rect($"Star {i + 1}", canvas.transform, Vector2.one, Vector2.one, new Vector2(-40 - (4 - i) * 52, -44), new Vector2(34, 34));
                r.localRotation = Quaternion.Euler(0, 0, 45);
                var img = r.gameObject.AddComponent<Image>(); img.raycastTarget = false;
                hud.stars[i] = img;
            }
        }
    }
}
