using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public sealed partial class CareSystem : IDisposable
    {
        public const int SlotCount = 4;
        public const string HomeId = "home";
        public const string RestId = "rest";
        private readonly WorkSystem work;
        private readonly Dictionary<string, CareLesson> lessons;
        private readonly Dictionary<string, CareGift> gifts;
        private readonly CareSynergy[] synergies;
        private readonly CareEnding[] endings;
        private readonly CareEnding fallbackEnding;
        private readonly int lowFriendshipMax, highFriendshipMin;
        private readonly CareGradeSettings grades;
        private readonly string[] plan = new string[SlotCount];
        private int shownMonth;
        private WorkPhase shownPhase;
        private bool disposed;
        private IReadOnlyList<CareEnding> endingChoices;

        public CareRun Run { get; }
        public int StatMax => Run.StatMax;
        public IReadOnlyList<string> Plan { get; }
        public IReadOnlyList<CareLesson> Lessons { get; }
        public IReadOnlyList<CareGift> Gifts { get; }
        public IReadOnlyList<CareSynergy> Synergies { get; }
        public long Money => work.Run.Money;
        public int Month => work.Run.Month;
        public bool CanPlan => !disposed && !work.IsPaused && Run.FinishedMonth != Month && Month > 0
            && (work.Run.Phase == WorkPhase.AfterWork || work.Run.Phase == WorkPhase.Finished);
        // 엔딩 선택으로 복원할 때는 이미 확인한 결과창을 다시 만들지 않습니다.
        public bool HasResult => Month > 0 && Run.FinishedMonth == Month;
        public bool IsPlanFull => plan.All(id => !string.IsNullOrEmpty(id));
        public event Action Changed;
        public event Action<CareMonthResult> MonthFinished;
        public event Action<CareEndingResult> EndingReady;

        public CareSystem(WorkSystem work, IEnumerable<CareLesson> lessons, IEnumerable<CareSynergy> synergies,
            IEnumerable<CareGift> gifts, IEnumerable<CareEnding> endings, CareEnding fallbackEnding,
            IEnumerable<CareGain> initialStats, int friendship = 0, int lowFriendshipMax = -1, int highFriendshipMin = 50,
            CareGradeSettings grades = null, int statMax = 300)
        {
            Run = new CareRun(statMax);
            this.work = work ?? throw new ArgumentNullException(nameof(work));
            var lessonList = lessons.ToArray();
            var giftList = gifts.ToArray();
            this.synergies = synergies.ToArray();
            this.endings = endings.OrderByDescending(x => x.Priority).ThenBy(x => x.Id, StringComparer.Ordinal).ToArray();
            this.fallbackEnding = fallbackEnding ?? throw new ArgumentNullException(nameof(fallbackEnding));
            this.lowFriendshipMax = lowFriendshipMax; this.highFriendshipMin = highFriendshipMin;
            this.grades = (grades ?? new CareGradeSettings()).Copy();
            this.grades.Check();
            CareDataCheck.Check(lessonList, this.synergies, giftList, this.endings, fallbackEnding,
                initialStats, lowFriendshipMax, highFriendshipMin);
            this.lessons = lessonList.ToDictionary(x => x.Id);
            this.gifts = giftList.ToDictionary(x => x.Id);
            Lessons = Array.AsReadOnly(lessonList); Gifts = Array.AsReadOnly(giftList);
            Synergies = Array.AsReadOnly(this.synergies);
            Plan = Array.AsReadOnly(plan);
            Run.AddStats(initialStats);
            Run.Friendship = friendship;
            work.Changed += OnWorkChanged;
        }

        public bool SetLesson(int slot, string lessonId)
        {
            if (!CanPlan || slot < 0 || slot >= SlotCount || lessonId == null
                || !lessons.TryGetValue(lessonId, out var lesson) || lesson.UnlockMonth > Month) return false;
            if (plan[slot] == lessonId) return true;
            plan[slot] = lessonId;
            Changed?.Invoke();
            return true;
        }

        public int GetLessonCost(string id) => id != null && lessons.TryGetValue(id, out var lesson)
            ? work.Upgrades.GetTuitionCost(lesson.Cost) : 0;

        public bool ClearLesson(int slot)
        {
            if (!CanPlan || slot < 0 || slot >= SlotCount) return false;
            plan[slot] = null;
            Changed?.Invoke();
            return true;
        }

        public CarePreview Preview() => BuildPreview(plan);

        private CarePreview BuildPreview(IEnumerable<string> lessonIds)
        {
            var slots = lessonIds.ToArray();
            var selected = slots.Where(id => !string.IsNullOrEmpty(id)).Select(id => lessons[id]).ToArray();
            long cost = selected.Sum(x => (long)work.Upgrades.GetTuitionCost(x.Cost));
            var counts = selected.GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.Count());
            var active = synergies.Where(synergy => synergy.Lessons.GroupBy(x => x)
                .All(need => counts.TryGetValue(need.Key, out int count) && count >= need.Count())).ToArray();
            var baseGains = SumGains(selected.SelectMany(x => x.Gains));
            var synergyGains = SumGains(active.SelectMany(x => x.Gains));
            var percents = new long[16];
            foreach (var id in Run.ActiveGifts)
            {
                if (string.IsNullOrEmpty(id)) continue;
                foreach (var bonus in gifts[id].Bonuses) percents[(int)bonus.Stat] += bonus.Percent;
            }
            var total = new long[16];
            var extra = new long[16];
            for (int i = 4; i < total.Length; i++)
            {
                long before = baseGains[i] + synergyGains[i];
                long percent = percents[i] + percents[(int)CareStats.GetMain((CareStat)i)];
                int room = StatMax - Run.GetStat((CareStat)i);
                total[i] = (long)Math.Min(room, Math.Floor(before * (1 + percent / 100m)));
                extra[i] = Math.Max(0, total[i] - Math.Min(room, before));
            }
            return new CarePreview(cost, slots, active.Select(x => x.Id).ToArray(),
                ToGains(baseGains), ToGains(synergyGains), ToGains(extra), ToGains(total));
        }

        public bool FinishMonth()
        {
            if (!CanPlan || !IsPlanFull) return false;
            // 예측 결과를 재사용하지 않고 현재 잔액·할인·선물로 다시 계산합니다.
            var result = Preview();
            if (!work.SpendMoney(result.Cost)) return false;
            var weeks = BuildWeekResults();
            Run.AddStats(result.TotalGains);
            Run.FinishedMonth = Month;
            Run.LastResult = new CareMonthResult(Month, result, Money, Run, grades, weeks);
            work.Notify();
            Changed?.Invoke();
            MonthFinished?.Invoke(Run.LastResult);
            return true;
        }

        private CareWeekResult[] BuildWeekResults()
        {
            var weeks = new CareWeekResult[SlotCount];
            var previous = new int[16];
            for (int week = 0; week < SlotCount; week++)
            {
                // 누적 성장의 차이로 나누어 가구 보정의 소수점 버림·최대치까지 월 합계와 맞춥니다.
                // 시너지는 필요한 수업을 모두 들은 주에 한 번 포함됩니다.
                var cumulative = BuildPreview(plan.Take(week + 1));
                var gains = new List<CareGain>();
                for (int index = 4; index < previous.Length; index++)
                {
                    var stat = (CareStat)index;
                    int total = cumulative.GetGain(stat);
                    int change = total - previous[index];
                    if (change != 0) gains.Add(new CareGain(stat, change));
                    previous[index] = total;
                }
                weeks[week] = new CareWeekResult(week + 1, plan[week], gains.ToArray());
            }
            return weeks;
        }

        public bool CanBuyGift(string id) => CanPlan && id != null && gifts.TryGetValue(id, out var gift)
            && !Run.Gifts.Contains(id) && Money >= gift.Cost;

        public bool BuyGift(string id)
        {
            if (!CanBuyGift(id)) return false;
            var gift = gifts[id];
            if (!work.SpendMoney(gift.Cost)) return false;
            Run.AddGift(id);
            Run.SetActiveGift(gift.Kind, id);
            ChangeFriendship(gift.Friendship);
            work.Notify();
            Changed?.Invoke();
            return true;
        }

        public bool SetActiveGift(string id)
        {
            if (!CanPlan || id == null || !gifts.TryGetValue(id, out var gift) || !Run.Gifts.Contains(id)) return false;
            if (Run.GetActiveGift(gift.Kind) == id) return true;
            Run.SetActiveGift(gift.Kind, id);
            Changed?.Invoke();
            return true;
        }

        public bool ClearActiveGift(CareGiftKind kind)
        {
            if (!CanPlan || !Enum.IsDefined(typeof(CareGiftKind), kind)) return false;
            if (Run.GetActiveGift(kind) == null) return true;
            Run.SetActiveGift(kind, null);
            Changed?.Invoke();
            return true;
        }

        public CareGrade GetGrade(CareStat main) => GetGrade(main, Run.GetStat(main));
        public CareGradeSettings CopyGrades() => grades.Copy();

        public CareGrade GetGrade(CareStat main, int value)
        {
            if (!CareStats.IsMain(main)) throw new ArgumentException("대표 스탯만 등급을 계산합니다.", nameof(main));
            return grades.GetGrade(value);
        }

        // 칼퇴·야근 등에서 호출할 통로입니다. 여기에는 획득 산식을 두지 않습니다.
        public void AddFriendship(int amount)
        {
            if (disposed || amount == 0 || Run.Ending != null) return;
            ChangeFriendship(amount);
            Changed?.Invoke();
        }

        public IReadOnlyList<CareEnding> GetEndingChoices()
        {
            if (disposed || Month != 12 || !HasResult || work.Run.Phase != WorkPhase.Finished) return Array.Empty<CareEnding>();
            if (endingChoices != null) return endingChoices;
            var choices = endings.Where(ending => ending.MainNeeds.All(need => (int)GetGrade(need.Stat) >= (int)need.Grade)
                && ending.Needs.All(need => Run.GetStat(need.Stat) >= need.Amount)).Take(5).ToArray();
            endingChoices = Array.AsReadOnly(choices.Length > 0 ? choices : new[] { fallbackEnding });
            return endingChoices;
        }

        public CareEndingResult ResolveEnding()
        {
            if (disposed) return null;
            if (Run.Ending != null) return Run.Ending;
            var choices = GetEndingChoices();
            if (choices.Count == 1) ChooseEnding(choices[0].Id);
            return Run.Ending;
        }

        public bool ChooseEnding(string id)
        {
            if (disposed || Run.Ending != null || string.IsNullOrEmpty(id)) return false;
            var ending = GetEndingChoices().FirstOrDefault(choice => choice.Id == id);
            if (ending == null) return false;
            var friendship = Run.Friendship <= lowFriendshipMax ? CareFriendship.Low
                : Run.Friendship >= highFriendshipMin ? CareFriendship.High : CareFriendship.Normal;
            Run.Ending = new CareEndingResult(ending, friendship);
            Changed?.Invoke();
            EndingReady?.Invoke(Run.Ending);
            return true;
        }

        private void ChangeFriendship(int amount) => Run.Friendship =
            (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, Run.Friendship + (long)amount));

        private void OnWorkChanged()
        {
            if (shownMonth == Month && shownPhase == work.Run.Phase) return;
            if (shownMonth != Month)
                Array.Clear(plan, 0, plan.Length);
            shownMonth = Month;
            shownPhase = work.Run.Phase;
            Changed?.Invoke();
        }

        private static long[] SumGains(IEnumerable<CareGain> gains)
        {
            var result = new long[16];
            foreach (var gain in gains) result[(int)gain.Stat] += gain.Amount;
            return result;
        }

        private static CareGain[] ToGains(long[] values) => values.Select((amount, stat) =>
            new CareGain((CareStat)stat, (int)Math.Min(int.MaxValue, amount))).Where(x => x.Amount != 0).ToArray();

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            work.Changed -= OnWorkChanged;
            Changed = null; MonthFinished = null; EndingReady = null;
        }
    }
}
