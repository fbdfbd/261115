namespace WorkGame
{
    public sealed class QuarterResult
    {
        public int Month { get; }
        public int Right { get; }
        public int Total { get; }
        public WorkGrade Grade { get; }
        public long Bonus { get; }
        public int BonusPercent { get; }
        public double RightPercent => Total == 0 ? 0 : Right * 100.0 / Total;

        public QuarterResult(int month, int right, int total, WorkGrade grade, long bonus, int bonusPercent = 0)
        {
            Month = month;
            Right = right;
            Total = total;
            Grade = grade;
            Bonus = bonus;
            BonusPercent = bonusPercent;
        }
    }

    // Hand this to the future care/scheduling part after leaving work.
    public sealed class MonthResult
    {
        public int Month { get; }
        public double OvertimeSeconds { get; }
        public double LeaveDifference { get; }
        public int Right { get; }
        public int Wrong { get; }
        public int Pending { get; }
        public int Attendance { get; }
        public QuarterResult Review { get; }
        public long SkillPointsEarned { get; }
        public int EarlyLeaveBonus { get; }
        public bool NewbieProtectionUsed { get; }
        public long SkillPointPenalty { get; }
        public double ExtraPointPercent { get; }
        public long SkillPointTotal => SkillPointsEarned - SkillPointPenalty + EarlyLeaveBonus;
        public int Salary { get; }
        public long MoneyTotal => Salary + (Review?.Bonus ?? 0);
        public long MoneyBalance { get; }

        public MonthResult(int month, double overtimeSeconds, int right, int wrong,
            int pending, int attendance, QuarterResult review, long skillPointsEarned = 0,
            int earlyLeaveBonus = 0, bool newbieProtectionUsed = false,
            long skillPointPenalty = 0, double extraPointPercent = 0, int salary = 0, long moneyBalance = 0,
            double leaveDifference = 0)
        {
            Month = month;
            OvertimeSeconds = overtimeSeconds;
            LeaveDifference = leaveDifference;
            Right = right;
            Wrong = wrong;
            Pending = pending;
            Attendance = attendance;
            Review = review;
            SkillPointsEarned = skillPointsEarned;
            EarlyLeaveBonus = earlyLeaveBonus;
            NewbieProtectionUsed = newbieProtectionUsed;
            SkillPointPenalty = skillPointPenalty;
            ExtraPointPercent = extraPointPercent;
            Salary = salary;
            MoneyBalance = moneyBalance;
        }
    }
}
