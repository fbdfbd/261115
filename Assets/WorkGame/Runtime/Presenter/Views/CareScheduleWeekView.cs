using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareScheduleWeekView : MonoBehaviour
    {
        [SerializeField, Range(0, 3)] private int week;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text synergyText;
        [SerializeField] private Image icon;
        [SerializeField] private CareScheduleHighlight highlight;
        public int Week => week;
        public event Action<int> Clicked;
        private void OnEnable()
        {
            if (button != null) button.onClick.AddListener(Click);
        }
        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(Click);
            ClearHint();
        }
        private void Click() => Clicked?.Invoke(week);
        public void Show(string name, Sprite sprite, int cost, string links, bool linked, Color color)
        {
            bool filled = !string.IsNullOrEmpty(name);
            if (nameText != null) nameText.text = filled ? name : "";
            if (costText != null) costText.text = filled ? cost == 0 ? "무료" : cost.ToString("N0") : "";
            if (icon != null) { icon.sprite = sprite; icon.enabled = sprite != null; }
            if (button != null) button.interactable = filled;
            if (synergyText != null) synergyText.text = links;
            if (highlight != null) highlight.Show(linked, color);
        }
        public void ClearHint() { if (highlight != null) highlight.Clear(); }
    }
}
