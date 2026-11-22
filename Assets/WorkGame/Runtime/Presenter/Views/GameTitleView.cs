using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class GameTitleView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup menuGroup;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button collectionButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text errorText;
        [Header("엔딩을 하나라도 끝까지 본 뒤 이미지 교체")]
        [SerializeField] private UnityEngine.UI.Image endingImage;
        [SerializeField] private Sprite afterEndingSprite;
        public event Action NewGameRequested;
        public event Action ContinueRequested;
        public event Action CollectionRequested;
        public event Action SettingsRequested;
        public event Action QuitRequested;
        private bool collectionAvailable;
        private void OnEnable()
        {
            if (newGameButton != null) newGameButton.onClick.AddListener(NewGame);
            if (continueButton != null) continueButton.onClick.AddListener(Continue);
            if (collectionButton != null) collectionButton.onClick.AddListener(Collection);
            if (settingsButton != null) settingsButton.onClick.AddListener(Settings);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
        }
        private void OnDisable()
        {
            if (newGameButton != null) newGameButton.onClick.RemoveListener(NewGame);
            if (continueButton != null) continueButton.onClick.RemoveListener(Continue);
            if (collectionButton != null) collectionButton.onClick.RemoveListener(Collection);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(Settings);
            if (quitButton != null) quitButton.onClick.RemoveListener(Quit);
        }
        public void SetContinueAvailable(bool available) { if (continueButton != null) continueButton.interactable = available; }
        public void ShowEndingImage(bool hasReadEnding)
        {
            if (hasReadEnding && endingImage != null && afterEndingSprite != null)
                endingImage.sprite = afterEndingSprite;
        }
        public void SetCollectionAvailable(bool available)
        {
            collectionAvailable = available;
            if (collectionButton != null) collectionButton.interactable = available;
        }
        public void SetQuitAvailable(bool available) { if (quitButton != null) quitButton.interactable = available; }
        public void SetMenuInteractable(bool interactable) { if (menuGroup != null) menuGroup.interactable = interactable; }
        public void ShowError(string message)
        {
            if (errorText == null) return;
            errorText.text = message ?? ""; WorkUi.Show(errorText.gameObject, !string.IsNullOrEmpty(message));
        }
        private void NewGame() => NewGameRequested?.Invoke();
        private void Continue() => ContinueRequested?.Invoke();
        private void Collection() { if (collectionAvailable) CollectionRequested?.Invoke(); }
        private void Settings() => SettingsRequested?.Invoke();
        private void Quit() => QuitRequested?.Invoke();
    }
}
