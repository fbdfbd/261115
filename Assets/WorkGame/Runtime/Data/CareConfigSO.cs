using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorkGame
{
    [CreateAssetMenu(menuName = "Work Game/Care/Config")]
    public sealed class CareConfigSO : ScriptableObject
    {
        public List<CareLessonSO> Lessons = new List<CareLessonSO>();
        public List<CareSynergySO> Synergies = new List<CareSynergySO>();
        public List<CareGiftSO> Gifts = new List<CareGiftSO>();
        public List<CareEndingSO> Endings = new List<CareEndingSO>();
        public CareEndingSO FallbackEnding;
        public List<CareGain> InitialStats = new List<CareGain>();
        public int InitialFriendship;
        public int LowFriendshipMax = -1;
        public int HighFriendshipMin = 50;
        public CareGradeSettings Grades = new CareGradeSettings();
        [Min(1)] public int StatMax = 300;
        [Tooltip("월별 대화·퇴근 반응·약속 JSON입니다. 이미지는 ImageKey로 View에 전달합니다.")]
        public TextAsset Dialogues;
        [Header("무료 기본 수업 — 항상 4칸 편성 가능")]
        public List<CareGain> HomeGains = new List<CareGain>
        {
            new CareGain(CareStat.Focus, 1), new CareGain(CareStat.Endurance, 1),
            new CareGain(CareStat.Empathy, 1), new CareGain(CareStat.Teamwork, 1)
        };
        public List<CareGain> RestGains = new List<CareGain>();

        public CareSystem CreateSystem(WorkSystem work)
        {
            if (Lessons == null || Synergies == null || Gifts == null || Endings == null
                || Lessons.Any(x => x == null) || Synergies.Any(x => x == null)
                || Gifts.Any(x => x == null) || Endings.Any(x => x == null))
                throw new ArgumentException("육성 목록에 비어 있는 SO가 있습니다.");
            var free = new[]
            {
                new CareLesson(CareSystem.HomeId, "홈스쿨링", 1, 0, HomeGains),
                new CareLesson(CareSystem.RestId, "휴식", 1, 0, RestGains)
            };
            var fallback = FallbackEnding != null ? FallbackEnding.CreateEnding()
                : new CareEnding("fallback", "백수", int.MinValue, Array.Empty<CareGain>(), "", "", "", "");
            return new CareSystem(work, free.Concat(Lessons.Select(x => x.CreateLesson())),
                Synergies.Select(x => x.CreateSynergy()), Gifts.Select(x => x.CreateGift()),
                Endings.Select(x => x.CreateEnding()), fallback, InitialStats,
                InitialFriendship, LowFriendshipMax, HighFriendshipMin, Grades, StatMax);
        }

        public CareDialogueFile CreateDialogues()
        {
            if (Dialogues == null) return new CareDialogueFile();
            var data = JsonUtility.FromJson<CareDialogueFile>(Dialogues.text);
            if (data == null) throw new ArgumentException("육성 대화 JSON을 읽을 수 없습니다.");
            if (data.Dialogues != null)
                foreach (var dialogue in data.Dialogues)
                    if (dialogue != null && dialogue.Choices == null) dialogue.Choices = Array.Empty<CareDialogueChoice>();
            return data;
        }
    }
}
