using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System;
using System.Linq;
using NUnit.Framework;

namespace WorkGame.Tests
{
    public sealed class WorkSystemTests
    {
        private static WorkPaper Paper(int id = 1, int month = 1, PickRule rule = PickRule.Highest)
        {
            return new WorkPaper(id, month, "01", "Test", "제품", "점수", "", rule, PaperDisplay.Number,
                new[] { new WorkChoice("A", 12), new WorkChoice("B", 4), new WorkChoice("C", 8) });
        }

        private static WorkSystem Create(params WorkPaper[] papers)
        {
            var system = new WorkSystem(new WorkSettings(), papers);
            system.StartRun();
            return system;
        }

        [TestCase(0, 2, 0, false, 200)]
        [TestCase(100, 2, 0, true, 400)]
        [TestCase(100, 3, 0, true, 600)]
        [TestCase(100, 2, 1, false, 0)]
        public void SubmitReportsActualPointBoost(int chance, int multiplier, int choice, bool boosted, int gained)
        {
            using var system = Create(Paper());
            var upgrades = new WorkUpgradeSystem(system, new[]
            {
                new WorkUpgradeNode("luck", 0, Array.Empty<string>(), new[]
                {
                    new WorkUpgradeEffect { Kind = WorkUpgradeEffectKind.PointFlat, Value = 100 },
                    new WorkUpgradeEffect { Kind = WorkUpgradeEffectKind.PointChancePercent, Value = chance },
                    new WorkUpgradeEffect { Kind = WorkUpgradeEffectKind.PointMultiplier, Value = multiplier }
                })
            });
            Assert.That(upgrades.Buy("luck"), Is.True);
            WorkSubmitResult result = null;
            system.PaperSubmitted += value => result = value;
            system.StartHold(1, choice);
            system.AddTime(0.5);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsPointBoosted, Is.EqualTo(boosted));
            Assert.That(result.SkillPointsGained, Is.EqualTo(gained));
        }

        [TestCase("instant", true)]
        [TestCase("hold", false)]
        [TestCase("fax", false)]
        public void OnlyInstantSkillMarksSubmissionAsInstant(string source, bool expected)
        {
            using var system = Create(Paper());
            var upgrades = new WorkUpgradeSystem(system, new[]
            {
                new WorkUpgradeNode("instant", 0, Array.Empty<string>(), new[]
                {
                    new WorkUpgradeEffect { Kind = WorkUpgradeEffectKind.InstantChancePercent, Value = 100 },
                    new WorkUpgradeEffect { Kind = WorkUpgradeEffectKind.PointChancePercent, Value = 100 },
                    new WorkUpgradeEffect { Kind = WorkUpgradeEffectKind.PointMultiplier, Value = 2 }
                })
            });
            Assert.That(upgrades.Buy("instant"), Is.True);
            WorkSubmitResult result = null;
            int submissions = 0;
            system.PaperSubmitted += value => { result = value; submissions++; };
            if (source == "instant") Assert.That(system.TryInstantPaper(1), Is.True);
            else if (source == "fax")
            {
                Assert.That(system.Fax.TrySend(1, true), Is.True);
                system.AddTime(system.Fax.SecondsPerPaper);
            }
            else
            {
                system.StartHold(1, 0);
                system.AddTime(0.5);
            }
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsInstant, Is.EqualTo(expected));
            Assert.That(result.IsPointBoosted, Is.True);
            Assert.That(submissions, Is.EqualTo(1));
            Assert.That(system.MonthDone, Is.EqualTo(1));
            Assert.That(system.TryInstantPaper(1), Is.False);
        }

        [TestCase(0)]
        [TestCase(25)]
        [TestCase(75)]
        public void FaxProcessesInOrderAndWaitingPapersStartAtZero(int speedPercent)
        {
            using var system = Create(Paper(), Paper(2), Paper(3));
            var upgrades = new WorkUpgradeSystem(system, new[]
            {
                new WorkUpgradeNode("fax_queue", 0, Array.Empty<string>(), new[]
                {
                    new WorkUpgradeEffect { Kind = WorkUpgradeEffectKind.FaxSlots, Value = 2 },
                    new WorkUpgradeEffect { Kind = WorkUpgradeEffectKind.FaxSpeedPercent, Value = speedPercent }
                })
            });
            Assert.That(upgrades.Buy("fax_queue"), Is.True);
            var submitted = new List<int>();
            system.PaperSubmitted += result => submitted.Add(result.PaperId);
            for (int id = 1; id <= 3; id++)
                Assert.That(system.Fax.TrySend(id, true), Is.True);

            double duration = system.Fax.SecondsPerPaper / (1 + speedPercent / 100d);
            for (int id = 1; id <= 3; id++)
            {
                Assert.That(system.Fax.Jobs[0].PaperId, Is.EqualTo(id));
                Assert.That(system.Fax.Jobs.All(job => job.Progress == 0), Is.True);
                system.AddTime(duration / 2);
                Assert.That(system.Fax.Jobs[0].Progress, Is.EqualTo(0.5).Within(0.000001));
                Assert.That(system.Fax.Jobs.Skip(1).All(job => job.Progress == 0), Is.True);
                // 완료 프레임에 시간이 남아도 다음 장은 0%에서 시작합니다.
                system.AddTime(duration);
                Assert.That(submitted.Count, Is.EqualTo(id));
                Assert.That(system.Fax.Jobs.Count, Is.EqualTo(3 - id));
            }
            Assert.That(submitted, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [TestCase(PaperDisplay.Stars, 0, "★☆☆☆☆")]
        [TestCase(PaperDisplay.Stars, 2, "★★★★★")]
        [TestCase(PaperDisplay.Grade, 0, "D")]
        [TestCase(PaperDisplay.Grade, 1, "B")]
        [TestCase(PaperDisplay.Grade, 2, "S")]
        [TestCase(PaperDisplay.Number, 1, "3만원")]
        public void ChoiceTextMatchesDisplay(PaperDisplay display, int index, string text)
        {
            var paper = new WorkPaper(1, 1, "01", "Test", "제품", "점수", "만원", PickRule.Highest, display,
                new[] { new WorkChoice("A", 1), new WorkChoice("B", 3), new WorkChoice("C", 5) });
            Assert.That(paper.GetChoiceText(index), Is.EqualTo(text));
        }

        [Test]
        public void BarScaleStaysFixedInsteadOfStretchingToTheLargestChoice()
        {
            var paper = new WorkPaper(1, 1, "01", "Test", "제품", "점수", "%", PickRule.Highest,
                PaperDisplay.HorizontalBar, new[] { new WorkChoice("A", 10), new WorkChoice("B", 50), new WorkChoice("C", 90) }, "짧은 이름", 100);
            using var system = Create(paper);
            Assert.That(system.CurrentPaper.DisplayMax, Is.EqualTo(100));
            Assert.That(system.CurrentPaper.GetBarFill(0), Is.EqualTo(0.1f).Within(0.0001));
            Assert.That(system.CurrentPaper.GetBarFill(2), Is.EqualTo(0.9f).Within(0.0001));
            Assert.That(system.CurrentPaper.CategoryShortName, Is.EqualTo("짧은 이름"));
        }

        private static void NextMonth(WorkSystem system)
        {
            system.AddTime(120);
            Assert.That(system.LeaveWork(), Is.True);
            Assert.That(system.StartNextMonth(), Is.True);
        }

        [Test]
        public void DeskCanSelectAnyPendingPaper()
        {
            using var system = Create(Paper(), Paper(2));
            Assert.That(system.SelectPaper(2), Is.True);
            Assert.That(system.CurrentPaper.Id, Is.EqualTo(2));
            system.StartHold(2, 0);
            system.AddTime(0.5);
            Assert.That(system.Run.Papers[1].State, Is.EqualTo(PaperState.Right));
            Assert.That(system.Run.Papers[0].State, Is.EqualTo(PaperState.Pending));
            Assert.That(system.MonthDone, Is.EqualTo(1));
            Assert.That(system.MonthTotal, Is.EqualTo(2));
            Assert.That(system.SelectPaper(2), Is.False);
        }

        [Test]
        public void ClosingPaperCancelsItsHold()
        {
            using var system = Create(Paper(), Paper(2));
            system.SelectPaper(2);
            system.StartHold(2, 0);
            system.AddTime(0.25);
            system.ClearPaper();
            system.AddTime(1);
            Assert.That(system.Run.Papers[1].State, Is.EqualTo(PaperState.Pending));
            Assert.That(system.HoldProgress, Is.Zero);
        }

        [Test]
        public void PauseFreezesTimeCancelsHoldAndBlocksWorkInput()
        {
            using var system = Create(Paper());
            system.SelectPaper(1);
            system.StartHold(1, 0);
            system.AddTime(0.25);
            system.SetPaused(true);
            system.AddTime(1000);
            Assert.That(system.Run.Seconds, Is.EqualTo(0.25));
            Assert.That(system.HoldProgress, Is.Zero);
            Assert.That(system.SelectPaper(1), Is.False);
            Assert.That(system.StartHold(1, 0), Is.False);
            Assert.That(system.LeaveWork(), Is.False);
            system.SetPaused(false);
            system.StartHold(1, 0);
            system.AddTime(0.5);
            Assert.That(system.Run.Papers[0].State, Is.EqualTo(PaperState.Right));
        }

        [Test]
        public void MonthProgressIncludesBacklogAndResetsAtNextMonth()
        {
            using var system = Create(Paper(), Paper(2, 2));
            Assert.That(system.MonthTotal, Is.EqualTo(1));
            NextMonth(system);
            Assert.That(system.MonthTotal, Is.EqualTo(2));
            Assert.That(system.MonthDone, Is.Zero);
            system.SelectPaper(2);
            system.StartHold(2, 1);
            system.AddTime(0.5);
            Assert.That(system.MonthDone, Is.EqualTo(1));
            Assert.That(system.MonthTotal, Is.EqualTo(2));
            NextMonth(system);
            Assert.That(system.MonthTotal, Is.EqualTo(1));
            Assert.That(system.MonthDone, Is.Zero);
        }

        [TestCase(85, 100, WorkGrade.S)]
        [TestCase(84, 100, WorkGrade.A)]
        [TestCase(70, 100, WorkGrade.A)]
        [TestCase(69, 100, WorkGrade.B)]
        [TestCase(17, 20, WorkGrade.S)]
        [TestCase(14, 20, WorkGrade.A)]
        [TestCase(5, 6, WorkGrade.A)]
        [TestCase(0, 0, WorkGrade.B)]
        public void GradeUsesExactPercent(int right, int total, WorkGrade expected)
        {
            Assert.That(WorkRules.GetGrade(right, total), Is.EqualTo(expected));
        }

        [TestCase(PickRule.Highest, 0)]
        [TestCase(PickRule.Lowest, 1)]
        public void CorrectChoiceDependsOnRule(PickRule rule, int rightIndex)
        {
            var paper = Paper(rule: rule);
            for (int i = 0; i < 3; i++) Assert.That(WorkRules.IsRight(paper, i), Is.EqualTo(i == rightIndex));
        }

        [Test]
        public void CannotLeaveBeforeTwoMinutesButWorkDoesNotStopAfter()
        {
            using var system = Create(Paper());
            system.AddTime(119.9);
            Assert.That(system.LeaveWork(), Is.False);
            system.AddTime(0.1);
            Assert.That(system.CanLeave, Is.True);
            Assert.That(system.Run.Phase, Is.EqualTo(WorkPhase.Working));
            system.AddTime(600);
            system.StartHold(1, 0);
            system.AddTime(0.5);
            Assert.That(system.Run.Papers[0].State, Is.EqualTo(PaperState.Right));
            system.LeaveWork();
            Assert.That(system.Run.LastResult.OvertimeSeconds, Is.EqualTo(600.5).Within(0.0001));
            Assert.That(system.LeaveWork(), Is.False);
        }

        [Test]
        public void HoldCanBeCancelledAndOnlySubmitsOnce()
        {
            using var system = Create(Paper());
            system.StartHold(1, 0);
            system.AddTime(0.25);
            system.StopHold();
            system.AddTime(1);
            Assert.That(system.Run.Papers[0].State, Is.EqualTo(PaperState.Pending));
            system.StartHold(1, 0);
            system.AddTime(0.49);
            Assert.That(system.Run.Papers[0].State, Is.EqualTo(PaperState.Pending));
            system.AddTime(0.01);
            Assert.That(system.Run.Papers[0].State, Is.EqualTo(PaperState.Right));
            Assert.That(system.StartHold(1, 1), Is.False);
            system.AddTime(120);
            system.LeaveWork();
            Assert.That(system.Run.LastResult.Right, Is.EqualTo(1));
        }

        [Test]
        public void SwitchingChoiceResetsHoldTime()
        {
            using var system = Create(Paper());
            system.StartHold(1, 0);
            system.AddTime(0.4);
            system.StartHold(1, 1);
            system.AddTime(0.1);
            Assert.That(system.Run.Papers[0].State, Is.EqualTo(PaperState.Pending));
            system.AddTime(0.4);
            Assert.That(system.Run.Papers[0].State, Is.EqualTo(PaperState.Wrong));
        }

        [Test]
        public void RepeatedPointerDownDoesNotResetSameHold()
        {
            using var system = Create(Paper());
            system.StartHold(1, 0);
            system.AddTime(0.25);
            system.StartHold(1, 0);
            system.AddTime(0.25);
            Assert.That(system.Run.Papers[0].State, Is.EqualTo(PaperState.Right));
        }

        [Test]
        public void UpgradeSpeedOnlyChangesWorkProgress()
        {
            using var system = Create(Paper(), Paper(2));
            system.SetWorkSpeed(2);
            system.StartHold(1, 0);
            system.AddTime(0.25);
            Assert.That(system.Run.Papers[0].State, Is.EqualTo(PaperState.Right));
            Assert.That(system.Run.Seconds, Is.EqualTo(0.25));
            Assert.That(system.CanLeave, Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => system.SetWorkSpeed(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => system.SetWorkSpeed(double.NaN));
        }

        [Test]
        public void ChangingSpeedDoesNotRetroactivelyChangeProgress()
        {
            using var system = Create(Paper());
            system.StartHold(1, 0);
            system.AddTime(0.1);
            system.SetWorkSpeed(2);
            Assert.That(system.HoldProgress, Is.EqualTo(0.2).Within(0.0001));
            system.AddTime(0.2);
            Assert.That(system.Run.Papers[0].State, Is.EqualTo(PaperState.Right));
        }

        [Test]
        public void WrongAnswerReducesAttendanceOnceAndCannotBeRetried()
        {
            var settings = new WorkSettings { WrongPenalty = 7, StartAttendance = 5 };
            using var system = new WorkSystem(settings, new[] { Paper() });
            system.StartRun();
            system.StartHold(1, 1);
            system.AddTime(1000);
            Assert.That(system.Run.Attendance, Is.Zero);
            Assert.That(system.Run.Papers[0].State, Is.EqualTo(PaperState.Wrong));
            Assert.That(system.StartHold(1, 0), Is.False);
            system.LeaveWork();
            Assert.That(system.Run.LastResult.Wrong, Is.EqualTo(1));
        }

        [Test]
        public void PendingWorkCarriesOverWithoutDuplicating()
        {
            using var system = Create(Paper(), Paper(2, 2));
            Assert.That(system.Run.Papers.Count, Is.EqualTo(1));
            NextMonth(system);
            Assert.That(system.Run.Papers.Count, Is.EqualTo(2));
            Assert.That(system.Run.LastResult.Pending, Is.EqualTo(1));
            system.StartHold(1, 0);
            system.AddTime(0.5);
            Assert.That(system.Run.Papers[0].State, Is.EqualTo(PaperState.Right));
        }

        [Test]
        public void ReviewCountsPendingAsWrongAndIncludesEarlierMonths()
        {
            using var system = Create(Paper(), Paper(2, 2), Paper(3, 3));
            NextMonth(system);
            system.StartHold(1, 0);
            system.AddTime(0.5);
            NextMonth(system);
            system.StartHold(2, 0);
            system.AddTime(0.5);
            system.AddTime(120);
            system.LeaveWork();
            var review = system.Run.LastResult.Review;
            Assert.That(review.Total, Is.EqualTo(3));
            Assert.That(review.Right, Is.EqualTo(2));
            Assert.That(review.Grade, Is.EqualTo(WorkGrade.B));
            Assert.That(system.Run.Bonus, Is.EqualTo(100));
        }

        [Test]
        public void FinishingAllWorkAllowsEarlyLeaveButDoesNotAutoLeave()
        {
            using var system = Create(Paper(), Paper(2));
            Assert.That(system.CanLeave, Is.False);
            system.StartHold(1, 0);
            system.AddTime(0.5);
            Assert.That(system.CanLeave, Is.False);
            system.StartHold(2, 0);
            system.AddTime(0.5);
            Assert.That(system.CanLeave, Is.True);
            Assert.That(system.Run.Phase, Is.EqualTo(WorkPhase.Working));
            Assert.That(system.CurrentPaper, Is.Null);
            Assert.That(system.LeaveWork(), Is.True);
            Assert.That(system.Run.Seconds, Is.EqualTo(1));
            Assert.That(system.Run.LastResult.OvertimeSeconds, Is.Zero);
            Assert.That(system.CanLeave, Is.False);
        }

        [Test]
        public void PapersArePresentedOneAtATimeAndWrongSubmissionAdvances()
        {
            using var system = Create(Paper(), Paper(2));
            Assert.That(system.CurrentPaper.Id, Is.EqualTo(1));
            Assert.That(system.PendingCount, Is.EqualTo(2));
            Assert.That(system.StartHold(2, 0), Is.False);
            system.StartHold(1, 1);
            system.AddTime(0.5);
            Assert.That(system.CurrentPaper.Id, Is.EqualTo(2));
            Assert.That(system.PendingCount, Is.EqualTo(1));
            Assert.That(system.HeldPaperId, Is.EqualTo(-1));
            system.AddTime(10);
            Assert.That(system.CurrentPaper.State, Is.EqualTo(PaperState.Pending));
        }

        [Test]
        public void BacklogComesBeforeNewPapersAndMustBeDoneForEarlyLeave()
        {
            using var system = Create(Paper(), Paper(2, 2));
            NextMonth(system);
            Assert.That(system.CurrentPaper.Id, Is.EqualTo(1));
            Assert.That(system.StartHold(2, 0), Is.False);
            Assert.That(system.CanLeave, Is.False);
            system.StartHold(1, 0);
            system.AddTime(0.5);
            Assert.That(system.CurrentPaper.Id, Is.EqualTo(2));
            Assert.That(system.CanLeave, Is.False);
            system.StartHold(2, 1);
            system.AddTime(0.5);
            Assert.That(system.CanLeave, Is.True);
        }

        [Test]
        public void MonthWithoutWorkCanLeaveImmediately()
        {
            using var system = Create(Paper(1, 2));
            Assert.That(system.CanLeave, Is.True);
            Assert.That(system.CurrentPaper, Is.Null);
            Assert.That(system.LeaveWork(), Is.True);
            Assert.That(system.Run.LastResult.OvertimeSeconds, Is.Zero);
            system.StartNextMonth();
            Assert.That(system.CanLeave, Is.False);
            Assert.That(system.CurrentPaper.Id, Is.EqualTo(1));
        }

        [Test]
        public void OldBacklogDoesNotChangePastReviewOrNextQuarter()
        {
            using var system = Create(Paper(), Paper(2, 4));
            NextMonth(system);
            NextMonth(system);
            NextMonth(system);
            var firstReview = system.Run.LastResult.Review;
            system.StartHold(1, 0);
            system.AddTime(0.5);
            system.StartHold(2, 0);
            system.AddTime(0.5);
            NextMonth(system);
            NextMonth(system);
            system.AddTime(120);
            system.LeaveWork();
            Assert.That(firstReview.Right, Is.Zero);
            Assert.That(system.Run.LastResult.Review.Total, Is.EqualTo(1));
            Assert.That(system.Run.LastResult.Review.Right, Is.EqualTo(1));
            Assert.That(system.Run.LastResult.Review.Grade, Is.EqualTo(WorkGrade.S));
        }

        [Test]
        public void RunEndsAtMonthTwelveAndEmptyQuartersGiveNoBonus()
        {
            using var system = Create();
            for (int month = 1; month <= 12; month++)
            {
                Assert.That(system.Run.Month, Is.EqualTo(month));
                system.AddTime(120);
                Assert.That(system.LeaveWork(), Is.True);
                Assert.That(system.StartNextMonth(), Is.EqualTo(month < 12));
            }
            Assert.That(system.Run.Phase, Is.EqualTo(WorkPhase.Finished));
            Assert.That(system.Run.Months.Count, Is.EqualTo(12));
            Assert.That(system.Run.Months.Count(x => x.Review != null), Is.EqualTo(4));
            Assert.That(system.Run.Bonus, Is.Zero);
            system.AddTime(999);
            Assert.That(system.Run.Seconds, Is.EqualTo(120));
            Assert.That(system.StartRun(), Is.False);
        }

        [Test]
        public void SettingsAreCopiedSoEditingSourceDoesNotChangeRun()
        {
            var settings = new WorkSettings();
            using var system = new WorkSystem(settings, new[] { Paper() });
            settings.HoldSeconds = 99;
            system.StartRun();
            system.StartHold(1, 0);
            system.AddTime(0.5);
            Assert.That(system.Run.Papers[0].State, Is.EqualTo(PaperState.Right));
        }

        [Test]
        public void TwoRunsCannotChangeEachOthersPapers()
        {
            var paper = Paper();
            using var first = Create(paper);
            using var second = Create(paper);
            first.StartHold(1, 0);
            first.AddTime(0.5);
            Assert.That(second.Run.Papers[0].State, Is.EqualTo(PaperState.Pending));
            Assert.That(paper.State, Is.EqualTo(PaperState.Pending));
        }

        [Test]
        public void InvalidInputCannotSubmitFutureOrMissingWork()
        {
            using var system = Create(Paper(), Paper(2, 2));
            Assert.That(system.StartHold(2, 0), Is.False);
            Assert.That(system.StartHold(99, 0), Is.False);
            Assert.That(system.StartHold(1, -1), Is.False);
            Assert.That(system.StartHold(1, 3), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => system.AddTime(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => system.AddTime(-1));
            Assert.That(system.StartNextMonth(), Is.False);
        }

        [Test]
        public void DisposedSystemStopsSendingEventsAndAcceptingInput()
        {
            var system = Create(Paper());
            int updates = 0;
            system.Changed += () => updates++;
            system.AddTime(1);
            Assert.That(updates, Is.EqualTo(1));
            system.Dispose();
            system.AddTime(1);
            Assert.That(updates, Is.EqualTo(1));
            Assert.That(system.StartHold(1, 0), Is.False);
            Assert.That(system.LeaveWork(), Is.False);
        }

        [Test]
        public void InvalidSettingsAndDuplicatePapersAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new WorkSettings { HoldSeconds = 0 }.Check());
            Assert.Throws<ArgumentException>(() => new WorkSettings { MonthSeconds = double.NaN }.Check());
            Assert.Throws<ArgumentOutOfRangeException>(() => new WorkSettings { APercent = 85 }.Check());
            Assert.Throws<ArgumentException>(() => new WorkSystem(new WorkSettings(), new[] { Paper(), Paper() }));
        }
    }
}

namespace WorkGame.Tests
{
    public sealed class WorkSkillExpansionTests
    {
        private static WorkPaper Paper(int id, int month, string category = "A", string metric = "metric") =>
            new WorkPaper(id, month, category, category, "target", metric, "", PickRule.Highest,
                PaperDisplay.Number, new[] { new WorkChoice("A", 2), new WorkChoice("B", 1) });

        private static WorkUpgradeNode Node(string id, WorkUpgradeEffectKind kind, int value, string parent = null) =>
            new WorkUpgradeNode(id, 0, parent == null ? Array.Empty<string>() : new[] { parent },
                new[] { new WorkUpgradeEffect { Kind = kind, Value = value } });

        private static WorkUpgradeNode[] Nodes() => new[]
        {
            Node("p1", WorkUpgradeEffectKind.WorkloadPercent, 10),
            Node("p2", WorkUpgradeEffectKind.WorkloadPercent, 20, "p1"),
            Node("p3", WorkUpgradeEffectKind.WorkloadPercent, 30, "p2"),
            Node("h1", WorkUpgradeEffectKind.CareGrowthPercent, 5),
            Node("h2", WorkUpgradeEffectKind.CareGrowthPercent, 10, "h1"),
            Node("quiet", WorkUpgradeEffectKind.DoNotDisturbUnlock, 1)
        };

        [Test]
        public void HigherStagesReplaceEarlierPercentagesAndRebuildIsIdempotent()
        {
            using var work = new WorkSystem(new WorkSettings(), Array.Empty<WorkPaper>());
            var upgrades = new WorkUpgradeSystem(work, Nodes());
            Assert.That(upgrades.Buy("p2"), Is.False);
            foreach (var id in new[] { "p1", "p2", "p3", "h1", "h2" }) Assert.That(upgrades.Buy(id), Is.True);
            upgrades.RebuildStats(); upgrades.RebuildStats();
            Assert.That(work.Upgrades.WorkloadBonus, Is.EqualTo(0.3).Within(0.00001));
            Assert.That(work.Upgrades.CareGrowthBonus, Is.EqualTo(0.1).Within(0.00001));
        }

        [TestCase("p1", 27)]
        [TestCase("p2", 30)]
        [TestCase("p3", 32)]
        public void InheritedStageAppliesToFirstMonthWithFloorRounding(string stage, int count)
        {
            using var work = new WorkSystem(new WorkSettings(), Enumerable.Range(1, 25).Select(i => Paper(i, 1)),
                7, Enumerable.Range(100, 7).Select(i => Paper(i, 1)));
            var ids = stage == "p1" ? new[] { "p1" } : stage == "p2" ? new[] { "p1", "p2" } : new[] { "p1", "p2", "p3" };
            work.InheritUpgrades(ids);
            _ = new WorkUpgradeSystem(work, Nodes());
            work.StartRun();
            Assert.That(work.MonthTotal, Is.EqualTo(count));
        }

        [Test]
        public void PurchaseChangesNextAllocationOnlyAndCarryoverAndSaveStayStable()
        {
            var papers = Enumerable.Range(1, 2).SelectMany(m => Enumerable.Range(1, 10).Select(i => Paper(m * 100 + i, m))).ToArray();
            var reserves = Enumerable.Range(1, 2).SelectMany(m => Enumerable.Range(1, 3).Select(i => Paper(m * 1000 + i, m))).ToArray();
            using var work = new WorkSystem(new WorkSettings(), papers, 7, reserves);
            var upgrades = new WorkUpgradeSystem(work, Nodes());
            upgrades.Buy("p1"); work.StartRun();
            Assert.That(work.MonthTotal, Is.EqualTo(11));
            upgrades.Buy("p2");
            Assert.That(work.MonthTotal, Is.EqualTo(11));
            work.StartHold(101, 0); work.AddTime(0.5); work.AddTime(90);
            Assert.That(work.LeaveWork(), Is.True);
            Assert.That(work.StartNextMonth(), Is.True);
            Assert.That(work.MonthTotal, Is.EqualTo(22)); // 10 carried + 10 base + 2 new extras.
            var saved = JsonUtility.FromJson<WorkSaveData>(JsonUtility.ToJson(work.CaptureSave()));
            using var restored = new WorkSystem(new WorkSettings(), papers, 7, reserves);
            restored.RestoreSave(saved);
            var restoredUpgrades = new WorkUpgradeSystem(restored, Nodes());
            restoredUpgrades.Buy("p3");
            Assert.That(restored.MonthPapers.Select(x => x.Id), Is.EqualTo(work.MonthPapers.Select(x => x.Id)));
            Assert.That(restored.MonthTotal, Is.EqualTo(22));
            saved.MonthlyExtraCounts[1] = 3;
            using var invalid = new WorkSystem(new WorkSettings(), papers, 7, reserves);
            Assert.Throws<ArgumentException>(() => invalid.RestoreSave(saved));
        }

        [Test]
        public void OldSaveWithoutNewFieldsRestoresExistingPapersAndNotificationsStayOn()
        {
            var papers = Enumerable.Range(1, 10).Select(i => Paper(i, 1)).ToArray();
            using var old = new WorkSystem(new WorkSettings(), papers, 7);
            old.StartRun();
            var saved = old.CaptureSave(); saved.ReservePlanKey = null; saved.MonthlyExtraCounts = null;
            saved.DoNotDisturbEnabled = false;
            using var restored = new WorkSystem(new WorkSettings(), papers, 7, Enumerable.Range(100, 3).Select(i => Paper(i, 1)));
            restored.RestoreSave(saved);
            _ = new WorkUpgradeSystem(restored, Nodes());
            Assert.That(restored.MonthTotal, Is.EqualTo(10));
            Assert.That(restored.NotificationsMuted, Is.False);
        }

        [TestCase(10, "B")]
        [TestCase(14, "A")]
        public void ReserveUsesSelectedCategoriesFirstAndFallsBackOnlyWhenExhausted(int available, string expected)
        {
            var a = ScriptableObject.CreateInstance<WorkCategorySO>();
            var b = ScriptableObject.CreateInstance<WorkCategorySO>();
            var plan = ScriptableObject.CreateInstance<WorkPlanSO>();
            try
            {
                a.Id = "A"; a.Title = "A"; a.Target = "target";
                b.Id = "B"; b.Title = "B"; b.Target = "target";
                a.Metrics = Enumerable.Range(0, available).Select(i => new WorkMetric { Name = "m" + i }).ToList();
                b.Metrics = Enumerable.Range(0, 4).Select(i => new WorkMetric { Name = "b" + i }).ToList();
                plan.Batches = new List<WorkBatch> { new WorkBatch { Month = 1, Category = a, Count = 10 }, new WorkBatch { Month = 1, Category = b, Count = 0 } };
                var originals = Enumerable.Range(0, 10).Select(i => Paper(i + 1, 1, "A", "m" + i)).ToArray();
                var reserve = WorkPaperFactory.CreateReserve(plan, 3, 42, originals);
                var same = WorkPaperFactory.CreateReserve(plan, 3, 42, originals);
                Assert.That(reserve.Count, Is.EqualTo(3));
                Assert.That(reserve.All(x => x.CategoryId == expected), Is.True);
                Assert.That(reserve.Select(x => x.Metric).Distinct().Count(), Is.EqualTo(3));
                Assert.That(reserve.Select(x => x.Metric), Is.EqualTo(same.Select(x => x.Metric)));
            }
            finally { UnityEngine.Object.DestroyImmediate(plan); UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); }
        }

        [TestCase(0, 52)]
        [TestCase(95, 5)]
        public void HealthyFoodAddsToFurnitureForLessonsAndSynergyAndRespectsCapAndWeeklyTotals(int initial, int gain)
        {
            using var work = new WorkSystem(new WorkSettings(), Array.Empty<WorkPaper>());
            var upgrades = new WorkUpgradeSystem(work, Nodes()); work.StartRun(); work.LeaveWork();
            var fallback = new CareEnding("fallback", "Fallback", 0, Array.Empty<CareGain>(), "", "", "", "");
            using var care = new CareSystem(work, new[]
            {
                new CareLesson("home", "Home", 1, 0, Array.Empty<CareGain>()),
                new CareLesson("rest", "Rest", 1, 0, Array.Empty<CareGain>()),
                new CareLesson("math", "Math", 1, 0, new[] { new CareGain(CareStat.Logic, 5) })
            }, new[] { new CareSynergy("pair", "Pair", new[] { "math", "math" }, new[] { new CareGain(CareStat.Logic, 20) }) },
                new[] { new CareGift("desk", "Desk", CareGiftKind.Desk, 0, 0, new[] { new CareGiftBonus { Stat = CareStat.Knowledge, Percent = 20 } }) },
                Array.Empty<CareEnding>(), fallback, new[] { new CareGain(CareStat.Logic, initial) }, statMax: 100);
            care.BuyGift("desk");
            for (int i = 0; i < 4; i++) care.SetLesson(i, "math");
            upgrades.Buy("h1"); upgrades.Buy("h2");
            Assert.That(care.Run.GetStat(CareStat.Logic), Is.EqualTo(initial));
            Assert.That(care.Preview().GetGain(CareStat.Logic), Is.EqualTo(gain));
            Assert.That(care.FinishMonth(), Is.True);
            Assert.That(care.Run.GetStat(CareStat.Logic), Is.EqualTo(initial + gain));
            Assert.That(care.Run.LastResult.Weeks.Sum(w => w.Gains.Where(g => g.Stat == CareStat.Logic).Sum(g => g.Amount)), Is.EqualTo(gain));
        }

        [Test]
        public void DoNotDisturbRequiresUnlockDefaultsOnAndToggleSurvivesSave()
        {
            using var work = new WorkSystem(new WorkSettings(), new[] { Paper(1, 1) });
            var upgrades = new WorkUpgradeSystem(work, Nodes()); work.StartRun();
            Assert.That(work.NotificationsMuted, Is.False);
            work.SetDoNotDisturbEnabled(false); upgrades.Buy("quiet");
            Assert.That(work.NotificationsMuted, Is.True);
            work.SetDoNotDisturbEnabled(false);
            using var restored = new WorkSystem(new WorkSettings(), new[] { Paper(1, 1) });
            restored.RestoreSave(JsonUtility.FromJson<WorkSaveData>(JsonUtility.ToJson(work.CaptureSave())));
            _ = new WorkUpgradeSystem(restored, Nodes());
            Assert.That(restored.NotificationsMuted, Is.False);
            restored.SetDoNotDisturbEnabled(true);
            Assert.That(restored.NotificationsMuted, Is.True);
            restored.StartHold(1, 0); restored.AddTime(0.5);
            Assert.That(restored.Run.SkillPoints, Is.EqualTo(100));
        }

        [Test]
        public void MutingClearsVisibleAndQueuedBubblesAndUnmutingDoesNotReplayThem()
        {
            var root = new GameObject("Feedback"); root.SetActive(false);
            try
            {
                var bubbleObject = new GameObject("Bubble", typeof(RectTransform), typeof(CanvasGroup), typeof(WorkFeedbackBubbleView));
                bubbleObject.transform.SetParent(root.transform, false);
                var bubble = bubbleObject.GetComponent<WorkFeedbackBubbleView>();
                var view = root.AddComponent<WorkFeedbackView>();
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                typeof(WorkFeedbackView).GetField("bubbles", flags).SetValue(view, new List<WorkFeedbackBubbleView> { bubble });
                root.SetActive(true);
                var message = new WorkFeedbackMessage(FeedbackResult.Success, "Test");
                view.Show(message); view.Show(message);
                var waiting = (ICollection)typeof(WorkFeedbackView).GetField("waiting", flags).GetValue(view);
                Assert.That(bubble.IsShowing, Is.True); Assert.That(waiting.Count, Is.EqualTo(1));
                view.SetSuppressed(true);
                Assert.That(bubble.IsShowing, Is.False); Assert.That(waiting.Count, Is.Zero);
                view.Show(message); Assert.That(waiting.Count, Is.Zero);
                view.SetSuppressed(false); Assert.That(bubble.IsShowing, Is.False);
                view.Show(message); Assert.That(bubble.IsShowing, Is.True);
                view.Clear();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
