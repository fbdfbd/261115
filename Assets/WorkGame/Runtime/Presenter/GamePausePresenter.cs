using System;

namespace WorkGame
{
    // 업무와 육성이 같은 정지 상태와 버튼을 사용합니다.
    public sealed class GamePausePresenter : IDisposable
    {
        private readonly WorkSystem work;
        private readonly WorkMenuView menu;
        private readonly GamePauseView view;
        private readonly GameOptionsRoot options;
        private readonly GameSaveSlotsRoot saveSlots;
        private readonly Action<bool> blockDialogue;
        private readonly Action returnToTitle, save, load;
        private bool connected;
        private bool pausedBeforeHelp;
        private bool hasState, lastPaused, lastHelp, lastOptions;

        public GamePausePresenter(WorkSystem work, WorkMenuView menu, GamePauseView view,
            GameOptionsRoot options, Action<bool> blockDialogue, Action returnToTitle, Action save, Action load,
            GameSaveSlotsRoot saveSlots = null)
        {
            this.work = work; this.menu = menu; this.view = view; this.options = options;
            this.saveSlots = saveSlots;
            this.blockDialogue = blockDialogue; this.returnToTitle = returnToTitle; this.save = save; this.load = load;
        }
        public void Connect()
        {
            if (connected || view == null) return;
            connected = true;
            if (menu != null)
            {
                menu.SetSharedMenu(true);
                menu.PauseClicked += Open; menu.ResumeClicked += Resume; menu.HelpClicked += OpenHelp;
            }
            view.ResumeRequested += Resume; view.SettingsRequested += OpenSettings;
            view.TitleRequested += returnToTitle; view.SaveRequested += save; view.LoadRequested += load;
            view.HelpCloseRequested += CloseHelp;
            work.Changed += Refresh;
            if (options != null) options.Closed += Refresh;
            if (saveSlots != null) { saveSlots.Opened += Refresh; saveSlots.Closed += Refresh; }
            view.SetSettingsAvailable(options != null); Refresh();
        }
        public void Open()
        {
            if (work.Run.Phase == WorkPhase.Ready) return;
            view.transform.SetAsLastSibling();
            view.ShowError(null); work.SetPaused(true); Refresh();
        }
        public void Resume()
        {
            if ((options != null && options.IsOpen) || (saveSlots != null && saveSlots.IsOpen)) return;
            view.ShowHelp(false); work.SetPaused(false); Refresh();
        }
        public void OpenHelp()
        {
            if (work.Run.Phase == WorkPhase.Ready || view.IsHelpOpen) return;
            view.transform.SetAsLastSibling();
            pausedBeforeHelp = work.IsPaused;
            view.ShowHelp(true); work.SetPaused(true); Refresh();
        }
        private void CloseHelp()
        {
            view.ShowHelp(false);
            if (!pausedBeforeHelp) work.SetPaused(false);
            Refresh();
        }
        private void OpenSettings()
        {
            if (!work.IsPaused) return;
            options?.Open();
            Refresh();
        }
        private void Refresh()
        {
            bool optionsOpen = (options != null && options.IsOpen) || (saveSlots != null && saveSlots.IsOpen);
            if (hasState && lastPaused == work.IsPaused && lastHelp == view.IsHelpOpen && lastOptions == optionsOpen) return;
            hasState = true; lastPaused = work.IsPaused; lastHelp = view.IsHelpOpen; lastOptions = optionsOpen;
            blockDialogue?.Invoke(work.IsPaused);
            view.ShowPause(work.IsPaused && !view.IsHelpOpen && !optionsOpen);
        }
        public void Dispose()
        {
            if (!connected) return;
            connected = false;
            if (menu != null)
            {
                menu.PauseClicked -= Open; menu.ResumeClicked -= Resume; menu.HelpClicked -= OpenHelp;
                menu.SetSharedMenu(false);
            }
            view.ResumeRequested -= Resume; view.SettingsRequested -= OpenSettings;
            view.TitleRequested -= returnToTitle; view.SaveRequested -= save; view.LoadRequested -= load;
            view.HelpCloseRequested -= CloseHelp;
            work.Changed -= Refresh;
            if (options != null) options.Closed -= Refresh;
            if (saveSlots != null) { saveSlots.Opened -= Refresh; saveSlots.Closed -= Refresh; }
            view.Hide();
        }
    }
}
