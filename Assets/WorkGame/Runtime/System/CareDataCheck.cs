using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    internal static class CareDataCheck
    {
        internal static void Check(CareLesson[] lessons, CareSynergy[] synergies, CareGift[] gifts,
            CareEnding[] endings, CareEnding fallback, IEnumerable<CareGain> initial, int lowMax, int highMin)
        {
            CheckIds(lessons.Select(x => x.Id), "수업");
            CheckIds(synergies.Select(x => x.Id), "시너지");
            CheckIds(gifts.Select(x => x.Id), "선물");
            CheckIds(endings.Select(x => x.Id).Append(fallback.Id), "엔딩");
            CheckGains(initial);
            if (lowMax >= highMin) throw new ArgumentException("친밀도 낮음 상한은 높음 하한보다 작아야 합니다.");
            foreach (var lesson in lessons)
            {
                CheckItem(lesson.Name, lesson.Cost, lesson.UnlockMonth);
                CheckGains(lesson.Gains);
            }
            foreach (string id in new[] { CareSystem.HomeId, CareSystem.RestId })
                if (!lessons.Any(x => x.Id == id && x.Cost == 0 && x.UnlockMonth == 1))
                    throw new ArgumentException("무료 기본 수업이 필요합니다: " + id);
            foreach (var synergy in synergies)
            {
                if (string.IsNullOrWhiteSpace(synergy.Name) || synergy.Lessons.Count < 2
                    || synergy.Lessons.Count > CareSystem.SlotCount
                    || synergy.Lessons.Any(id => !lessons.Any(x => x.Id == id)))
                    throw new ArgumentException("시너지는 등록된 수업 2~4개로 지정하세요: " + synergy.Id);
                CheckGains(synergy.Gains);
            }
            foreach (var gift in gifts)
            {
                if (string.IsNullOrWhiteSpace(gift.Name) || gift.Cost < 0 || !Enum.IsDefined(typeof(CareGiftKind), gift.Kind))
                    throw new ArgumentException("선물 이름·가격·분류를 확인하세요: " + gift.Id);
                foreach (var bonus in gift.Bonuses)
                    if (!Enum.IsDefined(typeof(CareStat), bonus.Stat) || bonus.Percent < 0)
                        throw new ArgumentException("선물 보정은 유효한 스탯과 0 이상의 퍼센트가 필요합니다: " + gift.Id);
            }
            foreach (var ending in endings.Append(fallback))
            {
                if (string.IsNullOrWhiteSpace(ending.Name)) throw new ArgumentException("엔딩 이름을 확인하세요: " + ending.Id);
                foreach (var need in ending.MainNeeds)
                    if (!CareStats.IsMain(need.Stat) || !Enum.IsDefined(typeof(CareGrade), need.Grade) || need.Grade == CareGrade.None)
                        throw new ArgumentException("엔딩 대표 조건은 지식/체력/감성/사교와 B/A/S 등급으로 지정하세요: " + ending.Id);
                CheckGains(ending.Needs);
            }
        }

        private static void CheckIds(IEnumerable<string> ids, string label)
        {
            var seen = new HashSet<string>();
            foreach (string id in ids)
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                    throw new ArgumentException(label + " ID가 비어 있거나 중복되었습니다: " + id);
        }

        private static void CheckItem(string name, int cost, int month)
        {
            if (string.IsNullOrWhiteSpace(name) || cost < 0 || (month != 1 && month != 3 && month != 6 && month != 9))
                throw new ArgumentException("이름·가격·해금 월(1/3/6/9)을 확인하세요: " + name);
        }

        private static void CheckGains(IEnumerable<CareGain> gains)
        {
            if (gains == null || gains.Any(x => x == null || !Enum.IsDefined(typeof(CareStat), x.Stat)
                || CareStats.IsMain(x.Stat) || x.Amount < 0))
                throw new ArgumentException("성장량·세부 엔딩 조건에는 세부 스탯과 0 이상의 수치를 지정하세요. 대표 스탯은 자동 합산됩니다.");
        }
    }
}
