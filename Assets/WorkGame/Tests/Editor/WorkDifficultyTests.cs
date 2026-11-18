using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WorkGame.Tests
{
    public sealed class WorkDifficultyTests
    {
        [Test]
        public void FirstThreeMonthsUseOnlyEasyAndAprilCanUseBothDifficulties()
        {
            var category = ScriptableObject.CreateInstance<WorkCategorySO>();
            var plan = ScriptableObject.CreateInstance<WorkPlanSO>();
            try
            {
                category.Id = "test"; category.Title = "Test"; category.Target = "Target";
                category.Metrics = new List<WorkMetric> {
                    Metric("Easy A", WorkDifficulty.Easy), Metric("Easy B", WorkDifficulty.Easy),
                    Metric("Normal A", WorkDifficulty.Normal), Metric("Normal B", WorkDifficulty.Normal)
                };
                for (int month = 1; month <= 4; month++)
                    plan.Batches.Add(new WorkBatch { Month = month, Category = category, Count = month <= 3 ? 2 : 4 });
                var first = WorkPaperFactory.Create(plan, 3, 42);
                var same = WorkPaperFactory.Create(plan, 3, 42);
                Assert.That(first.Select(p => (p.Month, p.Metric, string.Join(",", p.Choices.Select(c => c.Value)))),
                    Is.EqualTo(same.Select(p => (p.Month, p.Metric, string.Join(",", p.Choices.Select(c => c.Value))))));
                Assert.That(first.Where(p => p.Month <= 3).All(p => p.Metric.StartsWith("Easy")), Is.True);
                Assert.That(first.Where(p => p.Month == 4).Select(p => p.Metric),
                    Is.EquivalentTo(category.Metrics.Select(m => m.Name)));
                Assert.That(category.Metrics.Count, Is.EqualTo(4));
            }
            finally { UnityEngine.Object.DestroyImmediate(plan); UnityEngine.Object.DestroyImmediate(category); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InsufficientEasyQuestionsAreReportedBeforeGeneratingPapers(bool randomize)
        {
            var category = ScriptableObject.CreateInstance<WorkCategorySO>();
            var plan = ScriptableObject.CreateInstance<WorkPlanSO>();
            var config = ScriptableObject.CreateInstance<WorkConfigSO>();
            try
            {
                category.Id = "test"; category.Title = "Test"; category.Target = "Target";
                category.Metrics = new List<WorkMetric> { Metric("Easy", WorkDifficulty.Easy), Metric("Normal", WorkDifficulty.Normal) };
                plan.RandomizeCategories = randomize;
                plan.Batches.Add(new WorkBatch { Month = 1, Category = category, Count = 2 });
                config.Plan = plan;
                Assert.That(config.GetErrors().Any(e => e.Contains("eligible unique metrics")), Is.True);
                Assert.Throws<InvalidOperationException>(() => WorkPaperFactory.Create(plan, 3, 1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config); UnityEngine.Object.DestroyImmediate(plan);
                UnityEngine.Object.DestroyImmediate(category);
            }
        }

        [Test]
        public void CsvImportsDifficultyAndLegacyRowsDefaultToNormal()
        {
            var category = ScriptableObject.CreateInstance<WorkCategorySO>();
            var plan = ScriptableObject.CreateInstance<WorkPlanSO>();
            var config = ScriptableObject.CreateInstance<WorkConfigSO>();
            try
            {
                category.Id = "difficulty_csv_fixture"; category.Title = "Fixture"; category.Target = "Target";
                category.Metrics.Add(Metric("Original", WorkDifficulty.Easy));
                plan.Batches.Add(new WorkBatch { Month = 4, Category = category, Count = 1 });
                config.Plan = plan;
                new WorkGame.Editor.WorkCsvImport(
                    "Id,Title,Target,Metric,Unit,Rule,Display,Min,Max,Difficulty\n" +
                    "difficulty_csv_fixture,Fixture,Target,Easy question,,Highest,Number,1,10,Easy\n", true).Apply(config);
                Assert.That(category.Metrics.Single().Difficulty, Is.EqualTo(WorkDifficulty.Easy));
                new WorkGame.Editor.WorkCsvImport(
                    "Id,Title,Target,Metric,Unit,Rule,Display,Min,Max\n" +
                    "difficulty_csv_fixture,Fixture,Target,Legacy question,,Highest,Number,1,10\n", true).Apply(config);
                Assert.That(category.Metrics.Single().Difficulty, Is.EqualTo(WorkDifficulty.Normal));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config); UnityEngine.Object.DestroyImmediate(plan);
                UnityEngine.Object.DestroyImmediate(category);
            }
        }

        [Test]
        public void AuthoredCategoriesAndMonthPlanRespectTheDifficultySchedule()
        {
            var config = AssetDatabase.LoadAssetAtPath<WorkConfigSO>("Assets/WorkGame/Data/WorkConfig.asset");
            Assert.That(config.GetErrors(), Is.Empty);
            var categories = config.Plan.Batches.Select(b => b.Category).Distinct().ToDictionary(c => c.Id);
            Assert.That(categories.Count, Is.EqualTo(15));
            foreach (var category in categories.Values)
            {
                Assert.That(category.Metrics.Count, Is.EqualTo(45), category.Id);
                Assert.That(category.Metrics.Count(m => m.Difficulty == WorkDifficulty.Easy), Is.EqualTo(20), category.Id);
                Assert.That(category.Metrics.Count(m => m.Difficulty == WorkDifficulty.Normal), Is.EqualTo(25), category.Id);
            }
            for (int seed = 0; seed < 20; seed++)
            {
                var papers = WorkPaperFactory.Create(config.Plan, config.ChoiceCount, seed);
                foreach (var month in papers.GroupBy(p => p.Month))
                {
                    Assert.That(month.Count(), Is.EqualTo(config.Plan.Batches.Where(b => b.Month == month.Key).Sum(b => b.Count)));
                    Assert.That(month.Select(p => p.CategoryId).Distinct().Count(), Is.EqualTo(WorkPaperFactory.CategoryCountForMonth(month.Key)));
                    foreach (var group in month.GroupBy(p => p.CategoryId))
                    {
                        Assert.That(group.Select(p => (p.Agency, p.Title, p.Metric)).Distinct().Count(), Is.EqualTo(group.Count()));
                        foreach (var paper in group.Where(p => p.Month <= 3))
                            Assert.That(categories[paper.CategoryId].Metrics.Single(m => m.Title == paper.Title && m.Name == paper.Metric && m.Agency == paper.Agency).Difficulty,
                                Is.EqualTo(WorkDifficulty.Easy));
                    }
                }
            }
        }

        private static WorkMetric Metric(string name, WorkDifficulty difficulty) =>
            new WorkMetric { Name = name, Title = name, Difficulty = difficulty, Min = 1, Max = 10 };
    }
}
