using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareDialogueView : MonoBehaviour, ICanvasRaycastFilter
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private CareDialogueClickArea clickArea;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private GameObject continueHint;
        [SerializeField] private Button logButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private List<Button> choiceButtons = new List<Button>();
        [SerializeField] private List<TMP_Text> choiceTexts = new List<TMP_Text>();
        [SerializeField] private RectTransform choicesRoot;
        [Header("로그 — 이 프리팹 내부")]
        [SerializeField] private CanvasGroup logGroup;
        [SerializeField] private TMP_Text logText;
        [SerializeField] private RectTransform logContent;
        [SerializeField] private RectTransform logViewport;
        [SerializeField] private ScrollRect logScroll;
        [SerializeField] private List<Button> logCloseButtons = new List<Button>();
        [Header("정지·나가기 등 클릭을 통과시킬 영역")]
        [SerializeField] private List<RectTransform> ignoredClickAreas = new List<RectTransform>();
        [Header("대사 이미지 변경 — 빈 ImageKey는 유지")]
        [SerializeField] private UnityEvent<string> imageKeyChanged = new UnityEvent<string>();
        private readonly List<UnityAction> choiceActions = new List<UnityAction>();
        private bool visible, hasChoices, canSkip;
        private string lastImageKey;
        private float choiceStep;
        private int choiceCount = -1;
        public bool LogOpen { get; private set; }
        public bool InputBlocked { get; private set; }
        public event Action NextRequested;
        public event Action<int> ChoiceRequested;
        public event Action SkipRequested;
        public event Action LogRequested;

        private void Awake() => Hide();
        private void OnEnable()
        {
            if (clickArea != null) clickArea.Clicked += Next;
            if (logButton != null) logButton.onClick.AddListener(OpenLog);
            if (skipButton != null) skipButton.onClick.AddListener(Skip);
            foreach (var button in logCloseButtons) if (button != null) button.onClick.AddListener(CloseLog);
            choiceActions.Clear();
            for (int i = 0; i < choiceButtons.Count; i++)
            {
                int index = i;
                UnityAction action = () => Choose(index);
                choiceActions.Add(action);
                if (choiceButtons[i] != null) choiceButtons[i].onClick.AddListener(action);
            }
        }
        private void OnDisable()
        {
            if (clickArea != null) clickArea.Clicked -= Next;
            if (logButton != null) logButton.onClick.RemoveListener(OpenLog);
            if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
            foreach (var button in logCloseButtons) if (button != null) button.onClick.RemoveListener(CloseLog);
            for (int i = 0; i < choiceActions.Count; i++)
                if (choiceButtons[i] != null) choiceButtons[i].onClick.RemoveListener(choiceActions[i]);
            choiceActions.Clear();
            Hide();
        }

        public void Show(CareDialogueData dialogue, int lineIndex, bool allowSkip)
        {
            if (dialogue == null) { Hide(); return; }
            visible = true; canSkip = allowSkip;
            WorkUi.Show(panelGroup, true);
            var line = dialogue.Lines[lineIndex];
            if (speakerText != null) speakerText.text = string.IsNullOrEmpty(line.Speaker) ? "달이" : line.Speaker;
            if (bodyText != null && bodyText.text != line.Text) bodyText.text = line.Text;
            hasChoices = lineIndex == dialogue.Lines.Length - 1 && dialogue.Choices.Length > 0;
            ArrangeChoices(hasChoices ? Math.Min(dialogue.Choices.Length, choiceButtons.Count) : 0);
            for (int i = 0; i < choiceButtons.Count; i++)
            {
                bool show = hasChoices && i < dialogue.Choices.Length;
                if (choiceButtons[i] != null) WorkUi.Show(choiceButtons[i].gameObject, show);
                if (show && i < choiceTexts.Count && choiceTexts[i] != null) choiceTexts[i].text = dialogue.Choices[i].Text;
            }
            if (continueHint != null) WorkUi.Show(continueHint, !hasChoices);
            UpdateInput();
            if (!string.IsNullOrEmpty(line.ImageKey) && lastImageKey != line.ImageKey)
            {
                lastImageKey = line.ImageKey;
                imageKeyChanged.Invoke(line.ImageKey);
            }
        }

        public void Hide()
        {
            visible = false; LogOpen = false; lastImageKey = null;
            WorkUi.Show(logGroup, false); WorkUi.Show(panelGroup, false);
        }
        private void ArrangeChoices(int count)
        {
            if (count == 0 || count == choiceCount) return;
            if (choicesRoot == null && choiceButtons.Count > 0 && choiceButtons[0] != null)
                choicesRoot = choiceButtons[0].transform.parent as RectTransform;
            if (choicesRoot == null) return;
            if (choiceStep <= 0) choiceStep = choicesRoot.rect.height / choiceButtons.Count;
            choicesRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, choiceStep * count);
            for (int i = 0; i < count; i++)
            {
                if (choiceButtons[i] == null) continue;
                var rect = (RectTransform)choiceButtons[i].transform;
                rect.anchorMin = new Vector2(rect.anchorMin.x, 1 - (i + 1f) / count);
                rect.anchorMax = new Vector2(rect.anchorMax.x, 1 - i / (float)count);
            }
            choiceCount = count;
        }
        public void SetInputBlocked(bool blocked) { InputBlocked = blocked; UpdateInput(); }
        private void UpdateInput()
        {
            bool allowed = visible && !InputBlocked && !LogOpen;
            if (skipButton != null) skipButton.interactable = allowed && canSkip;
            if (logButton != null) logButton.interactable = visible && !InputBlocked && !LogOpen;
            foreach (var button in choiceButtons) if (button != null) button.interactable = allowed;
        }
        private void Next() { if (visible && !InputBlocked && !LogOpen && !hasChoices) NextRequested?.Invoke(); }
        private void Choose(int index) { if (visible && !InputBlocked && !LogOpen && hasChoices) ChoiceRequested?.Invoke(index); }
        private void Skip() { if (visible && !InputBlocked && !LogOpen && canSkip) SkipRequested?.Invoke(); }
        private void OpenLog()
        {
            if (!visible || InputBlocked) return;
            LogOpen = true; WorkUi.Show(logGroup, true); UpdateInput(); LogRequested?.Invoke();
        }
        private void CloseLog() { LogOpen = false; WorkUi.Show(logGroup, false); UpdateInput(); }
        public void ShowLog(string text)
        {
            if (logText == null || logContent == null || logViewport == null) return;
            if (logText.text != text) logText.text = text;
            float width = Mathf.Max(1, logViewport.rect.width - 32);
            float height = Mathf.Max(logViewport.rect.height, logText.GetPreferredValues(text, width, Mathf.Infinity).y + 32);
            logContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (logScroll != null) { logScroll.StopMovement(); logScroll.verticalNormalizedPosition = 0; }
        }

        public bool IsRaycastLocationValid(Vector2 point, Camera camera)
        {
            foreach (var area in ignoredClickAreas)
                if (area != null && area.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(area, point, camera))
                    return false;
            return true;
        }
    }
}
