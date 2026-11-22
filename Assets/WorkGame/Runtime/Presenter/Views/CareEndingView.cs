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
        [Header("글자 표시·이미지 연출")]
        [SerializeField, Min(0.001f)] private float characterSeconds = 0.035f;
        [SerializeField, Min(0)] private float imageSeconds = 0.3f;
        [Header("마지막 페이지 완료 / 타이틀 버튼")]
        [SerializeField] private UnityEvent onCompleted = new UnityEvent();
        [SerializeField] private UnityEvent onTitleRequested = new UnityEvent();
        private Tween writing;
        private Tween imageTween;
        private int lineCount, characterCount, visibleCharacters, nextLine;
        private int scrollLine = -1;
        private int[] lineEnds = Array.Empty<int>();
        private float[] lineHeights = Array.Empty<float>();
        private bool paused, visible, complete;
        public bool InputBlocked { get; private set; }
        public bool IsWriting => writing != null;
        public bool HasNextLine => nextLine < lineCount;
        public event Action NextRequested;
        public event Action Completed;
        public event Action TitleRequested;

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
        private void GoToTitle()
        {
            if (!visible || !complete || paused || InputBlocked) return;
            TitleRequested?.Invoke();
            onTitleRequested.Invoke();
        }
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
            letterText.textWrappingMode = TextWrappingModes.Normal;
            letterText.overflowMode = TextOverflowModes.Overflow;
            letterText.text = text ?? "";
            letterText.maxVisibleLines = int.MaxValue;
            letterText.maxVisibleCharacters = int.MaxValue;
            Canvas.ForceUpdateCanvases();
            letterText.ForceMeshUpdate();
            letterText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, letterText.preferredHeight);
            letterText.ForceMeshUpdate();
            lineCount = string.IsNullOrEmpty(text) ? 0 : letterText.textInfo.lineCount;
            characterCount = letterText.textInfo.characterCount;
            if (lineEnds.Length < lineCount)
            {
                Array.Resize(ref lineEnds, lineCount); Array.Resize(ref lineHeights, lineCount);
            }
            for (int i = 0; i < lineCount; i++)
            {
                var line = letterText.textInfo.lineInfo[i];
                lineEnds[i] = line.lastCharacterIndex + 1;
                lineHeights[i] = line.visibleCharacterCount > 0
                    ? letterText.textInfo.lineInfo[0].ascender - line.descender : i > 0 ? lineHeights[i - 1] : 0;
            }
            visibleCharacters = 0; nextLine = 0; scrollLine = -1;
            letterText.maxVisibleCharacters = 0;
            if (letterScroll != null) letterScroll.verticalNormalizedPosition = 1;
            if (characterCount == 0) { UpdateHint(false); return; }
            ShowNextLine();
        }
        public void ShowNextLine()
        {
            if (writing != null || !HasNextLine || letterText == null) return;
            int end = Mathf.Min(characterCount, lineEnds[nextLine++]);
            float duration = (end - visibleCharacters) * characterSeconds;
            UpdateHint(true);
            writing = DOTween.To(() => visibleCharacters, ShowCharacters, end, duration)
                .SetEase(Ease.Linear).SetUpdate(true).OnComplete(() =>
            {
                writing = null; UpdateHint(false);
            });
            if (paused) writing.Pause();
        }
        private void ShowCharacters(int count)
        {
            if (visibleCharacters == count || letterText == null) return;
            visibleCharacters = count; letterText.maxVisibleCharacters = count;
            int line = Mathf.Max(0, scrollLine);
            while (line + 1 < lineCount && count > lineEnds[line]) line++;
            if (line == scrollLine) return;
            scrollLine = line; ScrollToLine(line);
        }
        private void ScrollToLine(int line)
        {
            if (letterScroll == null || letterScroll.viewport == null || lineCount == 0) return;
            float total = letterText.rectTransform.rect.height - letterScroll.viewport.rect.height;
            if (total <= 0) return;
            letterScroll.verticalNormalizedPosition = 1 - Mathf.Clamp01((lineHeights[line] - letterScroll.viewport.rect.height + 12) / total);
        }
        public void FinishLine()
        {
            writing?.Complete();
        }
        public void FinishWriting()
        {
            StopWriting();
            nextLine = lineCount;
            if (letterText != null)
            {
                ShowCharacters(characterCount);
                letterText.maxVisibleCharacters = int.MaxValue;
                letterText.maxVisibleLines = int.MaxValue;
            }
            UpdateHint(false);
        }
        private void UpdateHint(bool writingNow)
        {
            if (hintText != null) hintText.text = writingNow ? "화면을 누르면 현재 줄을 모두 표시합니다" : "화면을 눌러 계속";
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
            if (notify) { Completed?.Invoke(); onCompleted.Invoke(); }
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
