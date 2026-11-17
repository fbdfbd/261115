using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public sealed class CareFlowSystem : IDisposable
    {
        private readonly WorkSystem work;
        private readonly CareSystem care;
        private readonly CareLeaveSettings leave;
        private readonly Dictionary<int, string> monthTalks;
        private int shownMonth = -1;
        private WorkPhase shownPhase;
        private string offeredPromise;
        private bool disposed;
        public CareDialogueSystem Dialogue { get; }
        public CarePromiseSystem Promises { get; }
        public CareFlowPhase Phase { get; private set; } = CareFlowPhase.Working;
        public CarePanel Panel { get; private set; }
        public CareLeaveKind LeaveKind { get; private set; }
        public bool CanGoNext => !disposed && Phase == CareFlowPhase.Free && care.CanPlan && care.IsPlanFull;
        public bool CanTalk => !disposed && Phase == CareFlowPhase.Free && monthTalks.ContainsKey(care.Month)
            && !Dialogue.WasCompleted("talk/" + care.Month);
        public bool CanEditSchedule => Phase == CareFlowPhase.Panel && Panel == CarePanel.Schedule;
        public bool CanBuyGifts => Phase == CareFlowPhase.Panel && (Panel == CarePanel.Shop || Panel == CarePanel.Furniture);
        public bool CanEditGifts => Phase == CareFlowPhase.Panel && Panel == CarePanel.Furniture;
        public event Action Changed;
        public event Action Finished;
        public event Action<string> Blocked;

        public CareFlowSystem(WorkSystem work, CareSystem care, CareDialogueFile data)
        {
            CareDialogueCheck.Check(data);
            this.work = work; this.care = care; leave = data.Leave;
            monthTalks = data.MonthTalks.ToDictionary(x => x.Month, x => x.Dialogue);
            Dialogue = new CareDialogueSystem(data.Dialogues.ToDictionary(x => x.Id), care.AddFriendship);
            Promises = new CarePromiseSystem(data.Promises);
            Dialogue.Changed += Refresh;
            Dialogue.Finished += FinishDialogue;
            care.Changed += Refresh;
            work.Changed += OnWorkChanged;
        }

        // 급여 정산서를 닫고 육성 메인이 보인 시점에 한 번 호출합니다.
        public void EnterCare()
        {
            if (disposed || Phase != CareFlowPhase.Report || work.Run.LastResult == null) return;
            var result = work.Run.LastResult;
            LeaveKind = leave.GetKind(result.LeaveDifference);
            var promise = Promises.Resolve(result.Month, LeaveKind);
            // 약속 결과가 있으면 일반 반응을 대체해 친밀도 효과를 중복 적용하지 않습니다.
            string id = promise != null ? promise.ResultDialogue : leave.GetDialogue(LeaveKind);
            if (!Play(id, "entry/" + result.Month)) SetPhase(CareFlowPhase.Free);
        }

        public bool OpenPanel(CarePanel panel)
        {
            bool fromStats = Phase == CareFlowPhase.Panel && Panel == CarePanel.Stats && panel == CarePanel.Furniture;
            if (disposed || (Phase != CareFlowPhase.Free && !fromStats)
                || panel == CarePanel.None || !Enum.IsDefined(typeof(CarePanel), panel)) return false;
            Panel = panel;
            SetPhase(CareFlowPhase.Panel);
            return true;
        }

        public void ClosePanel()
        {
            if (disposed || Phase != CareFlowPhase.Panel) return;
            Panel = CarePanel.None;
            SetPhase(CareFlowPhase.Free);
        }

        public bool Talk()
        {
            if (!CanTalk) return false;
            return Play(monthTalks[care.Month], "talk/" + care.Month);
        }

        public bool GoNext()
        {
            if (!CanGoNext) return false;
            Phase = CareFlowPhase.Result;
            if (care.FinishMonth()) return true;
            SetPhase(CareFlowPhase.Free);
            Blocked?.Invoke("수업료가 부족합니다. 편성이나 보유 현금을 확인하세요.");
            return false;
        }

        public void ConfirmResult()
        {
            if (disposed || Phase != CareFlowPhase.Result || !care.HasResult) return;
            var promise = Promises.Offer(care.Month);
            if (promise != null)
            {
                offeredPromise = promise.Promise.Id;
                if (Play(promise.Promise.OfferDialogue, "offer/" + care.Month)) return;
                offeredPromise = null;
            }
            AdvanceMonth();
        }

        private bool Play(string id, string key)
        {
            if (string.IsNullOrEmpty(id) || Dialogue.WasCompleted(key)) return false;
            Phase = CareFlowPhase.Dialogue;
            return Dialogue.Play(id, key, care.Month);
        }

        private void FinishDialogue(CareDialogueResult result)
        {
            if (offeredPromise != null)
            {
                Promises.Reply(offeredPromise, result.ChoiceKey == "accept");
                offeredPromise = null;
                AdvanceMonth();
            }
            else SetPhase(CareFlowPhase.Free);
        }

        private void AdvanceMonth()
        {
            if (work.Run.Phase == WorkPhase.AfterWork)
            {
                SetPhase(CareFlowPhase.Working);
                work.StartNextMonth();
            }
            else if (work.Run.Phase == WorkPhase.Finished)
            {
                SetPhase(CareFlowPhase.Ended);
                care.ResolveEnding();
                Finished?.Invoke();
            }
        }

        private void OnWorkChanged()
        {
            if (shownMonth == care.Month && shownPhase == work.Run.Phase) return;
            shownMonth = care.Month; shownPhase = work.Run.Phase;
            Panel = CarePanel.None;
            if (shownPhase == WorkPhase.Working) SetPhase(CareFlowPhase.Working);
            else if (shownPhase == WorkPhase.AfterWork || shownPhase == WorkPhase.Finished) SetPhase(CareFlowPhase.Report);
        }
        private void SetPhase(CareFlowPhase phase) { Phase = phase; Refresh(); }
        private void Refresh() { if (!disposed) Changed?.Invoke(); }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            work.Changed -= OnWorkChanged;
            care.Changed -= Refresh;
            Dialogue.Changed -= Refresh;
            Dialogue.Finished -= FinishDialogue;
            Dialogue.Clear();
            Changed = null; Finished = null; Blocked = null;
        }
    }
}
