using StreetEmpire.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace StreetEmpire.UI
{
    /// <summary>Drag anywhere on this (the right side of the screen) to turn the camera.</summary>
    public class TouchLookArea : MonoBehaviour, IDragHandler
    {
        public void OnDrag(PointerEventData e) => CameraRig.AddTouchLook(e.delta);
    }
}
