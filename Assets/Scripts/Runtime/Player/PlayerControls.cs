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

        public readonly InputAction move, look, jump, sprint, use, attack;

        PlayerControls()
        {
            move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");

            look = new InputAction("Look", InputActionType.Value, "<Gamepad>/rightStick", expectedControlType: "Vector2");
            jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space"); jump.AddBinding("<Gamepad>/buttonSouth");
            sprint = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift"); sprint.AddBinding("<Gamepad>/leftShoulder"); sprint.AddBinding("<Gamepad>/leftStickPress");
            use = new InputAction("Use", InputActionType.Button, "<Keyboard>/e"); use.AddBinding("<Keyboard>/f"); use.AddBinding("<Gamepad>/buttonNorth");
            attack = new InputAction("Attack", InputActionType.Button, "<Gamepad>/buttonWest"); attack.AddBinding("<Keyboard>/j");
            foreach (var a in new[] { move, look, jump, sprint, use, attack }) a.Enable();
        }

        public Vector2 Move => Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
        public Vector2 LookStick => look.ReadValue<Vector2>();
        public bool Sprint => sprint.IsPressed();
        public bool JumpPressed => jump.WasPressedThisFrame();
        public bool UsePressed => use.WasPressedThisFrame();
    }
}
