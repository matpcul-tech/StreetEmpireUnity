// All player input in one place, built in code: keyboard and mouse, gamepad, and the on-screen phone
// controls (OnScreenStick and OnScreenButton feed a virtual gamepad, so they use the gamepad bindings).
using UnityEngine;
using UnityEngine.InputSystem;

namespace StreetEmpire.Player
{
    public class PlayerControls
    {
        static PlayerControls instance;
        public static PlayerControls Instance => instance ??= new PlayerControls();

        public readonly InputAction move, look, jump, sprint, use, attack, click, gas, reverse;

        PlayerControls()
        {
            move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");

            look = new InputAction("Look", InputActionType.Value, "<Gamepad>/rightStick", expectedControlType: "Vector2");
            jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space"); jump.AddBinding("<Gamepad>/buttonSouth");
            sprint = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift"); sprint.AddBinding("<Gamepad>/leftShoulder"); sprint.AddBinding("<Gamepad>/leftStickPress");
                        // the web keys: F (or a click while the mouse is locked) hits, E or Enter uses; J stays as a spare hit key
            use = new InputAction("Use", InputActionType.Button, "<Keyboard>/e"); use.AddBinding("<Keyboard>/enter"); use.AddBinding("<Gamepad>/buttonNorth");
            attack = new InputAction("Attack", InputActionType.Button, "<Gamepad>/buttonWest"); attack.AddBinding("<Keyboard>/f"); attack.AddBinding("<Keyboard>/j");
            click = new InputAction("Click", InputActionType.Button, "<Mouse>/leftButton");
            // driving pedals: the phone's GAS and REV buttons, and the triggers on a real gamepad
            gas = new InputAction("Gas", InputActionType.Button, "<Gamepad>/rightTrigger");
            reverse = new InputAction("Reverse", InputActionType.Button, "<Gamepad>/leftTrigger");
            foreach (var a in new[] { move, look, jump, sprint, use, attack, click, gas, reverse }) a.Enable();
        }

        public Vector2 Move => Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
        public Vector2 LookStick => look.ReadValue<Vector2>();
        public bool Sprint => sprint.IsPressed();
        public bool Gas => gas.IsPressed();
        public bool Reverse => reverse.IsPressed();
        /// <summary>True while the phone controls are up (HudController sets it). The stick then only steers a car.</summary>
        public static bool TouchPedals;
        public bool JumpPressed => jump.WasPressedThisFrame();
        public bool UsePressed => use.WasPressedThisFrame();
        public bool AttackPressed => attack.WasPressedThisFrame();
        public bool ClickPressed => click.WasPressedThisFrame();
    }
}
