namespace WorkGame
{
    public sealed class QuarterResult
    {
        public int Month { get; }
        public int Right { get; }
        public int Total { get; }
        public WorkGrade Grade { get; }
        public int Bonus { get; }
        public double RightPercent => Total == 0 ? 0 : Right * 100.0 / Total;

        public QuarterResult(int month, int right, int total, WorkGrade grade, int bonus)
        {
            Month = month;
            Right = right;
            Total = total;
            Grade = grade;
            Bonus = bonus;
        }
    }

    // Hand this to the future care/scheduling part after leaving work.
    public sealed class MonthResult
    {
        public int Month { get; }
        public double OvertimeSeconds { get; }
        public int Right { get; }
        public int Wrong { get; }
        public int Pending { get; }
        public int Attendance { get; }
        public QuarterResult Review { get; }

        public MonthResult(int month, double overtimeSeconds, int right, int wrong,
            int pending, int attendance, QuarterResult review)
        {
            Month = month;
            OvertimeSeconds = overtimeSeconds;
            Right = right;
            Wrong = wrong;
            Pending = pending;
            Attendance = attendance;
            Review = review;
        }
    }
}
