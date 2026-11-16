using System;

namespace WorkGame
{
    [Serializable]
    public sealed class WorkSettings
    {
        public double MonthSeconds = 120;
        public double HoldSeconds = 0.5;
        public int StartAttendance = 100;
        public int WrongPenalty = 1;
        public int SPercent = 85;
        public int APercent = 70;
        public int SBonus = 300;
        public int ABonus = 200;
        public int BBonus = 100;

        public WorkSettings Copy() => (WorkSettings)MemberwiseClone();

        public void Check()
        {
            if (!IsPositive(MonthSeconds) || !IsPositive(HoldSeconds))
                throw new ArgumentException("MonthSeconds and HoldSeconds must be finite and positive.");
            if (StartAttendance < 0 || WrongPenalty < 0 || SBonus < 0 || ABonus < 0 || BBonus < 0)
                throw new ArgumentException("Attendance, penalty and bonuses cannot be negative.");
            WorkRules.GetGrade(0, 1, SPercent, APercent);
        }

        public int GetBonus(WorkGrade grade)
        {
            if (grade == WorkGrade.S) return SBonus;
            if (grade == WorkGrade.A) return ABonus;
            return BBonus;
        }

        internal static bool IsPositive(double value) => value > 0 && !double.IsInfinity(value);
    }
}
