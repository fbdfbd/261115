using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    // Observe committed state, so rejected actions, refreshes and load/bind never sound like a purchase.
    public sealed class CareSfxPresenter : IDisposable
    {
        private readonly WorkSystem work;
        private readonly CareSystem care;
        private readonly CareFlowSystem flow;
        private readonly CareSfxSO sounds;
        private readonly IGameSfxPlayer player;
        private readonly ICareSfxSource[] sources;
        private readonly List<IGameSfxHandle> voices = new List<IGameSfxHandle>();
        private string[] plan, equipped;
        private HashSet<string> gifts, synergies;
        private int month, upgrades;
        private CareFlowPhase phase;
        private CarePanel panel;
        private bool disposed;

        public CareSfxPresenter(WorkSystem work, CareSystem care, CareFlowSystem flow,
            CareSfxSO sounds, IGameSfxPlayer player, params ICareSfxSource[] sources)
        {
            this.work = work; this.care = care; this.flow = flow;
            this.sounds = sounds; this.player = player;
            this.sources = sources.Where(x => x != null).Distinct().ToArray();
            SnapshotCare(); upgrades = work.Run.BoughtUpgrades.Count;
            phase = flow.Phase; panel = flow.Panel;
            care.Changed += OnCareChanged;
            work.Changed += OnWorkChanged;
            flow.Changed += OnFlowChanged;
            flow.Blocked += OnBlocked;
            foreach (var source in this.sources) source.SoundRequested += OnCue;
            SyncVoices();
        }

        private void SnapshotCare()
        {
            month = care.Month; plan = care.Plan.ToArray(); equipped = care.Run.ActiveGifts.ToArray();
            gifts = new HashSet<string>(care.Run.Gifts);
            synergies = care.CanPlan ? new HashSet<string>(care.Preview().Synergies) : new HashSet<string>();
        }

        private void OnCareChanged()
        {
            bool sameMonth = month == care.Month;
            bool changedPlan = !plan.SequenceEqual(care.Plan);
            bool added = changedPlan && care.Plan.Where((id, i) => id != plan[i]).Any(id => !string.IsNullOrEmpty(id));
            bool bought = care.Run.Gifts.Any(id => !gifts.Contains(id));
            bool changedEquipment = !equipped.SequenceEqual(care.Run.ActiveGifts);
            bool putOn = changedEquipment && care.Run.ActiveGifts.Where((id, i) => id != equipped[i]).Any(id => id != null);
            var previousSynergies = synergies;
            SnapshotCare();
            if (!sameMonth) return;
            if (changedPlan && care.CanPlan)
            {
                OnCue(added ? CareSfxCue.ScheduleAdd : CareSfxCue.ScheduleRemove);
                if (synergies.Except(previousSynergies).Any()) OnCue(CareSfxCue.Synergy);
            }
            if (bought) OnCue(CareSfxCue.Purchase);
            else if (changedEquipment) OnCue(putOn ? CareSfxCue.Equip : CareSfxCue.Unequip);
        }

        private void OnWorkChanged()
        {
            int count = work.Run.BoughtUpgrades.Count;
            bool bought = count > upgrades;
            upgrades = count;
            if (bought) OnCue(CareSfxCue.Purchase);
            SyncVoices();
        }

        private void OnFlowChanged()
        {
            var oldPhase = phase; var oldPanel = panel;
            phase = flow.Phase; panel = flow.Panel;
            if (phase == CareFlowPhase.Panel && (oldPhase != phase || oldPanel != panel)) OnCue(CareSfxCue.PanelOpen);
            else if (oldPhase == CareFlowPhase.Panel && phase == CareFlowPhase.Free) OnCue(CareSfxCue.PanelClose);
            if (phase == CareFlowPhase.Ended && oldPhase == CareFlowPhase.EndingChoice) OnCue(CareSfxCue.EndingSelect);
            SyncVoices(oldPhase != phase && (phase == CareFlowPhase.Working || phase == CareFlowPhase.Report));
        }

        private void OnBlocked(string message) => OnCue(CareSfxCue.Denied);
        public void OnCue(CareSfxCue cue)
        {
            if (disposed || work.IsPaused) return;
            if (cue != CareSfxCue.Purchase && (flow.Phase == CareFlowPhase.Working || flow.Phase == CareFlowPhase.Report)) return;
            var handle = player.Play(sounds.Get(cue));
            if (handle != null) voices.Add(handle);
        }
        private void SyncVoices(bool stop = false)
        {
            foreach (var report in sources.OfType<CareMonthReportView>()) report.SetPaused(work.IsPaused);
            foreach (var voice in voices)
                if (stop) voice.Dispose(); else voice.SetPaused(work.IsPaused);
        }
        public void Tick()
        {
            for (int i = voices.Count - 1; i >= 0; i--)
                if (!voices[i].IsPlaying) { voices[i].Dispose(); voices.RemoveAt(i); }
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            care.Changed -= OnCareChanged; work.Changed -= OnWorkChanged;
            flow.Changed -= OnFlowChanged; flow.Blocked -= OnBlocked;
            foreach (var source in sources) source.SoundRequested -= OnCue;
            foreach (var voice in voices) voice.Dispose();
            voices.Clear();
        }
    }
}
