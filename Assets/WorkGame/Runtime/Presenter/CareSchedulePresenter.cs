using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareSchedulePresenter : IDisposable
    {
        private readonly WorkSystem work;
        private readonly CarePresenter actions;
        private readonly CareSystem care;
        private readonly CareFlowSystem flow;
        private readonly CareScheduleView view;
        private readonly Button openButton;
        private readonly Dictionary<string, CareLesson> lessons;
        private readonly Dictionary<string, CareLessonSO> art;
        private readonly CareConfigSO config;
        private readonly CareSynergy[] synergies;
        private readonly CareGradeSettings grades;
        private readonly HashSet<string> shownUnlocks;
        private CarePreview preview;
        private string selected, focusedSynergy;
        private bool connected, wasVisible;
        private int shownMonth = -1;
        private long shownMoney;
        private double shownDiscount;
        private double shownGrowth;
        private bool Visible => flow.Phase == CareFlowPhase.Panel && flow.Panel == CarePanel.Schedule;

        public CareSchedulePresenter(WorkSystem work, CarePresenter actions, CareFlowSystem flow,
            CareScheduleView view, Button openButton, CareConfigSO config)
        {
            this.work = work; this.actions = actions; care = actions.System;
            this.flow = flow; this.view = view; this.openButton = openButton;
            lessons = care.Lessons.ToDictionary(x => x.Id);
            art = config.Lessons.Where(x => x != null).ToDictionary(x => x.Id);
            this.config = config;
            synergies = care.Synergies.ToArray();
            grades = care.CopyGrades();
            shownUnlocks = new HashSet<string>(care.Lessons.Where(x => x.UnlockMonth <= 1).Select(x => x.Id));
        }
        public void Connect()
        {
            if (connected) return;
            connected = true;
            care.Changed += RefreshData; flow.Changed += RefreshPanel; work.Changed += RefreshMoney;
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (view != null)
            {
                view.CloseClicked += Close; view.LessonClicked += Select;
                foreach (var week in view.Weeks) if (week != null) week.Clicked += Clear;
                foreach (var row in view.Synergies) if (row != null) row.Clicked += FocusSynergy;
            }
            RefreshPanel();
        }
        public void Open() { if (view != null) flow.OpenPanel(CarePanel.Schedule); }
        private void Close() { if (Visible) flow.ClosePanel(); }
        private void Select(string id)
        {
            if (!Visible || !lessons.ContainsKey(id)) return;
            selected = id; focusedSynergy = null;
            if (lessons[id].UnlockMonth <= care.Month)
            {
                for (int week = 0; week < care.Plan.Count; week++)
                    if (string.IsNullOrEmpty(care.Plan[week]))
                    {
                        if (actions.SetLesson(week, id)) return;
                        break;
                    }
            }
            ShowSelection(); ShowCatalog(); ShowWeeks();
        }
        private void Clear(int week)
        {
            if (Visible && week >= 0 && week < care.Plan.Count && !string.IsNullOrEmpty(care.Plan[week])) actions.ClearLesson(week);
        }
        private void FocusSynergy(string id)
        {
            if (!Visible || preview == null || !preview.Synergies.Contains(id)) return;
            focusedSynergy = focusedSynergy == id ? null : id;
            ShowCatalog(); ShowWeeks();
        }
        private void RefreshPanel()
        {
            if (openButton != null) openButton.interactable = view != null && flow.Phase == CareFlowPhase.Free;
            if (view == null || wasVisible == Visible) return;
            wasVisible = Visible; view.SetVisible(Visible);
            if (Visible) RefreshData();
        }
        private void RefreshMoney()
        {
            if (Visible && (shownMoney != care.Money || shownDiscount != work.Upgrades.TuitionDiscount
                || shownGrowth != work.Upgrades.CareGrowthBonus)) RefreshData();
        }
        private void RefreshData()
        {
            if (!Visible || view == null) return;
            if (shownMonth != care.Month) { shownMonth = care.Month; selected = null; focusedSynergy = null; }
            shownMoney = care.Money; shownDiscount = work.Upgrades.TuitionDiscount;
            shownGrowth = work.Upgrades.CareGrowthBonus;
            preview = care.Preview();
            if (focusedSynergy != null && !preview.Synergies.Contains(focusedSynergy)) focusedSynergy = null;
            view.ShowTotals(shownMoney, care.Plan.Count(id => !string.IsNullOrEmpty(id)), preview.Cost,
                preview.Cost <= shownMoney, preview.Synergies.Count);
            ShowSelection(); ShowCatalog(); ShowWeeks(); ShowSynergies(); ShowStats();
        }
        private void ShowStats()
        {
            int max = (int)Math.Min(int.MaxValue, care.StatMax * 3L);
            foreach (var row in view.Stats)
            {
                if (row == null || !CareStats.IsMain(row.Stat)) continue;
                int current = care.Run.GetStat(row.Stat);
                int next = (int)Math.Min(max, (long)current + preview.GetGain(row.Stat));
                row.Show(current, next, grades);
            }
        }
        private void ShowSelection()
        {
            if (selected == null || !lessons.TryGetValue(selected, out var lesson))
            {
                view.ShowSelection("수업을 선택하세요", "수업을 누르면 첫 빈 주차에 편성돼요. 아래 주차를 누르면 해당 수업을 뺄 수 있어요.", null, "", "");
                return;
            }
            art.TryGetValue(selected, out var asset);
            string description = asset != null ? asset.Description : selected == CareSystem.HomeId
                ? "집에서 조금씩 복습해요. 집중력·지구력·공감력·협동심이 조금 오릅니다." : "이번 주는 쉬어 가요. 수업료는 들지 않습니다.";
            string cost = lesson.UnlockMonth > care.Month ? "아직 잠긴 수업"
                : lesson.Cost == 0 ? "무료" : "수업료  " + care.GetLessonCost(selected).ToString("N0");
            view.ShowSelection(lesson.Name, description, GetIcon(selected), FormatGains(lesson.Gains), cost);
        }
        private void ShowCatalog()
        {
            var partners = new HashSet<string>();
            int colorIndex = -1;
            for (int i = 0; i < synergies.Length; i++)
            {
                var synergy = synergies[i];
                bool match = focusedSynergy != null ? synergy.Id == focusedSynergy
                    : selected != null && synergy.Lessons.Contains(selected);
                if (!match || synergy.Lessons.Any(id => lessons[id].UnlockMonth > care.Month)) continue;
                foreach (string id in synergy.Lessons) partners.Add(id);
                colorIndex = i;
            }
            var color = focusedSynergy != null && colorIndex >= 0 ? view.GetSynergyColor(colorIndex) : view.CandidateColor;
            foreach (var node in view.Lessons)
                if (node != null && lessons.TryGetValue(node.LessonId, out var lesson))
                {
                    node.Show(lesson.UnlockMonth <= care.Month, node.LessonId == selected, partners.Contains(node.LessonId), color);
                    if (lesson.UnlockMonth <= care.Month && shownUnlocks.Add(node.LessonId)) node.PlayUnlock();
                }
        }
        private List<int> FindWeeks(CareSynergy synergy)
        {
            var needs = synergy.Lessons.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
            var result = new List<int>();
            for (int i = 0; i < care.Plan.Count; i++)
            {
                string id = care.Plan[i];
                if (id == null || !needs.TryGetValue(id, out int count) || count == 0) continue;
                needs[id]--; result.Add(i);
            }
            return result;
        }
        private void ShowWeeks()
        {
            if (preview == null) return;
            var marks = new string[CareSystem.SlotCount];
            var focused = new HashSet<int>();
            var weekColors = new UnityEngine.Color[CareSystem.SlotCount];
            for (int i = 0; i < preview.Synergies.Count; i++)
            {
                int definition = Array.FindIndex(synergies, x => x.Id == preview.Synergies[i]);
                if (definition < 0) continue;
                var color = view.GetSynergyColor(definition);
                string tag = "<color=#" + UnityEngine.ColorUtility.ToHtmlStringRGB(color) + ">+</color> ";
                foreach (int week in FindWeeks(synergies[definition]))
                {
                    marks[week] += tag;
                    if (focusedSynergy == null || synergies[definition].Id == focusedSynergy)
                    {
                        if (focused.Add(week)) weekColors[week] = color;
                    }
                }
            }
            foreach (var week in view.Weeks)
            {
                if (week == null || week.Week < 0 || week.Week >= care.Plan.Count) continue;
                string id = care.Plan[week.Week];
                CareLesson lesson = id != null && lessons.TryGetValue(id, out var found) ? found : null;
                CareLessonSO asset = id != null && art.TryGetValue(id, out var foundArt) ? foundArt : null;
                week.Show(lesson != null ? asset != null ? asset.DisplayName : lesson.Name : null,
                    GetIcon(id), care.GetLessonCost(id), marks[week.Week], focused.Contains(week.Week), weekColors[week.Week]);
            }
        }
        private void ShowSynergies()
        {
            for (int i = 0; i < view.Synergies.Count; i++)
            {
                var row = view.Synergies[i]; if (row == null) continue;
                bool visible = i < preview.Synergies.Count;
                WorkUi.Show(row.gameObject, visible);
                if (!visible) continue;
                int definition = Array.FindIndex(synergies, x => x.Id == preview.Synergies[i]);
                var synergy = synergies[definition];
                string classes = string.Join(" + ", FindWeeks(synergy).Select(index => "[" + (index + 1) + "] " + GetLessonName(care.Plan[index])));
                string gains = string.Join(" · ", synergy.Gains.Where(gain => gain.Amount > 0)
                    .Select(gain => CareStatNames.GetName(gain.Stat) + "(" + CareStatNames.GetName(CareStats.GetMain(gain.Stat)) + ") +" + gain.Amount));
                row.Show(synergy.Id, classes + " | <b>" + synergy.Name + "</b> | " + gains, view.GetSynergyColor(definition));
            }
        }
        private string GetLessonName(string id) => art.TryGetValue(id, out var asset) ? asset.DisplayName : lessons[id].Name;
        private static string FormatGains(IEnumerable<CareGain> gains)
        {
            string text = string.Join(" · ", gains.Where(x => x.Amount > 0).Select(x => CareStatNames.GetName(x.Stat) + " +" + x.Amount));
            return string.IsNullOrEmpty(text) ? "—" : text;
        }
        public void Disconnect()
        {
            if (!connected) return;
            connected = false;
            care.Changed -= RefreshData; flow.Changed -= RefreshPanel; work.Changed -= RefreshMoney;
            if (openButton != null) { openButton.onClick.RemoveListener(Open); openButton.interactable = false; }
            if (view != null)
            {
                view.CloseClicked -= Close; view.LessonClicked -= Select;
                foreach (var week in view.Weeks) if (week != null) week.Clicked -= Clear;
                foreach (var row in view.Synergies) if (row != null) row.Clicked -= FocusSynergy;
                view.SetVisible(false);
            }
            wasVisible = false;
        }
        public void Dispose() => Disconnect();
        private UnityEngine.Sprite GetIcon(string id)
        {
            if (id == CareSystem.HomeId) return config.HomeIcon;
            if (id == CareSystem.RestId) return config.RestIcon;
            return id != null && art.TryGetValue(id, out var asset) ? asset.Icon : null;
        }
    }
}
