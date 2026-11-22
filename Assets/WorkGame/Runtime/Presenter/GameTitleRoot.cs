using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace WorkGame
{
    public sealed class GameTitleRoot : MonoBehaviour
    {
        [SerializeField] private GameTitleView view;
        [SerializeField] private GameSaveSlotsRoot saveSlotsRoot;
        [SerializeField] private GameOptionsRoot optionsRoot;
        [SerializeField] private GameCollectionRoot collectionRoot;
        [SerializeField] private string gameScene = "02_Game";
        [Header("모음집 진입 시 추가 연출 — 선택 사항")]
        [SerializeField] private UnityEvent onCollectionRequested = new UnityEvent();
        private GameTitlePresenter presenter;
        private bool leaving;
        private void OnEnable()
        {
            presenter = new GameTitlePresenter(new WorkSaveSystem(new PlayerPrefsSaveStore()), view,
                NewGame, ContinueGame, OpenCollection, OpenSettings, Quit);
            presenter.Connect();
            if (view != null)
            {
                view.SetMenuInteractable(true); view.ShowError(null);
                view.SetQuitAvailable(Application.platform != RuntimePlatform.WebGLPlayer);
            }
            if (saveSlotsRoot != null) saveSlotsRoot.Closed += ReturnToMenu;
            if (optionsRoot != null) optionsRoot.Closed += ReturnToMenu;
            if (collectionRoot != null) collectionRoot.Closed += ReturnToMenu;
        }
        private void OnDisable()
        {
            if (saveSlotsRoot != null) saveSlotsRoot.Closed -= ReturnToMenu;
            if (optionsRoot != null) optionsRoot.Closed -= ReturnToMenu;
            if (collectionRoot != null) collectionRoot.Closed -= ReturnToMenu;
            presenter?.Dispose(); presenter = null;
        }
        public void NewGame()
        {
            if (leaving) return;
            if (!Application.CanStreamedLevelBeLoaded(gameScene)) { view?.ShowError("게임 씬을 빌드 목록에 추가해 주세요."); return; }
            try
            {
                optionsRoot?.Options?.Save();
                leaving = true; view?.SetMenuInteractable(false);
                WorkStartRequest.NewRun();
                SceneManager.LoadScene(gameScene);
            }
            catch (Exception error) { leaving = false; ReturnToMenu(); view?.ShowError("게임을 시작하지 못했습니다."); Debug.LogException(error, this); }
        }
        public void ContinueGame()
        {
            if (leaving) return;
            if (saveSlotsRoot == null) { view?.ShowError("저장 슬롯 패널을 연결해 주세요."); return; }
            view?.ShowError(null); saveSlotsRoot.OpenLoad(); view?.SetMenuInteractable(false);
        }
        public void OpenSettings()
        {
            if (leaving) return;
            if (optionsRoot == null) { view?.ShowError("옵션 패널을 연결해 주세요."); return; }
            view?.ShowError(null); optionsRoot.Open(); view?.SetMenuInteractable(false);
        }
        public void OpenCollection()
        {
            if (leaving || presenter == null || !presenter.CanOpenCollection) return;
            if (collectionRoot == null) { view?.ShowError("모음집 패널을 연결해 주세요."); return; }
            view?.ShowError(null); collectionRoot.Open();
            view?.SetMenuInteractable(!collectionRoot.IsOpen); onCollectionRequested.Invoke();
        }
        public void Quit()
        {
            if (leaving || Application.platform == RuntimePlatform.WebGLPlayer) return;
            try
            {
                optionsRoot?.Options?.Save();
                leaving = true;
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            catch (Exception error) { leaving = false; view?.ShowError("종료하지 못했습니다."); Debug.LogException(error, this); }
        }
        private void ReturnToMenu()
        {
            view?.SetMenuInteractable(true); presenter?.Refresh();
        }
    }
}
