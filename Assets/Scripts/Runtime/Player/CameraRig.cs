// Third person orbit: drives the Cinemachine orbital follow from mouse (click to lock the cursor),
// gamepad right stick, or a drag on the right side of a phone screen. Feels like the web game's camera
// (main.js): 0.0026 rad a mouse pixel, over the right shoulder on foot, centered behind a car, and pulled in
// when a building wall is behind (insideBuilding in the web), never by a pole or a palm.
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace StreetEmpire.Player
{
    public class CameraRig : MonoBehaviour
    {
        public CinemachineOrbitalFollow orbit;
        public float mouseSensitivity = 0.149f, stickSpeed = 150f, touchSensitivity = 0.22f;
        public CinemachineRotationComposer composer;
        public float radius = 5.2f, shoulder = -0.05f;   // orbit radius wanted, and the on foot screen offset
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
            // the lowest the camera may swing depends on how far back it sits: 6 degrees below level is fine at
            // 5 m on foot, and under the road at the 8 to 17 m it keeps behind a car or a bus
            var cmc = orbit.GetComponent<CinemachineCamera>(); var follow = cmc != null ? cmc.Follow : null;
            float low = follow != null ? Mathf.Max(range.x, FloorAngle(follow.position.y, orbit.Radius)) : range.x;
            orbit.VerticalAxis.Value = Mathf.Clamp(orbit.VerticalAxis.Value - look.y, Mathf.Min(low, range.y), range.y);

            if (composer != null)
            {
                var c = composer.Composition; c.ScreenPosition.x = Mathf.Lerp(c.ScreenPosition.x, alignTo != null ? 0f : shoulder, 1f - Mathf.Exp(-6f * Time.deltaTime));
                composer.Composition = c;
            }
            orbit.Radius = Mathf.Lerp(orbit.Radius, Clearance(), 1f - Mathf.Exp(-12f * Time.deltaTime));
        }

        /// <summary>How high the camera stays above the street (the town is flat, streets at height 0).</summary>
        public const float FloorHeight = 0.7f;

        /// <summary>The lowest orbit angle (degrees, negative is below the target) that keeps a camera this far from
        /// a target at this height above FloorHeight.</summary>
        public static float FloorAngle(float targetHeight, float orbitRadius)
            => Mathf.Asin(Mathf.Clamp((FloorHeight - targetHeight) / Mathf.Max(orbitRadius, 0.5f), -1f, 1f)) * Mathf.Rad2Deg;

        static readonly RaycastHit[] hits = new RaycastHit[16];

        /// <summary>How far back the camera can sit before a building wall gets between it and the player.</summary>
        float Clearance()
        {
            var cm = orbit.GetComponent<CinemachineCamera>(); var t = cm != null ? cm.Follow : null;
            if (t == null) return radius;
            var dir = Quaternion.Euler(orbit.VerticalAxis.Value, orbit.HorizontalAxis.Value, 0) * Vector3.back;
            int n = Physics.SphereCastNonAlloc(t.position, 0.25f, dir, hits, radius, ~0, QueryTriggerInteraction.Ignore);
            float best = radius;
            for (int i = 0; i < n; i++)
                if (hits[i].distance > 0 && hits[i].collider.GetComponent<StreetEmpire.City.BuildingMarker>() != null) best = Mathf.Min(best, hits[i].distance);
            return Mathf.Max(1.5f, best);
        }
    }
}
