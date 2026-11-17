using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorkGame
{
    [CreateAssetMenu(menuName = "Work Game/Care/Synergy")]
    public sealed class CareSynergySO : ScriptableObject
    {
        public string Id;
        public string Name;
        [TextArea] public string Description;
        [Tooltip("순서 무관. 같은 수업 2회 조건은 같은 SO를 두 번 넣습니다. 월 1회 발동합니다.")]
        public List<CareLessonSO> Lessons = new List<CareLessonSO>();
        public List<CareGain> Gains = new List<CareGain>();
        public CareSynergy CreateSynergy() => new CareSynergy(Id, Name,
            Lessons.Select(x => x != null ? x.Id : throw new ArgumentException("시너지 수업이 비어 있습니다: " + Id)), Gains);
    }
}
