using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public sealed class CareEndingChoicePresenter : IDisposable
    {
        private readonly CareSystem care;
        private readonly CareFlowSystem flow;
        private readonly CareEndingChoiceView view;
        private readonly Func<string, bool> wasRead;
        private readonly Dictionary<string, CareEndingSO> art;
        private bool connected;

        public CareEndingChoicePresenter(CareSystem care, CareFlowSystem flow, CareEndingChoiceView view,
            CareConfigSO config, Func<string, bool> wasRead)
        {
            this.care = care; this.flow = flow; this.view = view; this.wasRead = wasRead;
            art = config.Endings.Concat(new[] { config.FallbackEnding }).Where(item => item != null).ToDictionary(item => item.Id);
        }
        public void Connect()
        {
            if (connected || view == null) return;
            connected = true;
            flow.Changed += Refresh; view.Selected += Select;
            Refresh();
        }
        public void Refresh()
        {
            if (view == null) return;
            if (flow.Phase != CareFlowPhase.EndingChoice || care.Run.Ending != null) { view.Hide(); return; }
            var choices = care.GetEndingChoices();
            for (int i = 0; i < choices.Count && i < view.Cards.Count; i++)
            {
                var ending = choices[i];
                art.TryGetValue(ending.Id, out var asset);
                string title = wasRead != null && wasRead(ending.Id) ? ending.Name : "???";
                string description = asset != null && !string.IsNullOrWhiteSpace(asset.ChoiceText)
                    ? asset.ChoiceText : "달이가 새로운 길을 선택합니다.";
                if (view.Cards[i] != null) view.Cards[i].Show(ending.Id, title, description);
            }
            view.Show(choices.Count);
        }
        private void Select(string id)
        {
            if (flow.ChooseEnding(id)) return;
            view.CancelSelection();
            Refresh();
        }
        public void Disconnect()
        {
            if (!connected) return;
            connected = false;
            flow.Changed -= Refresh;
            if (view != null) { view.Selected -= Select; view.Hide(); }
        }
        public void Dispose() => Disconnect();
    }
}
