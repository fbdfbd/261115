using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public sealed class WorkPresenter : IDisposable
    {
        private readonly WorkMenuView menuView;
        private readonly WorkHudView hudView;
        private readonly WorkDeskView deskView;
        private readonly WorkPaperView paperView;
        private readonly WorkFeedback feedback;
        private readonly WorkFeedbackView feedbackView;
        private readonly Dictionary<string, WorkCategorySO> categories = new Dictionary<string, WorkCategorySO>();
        private WorkPaper openedPaper;
        private int shownMonth = -1;
        private int shownPending = -1;
        private bool connected;
        private bool disposed;

        public WorkSystem System { get; }
        public WorkPaper OpenedPaper => openedPaper;

        public WorkPresenter(WorkSystem system, WorkMenuView menuView = null, WorkHudView hudView = null,
            WorkDeskView deskView = null, WorkPaperView paperView = null,
            IEnumerable<WorkCategorySO> categories = null, WorkFeedback feedback = null,
            WorkFeedbackView feedbackView = null)
        {
            System = system ?? throw new ArgumentNullException(nameof(system));
            this.menuView = menuView;
            this.hudView = hudView;
            this.deskView = deskView;
            this.paperView = paperView;
            this.feedback = feedback;
            this.feedbackView = feedbackView;
            if (categories != null)
                foreach (var category in categories)
                    if (category != null) this.categories[category.Id] = category;
        }

        public void Connect()
        {
            if (disposed || connected) return;
            connected = true;
            System.Changed += Refresh;
            System.PaperSubmitted += ShowFeedback;
            if (menuView != null)
            {
                menuView.LeaveClicked += LeaveWork;
                menuView.PauseClicked += Pause;
                menuView.ResumeClicked += Resume;
            }
            if (deskView != null) deskView.PaperClicked += OpenPaper;
            if (paperView != null)
            {
                paperView.CloseClicked += ClosePaper;
                paperView.ChoiceStarted += StartChoice;
                paperView.ChoiceStopped += StopHold;
            }
            if (System.Run.Phase != WorkPhase.Ready) Refresh();
        }

        public void Disconnect()
        {
            if (!connected) return;
            connected = false;
            System.Changed -= Refresh;
            System.PaperSubmitted -= ShowFeedback;
            feedbackView?.Clear();
            if (menuView != null)
            {
                menuView.LeaveClicked -= LeaveWork;
                menuView.PauseClicked -= Pause;
                menuView.ResumeClicked -= Resume;
            }
            if (deskView != null) deskView.PaperClicked -= OpenPaper;
            if (paperView != null)
            {
                paperView.CloseClicked -= ClosePaper;
                paperView.ChoiceStarted -= StartChoice;
                paperView.ChoiceStopped -= StopHold;
                paperView.ShowHold(-1, 0, false);
            }
            System.StopHold();
        }

        public void OpenPaper(int paperId)
        {
            if (!connected || !System.SelectPaper(paperId)) return;
            openedPaper = System.CurrentPaper;
            paperView?.Show(openedPaper);
        }

        public void ClosePaper()
        {
            if (disposed) return;
            openedPaper = null;
            System?.ClearPaper();
            paperView?.Hide();
        }

        public void StartHold(int paperId, int choiceIndex)
        {
            if (connected && openedPaper != null && openedPaper.Id == paperId) System.StartHold(paperId, choiceIndex);
        }
        public void StartChoice(int choiceIndex)
        {
            if (openedPaper != null) StartHold(openedPaper.Id, choiceIndex);
        }
        public void StopHold() { if (!disposed) System.StopHold(); }
        public void LeaveWork() { if (connected) System.LeaveWork(); }
        public void StartNextMonth() { if (connected) System.StartNextMonth(); }
        public void Pause() { if (connected) System.SetPaused(true); }
        public void Resume() { if (connected) System.SetPaused(false); }

        private void ShowFeedback(WorkSubmitResult result)
        {
            if (feedback != null && feedbackView != null) feedbackView.Show(feedback.Pick(result));
        }

        private void Refresh()
        {
            bool newMonth = shownMonth != System.Run.Month;
            bool working = System.Run.Phase == WorkPhase.Working;
            deskView?.SetWorking(working);
            if (newMonth)
            {
                shownMonth = System.Run.Month;
                feedbackView?.Clear();
                deskView?.ClearPapers();
                var monthCategories = System.MonthPapers.Where(x => x.Month == System.Run.Month)
                    .Select(x => x.CategoryId).Distinct()
                    .Where(categories.ContainsKey).Select(x => categories[x]);
                hudView?.ShowMonth(System.Run.Month, monthCategories);
            }
            if (newMonth || shownPending != System.PendingCount)
            {
                shownPending = System.PendingCount;
                deskView?.ShowPapers(System.MonthPapers, System.Run.Month);
            }
            if (openedPaper != null && (!working || openedPaper.State != PaperState.Pending))
            {
                openedPaper = null;
                paperView?.Hide();
            }
            if (openedPaper != null)
                paperView?.ShowHold(System.HeldChoiceIndex, (float)System.HoldProgress, !System.IsPaused);
            hudView?.ShowProgress(System.MonthDone, System.MonthTotal);
            menuView?.ShowState(System.CanLeave, System.IsPaused, working);
        }

        public void Dispose()
        {
            if (disposed) return;
            Disconnect();
            disposed = true;
            openedPaper = null;
            paperView?.Hide();
            deskView?.ClearPapers();
        }
    }
}
