using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public static class WorkDataCheck
    {
        public static List<string> GetErrors(WorkConfigSO config)
        {
            var errors = new List<string>();
            if (config == null) { errors.Add("Config is missing."); return errors; }
            try
            {
                if (config.Settings == null) errors.Add("Settings are missing.");
                else config.Settings.Check();
            }
            catch (ArgumentException e) { errors.Add(e.Message); }
            if (config.ChoiceCount < 2 || config.ChoiceCount > 5) errors.Add("ChoiceCount must be 2 to 5.");
            if (config.Plan == null || config.Plan.Batches == null)
            {
                errors.Add("Month plan is missing.");
                return errors;
            }

            var categories = new Dictionary<string, WorkCategorySO>();
            var monthKeys = new HashSet<string>();
            foreach (var batch in config.Plan.Batches)
            {
                if (batch == null) { errors.Add("Empty batch."); continue; }
                if (batch.Month < 1 || batch.Month > 12 || batch.Count < 0)
                    errors.Add("Batch month must be 1 to 12 and count cannot be negative.");
                var category = batch.Category;
                if (category == null) { errors.Add($"Month {batch.Month}: category is missing."); continue; }
                if (category.Metrics != null && batch.Count > category.Metrics.Count)
                    errors.Add($"Month {batch.Month}/{category.Id}: {batch.Count} papers requested, but only {category.Metrics.Count} unique metrics are available.");
                if (!monthKeys.Add($"{batch.Month}:{category.Id}")) errors.Add($"Duplicate month/category: {batch.Month}/{category.Id}");
                if (string.IsNullOrWhiteSpace(category.Id)) { errors.Add("Category ID is empty."); continue; }
                if (categories.TryGetValue(category.Id, out var previous))
                {
                    if (previous != category) errors.Add($"Duplicate category ID: {category.Id}");
                    continue;
                }
                categories.Add(category.Id, category);
                if (string.IsNullOrWhiteSpace(category.Title) || string.IsNullOrWhiteSpace(category.Target))
                    errors.Add($"{category.Id}: title and target are required.");
                if (category.Metrics == null || category.Metrics.Count == 0)
                {
                    errors.Add($"{category.Id}: metrics are missing.");
                    continue;
                }
                var names = new HashSet<string>();
                foreach (var metric in category.Metrics)
                {
                    if (metric == null) { errors.Add($"{category.Id}: empty metric."); continue; }
                    if (string.IsNullOrWhiteSpace(metric.Name) || !names.Add(metric.Name))
                        errors.Add($"{category.Id}: empty or duplicate metric name.");
                    if ((long)metric.Max - metric.Min + 1 < config.ChoiceCount || metric.Max == int.MaxValue)
                        errors.Add($"{category.Id}/{metric.Name}: range needs at least ChoiceCount unique values; Max must be below int.MaxValue.");
                    if (!Enum.IsDefined(typeof(PickRule), metric.Rule) || !Enum.IsDefined(typeof(PaperDisplay), metric.Display))
                        errors.Add($"{category.Id}/{metric.Name}: invalid rule or display.");
                    if (metric.ScaleMax < 0)
                        errors.Add($"{category.Id}/{metric.Name}: ScaleMax cannot be negative.");
                    if ((metric.Display == PaperDisplay.Stars || metric.Display == PaperDisplay.Grade)
                        && (metric.Min < 1 || metric.Max > 5))
                        errors.Add($"{category.Id}/{metric.Name}: stars and grades must use values 1 to 5.");
                    if (metric.Display == PaperDisplay.HorizontalBar || metric.Display == PaperDisplay.VerticalBar)
                    {
                        int scaleMax = metric.ScaleMax > 0 ? metric.ScaleMax : metric.Max;
                        if (metric.Min < 0 || scaleMax <= 0 || scaleMax < metric.Max)
                            errors.Add($"{category.Id}/{metric.Name}: bars need nonnegative values and a scale covering Max.");
                    }
                }
            }
            return errors;
        }
    }
}
