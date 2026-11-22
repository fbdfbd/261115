using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public sealed class CareStatusPresenter : IDisposable
    {
        private readonly CareSystem care;
        private readonly CareFlowSystem flow;
        private readonly CareHudView hud;
        private readonly CareStatusView view;
        private readonly CareGradeSettings grades;
        private readonly Dictionary<string, CareGift> gifts;
        private readonly Dictionary<string, CareGiftSO> giftArt;
        private bool connected;
        public event Action FurnitureRequested;
        private bool Visible => flow.Phase == CareFlowPhase.Panel && flow.Panel == CarePanel.Stats;

        public CareStatusPresenter(CareSystem care, CareFlowSystem flow, CareHudView hud, CareStatusView view, CareConfigSO config)
        {
            this.care = care; this.flow = flow; this.hud = hud; this.view = view;
            grades = care.CopyGrades();
            gifts = care.Gifts.ToDictionary(x => x.Id);
            giftArt = config.Gifts.Where(x => x != null).ToDictionary(x => x.Id);
        }

        public void Connect()
        {
            if (connected) return;
            connected = true;
            care.Changed += RefreshData;
            flow.Changed += RefreshPanel;
            if (hud != null) hud.DetailsClicked += Open;
            if (view != null) { view.CloseClicked += Close; view.FurnitureClicked += OpenFurniture; }
            RefreshData(); RefreshPanel();
        }

        private void Open() => flow.OpenPanel(CarePanel.Stats);
        private void Close() { if (Visible) flow.ClosePanel(); }
        private void OpenFurniture() { if (Visible) FurnitureRequested?.Invoke(); }

        private void RefreshPanel()
        {
            if (hud != null) hud.SetCanOpen(flow.Phase == CareFlowPhase.Free && view != null);
            if (view == null) return;
            view.SetVisible(Visible);
            if (Visible) RefreshStatus();
        }

        private void RefreshData()
        {
            if (hud != null)
            {
                hud.SetMonth(care.Month);
                ShowRows(hud.MainStats);
            }
            if (Visible && view != null) RefreshStatus();
        }

        private void RefreshStatus()
        {
            ShowRows(view.MainStats); ShowRows(view.DetailStats);
            foreach (var slot in view.Equipped)
            {
                if (slot == null) continue;
                string id = slot.Reserved ? null : care.Run.GetActiveGift(slot.Kind);
                CareGift gift = id != null && gifts.TryGetValue(id, out var item) ? item : null;
                CareGiftSO art = id != null && giftArt.TryGetValue(id, out var asset) ? asset : null;
                slot.Show(gift?.Name, art != null ? art.Icon : null);
            }
        }

        private void ShowRows(IReadOnlyList<CareStatRowView> rows)
        {
            foreach (var row in rows)
            {
                if (row == null) continue;
                bool main = CareStats.IsMain(row.Stat);
                row.Show(care.Run.GetStat(row.Stat), main ? care.GetGrade(row.Stat) : CareGrade.None,
                    main ? (int)Math.Min(int.MaxValue, care.StatMax * 3L) : care.StatMax, grades);
            }
        }

        public void Disconnect()
        {
            if (!connected) return;
            connected = false;
            care.Changed -= RefreshData; flow.Changed -= RefreshPanel;
            if (hud != null) { hud.DetailsClicked -= Open; hud.SetCanOpen(false); }
            if (view != null)
            {
                view.CloseClicked -= Close; view.FurnitureClicked -= OpenFurniture;
                view.SetVisible(false);
            }
        }
        public void Dispose() { Disconnect(); FurnitureRequested = null; }
    }
}
