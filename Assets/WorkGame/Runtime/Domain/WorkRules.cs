using System;

namespace WorkGame
{
    public static class WorkRules
    {
        public static int GetRightIndex(WorkPaper paper)
        {
            int best = 0;
            for (int i = 1; i < paper.Choices.Count; i++)
                if (paper.Rule == PickRule.Highest ? paper.Choices[i].Value > paper.Choices[best].Value
                    : paper.Choices[i].Value < paper.Choices[best].Value) best = i;
            return best;
        }

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
        public static WorkGrade GetGrade(int right, int total, int sPercent = 90, int aPercent = 80)
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
