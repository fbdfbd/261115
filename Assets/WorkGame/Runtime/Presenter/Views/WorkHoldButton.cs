using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WorkGame
{
    [RequireComponent(typeof(Button))]
    public sealed class WorkHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerExitHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image fill;
        [SerializeField, Min(0.1f)] private float holdSeconds = 0.5f;
        private Tween holdTween;
        private ScrollRect scroll;
        private bool allowed = true;
        private bool forwardingDrag;
        private int pointerId = int.MinValue;
        private float progress;
        public event Action Pressed;
        public event Action Completed;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            scroll = GetComponentInParent<ScrollRect>();
            // Fill type and direction belong to the prefab's Image settings.
            if (fill != null) fill.raycastTarget = false;
            SetProgress(0);
        }

        public void SetAllowed(bool value)
        {
            allowed = value;
            if (!allowed) CancelHold();
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || pointerId != int.MinValue) return;
            if (button == null) button = GetComponent<Button>();
            if (!button.IsActive() || !button.IsInteractable()) return;
            Pressed?.Invoke();
            if (!allowed || !isActiveAndEnabled || !button.IsInteractable()) return;
            pointerId = data.pointerId;
            holdTween = DOTween.To(() => progress, SetProgress, 1, Mathf.Max(0.1f, holdSeconds))
                .SetEase(Ease.Linear).SetUpdate(true)
                .OnUpdate(() => { if (!button.IsInteractable()) CancelHold(); })
                .OnComplete(() => {
                    holdTween = null;
                    SetProgress(0);
                    Completed?.Invoke();
                });
        }

        public void OnPointerUp(PointerEventData data) { if (data.pointerId == pointerId) CancelHold(); }
        public void OnPointerExit(PointerEventData data) { if (data.pointerId == pointerId) CancelHold(); }

        public void OnInitializePotentialDrag(PointerEventData data)
        {
            if (scroll == null) scroll = GetComponentInParent<ScrollRect>();
            if (scroll != null) scroll.OnInitializePotentialDrag(data);
        }

        public void OnBeginDrag(PointerEventData data)
        {
            CancelHold();
            forwardingDrag = scroll != null && scroll.isActiveAndEnabled;
            if (forwardingDrag) scroll.OnBeginDrag(data);
        }
        public void OnDrag(PointerEventData data) { if (forwardingDrag) scroll.OnDrag(data); }
        public void OnEndDrag(PointerEventData data)
        {
            if (forwardingDrag && scroll != null) scroll.OnEndDrag(data);
            forwardingDrag = false;
        }

        public void CancelHold()
        {
            holdTween?.Kill();
            holdTween = null;
            pointerId = int.MinValue;
            SetProgress(0);
        }

        private void SetProgress(float value)
        {
            progress = value;
            if (fill != null) fill.fillAmount = value;
        }
        private void OnDisable() { CancelHold(); forwardingDrag = false; }
    }
}
