using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareEndingCardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text nameText, descriptionText;
        private string endingId;
        public event Action<string> Clicked;
        private void OnEnable() { if (button != null) button.onClick.AddListener(Click); }
        private void OnDisable() { if (button != null) button.onClick.RemoveListener(Click); }
        private void Click() => Clicked?.Invoke(endingId);
        public void Show(string id, string name, string description, Color backgroundColor)
        {
            endingId = id;
            if (button != null && button.targetGraphic != null) button.targetGraphic.color = backgroundColor;
            if (nameText != null) nameText.text = name;
            if (descriptionText != null) descriptionText.text = description;
        }
    }
}
