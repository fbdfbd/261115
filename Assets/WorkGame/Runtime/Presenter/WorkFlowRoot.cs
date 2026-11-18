using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace WorkGame
{
    // Scene flow only. Work results, upgrades and month progression stay in their systems.
    public sealed class WorkFlowRoot : MonoBehaviour
    {
        [SerializeField] private WorkRoot workRoot;
        [Header("업무 중 표시할 그룹 — 부모 CanvasGroup 위주로 연결")]
        [SerializeField] private List<CanvasGroup> workGroups = new List<CanvasGroup>();
        [Header("퇴근 후 표시할 그룹 — 강화 진입 / 다음 달 버튼 등")]
        [SerializeField] private List<CanvasGroup> afterWorkGroups = new List<CanvasGroup>();
        [Header("버튼 — OnClick 수동 연결 없이 필드에 연결")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button nextMonthButton;
        [Header("월말 정산 — 씬에 배치한 프리팹 연결")]
        [SerializeField] private WorkMonthReportView monthReport;
        [SerializeField] private UnityEvent onRunFinished = new UnityEvent();
        private WorkSystem work;
        private WorkPhase lastPhase;
        private int lastMonth = -1;
        private int finishedReportMonth = -1;

        private void OnEnable()
        {
            if (monthReport != null) monthReport.Confirmed += FinishReport;
            if (upgradeButton != null) upgradeButton.onClick.AddListener(OpenUpgrades);
            if (nextMonthButton != null) nextMonthButton.onClick.AddListener(StartNextMonth);
            if (workRoot != null)
            {
                workRoot.RunStarted += Bind;
                workRoot.GameFinished += FinishRun;
                workRoot.CareFlowChanged += RefreshCareButtons;
                Bind(workRoot.System);
            }
            else ShowEmpty();
        }

        private void OnDisable()
        {
            if (monthReport != null)
            {
                monthReport.Confirmed -= FinishReport;
                monthReport.Hide();
            }
            if (upgradeButton != null) upgradeButton.onClick.RemoveListener(OpenUpgrades);
            if (nextMonthButton != null) nextMonthButton.onClick.RemoveListener(StartNextMonth);
            if (workRoot != null)
            {
                workRoot.RunStarted -= Bind;
                workRoot.GameFinished -= FinishRun;
                workRoot.CareFlowChanged -= RefreshCareButtons;
            }
            if (work != null) work.Changed -= Refresh;
            work = null;
        }

        private void Bind(WorkSystem system)
        {
            if (work != null) work.Changed -= Refresh;
            work = system;
            lastMonth = -1;
            finishedReportMonth = work != null && work.Run.Month == workRoot.LoadedMonth
                && workRoot.LoadedPoint.HasValue && workRoot.LoadedPoint != SavePoint.WorkStart
                ? work.Run.Month : -1;
            if (work == null) { ShowEmpty(); return; }
            work.Changed += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            if (work == null || (lastPhase == work.Run.Phase && lastMonth == work.Run.Month)) return;
            lastPhase = work.Run.Phase;
            lastMonth = work.Run.Month;
            bool working = lastPhase == WorkPhase.Working;
            bool afterWork = lastPhase == WorkPhase.AfterWork || lastPhase == WorkPhase.Finished;
            bool ending = workRoot.CareFlow?.Phase == CareFlowPhase.EndingChoice || workRoot.CareFlow?.Phase == CareFlowPhase.Ended;
            bool showReport = afterWork && !ending && monthReport != null && finishedReportMonth != lastMonth;
            workRoot.CloseUpgrades();
            // 정산서가 업무 그룹 안에 있어도 확인할 때까지 부모를 표시합니다.
            ShowGroups(workGroups, working || showReport);
            ShowGroups(afterWorkGroups, afterWork && !showReport && !ending);
            if (monthReport != null)
            {
                if (showReport) monthReport.Open(work.Run.LastResult);
                else monthReport.Hide();
            }
            if (afterWork && !showReport && !ending) workRoot.EnterCare();
            RefreshCareButtons();
        }

        private void RefreshCareButtons()
        {
            if (work == null) return;
            if (workRoot.CareFlow?.Phase == CareFlowPhase.EndingChoice || workRoot.CareFlow?.Phase == CareFlowPhase.Ended)
            {
                ShowGroups(workGroups, false);
                ShowGroups(afterWorkGroups, false);
            }
            bool afterWork = work.Run.Phase == WorkPhase.AfterWork || work.Run.Phase == WorkPhase.Finished;
            bool reportDone = monthReport == null || finishedReportMonth == work.Run.Month;
            var flow = workRoot.CareFlow;
            bool free = afterWork && reportDone && (flow == null || flow.Phase == CareFlowPhase.Free);
            if (upgradeButton != null) upgradeButton.interactable = free && workRoot.Upgrades != null;
            if (nextMonthButton != null) nextMonthButton.interactable = free
                && (flow != null ? flow.CanGoNext : work.Run.Phase == WorkPhase.AfterWork);
        }

        public void OpenUpgrades()
        {
            if (workRoot == null || work == null || workRoot.Upgrades == null) return;
            if (monthReport != null && monthReport.IsOpen) return;
            if (work.Run.Phase == WorkPhase.AfterWork || work.Run.Phase == WorkPhase.Finished) workRoot.OpenUpgrades();
        }

        public void StartNextMonth()
        {
            if (workRoot == null || work == null) return;
            if (work.Run.Phase != WorkPhase.AfterWork && !(work.Run.Phase == WorkPhase.Finished && workRoot.Care != null)) return;
            if (monthReport != null && finishedReportMonth != work.Run.Month) return;
            if (workRoot.CareFlow != null && !workRoot.CareFlow.CanGoNext) return;
            workRoot.CloseUpgrades();
            workRoot.StartNextMonth();
        }

        private void FinishReport()
        {
            if (work == null) return;
            finishedReportMonth = work.Run.Month;
            lastMonth = -1;
            Refresh();
            if (work.Run.Phase == WorkPhase.Finished && workRoot.Care == null) onRunFinished.Invoke();
        }

        private void FinishRun()
        {
            workRoot.CloseUpgrades();
            ShowGroups(workGroups, false);
            ShowGroups(afterWorkGroups, false);
            if (nextMonthButton != null) nextMonthButton.interactable = false;
            onRunFinished.Invoke();
        }

        private void ShowEmpty()
        {
            if (monthReport != null) monthReport.Hide();
            ShowGroups(workGroups, false);
            ShowGroups(afterWorkGroups, false);
            if (upgradeButton != null) upgradeButton.interactable = false;
            if (nextMonthButton != null) nextMonthButton.interactable = false;
            if (workRoot != null) workRoot.CloseUpgrades();
        }

        private static void ShowGroups(List<CanvasGroup> groups, bool visible)
        {
            foreach (var group in groups) WorkUi.Show(group, visible);
        }
    }
}
