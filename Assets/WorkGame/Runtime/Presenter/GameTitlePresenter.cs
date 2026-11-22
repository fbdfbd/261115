using System;
using System.Linq;

namespace WorkGame
{
    public sealed class GameTitlePresenter : IDisposable
    {
        private readonly WorkSaveSystem saves;
        private readonly GameTitleView view;
        private readonly Action newGame, continueGame, collection, settings, quit;
        private bool connected;
        public bool CanOpenCollection => saves.HasReadEnding;
        public GameTitlePresenter(WorkSaveSystem saves, GameTitleView view, Action newGame, Action continueGame,
            Action collection, Action settings, Action quit)
        {
            this.saves = saves; this.view = view; this.newGame = newGame; this.continueGame = continueGame;
            this.collection = collection; this.settings = settings; this.quit = quit;
        }
        public void Connect()
        {
            if (connected || view == null) return;
            connected = true;
            view.NewGameRequested += newGame; view.ContinueRequested += continueGame;
            view.CollectionRequested += OpenCollection; view.SettingsRequested += settings; view.QuitRequested += quit;
            Refresh();
        }
        public void Refresh()
        {
            if (view != null) view.SetContinueAvailable(((SaveSlot[])Enum.GetValues(typeof(SaveSlot))).Any(saves.HasSave));
            if (view != null) view.SetCollectionAvailable(CanOpenCollection);
        }
        private void OpenCollection() { if (CanOpenCollection) collection?.Invoke(); }
        public void Dispose()
        {
            if (!connected) return;
            connected = false;
            view.NewGameRequested -= newGame; view.ContinueRequested -= continueGame;
            view.CollectionRequested -= OpenCollection; view.SettingsRequested -= settings; view.QuitRequested -= quit;
        }
    }
}
