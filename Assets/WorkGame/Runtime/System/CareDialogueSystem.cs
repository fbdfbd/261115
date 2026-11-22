using System;
using System.Collections.Generic;

namespace WorkGame
{
    public sealed partial class CareDialogueSystem
    {
        private readonly Dictionary<string, CareDialogueData> dialogues;
        private readonly Action<int> addFriendship;
        private readonly List<string> completed = new List<string>();
        private readonly List<CareDialogueLog> history = new List<CareDialogueLog>();
        private Func<string, bool> wasRead;
        private bool skipAllowed;
        private int month;
        private string recordKey;
        private long friendshipTotal;
        private string selectedChoice;
        private readonly List<CareDialogueLine> presentationLines = new List<CareDialogueLine>();
        public IReadOnlyList<string> Completed { get; }
        public IReadOnlyList<CareDialogueLog> History { get; }
        public CareDialogueData Current { get; private set; }
        // Remains stable across NextDialogue branches; audio is scoped to the whole event.
        public string RootDialogueId { get; private set; }
        public int LineIndex { get; private set; }
        public CareDialogueLine Line => Current?.Lines[LineIndex];
        public bool IsOpen => Current != null;
        public bool HasChoices => IsOpen && LineIndex == Current.Lines.Length - 1 && Current.Choices.Length > 0;
        public bool CanSkip => IsOpen && skipAllowed && !HasChoices;
        public bool PresentationBlocked { get; private set; }
        public void SetPresentationBlocked(bool blocked) => PresentationBlocked = blocked;
        public event Action<CareDialogueLine, bool> LineEntered;
        public event Action<CareDialogueChoice> ChoiceSelected;
        public event Action Changed;
        public event Action<CareDialogueResult> Finished;
        public event Action<string> ReadCompleted;
        public void ReplayPresentation(Action<CareDialogueLine, bool> present)
        {
            if (!IsOpen || present == null) return;
            foreach (var line in presentationLines) present(line, true);
        }

        internal CareDialogueSystem(Dictionary<string, CareDialogueData> dialogues, Action<int> addFriendship)
        {
            this.dialogues = dialogues; this.addFriendship = addFriendship;
            Completed = completed.AsReadOnly();
            History = history.AsReadOnly();
        }

        // 세이브 미연결 시 스킵 불가. 읽음 판정은 대화 시작 때 고정합니다.
        public void SetReadCheck(Func<string, bool> check) => wasRead = check;

        public bool WasCompleted(string key) => completed.Contains(key);
        internal void Clear() { Current = null; RootDialogueId = null; recordKey = null; PresentationBlocked = false; presentationLines.Clear(); }
        internal bool Play(string id, string key, int currentMonth)
        {
            if (IsOpen || string.IsNullOrEmpty(id) || WasCompleted(key) || !dialogues.TryGetValue(id, out var dialogue)) return false;
            recordKey = key; Current = dialogue; LineIndex = 0; month = currentMonth;
            RootDialogueId = id;
            friendshipTotal = 0; selectedChoice = null; PresentationBlocked = false;
            presentationLines.Clear();
            skipAllowed = wasRead != null && wasRead(id);
            LogLine();
            Changed?.Invoke();
            LineEntered?.Invoke(Line, false);
            return true;
        }

        public bool Next()
        {
            if (!IsOpen || HasChoices || PresentationBlocked) return false;
            if (LineIndex < Current.Lines.Length - 1) { LineIndex++; LogLine(); Changed?.Invoke(); LineEntered?.Invoke(Line, false); }
            else Finish(null);
            return true;
        }

        public bool Choose(int index)
        {
            if (!HasChoices || PresentationBlocked || index < 0 || index >= Current.Choices.Length) return false;
            Finish(Current.Choices[index]);
            return true;
        }

        public bool Skip(bool finishDialogue = true)
        {
            if (!CanSkip || PresentationBlocked) return false;
            while (LineIndex < Current.Lines.Length - 1) { LineIndex++; LogLine(); LineEntered?.Invoke(Line, true); }
            if (Current.Choices.Length > 0) Changed?.Invoke();
            else if (finishDialogue) Finish(null);
            else Changed?.Invoke();
            return true;
        }

        private void LogLine()
        {
            var line = Line;
            presentationLines.Add(line);
            history.Add(new CareDialogueLog(month, Current.Id, string.IsNullOrEmpty(line.Speaker) ? "달이" : line.Speaker,
                line.Text, line.ImageKey));
        }

        private void Finish(CareDialogueChoice choice)
        {
            string finishedId = Current.Id;
            friendshipTotal += Current.Friendship + (long)(choice?.Friendship ?? 0);
            if (choice != null) history.Add(new CareDialogueLog(month, Current.Id, "나르미", choice.Text, null, true));
            if (choice != null)
            {
                selectedChoice = choice.Key;
                ChoiceSelected?.Invoke(choice);
            }
            if (!string.IsNullOrEmpty(choice?.NextDialogue))
            {
                Current = dialogues[choice.NextDialogue]; LineIndex = 0;
                skipAllowed = wasRead != null && wasRead(Current.Id);
                LogLine(); Changed?.Invoke(); LineEntered?.Invoke(Line, false);
                ReadCompleted?.Invoke(finishedId);
                return;
            }
            int amount = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, friendshipTotal));
            var result = new CareDialogueResult(recordKey, finishedId, selectedChoice, amount);
            completed.Add(recordKey);
            Current = null; RootDialogueId = null; recordKey = null;
            addFriendship(amount);
            Changed?.Invoke();
            Finished?.Invoke(result);
            ReadCompleted?.Invoke(finishedId);
        }
    }
}
