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
        private readonly WorkFaxView faxView;
        private readonly Dictionary<string, WorkCategorySO> categories = new Dictionary<string, WorkCategorySO>();
        private WorkPaper openedPaper;
        private int shownMonth = -1;
        private int shownPending = -1;
        private int shownFaxCount = -1;
        private bool shownSorted;
        private bool connected;
        private bool disposed;

        public WorkSystem System { get; }
        public WorkPaper OpenedPaper => openedPaper;

        public WorkPresenter(WorkSystem system, WorkMenuView menuView = null, WorkHudView hudView = null,
            WorkDeskView deskView = null, WorkPaperView paperView = null,
            IEnumerable<WorkCategorySO> categories = null, WorkFeedback feedback = null,
            WorkFeedbackView feedbackView = null, WorkFaxView faxView = null)
        {
            System = system ?? throw new ArgumentNullException(nameof(system));
            this.menuView = menuView;
            this.hudView = hudView;
            this.deskView = deskView;
            this.paperView = paperView;
            this.feedback = feedback;
            this.feedbackView = feedbackView;
            this.faxView = faxView;
            if (categories != null)
                foreach (var category in categories)
                    if (category != null) this.categories[category.Id] = category;
        }

        public void Connect()
        {
            if (disposed || connected) return;
            connected = true;
            System.Changed += Refresh;
            System.PaperSubmitted += OnPaperSubmitted;
            if (faxView != null) faxView.PaperDropped += SendFax;
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
            System.PaperSubmitted -= OnPaperSubmitted;
            if (faxView != null) faxView.PaperDropped -= SendFax;
            if (feedbackView != null) feedbackView.Clear();
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
            if (connected && System.TryInstantPaper(paperId)) return;
            if (!connected || !System.SelectPaper(paperId)) return;
            openedPaper = System.CurrentPaper;
            paperView?.Show(openedPaper, System.Upgrades.HighlightLevel, System.Run.Seconds);
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
        private void SendFax(int paperId)
        {
            if (connected) System.Fax.TrySend(paperId, faxView != null && faxView.ForceEnabled);
        }

        private void OnPaperSubmitted(WorkSubmitResult result)
        {
            hudView?.ShowSkillPointResult(System.Run.SkillPoints, result.IsRight, result.IsPointBoosted);
            if (feedback != null && feedbackView != null) feedbackView.Show(feedback.Pick(result));
        }

        private void Refresh()
        {
            bool newMonth = shownMonth != System.Run.Month;
            bool working = System.Run.Phase == WorkPhase.Working;
            bool sorted = System.Upgrades.SortUnlocked && System.Run.SortEnabled;
            bool faxEnabled = (faxView != null && faxView.ForceEnabled)
                || (System.Upgrades.FaxUnlocked && System.Run.FaxVisible);
            deskView?.SetSorted(sorted);
            deskView?.SetFaxDragEnabled(System.IsWorking && faxEnabled);
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
            if (newMonth || shownPending != System.PendingCount || shownFaxCount != System.Fax.Jobs.Count || shownSorted != sorted)
            {
                shownPending = System.PendingCount;
                shownFaxCount = System.Fax.Jobs.Count;
                shownSorted = sorted;
                deskView?.ShowPapers(System.MonthPapers, System.Run.Month);
            }
            if (openedPaper != null && (!working || openedPaper.IsInFax || openedPaper.State != PaperState.Pending))
            {
                openedPaper = null;
                paperView?.Hide();
            }
            if (openedPaper != null)
            {
                paperView?.ShowAssist(System.Upgrades.HighlightLevel, System.Run.Seconds);
                paperView?.ShowHold(System.HeldChoiceIndex, (float)System.HoldProgress, !System.IsPaused);
            }
            int faxCount = System.Fax.Jobs.Count;
            faxView?.ShowState(working && faxEnabled,
                !System.IsPaused && faxCount < System.Upgrades.FaxSlots, faxCount, System.Upgrades.FaxSlots,
                faxCount == 0 ? 0 : (float)System.Fax.Jobs[0].Progress);
            hudView?.ShowProgress(System.MonthDone, System.MonthTotal);
            hudView?.ShowSkillPoints(System.Run.SkillPoints);
            hudView?.ShowTime(System.RemainingSeconds, System.MonthSeconds);
            menuView?.ShowState(System.CanLeave, System.IsPaused, working);
        }

        public void Dispose()
        {
            if (disposed) return;
            Disconnect();
            disposed = true;
            openedPaper = null;
            // Unity 객체는 파괴된 뒤에도 C# 참조가 남으므로 ?. 대신 Unity null 검사를 씁니다.
            if (paperView != null) paperView.Hide();
            if (deskView != null) deskView.ClearPapers();
        }
    }
}
