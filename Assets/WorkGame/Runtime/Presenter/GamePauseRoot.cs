using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace WorkGame
{
    public sealed class GamePauseRoot : MonoBehaviour
    {
        [SerializeField] private WorkRoot workRoot;
        [SerializeField] private GamePauseView view;
        [SerializeField] private GameOptionsRoot optionsRoot;
        [SerializeField] private GameSaveSlotsRoot saveSlotsRoot;
        [Header("저장·불러오기 진입 시 추가 연출 — 선택 사항")]
        [SerializeField] private UnityEvent onSaveRequested = new UnityEvent();
        [SerializeField] private UnityEvent onLoadRequested = new UnityEvent();
        private GamePausePresenter presenter;
        private bool quitting;
        private void OnEnable()
        {
            if (workRoot == null) return;
            workRoot.RunStarted += Bind;
            Bind(workRoot.System);
        }
        private void OnDisable()
        {
            if (workRoot != null) workRoot.RunStarted -= Bind;
            presenter?.Dispose(); presenter = null;
            if (!quitting && workRoot != null) { workRoot.System?.SetPaused(false); workRoot.SetCareDialogueBlocked(false); }
        }
        private void Bind(WorkSystem work)
        {
            presenter?.Dispose(); presenter = null;
            if (work == null) return;
            presenter = new GamePausePresenter(work, workRoot.MenuView, view, optionsRoot, workRoot.SetCareDialogueBlocked,
                ReturnToTitle, OpenSave, OpenLoad, saveSlotsRoot);
            presenter.Connect();
        }
        public void Open() { transform.SetAsLastSibling(); presenter?.Open(); }
        public void Resume() => presenter?.Resume();
        public void OpenHelp() { transform.SetAsLastSibling(); presenter?.OpenHelp(); }
        public void OpenSave()
        {
            if (saveSlotsRoot == null) { view?.ShowError("저장 슬롯 패널을 연결해 주세요."); return; }
            saveSlotsRoot.OpenSave(); onSaveRequested.Invoke();
        }
        public void OpenLoad()
        {
            if (saveSlotsRoot == null) { view?.ShowError("저장 슬롯 패널을 연결해 주세요."); return; }
            saveSlotsRoot.OpenLoad(); onLoadRequested.Invoke();
        }
        public void Quit() => ReturnToTitle(); // 기존 OnClick 연결도 타이틀 복귀로 동작합니다.
        public void ReturnToTitle()
        {
            if (quitting) return;
            if (!Application.CanStreamedLevelBeLoaded("01_Title")) { view?.ShowError("타이틀 씬을 빌드 목록에 추가해 주세요."); return; }
            try
            {
                if (workRoot == null || !workRoot.Saves.SaveAuto()) { view?.ShowError("자동 저장할 시작 상태가 없습니다."); return; }
                optionsRoot?.Options?.Save();
                quitting = true;
                WorkStartRequest.NewRun();
                SceneManager.LoadScene("01_Title");
            }
            catch (Exception error) { quitting = false; view?.ShowError("저장 또는 타이틀 이동에 실패했습니다."); Debug.LogException(error, this); }
        }
    }
}
