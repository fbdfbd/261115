using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class WorkRoot : MonoBehaviour
    {
        [SerializeField] private WorkConfigSO config;
        [Tooltip("끄면 매 실행마다 다른 업무를 뽑습니다. 켜면 Seed로 같은 업무를 재현합니다.")]
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int seed = 1;
        [Header("캔버스별 View")]
        [SerializeField] private WorkMenuView menuView;
        [SerializeField] private WorkHudView hudView;
        [SerializeField] private WorkDeskView deskView;
        [SerializeField] private WorkPaperView paperView;
        [Header("판정 토스트 — 선택 사항")]
        [SerializeField] private WorkFeedbackView feedbackView;
        [SerializeField] private WorkFeedbackSO feedbackLines;
        [SerializeField] private WorkFeedbackSettingsSO feedbackSettings;
        [Header("강화 트리 — 화면 진입은 UI 버튼에서 연결")]
        [SerializeField] private WorkUpgradeTreeSO upgradeTree;
        [SerializeField] private WorkUpgradeTreeView upgradeView;
        [SerializeField] private WorkFaxView faxView;
        [Header("육성 — 미연결 시 기존 업무 루프 유지")]
        [SerializeField] private CareConfigSO careConfig;
        [Header("달이 상태 — 씬에 배치한 View 연결")]
        [SerializeField] private CareHudView careHudView;
        [SerializeField] private CareStatusView careStatusView;
        [Header("가구함 — 메인 진입 버튼과 씬 View")]
        [SerializeField] private CareFurnitureView careFurnitureView;
        [SerializeField] private Button furnitureButton;
        [Header("달이 대화 — 로그 포함 프리팹")]
        [SerializeField] private CareDialogueView careDialogueView;
        [Header("시간표 — 메인 진입 버튼과 씬 View")]
        [SerializeField] private CareScheduleView careScheduleView;
        [SerializeField] private Button scheduleButton;
        [Header("육성 결과 — 확인 후 약속 대화 / 다음 달")]
        [SerializeField] private CareMonthReportView careMonthReportView;
        [Header("엔딩 — 다른 전환 그룹 바깥에 배치한 전용 View")]
        [SerializeField] private CareEndingView careEndingView;
        private WorkUpgradePresenter upgradePresenter;
        private CareStatusPresenter careStatusPresenter;
        private CareFurniturePresenter careFurniturePresenter;
        private CareDialoguePresenter careDialoguePresenter;
        private CareSchedulePresenter careSchedulePresenter;
        private CareEndingPresenter careEndingPresenter;
        private CareMonthReportPresenter careMonthReportPresenter;
        private Func<string, bool> dialogueReadCheck;

        public WorkSystem System { get; private set; }
        public WorkPresenter Presenter { get; private set; }
        public WorkUpgradeSystem Upgrades { get; private set; }
        public CareSystem Care { get; private set; }
        public CarePresenter CarePresenter { get; private set; }
        public CareFlowSystem CareFlow { get; private set; }
        public event Action<WorkSystem> RunStarted;
        public event Action<CareMonthResult> CareMonthFinished;
        public event Action<CareEndingResult> CareEndingReady;
        public event Action GameFinished;
        public event Action<string> CareBlocked;
        public event Action CareFlowChanged;
        public event Action FurnitureRequested;
        public event Action<string> CareDialogueReadCompleted;

        private void Start() => StartRun();
        private void OnEnable()
        {
            Presenter?.Connect(); upgradePresenter?.Connect();
            careStatusPresenter?.Connect();
            careFurniturePresenter?.Connect();
            careDialoguePresenter?.Connect();
            careSchedulePresenter?.Connect();
            careEndingPresenter?.Connect();
            careMonthReportPresenter?.Connect();
            if (upgradeView != null) upgradeView.Closed += OnUpgradeClosed;
        }
        private void Update() => System?.AddTime(Time.unscaledDeltaTime);
        private void OnDisable()
        {
            if (upgradeView != null) upgradeView.Closed -= OnUpgradeClosed;
            careStatusPresenter?.Disconnect();
            careFurniturePresenter?.Disconnect();
            careDialoguePresenter?.Disconnect();
            careSchedulePresenter?.Disconnect();
            careEndingPresenter?.Disconnect();
            careMonthReportPresenter?.Disconnect();
            upgradePresenter?.Disconnect(); Presenter?.Disconnect();
        }
        private void OnDestroy() => DisposeRun();
        private void OnApplicationFocus(bool focused) { if (!focused) Presenter?.StopHold(); }
        private void OnApplicationPause(bool paused) { if (paused) Presenter?.StopHold(); }

        public void StartRun()
        {
            if (config == null) { Debug.LogError("WorkConfig is missing.", this); return; }
            WorkSystem next = null;
            CareSystem nextCare = null;
            CareFlowSystem nextCareFlow = null;
            int runSeed = useFixedSeed ? seed : UnityEngine.Random.Range(0, int.MaxValue);
            try
            {
                next = config.CreateSystem(runSeed);
                if (careConfig != null)
                {
                    nextCare = careConfig.CreateSystem(next);
                    nextCareFlow = new CareFlowSystem(next, nextCare, careConfig.CreateDialogues());
                }
            }
            catch (Exception e)
            {
                nextCareFlow?.Dispose();
                nextCare?.Dispose();
                next?.Dispose();
                Debug.LogError(e.Message, this);
                return;
            }
            DisposeRun();
            System = next;
            Care = nextCare;
            CareFlow = nextCareFlow;
            if (Care != null)
            {
                CareFlow.Changed += OnCareFlowChanged;
                CareFlow.Finished += OnGameFinished;
                CareFlow.Blocked += OnCareBlocked;
                CareFlow.Dialogue.SetReadCheck(dialogueReadCheck);
                CareFlow.Dialogue.ReadCompleted += OnDialogueRead;
                CarePresenter = new CarePresenter(Care, CareFlow);
                CarePresenter.MonthFinished += ShowCareMonth;
                CarePresenter.EndingReady += ShowCareEnding;
                careStatusPresenter = new CareStatusPresenter(Care, CareFlow, careHudView, careStatusView, careConfig);
                careStatusPresenter.FurnitureRequested += RequestFurniture;
                careStatusPresenter.Connect();
                careFurniturePresenter = new CareFurniturePresenter(System, CarePresenter, CareFlow,
                    careFurnitureView, furnitureButton, careConfig);
                careFurniturePresenter.Connect();
                careDialoguePresenter = new CareDialoguePresenter(System, CareFlow, careDialogueView);
                careDialoguePresenter.Connect();
                careSchedulePresenter = new CareSchedulePresenter(System, CarePresenter, CareFlow,
                    careScheduleView, scheduleButton, careConfig);
                careSchedulePresenter.Connect();
                careEndingPresenter = new CareEndingPresenter(Care, System, careEndingView, careConfig);
                careEndingPresenter.Connect();
                careMonthReportPresenter = new CareMonthReportPresenter(Care, CareFlow, careMonthReportView, careConfig);
                careMonthReportPresenter.Connect();
            }
            Upgrades = CreateUpgrades();
            var categories = config.Plan.Batches.Select(x => x.Category).Distinct();
            Presenter = new WorkPresenter(System, menuView, hudView, deskView, paperView, categories,
                CreateFeedback(runSeed), feedbackView, faxView);
            Presenter.Connect();
            if (Upgrades != null && upgradeView != null)
            {
                upgradePresenter = new WorkUpgradePresenter(System, Upgrades, upgradeView);
                upgradePresenter.Connect();
            }
            System.StartRun();
            RunStarted?.Invoke(System);
        }

        private WorkUpgradeSystem CreateUpgrades()
        {
            if (upgradeTree == null) return null;
            try
            {
                var nodes = upgradeTree.CreateNodes();
                System.Fax.SetSecondsPerPaper(upgradeTree.FaxSeconds);
                return new WorkUpgradeSystem(System, nodes);
            }
            catch (Exception e) { Debug.LogError("강화 트리: " + e.Message, this); return null; }
        }

        private WorkFeedback CreateFeedback(int runSeed)
        {
            if (feedbackView == null) return null;
            if (feedbackLines == null || feedbackSettings == null)
            {
                Debug.LogWarning("판정 토스트의 대사 SO와 단계 설정 SO를 연결해 주세요.", this);
                return null;
            }
            try
            {
                return new WorkFeedback(feedbackLines.Lines, feedbackSettings.SuccessCounts,
                    feedbackSettings.FailureCounts, unchecked(runSeed + 7919));
            }
            catch (Exception e) { Debug.LogWarning("판정 토스트: " + e.Message, this); return null; }
        }

        // Unity UI events can target this component; the Presenter stays a plain C# object.
        public void OpenPaper(int paperId) => Presenter?.OpenPaper(paperId);
        public void ClosePaper() => Presenter?.ClosePaper();
        public void StartHold(int paperId, int choiceIndex) => Presenter?.StartHold(paperId, choiceIndex);
        public void StartChoice(int choiceIndex) => Presenter?.StartChoice(choiceIndex);
        public void StopHold() => Presenter?.StopHold();
        public void LeaveWork() => Presenter?.LeaveWork();
        public void StartNextMonth()
        {
            if (CareFlow == null) { Presenter?.StartNextMonth(); return; }
            CareFlow.GoNext();
        }

        // 육성 결과창 확인 버튼에서 호출합니다. 한 달에 한 번만 다음 단계로 갑니다.
        public void ContinueAfterCare() => CareFlow?.ConfirmResult();
        public void EnterCare() => CareFlow?.EnterCare();
        public void OpenCarePanel(int kind)
        {
            if (!Enum.IsDefined(typeof(CarePanel), kind)) return;
            if ((CarePanel)kind == CarePanel.Skills) OpenUpgrades();
            else CareFlow?.OpenPanel((CarePanel)kind);
        }
        public void CloseCarePanel()
        {
            if (CareFlow?.Panel == CarePanel.Skills) CloseUpgrades();
            else CareFlow?.ClosePanel();
        }
        public void TalkToDali() => CareFlow?.Talk();
        public void NextCareDialogue() => CareFlow?.Dialogue.Next();
        public void ChooseCareDialogue(int index) => CareFlow?.Dialogue.Choose(index);
        public void SetDialogueReadCheck(Func<string, bool> check)
        {
            dialogueReadCheck = check;
            CareFlow?.Dialogue.SetReadCheck(check);
        }
        private void OnDialogueRead(string id) => CareDialogueReadCompleted?.Invoke(id);
        public void SetCareDialogueBlocked(bool blocked) => careDialogueView?.SetInputBlocked(blocked);
        public void ClearCareLesson(int slot) => CarePresenter?.ClearLesson(slot);
        public void OpenSchedule() => careSchedulePresenter?.Open();
        private void OnCareFlowChanged() => CareFlowChanged?.Invoke();
        private void OnCareBlocked(string message) => CareBlocked?.Invoke(message);
        private void OnGameFinished() => GameFinished?.Invoke();
        public void OpenFurniture() => careFurniturePresenter?.Open();
        private void RequestFurniture()
        {
            OpenFurniture();
            FurnitureRequested?.Invoke();
        }

        public void SetHomeLesson(int slot) => CarePresenter?.SetHome(slot);
        public void SetRestLesson(int slot) => CarePresenter?.SetRest(slot);
        public void BuyGift(string id) => CarePresenter?.BuyGift(id);
        public void SetActiveGift(string id) => CarePresenter?.SetActiveGift(id);
        public void ClearActiveGift(int kind)
        {
            if (Enum.IsDefined(typeof(CareGiftKind), kind)) CarePresenter?.ClearActiveGift((CareGiftKind)kind);
        }
        public void AddFriendship(int amount) => CarePresenter?.AddFriendship(amount);
        private void ShowCareMonth(CareMonthResult result) => CareMonthFinished?.Invoke(result);
        private void ShowCareEnding(CareEndingResult result) => CareEndingReady?.Invoke(result);
        public void Pause() => Presenter?.Pause();
        public void Resume() => Presenter?.Resume();
        public void OpenUpgrades()
        {
            if (upgradeView == null || (CareFlow != null && !CareFlow.OpenPanel(CarePanel.Skills))) return;
            upgradeView.Open();
        }
        public void CloseUpgrades() => upgradeView?.Close();
        private void OnUpgradeClosed()
        {
            if (CareFlow?.Panel == CarePanel.Skills) CareFlow.ClosePanel();
        }
        public void BuyUpgrade(string id) => Upgrades?.Buy(id);
        public void SetSortEnabled(bool enabled) => System?.SetSortEnabled(enabled);
        public void SetFaxVisible(bool visible) => System?.SetFaxVisible(visible);

        private void DisposeRun()
        {
            careMonthReportPresenter?.Dispose();
            careMonthReportPresenter = null;
            careEndingPresenter?.Dispose();
            careEndingPresenter = null;
            careSchedulePresenter?.Dispose();
            careSchedulePresenter = null;
            careDialoguePresenter?.Dispose();
            careDialoguePresenter = null;
            if (CareFlow != null) CareFlow.Dialogue.ReadCompleted -= OnDialogueRead;
            careFurniturePresenter?.Dispose();
            careFurniturePresenter = null;
            careStatusPresenter?.Dispose();
            careStatusPresenter = null;
            CareFlow?.Dispose();
            CareFlow = null;
            CarePresenter?.Dispose();
            CarePresenter = null;
            Care?.Dispose();
            Care = null;
            upgradePresenter?.Dispose();
            upgradePresenter = null;
            Upgrades = null;
            Presenter?.Dispose();
            System?.Dispose();
            Presenter = null;
            System = null;
        }
    }
}
