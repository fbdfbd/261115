using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    [RequireComponent(typeof(WorkHoldButton))]
    public sealed class WorkUpgradeNodeView : MonoBehaviour
    {
        [SerializeField] private WorkUpgradeSO node;
        [SerializeField] private Button buyButton;
        [SerializeField] private WorkHoldButton holdButton;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject lockedMark;
        [SerializeField] private GameObject boughtMark;
        [SerializeField] private GameObject selectedMark;
        [SerializeField] private TMP_Text onOffText;
        [Header("구매 완료 배경")]
        [SerializeField] private Image background;
        [SerializeField] private Color boughtBackgroundColor = new Color(0.72f, 0.79f, 0.84f);
        [SerializeField] private Color lockedBackgroundColor = new Color(0.88f, 0.86f, 0.82f);
        private Color normalBackgroundColor;
        private bool backgroundSaved;
        public string NodeId => node != null ? node.Id : "";
        public event Action<string> BuyClicked;
        public event Action<WorkUpgradeSO> Selected;

        private void OnEnable()
        {
            if (holdButton == null) holdButton = GetComponent<WorkHoldButton>();
            if (holdButton == null) return;
            holdButton.Pressed += SelectNode;
            holdButton.Completed += Buy;
        }
        private void OnDisable()
        {
            if (holdButton == null) return;
            holdButton.CancelHold();
            holdButton.Pressed -= SelectNode;
            holdButton.Completed -= Buy;
        }
        private void Buy() => BuyClicked?.Invoke(NodeId);
        private void SelectNode() { if (node != null) Selected?.Invoke(node); }
        public void SetSelected(bool selected) => WorkUi.Show(selectedMark, selected);
        public void CancelHold() => holdButton?.CancelHold();

        public void Show(WorkUpgradeState state, bool canBuy, bool featureEnabled = false)
        {
            if (node == null) { if (buyButton != null) buyButton.interactable = false; return; }
            ShowBackground(state);
            if (nameText != null) nameText.text = node.Name;
            if (descriptionText != null) descriptionText.text = node.Description;
            if (costText != null) costText.text = state == WorkUpgradeState.Bought ? "구매 완료" : node.Cost.ToString();
            if (icon != null) { icon.sprite = node.Icon; icon.enabled = node.Icon != null; }
            if (buyButton != null) buyButton.interactable = true;
            if (holdButton == null) holdButton = GetComponent<WorkHoldButton>();
            if (holdButton != null) holdButton.SetAllowed(canBuy);
            WorkUi.Show(lockedMark, state == WorkUpgradeState.Locked);
            WorkUi.Show(boughtMark, state == WorkUpgradeState.Bought);
            if (onOffText != null)
            {
                WorkUi.Show(onOffText.gameObject, node.SwitchKind != WorkUpgradeSwitch.None);
                onOffText.text = state != WorkUpgradeState.Bought ? "미해금" : featureEnabled ? "ON" : "OFF";
            }
        }

        private void ShowBackground(WorkUpgradeState state)
        {
            if (background == null && buyButton != null) background = buyButton.targetGraphic as Image;
            if (background == null) background = GetComponent<Image>();
            if (background == null) return;
            if (!backgroundSaved)
            {
                normalBackgroundColor = background.color;
                backgroundSaved = true;
            }
            background.color = state == WorkUpgradeState.Bought ? boughtBackgroundColor
                : state == WorkUpgradeState.Locked ? lockedBackgroundColor : normalBackgroundColor;
        }
    }
}
