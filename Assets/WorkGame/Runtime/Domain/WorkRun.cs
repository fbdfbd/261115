using System.Collections.Generic;

namespace WorkGame
{
    public sealed class WorkRun
    {
        private readonly List<WorkPaper> papers = new List<WorkPaper>();
        private readonly List<MonthResult> months = new List<MonthResult>();

        public int Month { get; internal set; }
        public double Seconds { get; internal set; }
        public int Attendance { get; internal set; }
        public int Bonus { get; internal set; }
        public WorkPhase Phase { get; internal set; } = WorkPhase.Ready;
        public IReadOnlyList<WorkPaper> Papers { get; }
        public IReadOnlyList<MonthResult> Months { get; }
        public MonthResult LastResult => months.Count == 0 ? null : months[months.Count - 1];

        public WorkRun()
        {
            Papers = papers.AsReadOnly();
            Months = months.AsReadOnly();
        }

        internal void AddPaper(WorkPaper paper) => papers.Add(paper);
        internal void AddResult(MonthResult result) => months.Add(result);
    }
}
