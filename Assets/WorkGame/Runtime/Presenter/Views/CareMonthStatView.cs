using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareMonthStatView : MonoBehaviour
    {
        [SerializeField] private CareStat stat;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text nameText, valueText, gradeText;
        [SerializeField] private Slider slider;
        [SerializeField] private Color upColor = new Color(0.2f, 0.55f, 0.33f);
        [SerializeField] private Color downColor = new Color(0.75f, 0.25f, 0.23f);
        [SerializeField] private Color normalColor = new Color(0.17f, 0.21f, 0.19f);
        private Vector3 baseScale;
        private bool hasScale;
        private int before, after, delta;
        private int lastValue = int.MinValue;
        private string changeColor;
        private CareGradeSettings grades;
        private CareGrade startGrade, shownGrade = (CareGrade)(-1);
        public CareStat Stat => stat;
        public bool GainedGrade => grades != null && after > before && grades.GetGrade(after) > startGrade;

        public void Prepare(int before, int after, int change, CareGradeSettings grades)
        {
            if (!hasScale) { baseScale = transform.localScale; hasScale = true; }
            this.before = before; this.after = after; delta = change;
            this.grades = grades; startGrade = grades.GetGrade(before);
            shownGrade = (CareGrade)(-1);
            lastValue = int.MinValue;
            changeColor = ColorUtility.ToHtmlStringRGB(delta >= 0 ? upColor : downColor);
            if (nameText != null) nameText.text = CareStatNames.GetName(stat);
            if (slider != null)
            {
                slider.minValue = 0; slider.maxValue = 1;
                slider.wholeNumbers = false; slider.interactable = false;
            }
            ShowValue(before);
            if (group != null) group.alpha = 0;
            transform.localScale = baseScale * 0.94f;
        }
        public Sequence CreateAnimation(float seconds)
        {
            var sequence = DOTween.Sequence();
            if (group != null) sequence.Join(DOTween.To(() => group.alpha, value => group.alpha = value, 1, seconds));
            sequence.Join(transform.DOScale(baseScale, seconds).SetEase(Ease.OutSine));
            float shown = before;
            sequence.Join(DOTween.To(() => shown, value =>
            {
                shown = value;
                ShowBar(value);
                ShowValueText(Mathf.FloorToInt(value));
            }, after, seconds).SetEase(Ease.OutSine));
            return sequence;
        }
        public void ShowFinal()
        {
            ResetAnimation();
            ShowValue(after);
        }
        public void ResetAnimation()
        {
            if (hasScale) transform.localScale = baseScale;
            if (group != null) group.alpha = 1;
        }
        private void ShowValue(int value)
        {
            ShowBar(value);
            ShowValueText(value);
        }
        private void ShowBar(float value)
        {
            var progress = grades.GetProgress(value);
            if (slider != null) slider.SetValueWithoutNotify(progress.Ratio > 0 ? Mathf.Max(0.1f, (float)progress.Ratio) : 0);
            if (shownGrade == progress.Grade) return;
            shownGrade = progress.Grade;
            if (gradeText == null) return;
            gradeText.text = CareGradeNames.GetName(progress.Grade);
            gradeText.color = (int)progress.Grade > (int)startGrade ? upColor : normalColor;
        }
        private void ShowValueText(int value)
        {
            if (valueText == null || lastValue == value) return;
            lastValue = value;
            int change = value - before;
            string gain = change >= 0 ? "+" + change.ToString("N0") : change.ToString("N0");
            valueText.text = value.ToString("N0") + (delta == 0 ? "" : " (<color=#" + changeColor + ">" + gain + "</color>)");
        }
    }
}
