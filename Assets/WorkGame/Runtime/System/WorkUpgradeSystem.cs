using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public sealed class WorkUpgradeSystem
    {
        private readonly WorkSystem work;
        private readonly Dictionary<string, WorkUpgradeNode> nodes;
        public WorkUpgradeStats Stats { get; private set; }
        public event Action Changed;

        public WorkUpgradeSystem(WorkSystem work, IEnumerable<WorkUpgradeNode> nodes)
        {
            this.work = work ?? throw new ArgumentNullException(nameof(work));
            this.nodes = nodes.ToDictionary(x => x.Id);
            RebuildStats();
        }

        public WorkUpgradeState GetState(string id)
        {
            if (!nodes.TryGetValue(id ?? "", out var node)) return WorkUpgradeState.Locked;
            if (work.Run.BoughtUpgrades.Contains(id)) return WorkUpgradeState.Bought;
            return node.Required.All(work.Run.BoughtUpgrades.Contains) ? WorkUpgradeState.Available : WorkUpgradeState.Locked;
        }

        public bool CanBuy(string id) => !work.IsPaused && GetState(id) == WorkUpgradeState.Available && work.Run.SkillPoints >= nodes[id].Cost;

        public WorkUpgradeSwitch GetSwitchKind(string id)
        {
            if (!nodes.TryGetValue(id ?? "", out var node)) return WorkUpgradeSwitch.None;
            if (node.Effects.Any(x => x.Kind == WorkUpgradeEffectKind.FaxUnlock)) return WorkUpgradeSwitch.Fax;
            return node.Effects.Any(x => x.Kind == WorkUpgradeEffectKind.SortUnlock) ? WorkUpgradeSwitch.Sort : WorkUpgradeSwitch.None;
        }

        public bool Buy(string id)
        {
            if (!CanBuy(id) || !work.BuyUpgrade(id, nodes[id].Cost)) return false;
            RebuildStats();
            Changed?.Invoke();
            return true;
        }

        // Rebuild from definitions, never from previously modified values.
        public void RebuildStats()
        {
            var stats = new WorkUpgradeStats();
            foreach (var id in work.Run.BoughtUpgrades)
            {
                if (!nodes.TryGetValue(id, out var node)) continue;
                foreach (var effect in node.Effects)
                {
                    double value = effect.Value;
                    switch (effect.Kind)
                    {
                        case WorkUpgradeEffectKind.FaxUnlock: stats.FaxUnlocked = true; break;
                        case WorkUpgradeEffectKind.FaxSpeedPercent: stats.FaxSpeedBonus += value / 100; break;
                        case WorkUpgradeEffectKind.FaxSlots: stats.FaxSlots += (int)value; break;
                        case WorkUpgradeEffectKind.SortUnlock: stats.SortUnlocked = true; break;
                        case WorkUpgradeEffectKind.HighlightLevel: stats.HighlightLevel = Math.Max(stats.HighlightLevel, (int)value); break;
                        case WorkUpgradeEffectKind.InstantChancePercent: stats.InstantChance += value / 100; break;
                        case WorkUpgradeEffectKind.PointFlat: stats.PointFlat += (int)value; break;
                        case WorkUpgradeEffectKind.PointPercent: stats.PointBonus += value / 100; break;
                        case WorkUpgradeEffectKind.PointChancePercent: stats.PointChance += value / 100; break;
                        case WorkUpgradeEffectKind.PointMultiplier: stats.PointMultiplier = Math.Max(stats.PointMultiplier, value); break;
                        case WorkUpgradeEffectKind.TuitionDiscountPercent: stats.TuitionDiscount += value / 100; break;
                        case WorkUpgradeEffectKind.ExtraSeconds: stats.ExtraSeconds += (int)value; break;
                        case WorkUpgradeEffectKind.NewbieProtection: stats.WrongPenaltyReduction += value / 100; break;
                        case WorkUpgradeEffectKind.EarlyLeaveBonusPercent: stats.EarlyLeaveBonus = Math.Max(stats.EarlyLeaveBonus, value / 100); break;
                    }
                }
            }
            stats.InstantChance = Math.Min(1, stats.InstantChance);
            stats.PointChance = Math.Min(1, stats.PointChance);
            stats.TuitionDiscount = Math.Min(1, stats.TuitionDiscount);
            stats.EarlyLeaveBonus = Math.Min(1, stats.EarlyLeaveBonus);
            stats.WrongPenaltyReduction = Math.Min(1, stats.WrongPenaltyReduction);
            Stats = stats;
            work.ApplyUpgradeStats(stats);
        }
    }
}
