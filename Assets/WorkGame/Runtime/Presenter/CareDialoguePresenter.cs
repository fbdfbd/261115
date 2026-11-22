using System;
using System.Text;

namespace WorkGame
{
    public sealed class CareDialoguePresenter : IDisposable
    {
        private readonly WorkSystem work;
        private readonly CareFlowSystem flow;
        private readonly CareDialogueView view;
        private CareDialoguePresentationView presentation;
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
            presentation = view.Presentation;
            if (presentation != null) presentation.BlockingChanged += SetPresentationBlocked;
            flow.Dialogue.LineEntered += EnterLine;
            flow.Changed += Refresh; work.Changed += RefreshPause;
            view.NextRequested += Next; view.ChoiceRequested += Choose;
            view.SkipRequested += Skip; view.LogRequested += ShowLog;
            Refresh();
            flow.Dialogue.ReplayPresentation(EnterLine);
        }
        private bool CanInput => flow.Phase != CareFlowPhase.Ended && !work.IsPaused && !view.InputBlocked
            && !view.LogOpen && !flow.Dialogue.PresentationBlocked;
        private void SetPresentationBlocked(bool value)
        {
            flow.Dialogue.SetPresentationBlocked(value);
            view.SetPresentationPlaying(value);
        }
        private void EnterLine(CareDialogueLine line, bool immediate) => presentation?.Play(line.Cues, immediate);
        private void Next()
        {
            if (!CanInput) return;
            if (flow.Dialogue.IsOpen && !flow.Dialogue.HasChoices
                && flow.Dialogue.LineIndex == flow.Dialogue.Current.Lines.Length - 1
                && presentation != null && (presentation.HasCutscene || flow.WillAdvanceAfterDialogue))
            {
                FinishCutscene(() => flow.Dialogue.Next());
            }
            else flow.Dialogue.Next();
        }
        private void Choose(int index)
        {
            if (!CanInput || !flow.Dialogue.HasChoices || index < 0
                || index >= flow.Dialogue.Current.Choices.Length) return;
            if (presentation != null && (presentation.HasCutscene || flow.WillAdvanceAfterDialogue)
                && string.IsNullOrEmpty(flow.Dialogue.Current.Choices[index].NextDialogue))
                FinishCutscene(() => flow.Dialogue.Choose(index));
            else flow.Dialogue.Choose(index);
        }
        private void FinishCutscene(Action finish)
        {
            presentation.FinishDialogue(() =>
            {
                // Commit the final line or choice while the diagonal overlay fully covers the screen.
                flow.Dialogue.SetPresentationBlocked(false);
                finish();
            }, flow.WillAdvanceAfterDialogue);
        }
        private void Skip()
        {
            if (!CanInput || !flow.Dialogue.Skip(false)) return;
            if (!flow.Dialogue.HasChoices) Next();
        }
        private void Refresh()
        {
            if (flow.Phase == CareFlowPhase.Ended || flow.Phase == CareFlowPhase.EndingChoice) return;
            paused = work.IsPaused;
            presentation?.SetPaused(paused);
            view.SetPaused(paused);
            view.Show(flow.Dialogue.Current, flow.Dialogue.LineIndex, flow.Dialogue.CanSkip && !paused,
                flow.IsStoryPlaying ? flow.StoryRewardText : "");
            if (!flow.Dialogue.IsOpen) presentation?.Clear();
        }
        private void RefreshPause() { if (paused != work.IsPaused) Refresh(); }
        private void ShowLog()
        {
            if (flow.Phase == CareFlowPhase.Ended || flow.Phase == CareFlowPhase.EndingChoice) return;
            var entries = flow.Dialogue.History;
            if (logCount != entries.Count)
            {
                var text = new StringBuilder();
                foreach (var entry in entries)
                {
                    if (entry.DialogueId == flow.IntroDialogueId) text.Append("한 달 전");
                    else text.Append(entry.Month).Append("월");
                    if (!string.IsNullOrWhiteSpace(entry.Speaker)
                        && !string.Equals(entry.Speaker.Trim(), "나레이션", StringComparison.Ordinal))
                        text.Append(" · ").Append(entry.Speaker);
                    text.AppendLine();
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
            flow.Dialogue.LineEntered -= EnterLine;
            if (presentation != null) { presentation.Clear(true); presentation.BlockingChanged -= SetPresentationBlocked; }
            SetPresentationBlocked(false);
            flow.Changed -= Refresh; work.Changed -= RefreshPause;
            view.NextRequested -= Next; view.ChoiceRequested -= Choose;
            view.SkipRequested -= Skip; view.LogRequested -= ShowLog;
            view.Hide();
        }
        public void Dispose() => Disconnect();
    }
}
