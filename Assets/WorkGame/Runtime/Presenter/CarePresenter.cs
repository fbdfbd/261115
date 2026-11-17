using System;

namespace WorkGame
{
    // View가 정해지면 이 입력·이벤트에 연결합니다. MonoBehaviour가 아닙니다.
    public sealed class CarePresenter : IDisposable
    {
        public CareSystem System { get; }
        private readonly CareFlowSystem flow;
        public event Action Changed;
        public event Action<CareMonthResult> MonthFinished;
        public event Action<CareEndingResult> EndingReady;

        public CarePresenter(CareSystem system, CareFlowSystem flow = null)
        {
            System = system ?? throw new ArgumentNullException(nameof(system));
            this.flow = flow;
            System.Changed += Refresh;
            System.MonthFinished += ShowMonth;
            System.EndingReady += ShowEnding;
        }

        public bool SetLesson(int slot, string id) => (flow == null || flow.CanEditSchedule) && System.SetLesson(slot, id);
        public bool ClearLesson(int slot) => (flow == null || flow.CanEditSchedule) && System.ClearLesson(slot);
        public bool SetHome(int slot) => SetLesson(slot, CareSystem.HomeId);
        public bool SetRest(int slot) => SetLesson(slot, CareSystem.RestId);
        public CarePreview Preview() => System.Preview();
        public bool BuyGift(string id) => (flow == null || flow.CanBuyGifts) && System.BuyGift(id);
        public bool SetActiveGift(string id) => (flow == null || flow.CanEditGifts) && System.SetActiveGift(id);
        public bool ClearActiveGift(CareGiftKind kind) => (flow == null || flow.CanEditGifts) && System.ClearActiveGift(kind);
        public bool FinishMonth() => flow != null ? flow.GoNext() : System.FinishMonth();
        public void AddFriendship(int amount) => System.AddFriendship(amount);
        private void Refresh() => Changed?.Invoke();
        private void ShowMonth(CareMonthResult result) => MonthFinished?.Invoke(result);
        private void ShowEnding(CareEndingResult result) => EndingReady?.Invoke(result);

        public void Dispose()
        {
            System.Changed -= Refresh;
            System.MonthFinished -= ShowMonth;
            System.EndingReady -= ShowEnding;
            Changed = null; MonthFinished = null; EndingReady = null;
        }
    }
}
