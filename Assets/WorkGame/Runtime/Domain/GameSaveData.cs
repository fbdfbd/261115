using System;

namespace WorkGame
{
    public enum SavePoint { WorkStart, CareStart, EndingChoice }
    // 기존 저장 키를 유지하도록 새 슬롯은 뒤에 추가합니다.
    public enum SaveSlot { Auto, Manual1, Manual2, Manual3, Manual4 }

    // 저장 파일에는 숫자와 ID만 둡니다. SO, 이미지, UI 상태는 저장하지 않습니다.
    [Serializable]
    public sealed class GameSaveData
    {
        public int Version;
        public string SavedAtUtc;
        public int Seed;
        public SavePoint Point;
        public WorkSaveData Work;
        public CareSaveData Care;
        public CareFlowSaveData Flow;
    }

    [Serializable]
    public sealed class WorkSaveData
    {
        public string PlanKey;
        public int Month;
        public WorkPhase Phase;
        public double Seconds;
        public int Attendance;
        public long Bonus;
        public long Money;
        public int SkillPoints;
        public string[] Upgrades;
        public bool SortEnabled;
        public bool FaxVisible;
        public bool DoNotDisturbEnabled;
        public string ReservePlanKey;
        public int[] MonthlyExtraCounts;
        public int RandomSeed;
        public int RandomCalls;
        public double WorkSpeed;
        public int PointsPerPaper;
        public int Right;
        public int Wrong;
        public bool ProtectionUsed;
        public long PointsEarned;
        public long PointPenalty;
        public SavedPaper[] Papers;
        public int[] MonthPapers;
        public SavedWorkMonth[] Months;
    }

    [Serializable]
    public sealed class SavedPaper
    {
        public int Id;
        public int Choice;
    }

    [Serializable]
    public sealed class SavedWorkMonth
    {
        public int Month, Right, Wrong, Pending, Attendance;
        public double Overtime, LeaveDifference, ExtraPointPercent;
        public long PointsEarned, PointPenalty, MoneyBalance;
        public int EarlyBonus, Salary;
        public bool ProtectionUsed, HasReview;
        public int ReviewRight, ReviewTotal, BonusPercent;
        public WorkGrade Grade;
        public long Bonus;
    }

    [Serializable]
    public sealed class CareSaveData
    {
        public int[] Stats;
        public int Friendship;
        public int FinishedMonth;
        public string[] Gifts;
        public string[] ActiveGifts;
        public string[] EndingChoices;
    }

    [Serializable]
    public sealed class CareFlowSaveData
    {
        public int HistoryVersion;
        public string[] StoryUnlocks = Array.Empty<string>();
        public string[] Completed;
        public SavedDialogueLog[] History;
        public SavedPromise[] Promises;
    }

    [Serializable]
    public sealed class SavedDialogueLog
    {
        public bool Legacy;
        public int Month;
        public string Dialogue;
        public int Index;
        public bool IsChoice;
    }

    [Serializable]
    public sealed class SavedPromise
    {
        public string Id;
        public CarePromiseState State;
        public string ResultDialogue;
    }

    [Serializable]
    public sealed class GameProfileData
    {
        public int Version;
        public string[] Endings = Array.Empty<string>();
        public string[] Dialogues = Array.Empty<string>();
        public string[] Upgrades = Array.Empty<string>();
        public bool SharedUpgradesReady;
    }
}
