using System;
using System.Text;

namespace WorkGame
{
    public sealed class CareDialoguePresenter : IDisposable
    {
        private readonly WorkSystem work;
        private readonly CareFlowSystem flow;
        private readonly CareDialogueView view;
        private bool connected, paused;
        private int logCount = -1;
        private string log = "";
        public CareDialoguePresenter(WorkSystem work, CareFlowSystem flow, CareDialogueView view)
        {
            this.work = work; this.flow = flow; this.view = view;
        }
        public void Connect()
        {
            if (connected || view == null) return;
            connected = true;
            flow.Changed += Refresh; work.Changed += RefreshPause;
            view.NextRequested += Next; view.ChoiceRequested += Choose;
            view.SkipRequested += Skip; view.LogRequested += ShowLog;
            Refresh();
        }
        private bool CanInput => !work.IsPaused && !view.InputBlocked && !view.LogOpen;
        private void Next() { if (CanInput) flow.Dialogue.Next(); }
        private void Choose(int index) { if (CanInput) flow.Dialogue.Choose(index); }
        private void Skip() { if (CanInput) flow.Dialogue.Skip(); }
        private void Refresh()
        {
            paused = work.IsPaused;
            view.Show(flow.Dialogue.Current, flow.Dialogue.LineIndex, flow.Dialogue.CanSkip && !paused);
        }
        private void RefreshPause() { if (paused != work.IsPaused) Refresh(); }
        private void ShowLog()
        {
            var entries = flow.Dialogue.History;
            if (logCount != entries.Count)
            {
                var text = new StringBuilder();
                foreach (var entry in entries)
                {
                    text.Append(entry.Month).Append("월 · ").AppendLine(entry.Speaker);
                    text.AppendLine(entry.Text).AppendLine();
                }
                log = text.ToString(); logCount = entries.Count;
            }
            view.ShowLog(log);
        }
        public void Disconnect()
        {
            if (!connected) return;
            connected = false;
            flow.Changed -= Refresh; work.Changed -= RefreshPause;
            view.NextRequested -= Next; view.ChoiceRequested -= Choose;
            view.SkipRequested -= Skip; view.LogRequested -= ShowLog;
            view.Hide();
        }
        public void Dispose() => Disconnect();
    }
}
