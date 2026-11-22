using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareStatRowView : MonoBehaviour
    {
        [SerializeField] private CareStat stat;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text gradeText;
        [SerializeField] private Slider slider;
        [SerializeField, Min(0.01f)] private float moveSeconds = 0.35f;
        private int lastValue = -1, lastMax = -1;
        private CareGrade lastGrade = (CareGrade)(-1);
        private bool named;
        private CareGradeSettings grades;
        private Tween tween;
        private float shownValue;
        private int shownNumber = -1;
        private CareGrade shownGrade = (CareGrade)(-1);
        public CareStat Stat => stat;

        public void Show(int value, CareGrade grade, int max, CareGradeSettings grades)
        {
            if (!named && nameText != null) { nameText.text = CareStatNames.GetName(stat); named = true; }
            if (CareStats.IsMain(stat))
            {
                if (lastValue == value && this.grades == grades) return;
                bool first = lastValue < 0;
                this.grades = grades;
                tween?.Kill();
                if (slider != null)
                {
                    slider.minValue = 0; slider.maxValue = 1;
                    slider.wholeNumbers = false; slider.interactable = false;
                }
                if (first || !isActiveAndEnabled) ShowMain(value);
                else tween = DOTween.To(() => shownValue, ShowMain, value, moveSeconds).SetEase(Ease.OutSine).SetUpdate(true);
                lastValue = value;
                return;
            }
            if (lastValue != value && valueText != null) valueText.text = value.ToString("N0");
            if (lastGrade != grade && gradeText != null) gradeText.text = CareGradeNames.GetName(grade);
            if (slider != null && (lastValue != value || lastMax != max))
            {
                slider.minValue = 0; slider.maxValue = 1;
                slider.wholeNumbers = false; slider.interactable = false;
                float ratio = max > 0 ? (float)value / max : 0;
                slider.SetValueWithoutNotify(ratio > 0 ? Mathf.Max(0.1f, ratio) : 0);
            }
            lastValue = value; lastGrade = grade; lastMax = max;
        }
        private void ShowMain(float value)
        {
            shownValue = value;
            var progress = grades.GetProgress(value);
            if (slider != null) slider.SetValueWithoutNotify(progress.Ratio > 0 ? Mathf.Max(0.1f, (float)progress.Ratio) : 0);
            if (shownGrade != progress.Grade && gradeText != null) gradeText.text = CareGradeNames.GetName(progress.Grade);
            shownGrade = progress.Grade;
            int number = Mathf.FloorToInt(value);
            if (shownNumber != number && valueText != null) valueText.text = number.ToString("N0");
            shownNumber = number;
        }
        private void OnDisable()
        {
            tween?.Kill(); tween = null;
            lastValue = -1;
        }
    }
}
