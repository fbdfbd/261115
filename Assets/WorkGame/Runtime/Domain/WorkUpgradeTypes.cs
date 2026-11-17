using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public enum WorkUpgradeState { Locked, Available, Bought }
    public enum WorkUpgradeSwitch { None, Fax, Sort }

    public enum WorkUpgradeEffectKind
    {
        FaxUnlock, FaxSpeedPercent, FaxSlots, SortUnlock, HighlightLevel,
        InstantChancePercent, PointFlat, PointPercent, PointChancePercent,
        PointMultiplier, TuitionDiscountPercent, ExtraSeconds,
        NewbieProtection, EarlyLeaveBonusPercent
    }

    [Serializable]
    public sealed class WorkUpgradeEffect
    {
        public WorkUpgradeEffectKind Kind;
        public double Value;
    }

    public sealed class WorkUpgradeNode
    {
        public string Id { get; }
        public int Cost { get; }
        public IReadOnlyList<string> Required { get; }
        public IReadOnlyList<WorkUpgradeEffect> Effects { get; }

        public WorkUpgradeNode(string id, int cost, IEnumerable<string> required, IEnumerable<WorkUpgradeEffect> effects)
        {
            Id = id;
            Cost = cost;
            Required = Array.AsReadOnly(required.ToArray());
            Effects = Array.AsReadOnly(effects.Select(x => new WorkUpgradeEffect { Kind = x.Kind, Value = x.Value }).ToArray());
        }
    }

    public sealed class WorkUpgradeStats
    {
        public bool FaxUnlocked { get; internal set; }
        public double FaxSpeedBonus { get; internal set; }
        public int FaxSlots { get; internal set; } = 1;
        public bool SortUnlocked { get; internal set; }
        public int HighlightLevel { get; internal set; }
        public double InstantChance { get; internal set; }
        public int PointFlat { get; internal set; }
        public double PointBonus { get; internal set; }
        public double PointChance { get; internal set; }
        public double PointMultiplier { get; internal set; } = 2;
        public double TuitionDiscount { get; internal set; }
        public int ExtraSeconds { get; internal set; }
        public double WrongPenaltyReduction { get; internal set; }
        public double EarlyLeaveBonus { get; internal set; }

        public int GetTuitionCost(int originalCost)
        {
            if (originalCost < 0) throw new ArgumentOutOfRangeException(nameof(originalCost));
            return (int)Math.Floor(originalCost * (1 - (decimal)TuitionDiscount));
        }
    }

    public sealed class WorkFaxJob
    {
        public int PaperId { get; }
        public double Progress { get; internal set; }
        internal WorkFaxJob(int paperId) { PaperId = paperId; }
    }
}
