using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WorkGame.Editor
{
    public sealed class WorkCsvImport
    {
        private readonly bool isCategories;
        private readonly List<Dictionary<string, string>> rows;
        public string Preview { get; }

        public WorkCsvImport(string text, bool isCategories)
        {
            this.isCategories = isCategories;
            rows = isCategories
                ? ReadCategories(text)
                : WorkCsv.Read(text, "Month", "CategoryId", "Count");
            Preview = isCategories
                ? string.Join("\n", rows.GroupBy(x => x["Id"]).Select(x => $"{x.Key}: {x.First()["Title"]} / {x.Count()} metrics (replace)"))
                : string.Join("\n", rows.Select(x => $"Month {x["Month"]} / {x["CategoryId"]} → {x["Count"]} papers"));
        }

        public void Check(WorkConfigSO config)
        {
            if (config == null || config.Plan == null) throw new InvalidOperationException("Config and Plan are required.");
            var tempObjects = new List<ScriptableObject>();
            try
            {
                var categories = GetCategories(config);
                if (isCategories)
                {
                    foreach (var group in rows.GroupBy(x => x["Id"]))
                    {
                        CheckId(group.Key);
                        var category = ScriptableObject.CreateInstance<WorkCategorySO>();
                        tempObjects.Add(category);
                        FillCategory(category, group.ToList());
                        categories[group.Key] = category;
                    }
                }
                var plan = ScriptableObject.CreateInstance<WorkPlanSO>();
                tempObjects.Add(plan);
                plan.RandomizeCategories = config.Plan.RandomizeCategories;
                plan.Batches = config.Plan.Batches.Select(x => new WorkBatch {
                    Month = x.Month, Count = x.Count, Category = x.Category == null ? null : categories[x.Category.Id]
                }).ToList();
                if (!isCategories) FillPlan(plan, categories);

                // Check imported categories even if the current plan does not use them yet.
                foreach (var category in categories.Values.Where(x => !plan.Batches.Any(b => b.Category == x)))
                    plan.Batches.Add(new WorkBatch { Month = 12, Category = category, Count = 0 });
                var tempConfig = ScriptableObject.CreateInstance<WorkConfigSO>();
                tempObjects.Add(tempConfig);
                tempConfig.Settings = config.Settings;
                tempConfig.ChoiceCount = config.ChoiceCount;
                tempConfig.Plan = plan;
                var errors = tempConfig.GetErrors();
                if (errors.Count > 0) throw new FormatException(string.Join("\n", errors));
            }
            finally { foreach (var item in tempObjects) UnityEngine.Object.DestroyImmediate(item); }
        }

        public void Apply(WorkConfigSO config)
        {
            Check(config); // Finish validation before changing any assets.
            var categories = GetCategories(config);
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Import Work CSV");
            if (isCategories)
            {
                Directory.CreateDirectory(WorkDataMaker.Folder);
                AssetDatabase.Refresh();
                foreach (var group in rows.GroupBy(x => x["Id"]))
                {
                    if (!categories.TryGetValue(group.Key, out var category))
                    {
                        string path = AssetDatabase.GenerateUniqueAssetPath($"{WorkDataMaker.Folder}/Category_{group.Key}.asset");
                        category = WorkDataMaker.LoadOrCreate<WorkCategorySO>(path);
                        Undo.RegisterCreatedObjectUndo(category, "Import Work Category");
                    }
                    else Undo.RecordObject(category, "Import Work Category");
                    FillCategory(category, group.ToList());
                    EditorUtility.SetDirty(category);
                }
            }
            else
            {
                Undo.RecordObject(config.Plan, "Import Work Plan");
                FillPlan(config.Plan, categories);
                EditorUtility.SetDirty(config.Plan);
            }
            Undo.CollapseUndoOperations(undoGroup);
            AssetDatabase.SaveAssets();
        }

        private static Dictionary<string, WorkCategorySO> GetCategories(WorkConfigSO config)
        {
            return WorkDataMaker.FindCategories().Concat(config.Plan.Batches.Where(x => x.Category != null).Select(x => x.Category))
                .Distinct().ToDictionary(x => x.Id);
        }

        internal static void FillCategory(WorkCategorySO category, List<Dictionary<string, string>> values)
        {
            var first = values[0];
            if (values.Any(x => x["Title"] != first["Title"] || x["Target"] != first["Target"]))
                throw new FormatException($"{first["Id"]}: Title/Target differ between rows.");
            category.Id = first["Id"];
            category.Title = first["Title"];
            if (first.TryGetValue("ShortName", out string shortName))
            {
                if (values.Any(x => x["ShortName"] != shortName))
                    throw new FormatException($"{first["Id"]}: ShortName differs between rows.");
                category.ShortName = shortName;
            }
            category.Target = first["Target"];
            category.Metrics = values.Select(x => new WorkMetric {
                Title = x.TryGetValue("PaperTitle", out string title) ? title : "",
                Agency = x.TryGetValue("Agency", out string agency) ? agency : "",
                Difficulty = x.TryGetValue("Difficulty", out string difficulty) && !string.IsNullOrWhiteSpace(difficulty)
                    ? ReadEnum<WorkDifficulty>(difficulty) : WorkDifficulty.Normal,
                HighlightWords = x.TryGetValue("HighlightWords", out string words)
                    ? words.Split('|').Where(w => !string.IsNullOrWhiteSpace(w)).Select(w => w.Trim()).ToList()
                    : new List<string>(),
                Name = x["Metric"], Unit = x["Unit"], Rule = ReadEnum<PickRule>(x["Rule"]),
                Display = ReadEnum<PaperDisplay>(x["Display"]), Min = ReadInt(x["Min"]), Max = ReadInt(x["Max"]),
                ScaleMax = x.TryGetValue("ScaleMax", out string scale) ? ReadInt(scale) : 0
            }).ToList();
        }

        private void FillPlan(WorkPlanSO plan, Dictionary<string, WorkCategorySO> categories)
        {
            var keys = new HashSet<string>();
            foreach (var row in rows)
            {
                int month = ReadInt(row["Month"]);
                if (!categories.TryGetValue(row["CategoryId"], out var category))
                    throw new FormatException($"Unknown CategoryId: {row["CategoryId"]}");
                if (!keys.Add($"{month}:{category.Id}")) throw new FormatException("Duplicate month/category in CSV.");
                var batch = plan.Batches.FirstOrDefault(x => x.Month == month && x.Category == category);
                if (batch == null)
                {
                    batch = new WorkBatch { Month = month, Category = category };
                    plan.Batches.Add(batch);
                }
                batch.Count = ReadInt(row["Count"]);
            }
        }

        internal static List<Dictionary<string, string>> ReadCategories(string text)
        {
            var headers = new List<string> { "Id", "Title" };
            if (WorkCsv.HasColumn(text, "ShortName")) headers.Add("ShortName");
            headers.Add("Target");
            if (WorkCsv.HasColumn(text, "PaperTitle")) headers.Add("PaperTitle");
            headers.AddRange(new[] { "Metric", "Unit", "Rule", "Display", "Min", "Max" });
            if (WorkCsv.HasColumn(text, "ScaleMax")) headers.Add("ScaleMax");
            if (WorkCsv.HasColumn(text, "HighlightWords")) headers.Add("HighlightWords");
            if (WorkCsv.HasColumn(text, "Agency")) headers.Add("Agency");
            if (WorkCsv.HasColumn(text, "Difficulty")) headers.Add("Difficulty");
            return WorkCsv.Read(text, headers.ToArray());
        }

        private static int ReadInt(string value)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                throw new FormatException($"Not an integer: {value}");
            return result;
        }

        private static T ReadEnum<T>(string value) where T : struct
        {
            if (!Enum.TryParse(value, true, out T result) || !Enum.IsDefined(typeof(T), result))
                throw new FormatException($"Unknown {typeof(T).Name}: {value}");
            return result;
        }

        private static void CheckId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Any(x => !char.IsLetterOrDigit(x) && x != '_' && x != '-'))
                throw new FormatException("Category ID may contain letters, digits, underscore and dash only.");
        }
    }
}
