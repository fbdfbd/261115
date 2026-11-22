using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
                    ? asset.ChoiceText : "5년 후 달이의 모습입니다.";
                if (view.Cards[i] != null) view.Cards[i].Show(ending.Id, title, description, GetCardColor(ending));
            }
            view.Show(choices.Count);
        }
        private static Color GetCardColor(CareEnding ending)
        {
            CareMainNeed main = null;
            foreach (var need in ending.MainNeeds)
                if (main == null || (int)need.Grade > (int)main.Grade) main = need;
            if (main == null) return Color.white;
            switch (main.Stat)
            {
                case CareStat.Knowledge: return new Color(227f / 255, 237f / 255, 250f / 255);
                case CareStat.Fitness: return new Color(224f / 255, 240f / 255, 227f / 255);
                case CareStat.Emotion: return new Color(250f / 255, 232f / 255, 237f / 255);
                case CareStat.Social: return new Color(250f / 255, 242f / 255, 219f / 255);
                default: return Color.white;
            }
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
