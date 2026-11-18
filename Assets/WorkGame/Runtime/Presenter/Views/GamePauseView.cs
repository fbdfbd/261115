using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace WorkGame
{
    public sealed class GamePauseView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button backgroundButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [FormerlySerializedAs("quitButton")]
        [SerializeField] private Button titleButton;
        [SerializeField] private TMP_Text errorText;
        [Header("공용 도움말")]
        [SerializeField] private CanvasGroup helpGroup;
        [SerializeField] private Button helpCloseButton;
        [SerializeField] private Button helpBackgroundButton;
        public bool IsHelpOpen { get; private set; }
        public event Action ResumeRequested;
        public event Action SettingsRequested;
        public event Action SaveRequested;
        public event Action LoadRequested;
        public event Action TitleRequested;
        public event Action HelpCloseRequested;

        private void Awake() => Hide();
        private void OnEnable()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (backgroundButton != null) backgroundButton.onClick.AddListener(Resume);
            if (settingsButton != null) settingsButton.onClick.AddListener(Settings);
            if (saveButton != null) saveButton.onClick.AddListener(Save);
            if (loadButton != null) loadButton.onClick.AddListener(Load);
            if (titleButton != null) titleButton.onClick.AddListener(Title);
            if (helpCloseButton != null) helpCloseButton.onClick.AddListener(CloseHelp);
            if (helpBackgroundButton != null) helpBackgroundButton.onClick.AddListener(CloseHelp);
        }
        private void OnDisable()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
            if (backgroundButton != null) backgroundButton.onClick.RemoveListener(Resume);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(Settings);
            if (saveButton != null) saveButton.onClick.RemoveListener(Save);
            if (loadButton != null) loadButton.onClick.RemoveListener(Load);
            if (titleButton != null) titleButton.onClick.RemoveListener(Title);
            if (helpCloseButton != null) helpCloseButton.onClick.RemoveListener(CloseHelp);
            if (helpBackgroundButton != null) helpBackgroundButton.onClick.RemoveListener(CloseHelp);
            Hide();
        }
        public void ShowPause(bool show) => WorkUi.Show(panelGroup, show);
        public void ShowHelp(bool show) { IsHelpOpen = show; WorkUi.Show(helpGroup, show); }
        public void SetSettingsAvailable(bool available) { if (settingsButton != null) settingsButton.interactable = available; }
        public void ShowError(string message)
        {
            if (errorText == null) return;
            errorText.text = message ?? ""; WorkUi.Show(errorText.gameObject, !string.IsNullOrEmpty(message));
        }
        public void Hide() { ShowPause(false); ShowHelp(false); ShowError(null); }
        private void Resume() => ResumeRequested?.Invoke();
        private void Settings() => SettingsRequested?.Invoke();
        private void Save() => SaveRequested?.Invoke();
        private void Load() => LoadRequested?.Invoke();
        private void Title() => TitleRequested?.Invoke();
        private void CloseHelp() => HelpCloseRequested?.Invoke();
    }
}
