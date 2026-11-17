using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorkGame
{
    [CreateAssetMenu(menuName = "Work Game/Config")]
    public sealed class WorkConfigSO : ScriptableObject
    {
        public WorkSettings Settings = new WorkSettings();
        public WorkPlanSO Plan;
        [Range(2, 5)] public int ChoiceCount = 3;

        public List<string> GetErrors() => WorkDataCheck.GetErrors(this);

        public WorkSystem CreateSystem(int seed)
        {
            var errors = GetErrors();
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            return new WorkSystem(Settings, WorkPaperFactory.Create(Plan, ChoiceCount, seed), unchecked(seed + 3571));
        }
    }
}
