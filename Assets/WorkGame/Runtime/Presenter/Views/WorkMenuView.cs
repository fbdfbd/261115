using System;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public class WorkMenuView : MonoBehaviour
    {
        [Header("기능 표시")]
        [SerializeField] private bool showLeave = true;
        [SerializeField] private bool showPause = true;
        [SerializeField] private bool showHelp = true;
        [Header("퇴근")]
        [SerializeField] private Button leaveButton;
        [SerializeField] private CanvasGroup leaveGroup;
        [Header("정지")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [Header("도움말 — 참조만 준비")]
        [SerializeField] private Button helpButton;
        [SerializeField] private GameObject helpPanel;
        private bool leaveVisible;
        private bool hasLeaveState;

        public event Action LeaveClicked;
        public event Action PauseClicked;
        public event Action ResumeClicked;
        public GameObject HelpPanel => helpPanel;

        private void Awake()
        {
            SetLeaveVisible(false);
            WorkUi.Show(pausePanel, false);
        }

        private void OnEnable()
        {
            if (leaveButton != null) leaveButton.onClick.AddListener(ClickLeave);
            if (pauseButton != null) pauseButton.onClick.AddListener(ClickPause);
            if (resumeButton != null) resumeButton.onClick.AddListener(ClickResume);
        }

        private void OnDisable()
        {
            if (leaveButton != null) leaveButton.onClick.RemoveListener(ClickLeave);
            if (pauseButton != null) pauseButton.onClick.RemoveListener(ClickPause);
            if (resumeButton != null) resumeButton.onClick.RemoveListener(ClickResume);
        }

        public void ShowState(bool canLeave, bool paused, bool working)
        {
            WorkUi.Show(leaveButton != null ? leaveButton.gameObject : null, showLeave);
            bool visible = showLeave && canLeave;
            if (!hasLeaveState || visible != leaveVisible)
            {
                hasLeaveState = true;
                leaveVisible = visible;
                SetLeaveVisible(visible);
            }
            if (leaveButton != null) leaveButton.interactable = showLeave && canLeave;
            WorkUi.Show(pauseButton != null ? pauseButton.gameObject : null, showPause);
            if (pauseButton != null) pauseButton.interactable = working && !paused;
            WorkUi.Show(pausePanel, working && paused);
            WorkUi.Show(helpButton != null ? helpButton.gameObject : null, showHelp);
        }

        // Keep visibility in one place so a fade can be added here later.
        public virtual void SetLeaveVisible(bool visible) => WorkUi.Show(leaveGroup, visible);
        private void ClickLeave() => LeaveClicked?.Invoke();
        private void ClickPause() => PauseClicked?.Invoke();
        private void ClickResume() => ResumeClicked?.Invoke();
    }
}
