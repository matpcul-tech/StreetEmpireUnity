// Third person orbit: drives the Cinemachine orbital follow from mouse (click to lock the cursor),
// gamepad right stick, or a drag on the right side of a phone screen.
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace StreetEmpire.Player
{
    public class CameraRig : MonoBehaviour
    {
        public CinemachineOrbitalFollow orbit;
        public float mouseSensitivity = 0.12f, stickSpeed = 150f, touchSensitivity = 0.22f;
        public Transform alignTo;           // while driving: swing back behind the car when the player is not looking around
        public float alignDelay = 1.2f, alignSpeed = 2.5f;
        static Vector2 touchDelta;
        float idle;

        public static void AddTouchLook(Vector2 pixels) => touchDelta += pixels;

        void Update()
        {
            if (orbit == null) return;
            var look = PlayerControls.Instance.LookStick * stickSpeed * Time.deltaTime;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (mouse.leftButton.wasPressedThisFrame && !overUi && Touchscreen.current == null) Cursor.lockState = CursorLockMode.Locked;
                if (Cursor.lockState == CursorLockMode.Locked || mouse.rightButton.isPressed) look += mouse.delta.ReadValue() * mouseSensitivity;
            }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Cursor.lockState = CursorLockMode.None;
            look += touchDelta * touchSensitivity; touchDelta = Vector2.zero;

            idle = look.sqrMagnitude > 1e-4f ? 0f : idle + Time.deltaTime;
            float h = orbit.HorizontalAxis.Value + look.x;
            if (alignTo != null && idle > alignDelay) h = Mathf.LerpAngle(h, alignTo.eulerAngles.y, 1f - Mathf.Exp(-alignSpeed * Time.deltaTime));
            orbit.HorizontalAxis.Value = Mathf.DeltaAngle(0f, h);
            var range = orbit.VerticalAxis.Range;
            orbit.VerticalAxis.Value = Mathf.Clamp(orbit.VerticalAxis.Value - look.y, range.x, range.y);
        }
    }
}
