using System;
using System.Collections.Generic;

namespace WorkGame
{
    public sealed class WorkFaxSystem
    {
        private readonly WorkSystem work;
        private readonly List<WorkFaxJob> jobs = new List<WorkFaxJob>();
        public IReadOnlyList<WorkFaxJob> Jobs { get; }
        public double SecondsPerPaper { get; private set; } = 5;

        public WorkFaxSystem(WorkSystem work)
        {
            this.work = work;
            Jobs = jobs.AsReadOnly();
        }

        public void SetSecondsPerPaper(double seconds)
        {
            if (!WorkSettings.IsPositive(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
            SecondsPerPaper = seconds;
        }

        public bool TrySend(int paperId, bool forceUnlocked = false)
        {
            if (!work.IsWorking || (!work.Upgrades.FaxUnlocked && !forceUnlocked)
                || jobs.Count >= work.Upgrades.FaxSlots) return false;
            var paper = work.GetPendingPaper(paperId);
            if (paper == null || paper.IsInFax) return false;
            work.ReleasePaper(paperId);
            paper.IsInFax = true;
            jobs.Add(new WorkFaxJob(paperId));
            work.Notify();
            return true;
        }

        internal void AddTime(double seconds)
        {
            if (!work.IsWorking || jobs.Count == 0) return;
            double progress = seconds * (1 + work.Upgrades.FaxSpeedBonus) / SecondsPerPaper;
            // 맨 앞 한 장만 처리하고, 대기 중인 서류는 0%를 유지합니다.
            var job = jobs[0];
            job.Progress = Math.Min(1, job.Progress + progress);
            if (job.Progress < 1) return;
            jobs.RemoveAt(0);
            var paper = work.GetPendingPaper(job.PaperId);
            if (paper == null) return;
            paper.IsInFax = false;
            work.ProcessPaper(paper.Id);
        }

        internal void Clear()
        {
            foreach (var job in jobs)
            {
                var paper = work.GetPendingPaper(job.PaperId);
                if (paper != null) paper.IsInFax = false;
            }
            jobs.Clear();
        }
    }
}
