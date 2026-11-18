using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace WorkGame
{
    public sealed partial class WorkSystem
    {
        public WorkSaveData CaptureSave() => new WorkSaveData
        {
            PlanKey = GetPlanKey(), Month = Run.Month, Phase = Run.Phase, Seconds = Run.Seconds,
            Attendance = Run.Attendance, Bonus = Run.Bonus, Money = Run.Money, SkillPoints = Run.SkillPoints,
            Upgrades = Run.BoughtUpgrades.ToArray(), SortEnabled = Run.SortEnabled, FaxVisible = Run.FaxVisible,
            RandomSeed = randomSeed, RandomCalls = randomCalls, WorkSpeed = WorkSpeed, PointsPerPaper = SkillPointsPerPaper,
            Right = rightThisMonth, Wrong = wrongThisMonth, ProtectionUsed = newbieProtectionUsed,
            PointsEarned = SkillPointsEarnedThisMonth, PointPenalty = SkillPointPenaltyThisMonth,
            Papers = Run.Papers.Select(paper => new SavedPaper { Id = paper.Id, Choice = paper.PickedIndex }).ToArray(),
            MonthPapers = monthPapers.Select(paper => paper.Id).ToArray(),
            Months = Run.Months.Select(SaveMonth).ToArray()
        };

        // 새 시스템에만 복원합니다. 호출자는 모두 복원한 뒤 View를 연결합니다.
        public void RestoreSave(WorkSaveData data)
        {
            if (disposed || Run.Phase != WorkPhase.Ready) throw new InvalidOperationException("새 업무 시스템에만 복원할 수 있습니다.");
            CheckSave(data);
            var papers = plannedPapers.ToDictionary(paper => paper.Id);
            foreach (var saved in data.Papers)
            {
                var paper = papers[saved.Id];
                if (saved.Choice >= 0) paper.Submit(saved.Choice);
                Run.AddPaper(paper);
            }
            foreach (int id in data.MonthPapers) monthPapers.Add(papers[id]);
            foreach (var month in data.Months) Run.AddResult(LoadMonth(month));
            foreach (string id in data.Upgrades) Run.AddUpgrade(id);
            Run.Month = data.Month; Run.Phase = data.Phase; Run.Seconds = data.Seconds;
            Run.Attendance = data.Attendance; Run.Bonus = data.Bonus; Run.Money = data.Money;
            Run.SkillPoints = data.SkillPoints; Run.SortEnabled = data.SortEnabled; Run.FaxVisible = data.FaxVisible;
            rightThisMonth = data.Right; wrongThisMonth = data.Wrong; newbieProtectionUsed = data.ProtectionUsed;
            SkillPointsEarnedThisMonth = data.PointsEarned; SkillPointPenaltyThisMonth = data.PointPenalty;
            WorkSpeed = data.WorkSpeed; SkillPointsPerPaper = data.PointsPerPaper;
            randomSeed = data.RandomSeed; randomCalls = data.RandomCalls;
            random = new System.Random(randomSeed);
            for (int i = 0; i < randomCalls; i++) random.NextDouble();
        }

        private void CheckSave(WorkSaveData data)
        {
            if (data == null || data.PlanKey != GetPlanKey()) throw new ArgumentException("업무 데이터가 저장 당시와 다릅니다.");
            if (data.Month < 1 || data.Month > 12 || data.Papers == null || data.MonthPapers == null || data.Months == null
                || data.Upgrades == null || data.Upgrades.Any(string.IsNullOrWhiteSpace)
                || data.Upgrades.Distinct().Count() != data.Upgrades.Length
                || data.Money < 0 || data.SkillPoints < 0 || data.Attendance < 0 || data.Bonus < 0
                || data.RandomCalls < 0 || data.RandomCalls > 1000000 || !WorkSettings.IsPositive(data.WorkSpeed)
                || data.PointsPerPaper < 0 || double.IsNaN(data.Seconds) || double.IsInfinity(data.Seconds) || data.Seconds < 0
                || data.Right < 0 || data.Wrong < 0 || data.PointsEarned < 0 || data.PointPenalty < 0)
                throw new ArgumentException("업무 저장 값이 올바르지 않습니다.");
            if (data.Phase != WorkPhase.Working && data.Phase != (data.Month == 12 ? WorkPhase.Finished : WorkPhase.AfterWork))
                throw new ArgumentException("업무 진행 단계가 올바르지 않습니다.");
            var expected = plannedPapers.Where(paper => paper.Month <= data.Month).ToDictionary(paper => paper.Id);
            if (data.Papers.Length != expected.Count || data.Papers.Any(saved => saved == null
                || !expected.TryGetValue(saved.Id, out var paper) || saved.Choice < -1 || saved.Choice >= paper.Choices.Count)
                || data.Papers.Select(saved => saved.Id).Distinct().Count() != data.Papers.Length)
                throw new ArgumentException("서류 저장 값이 올바르지 않습니다.");
            var assigned = data.Papers.Select(saved => saved.Id).ToHashSet();
            if (data.MonthPapers.Any(id => !assigned.Contains(id)) || data.MonthPapers.Distinct().Count() != data.MonthPapers.Length
                || data.Papers.Any(saved => (expected[saved.Id].Month == data.Month || saved.Choice == -1) && !data.MonthPapers.Contains(saved.Id))
                || data.Months.Length != data.Month - (data.Phase == WorkPhase.Working ? 1 : 0)
                || data.Months.Where((month, index) => month == null || month.Month != index + 1).Any())
                throw new ArgumentException("월별 실적 또는 서류 목록이 올바르지 않습니다.");
        }

        private string GetPlanKey()
        {
            // 같은 Seed라도 SO 내용이 달라졌으면 잘못된 서류로 조용히 복원하지 않습니다.
            var text = new StringBuilder(JsonUtility.ToJson(settings));
            foreach (var paper in plannedPapers)
            {
                text.Append('|').Append(paper.Id).Append(':').Append(paper.Month).Append(':').Append(paper.CategoryId)
                    .Append(':').Append((int)paper.Rule).Append(':').Append((int)paper.Display).Append(':').Append(paper.Metric)
                    .Append(':').Append(paper.Title).Append(':').Append(paper.DisplayMax);
                foreach (var choice in paper.Choices) text.Append('/').Append(choice.Name).Append('=').Append(choice.Value);
            }
            using (var hash = SHA256.Create())
                return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
        }

        private static SavedWorkMonth SaveMonth(MonthResult month) => new SavedWorkMonth
        {
            Month = month.Month, Right = month.Right, Wrong = month.Wrong, Pending = month.Pending, Attendance = month.Attendance,
            Overtime = month.OvertimeSeconds, LeaveDifference = month.LeaveDifference, ExtraPointPercent = month.ExtraPointPercent,
            PointsEarned = month.SkillPointsEarned, PointPenalty = month.SkillPointPenalty, MoneyBalance = month.MoneyBalance,
            EarlyBonus = month.EarlyLeaveBonus, Salary = month.Salary, ProtectionUsed = month.NewbieProtectionUsed,
            HasReview = month.Review != null, ReviewRight = month.Review?.Right ?? 0, ReviewTotal = month.Review?.Total ?? 0,
            BonusPercent = month.Review?.BonusPercent ?? 0, Grade = month.Review?.Grade ?? WorkGrade.B, Bonus = month.Review?.Bonus ?? 0
        };

        private static MonthResult LoadMonth(SavedWorkMonth month) => new MonthResult(month.Month, month.Overtime,
            month.Right, month.Wrong, month.Pending, month.Attendance,
            month.HasReview ? new QuarterResult(month.Month, month.ReviewRight, month.ReviewTotal, month.Grade, month.Bonus, month.BonusPercent) : null,
            month.PointsEarned, month.EarlyBonus, month.ProtectionUsed, month.PointPenalty, month.ExtraPointPercent,
            month.Salary, month.MoneyBalance, month.LeaveDifference);
    }
}
