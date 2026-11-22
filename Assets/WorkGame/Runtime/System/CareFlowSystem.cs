using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public sealed partial class CareFlowSystem : IDisposable
    {
        private readonly WorkSystem work;
        private readonly CareSystem care;
        private readonly CareLeaveSettings leave;
        private readonly CareDialogueFile dialogueData;
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
        public bool IsIntro { get; private set; }
        public string IntroDialogueId => dialogueData.IntroDialogue;
        public bool HasReadSuccessfulPromise(string id) => Promises.Records.Any(record =>
            record.Promise.Id == id && record.State == CarePromiseState.Succeeded
            && Dialogue.WasCompleted("entry/" + record.Promise.TargetMonth));
        public bool IsPaused => work.IsPaused;
        public bool CanGoNext => !disposed && !IsPaused && Phase == CareFlowPhase.Free
            && care.CanPlan && care.IsPlanFull;
        public bool CanTalk => !disposed && !IsPaused && Phase == CareFlowPhase.Free && monthTalks.ContainsKey(care.Month)
            && !Dialogue.WasCompleted("talk/" + care.Month);
        public bool CanEditSchedule => Phase == CareFlowPhase.Panel && Panel == CarePanel.Schedule;
        public bool CanBuyGifts => Phase == CareFlowPhase.Panel && (Panel == CarePanel.Shop || Panel == CarePanel.Furniture);
        public bool CanEditGifts => Phase == CareFlowPhase.Panel && Panel == CarePanel.Furniture;
        public event Action Changed;
        public event Action Finished;
        public event Action<string> Blocked;
        public event Action CareStarting;
        public event Action EndingStarting;

        public CareFlowSystem(WorkSystem work, CareSystem care, CareDialogueFile data)
        {
            CareDialogueCheck.Check(data);
            this.work = work; this.care = care; leave = data.Leave; dialogueData = data;
            monthTalks = data.MonthTalks.ToDictionary(x => x.Month, x => x.Dialogue);
            Dialogue = new CareDialogueSystem(data.Dialogues.ToDictionary(x => x.Id), care.AddFriendship);
            Promises = new CarePromiseSystem(data.Promises);
            InitializeStories(data);
            Dialogue.Changed += Refresh;
            Dialogue.Finished += FinishDialogue;
            care.Changed += Refresh;
            work.Changed += OnWorkChanged;
        }

        // 새 게임에서만 호출합니다. Ready 상태를 유지하므로 업무 시간·급여·월 진행은 발생하지 않습니다.
        public bool StartIntro()
        {
            if (disposed || IsIntro || work.Run.Phase != WorkPhase.Ready || IsPaused
                || string.IsNullOrEmpty(dialogueData.IntroDialogue) || Dialogue.IsOpen) return false;
            IsIntro = true;
            if (Play(dialogueData.IntroDialogue, "intro")) return true;
            IsIntro = false;
            SetPhase(CareFlowPhase.Working);
            return false;
        }

        // 급여 정산서를 닫고 육성 메인이 보인 시점에 한 번 호출합니다.
        public void EnterCare()
        {
            if (disposed || Phase != CareFlowPhase.Report || work.Run.LastResult == null) return;
            CareStarting?.Invoke(); // 약속 결과·친밀도를 바꾸기 전에 시작 상태를 저장합니다.
            var result = work.Run.LastResult;
            LeaveKind = leave.GetKind(result.LeaveDifference);
            var promise = Promises.Resolve(result.Month, LeaveKind);
            // 약속 결과가 있으면 일반 반응을 대체해 친밀도 효과를 중복 적용하지 않습니다.
            string id = promise != null ? promise.ResultDialogue : leave.GetDialogue(LeaveKind, result.Month);
            if (!Play(id, "entry/" + result.Month)) SetPhase(CareFlowPhase.Free);
        }

        public bool OpenPanel(CarePanel panel)
        {
            bool fromStats = Phase == CareFlowPhase.Panel && Panel == CarePanel.Stats && panel == CarePanel.Furniture;
            if (disposed || IsPaused || (Phase != CareFlowPhase.Free && !fromStats)
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
            if (disposed || IsPaused || Phase != CareFlowPhase.Result || !care.HasResult) return;
            ContinueMonthEnd();
        }

        public bool ChooseEnding(string id)
        {
            if (disposed || IsPaused || Phase != CareFlowPhase.EndingChoice || !care.ChooseEnding(id)) return false;
            SetPhase(CareFlowPhase.Ended);
            Finished?.Invoke();
            return true;
        }

        // 저장된 선택·약속 기록으로 재구성하므로 엔딩 선택 체크포인트에서도 같은 회고를 보여 줍니다.
        public string[] BuildEndingMemories()
        {
            var memories = new List<(int Month, string Text, int Priority)>();
            var definitions = dialogueData.Dialogues.ToDictionary(x => x.Id);
            var recalled = new HashSet<string>();
            foreach (var log in Dialogue.History)
            {
                if (!definitions.TryGetValue(log.DialogueId, out var definition)) continue;
                string text = log.IsChoice
                    ? definition.Choices.FirstOrDefault(x => x.Text == log.Text)?.EndingText
                    : definition.EndingText;
                string key = log.DialogueId + (log.IsChoice ? "/choice" : "/dialogue");
                if (!string.IsNullOrWhiteSpace(text) && recalled.Add(key))
                    memories.Add((log.Month, text, log.DialogueId == "story_02_nameplate" ? 4
                        : log.DialogueId.StartsWith("story_", StringComparison.Ordinal) ? 2 : 1));
            }
            foreach (var record in Promises.Records)
            {
                var promise = record.Promise;
                string text = record.State == CarePromiseState.Succeeded
                    ? !string.IsNullOrEmpty(promise.EarlySuccessDialogue)
                        && record.ResultDialogue == promise.EarlySuccessDialogue
                        && !string.IsNullOrWhiteSpace(promise.EarlySuccessEndingText)
                        ? promise.EarlySuccessEndingText : promise.SuccessEndingText
                    : record.State == CarePromiseState.Failed ? promise.FailureEndingText
                    : record.State == CarePromiseState.Declined ? promise.DeclinedEndingText : null;
                if (!string.IsNullOrWhiteSpace(text))
                    memories.Add((promise.TargetMonth, text, 3));
            }
            // One recollection per event month. Prefer the actual final choice/result over its setup.
            var pages = memories.GroupBy(memory => memory.Month)
                .Select(month => month.OrderBy(memory => memory.Priority).Last())
                .OrderByDescending(memory => memory.Priority).ThenByDescending(memory => memory.Month).Take(3)
                .OrderBy(memory => memory.Month).Select(memory => memory.Text).ToList();
            if (pages.Count > 0 && !string.IsNullOrWhiteSpace(dialogueData.EndingIntroText))
                pages.Insert(0, dialogueData.EndingIntroText);
            return pages.ToArray();
        }

        private bool Play(string id, string key)
        {
            if (string.IsNullOrEmpty(id) || Dialogue.WasCompleted(key)) return false;
            Phase = CareFlowPhase.Dialogue;
            return Dialogue.Play(id, key, IsIntro ? 1 : care.Month);
        }

        private void FinishDialogue(CareDialogueResult result)
        {
            if (IsIntro && result.RecordKey == "intro")
            {
                IsIntro = false;
                work.StartRun();
            }
            else if (activeStory != null) CompleteStory();
            else if (offeredPromise != null)
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
                care.GetEndingChoices();
                EndingStarting?.Invoke(); // 후보가 하나여도 선택 전 상태가 체크포인트입니다.
                ResumeEndingChoice();
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
            Dialogue.ChoiceSelected -= SelectStoryChoice;
            Dialogue.Clear();
            Changed = null; Finished = null; Blocked = null;
            CareStarting = null; EndingStarting = null;
        }
    }
}
