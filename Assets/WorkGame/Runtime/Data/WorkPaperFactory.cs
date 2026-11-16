using System;
using System.Collections.Generic;

namespace WorkGame
{
    public static class WorkPaperFactory
    {
        public static List<WorkPaper> Create(WorkPlanSO plan, int choiceCount, int seed)
        {
            var random = new Random(seed);
            var papers = new List<WorkPaper>();
            foreach (var batch in plan.Batches)
            {
                var category = batch.Category;
                if (batch.Count > category.Metrics.Count)
                    throw new InvalidOperationException($"Month {batch.Month}/{category.Id}: not enough unique metrics for {batch.Count} papers.");
                var remaining = new List<WorkMetric>(category.Metrics);
                for (int i = 0; i < batch.Count; i++)
                {
                    int index = random.Next(remaining.Count);
                    var metric = remaining[index];
                    remaining.RemoveAt(index);
                    var choices = MakeChoices(random, metric, choiceCount, category.Target);
                    papers.Add(new WorkPaper(papers.Count + 1, batch.Month, category.Id, category.Title,
                        category.Target, metric.Name, metric.Unit, metric.Rule, metric.Display, choices,
                        category.ShortName, metric.ScaleMax > 0 ? metric.ScaleMax : metric.Max, metric.Title));
                }
            }
            return papers;
        }

        private static WorkChoice[] MakeChoices(Random random, WorkMetric metric, int count, string target)
        {
            // Sample without replacement; always terminates even when the range equals choice count.
            var used = new HashSet<int>();
            var values = new int[count];
            int index = 0;
            for (int top = metric.Max - count + 1; top <= metric.Max; top++)
            {
                int pick = random.Next(metric.Min, top + 1);
                int value = used.Contains(pick) ? top : pick;
                used.Add(value);
                values[index++] = value;
            }
            // Shuffle so A is not favored when the available range is small.
            for (int i = count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int temp = values[i];
                values[i] = values[j];
                values[j] = temp;
            }
            var choices = new WorkChoice[count];
            for (int i = 0; i < count; i++)
                choices[i] = new WorkChoice($"{(char)('A' + i)} {target}", values[i]);
            return choices;
        }
    }
}
