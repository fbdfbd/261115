using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareScheduleView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private List<Button> closeButtons = new List<Button>();
        [SerializeField] private Button homeButton, restButton;
        [SerializeField] private List<CareLessonNodeView> lessons = new List<CareLessonNodeView>();
        [SerializeField] private List<CareScheduleWeekView> weeks = new List<CareScheduleWeekView>();
        [SerializeField] private List<CareScheduleSynergyView> synergies = new List<CareScheduleSynergyView>();
        [SerializeField] private List<CareScheduleStatView> stats = new List<CareScheduleStatView>();
        [SerializeField] private Image selectedIcon;
        [SerializeField] private TMP_Text selectedTitle, selectedDescription, selectedGains, selectedCost;
        [SerializeField] private TMP_Text moneyText, planText, costText;
        [SerializeField] private TMP_Text helpText;
        [SerializeField] private GameObject noSynergy;
        [SerializeField] private Color candidateColor = new Color(0.88f, 0.62f, 0.22f);
        [SerializeField] private Color[] synergyColors =
        {
            new Color(0.24f, 0.48f, 0.78f), new Color(0.7f, 0.34f, 0.57f),
            new Color(0.27f, 0.59f, 0.39f), new Color(0.72f, 0.49f, 0.2f),
            new Color(0.5f, 0.38f, 0.72f), new Color(0.18f, 0.58f, 0.62f),
            new Color(0.72f, 0.36f, 0.29f), new Color(0.46f, 0.55f, 0.24f)
        };
        public Color CandidateColor => candidateColor;
        public Color GetSynergyColor(int index) => synergyColors != null && synergyColors.Length > 0
            ? synergyColors[index % synergyColors.Length] : candidateColor;
        public IReadOnlyList<CareLessonNodeView> Lessons => lessons;
        public IReadOnlyList<CareScheduleWeekView> Weeks => weeks;
        public IReadOnlyList<CareScheduleSynergyView> Synergies => synergies;
        public IReadOnlyList<CareScheduleStatView> Stats => stats;
        public event Action CloseClicked;
        public event Action<string> LessonClicked;
        private void Awake() => SetVisible(false);
        private void OnEnable()
        {
            foreach (var button in closeButtons) if (button != null) button.onClick.AddListener(Close);
            foreach (var node in lessons) if (node != null) node.Clicked += Select;
            if (homeButton != null) homeButton.onClick.AddListener(Home);
            if (restButton != null) restButton.onClick.AddListener(Rest);
        }
        private void OnDisable()
        {
            foreach (var button in closeButtons) if (button != null) button.onClick.RemoveListener(Close);
            foreach (var node in lessons) if (node != null) node.Clicked -= Select;
            if (homeButton != null) homeButton.onClick.RemoveListener(Home);
            if (restButton != null) restButton.onClick.RemoveListener(Rest);
            ClearHints();
        }
        private void Close() => CloseClicked?.Invoke();
        private void Select(string id) => LessonClicked?.Invoke(id);
        private void Home() => Select(CareSystem.HomeId);
        private void Rest() => Select(CareSystem.RestId);
        public void SetVisible(bool visible)
        {
            if (!visible) ClearHints();
            WorkUi.Show(panelGroup, visible);
        }
        private void ClearHints()
        {
            foreach (var node in lessons) if (node != null) node.ClearHint();
            foreach (var week in weeks) if (week != null) week.ClearHint();
            foreach (var stat in stats) if (stat != null) stat.StopPreview();
        }
        public void ShowSelection(string title, string description, Sprite sprite, string gains, string cost)
        {
            if (selectedTitle != null) selectedTitle.text = title;
            if (selectedDescription != null) selectedDescription.text = description;
            if (selectedIcon != null) { selectedIcon.sprite = sprite; selectedIcon.enabled = sprite != null; }
            if (selectedGains != null) selectedGains.text = gains;
            if (selectedCost != null) selectedCost.text = cost;
        }
        public void ShowTotals(long money, int count, long cost, bool affordable, int synergyCount)
        {
            if (moneyText != null) moneyText.text = "보유 현금  " + money.ToString("N0");
            if (planText != null) planText.text = "편성  " + count + " / 4";
            if (helpText != null) helpText.text = count >= CareSystem.SlotCount
                ? "편성이 가득 찼어요. 아래 주차를 눌러 먼저 비워 주세요."
                : "수업을 누르면 첫 빈 주차에 편성됩니다. 아래 주차를 누르면 해제됩니다.";
            if (costText != null)
            {
                costText.text = "수업료  " + cost.ToString("N0");
                costText.color = affordable ? new Color(0.17f, 0.21f, 0.19f) : new Color(0.75f, 0.25f, 0.23f);
            }
            if (noSynergy != null) WorkUi.Show(noSynergy, synergyCount == 0);
        }
    }
}
