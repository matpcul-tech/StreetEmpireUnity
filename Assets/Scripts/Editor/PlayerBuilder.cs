// M2: puts the player (Meshy player v2, Humanoid) into the Eastside scene with the Quaternius clips,
// the Cinemachine third person camera and the phone controls.
using System.IO;
using System.Linq;
using StreetEmpire.Player;
using StreetEmpire.Sim.Data;
using StreetEmpire.Sim;
using StreetEmpire.UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace StreetEmpire.EditorTools
{
    public static class PlayerBuilder
    {
        public const string PlayerFbx = "Assets/Art/Models/Characters/Player/player.fbx";
        const string ClipsFbx = "Assets/Art/Animations/ual-clips.fbx";
        public const string ControllerPath = "Assets/Art/Animations/Player.controller";

        public static GameObject Build(WorldFile w, Block start)
        {
            // spawn where createState() puts the player: east edge of the first block
            var (cx, cz) = Simulation.BlockCenter(w, start);
            var (x, z) = WorldSpace.FromSource(cx + w.block / 2 + 3, cz);
            var root = new GameObject("Player") { tag = "Player" };
            root.transform.position = new Vector3(x, 0.05f, z);
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.32f; cc.center = new Vector3(0, 0.92f, 0); cc.stepOffset = 0.35f; cc.slopeLimit = 50f;

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerFbx);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            model.name = "Model"; model.transform.SetParent(root.transform, false);
            var anim = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            anim.runtimeAnimatorController = Controller();
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var mat = PlayerMaterial();
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()) { r.sharedMaterial = mat; r.updateWhenOffscreen = true; }

            var motor = root.AddComponent<PlayerMotor>();
            motor.animator = anim;

            var target = new GameObject("CameraTarget").transform;
            target.SetParent(root.transform, false); target.localPosition = new Vector3(0, 1.55f, 0);
            var follow = Camera(target, root.transform.eulerAngles.y);
            var user = root.AddComponent<VehicleUser>();
            user.followCamera = follow; user.footCameraTarget = target;
            Hud();
            return root;
        }

        static CinemachineCamera Camera(Transform target, float yaw)
        {
            var main = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = main.AddComponent<UnityEngine.Camera>();
            cam.nearClipPlane = 0.2f; cam.farClipPlane = 600f; cam.clearFlags = CameraClearFlags.Skybox;
            main.AddComponent<AudioListener>();
            main.AddComponent<CinemachineBrain>();

            var vcam = new GameObject("Follow Camera");
            var cm = vcam.AddComponent<CinemachineCamera>();
            cm.Follow = target; cm.LookAt = target;
            var lens = cm.Lens; lens.FieldOfView = 58f; lens.NearClipPlane = 0.2f; lens.FarClipPlane = 600f; cm.Lens = lens;
            var orbit = vcam.AddComponent<CinemachineOrbitalFollow>();
            orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbit.Radius = 5.2f;
            orbit.VerticalAxis.Range = new Vector2(-8f, 60f); orbit.VerticalAxis.Value = 14f;
            orbit.HorizontalAxis.Value = yaw;
            vcam.AddComponent<CinemachineRotationComposer>();
            var rig = vcam.AddComponent<CameraRig>(); rig.orbit = orbit;
            return cm;
        }

        static RuntimeAnimatorController Controller()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ClipsFbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);
            AnimationClip C(string n) { if (clips.TryGetValue(n, out var c)) return c; Debug.LogError($"[PlayerBuilder] missing clip {n}; have {string.Join(", ", clips.Keys)}"); return null; }
            AssetDatabase.DeleteAsset(ControllerPath);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            var sm = ac.layers[0].stateMachine;
            var loco = ac.CreateBlendTreeInController("Locomotion", out var tree, 0);
            tree.blendType = BlendTreeType.Simple1D; tree.blendParameter = "Speed"; tree.useAutomaticThresholds = false;
            tree.AddChild(C("Idle_Loop"), 0f); tree.AddChild(C("Walk_Loop"), 1.8f); tree.AddChild(C("Jog_Fwd_Loop"), 5f); tree.AddChild(C("Sprint_Loop"), 9f);
            sm.defaultState = loco;
            var jumpStart = sm.AddState("Jump Start"); jumpStart.motion = C("Jump_Start");
            var air = sm.AddState("In Air"); air.motion = C("Jump_Loop");
            var t1 = sm.AddAnyStateTransition(jumpStart); t1.AddCondition(AnimatorConditionMode.If, 0, "Jump"); t1.duration = 0.08f; t1.canTransitionToSelf = false;
            var t2 = jumpStart.AddTransition(air); t2.hasExitTime = true; t2.exitTime = 0.85f; t2.duration = 0.1f;
            foreach (var from in new[] { jumpStart, air })
            {
                var land = from.AddTransition(loco); land.AddCondition(AnimatorConditionMode.If, 0, "Grounded"); land.hasExitTime = from == jumpStart; land.exitTime = 0.5f; land.duration = 0.12f;
            }
            AssetDatabase.SaveAssets();
            return ac;
        }

        public static Material PlayerMaterial()
        {
            const string p = "Assets/Art/Materials/Player.mat";
            Directory.CreateDirectory("Assets/Art/Materials");
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Models/Characters/Player/player_basecolor.jpg"));
            m.SetColor("_BaseColor", Color.white);
            var n = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Models/Characters/Player/player_normal.jpg");
            if (n != null) { m.SetTexture("_BumpMap", n); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Smoothness", 0.3f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Hud()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>();

            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.6f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var hud = canvasGo.AddComponent<HudController>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var touch = Rect("TouchControls", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // right side drag area to turn the camera, behind the buttons
            var look = Rect("LookArea", touch, new Vector2(0.45f, 0), Vector2.one, Vector2.zero, Vector2.zero);
            var li = look.gameObject.AddComponent<Image>(); li.color = new Color(1, 1, 1, 0.001f);
            look.gameObject.AddComponent<TouchLookArea>();

            // left stick
            var stickBase = Rect("StickBase", touch, Vector2.zero, Vector2.zero, new Vector2(250, 250), new Vector2(300, 300));
            var bi = stickBase.gameObject.AddComponent<Image>(); bi.sprite = knob; bi.color = new Color(1, 1, 1, 0.18f);
            var stick = Rect("Stick", stickBase, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 130));
            var si = stick.gameObject.AddComponent<Image>(); si.sprite = knob; si.color = new Color(1f, 0.54f, 0.1f, 0.75f);
            var os = stick.gameObject.AddComponent<OnScreenStick>(); os.controlPath = "<Gamepad>/leftStick"; os.movementRange = 95;

            Button(touch, "Jump", "<Gamepad>/buttonSouth", new Vector2(-170, 150), 150, font, knob);
            Button(touch, "Sprint", "<Gamepad>/leftShoulder", new Vector2(-350, 120), 130, font, knob);
            Button(touch, "Use", "<Gamepad>/buttonNorth", new Vector2(-170, 330), 130, font, knob);
            Button(touch, "Hit", "<Gamepad>/buttonWest", new Vector2(-340, 300), 120, font, knob);

            var hints = Rect("KeyHints", canvasGo.transform, Vector2.zero, Vector2.zero, new Vector2(24, 20), new Vector2(900, 40));
            hints.pivot = Vector2.zero;
            var ht = hints.gameObject.AddComponent<Text>(); ht.font = font; ht.fontSize = 22; ht.color = new Color(1, 1, 1, 0.8f);
            ht.text = "WASD move   Mouse look (click to lock, Esc to free)   Shift sprint   Space jump   E use   J hit";

            hud.touchControls = touch.gameObject; hud.keyHints = hints.gameObject;
        }

        static void Button(RectTransform parent, string label, string path, Vector2 fromBottomRight, float size, Font font, Sprite sprite)
        {
            var b = Rect(label, parent, Vector2.right, Vector2.right, fromBottomRight, new Vector2(size, size));
            var img = b.gameObject.AddComponent<Image>(); img.sprite = sprite; img.color = new Color(0.1f, 0.1f, 0.1f, 0.55f);
            var ob = b.gameObject.AddComponent<OnScreenButton>(); ob.controlPath = path;
            var t = Rect("Label", b, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Text>();
            t.font = font; t.text = label.ToUpperInvariant(); t.fontSize = 26; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); var r = (RectTransform)go.transform;
            r.SetParent(parent, false); r.anchorMin = aMin; r.anchorMax = aMax; r.anchoredPosition = pos; r.sizeDelta = size;
            return r;
        }
    }
}
