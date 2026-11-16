using System;

namespace WorkGame
{
    public static class WorkRules
    {
        public static bool IsRight(WorkPaper paper, int choiceIndex)
        {
            if (choiceIndex < 0 || choiceIndex >= paper.Choices.Count)
                return false;

            int value = paper.Choices[choiceIndex].Value;
            foreach (var choice in paper.Choices)
            {
                if (paper.Rule == PickRule.Highest && choice.Value > value) return false;
                if (paper.Rule == PickRule.Lowest && choice.Value < value) return false;
            }
            return true;
        }

        // Empty quarters receive B and no bonus. Compare before rounding for display.
        public static WorkGrade GetGrade(int right, int total, int sPercent = 85, int aPercent = 70)
        {
            if (right < 0 || total < 0 || right > total)
                throw new ArgumentOutOfRangeException(nameof(right));
            if (aPercent < 0 || sPercent > 100 || aPercent >= sPercent)
                throw new ArgumentOutOfRangeException(nameof(sPercent));
            if (total == 0) return WorkGrade.B;
            if ((long)right * 100 >= (long)total * sPercent) return WorkGrade.S;
            if ((long)right * 100 >= (long)total * aPercent) return WorkGrade.A;
            return WorkGrade.B;
        }
    }
}
