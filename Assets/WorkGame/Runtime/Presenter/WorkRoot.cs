using System;
using System.Linq;
using UnityEngine;

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

        public WorkSystem System { get; private set; }
        public WorkPresenter Presenter { get; private set; }

        private void Start() => StartRun();
        private void OnEnable() => Presenter?.Connect();
        private void Update() => System?.AddTime(Time.unscaledDeltaTime);
        private void OnDisable() => Presenter?.Disconnect();
        private void OnDestroy() => DisposeRun();
        private void OnApplicationFocus(bool focused) { if (!focused) Presenter?.StopHold(); }
        private void OnApplicationPause(bool paused) { if (paused) Presenter?.StopHold(); }

        public void StartRun()
        {
            if (config == null) { Debug.LogError("WorkConfig is missing.", this); return; }
            WorkSystem next;
            int runSeed = useFixedSeed ? seed : UnityEngine.Random.Range(0, int.MaxValue);
            try { next = config.CreateSystem(runSeed); }
            catch (Exception e) { Debug.LogError(e.Message, this); return; }
            DisposeRun();
            System = next;
            var categories = config.Plan.Batches.Select(x => x.Category).Distinct();
            Presenter = new WorkPresenter(System, menuView, hudView, deskView, paperView, categories,
                CreateFeedback(runSeed), feedbackView);
            Presenter.Connect();
            System.StartRun();
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
        public void StartNextMonth() => Presenter?.StartNextMonth();
        public void Pause() => Presenter?.Pause();
        public void Resume() => Presenter?.Resume();

        private void DisposeRun()
        {
            Presenter?.Dispose();
            System?.Dispose();
            Presenter = null;
            System = null;
        }
    }
}
