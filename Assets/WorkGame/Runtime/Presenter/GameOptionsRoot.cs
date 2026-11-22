using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class GameOptionsRoot : MonoBehaviour
    {
        [SerializeField] private GameOptionsView view;
        [SerializeField] private WorkRoot workRoot;
        [SerializeField] private List<Button> openButtons = new List<Button>();
        [SerializeField] private string titleScene = "01_Title";
        [Header("음량 연결 — 0~1, 나중에 AudioManager 등에 연결")]
        [SerializeField] private UnityEvent<float> onMusicVolume = new UnityEvent<float>();
        [SerializeField] private UnityEvent<float> onSoundVolume = new UnityEvent<float>();
        private GameOptionsPresenter presenter;
        private WorkSystem pausedWork;
        private bool resumeWorkOnClose;
        private bool changingScene;
        public GameOptionsSystem Options { get; private set; }
        public bool IsOpen => view != null && view.IsOpen;
        public event Action Closed;

        private void Awake() => Options = new GameOptionsSystem();
        private void OnEnable()
        {
            presenter = new GameOptionsPresenter(Options, view); presenter.Connect();
            if (view != null)
            {
                view.CloseRequested += Close; view.TitleRequested += ReturnToTitle;
                view.ShowTitleButton(SceneManager.GetActiveScene().name != titleScene);
            }
            foreach (var button in openButtons) if (button != null) button.onClick.AddListener(Open);
            Options.MusicChanged += SendMusic; Options.SoundChanged += SendSound;
            SendMusic(Options.MusicVolume); SendSound(Options.SoundVolume);
        }
        private void OnDisable()
        {
            foreach (var button in openButtons) if (button != null) button.onClick.RemoveListener(Open);
            if (view != null) { view.CloseRequested -= Close; view.TitleRequested -= ReturnToTitle; view.Hide(); }
            Options.MusicChanged -= SendMusic; Options.SoundChanged -= SendSound;
            presenter?.Dispose(); presenter = null;
            SaveOptions(); ResumeWork();
        }
        private void OnApplicationPause(bool paused) { if (paused) SaveOptions(); }
        private void OnApplicationFocus(bool focused) { if (!focused) SaveOptions(); }
        private void OnApplicationQuit() => SaveOptions();

        public void Open()
        {
            if (view == null || view.IsOpen || changingScene) return;
            presenter?.RefreshDisplay();
            transform.SetAsLastSibling();
            view.Open();
            if (workRoot != null)
            {
                workRoot.SetCareDialogueBlocked(true);
                var work = workRoot.System;
                if (work != null && work.Run.Phase != WorkPhase.Ready)
                {
                    pausedWork = work;
                    resumeWorkOnClose = !work.IsPaused;
                    work.SetPaused(true);
                }
            }
        }
        public void Close()
        {
            if (changingScene) return;
            SaveOptions(); view?.Hide(); ResumeWork();
            Closed?.Invoke();
        }
        public void ReturnToTitle()
        {
            if (changingScene) return;
            if (!Application.CanStreamedLevelBeLoaded(titleScene))
            {
                view?.ShowError("타이틀 씬을 빌드 목록에 추가해 주세요."); return;
            }
            try
            {
                Options.Save();
                // 지금 진행 상태를 새로 캡처하지 않고 기존 시작 체크포인트만 다시 씁니다.
                if (workRoot != null && !workRoot.Saves.SaveAuto())
                {
                    view?.ShowError("자동 저장할 시작 상태가 없습니다."); return;
                }
                changingScene = true;
                WorkStartRequest.NewRun();
                SceneManager.LoadScene(titleScene);
            }
            catch (Exception error) { changingScene = false; view?.ShowError("저장 또는 타이틀 이동에 실패했습니다."); Debug.LogException(error, this); }
        }
        private void ResumeWork()
        {
            if (!changingScene && resumeWorkOnClose && workRoot != null && pausedWork == workRoot.System) pausedWork?.SetPaused(false);
            if (workRoot != null) workRoot.SetCareDialogueBlocked(workRoot.System?.IsPaused ?? false);
            pausedWork = null;
            resumeWorkOnClose = false;
        }
        private void SaveOptions()
        {
            try { Options?.Save(); }
            catch (Exception error) { view?.ShowError("옵션을 저장하지 못했습니다."); Debug.LogException(error, this); }
        }
        private void SendMusic(float volume) => onMusicVolume.Invoke(volume);
        private void SendSound(float volume) => onSoundVolume.Invoke(volume);
    }
}
