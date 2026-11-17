using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public sealed class WorkSystem : IDisposable
    {
        private readonly WorkSettings settings;
        private readonly WorkPaper[] plannedPapers;
        private readonly Random random;
        private WorkPaper heldPaper;
        private WorkPaper selectedPaper;
        private readonly List<WorkPaper> monthPapers = new List<WorkPaper>();
        private int heldChoice = -1;
        private double heldSeconds;
        private int rightThisMonth;
        private int wrongThisMonth;
        private bool newbieProtectionUsed;
        private bool disposed;

        public WorkRun Run { get; } = new WorkRun();
        public event Action Changed;
        public event Action<WorkSubmitResult> PaperSubmitted;
        public bool IsPaused { get; private set; }
        public bool IsWorking => !disposed && !IsPaused && Run.Phase == WorkPhase.Working;
        public WorkUpgradeStats Upgrades { get; private set; } = new WorkUpgradeStats();
        public WorkFaxSystem Fax { get; }
        public IReadOnlyList<WorkPaper> MonthPapers { get; }
        public int MonthTotal => monthPapers.Count;
        public int MonthDone => MonthTotal - PendingCount;
        public int PendingCount => Run.Papers.Count(x => x.State == PaperState.Pending);
        public WorkPaper CurrentPaper => Run.Phase == WorkPhase.Working
            ? selectedPaper ?? Run.Papers.FirstOrDefault(x => x.State == PaperState.Pending && !x.IsInFax) : null;
        public bool CanLeave => !disposed && !IsPaused && Run.Phase == WorkPhase.Working
            && (Run.Seconds >= MonthSeconds || PendingCount == 0);
        public double HoldProgress => heldPaper == null ? 0 : Math.Min(1, heldSeconds / settings.HoldSeconds);
        public int HeldPaperId => heldPaper?.Id ?? -1;
        public int HeldChoiceIndex => heldChoice;
        public double WorkSpeed { get; private set; } = 1;
        public int SkillPointsPerPaper { get; private set; }
        public double MonthSeconds => settings.MonthSeconds + Upgrades.ExtraSeconds;
        public double RemainingSeconds => Math.Max(0, MonthSeconds - Run.Seconds);
        public long SkillPointsEarnedThisMonth { get; private set; }
        public long SkillPointPenaltyThisMonth { get; private set; }

        // The plan is copied and validated once; ScriptableObjects stay untouched during play.
        public WorkSystem(WorkSettings settings, IEnumerable<WorkPaper> plannedPapers, int seed = 1)
        {
            this.settings = settings?.Copy() ?? throw new ArgumentNullException(nameof(settings));
            this.settings.Check();
            SkillPointsPerPaper = this.settings.SkillPointsPerPaper;
            random = new Random(seed);
            Fax = new WorkFaxSystem(this);
            MonthPapers = monthPapers.AsReadOnly();
            this.plannedPapers = plannedPapers?.ToArray() ?? throw new ArgumentNullException(nameof(plannedPapers));
            if (this.plannedPapers.Any(x => x == null || x.Month < 1 || x.Month > 12 || x.State != PaperState.Pending))
                throw new ArgumentException("Plan needs fresh papers for months 1 to 12.");
            if (this.plannedPapers.Select(x => x.Id).Distinct().Count() != this.plannedPapers.Length)
                throw new ArgumentException("Paper IDs must be unique.");
            this.plannedPapers = this.plannedPapers.Select(x => x.Copy()).ToArray();
        }

        public bool StartRun()
        {
            if (disposed || Run.Phase != WorkPhase.Ready) return false;
            Run.Attendance = settings.StartAttendance;
            BeginMonth(1);
            Notify();
            return true;
        }

        public void AddTime(double seconds)
        {
            if (disposed || IsPaused || Run.Phase != WorkPhase.Working) return;
            if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (seconds == 0) return;

            Run.Seconds += seconds;
            Fax.AddTime(seconds);
            if (heldPaper != null)
            {
                heldSeconds += seconds * WorkSpeed;
                if (heldSeconds >= settings.HoldSeconds) FinishHold();
            }
            Notify();
        }

        public bool StartHold(int paperId, int choiceIndex)
        {
            if (disposed || IsPaused || Run.Phase != WorkPhase.Working) return false;
            var paper = CurrentPaper;
            if (paper == null || paper.IsInFax || paper.Id != paperId || choiceIndex < 0 || choiceIndex >= paper.Choices.Count)
                return false;
            if (heldPaper == paper && heldChoice == choiceIndex) return true;

            ClearHold();
            heldPaper = paper;
            heldChoice = choiceIndex;
            Notify();
            return true;
        }

        public void StopHold()
        {
            if (disposed || heldPaper == null) return;
            ClearHold();
            Notify();
        }

        public bool SelectPaper(int paperId)
        {
            if (disposed || IsPaused || Run.Phase != WorkPhase.Working) return false;
            var paper = GetPendingPaper(paperId);
            if (paper == null || paper.IsInFax) return false;
            ClearHold();
            selectedPaper = paper;
            Notify();
            return true;
        }

        public void ClearPaper()
        {
            if (disposed || (selectedPaper == null && heldPaper == null)) return;
            ClearHold();
            selectedPaper = null;
            Notify();
        }

        public void SetPaused(bool paused)
        {
            if (disposed || Run.Phase != WorkPhase.Working || IsPaused == paused) return;
            ClearHold();
            IsPaused = paused;
            Notify();
        }

        // Future upgrades can change speed without changing month length.
        public void SetWorkSpeed(double speed)
        {
            if (disposed) return;
            if (!WorkSettings.IsPositive(speed)) throw new ArgumentOutOfRangeException(nameof(speed));
            WorkSpeed = speed;
            Notify();
        }

        // Upgrades can change the reward for future submissions.
        public void SetSkillPointsPerPaper(int points)
        {
            if (disposed) return;
            if (points < 0) throw new ArgumentOutOfRangeException(nameof(points));
            SkillPointsPerPaper = points;
            Notify();
        }

        public bool TrySpendMoney(long cost)
        {
            if (!SpendMoney(cost)) return false;
            Notify();
            return true;
        }

        // 결제와 육성 결과를 모두 반영한 다음 호출자가 한 번 알립니다.
        internal bool SpendMoney(long cost)
        {
            if (disposed || cost < 0 || Run.Money < cost) return false;
            Run.Money -= cost;
            return true;
        }

        public bool TrySpendSkillPoints(int cost)
        {
            if (disposed) return false;
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (Run.SkillPoints < cost) return false;
            Run.SkillPoints -= cost;
            Notify();
            return true;
        }

        internal bool BuyUpgrade(string id, int cost)
        {
            if (disposed || cost < 0 || Run.SkillPoints < cost || Run.BoughtUpgrades.Contains(id)) return false;
            Run.SkillPoints -= cost;
            Run.AddUpgrade(id);
            return true;
        }

        internal void ApplyUpgradeStats(WorkUpgradeStats stats)
        {
            if (disposed) return;
            Upgrades = stats;
            Notify();
        }

        public void SetSortEnabled(bool enabled) { if (!disposed) { Run.SortEnabled = enabled; Notify(); } }
        public void SetFaxVisible(bool visible) { if (!disposed) { Run.FaxVisible = visible; Notify(); } }

        public WorkPaper GetPendingPaper(int id) => Run.Papers.FirstOrDefault(x => x.Id == id && x.State == PaperState.Pending);

        internal void ReleasePaper(int id)
        {
            if (heldPaper?.Id == id) ClearHold();
            if (selectedPaper?.Id == id) selectedPaper = null;
        }

        public bool TryInstantPaper(int id)
        {
            var paper = GetPendingPaper(id);
            if (!IsWorking || paper == null || paper.IsInFax || Upgrades.InstantChance <= 0) return false;
            return random.NextDouble() < Upgrades.InstantChance && ProcessPaper(id);
        }

        internal bool ProcessPaper(int id)
        {
            var paper = GetPendingPaper(id);
            if (!IsWorking || paper == null || paper.IsInFax) return false;
            bool done = CompletePaper(paper, WorkRules.GetRightIndex(paper));
            if (done) Notify();
            return done;
        }

        public bool LeaveWork()
        {
            if (disposed || !CanLeave) return false;
            Fax.Clear();
            ClearHold();
            selectedPaper = null;
            int earlyLeaveBonus = GetEarlyLeaveBonus();
            Run.SkillPoints += earlyLeaveBonus;
            var review = Run.Month % 3 == 0 ? ReviewQuarter() : null;
            Run.Money += settings.MonthlySalary + (review?.Bonus ?? 0);
            var result = new MonthResult(Run.Month, Math.Max(0, Run.Seconds - MonthSeconds),
                rightThisMonth, wrongThisMonth, PendingCount,
                Run.Attendance, review, SkillPointsEarnedThisMonth, earlyLeaveBonus, newbieProtectionUsed,
                SkillPointPenaltyThisMonth, GetEarlyLeavePercent(), settings.MonthlySalary, Run.Money,
                Run.Seconds - MonthSeconds);
            Run.AddResult(result);
            Run.Phase = Run.Month == 12 ? WorkPhase.Finished : WorkPhase.AfterWork;
            Notify();
            return true;
        }

        public bool StartNextMonth()
        {
            if (disposed || Run.Phase != WorkPhase.AfterWork) return false;
            BeginMonth(Run.Month + 1);
            Notify();
            return true;
        }

        private void BeginMonth(int month)
        {
            Run.Month = month;
            Run.Seconds = 0;
            Run.Phase = WorkPhase.Working;
            rightThisMonth = 0;
            wrongThisMonth = 0;
            newbieProtectionUsed = false;
            SkillPointsEarnedThisMonth = 0;
            SkillPointPenaltyThisMonth = 0;
            IsPaused = false;
            selectedPaper = null;
            foreach (var paper in plannedPapers.Where(x => x.Month == month)) Run.AddPaper(paper);
            monthPapers.Clear();
            monthPapers.AddRange(Run.Papers.Where(x => x.State == PaperState.Pending));
        }

        private void FinishHold()
        {
            var paper = heldPaper;
            int choice = heldChoice;
            ClearHold(); // One press submits once, even if a frame is very long.
            CompletePaper(paper, choice);
        }

        private bool CompletePaper(WorkPaper paper, int choice)
        {
            if (!IsWorking || paper == null || paper.IsInFax || !paper.Submit(choice)) return false;
            ReleasePaper(paper.Id);
            bool protectedMistake = paper.State == PaperState.Wrong && Upgrades.WrongPenaltyReduction > 0;
            if (paper.State == PaperState.Right) rightThisMonth++;
            else
            {
                wrongThisMonth++;
                if (protectedMistake) newbieProtectionUsed = true;
                Run.Attendance = Math.Max(0, Run.Attendance - GetWrongPenalty(settings.WrongPenalty));
            }
            bool pointBoosted = false;
            int gained = paper.State == PaperState.Right
                ? GetPointReward(out pointBoosted)
                : -Math.Min(Run.SkillPoints, GetWrongPenalty(settings.WrongSkillPointPenalty));
            Run.SkillPoints += gained;
            if (gained > 0) SkillPointsEarnedThisMonth += gained;
            if (gained < 0) SkillPointPenaltyThisMonth -= gained;
            PaperSubmitted?.Invoke(new WorkSubmitResult(paper.Id, paper.State == PaperState.Right,
                rightThisMonth, wrongThisMonth, gained, protectedMistake, pointBoosted));
            return true;
        }

        private int GetWrongPenalty(int amount) =>
            (int)Math.Floor(amount * (1 - (decimal)Upgrades.WrongPenaltyReduction));

        private int GetPointReward(out bool boosted)
        {
            double multiplier = Upgrades.PointChance > 0 && random.NextDouble() < Upgrades.PointChance
                ? Upgrades.PointMultiplier : 1;
            boosted = multiplier > 1;
            decimal points = (SkillPointsPerPaper + (decimal)Upgrades.PointFlat) * (1 + (decimal)Upgrades.PointBonus) * (decimal)multiplier;
            return (int)Math.Min(int.MaxValue - Run.SkillPoints, Math.Floor(points));
        }

        private int GetEarlyLeaveBonus()
        {
            if (Upgrades.EarlyLeaveBonus <= 0 || PendingCount != 0 || RemainingSeconds <= 0) return 0;
            decimal timeRatio = Math.Min(1, (decimal)RemainingSeconds / (decimal)MonthSeconds);
            decimal bonus = SkillPointsEarnedThisMonth * timeRatio * (decimal)Upgrades.EarlyLeaveBonus;
            return (int)Math.Min(int.MaxValue - Run.SkillPoints, Math.Floor(bonus));
        }

        private double GetEarlyLeavePercent() => PendingCount == 0 && RemainingSeconds > 0
            ? Math.Min(1, RemainingSeconds / MonthSeconds) * Upgrades.EarlyLeaveBonus * 100 : 0;

        private QuarterResult ReviewQuarter()
        {
            // Original assignment month owns the score. Old backlog never enters a new quarter twice.
            var papers = Run.Papers.Where(x => x.Month >= Run.Month - 2 && x.Month <= Run.Month).ToArray();
            int right = papers.Count(x => x.State == PaperState.Right);
            var grade = WorkRules.GetGrade(right, papers.Length, settings.SPercent, settings.APercent);
            long bonus = papers.Length == 0 ? 0 : settings.GetBonus(grade);
            Run.Bonus += bonus;
            int bonusPercent = papers.Length == 0 ? 0 : settings.GetBonusPercent(grade);
            return new QuarterResult(Run.Month, right, papers.Length, grade, bonus, bonusPercent);
        }

        private void ClearHold()
        {
            heldPaper = null;
            heldChoice = -1;
            heldSeconds = 0;
        }

        internal void Notify() => Changed?.Invoke();

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Fax.Clear();
            ClearHold();
            selectedPaper = null;
            Changed = null;
            PaperSubmitted = null;
        }
    }
}
