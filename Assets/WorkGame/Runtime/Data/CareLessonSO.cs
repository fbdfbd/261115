using System.Collections.Generic;
using UnityEngine;

namespace WorkGame
{
    [CreateAssetMenu(menuName = "Work Game/Care/Lesson")]
    public sealed class CareLessonSO : ScriptableObject
    {
        public string Id;
        public string Name;
        public string ShortName;
        public string DisplayName => string.IsNullOrWhiteSpace(ShortName) ? Name : ShortName;
        [TextArea] public string Description;
        public Sprite Icon;
        [Header("육성 결과 — 이미지가 없으면 결과창 기본 이미지 사용")]
        public Sprite ResultImage;
        [TextArea(1, 2)] public string ResultLine;
        [Tooltip("1 / 3 / 6 / 9월. 해당 월 퇴근 후부터 이용합니다.")]
        public int UnlockMonth = 1;
        [Min(0)] public int Cost;
        public List<CareGain> Gains = new List<CareGain>();
        public CareLesson CreateLesson() => new CareLesson(Id, Name, UnlockMonth, Cost, Gains);
    }
}
