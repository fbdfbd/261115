using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareFurnitureView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private List<Button> closeButtons = new List<Button>();
        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private List<CareFurnitureNodeView> nodes = new List<CareFurnitureNodeView>();
        [SerializeField] private List<CareEquippedItemView> equipped = new List<CareEquippedItemView>();
        private long lastMoney = -1;
        public IReadOnlyList<CareFurnitureNodeView> Nodes => nodes;
        public IReadOnlyList<CareEquippedItemView> Equipped => equipped;
        public event Action CloseClicked;
        private void Awake() => SetVisible(false);
        private void OnEnable() { foreach (var button in closeButtons) if (button != null) button.onClick.AddListener(Close); }
        private void OnDisable()
        {
            foreach (var button in closeButtons) if (button != null) button.onClick.RemoveListener(Close);
            CancelHolds();
        }
        private void Close() => CloseClicked?.Invoke();
        public void SetVisible(bool visible)
        {
            if (!visible) CancelHolds();
            WorkUi.Show(panelGroup, visible);
        }
        public void ShowMoney(long money)
        {
            if (lastMoney == money) return;
            lastMoney = money;
            if (moneyText != null) moneyText.text = "보유 현금  " + money.ToString("N0");
        }
        private void CancelHolds() { foreach (var node in nodes) if (node != null) node.CancelHold(); }
    }
}
