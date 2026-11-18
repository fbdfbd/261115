using System;

namespace WorkGame
{
    public enum CareLeaveKind { Early, SlightlyEarly, OnTime, Overtime }
    public enum CarePromiseState { Offered, Accepted, Declined, Succeeded, Failed }
    public enum CareFlowPhase { Working, Report, Dialogue, Free, Panel, Result, Ended, EndingChoice }
    public enum CarePanel { None, Skills, Stats, Schedule, Shop, Furniture }

    [Serializable]
    public sealed class CareDialogueLine
    {
        public string Speaker;
        public string Text;
        // View가 이 키를 이미지로 바꿉니다. 빈 값이면 이전 이미지를 유지합니다.
        public string ImageKey;
    }

    public sealed class CareDialogueLog
    {
        public int Month { get; }
        public string DialogueId { get; }
        public string Speaker { get; }
        public string Text { get; }
        public string ImageKey { get; }
        public bool IsChoice { get; }
        internal CareDialogueLog(int month, string dialogueId, string speaker, string text, string imageKey, bool isChoice = false)
        {
            Month = month; DialogueId = dialogueId; Speaker = speaker;
            Text = text; ImageKey = imageKey; IsChoice = isChoice;
        }
    }

    [Serializable]
    public sealed class CareDialogueChoice
    {
        public string Key;
        public string Text;
        public int Friendship;
    }

    [Serializable]
    public sealed class CareDialogueData
    {
        public string Id;
        public CareDialogueLine[] Lines = Array.Empty<CareDialogueLine>();
        public CareDialogueChoice[] Choices = Array.Empty<CareDialogueChoice>();
        public int Friendship;
    }

    [Serializable]
    public sealed class CareMonthTalk
    {
        public int Month;
        public string Dialogue;
    }

    [Serializable]
    public sealed class CareLeaveSettings
    {
        public double EarlySeconds = 30;
        public double OnTimeSeconds = 10;
        public string EarlyDialogue;
        public string SlightlyEarlyDialogue;
        public string OnTimeDialogue;
        public string OvertimeDialogue;
        public CareLeaveKind GetKind(double difference) => difference <= -EarlySeconds ? CareLeaveKind.Early
            : difference < -OnTimeSeconds ? CareLeaveKind.SlightlyEarly
            : difference <= OnTimeSeconds ? CareLeaveKind.OnTime : CareLeaveKind.Overtime;
        public string GetDialogue(CareLeaveKind kind)
        {
            if (kind == CareLeaveKind.Early) return EarlyDialogue;
            if (kind == CareLeaveKind.SlightlyEarly) return SlightlyEarlyDialogue;
            return kind == CareLeaveKind.OnTime ? OnTimeDialogue : OvertimeDialogue;
        }
    }

    [Serializable]
    public sealed class CarePromiseData
    {
        public string Id;
        public int OfferMonth;
        public int TargetMonth;
        // Early: 30초 이상 남김. NoOvertime: 야근이 아니면 성공.
        public string Condition;
        public string OfferDialogue;
        public string SuccessDialogue;
        public string EarlySuccessDialogue;
        public string FailureDialogue;
    }

    [Serializable]
    public sealed class CareDialogueFile
    {
        public CareLeaveSettings Leave = new CareLeaveSettings();
        public CareDialogueData[] Dialogues = Array.Empty<CareDialogueData>();
        public CareMonthTalk[] MonthTalks = Array.Empty<CareMonthTalk>();
        public CarePromiseData[] Promises = Array.Empty<CarePromiseData>();
    }

    public sealed class CareDialogueResult
    {
        public string RecordKey { get; }
        public string DialogueId { get; }
        public string ChoiceKey { get; }
        public int Friendship { get; }
        internal CareDialogueResult(string recordKey, string id, string choice, int friendship)
        {
            RecordKey = recordKey; DialogueId = id; ChoiceKey = choice; Friendship = friendship;
        }
    }

    public sealed class CarePromiseRecord
    {
        public CarePromiseData Promise { get; }
        public CarePromiseState State { get; internal set; } = CarePromiseState.Offered;
        public string ResultDialogue { get; internal set; }
        internal CarePromiseRecord(CarePromiseData promise) { Promise = promise; }
    }
}
