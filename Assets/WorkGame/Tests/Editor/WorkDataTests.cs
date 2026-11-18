using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using WorkGame.Editor;

namespace WorkGame.Tests
{
    public sealed class WorkDataTests
    {
        private WorkConfigSO config;
        private WorkPlanSO plan;
        private WorkCategorySO category;

        [SetUp]
        public void SetUp()
        {
            category = ScriptableObject.CreateInstance<WorkCategorySO>();
            category.Id = "test_category";
            category.Title = "Test";
            category.Target = "제품";
            category.Metrics.Add(new WorkMetric { Name = "점수", Min = 1, Max = 3, Difficulty = WorkDifficulty.Easy });
            category.Metrics.Add(new WorkMetric { Name = "보조 점수 1", Min = 1, Max = 3, Difficulty = WorkDifficulty.Easy });
            category.Metrics.Add(new WorkMetric { Name = "보조 점수 2", Min = 1, Max = 3, Difficulty = WorkDifficulty.Easy });
            plan = ScriptableObject.CreateInstance<WorkPlanSO>();
            for (int month = 1; month <= 12; month++)
                plan.Batches.Add(new WorkBatch { Month = month, Category = category, Count = 3 });
            config = ScriptableObject.CreateInstance<WorkConfigSO>();
            config.Plan = plan;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(config);
            UnityEngine.Object.DestroyImmediate(plan);
            UnityEngine.Object.DestroyImmediate(category);
        }

        [Test]
        public void SeededPapersAreRepeatableUniqueAndLeaveAssetsUntouched()
        {
            using var first = config.CreateSystem(123);
            using var second = config.CreateSystem(123);
            first.StartRun();
            second.StartRun();
            for (int month = 1; month <= 12; month++)
            {
                foreach (var paper in first.Run.Papers.Where(x => x.Month == month))
                {
                    Assert.That(paper.Choices.Select(x => x.Value).Distinct().Count(), Is.EqualTo(3));
                    Assert.That(paper.Choices.Select(x => x.Value), Is.EquivalentTo(new[] { 1, 2, 3 }));
                    var other = second.Run.Papers.Single(x => x.Id == paper.Id);
                    Assert.That(paper.Choices.Select(x => x.Value), Is.EqualTo(other.Choices.Select(x => x.Value)));
                }
                first.AddTime(120);
                second.AddTime(120);
                first.LeaveWork();
                second.LeaveWork();
                first.StartNextMonth();
                second.StartNextMonth();
            }
            Assert.That(config.Plan.Batches.Count, Is.EqualTo(12));
            Assert.That(category.Metrics[0].Max, Is.EqualTo(3));
            Assert.That(first.Run.Papers.Select(x => x.Id).Distinct().Count(), Is.EqualTo(36));
        }

        [Test]
        public void RandomMonthlyCategoriesRespectLimitsTotalsAndSeed()
        {
            var pool = new List<WorkCategorySO>();
            try
            {
                plan.Batches.Clear();
                plan.RandomizeCategories = true;
                for (int i = 0; i < 15; i++)
                {
                    var item = ScriptableObject.CreateInstance<WorkCategorySO>();
                    pool.Add(item);
                    item.Id = i.ToString();
                    item.Title = "Category " + i;
                    item.Target = "Target";
                    for (int j = 0; j < 45; j++)
                        item.Metrics.Add(new WorkMetric { Name = "Metric " + j, Max = 100,
                            Difficulty = j < 30 ? WorkDifficulty.Easy : WorkDifficulty.Normal });
                    plan.Batches.Add(new WorkBatch { Month = 1, Category = item, Count = 0 });
                }
                for (int month = 1; month <= 12; month++)
                    plan.Batches.Add(new WorkBatch { Month = month, Category = pool[0], Count = 30 });

                var first = WorkPaperFactory.Create(plan, 3, 123);
                var same = WorkPaperFactory.Create(plan, 3, 123);
                var different = WorkPaperFactory.Create(plan, 3, 456);
                Assert.That(first.Select(x => (x.Month, x.CategoryId, x.Metric)),
                    Is.EqualTo(same.Select(x => (x.Month, x.CategoryId, x.Metric))));
                Assert.That(first.Select(x => x.CategoryId), Is.Not.EqualTo(different.Select(x => x.CategoryId)));
                foreach (var month in first.GroupBy(x => x.Month))
                {
                    Assert.That(month.Count(), Is.EqualTo(30));
                    Assert.That(month.Select(x => x.CategoryId).Distinct().Count(),
                        Is.EqualTo(month.Key <= 3 ? 1 : month.Key <= 6 ? 2 : 3));
                    foreach (var group in month.GroupBy(x => x.CategoryId))
                        Assert.That(group.Select(x => x.Metric).Distinct().Count(), Is.EqualTo(group.Count()));
                }
                Assert.That(pool.All(x => x.Metrics.Count == 45), Is.True);
                Assert.That(plan.Batches.Count, Is.EqualTo(27));
            }
            finally
            {
                foreach (var item in pool) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void InvalidDataIsReportedBeforeRunStarts()
        {
            category.Metrics[0].Max = 2;
            plan.Batches.Add(new WorkBatch { Month = 1, Category = category });
            Assert.That(config.GetErrors().Count, Is.GreaterThanOrEqualTo(2));
            Assert.Throws<InvalidOperationException>(() => config.CreateSystem(1));
        }

        [TestCase(PaperDisplay.Stars, 0, 5, 5)]
        [TestCase(PaperDisplay.Grade, 1, 6, 6)]
        [TestCase(PaperDisplay.HorizontalBar, 10, 100, 90)]
        [TestCase(PaperDisplay.VerticalBar, -1, 100, 100)]
        public void InvalidDisplayRangesAreRejected(PaperDisplay display, int min, int max, int scaleMax)
        {
            category.Metrics[0].Display = display;
            category.Metrics[0].Min = min;
            category.Metrics[0].Max = max;
            category.Metrics[0].ScaleMax = scaleMax;
            Assert.That(config.GetErrors(), Is.Not.Empty);
        }

        [Test]
        public void CsvCarriesShortNameAndFixedBarScaleIntoPapers()
        {
            foreach (var batch in plan.Batches) batch.Count = 1;
            var import = new WorkCsvImport(
                "Id,Title,ShortName,Target,Metric,Unit,Rule,Display,Min,Max,ScaleMax,Difficulty\n" +
                "test_category,Test,짧은 이름,제품,점수,%,Highest,HorizontalBar,10,90,100,Easy\n", true);
            import.Apply(config);
            using var system = config.CreateSystem(1);
            system.StartRun();
            Assert.That(system.CurrentPaper.DisplayMax, Is.EqualTo(100));
            Assert.That(system.CurrentPaper.CategoryShortName, Is.EqualTo("짧은 이름"));
            Assert.That(system.CurrentPaper.Choices.All(x => x.Value >= 10 && x.Value <= 90), Is.True);
        }

        [Test]
        public void CsvPreviewDoesNotChangePlanOrCategory()
        {
            var import = new WorkCsvImport("Month,CategoryId,Count\n1,test_category,2\n", false);
            import.Check(config);
            Assert.That(plan.Batches[0].Count, Is.EqualTo(3));
            foreach (var batch in plan.Batches) batch.Count = 1;
            var categories = new WorkCsvImport(
                "Id,Title,Target,Metric,Unit,Rule,Display,Min,Max,Difficulty\n" +
                "test_category,Changed,제품,\"점수,비교\",,Lowest,Number,1,5,Easy\n", true);
            categories.Check(config);
            Assert.That(category.Title, Is.EqualTo("Test"));
            Assert.That(category.Metrics[0].Name, Is.EqualTo("점수"));
        }

        [Test]
        public void ApplyingMonthCsvUpdatesOnlyMatchingRowsAndKeepsReferences()
        {
            var import = new WorkCsvImport("Month,CategoryId,Count\n1,test_category,2\n", false);
            import.Apply(config);
            Assert.That(plan.Batches[0].Count, Is.EqualTo(2));
            Assert.That(plan.Batches[1].Count, Is.EqualTo(3));
            Assert.That(plan.Batches.Count, Is.EqualTo(12));
            Assert.That(plan.Batches.All(x => x.Category == category), Is.True);
        }

        [Test]
        public void ApplyingCategoryCsvKeepsCategoryReferenceAndReplacesMetrics()
        {
            foreach (var batch in plan.Batches) batch.Count = 1;
            var import = new WorkCsvImport(
                "Id,Title,Target,Metric,Unit,Rule,Display,Min,Max,Difficulty\n" +
                "test_category,Changed,기업,지원 필요도,점,Lowest,HorizontalBar,1,10,Easy\n", true);
            import.Apply(config);
            Assert.That(category.Title, Is.EqualTo("Changed"));
            Assert.That(category.Metrics.Count, Is.EqualTo(1));
            Assert.That(category.Metrics[0].Rule, Is.EqualTo(PickRule.Lowest));
            Assert.That(plan.Batches.All(x => x.Category == category), Is.True);
        }

        [TestCase("Month,CategoryId,Count\n1,unknown,4\n")]
        [TestCase("Month,CategoryId,Count\n13,test_category,4\n")]
        [TestCase("Month,CategoryId,Count\n1,test_category,-1\n")]
        [TestCase("Month,CategoryId,Count\n1,test_category,4\n1,test_category,5\n")]
        public void BadMonthCsvLeavesOriginalDataUnchanged(string text)
        {
            var import = new WorkCsvImport(text, false);
            Assert.Throws<FormatException>(() => import.Check(config));
            Assert.That(plan.Batches.Count, Is.EqualTo(12));
            Assert.That(plan.Batches[0].Count, Is.EqualTo(3));
        }

        [Test]
        public void BadCategoryCsvCannotPartiallyApply()
        {
            var import = new WorkCsvImport(
                "Id,Title,Target,Metric,Unit,Rule,Display,Min,Max\n" +
                "test_category,Changed,제품,점수,,Highest,Number,1,2\n", true);
            Assert.Throws<FormatException>(() => import.Apply(config));
            Assert.That(category.Title, Is.EqualTo("Test"));
            Assert.That(category.Metrics[0].Max, Is.EqualTo(3));
        }

        [Test]
        public void CsvSupportsBomEscapedQuotesAndMultilineText()
        {
            foreach (var batch in plan.Batches) batch.Count = 1;
            var import = new WorkCsvImport(
                "\uFEFFId,Title,Target,Metric,Unit,Rule,Display,Min,Max,Difficulty\r\n" +
                "test_category,\"Title \"\"quoted\"\"\nnext line\",제품,점수,,Highest,Number,1,5,Easy\r\n", true);
            Assert.DoesNotThrow(() => import.Check(config));
        }

        [Test]
        public void EmptyOrBrokenCsvIsRejected()
        {
            Assert.Throws<FormatException>(() => new WorkCsvImport("Month,CategoryId,Count\n", false));
            Assert.Throws<FormatException>(() => new WorkCsvImport("Month,CategoryId,Count\n1,\"broken,3", false));
        }
    }
}
