using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    // 순서는 저장 데이터의 ID입니다. 중간에 끼우지 않고 뒤에 추가합니다.
    public enum CareStat
    {
        Knowledge, Fitness, Emotion, Social,
        Logic, Research, Focus, Strength, Endurance, Reflex,
        Imagination, Expression, Empathy, Speech, Teamwork, Leadership
    }

    public enum CareFriendship { Low, Normal, High }
    public enum CareGrade { None, B, A, S }
    public enum CareGiftKind { Desk, Equipment, Chair }

    public static class CareGradeNames
    {
        public static string GetName(CareGrade grade) => grade == CareGrade.S ? "S"
            : grade == CareGrade.A ? "A" : grade == CareGrade.B ? "B" : "C";
    }

    public readonly struct CareGradeProgress
    {
        public CareGrade Grade { get; }
        public double Ratio { get; }
        public CareGradeProgress(CareGrade grade, double ratio) { Grade = grade; Ratio = ratio; }
    }

    [Serializable]
    public sealed class CareGradeSettings
    {
        public int B = 100;
        public int A = 200;
        public int S = 350;
        public CareGrade GetGrade(int value) => value >= S ? CareGrade.S
            : value >= A ? CareGrade.A : value >= B ? CareGrade.B : CareGrade.None;
        public CareGradeProgress GetProgress(double value)
        {
            if (value >= S) return new CareGradeProgress(CareGrade.S, 1);
            if (value >= A) return new CareGradeProgress(CareGrade.A, (value - A) / (S - A));
            if (value >= B) return new CareGradeProgress(CareGrade.B, (value - B) / (A - B));
            return new CareGradeProgress(CareGrade.None, Math.Max(0, value) / B);
        }
        internal CareGradeSettings Copy() => (CareGradeSettings)MemberwiseClone();
        internal void Check()
        {
            if (B <= 0 || B >= A || A >= S) throw new ArgumentException("대표 등급 기준은 0 < B < A < S 순서여야 합니다.");
        }
    }

    [Serializable]
    public sealed class CareMainNeed
    {
        public CareStat Stat;
        public CareGrade Grade = CareGrade.B;
        internal CareMainNeed Copy() => new CareMainNeed { Stat = Stat, Grade = Grade };
    }

    public static class CareStats
    {
        public static bool IsMain(CareStat stat) => (int)stat >= 0 && (int)stat < 4;
        public static CareStat GetMain(CareStat detail)
        {
            if ((int)detail < 4 || (int)detail > 15) throw new ArgumentOutOfRangeException(nameof(detail));
            return (CareStat)(((int)detail - 4) / 3);
        }
    }

    public static class CareStatNames
    {
        private static readonly string[] names =
        {
            "지식", "체력", "감성", "사교", "논리력", "탐구력", "집중력", "근력",
            "지구력", "순발력", "상상력", "표현력", "공감력", "화술", "협동심", "리더십"
        };
        public static string GetName(CareStat stat)
        {
            int index = (int)stat;
            if (index < 0 || index >= names.Length) throw new ArgumentOutOfRangeException(nameof(stat));
            return names[index];
        }
    }

    [Serializable]
    public sealed class CareGain
    {
        public CareStat Stat;
        public int Amount;
        public CareGain() { }
        public CareGain(CareStat stat, int amount) { Stat = stat; Amount = amount; }
        internal CareGain Copy() => new CareGain(Stat, Amount);
    }

    [Serializable]
    public sealed class CareGiftBonus
    {
        public CareStat Stat;
        public int Percent;
        internal CareGiftBonus Copy() => new CareGiftBonus { Stat = Stat, Percent = Percent };
    }

    public sealed class CareLesson
    {
        public string Id { get; }
        public string Name { get; }
        public int UnlockMonth { get; }
        public int Cost { get; }
        public IReadOnlyList<CareGain> Gains { get; }
        public CareLesson(string id, string name, int month, int cost, IEnumerable<CareGain> gains)
        {
            Id = id; Name = name; UnlockMonth = month; Cost = cost;
            Gains = Array.AsReadOnly(gains.Select(x => x.Copy()).ToArray());
        }
    }

    public sealed class CareSynergy
    {
        public string Id { get; }
        public string Name { get; }
        // 같은 ID를 2번 지정하면 해당 수업 2회가 조건입니다.
        public IReadOnlyList<string> Lessons { get; }
        public IReadOnlyList<CareGain> Gains { get; }
        public CareSynergy(string id, string name, IEnumerable<string> lessons, IEnumerable<CareGain> gains)
        {
            Id = id; Name = name;
            Lessons = Array.AsReadOnly(lessons.ToArray());
            Gains = Array.AsReadOnly(gains.Select(x => x.Copy()).ToArray());
        }
    }

    public sealed class CareGift
    {
        public string Id { get; }
        public string Name { get; }
        public CareGiftKind Kind { get; }
        public int Cost { get; }
        public int Friendship { get; }
        public IReadOnlyList<CareGiftBonus> Bonuses { get; }
        public CareGift(string id, string name, CareGiftKind kind, int cost, int friendship, IEnumerable<CareGiftBonus> bonuses)
        {
            Id = id; Name = name; Kind = kind; Cost = cost; Friendship = friendship;
            Bonuses = Array.AsReadOnly(bonuses.Select(x => x.Copy()).ToArray());
        }
    }

    public sealed class CareEnding
    {
        public string Id { get; }
        public string Name { get; }
        public int Priority { get; }
        public IReadOnlyList<CareGain> Needs { get; }
        public IReadOnlyList<CareMainNeed> MainNeeds { get; }
        public string Text { get; }
        public string LowText { get; }
        public string NormalText { get; }
        public string HighText { get; }
        public CareEnding(string id, string name, int priority, IEnumerable<CareGain> needs,
            string text, string lowText, string normalText, string highText, IEnumerable<CareMainNeed> mainNeeds = null)
        {
            Id = id; Name = name; Priority = priority;
            MainNeeds = Array.AsReadOnly((mainNeeds ?? Array.Empty<CareMainNeed>()).Select(x => x.Copy()).ToArray());
            Needs = Array.AsReadOnly(needs.Select(x => x.Copy()).ToArray());
            Text = text; LowText = lowText; NormalText = normalText; HighText = highText;
        }
    }

    public sealed class CareRun
    {
        private readonly int[] stats = new int[12];
        private readonly List<string> gifts = new List<string>();
        private readonly string[] activeGifts = new string[3];
        public int Friendship { get; internal set; }
        public IReadOnlyList<string> Gifts { get; }
        public IReadOnlyList<string> ActiveGifts { get; }
        public int FinishedMonth { get; internal set; }
        public CareMonthResult LastResult { get; internal set; }
        public CareEndingResult Ending { get; internal set; }
        public int StatMax { get; }
        public CareRun(int statMax = 300)
        {
            if (statMax <= 0) throw new ArgumentOutOfRangeException(nameof(statMax));
            StatMax = statMax;
            Gifts = gifts.AsReadOnly(); ActiveGifts = Array.AsReadOnly(activeGifts);
        }
        public int GetStat(CareStat stat)
        {
            if (!Enum.IsDefined(typeof(CareStat), stat)) throw new ArgumentOutOfRangeException(nameof(stat));
            if (!CareStats.IsMain(stat)) return stats[(int)stat - 4];
            int first = (int)stat * 3;
            return (int)Math.Min(int.MaxValue, (long)stats[first] + stats[first + 1] + stats[first + 2]);
        }
        internal void AddStats(IEnumerable<CareGain> gains)
        {
            foreach (var gain in gains)
            {
                int index = (int)gain.Stat - 4;
                stats[index] = (int)Math.Min(StatMax, stats[index] + (long)gain.Amount);
            }
        }
        internal void AddGift(string id) => gifts.Add(id);
        public string GetActiveGift(CareGiftKind kind)
        {
            if (!Enum.IsDefined(typeof(CareGiftKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            return activeGifts[(int)kind];
        }
        internal void SetActiveGift(CareGiftKind kind, string id) => activeGifts[(int)kind] = id;
    }
}
