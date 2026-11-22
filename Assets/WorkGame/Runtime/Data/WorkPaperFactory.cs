using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public static class WorkPaperFactory
    {
        public static List<WorkPaper> Create(WorkPlanSO plan, int choiceCount, int seed)
        {
            var random = new Random(seed);
            var papers = new List<WorkPaper>();
            foreach (var batch in CreateBatches(plan, random))
            {
                var category = batch.Category;
                var remaining = GetAvailableMetrics(category, batch.Month).ToList();
                if (batch.Count > remaining.Count)
                    throw new InvalidOperationException($"Month {batch.Month}/{category.Id}: not enough eligible unique metrics for {batch.Count} papers ({remaining.Count} available).");
                for (int i = 0; i < batch.Count; i++)
                {
                    int index = random.Next(remaining.Count);
                    var metric = remaining[index];
                    remaining.RemoveAt(index);
                    var choices = MakeChoices(random, metric, choiceCount, category.Target);
                    papers.Add(new WorkPaper(papers.Count + 1, batch.Month, category.Id, category.Title,
                        category.Target, metric.Name, metric.Unit, metric.Rule, metric.Display, choices,
                        category.ShortName, metric.ScaleMax > 0 ? metric.ScaleMax : metric.Max, metric.Title, metric.HighlightWords, metric.Agency));
                }
            }
            return papers;
        }

        public static int CategoryCountForMonth(int month) => month <= 3 ? 1 : month <= 6 ? 2 : 3;

        // Keep the original draw and IDs unchanged for old saves. Extra papers use a separate draw.
        // The system assigns only the purchased percentage at the start of each month.
        public static List<WorkPaper> CreateReserve(WorkPlanSO plan, int choiceCount, int seed,
            IReadOnlyList<WorkPaper> originals)
        {
            var result = new List<WorkPaper>();
            var categories = plan.Batches.Select(x => x.Category).Distinct().ToArray();
            int nextId = originals.Count == 0 ? 1 : originals.Max(x => x.Id) + 1;
            foreach (var month in originals.GroupBy(x => x.Month).OrderBy(x => x.Key))
            {
                int count = (int)Math.Floor(month.Count() * 0.3m);
                var random = new Random(unchecked(seed * 397 ^ month.Key * 7919 ^ 104729));
                var selected = month.Select(x => x.CategoryId).ToHashSet();
                var used = month.Select(x => x.CategoryId + "\n" + x.Metric).ToHashSet();
                var available = categories.ToDictionary(x => x,
                    x => GetAvailableMetrics(x, month.Key).Where(m => !used.Contains(x.Id + "\n" + m.Name)).ToList());
                for (int i = 0; i < count; i++)
                {
                    var pool = categories.Where(x => selected.Contains(x.Id) && available[x].Count > 0).ToArray();
                    if (pool.Length == 0) pool = categories.Where(x => available[x].Count > 0).ToArray();
                    if (pool.Length == 0)
                        throw new InvalidOperationException($"Month {month.Key}: 추가 업무에 사용할 고유 항목이 부족합니다.");
                    var category = pool[random.Next(pool.Length)];
                    var metrics = available[category];
                    int index = random.Next(metrics.Count);
                    var metric = metrics[index]; metrics.RemoveAt(index);
                    result.Add(new WorkPaper(nextId++, month.Key, category.Id, category.Title, category.Target,
                        metric.Name, metric.Unit, metric.Rule, metric.Display,
                        MakeChoices(random, metric, choiceCount, category.Target), category.ShortName,
                        metric.ScaleMax > 0 ? metric.ScaleMax : metric.Max, metric.Title, metric.HighlightWords, metric.Agency));
                }
            }
            return result;
        }

        internal static IEnumerable<WorkMetric> GetAvailableMetrics(WorkCategorySO category, int month) =>
            (category.Metrics ?? Enumerable.Empty<WorkMetric>())
                .Where(metric => metric != null && (month > 3 || metric.Difficulty == WorkDifficulty.Easy));

        private static IEnumerable<WorkBatch> CreateBatches(WorkPlanSO plan, Random random)
        {
            if (!plan.RandomizeCategories)
            {
                foreach (var batch in plan.Batches) yield return batch;
                yield break;
            }

            // All registered categories form the pool, regardless of their authored month.
            var pool = plan.Batches.Select(x => x.Category).Distinct().ToArray();
            foreach (var month in plan.Batches.GroupBy(x => x.Month).OrderBy(x => x.Key))
            {
                int total = month.Sum(x => x.Count);
                if (total == 0) continue;
                int count = CategoryCountForMonth(month.Key);
                if (pool.Length < count || total < count)
                    throw new InvalidOperationException($"Month {month.Key}: need {count} categories and at least {count} papers.");
                var remaining = new List<WorkCategorySO>(pool);
                for (int i = 0; i < count; i++)
                {
                    int index = random.Next(remaining.Count);
                    var category = remaining[index];
                    remaining.RemoveAt(index);
                    yield return new WorkBatch { Month = month.Key, Category = category,
                        Count = total / count + (i < total % count ? 1 : 0) };
                }
            }
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
