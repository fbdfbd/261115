using System;

namespace WorkGame
{
    public sealed class GameOptionsPresenter : IDisposable
    {
        private readonly GameOptionsSystem options;
        private readonly GameOptionsView view;
        private bool connected;
        public GameOptionsPresenter(GameOptionsSystem options, GameOptionsView view) { this.options = options; this.view = view; }
        public void Connect()
        {
            if (connected || view == null) return;
            connected = true;
            options.Changed += Refresh;
            view.ResolutionChanged += options.SetResolution;
            view.WindowedChanged += options.SetWindowed;
            view.MusicChanged += options.SetMusic;
            view.SoundChanged += options.SetSound;
            view.SetResolutions(options); Refresh();
        }
        private void Refresh() => view.ShowState(options);
        public void Dispose()
        {
            if (!connected) return;
            connected = false;
            options.Changed -= Refresh;
            view.ResolutionChanged -= options.SetResolution;
            view.WindowedChanged -= options.SetWindowed;
            view.MusicChanged -= options.SetMusic;
            view.SoundChanged -= options.SetSound;
        }
    }
}
