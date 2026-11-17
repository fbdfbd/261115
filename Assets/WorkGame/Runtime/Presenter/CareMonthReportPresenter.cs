using System;
using System.Linq;

namespace WorkGame
{
    public sealed class CareMonthReportPresenter : IDisposable
    {
        private readonly CareSystem care;
        private readonly CareFlowSystem flow;
        private readonly CareMonthReportView view;
        private readonly CareConfigSO config;
        private readonly CareGradeSettings grades;
        private CareMonthResult shown;
        private int weekIndex;
        private bool connected;

        public CareMonthReportPresenter(CareSystem care, CareFlowSystem flow, CareMonthReportView view, CareConfigSO config)
        {
            this.care = care; this.flow = flow; this.view = view; this.config = config;
            grades = care.CopyGrades();
        }
        public void Connect()
        {
            if (connected || view == null) return;
            connected = true;
            care.MonthFinished += Show;
            flow.Changed += Refresh;
            view.Confirmed += Confirm;
            Refresh();
        }
        private void Refresh()
        {
            if (flow.Phase == CareFlowPhase.Result && care.HasResult) Show(care.Run.LastResult);
            else { shown = null; if (view != null) view.Hide(); }
        }
        private void Show(CareMonthResult result)
        {
            if (view == null || result == null || shown == result || flow.Phase != CareFlowPhase.Result) return;
            shown = result; weekIndex = 0;
            ShowWeek();
        }
        private void ShowWeek()
        {
            var result = shown;
            var week = result.Weeks.Count > 0 ? result.Weeks[weekIndex] : null;
            string lessonId = week != null ? week.LessonId : result.Schedule.Lessons.FirstOrDefault();
            var lesson = config.Lessons.FirstOrDefault(item => item != null && item.Id == lessonId);
            string line = lesson != null
                ? string.IsNullOrWhiteSpace(lesson.ResultLine) ? lesson.DisplayName + " 수업에서 배운 내용을 집에서도 연습했어요." : lesson.ResultLine
                : lessonId == CareSystem.RestId
                    ? "푹 쉬었어요. 꼬리까지 느긋하게 늘어뜨릴 만큼요."
                    : "집에서 도토리를 세고 책을 읽으며 차근차근 복습했어요.";
            foreach (var row in view.Stats)
            {
                if (row == null || !CareStats.IsMain(row.Stat)) continue;
                int monthAfter = result.Stats.First(item => item.Stat == row.Stat).Amount;
                int before = monthAfter - result.Schedule.GetGain(row.Stat);
                int delta = result.Schedule.GetGain(row.Stat);
                if (week != null)
                {
                    before += result.Weeks.Take(weekIndex).Sum(item => item.GetGain(row.Stat));
                    delta = week.GetGain(row.Stat);
                }
                int after = before + delta;
                row.Prepare(before, after, delta, grades);
            }
            view.Open(result.Month, line, lesson != null ? lesson.ResultImage : null, week != null ? week.Week : 0);
        }
        private void Confirm()
        {
            if (shown == null || flow.Phase != CareFlowPhase.Result) return;
            if (weekIndex + 1 < shown.Weeks.Count)
            {
                weekIndex++;
                ShowWeek();
                return;
            }
            shown = null;
            view.Hide();
            flow.ConfirmResult();
        }
        public void Disconnect()
        {
            if (!connected) return;
            connected = false;
            care.MonthFinished -= Show; flow.Changed -= Refresh;
            if (view != null) { view.Confirmed -= Confirm; view.Hide(); }
            shown = null;
        }
        public void Dispose() => Disconnect();
    }
}
