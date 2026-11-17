using System.Collections.Generic;

namespace WorkGame
{
    public sealed class WorkRun
    {
        private readonly List<WorkPaper> papers = new List<WorkPaper>();
        private readonly List<MonthResult> months = new List<MonthResult>();
        private readonly List<string> boughtUpgrades = new List<string>();

        public int Month { get; internal set; }
        public double Seconds { get; internal set; }
        public int Attendance { get; internal set; }
        public long Bonus { get; internal set; }
        public long Money { get; internal set; }
        public int SkillPoints { get; internal set; }
        public IReadOnlyList<string> BoughtUpgrades { get; }
        public bool SortEnabled { get; internal set; } = true;
        public bool FaxVisible { get; internal set; } = true;
        public WorkPhase Phase { get; internal set; } = WorkPhase.Ready;
        public IReadOnlyList<WorkPaper> Papers { get; }
        public IReadOnlyList<MonthResult> Months { get; }
        public MonthResult LastResult => months.Count == 0 ? null : months[months.Count - 1];

        public WorkRun()
        {
            Papers = papers.AsReadOnly();
            Months = months.AsReadOnly();
            BoughtUpgrades = boughtUpgrades.AsReadOnly();
        }

        internal void AddPaper(WorkPaper paper) => papers.Add(paper);
        internal void AddResult(MonthResult result) => months.Add(result);
        internal void AddUpgrade(string id) => boughtUpgrades.Add(id);
    }
}
