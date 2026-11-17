using System;

namespace WorkGame
{
    public sealed class WorkUpgradePresenter : IDisposable
    {
        private readonly WorkSystem work;
        private readonly WorkUpgradeSystem upgrades;
        private readonly WorkUpgradeTreeView view;
        private bool connected;
        private int lastPoints = -1;
        private int lastBought = -1;
        private bool lastSort;
        private bool lastFax;

        public WorkUpgradePresenter(WorkSystem work, WorkUpgradeSystem upgrades, WorkUpgradeTreeView view)
        { this.work = work; this.upgrades = upgrades; this.view = view; }

        public void Connect()
        {
            if (connected || view == null) return;
            connected = true;
            work.Changed += Refresh;
            view.BuyClicked += Buy;
            view.SortChanged += work.SetSortEnabled;
            view.FaxChanged += work.SetFaxVisible;
            view.SelectionChanged += RefreshSelection;
            lastPoints = lastBought = -1;
            Refresh();
        }

        public void Disconnect()
        {
            if (!connected) return;
            connected = false;
            work.Changed -= Refresh;
            view.BuyClicked -= Buy;
            view.SortChanged -= work.SetSortEnabled;
            view.FaxChanged -= work.SetFaxVisible;
            view.SelectionChanged -= RefreshSelection;
        }

        private void Buy(string id) => upgrades.Buy(id);

        private void Refresh()
        {
            if (lastPoints == work.Run.SkillPoints && lastBought == work.Run.BoughtUpgrades.Count
                && lastSort == work.Run.SortEnabled && lastFax == work.Run.FaxVisible) return;
            lastPoints = work.Run.SkillPoints;
            lastBought = work.Run.BoughtUpgrades.Count;
            lastSort = work.Run.SortEnabled;
            lastFax = work.Run.FaxVisible;
            foreach (var node in view.Nodes)
                if (node != null) node.Show(upgrades.GetState(node.NodeId), upgrades.CanBuy(node.NodeId),
                    IsFeatureEnabled(node.NodeId));
            view.ShowState(lastPoints, upgrades.Stats.SortUnlocked, lastSort, upgrades.Stats.FaxUnlocked, lastFax);
            RefreshSelection();
        }

        private bool IsFeatureEnabled(string id)
        {
            var node = upgrades.GetSwitchKind(id);
            return node == WorkUpgradeSwitch.Sort ? work.Run.SortEnabled
                : node == WorkUpgradeSwitch.Fax && work.Run.FaxVisible;
        }

        private void RefreshSelection() => view.ShowSelectionState(upgrades.GetState(view.SelectedNodeId), upgrades.CanBuy(view.SelectedNodeId));

        public void Dispose() => Disconnect();
    }
}
