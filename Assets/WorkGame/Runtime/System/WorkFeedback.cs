using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    // Chooses text only. It knows nothing about UI, animations or scene objects.
    public sealed class WorkFeedback
    {
        private readonly Dictionary<(FeedbackResult, int), WorkFeedbackLine[]> pools;
        private readonly Dictionary<(FeedbackResult, int), int> lastPicks = new Dictionary<(FeedbackResult, int), int>();
        private readonly int[] successCounts;
        private readonly int[] failureCounts;
        private readonly Random random;

        public WorkFeedback(IEnumerable<WorkFeedbackLine> lines, int[] successCounts, int[] failureCounts, int seed)
        {
            CheckCounts(successCounts, 5);
            CheckCounts(failureCounts, 3);
            this.successCounts = (int[])successCounts.Clone();
            this.failureCounts = (int[])failureCounts.Clone();
            random = new Random(seed);
            var copy = lines?.ToArray() ?? throw new ArgumentNullException(nameof(lines));
            var ids = new HashSet<string>();
            foreach (var line in copy)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.Id) || !ids.Add(line.Id)
                    || string.IsNullOrWhiteSpace(line.Text) || !Enum.IsDefined(typeof(FeedbackResult), line.Result)
                    || line.Tier < 1 || line.Tier > (line.Result == FeedbackResult.Success ? 5 : 3))
                    throw new ArgumentException("Feedback needs unique IDs, text, a valid result and tier.");
            }
            pools = copy.GroupBy(x => (x.Result, x.Tier)).ToDictionary(x => x.Key, x => x.ToArray());
            foreach (FeedbackResult result in Enum.GetValues(typeof(FeedbackResult)))
                for (int tier = 1; tier <= (result == FeedbackResult.Success ? 5 : 3); tier++)
                    if (!pools.ContainsKey((result, tier)))
                        throw new ArgumentException($"Feedback pool is missing: {result}/{tier}.");
        }

        public WorkFeedbackMessage Pick(WorkSubmitResult submitted)
        {
            var result = submitted.IsRight ? FeedbackResult.Success : FeedbackResult.Failure;
            int tier = GetTier(submitted.IsRight ? submitted.RightCount : submitted.WrongCount,
                submitted.IsRight ? successCounts : failureCounts);
            var key = (result, tier);
            var pool = pools[key];
            int pick;
            if (pool.Length > 1 && lastPicks.TryGetValue(key, out int last))
            {
                pick = random.Next(pool.Length - 1);
                if (pick >= last) pick++;
            }
            else pick = random.Next(pool.Length);
            lastPicks[key] = pick;
            return new WorkFeedbackMessage(result, pool[pick].Text);
        }

        private static int GetTier(int count, int[] steps)
        {
            for (int i = steps.Length - 1; i >= 0; i--)
                if (count >= steps[i]) return i + 1;
            return 1;
        }

        private static void CheckCounts(int[] counts, int length)
        {
            if (counts == null || counts.Length != length || counts[0] != 1)
                throw new ArgumentException($"Feedback thresholds need {length} steps, starting at 1.");
            for (int i = 1; i < counts.Length; i++)
                if (counts[i] <= counts[i - 1])
                    throw new ArgumentException("Feedback thresholds must increase.");
        }
    }
}
