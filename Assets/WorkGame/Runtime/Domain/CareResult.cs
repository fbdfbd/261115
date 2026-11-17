using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public sealed class CarePreview
    {
        public long Cost { get; }
        public IReadOnlyList<string> Lessons { get; }
        public IReadOnlyList<string> Synergies { get; }
        public IReadOnlyList<CareGain> LessonGains { get; }
        public IReadOnlyList<CareGain> SynergyGains { get; }
        public IReadOnlyList<CareGain> GiftGains { get; }
        public IReadOnlyList<CareGain> TotalGains { get; }
        public int GetGain(CareStat stat) => (int)Math.Min(int.MaxValue, TotalGains
            .Where(x => x.Stat == stat || (CareStats.IsMain(stat) && CareStats.GetMain(x.Stat) == stat))
            .Sum(x => (long)x.Amount));
        internal CarePreview(long cost, string[] lessons, string[] synergies,
            CareGain[] lessonGains, CareGain[] synergyGains, CareGain[] giftGains, CareGain[] totalGains)
        {
            Cost = cost; Lessons = Array.AsReadOnly(lessons); Synergies = Array.AsReadOnly(synergies);
            LessonGains = Array.AsReadOnly(lessonGains); SynergyGains = Array.AsReadOnly(synergyGains);
            GiftGains = Array.AsReadOnly(giftGains); TotalGains = Array.AsReadOnly(totalGains);
        }
    }

    public sealed class CareWeekResult
    {
        public int Week { get; }
        public string LessonId { get; }
        public IReadOnlyList<CareGain> Gains { get; }
        public int GetGain(CareStat stat) => (int)Math.Min(int.MaxValue, Gains
            .Where(gain => gain.Stat == stat || (CareStats.IsMain(stat) && CareStats.GetMain(gain.Stat) == stat))
            .Sum(gain => (long)gain.Amount));
        internal CareWeekResult(int week, string lessonId, CareGain[] gains)
        {
            Week = week; LessonId = lessonId; Gains = Array.AsReadOnly(gains);
        }
    }

    public sealed class CareMonthResult
    {
        public int Month { get; }
        public CarePreview Schedule { get; }
        public long MoneyBalance { get; }
        public int Friendship { get; }
        public IReadOnlyList<CareGain> Stats { get; }
        public IReadOnlyList<CareMainNeed> MainGrades { get; }
        public IReadOnlyList<CareWeekResult> Weeks { get; }
        internal CareMonthResult(int month, CarePreview schedule, long money, CareRun run, CareGradeSettings grades,
            IEnumerable<CareWeekResult> weeks = null)
        {
            Month = month; Schedule = schedule; MoneyBalance = money; Friendship = run.Friendship;
            Weeks = Array.AsReadOnly((weeks ?? Array.Empty<CareWeekResult>()).ToArray());
            Stats = Array.AsReadOnly(Enum.GetValues(typeof(CareStat)).Cast<CareStat>()
                .Select(stat => new CareGain(stat, run.GetStat(stat))).ToArray());
            MainGrades = Array.AsReadOnly(Enumerable.Range(0, 4).Select(index => new CareMainNeed
                { Stat = (CareStat)index, Grade = grades.GetGrade(run.GetStat((CareStat)index)) }).ToArray());
        }
    }

    public sealed class CareEndingResult
    {
        public CareEnding Ending { get; }
        public CareFriendship Friendship { get; }
        public string FriendshipText { get; }
        internal CareEndingResult(CareEnding ending, CareFriendship friendship)
        {
            Ending = ending; Friendship = friendship;
            FriendshipText = friendship == CareFriendship.Low ? ending.LowText
                : friendship == CareFriendship.High ? ending.HighText : ending.NormalText;
        }
    }
}
