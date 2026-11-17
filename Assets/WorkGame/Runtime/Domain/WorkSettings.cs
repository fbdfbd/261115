using System;

namespace WorkGame
{
    [Serializable]
    public sealed class WorkSettings
    {
        public double MonthSeconds = 90;
        public double HoldSeconds = 0.5;
        public int StartAttendance = 100;
        public int WrongPenalty = 1;
        public int SkillPointsPerPaper = 100;
        public int WrongSkillPointPenalty = 20;
        public int MonthlySalary = 1000;
        public int SPercent = 90;
        public int APercent = 80;
        public int SBonusPercent = 300;
        public int ABonusPercent = 200;
        public int BBonusPercent = 150;

        public WorkSettings Copy() => (WorkSettings)MemberwiseClone();

        public void Check()
        {
            if (!IsPositive(MonthSeconds) || !IsPositive(HoldSeconds))
                throw new ArgumentException("MonthSeconds and HoldSeconds must be finite and positive.");
            if (StartAttendance < 0 || WrongPenalty < 0 || SBonusPercent < 0 || ABonusPercent < 0 || BBonusPercent < 0)
                throw new ArgumentException("Attendance, penalty and bonuses cannot be negative.");
            if (SkillPointsPerPaper < 0 || WrongSkillPointPenalty < 0 || MonthlySalary < 0)
                throw new ArgumentException("Skill point rewards and penalties cannot be negative.");
            WorkRules.GetGrade(0, 1, SPercent, APercent);
        }

        public long GetBonus(WorkGrade grade) => MonthlySalary * (long)GetBonusPercent(grade) / 100;

        public int GetBonusPercent(WorkGrade grade)
        {
            if (grade == WorkGrade.S) return SBonusPercent;
            if (grade == WorkGrade.A) return ABonusPercent;
            return BBonusPercent;
        }

        internal static bool IsPositive(double value) => value > 0 && !double.IsInfinity(value);
    }
}
