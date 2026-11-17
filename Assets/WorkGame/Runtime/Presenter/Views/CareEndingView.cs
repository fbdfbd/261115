using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareEndingView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private CareDialogueClickArea clickArea;
        [SerializeField] private Image picture;
        [SerializeField] private CanvasGroup pictureGroup;
        [SerializeField] private TMP_Text titleText, letterText, pageText, hintText;
        [SerializeField] private ScrollRect letterScroll;
        [SerializeField] private Button titleButton;
        [Header("줄 표시·이미지 연출")]
        [SerializeField, Min(0.01f)] private float lineSeconds = 0.3f;
        [SerializeField, Min(0)] private float imageSeconds = 0.3f;
        [Header("마지막 페이지 완료 / 타이틀 버튼")]
        [SerializeField] private UnityEvent onCompleted = new UnityEvent();
        [SerializeField] private UnityEvent onTitleRequested = new UnityEvent();
        private Sequence writing;
        private Tween imageTween;
        private int lineCount;
        private bool paused, visible, complete;
        public bool InputBlocked { get; private set; }
        public bool IsWriting => writing != null;
        public event Action NextRequested;

        private void Awake() => Hide();
        private void OnEnable()
        {
            if (clickArea != null) clickArea.Clicked += Next;
            if (titleButton != null) titleButton.onClick.AddListener(GoToTitle);
        }
        private void OnDisable()
        {
            if (clickArea != null) clickArea.Clicked -= Next;
            if (titleButton != null) titleButton.onClick.RemoveListener(GoToTitle);
            Hide();
        }
        private void Next() { if (visible && !paused && !InputBlocked && !complete) NextRequested?.Invoke(); }
        private void GoToTitle() { if (visible && complete && !paused && !InputBlocked) onTitleRequested.Invoke(); }
        public void Open(string title)
        {
            visible = true; complete = false;
            WorkUi.Show(panelGroup, true);
            if (titleText != null) titleText.text = title;
            if (titleButton != null) WorkUi.Show(titleButton.gameObject, false);
            if (hintText != null) WorkUi.Show(hintText.gameObject, true);
        }
        public void ShowPage(Sprite sprite, string text, int number, int count)
        {
            StopWriting();
            if (pageText != null) pageText.text = number + " / " + count;
            ShowImage(sprite);
            if (letterText == null) return;
            letterText.text = text ?? "";
            letterText.maxVisibleLines = int.MaxValue;
            Canvas.ForceUpdateCanvases();
            letterText.ForceMeshUpdate();
            letterText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, letterText.preferredHeight);
            letterText.ForceMeshUpdate();
            lineCount = string.IsNullOrEmpty(text) ? 0 : letterText.textInfo.lineCount;
            letterText.maxVisibleLines = 0;
            if (letterScroll != null) letterScroll.verticalNormalizedPosition = 1;
            if (lineCount == 0) { UpdateHint(false); return; }
            UpdateHint(true);
            writing = DOTween.Sequence().SetUpdate(true);
            for (int i = 1; i <= lineCount; i++)
            {
                int line = i;
                writing.AppendInterval(lineSeconds).AppendCallback(() => RevealLine(line));
            }
            writing.OnComplete(() => { writing = null; UpdateHint(false); });
            if (paused) writing.Pause();
        }
        private void RevealLine(int count)
        {
            letterText.maxVisibleLines = count;
            if (letterScroll == null || letterScroll.viewport == null || count == 0) return;
            float total = letterText.rectTransform.rect.height - letterScroll.viewport.rect.height;
            if (total <= 0) return;
            var lines = letterText.textInfo.lineInfo;
            float shownHeight = lines[0].ascender - lines[count - 1].descender;
            letterScroll.verticalNormalizedPosition = 1 - Mathf.Clamp01((shownHeight - letterScroll.viewport.rect.height + 12) / total);
        }
        public void FinishWriting()
        {
            StopWriting();
            if (letterText != null) RevealLine(lineCount);
            UpdateHint(false);
        }
        private void UpdateHint(bool writingNow)
        {
            if (hintText != null) hintText.text = writingNow ? "화면을 누르면 편지를 모두 표시합니다" : "화면을 눌러 계속";
        }
        private void ShowImage(Sprite sprite)
        {
            if (sprite == null || picture == null || picture.sprite == sprite) return;
            imageTween?.Kill();
            picture.sprite = sprite; picture.enabled = true;
            if (pictureGroup == null) return;
            pictureGroup.alpha = 0;
            imageTween = DOTween.To(() => pictureGroup.alpha, value => pictureGroup.alpha = value, 1, imageSeconds).SetUpdate(true);
            if (paused) imageTween.Pause();
        }
        public void ClearImage()
        {
            imageTween?.Kill(); imageTween = null;
            if (picture != null) { picture.sprite = null; picture.enabled = false; }
            if (pictureGroup != null) pictureGroup.alpha = 1;
        }
        public void ShowComplete(bool notify = true)
        {
            FinishWriting(); complete = true;
            if (hintText != null) WorkUi.Show(hintText.gameObject, false);
            if (titleButton != null) WorkUi.Show(titleButton.gameObject, true);
            if (notify) onCompleted.Invoke();
        }
        public void SetInputBlocked(bool blocked)
        {
            InputBlocked = blocked;
            if (titleButton != null) titleButton.interactable = !blocked && !paused;
        }
        public void SetPaused(bool value)
        {
            paused = value;
            if (value) { writing?.Pause(); imageTween?.Pause(); }
            else { writing?.Play(); imageTween?.Play(); }
            SetInputBlocked(InputBlocked);
        }
        private void StopWriting() { writing?.Kill(); writing = null; }
        public void Hide()
        {
            StopWriting(); imageTween?.Kill(); imageTween = null;
            if (pictureGroup != null) pictureGroup.alpha = 1;
            visible = false;
            WorkUi.Show(panelGroup, false);
        }
    }
}
