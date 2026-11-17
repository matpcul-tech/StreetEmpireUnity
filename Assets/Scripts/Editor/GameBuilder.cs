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
            // the loop runs down the middle of the 2 m sidewalk ring (block/2 to block/2 + 2), two each way round
            var bc = new Vector3(bx, 0, bz);
            Ped(new Vector3(bx - edge, 0.3f, bz + 10), Vector3.forward, PedLooks[0], bc, edge, -1);
            Ped(new Vector3(bx - edge, 0.3f, bz - 14), Vector3.forward, PedLooks[1], bc, edge, 1);
            Ped(new Vector3(bx - 8, 0.3f, bz + edge), Vector3.right, PedLooks[2], bc, edge, 1);
            Ped(new Vector3(bx + 6, 0.3f, bz - edge), Vector3.right, PedLooks[3], bc, edge, -1);

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

            // the opening scuffle (state.js openingScuffle): two plain mugger bodies and one with a bat, inactive
            var opening = go.AddComponent<MuggerOpening>(); opening.job = runner;
            opening.templates = new[] { PedLooks[2], PedLooks[0], PedLooks[0] }.Select((look, i) =>
            {
                var m = Character($"Mugger Template {i + 1}", new Vector3(i * 3, -60, 0), 0, look, true);
                var h = m.AddComponent<Health>(); h.max = h.hp = 50;
                m.AddComponent<HitReaction>();
                m.AddComponent<Brawler>();
                if (i == 2) HoldBat(m);
                m.SetActive(false);
                return m;
            }).ToArray();

            var hud = Hud(runner, opening);
            var radar = Radar(hud.transform, unc.transform, runner);
            float lo = Mathf.Min(WorldSpace.FromSource(min, 0).x, WorldSpace.FromSource(max, 0).x) - 120f, size = max - min + 240f;
            radar.worldMin = new Vector2(lo, min - 120f); radar.worldSize = new Vector2(size, size);
            radar.streetX = session.streetX; radar.streetZ = session.streetZ; radar.spanX = session.spanX; radar.spanZ = session.spanZ;
            radar.blockSize = w.block;
            radar.blocks = s.blocks.Select(b => { var (bx2, bz2) = Simulation.BlockCenter(w, b); var (ux2, uz2) = WorldSpace.FromSource(bx2, bz2); return new Vector3(ux2, 0, uz2); }).ToArray();
            radar.yours = s.blocks.Select(b => b.owner == "player").ToArray();
            hud.turfOwned = s.blocks.Count(b => b.owner == "player"); hud.turfTotal = s.blocks.Count;
        }

        /// <summary>The bat (Models/Props/bat.glb, through Sovereign Asset) in the right fist, barrel forward in the bind pose.</summary>
        static void HoldBat(GameObject who)
        {
            var anim = who.GetComponentInChildren<Animator>();
            var hand = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
            var bat = PropBuilder.Loose("bat"); if (bat == null) return;
            bat.name = "Bat";
            if (hand == null) { bat.transform.SetParent(who.transform, false); bat.transform.localPosition = new Vector3(0.3f, 1f, 0.2f); return; }
            // the knob sits just under the fist, the barrel points ahead and a little up
            var dir = (who.transform.forward + who.transform.up * 0.25f).normalized;
            bat.transform.SetPositionAndRotation(hand.position + who.transform.up * -0.02f - dir * 0.10f + who.transform.forward * 0.04f,
                Quaternion.FromToRotation(Vector3.up, dir));
            bat.transform.SetParent(hand, true);
            foreach (var r in bat.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        static Minimap Radar(Transform canvas, Transform unc, MissionRunner job)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // a rim behind the round mask, then the turning map inside it
            var rim = PlayerBuilder.Rect("Radar", canvas, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(300, 300)); rim.pivot = new Vector2(0, 1);
            var rimImg = rim.gameObject.AddComponent<Image>(); rimImg.color = new Color(0, 0, 0, 0.55f); rimImg.raycastTarget = false;
            var frame = PlayerBuilder.Rect("Radar Mask", rim, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(288, 288));
            var maskImg = frame.gameObject.AddComponent<Image>(); maskImg.raycastTarget = false;
            frame.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var map = PlayerBuilder.Rect("Map", frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(288, 288));
            var raw = map.gameObject.AddComponent<RawImage>(); raw.raycastTarget = false;
            var arrow = PlayerBuilder.Rect("Player Arrow", frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 30)).gameObject.AddComponent<Image>();
            arrow.raycastTarget = false; arrow.gameObject.AddComponent<Outline>().effectColor = Color.black;
            Text Label(string name, string text, int fs, Color c)
            {
                var r = PlayerBuilder.Rect(name, frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
                var t = r.gameObject.AddComponent<Text>(); t.font = font; t.fontSize = fs; t.fontStyle = FontStyle.Bold; t.alignment = TextAnchor.MiddleCenter;
                t.color = c; t.text = text; t.raycastTarget = false;
                var o = t.gameObject.AddComponent<Outline>(); o.effectColor = Color.black; o.effectDistance = new Vector2(2, -2);
                return t;
            }
            var north = Label("North", "N", 28, Color.white);
            var letter = Label("Unc Letter", "U", 32, new Color32(0xff, 0xcc, 0x33, 255));
            var mm = rim.gameObject.AddComponent<Minimap>();
            mm.frame = frame; mm.map = map; mm.mapImage = raw; mm.arrow = arrow; mm.frameImage = maskImg; mm.north = north; mm.uncLetter = letter;
            mm.unc = unc; mm.job = job;
            return mm;
        }

        static void Ped(Vector3 at, Vector3 along, Look look, Vector3 blockCenter, float half, int lap)
        {
            var p = Character("Pedestrian", at, Quaternion.LookRotation(along).eulerAngles.y, look, true);
            var h = p.AddComponent<Health>(); h.max = h.hp = 30;
            p.AddComponent<HitReaction>();
            var ped = p.AddComponent<Ped>(); ped.along = along;
            ped.blockCenter = blockCenter; ped.half = half; ped.lap = lap;
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
            // the controller rests its capsule one skin width above the ground: put the capsule bottom that far
            // above the root so the root (and the soles, below) sit on the sidewalk, not 6 cm over it
            cc.height = 1.8f; cc.radius = 0.3f; cc.skinWidth = 0.03f; cc.center = new Vector3(0, 0.9f + cc.skinWidth, 0); cc.stepOffset = 0.35f;
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
            // no shift by the bind pose mesh: Humanoid places the hips from the clip and the avatar's own leg
            // length, so the soles meet the root as they do for the player (a shift by the old bent-leg bind
            // pose sank the feet and bent the knees)
            if (body)
            {
                var nb = root.AddComponent<NpcBody>(); nb.animator = anim; nb.model = model.transform;
                nb.restLean = RestLean(anim, root.transform);
            }
            return root;
        }

        public static float ModelBottom(GameObject model)
        {
            var rs = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (rs.Length == 0) return model.transform.position.y;
            var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
            return b.min.y;
        }

        /// <summary>Forward lean of the rest pose, hips to neck, in degrees (positive leans forward).</summary>
        public static float RestLean(Animator anim, Transform root)
        {
            if (anim == null || !anim.isHuman) return 0;
            var hips = anim.GetBoneTransform(HumanBodyBones.Hips); var neck = anim.GetBoneTransform(HumanBodyBones.Neck);
            if (hips == null || neck == null) return 0;
            var v = Vector3.ProjectOnPlane(neck.position - hips.position, root.right);
            return Vector3.SignedAngle(Vector3.up, v, root.right);
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

        static GameHud Hud(MissionRunner runner, MuggerOpening opening)
        {
            var canvas = GameObject.Find("HUD");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var hud = canvas.AddComponent<GameHud>(); hud.mission = runner; hud.opening = opening;

            Text T(string name, Vector2 anchor, Vector2 pos, Vector2 size, int fs, TextAnchor align)
            {
                var r = PlayerBuilder.Rect(name, canvas.transform, anchor, anchor, pos, size); r.pivot = anchor;
                var t = r.gameObject.AddComponent<Text>(); t.font = font; t.fontSize = fs; t.alignment = align; t.color = Color.white; t.raycastTarget = false;
                var o = t.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, 0.85f); o.effectDistance = new Vector2(2, -2);
                return t;
            }
            // the web #hud column, top right, at 1.5x its 1280 px sizes (the canvas is 1920 wide)
            var right = Vector2.one;
            hud.clock = T("Clock", right, new Vector2(-30, -14), new Vector2(300, 40), 33, TextAnchor.UpperRight); hud.clock.color = new Color32(0xee, 0xee, 0xee, 255);
            hud.weapon = T("Weapon", right, new Vector2(-30, -52), new Vector2(300, 30), 22, TextAnchor.UpperRight); hud.weapon.color = new Color32(0xdd, 0xdd, 0xdd, 255);
            hud.cash = T("Cash", right, new Vector2(-30, -78), new Vector2(400, 52), 45, TextAnchor.UpperRight); hud.cash.color = new Color32(0xf4, 0xe6, 0xc4, 255);
            hud.heatLabel = T("Heat", right, new Vector2(-30 - 5 * 24 - 8, -136), new Vector2(120, 28), 20, TextAnchor.UpperRight);
            hud.heatLabel.text = "HEAT";
            hud.stars = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var r = PlayerBuilder.Rect($"Heat Bar {i + 1}", canvas.transform, right, right, new Vector2(-30 - (4 - i) * 24, -132), new Vector2(28, 30));
                r.pivot = right;
                var img = r.gameObject.AddComponent<Image>(); img.raycastTarget = false; img.sprite = HudArt.Bar(false);
                hud.stars[i] = img;
            }
            var hp = PlayerBuilder.Rect("Health", canvas.transform, right, right, new Vector2(-30, -172), new Vector2(225, 15)); hp.pivot = right;
            var hpBack = hp.gameObject.AddComponent<Image>(); hpBack.color = new Color32(0x33, 0, 0, 255); hpBack.raycastTarget = false;
            hp.gameObject.AddComponent<Outline>().effectColor = Color.black;
            var fill = PlayerBuilder.Rect("Fill", hp, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); fill.pivot = Vector2.zero;
            hud.hpFill = fill.gameObject.AddComponent<Image>(); hud.hpFill.color = new Color32(0xee, 0x33, 0x33, 255); hud.hpFill.raycastTarget = false;
            hud.sub = T("Rep and Turf", right, new Vector2(-30, -194), new Vector2(400, 30), 21, TextAnchor.UpperRight); hud.sub.color = new Color32(0xcc, 0xcc, 0xcc, 255);

            // under the radar: the job objective and the onboarding box (rgba(10,8,6,.62), 3 px #ffcc33 left edge)
            hud.objective = T("Objective", new Vector2(0, 1), new Vector2(26, -336), new Vector2(760, 44), 27, TextAnchor.UpperLeft);
            var box = PlayerBuilder.Rect("Onboarding", canvas.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -384), new Vector2(560, 50)); box.pivot = new Vector2(0, 1);
            var bg = box.gameObject.AddComponent<Image>(); bg.color = new Color(10 / 255f, 8 / 255f, 6 / 255f, 0.62f); bg.raycastTarget = false;
            var edge = PlayerBuilder.Rect("Edge", box, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(5, 0)); edge.pivot = Vector2.zero;
            var ei = edge.gameObject.AddComponent<Image>(); ei.color = new Color32(0xff, 0xcc, 0x33, 255); ei.raycastTarget = false;
            var tr = PlayerBuilder.Rect("Text", box, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-24, 0));
            hud.onboarding = tr.gameObject.AddComponent<Text>(); hud.onboarding.font = font; hud.onboarding.fontSize = 23; hud.onboarding.supportRichText = true;
            hud.onboarding.alignment = TextAnchor.MiddleLeft; hud.onboarding.color = Color.white; hud.onboarding.raycastTarget = false;
            hud.onboardingBox = box.gameObject; box.gameObject.SetActive(false);

            hud.message = T("Message", new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(1400, 260), 36, TextAnchor.MiddleCenter);
            // the build label moves to the bottom right, out of the radar's way
            var ver = T("BuildLabel", new Vector2(1, 0), new Vector2(-24, 16), new Vector2(500, 30), 18, TextAnchor.LowerRight);
            ver.color = new Color(1, 1, 1, 0.55f); ver.text = BuildLabel.Fallback; ver.gameObject.AddComponent<BuildLabel>();
            return hud;
        }
    }
}
