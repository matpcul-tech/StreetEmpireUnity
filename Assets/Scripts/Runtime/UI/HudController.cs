using StreetEmpire.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StreetEmpire.UI
{
    /// <summary>Shows the on-screen stick and buttons on touch devices, and the key hints elsewhere.
    /// In a car the foot buttons (jump, sprint, hit) give way to the pedals (gas, reverse, drift).</summary>
    public class HudController : MonoBehaviour
    {
        public GameObject touchControls, keyHints, footButtons, driveButtons;
        public bool forceTouch;
        VehicleUser user;

        void Update()
        {
            bool touch = forceTouch || Application.isMobilePlatform || Touchscreen.current != null;
            if (touchControls && touchControls.activeSelf != touch) touchControls.SetActive(touch);
            if (keyHints && keyHints.activeSelf == touch) keyHints.SetActive(!touch);
            PlayerControls.TouchPedals = touch && driveButtons != null;
            if (user == null) user = FindFirstObjectByType<VehicleUser>();
            bool driving = user != null && user.Car != null;
            if (footButtons && footButtons.activeSelf == driving) footButtons.SetActive(!driving);
            if (driveButtons && driveButtons.activeSelf != driving) driveButtons.SetActive(driving);
        }
    }
}
