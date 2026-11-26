// The move stick's touch zone. The stick itself (OnScreenStick on the orange knob) only answered a touch that
// landed on the knob, 130 px wide, so a thumb that came down on the ring or beside it did nothing. This zone covers
// the lower left of the screen: wherever the thumb lands in it the stick appears under it and follows the drag,
// and it goes home on release. One finger owns it at a time.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;

namespace StreetEmpire.UI
{
    public class StickZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public OnScreenStick stick;
        public RectTransform stickBase;
        /// <summary>True while a finger is on the stick.</summary>
        public bool Held { get; private set; }
        Vector3 home; int finger;

        public void OnPointerDown(PointerEventData e)
        {
            if (Held || stick == null || stickBase == null) return;
            Held = true; finger = e.pointerId; home = stickBase.position;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out var at)) stickBase.position = at;
            stick.OnPointerDown(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (Held && e.pointerId == finger) stick.OnDrag(e);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!Held || e.pointerId != finger) return;
            stick.OnPointerUp(e); Release();
        }

        // the HUD hides the touch controls in menus and cut scenes: never leave the stick held or off its home
        void OnDisable() { if (Held) Release(); }

        void Release() { Held = false; if (stickBase != null) stickBase.position = home; }
    }
}
