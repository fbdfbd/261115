using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorkGame
{
    [CreateAssetMenu(menuName = "Work Game/Upgrade Node")]
    public sealed class WorkUpgradeSO : ScriptableObject
    {
        public string Id;
        public string Name;
        [TextArea] public string Description;
        public Sprite Icon;
        [Min(0)] public int Cost;
        public List<WorkUpgradeSO> Required = new List<WorkUpgradeSO>();
        public List<WorkUpgradeEffect> Effects = new List<WorkUpgradeEffect>();
        public WorkUpgradeSwitch SwitchKind => Effects.Any(x => x != null && x.Kind == WorkUpgradeEffectKind.FaxUnlock)
            ? WorkUpgradeSwitch.Fax : Effects.Any(x => x != null && x.Kind == WorkUpgradeEffectKind.SortUnlock)
                ? WorkUpgradeSwitch.Sort : Effects.Any(x => x != null && x.Kind == WorkUpgradeEffectKind.DoNotDisturbUnlock)
                    ? WorkUpgradeSwitch.DoNotDisturb : WorkUpgradeSwitch.None;
    }
}
