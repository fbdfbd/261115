using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class GameOptionsView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backgroundButton;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle windowedToggle;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private TMP_Text musicValue;
        [SerializeField] private Slider soundSlider;
        [SerializeField] private TMP_Text soundValue;
        [SerializeField] private Button titleButton;
        [Header("크레딧 — 옵션 안에서 별도 패널로 표시")]
        [SerializeField] private Button creditsButton;
        [SerializeField] private CanvasGroup creditsGroup;
        [SerializeField] private Button creditsCloseButton;
        [SerializeField] private Button creditsBackgroundButton;
        [SerializeField] private TMP_Text errorText;
        public bool IsOpen { get; private set; }
        public event Action CloseRequested;
        public event Action TitleRequested;
        public event Action<int> ResolutionChanged;
        public event Action<bool> WindowedChanged;
        public event Action<float> MusicChanged;
        public event Action<float> SoundChanged;

        private void Awake() => Hide();
        private void OnEnable()
        {
            if (closeButton != null) closeButton.onClick.AddListener(RequestClose);
            if (backgroundButton != null) backgroundButton.onClick.AddListener(RequestClose);
            if (titleButton != null) titleButton.onClick.AddListener(RequestTitle);
            if (creditsButton != null) creditsButton.onClick.AddListener(OpenCredits);
            if (creditsCloseButton != null) creditsCloseButton.onClick.AddListener(CloseCredits);
            if (creditsBackgroundButton != null) creditsBackgroundButton.onClick.AddListener(CloseCredits);
            if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(ChangeResolution);
            if (windowedToggle != null) windowedToggle.onValueChanged.AddListener(ChangeWindowed);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(ChangeMusic);
            if (soundSlider != null) soundSlider.onValueChanged.AddListener(ChangeSound);
        }
        private void OnDisable()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(RequestClose);
            if (backgroundButton != null) backgroundButton.onClick.RemoveListener(RequestClose);
            if (titleButton != null) titleButton.onClick.RemoveListener(RequestTitle);
            if (creditsButton != null) creditsButton.onClick.RemoveListener(OpenCredits);
            if (creditsCloseButton != null) creditsCloseButton.onClick.RemoveListener(CloseCredits);
            if (creditsBackgroundButton != null) creditsBackgroundButton.onClick.RemoveListener(CloseCredits);
            if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(ChangeResolution);
            if (windowedToggle != null) windowedToggle.onValueChanged.RemoveListener(ChangeWindowed);
            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(ChangeMusic);
            if (soundSlider != null) soundSlider.onValueChanged.RemoveListener(ChangeSound);
            Hide();
        }
        public void SetResolutions(GameOptionsSystem options)
        {
            if (resolutionDropdown == null) return;
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(options.CanChangeDisplay ? options.Resolutions.Select(size => size.ToString()).ToList()
                : new System.Collections.Generic.List<string> { "기기 설정 사용" });
            resolutionDropdown.interactable = options.CanChangeDisplay;
        }
        public void ShowState(GameOptionsSystem options)
        {
            if (resolutionDropdown != null) resolutionDropdown.SetValueWithoutNotify(options.CanChangeDisplay ? options.ResolutionIndex : 0);
            if (windowedToggle != null)
            {
                windowedToggle.SetIsOnWithoutNotify(options.Windowed);
                windowedToggle.interactable = options.CanChangeDisplay;
            }
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(options.MusicVolume);
            if (soundSlider != null) soundSlider.SetValueWithoutNotify(options.SoundVolume);
            if (musicValue != null) musicValue.SetText("{0}%", Mathf.RoundToInt(options.MusicVolume * 100));
            if (soundValue != null) soundValue.SetText("{0}%", Mathf.RoundToInt(options.SoundVolume * 100));
        }
        public void Open() { IsOpen = true; ShowError(null); WorkUi.Show(panelGroup, true); }
        public void ShowTitleButton(bool show) { if (titleButton != null) WorkUi.Show(titleButton.gameObject, show); }
        public void Hide()
        {
            IsOpen = false; resolutionDropdown?.Hide(); CloseCredits(); WorkUi.Show(panelGroup, false);
        }
        public void ShowError(string error)
        {
            if (errorText == null) return;
            errorText.text = error ?? "";
            WorkUi.Show(errorText.gameObject, !string.IsNullOrEmpty(error));
        }
        private void RequestClose() => CloseRequested?.Invoke();
        private void RequestTitle() => TitleRequested?.Invoke();
        private void OpenCredits() => WorkUi.Show(creditsGroup, true);
        private void CloseCredits() => WorkUi.Show(creditsGroup, false);
        private void ChangeResolution(int value) => ResolutionChanged?.Invoke(value);
        private void ChangeWindowed(bool value) => WindowedChanged?.Invoke(value);
        private void ChangeMusic(float value) => MusicChanged?.Invoke(value);
        private void ChangeSound(float value) => SoundChanged?.Invoke(value);
    }
}
