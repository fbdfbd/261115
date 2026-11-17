using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WorkGame
{
    public sealed class CareDialogueClickArea : MonoBehaviour, IPointerClickHandler
    {
        public event Action Clicked;
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) Clicked?.Invoke();
        }
    }
}
