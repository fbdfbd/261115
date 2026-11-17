using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareFurniturePresenter : IDisposable
    {
        private readonly WorkSystem work;
        private readonly CarePresenter actions;
        private readonly CareSystem care;
        private readonly CareFlowSystem flow;
        private readonly CareFurnitureView view;
        private readonly Button openButton;
        private readonly Dictionary<string, CareGift> gifts;
        private readonly Dictionary<string, CareGiftSO> art;
        private readonly Dictionary<CareEquippedItemView, Action> clearActions = new Dictionary<CareEquippedItemView, Action>();
        private bool connected;
        private bool wasVisible;
        private long shownMoney = -1;
        private bool Visible => flow.Phase == CareFlowPhase.Panel && flow.Panel == CarePanel.Furniture;

        public CareFurniturePresenter(WorkSystem work, CarePresenter actions, CareFlowSystem flow,
            CareFurnitureView view, Button openButton, CareConfigSO config)
        {
            this.work = work; this.actions = actions; care = actions.System;
            this.flow = flow; this.view = view; this.openButton = openButton;
            gifts = care.Gifts.ToDictionary(x => x.Id);
            art = config.Gifts.Where(x => x != null).ToDictionary(x => x.Id);
            if (view != null)
                foreach (var slot in view.Equipped)
                    if (slot != null && !clearActions.ContainsKey(slot))
                        clearActions.Add(slot, () => Clear(slot.Kind));
        }

        public void Connect()
        {
            if (connected) return;
            connected = true;
            care.Changed += RefreshData; flow.Changed += RefreshPanel; work.Changed += RefreshMoney;
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (view != null)
            {
                view.CloseClicked += Close;
                foreach (var node in view.Nodes)
                    if (node != null) { node.BuyRequested += Buy; node.ToggleRequested += Toggle; }
                foreach (var pair in clearActions) pair.Key.Clicked += pair.Value;
            }
            RefreshPanel();
        }

        public void Open() { if (view != null) flow.OpenPanel(CarePanel.Furniture); }
        private void Close() { if (Visible) flow.ClosePanel(); }
        private void Buy(string id) { if (Visible) actions.BuyGift(id); }
        private void Toggle(string id)
        {
            if (!Visible || !gifts.TryGetValue(id, out var gift)) return;
            if (care.Run.GetActiveGift(gift.Kind) == id) actions.ClearActiveGift(gift.Kind);
            else actions.SetActiveGift(id);
        }
        private void Clear(CareGiftKind kind) { if (Visible) actions.ClearActiveGift(kind); }
        private void RefreshPanel()
        {
            if (openButton != null) openButton.interactable = view != null && flow.Phase == CareFlowPhase.Free;
            if (view == null) return;
            if (wasVisible != Visible) { wasVisible = Visible; view.SetVisible(Visible); }
            if (Visible) RefreshData();
        }
        private void RefreshMoney() { if (Visible && shownMoney != care.Money) RefreshData(); }
        private void RefreshData()
        {
            if (!Visible || view == null) return;
            shownMoney = care.Money;
            view.ShowMoney(shownMoney);
            foreach (var node in view.Nodes)
            {
                if (node == null) continue;
                if (!gifts.TryGetValue(node.GiftId, out var gift)) { node.Show(false, false, false); continue; }
                node.Show(care.Run.Gifts.Contains(gift.Id), care.Run.GetActiveGift(gift.Kind) == gift.Id, care.CanBuyGift(gift.Id));
            }
            foreach (var slot in view.Equipped)
            {
                if (slot == null) continue;
                string id = care.Run.GetActiveGift(slot.Kind);
                CareGift gift = id != null && gifts.TryGetValue(id, out var found) ? found : null;
                CareGiftSO asset = id != null && art.TryGetValue(id, out var foundArt) ? foundArt : null;
                slot.Show(gift?.Name, asset != null ? asset.Icon : null);
            }
        }

        public void Disconnect()
        {
            if (!connected) return;
            connected = false;
            care.Changed -= RefreshData; flow.Changed -= RefreshPanel; work.Changed -= RefreshMoney;
            if (openButton != null) { openButton.onClick.RemoveListener(Open); openButton.interactable = false; }
            if (view != null)
            {
                view.CloseClicked -= Close;
                foreach (var node in view.Nodes)
                    if (node != null) { node.BuyRequested -= Buy; node.ToggleRequested -= Toggle; }
                foreach (var pair in clearActions) if (pair.Key != null) pair.Key.Clicked -= pair.Value;
                view.SetVisible(false);
            }
            wasVisible = false;
        }
        public void Dispose() => Disconnect();
    }
}
