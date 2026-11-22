using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareScheduleStatView : MonoBehaviour
    {
        [SerializeField] private CareStat stat;
        [SerializeField] private Slider currentSlider, previewSlider;
        [SerializeField] private TMP_Text nameText, valueText, gradeText;
        [SerializeField] private Color gradeColor = new Color(0.17f, 0.21f, 0.19f);
        [SerializeField] private Color gradeUpColor = new Color(0.2f, 0.55f, 0.33f);
        [SerializeField, Min(1)] private float gradeUpScale = 1.15f;
        [SerializeField, Min(0.01f)] private float moveSeconds = 0.35f;
        private Tween fillTween, gradeTween;
        private int lastCurrent = -1, lastPreview = -1;
        private CareGrade shownGrade = (CareGrade)(-1);
        private bool shownGradeUp;
        private float shownPreview;
        private CareGradeSettings grades;
        public CareStat Stat => stat;

        public void Show(int current, int preview, CareGradeSettings grades)
        {
            if (nameText != null) nameText.text = CareStatNames.GetName(stat);
            if (current == lastCurrent && preview == lastPreview && this.grades == grades) return;

            bool first = lastCurrent < 0;
            fillTween?.Kill();
            this.grades = grades;
            lastCurrent = current; lastPreview = preview;
            SetRange(currentSlider); SetRange(previewSlider);
            if (valueText != null) valueText.text = current == preview ? current.ToString("N0")
                : current.ToString("N0") + " → " + preview.ToString("N0");
            if (first) shownPreview = current;
            ShowBars(shownPreview);
            fillTween = DOTween.To(() => shownPreview, ShowBars, preview, moveSeconds)
                .SetEase(Ease.OutSine).SetUpdate(true);
        }
        private void ShowBars(float value)
        {
            shownPreview = value;
            var current = grades.GetProgress(lastCurrent);
            var preview = grades.GetProgress(value);
            // 다른 등급의 예측 막대가 시작되면 현재 막대가 앞을 가리지 않게 합니다.
            if (currentSlider != null) currentSlider.SetValueWithoutNotify(current.Grade == preview.Grade && current.Ratio > 0 ? Mathf.Max(0.1f, (float)current.Ratio) : 0);
            if (previewSlider != null) previewSlider.SetValueWithoutNotify(preview.Ratio > 0 ? Mathf.Max(0.1f, (float)preview.Ratio) : 0);
            bool gradeUp = (int)preview.Grade > (int)current.Grade;
            if (shownGrade == preview.Grade && shownGradeUp == gradeUp) return;
            shownGrade = preview.Grade; shownGradeUp = gradeUp;
            if (gradeText == null) return;
            gradeText.text = CareGradeNames.GetName(preview.Grade);
            gradeText.color = gradeUp ? gradeUpColor : gradeColor;
            gradeTween?.Kill();
            gradeTween = gradeText.transform.DOScale(gradeUp ? gradeUpScale : 1, moveSeconds)
                .SetEase(Ease.OutSine).SetUpdate(true);
        }
        private static void SetRange(Slider slider)
        {
            if (slider == null) return;
            slider.minValue = 0; slider.maxValue = 1;
            slider.wholeNumbers = false; slider.interactable = false;
        }
        public void StopPreview()
        {
            fillTween?.Kill(); fillTween = null;
            gradeTween?.Kill(); gradeTween = null;
            if (gradeText != null) gradeText.transform.localScale = Vector3.one;
            lastCurrent = -1;
            shownGrade = (CareGrade)(-1);
        }
        private void OnDisable() => StopPreview();
    }
}
