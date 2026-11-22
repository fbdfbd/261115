using System.Collections;
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
        [Tooltip("대화 중 숨길 일반 Care UI만 연결합니다. 다이얼로그와 그 부모 그룹은 제외하세요.")]
        [SerializeField] private List<CanvasGroup> afterWorkGroups = new List<CanvasGroup>();
        [Header("육성 배경 — 대화 중에도 표시")]
        [SerializeField] private CanvasGroup careBackgroundGroup;
        [SerializeField] private CareWindowView careWindowView;
        [Header("Care 전용 캐릭터 — 일반 UI 그룹과 분리, 대화 중에도 표시")]
        [SerializeField] private GameObject careCharacterRoot;
        [Header("Care 장착 가구 — 대화 중에도 표시")]
        [SerializeField] private CareRoomFurnitureView roomFurniture;
        [Header("버튼 — OnClick 수동 연결 없이 필드에 연결")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button nextMonthButton;
        [Header("월말 정산 — 씬에 배치한 프리팹 연결")]
        [SerializeField] private WorkMonthReportView monthReport;
        [Header("정산서 종료 후 전환 — 씬의 DiagonalTransition 연결")]
        [SerializeField] private DiagonalTransitionView reportTransition;
        [SerializeField] private UnityEvent onRunFinished = new UnityEvent();
        private WorkSystem work;
        private WorkPhase lastPhase;
        private int lastMonth = -1;
        private int finishedReportMonth = -1;
        private Coroutine reportTransitionRoutine;
        private DiagonalTransitionView activeReportTransition;
        private UnityAction reportCoveredHandler;
        private bool reportTransitionPending;

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
                if (workRoot.EndingView != null) workRoot.EndingView.SceneChanged += RefreshCareButtons;
                Bind(workRoot.System);
            }
            else ShowEmpty();
        }

        private void OnDisable()
        {
            if (roomFurniture != null) roomFurniture.SetVisible(false);
            CancelReportTransition();
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
                if (workRoot.EndingView != null) workRoot.EndingView.SceneChanged -= RefreshCareButtons;
            }
            if (work != null) work.Changed -= Refresh;
            work = null;
        }

        private void Bind(WorkSystem system)
        {
            CancelReportTransition();
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
            // Once covered, allow the Care screen to replace work under the transition.
            if (reportTransitionPending && (work == null || finishedReportMonth != work.Run.Month)) return;
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
            var flow = workRoot.CareFlow;
            bool intro = flow?.IsIntro == true;
            bool ending = flow?.Phase == CareFlowPhase.EndingChoice || flow?.Phase == CareFlowPhase.Ended;
            bool endingRoom = workRoot.EndingView != null && workRoot.EndingView.IsOpen && workRoot.EndingView.RoomVisible;
            if (ending) ShowGroups(workGroups, false);
            bool afterWork = work.Run.Phase == WorkPhase.AfterWork || work.Run.Phase == WorkPhase.Finished;
            bool reportDone = monthReport == null || finishedReportMonth == work.Run.Month;
            WorkUi.Show(careBackgroundGroup, intro || endingRoom || (afterWork && reportDone && !ending));
            if (endingRoom) careWindowView?.Show(CareLeaveKind.OnTime);
            if (intro) careWindowView?.Show(CareLeaveKind.OnTime);
            if (afterWork && reportDone && !ending && flow != null) careWindowView?.Show(flow.LeaveKind);
            if (roomFurniture != null) roomFurniture.SetVisible(endingRoom || (afterWork && reportDone && !ending && flow != null));
            if (careCharacterRoot != null)
            {
                bool showCharacter = endingRoom ? workRoot.EndingView.CharacterVisible
                    : intro ? flow.Dialogue.IsOpen && !flow.Dialogue.Line.HideCharacter
                    : afterWork && reportDone && !ending && flow != null;
                if (careCharacterRoot.activeSelf != showCharacter) careCharacterRoot.SetActive(showCharacter);
            }
            bool dialogueOpen = flow != null && flow.Dialogue.IsOpen;
            ShowGroups(afterWorkGroups, afterWork && reportDone && !ending && !dialogueOpen);
            bool free = !reportTransitionPending && afterWork && reportDone && (flow == null || flow.Phase == CareFlowPhase.Free);
            if (upgradeButton != null) upgradeButton.interactable = free && workRoot.Upgrades != null;
            if (nextMonthButton != null) nextMonthButton.interactable = free
                && (flow != null ? flow.CanGoNext : work.Run.Phase == WorkPhase.AfterWork);
        }

        public void OpenUpgrades()
        {
            if (reportTransitionPending) return;
            if (workRoot == null || work == null || workRoot.Upgrades == null) return;
            if (monthReport != null && monthReport.IsOpen) return;
            if (work.Run.Phase == WorkPhase.AfterWork || work.Run.Phase == WorkPhase.Finished) workRoot.OpenUpgrades();
        }

        public void StartNextMonth()
        {
            if (reportTransitionPending) return;
            if (workRoot == null || work == null) return;
            if (work.Run.Phase != WorkPhase.AfterWork && !(work.Run.Phase == WorkPhase.Finished && workRoot.Care != null)) return;
            if (monthReport != null && finishedReportMonth != work.Run.Month) return;
            if (workRoot.CareFlow != null && !workRoot.CareFlow.CanGoNext) return;
            workRoot.CloseUpgrades();
            workRoot.StartNextMonth();
        }

        private void FinishReport()
        {
            if (work == null || reportTransitionPending || finishedReportMonth == work.Run.Month) return;
            if (reportTransition != null && reportTransition.isActiveAndEnabled)
            {
                reportTransitionPending = true;
                activeReportTransition = reportTransition;
                RefreshCareButtons();
                reportTransitionRoutine = StartCoroutine(PlayReportTransition(work, work.Run.Month));
                return;
            }
            CompleteReport();
        }

        private IEnumerator PlayReportTransition(WorkSystem source, int month)
        {
            reportCoveredHandler = () =>
            {
                if (work == source && work.Run.Month == month
                    && (work.Run.Phase == WorkPhase.AfterWork || work.Run.Phase == WorkPhase.Finished))
                    CompleteReport();
            };
            activeReportTransition.Covered.AddListener(reportCoveredHandler);
            activeReportTransition.Play();
            // Assign the coroutine handle before completion, even if a listener cancels playback.
            yield return null;
            while (activeReportTransition != null && activeReportTransition.isActiveAndEnabled
                && activeReportTransition.IsPlaying)
                yield return null;

            reportTransitionRoutine = null;
            RemoveReportCoveredListener();
            activeReportTransition = null;
            reportTransitionPending = false;
            if (work == source && work.Run.Month == month
                && (work.Run.Phase == WorkPhase.AfterWork || work.Run.Phase == WorkPhase.Finished))
            {
                // Also proceed if the transition was interrupted before reaching full cover.
                CompleteReport();
                RefreshCareButtons();
            }
            else
            {
                lastMonth = -1;
                Refresh();
            }
        }

        private void CancelReportTransition()
        {
            if (reportTransitionRoutine != null) StopCoroutine(reportTransitionRoutine);
            reportTransitionRoutine = null;
            RemoveReportCoveredListener();
            if (activeReportTransition != null) activeReportTransition.HideImmediate();
            activeReportTransition = null;
            reportTransitionPending = false;
        }

        private void RemoveReportCoveredListener()
        {
            if (activeReportTransition != null && reportCoveredHandler != null)
                activeReportTransition.Covered.RemoveListener(reportCoveredHandler);
            reportCoveredHandler = null;
        }

        private void CompleteReport()
        {
            if (work == null || finishedReportMonth == work.Run.Month) return;
            finishedReportMonth = work.Run.Month;
            lastMonth = -1;
            Refresh();
            if (work.Run.Phase == WorkPhase.Finished && workRoot.Care == null) onRunFinished.Invoke();
        }

        private void FinishRun()
        {
            WorkUi.Show(careBackgroundGroup, false);
            if (roomFurniture != null) roomFurniture.SetVisible(false);
            CancelReportTransition();
            if (careCharacterRoot != null) careCharacterRoot.SetActive(false);
            workRoot.CloseUpgrades();
            ShowGroups(workGroups, false);
            ShowGroups(afterWorkGroups, false);
            if (nextMonthButton != null) nextMonthButton.interactable = false;
            RefreshCareButtons();
            onRunFinished.Invoke();
        }

        private void ShowEmpty()
        {
            WorkUi.Show(careBackgroundGroup, false);
            if (roomFurniture != null) roomFurniture.SetVisible(false);
            if (careCharacterRoot != null) careCharacterRoot.SetActive(false);
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
