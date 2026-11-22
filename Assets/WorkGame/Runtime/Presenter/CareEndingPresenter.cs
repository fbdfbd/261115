using System;
using System.Linq;
using UnityEngine;

namespace WorkGame
{
    public sealed class CareEndingPresenter : IDisposable
    {
        private readonly CareSystem care;
        private readonly WorkSystem work;
        private readonly CareEndingView view;
        private readonly CareDialogueView dialogue;
        private readonly CareConfigSO config;
        private readonly CareFlowSystem flow;
        private readonly Func<string, bool> wasRead;
        private CareEndingResult shown;
        private CareEndingLine[] lines = Array.Empty<CareEndingLine>();
        private CareDialogueData script;
        private Sprite careerImage;
        private int index;
        private bool connected, paused, finished, canSkip;
        public event Action<string> ReadCompleted;

        public CareEndingPresenter(CareSystem care, WorkSystem work, CareEndingView view, CareConfigSO config,
            CareFlowSystem flow = null, CareDialogueView dialogue = null, Func<string, bool> wasRead = null)
        {
            this.care = care; this.work = work; this.view = view; this.config = config;
            this.flow = flow; this.dialogue = dialogue; this.wasRead = wasRead;
        }
        public void Connect()
        {
            if (connected || view == null || dialogue == null) return;
            connected = true;
            view.ConfigureDialogue(dialogue);
            care.EndingReady += Open; work.Changed += RefreshPause;
            dialogue.NextRequested += Next; dialogue.SkipRequested += Skip; dialogue.LogRequested += ShowLog;
            view.Completed += FinishReading;
            if (care.Run.Ending != null) Open(care.Run.Ending);
            else view.Hide();
        }
        private void Open(CareEndingResult result)
        {
            if (result == null) return;
            if (shown != result)
            {
                shown = result; index = 0; finished = false; canSkip = wasRead?.Invoke(result.Ending.Id) == true;
                var asset = config.Endings.FirstOrDefault(item => item != null && item.Id == result.Ending.Id)
                    ?? config.FallbackEnding;
                careerImage = asset?.Pages?.FirstOrDefault(page => page?.Image != null)?.Image ?? asset?.CollectionImage;
                string careerText = asset?.Pages == null ? null : string.Join("\n", asset.Pages
                    .Where(page => page != null && !string.IsNullOrWhiteSpace(page.Text)).Select(page => page.Text));
                lines = CareEndingScript.Build(result, flow?.BuildEndingMemories(), careerText,
                    asset == null || asset.AppendFriendshipText);
                script = new CareDialogueData
                {
                    Id = "ending/" + result.Ending.Id,
                    Lines = lines.Select(line => new CareDialogueLine
                    {
                        Speaker = "나레이션", Text = line.Text,
                        Emotion = result.Friendship == CareFriendship.High ? CareDialogueEmotion.Happy : CareDialogueEmotion.Normal
                    }).ToArray()
                };
            }
            view.Open(result.Ending.Name, careerImage);
            paused = work.IsPaused; view.SetPaused(paused); dialogue.SetPaused(paused);
            if (finished) { dialogue.Hide(); view.ShowComplete(false); }
            else ShowLine();
        }
        private bool CanInput => shown != null && !finished && !work.IsPaused && !view.InputBlocked
            && !view.IsTransitioning && !dialogue.InputBlocked && !dialogue.LogOpen;
        private void ShowLine()
        {
            if (lines[index].Scene == CareEndingScene.Career) view.ShowCareer(careerImage);
            else view.ShowRoom(lines[index].CharacterVisible);
            bool cutsceneChanged = index > 0 && lines[index - 1].Scene != lines[index].Scene
                && (lines[index - 1].Scene == CareEndingScene.Career || lines[index].Scene == CareEndingScene.Career);
            view.PresentDialogue(dialogue,
                () => dialogue.Show(script, index, canSkip, playNarrationSound: true), cutsceneChanged);
        }
        private void Next()
        {
            if (!CanInput) return;
            if (++index < lines.Length) ShowLine();
            else Complete();
        }
        private void Complete()
        {
            finished = true; dialogue.Hide(); view.ShowComplete();
        }
        private void Skip() { if (CanInput && canSkip) Complete(); }
        private void ShowLog()
        {
            if (shown == null || finished) return;
            dialogue.ShowLog(string.Join("\n\n", lines.Take(index + 1).Select(line => line.Text)));
        }
        private void RefreshPause()
        {
            if (paused == work.IsPaused) return;
            paused = work.IsPaused; view.SetPaused(paused);
            if (shown != null && !finished) dialogue.SetPaused(paused);
        }
        private void FinishReading() { if (shown != null) ReadCompleted?.Invoke(shown.Ending.Id); }
        public void Disconnect()
        {
            if (!connected) return;
            connected = false;
            care.EndingReady -= Open; work.Changed -= RefreshPause;
            dialogue.NextRequested -= Next; dialogue.SkipRequested -= Skip; dialogue.LogRequested -= ShowLog;
            view.Completed -= FinishReading;
            if (shown != null) dialogue.Hide();
            view.Hide();
        }
        public void Dispose() => Disconnect();
    }
}
