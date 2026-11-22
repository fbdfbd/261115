using System;
using System.Linq;

namespace WorkGame
{
    [Serializable]
    public sealed class CareEndingGrowthText
    {
        public string[] Main = Array.Empty<string>();
        public string[] Detail = Array.Empty<string>();
        public string[] Balanced = Array.Empty<string>();
        public string[] Low = Array.Empty<string>();
    }

    public static class CareGrowthNarration
    {
        public static string[] Build(CareRun run, int mainThreshold, CareEndingGrowthText text)
        {
            if (text?.Main?.Length != 4 || text.Detail?.Length != 12) return Array.Empty<string>();
            var totals = Enumerable.Range(0, 4).Select(index => run.GetStat((CareStat)index)).ToArray();
            int highest = totals.Max();
            if (highest < mainThreshold) return text.Low ?? Array.Empty<string>();
            var leading = Enumerable.Range(0, 4).Where(index => totals[index] >= highest * 0.9).ToArray();
            if (leading.Length == 4) return text.Balanced ?? Array.Empty<string>();
            // Close scores describe multiple strengths instead of breaking a tie in favor of Knowledge.
            if (leading.Length > 1)
                return leading.Select(index => text.Main[index].Split('\n')[0]).ToArray();
            int main = leading[0];
            int detail = Enumerable.Range(main * 3, 3)
                .OrderByDescending(index => run.GetStat((CareStat)(index + 4))).First();
            return text.Main[main].Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Concat(new[] { text.Detail[detail] }).ToArray();
        }
    }
}
