using System;
using System.Collections.Generic;

namespace WorkGame
{
    public sealed class CareDialogueSystem
    {
        private readonly Dictionary<string, CareDialogueData> dialogues;
        private readonly Action<int> addFriendship;
        private readonly List<string> completed = new List<string>();
        private readonly List<CareDialogueLog> history = new List<CareDialogueLog>();
        private Func<string, bool> wasRead;
        private bool skipAllowed;
        private int month;
        private string recordKey;
        public IReadOnlyList<string> Completed { get; }
        public IReadOnlyList<CareDialogueLog> History { get; }
        public CareDialogueData Current { get; private set; }
        public int LineIndex { get; private set; }
        public CareDialogueLine Line => Current?.Lines[LineIndex];
        public bool IsOpen => Current != null;
        public bool HasChoices => IsOpen && LineIndex == Current.Lines.Length - 1 && Current.Choices.Length > 0;
        public bool CanSkip => IsOpen && skipAllowed && !HasChoices;
        public event Action Changed;
        public event Action<CareDialogueResult> Finished;
        public event Action<string> ReadCompleted;

        internal CareDialogueSystem(Dictionary<string, CareDialogueData> dialogues, Action<int> addFriendship)
        {
            this.dialogues = dialogues; this.addFriendship = addFriendship;
            Completed = completed.AsReadOnly();
            History = history.AsReadOnly();
        }

        // 세이브 미연결 시 스킵 불가. 읽음 판정은 대화 시작 때 고정합니다.
        public void SetReadCheck(Func<string, bool> check) => wasRead = check;

        public bool WasCompleted(string key) => completed.Contains(key);
        internal void Clear() { Current = null; recordKey = null; }
        internal bool Play(string id, string key, int currentMonth)
        {
            if (IsOpen || string.IsNullOrEmpty(id) || WasCompleted(key) || !dialogues.TryGetValue(id, out var dialogue)) return false;
            recordKey = key; Current = dialogue; LineIndex = 0; month = currentMonth;
            skipAllowed = wasRead != null && wasRead(id);
            LogLine();
            Changed?.Invoke();
            return true;
        }

        public bool Next()
        {
            if (!IsOpen || HasChoices) return false;
            if (LineIndex < Current.Lines.Length - 1) { LineIndex++; LogLine(); Changed?.Invoke(); }
            else Finish(null);
            return true;
        }

        public bool Choose(int index)
        {
            if (!HasChoices || index < 0 || index >= Current.Choices.Length) return false;
            Finish(Current.Choices[index]);
            return true;
        }

        public bool Skip()
        {
            if (!CanSkip) return false;
            while (LineIndex < Current.Lines.Length - 1) { LineIndex++; LogLine(); }
            if (Current.Choices.Length > 0) Changed?.Invoke();
            else Finish(null);
            return true;
        }

        private void LogLine()
        {
            var line = Line;
            history.Add(new CareDialogueLog(month, Current.Id, string.IsNullOrEmpty(line.Speaker) ? "달이" : line.Speaker,
                line.Text, line.ImageKey));
        }

        private void Finish(CareDialogueChoice choice)
        {
            int amount = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue,
                Current.Friendship + (long)(choice?.Friendship ?? 0)));
            var result = new CareDialogueResult(recordKey, Current.Id, choice?.Key, amount);
            if (choice != null) history.Add(new CareDialogueLog(month, Current.Id, "나르미", choice.Text, null, true));
            completed.Add(recordKey);
            Current = null; recordKey = null;
            addFriendship(amount);
            Changed?.Invoke();
            Finished?.Invoke(result);
            ReadCompleted?.Invoke(result.DialogueId);
        }
    }
}
