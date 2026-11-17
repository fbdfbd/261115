using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    [RequireComponent(typeof(Button), typeof(WorkHoldButton))]
    public sealed class CareFurnitureNodeView : MonoBehaviour
    {
        [SerializeField] private CareGiftSO gift;
        [SerializeField] private Button button;
        [SerializeField] private WorkHoldButton holdButton;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Image icon;
        [SerializeField] private Image background;
        [SerializeField] private Outline activeOutline;
        [SerializeField] private Color normalColor = new Color(0.95f, 0.95f, 0.92f);
        [SerializeField] private Color ownedColor = new Color(0.87f, 0.9f, 0.86f);
        [SerializeField] private Color activeColor = new Color(0.74f, 0.84f, 0.74f);
        [SerializeField] private Color priceColor = new Color(0.18f, 0.22f, 0.2f);
        [SerializeField] private Color lowMoneyColor = new Color(0.72f, 0.28f, 0.26f);
        private bool boughtThisPress;
        private bool owned, active, canBuy;
        private string lastLabel;
        public string GiftId => gift != null ? gift.Id : "";
        public event Action<string> BuyRequested;
        public event Action<string> ToggleRequested;

        private void Awake() => GetParts();
        private void GetParts()
        {
            if (button == null) button = GetComponent<Button>();
            if (holdButton == null) holdButton = GetComponent<WorkHoldButton>();
        }
        private void OnEnable()
        {
            GetParts();
            button.onClick.AddListener(Click);
            holdButton.Pressed += Press;
            holdButton.Completed += Buy;
        }
        private void OnDisable()
        {
            button.onClick.RemoveListener(Click);
            holdButton.Pressed -= Press;
            holdButton.Completed -= Buy;
            CancelHold();
        }

        public void Show(bool isOwned, bool isActive, bool canAfford)
        {
            GetParts();
            owned = isOwned; active = isActive; canBuy = !owned && canAfford && gift != null;
            button.interactable = owned || canBuy;
            holdButton.SetAllowed(canBuy);
            if (gift != null)
            {
                if (nameText != null && nameText.text != gift.Name) nameText.text = gift.Name;
                if (icon != null) { icon.sprite = gift.Icon; icon.enabled = gift.Icon != null; }
            }
            string label = active ? "장착 중" : owned ? "보유 중" : gift != null ? gift.Cost.ToString("N0") : "—";
            if (priceText != null)
            {
                if (lastLabel != label) priceText.text = label;
                priceText.color = owned || canBuy ? priceColor : lowMoneyColor;
            }
            lastLabel = label;
            if (background != null) background.color = active ? activeColor : owned ? ownedColor : normalColor;
            if (activeOutline != null) activeOutline.enabled = active;
        }

        private void Press() => boughtThisPress = false;
        private void Buy()
        {
            if (!canBuy) return;
            // 구매 후 같은 터치의 Button.onClick으로 바로 해제되지 않게 합니다.
            boughtThisPress = true;
            BuyRequested?.Invoke(GiftId);
        }
        private void Click()
        {
            if (boughtThisPress) { boughtThisPress = false; return; }
            if (owned) ToggleRequested?.Invoke(GiftId);
        }
        public void CancelHold()
        {
            if (holdButton != null) holdButton.CancelHold();
            boughtThisPress = false;
        }
    }
}
