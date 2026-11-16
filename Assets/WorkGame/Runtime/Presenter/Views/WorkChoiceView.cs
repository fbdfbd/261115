using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WorkGame
{
    [RequireComponent(typeof(Button))]
    public sealed class WorkChoiceView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image fill;
        private int index;
        private bool holding;

        public event Action<int> HoldStarted;
        public event Action HoldStopped;

        public void Show(int choiceIndex, string name)
        {
            CancelHold();
            index = choiceIndex;
            if (button == null) button = GetComponent<Button>();
            if (nameText != null) nameText.text = name;
            if (fill != null)
            {
                fill.raycastTarget = false;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
            SetProgress(0);
        }

        public void SetProgress(float progress)
        {
            if (fill != null) fill.fillAmount = Mathf.Clamp01(progress);
        }

        public void SetInput(bool enabled)
        {
            if (button == null) button = GetComponent<Button>();
            button.interactable = enabled;
            if (!enabled) CancelHold();
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || holding) return;
            if (button == null) button = GetComponent<Button>();
            if (!button.IsInteractable()) return;
            holding = true;
            HoldStarted?.Invoke(index);
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) CancelHold();
        }

        public void OnPointerExit(PointerEventData data) => CancelHold();
        private void OnDisable() => CancelHold();

        public void CancelHold()
        {
            SetProgress(0);
            if (!holding) return;
            holding = false;
            HoldStopped?.Invoke();
        }
    }
}
