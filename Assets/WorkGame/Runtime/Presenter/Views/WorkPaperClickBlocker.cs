using UnityEngine;
using UnityEngine.EventSystems;

namespace WorkGame
{
    // The black background is the parent Button. Consume paper-area presses before they reach it.
    public sealed class WorkPaperClickBlocker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public void OnPointerDown(PointerEventData data) { }
        public void OnPointerUp(PointerEventData data) { }
        public void OnPointerClick(PointerEventData data) { }
    }
}
