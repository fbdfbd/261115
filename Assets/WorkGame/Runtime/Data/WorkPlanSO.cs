using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorkGame
{
    [Serializable]
    public sealed class WorkBatch
    {
        [Range(1, 12)] public int Month = 1;
        public WorkCategorySO Category;
        [Min(0)] public int Count = 5;
    }

    [CreateAssetMenu(menuName = "Work Game/Month Plan")]
    public sealed class WorkPlanSO : ScriptableObject
    {
        public List<WorkBatch> Batches = new List<WorkBatch>();
    }
}
