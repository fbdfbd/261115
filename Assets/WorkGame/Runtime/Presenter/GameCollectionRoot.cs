using System;
using UnityEngine;

namespace WorkGame
{
    public sealed class GameCollectionRoot : MonoBehaviour
    {
        [SerializeField] private CareConfigSO config;
        [SerializeField] private GameCollectionView view;
        private GameCollectionPresenter presenter;
        public bool IsOpen => view != null && view.IsOpen;
        public event Action Closed;
        public event Action Changed;
        private void OnEnable()
        {
            if (config == null || view == null) { Debug.LogError("모음집의 Care Config / View를 연결하세요.", this); return; }
            presenter = new GameCollectionPresenter(config, new WorkSaveSystem(new PlayerPrefsSaveStore()), view);
            presenter.Connect(); view.CloseRequested += Close;
        }
        private void OnDisable()
        {
            if (view != null) { view.CloseRequested -= Close; view.Hide(); }
            presenter?.Dispose(); presenter = null;
            Changed?.Invoke();
        }
        public void Open()
        {
            bool wasOpen = IsOpen;
            transform.SetAsLastSibling();
            try { presenter?.Open(); }
            catch (Exception error) { Debug.LogException(error, this); }
            if (wasOpen != IsOpen) Changed?.Invoke();
        }
        public void Close()
        {
            bool wasOpen = IsOpen;
            view?.Hide();
            if (wasOpen != IsOpen) Changed?.Invoke();
            Closed?.Invoke();
        }
    }
}
