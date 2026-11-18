using System;

namespace WorkGame
{
    public sealed class GameSaveSlotsPresenter : IDisposable
    {
        private readonly WorkSaveSystem saves;
        private readonly GameSaveSlotsView view;
        private bool connected;
        private SavePanelMode mode;
        public event Action<SaveSlot> LoadRequested;
        public event Action CloseRequested;
        public GameSaveSlotsPresenter(WorkSaveSystem saves, GameSaveSlotsView view) { this.saves = saves; this.view = view; }
        public void Connect()
        {
            if (connected || view == null) return;
            connected = true; view.Selected += Select; view.CloseRequested += Close;
        }
        public void Open(SavePanelMode mode = SavePanelMode.Load)
        {
            if (view == null) return;
            this.mode = mode;
            view.Open(mode);
            Refresh();
            if (mode == SavePanelMode.Save && !saves.HasCheckpoint) view.ShowError("게임 진행 중에 저장할 수 있습니다.");
        }
        private void Refresh()
        {
            var slots = (SaveSlot[])Enum.GetValues(typeof(SaveSlot));
            foreach (var slot in slots)
            {
                var info = saves.GetSlotInfo(slot);
                bool selectable = mode == SavePanelMode.Save ? saves.HasCheckpoint && slot != SaveSlot.Auto : info.State == SaveSlotState.Ready;
                view.ShowSlot((int)slot, info, selectable);
            }
        }
        private void Select(SaveSlot slot)
        {
            if (!connected || !view.IsOpen) return;
            if (mode == SavePanelMode.Save)
            {
                if (slot == SaveSlot.Auto) return;
                try
                {
                    if (!saves.SaveManual(slot)) { view.ShowError("저장할 시작 상태가 없습니다."); return; }
                    Refresh();
                    view.ShowMessage("슬롯 " + (int)slot + "에 저장했습니다.");
                }
                catch (Exception saveError) { view.ShowError("저장하지 못했습니다: " + saveError.Message); }
                return;
            }
            // 패널을 열어 둔 사이 파일이 바뀌었을 수 있으므로 선택 시 다시 확인합니다.
            if (!saves.TryLoad(slot, out _, out var error)) { view.ShowError(error); return; }
            LoadRequested?.Invoke(slot);
        }
        private void Close() => CloseRequested?.Invoke();
        public void Dispose()
        {
            if (!connected) return;
            connected = false; view.Selected -= Select; view.CloseRequested -= Close;
            LoadRequested = null; CloseRequested = null;
        }
    }
}
