using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public sealed class CareEndingPresenter : IDisposable
    {
        private readonly CareSystem care;
        private readonly WorkSystem work;
        private readonly CareEndingView view;
        private readonly CareConfigSO config;
        private readonly List<CareEndingPage> pages = new List<CareEndingPage>();
        private CareEndingResult shown;
        private int page;
        private bool connected, paused, finished;
        public event Action<string> ReadCompleted;

        public CareEndingPresenter(CareSystem care, WorkSystem work, CareEndingView view, CareConfigSO config)
        {
            this.care = care; this.work = work; this.view = view; this.config = config;
        }
        public void Connect()
        {
            if (connected || view == null) return;
            connected = true;
            care.EndingReady += Open; work.Changed += RefreshPause;
            view.NextRequested += Next;
            view.Completed += FinishReading;
            if (care.Run.Ending != null) Open(care.Run.Ending);
            else view.Hide();
        }
        private void Open(CareEndingResult result)
        {
            if (result == null) return;
            if (shown != result)
            {
                shown = result; page = 0; finished = false;
                BuildPages(result);
                view.ClearImage();
            }
            view.Open(result.Ending.Name);
            ShowPage();
            if (finished) view.ShowComplete(false);
            paused = work.IsPaused; view.SetPaused(paused);
        }
        private void BuildPages(CareEndingResult result)
        {
            pages.Clear();
            var asset = config.Endings.FirstOrDefault(x => x != null && x.Id == result.Ending.Id);
            if (asset == null && config.FallbackEnding != null && config.FallbackEnding.Id == result.Ending.Id)
                asset = config.FallbackEnding;
            if (asset != null && asset.Pages != null)
                pages.AddRange(asset.Pages.Where(x => x != null)
                    .Select(x => new CareEndingPage { Image = x.Image, Text = x.Text }));
            if (pages.Count == 0) pages.Add(new CareEndingPage { Text = result.Ending.Text });
            if ((asset == null || asset.AppendFriendshipText) && !string.IsNullOrWhiteSpace(result.FriendshipText))
                pages.Add(new CareEndingPage { Text = result.FriendshipText });
        }
        private void ShowPage() => view.ShowPage(pages[page].Image, pages[page].Text, page + 1, pages.Count);
        private void Next()
        {
            if (shown == null || finished || work.IsPaused || view.InputBlocked) return;
            if (view.IsWriting) { view.FinishWriting(); return; }
            if (page + 1 < pages.Count) { page++; ShowPage(); }
            else { finished = true; view.ShowComplete(); }
        }
        private void RefreshPause()
        {
            if (paused == work.IsPaused) return;
            paused = work.IsPaused; view.SetPaused(paused);
        }
        private void FinishReading() { if (shown != null) ReadCompleted?.Invoke(shown.Ending.Id); }
        public void Disconnect()
        {
            if (!connected) return;
            connected = false;
            care.EndingReady -= Open; work.Changed -= RefreshPause;
            view.NextRequested -= Next;
            view.Completed -= FinishReading;
            view.Hide();
        }
        public void Dispose() => Disconnect();
    }
}
