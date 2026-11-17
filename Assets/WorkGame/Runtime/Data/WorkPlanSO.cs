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
        [Tooltip("전체 등록 카테고리 중 1~3월은 1개, 4~6월은 2개, 7월 이후는 3개를 추첨합니다. 월별 Count 합계는 유지합니다.")]
        public bool RandomizeCategories;
        public List<WorkBatch> Batches = new List<WorkBatch>();
    }
}
