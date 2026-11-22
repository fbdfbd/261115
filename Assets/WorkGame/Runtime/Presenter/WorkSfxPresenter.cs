using System;
using System.Collections.Generic;

namespace WorkGame
{
    // A run owns the subscriptions and sound handles; the persistent player owns only audio voices.
    public sealed class WorkSfxPresenter : IDisposable
    {
        private readonly WorkSystem work;
        private readonly WorkSfxSO sounds;
        private readonly IGameSfxPlayer player;
        private readonly WorkDeskView desk;
        private readonly WorkPaperView paper;
        private readonly WorkFeedbackView feedback;
        private readonly WorkHudView hud;
        private readonly WorkFaxView fax;
        private readonly WorkMonthReportView report;
        private readonly List<IGameSfxHandle> oneShots = new List<IGameSfxHandle>();
        private IGameSfxHandle holdLoop, faxLoop;
        private int heldPaper = -1, heldChoice = -1, month = -1;
        private double previousRemaining;
        private bool warned, disposed;

        public WorkSfxPresenter(WorkSystem work, WorkSfxSO sounds, IGameSfxPlayer player,
            WorkDeskView desk = null, WorkPaperView paper = null, WorkFeedbackView feedback = null,
            WorkHudView hud = null, WorkFaxView fax = null, WorkMonthReportView report = null)
        {
            this.work = work; this.sounds = sounds; this.player = player;
            this.desk = desk; this.paper = paper; this.feedback = feedback;
            this.hud = hud; this.fax = fax; this.report = report;
            work.Changed += Refresh;
            if (desk != null) desk.SoundRequested += OnCue;
            if (paper != null) paper.SoundRequested += OnCue;
            if (feedback != null) feedback.SoundRequested += OnCue;
            if (hud != null) hud.SoundRequested += OnCue;
            if (fax != null) fax.OperatingChanged += OnFaxChanged;
            if (report != null) report.SoundRequested += OnCue;
            Refresh();
        }

        private void OnFaxChanged(bool operating) => Refresh();

        private void Refresh()
        {
            if (disposed) return;
            bool newMonth = month != work.Run.Month;
            if (newMonth) { month = work.Run.Month; warned = false; previousRemaining = work.RemainingSeconds; }
            double remaining = work.RemainingSeconds;
            if (!newMonth && work.IsWorking && !warned && sounds.LowTimeSeconds > 0
                && previousRemaining > sounds.LowTimeSeconds && remaining <= sounds.LowTimeSeconds && remaining > 0)
            {
                warned = true;
                OnCue(WorkSfxCue.LowTime);
            }
            previousRemaining = remaining;
            bool holding = work.IsWorking && work.HeldPaperId >= 0 && work.HeldChoiceIndex >= 0;
            if (!holding || heldPaper != work.HeldPaperId || heldChoice != work.HeldChoiceIndex) Stop(ref holdLoop);
            heldPaper = holding ? work.HeldPaperId : -1;
            heldChoice = holding ? work.HeldChoiceIndex : -1;
            if (holding) EnsureLoop(ref holdLoop, sounds.HoldLoop);
            if (work.IsWorking && fax != null && fax.IsOperating) EnsureLoop(ref faxLoop, sounds.FaxLoop);
            else Stop(ref faxLoop);
            foreach (var handle in oneShots) handle.SetPaused(work.IsPaused);
        }

        private void EnsureLoop(ref IGameSfxHandle handle, GameSfxClip sound)
        {
            if (handle != null && handle.IsPlaying) return;
            Stop(ref handle);
            handle = player.Play(sound, true);
        }

        public void OnCue(WorkSfxCue cue)
        {
            if (disposed || work.IsPaused) return;
            bool isReport = cue == WorkSfxCue.ReportRow || cue == WorkSfxCue.ReportComplete;
            if (!isReport && !work.IsWorking) return;
            if (isReport && work.Run.Phase != WorkPhase.AfterWork && work.Run.Phase != WorkPhase.Finished) return;
            if (cue == WorkSfxCue.HoldComplete) Stop(ref holdLoop);
            // Loops are managed from authoritative hold/fax state, never by repeating UI callbacks.
            if (cue == WorkSfxCue.HoldLoop || cue == WorkSfxCue.FaxLoop) return;
            var handle = player.Play(sounds.Get(cue));
            if (handle != null) oneShots.Add(handle);
        }

        public void Tick()
        {
            if (disposed) return;
            for (int i = oneShots.Count - 1; i >= 0; i--)
                if (!oneShots[i].IsPlaying) { oneShots[i].Dispose(); oneShots.RemoveAt(i); }
        }

        private static void Stop(ref IGameSfxHandle handle) { handle?.Dispose(); handle = null; }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            work.Changed -= Refresh;
            if (!ReferenceEquals(desk, null)) desk.SoundRequested -= OnCue;
            if (!ReferenceEquals(paper, null)) paper.SoundRequested -= OnCue;
            if (!ReferenceEquals(feedback, null)) feedback.SoundRequested -= OnCue;
            if (!ReferenceEquals(hud, null)) hud.SoundRequested -= OnCue;
            if (!ReferenceEquals(fax, null)) fax.OperatingChanged -= OnFaxChanged;
            if (!ReferenceEquals(report, null)) report.SoundRequested -= OnCue;
            Stop(ref holdLoop); Stop(ref faxLoop);
            foreach (var handle in oneShots) handle.Dispose();
            oneShots.Clear();
        }
    }
}
