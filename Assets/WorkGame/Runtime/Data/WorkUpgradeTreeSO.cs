using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorkGame
{
    [CreateAssetMenu(menuName = "Work Game/Upgrade Tree")]
    public sealed class WorkUpgradeTreeSO : ScriptableObject
    {
        public List<WorkUpgradeSO> Nodes = new List<WorkUpgradeSO>();
        [Tooltip("팩스 한 건의 기본 처리시간. 속도 강화는 이 값을 기준으로 계산합니다.")]
        [Min(0.1f)] public float FaxSeconds = 5;

        public WorkUpgradeNode[] CreateNodes()
        {
            Check();
            return Nodes.Select(x => new WorkUpgradeNode(x.Id, x.Cost, x.Required.Select(r => r.Id), x.Effects)).ToArray();
        }

        public void Check()
        {
            if (!WorkSettings.IsPositive(FaxSeconds) || Nodes == null || Nodes.Count == 0)
                throw new ArgumentException("Upgrade tree needs nodes and a positive fax duration.");
            var ids = new HashSet<string>();
            foreach (var node in Nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id) || !ids.Add(node.Id) || node.Cost < 0
                    || string.IsNullOrWhiteSpace(node.Name) || node.Required == null || node.Effects == null)
                    throw new ArgumentException("Upgrade nodes need unique IDs, names, nonnegative costs and lists.");
                if (node.Required.Any(x => x == null || !Nodes.Contains(x)))
                    throw new ArgumentException($"{node.Id}: required nodes must belong to this tree.");
                foreach (var effect in node.Effects)
                {
                    if (effect == null || !Enum.IsDefined(typeof(WorkUpgradeEffectKind), effect.Kind)
                        || double.IsNaN(effect.Value) || double.IsInfinity(effect.Value) || effect.Value < 0)
                        throw new ArgumentException($"{node.Id}: invalid upgrade effect.");
                    bool whole = effect.Kind == WorkUpgradeEffectKind.FaxSlots || effect.Kind == WorkUpgradeEffectKind.HighlightLevel
                        || effect.Kind == WorkUpgradeEffectKind.PointFlat || effect.Kind == WorkUpgradeEffectKind.ExtraSeconds;
                    if (whole && (effect.Value > int.MaxValue || effect.Value != Math.Floor(effect.Value)))
                        throw new ArgumentException($"{node.Id}: this effect needs a whole number.");
                    if (effect.Kind == WorkUpgradeEffectKind.HighlightLevel && effect.Value > 3)
                        throw new ArgumentException($"{node.Id}: highlight level is 0 to 3.");
                    if (effect.Kind == WorkUpgradeEffectKind.PointMultiplier && effect.Value < 1)
                        throw new ArgumentException($"{node.Id}: multiplier must be at least 1.");
                }
            }
            var visiting = new HashSet<WorkUpgradeSO>();
            var done = new HashSet<WorkUpgradeSO>();
            foreach (var node in Nodes) CheckPath(node, visiting, done);
        }

        private static void CheckPath(WorkUpgradeSO node, HashSet<WorkUpgradeSO> visiting, HashSet<WorkUpgradeSO> done)
        {
            if (done.Contains(node)) return;
            if (!visiting.Add(node)) throw new ArgumentException("Upgrade prerequisites contain a cycle: " + node.Id);
            foreach (var required in node.Required) CheckPath(required, visiting, done);
            visiting.Remove(node);
            done.Add(node);
        }
    }
}
