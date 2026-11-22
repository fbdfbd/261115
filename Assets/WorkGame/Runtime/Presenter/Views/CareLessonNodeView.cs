using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareLessonNodeView : MonoBehaviour
    {
        [SerializeField] private CareLessonSO lesson;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text nameText;
        [SerializeField, HideInInspector] private TMP_Text lockedText; // 기존 프리팹의 월 표시 제거용
        [SerializeField] private CanvasGroup lockGroup;
        [SerializeField] private Image lockImage;
        [SerializeField] private GameObject lockPlaceholder;
        [SerializeField] private RectTransform unlockTarget;
        [SerializeField, Min(0.01f)] private float unlockSeconds = 0.35f;
        [SerializeField, Min(1)] private float unlockScale = 1.05f;
        [SerializeField] private Image icon;
        [SerializeField] private Image background;
        [SerializeField] private CareScheduleHighlight highlight;
        [SerializeField] private Color normalColor = new Color(0.95f, 0.95f, 0.92f);
        [SerializeField] private Color selectedColor = new Color(0.78f, 0.85f, 0.94f);
        public string LessonId => lesson != null ? lesson.Id : "";
        public event Action<string> Clicked;
        private Sequence unlockTween;
        private Vector3 baseScale;
        private bool isUnlocked;
        private void OnEnable() { if (button != null) button.onClick.AddListener(Click); }
        private void OnDisable() { if (button != null) button.onClick.RemoveListener(Click); ClearHint(); }
        private void Click() { if (isUnlocked) Clicked?.Invoke(LessonId); }
        public void Show(bool unlocked, bool selected, bool partner, Color color)
        {
            if (lesson == null) return;
            isUnlocked = unlocked;
            if (button != null) button.interactable = unlocked;
            if (lockImage != null) lockImage.enabled = lockImage.sprite != null;
            if (lockPlaceholder != null) WorkUi.Show(lockPlaceholder, lockImage == null || lockImage.sprite == null);
            if (nameText != null && nameText.text != lesson.DisplayName) nameText.text = lesson.DisplayName;
            if (icon != null) { icon.sprite = lesson.Icon; icon.enabled = lesson.Icon != null; }
            if (lockedText != null) WorkUi.Show(lockedText.gameObject, false);
            if (lockGroup != null && unlockTween == null) lockGroup.alpha = unlocked ? 0 : 1;
            if (background != null) background.color = selected ? selectedColor : normalColor;
            if (highlight != null) highlight.Show(unlocked && partner, color);
        }
        public void PlayUnlock()
        {
            StopUnlock();
            var target = unlockTarget != null ? unlockTarget : (RectTransform)transform;
            baseScale = target.localScale;
            if (lockGroup != null) lockGroup.alpha = 1;
            unlockTween = DOTween.Sequence().SetUpdate(true);
            if (lockGroup != null)
                unlockTween.Insert(0, DOTween.To(() => lockGroup.alpha, value => lockGroup.alpha = value, 0, unlockSeconds));
            unlockTween.Insert(0, target.DOScale(baseScale * unlockScale, unlockSeconds * 0.45f).SetEase(Ease.OutSine));
            unlockTween.Insert(unlockSeconds * 0.45f, target.DOScale(baseScale, unlockSeconds * 0.55f).SetEase(Ease.InOutSine));
            unlockTween.OnComplete(() => unlockTween = null);
        }
        private void StopUnlock()
        {
            if (unlockTween == null) return;
            unlockTween.Kill(); unlockTween = null;
            var target = unlockTarget != null ? unlockTarget : (RectTransform)transform;
            target.localScale = baseScale;
        }
        public void ClearHint()
        {
            if (highlight != null) highlight.Clear();
            StopUnlock();
            if (lockGroup != null) lockGroup.alpha = isUnlocked ? 0 : 1;
        }
    }
}
