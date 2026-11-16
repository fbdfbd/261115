using NUnit.Framework;

namespace WorkGame.Tests
{
    public sealed class WorkPresenterTests
    {
        private static WorkSystem NewSystem()
        {
            var choices = new[] { new WorkChoice("A", 10), new WorkChoice("B", 20), new WorkChoice("C", 30) };
            return new WorkSystem(new WorkSettings(), new[] {
                new WorkPaper(1, 1, "01", "Test", "제품", "점수", "", PickRule.Highest, PaperDisplay.Number, choices),
                new WorkPaper(2, 1, "01", "Test", "제품", "점수", "", PickRule.Highest, PaperDisplay.Number, choices)
            });
        }

        [Test]
        public void PresenterCanSelectAndClosePaperWithoutAScene()
        {
            using var system = NewSystem();
            using var presenter = new WorkPresenter(system);
            presenter.Connect();
            system.StartRun();
            presenter.OpenPaper(2);
            Assert.That(presenter.OpenedPaper.Id, Is.EqualTo(2));
            presenter.StartChoice(2);
            system.AddTime(0.25);
            presenter.ClosePaper();
            system.AddTime(1);
            Assert.That(presenter.OpenedPaper, Is.Null);
            Assert.That(system.Run.Papers[1].State, Is.EqualTo(PaperState.Pending));
            Assert.That(system.HoldProgress, Is.Zero);
        }

        [Test]
        public void SubmittedPaperClosesAndUpdatesMonthProgress()
        {
            using var system = NewSystem();
            using var presenter = new WorkPresenter(system);
            presenter.Connect();
            system.StartRun();
            presenter.OpenPaper(2);
            presenter.StartChoice(2);
            system.AddTime(0.5);
            Assert.That(presenter.OpenedPaper, Is.Null);
            Assert.That(system.MonthDone, Is.EqualTo(1));
        }

        [Test]
        public void DisconnectStopsInputAndReconnectRestoresIt()
        {
            using var system = NewSystem();
            using var presenter = new WorkPresenter(system);
            presenter.Connect();
            system.StartRun();
            presenter.OpenPaper(2);
            presenter.StartChoice(2);
            system.AddTime(0.25);
            presenter.Disconnect();
            presenter.StartChoice(2);
            system.AddTime(1);
            Assert.That(system.Run.Papers[1].State, Is.EqualTo(PaperState.Pending));
            presenter.Connect();
            presenter.Connect();
            presenter.StartChoice(2);
            system.AddTime(0.5);
            Assert.That(system.MonthDone, Is.EqualTo(1));
        }

        [Test]
        public void PresenterDisposalDoesNotDisposeTheSystemOwnedByRoot()
        {
            using var system = NewSystem();
            var presenter = new WorkPresenter(system);
            presenter.Connect();
            system.StartRun();
            presenter.OpenPaper(1);
            presenter.Dispose();
            presenter.OpenPaper(2);
            Assert.That(presenter.OpenedPaper, Is.Null);
            system.AddTime(120);
            Assert.That(system.CanLeave, Is.True);
        }
    }
}
