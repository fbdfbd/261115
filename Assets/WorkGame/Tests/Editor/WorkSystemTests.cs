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
