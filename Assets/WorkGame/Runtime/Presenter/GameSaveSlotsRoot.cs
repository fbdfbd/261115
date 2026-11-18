using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class GameSaveSlotsRoot : MonoBehaviour
    {
        [SerializeField] private GameSaveSlotsView view;
        [SerializeField] private List<Button> openButtons = new List<Button>();
        [SerializeField] private string gameScene = "02_Game";
        [Header("게임 안에서 저장·불러올 때 연결 — 타이틀에서는 비움")]
        [SerializeField] private WorkRoot workRoot;
        private GameSaveSlotsPresenter presenter;
        private WorkSystem pausedWork;
        private bool resumeOnClose, loading;
        public bool IsOpen => view != null && view.IsOpen;
        public event Action Opened;
        public event Action Closed;
        private void OnEnable()
        {
            var saves = workRoot != null ? workRoot.Saves : new WorkSaveSystem(new PlayerPrefsSaveStore());
            presenter = new GameSaveSlotsPresenter(saves, view);
            presenter.LoadRequested += Load; presenter.CloseRequested += Close; presenter.Connect();
            foreach (var button in openButtons) if (button != null) button.onClick.AddListener(Open);
        }
        private void OnDisable()
        {
            foreach (var button in openButtons) if (button != null) button.onClick.RemoveListener(Open);
            presenter?.Dispose(); presenter = null;
            view?.Hide(); ResumeWork();
        }
        // 타이틀의 이어하기 버튼에서 호출할 진입점입니다.
        public void Open() => OpenLoad();
        public void OpenLoad() => OpenPanel(SavePanelMode.Load);
        public void OpenSave() => OpenPanel(SavePanelMode.Save);
        private void OpenPanel(SavePanelMode mode)
        {
            if (view == null || view.IsOpen || loading) return;
            transform.SetAsLastSibling();
            if (workRoot != null)
            {
                pausedWork = workRoot.System;
                resumeOnClose = pausedWork != null && !pausedWork.IsPaused;
                pausedWork?.SetPaused(true);
                workRoot.SetCareDialogueBlocked(true);
            }
            presenter?.Open(mode);
            Opened?.Invoke();
        }
        public void Close()
        {
            if (loading) return;
            view?.Hide(); ResumeWork(); Closed?.Invoke();
        }
        private void Load(SaveSlot slot)
        {
            if (loading) return;
            try
            {
                if (workRoot != null)
                {
                    bool loaded = slot == SaveSlot.Auto ? workRoot.LoadAuto() : workRoot.LoadManual((int)slot);
                    if (!loaded) { view.ShowError(workRoot.SaveError); return; }
                    Close(); return;
                }
                if (!Application.CanStreamedLevelBeLoaded(gameScene)) { view.ShowError("게임 씬을 빌드 목록에 추가해 주세요."); return; }
                loading = true;
                WorkStartRequest.Load(slot);
                SceneManager.LoadScene(gameScene);
            }
            catch (Exception error)
            {
                loading = false; WorkStartRequest.NewRun();
                view?.ShowError("저장을 불러오지 못했습니다."); Debug.LogException(error, this);
            }
        }
        private void ResumeWork()
        {
            if (!loading && resumeOnClose && workRoot != null && pausedWork == workRoot.System) pausedWork?.SetPaused(false);
            if (workRoot != null) workRoot.SetCareDialogueBlocked(workRoot.System?.IsPaused ?? false);
            pausedWork = null; resumeOnClose = false;
        }
    }
}
