using UnityEngine;
using UnityEngine.InputSystem;

namespace StreetEmpire.UI
{
    /// <summary>Shows the on-screen stick and buttons on touch devices, and the key hints elsewhere.</summary>
    public class HudController : MonoBehaviour
    {
        public GameObject touchControls, keyHints;
        public bool forceTouch;

        void Update()
        {
            bool touch = forceTouch || Application.isMobilePlatform || Touchscreen.current != null;
            if (touchControls && touchControls.activeSelf != touch) touchControls.SetActive(touch);
            if (keyHints && keyHints.activeSelf == touch) keyHints.SetActive(!touch);
        }
    }
}
