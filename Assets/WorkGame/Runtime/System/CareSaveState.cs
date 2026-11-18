using System;
using System.Linq;

namespace WorkGame
{
    public sealed partial class CareSystem
    {
        public CareSaveData CaptureSave(SavePoint point) => new CareSaveData
        {
            Stats = Enumerable.Range(4, 12).Select(index => Run.GetStat((CareStat)index)).ToArray(),
            Friendship = Run.Friendship, FinishedMonth = Run.FinishedMonth,
            Gifts = Run.Gifts.ToArray(), ActiveGifts = Run.ActiveGifts.ToArray(),
            EndingChoices = point == SavePoint.EndingChoice ? GetEndingChoices().Select(ending => ending.Id).ToArray() : Array.Empty<string>()
        };

        public void RestoreSave(CareSaveData data)
        {
            if (disposed) throw new ObjectDisposedException(nameof(CareSystem));
            if (data?.Stats == null || data.Stats.Length != 12 || data.Stats.Any(value => value < 0 || value > StatMax)
                || data.FinishedMonth < 0 || data.FinishedMonth > 12 || data.Gifts == null || data.ActiveGifts == null
                || data.ActiveGifts.Length != Run.ActiveGifts.Count || data.EndingChoices == null
                || data.Gifts.Any(id => id == null || !gifts.ContainsKey(id)) || data.Gifts.Distinct().Count() != data.Gifts.Length)
                throw new ArgumentException("육성 저장 값을 확인하세요.");
            for (int i = 0; i < data.ActiveGifts.Length; i++)
            {
                string id = data.ActiveGifts[i];
                if (!string.IsNullOrEmpty(id) && (!data.Gifts.Contains(id) || (int)gifts[id].Kind != i))
                    throw new ArgumentException("장착 가구의 분류 또는 소유 기록이 다릅니다.");
            }
            var choices = endings.Append(fallbackEnding).ToDictionary(ending => ending.Id);
            if (data.EndingChoices.Length > 5 || data.EndingChoices.Any(id => id == null || !choices.ContainsKey(id))
                || data.EndingChoices.Distinct().Count() != data.EndingChoices.Length)
                throw new ArgumentException("저장된 엔딩 후보를 찾을 수 없습니다.");
            Run.RestoreSave(data);
            endingChoices = data.EndingChoices.Length == 0 ? null
                : Array.AsReadOnly(data.EndingChoices.Select(id => choices[id]).ToArray());
            Array.Clear(plan, 0, plan.Length);
            shownMonth = Month; shownPhase = work.Run.Phase;
        }
    }

    public sealed partial class CareRun
    {
        internal void RestoreSave(CareSaveData data)
        {
            Array.Copy(data.Stats, stats, stats.Length);
            gifts.Clear(); gifts.AddRange(data.Gifts);
            for (int i = 0; i < activeGifts.Length; i++)
                activeGifts[i] = string.IsNullOrEmpty(data.ActiveGifts[i]) ? null : data.ActiveGifts[i];
            Friendship = data.Friendship; FinishedMonth = data.FinishedMonth;
            LastResult = null; Ending = null;
        }
    }
}
