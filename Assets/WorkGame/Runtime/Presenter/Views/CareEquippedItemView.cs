using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareEquippedItemView : MonoBehaviour
    {
        [SerializeField] private CareGiftKind kind;
        [SerializeField] private bool reserved;
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        public CareGiftKind Kind => kind;
        public bool Reserved => reserved;
        public event Action Clicked;
        private string lastName;
        private Sprite lastIcon;
        private bool shown;

        private void OnEnable() { if (button != null) button.onClick.AddListener(Click); }
        private void OnDisable() { if (button != null) button.onClick.RemoveListener(Click); }
        private void Click() => Clicked?.Invoke();
        public void Show(string itemName, Sprite itemIcon)
        {
            string label = string.IsNullOrEmpty(itemName) ? "비어 있음" : itemName;
            if ((!shown || lastName != label) && nameText != null) nameText.text = label;
            if ((!shown || lastIcon != itemIcon) && icon != null)
            {
                icon.sprite = itemIcon;
                icon.enabled = itemIcon != null;
            }
            lastName = label; lastIcon = itemIcon; shown = true;
        }
    }
}
