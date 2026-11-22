using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareDialogueView : MonoBehaviour, ICanvasRaycastFilter, ICareSfxSource
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private CareDialogueClickArea clickArea;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private GameObject namePlate;
        [SerializeField] private TMP_Text bodyText;
        [Header("대사 글자 출력 — 클릭하면 현재 줄 즉시 표시")]
        [SerializeField, Min(0.001f)] private float characterSeconds = 0.035f;
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
        [Header("스토리 이미지·연출 — 미연결이어도 대화 진행 가능")]
        [SerializeField] private CareDialoguePresentationView presentation;
        [Header("Care 캐릭터 — 씬에 배치한 Character의 컴포넌트")]
        [SerializeField] private CharacterExpressionAnimator character;
        [SerializeField] private TMP_Text storyRewardText;
        public CareDialoguePresentationView Presentation => presentation != null ? presentation : GetComponent<CareDialoguePresentationView>();
        private bool presentationPlaying, paused;
        private readonly List<UnityAction> choiceActions = new List<UnityAction>();
        private bool visible, hasChoices, canSkip;
        private string lastImageKey;
        private CareDialogueData shownDialogue;
        private int shownLine = -1, letterCount, shownLetters;
        private float letterClock;
        private bool narrationLine, narrationSound;
        public bool IsWriting => shownLetters < letterCount;
        public event Action<CareSfxCue> SoundRequested;
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

        public void Show(CareDialogueData dialogue, int lineIndex, bool allowSkip, string rewardText = "",
            bool playNarrationSound = false)
        {
            if (dialogue == null) { Hide(); return; }
            bool newLine = shownDialogue != dialogue || shownLine != lineIndex;
            bool advancing = visible && newLine;
            visible = true; canSkip = allowSkip;
            narrationSound = playNarrationSound;
            WorkUi.Show(panelGroup, true);
            var line = dialogue.Lines[lineIndex];
            if (character != null) character.SetExpression((int)line.Emotion);
            bool narration = string.Equals(line.Speaker?.Trim(), "나레이션", StringComparison.Ordinal);
            if (namePlate != null) namePlate.SetActive(!narration);
            if (speakerText != null)
            {
                speakerText.gameObject.SetActive(!narration);
                speakerText.text = narration ? "" : string.IsNullOrEmpty(line.Speaker) ? "달이" : line.Speaker;
            }
            if (bodyText != null && bodyText.text != line.Text) bodyText.text = line.Text;
            if (newLine)
            {
                shownDialogue = dialogue; shownLine = lineIndex; narrationLine = narration;
                letterClock = 0; shownLetters = 0; letterCount = 0;
                if (bodyText != null)
                {
                    bodyText.maxVisibleCharacters = int.MaxValue;
                    bodyText.ForceMeshUpdate(true);
                    letterCount = bodyText.textInfo.characterCount;
                    bodyText.maxVisibleCharacters = 0;
                }
                if (advancing || playNarrationSound) SoundRequested?.Invoke(CareSfxCue.DialogueNext);
            }
            if (storyRewardText != null)
            {
                storyRewardText.text = rewardText;
                WorkUi.Show(storyRewardText.gameObject, !string.IsNullOrEmpty(rewardText));
            }
            hasChoices = lineIndex == dialogue.Lines.Length - 1 && dialogue.Choices.Length > 0;
            ArrangeChoices(hasChoices ? Math.Min(dialogue.Choices.Length, choiceButtons.Count) : 0);
            for (int i = 0; i < choiceButtons.Count; i++)
            {
                if (hasChoices && i < dialogue.Choices.Length && i < choiceTexts.Count && choiceTexts[i] != null)
                    choiceTexts[i].text = dialogue.Choices[i].Text;
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
            if (visible && character != null) character.Normal();
            visible = false; LogOpen = false; lastImageKey = null;
            shownDialogue = null; shownLine = -1; shownLetters = letterCount = 0;
            if (bodyText != null) bodyText.maxVisibleCharacters = int.MaxValue;
            WorkUi.Show(logGroup, false); WorkUi.Show(panelGroup, false);
            UpdateInput();
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
        public void SetOpacity(float alpha) { if (panelGroup != null) panelGroup.alpha = Mathf.Clamp01(alpha); }
        public void SetPresentationPlaying(bool value) { presentationPlaying = value; UpdateInput(); }
        public void SetPaused(bool value) { paused = value; UpdateInput(); }
        private void UpdateInput()
        {
            bool allowed = visible && !InputBlocked && !LogOpen && !presentationPlaying && !paused;
            if (skipButton != null) skipButton.interactable = allowed && canSkip;
            if (logButton != null) logButton.interactable = allowed;
            bool choicesVisible = visible && hasChoices && !IsWriting && !presentationPlaying;
            for (int i = 0; i < choiceButtons.Count; i++)
            {
                var button = choiceButtons[i];
                if (button == null) continue;
                bool show = choicesVisible && shownDialogue != null && i < shownDialogue.Choices.Length;
                button.interactable = allowed && show;
                if (button.gameObject.activeSelf != show) WorkUi.Show(button.gameObject, show);
            }
            if (continueHint != null) WorkUi.Show(continueHint, visible && !hasChoices && !IsWriting);
        }
        private void Update()
        {
            if (!visible || paused || InputBlocked || LogOpen || presentationPlaying || !IsWriting) return;
            letterClock += Time.unscaledDeltaTime;
            int count = Mathf.Min(letterCount, shownLetters + Mathf.FloorToInt(letterClock / Mathf.Max(0.001f, characterSeconds)));
            if (count == shownLetters) return;
            letterClock -= (count - shownLetters) * Mathf.Max(0.001f, characterSeconds);
            bool speak = (!narrationLine || narrationSound) && CareTextSound.HasLetter(bodyText, shownLetters, count);
            shownLetters = count;
            if (bodyText != null) bodyText.maxVisibleCharacters = count;
            if (speak) SoundRequested?.Invoke(CareSfxCue.DialogueBlip);
            if (!IsWriting) UpdateInput();
        }
        private void Next()
        {
            if (!visible || paused || InputBlocked || LogOpen || presentationPlaying) return;
            if (IsWriting)
            {
                shownLetters = letterCount;
                if (bodyText != null) bodyText.maxVisibleCharacters = int.MaxValue;
                UpdateInput();
                return;
            }
            if (!hasChoices) NextRequested?.Invoke();
        }
        private void Choose(int index)
        {
            if (!visible || paused || InputBlocked || LogOpen || presentationPlaying || IsWriting || !hasChoices
                || index < 0 || shownDialogue == null || index >= shownDialogue.Choices.Length) return;
            SoundRequested?.Invoke(CareSfxCue.DialogueChoice);
            ChoiceRequested?.Invoke(index);
        }
        private void Skip() { if (visible && !InputBlocked && !LogOpen && canSkip) SkipRequested?.Invoke(); }
        private void OpenLog()
        {
            if (!visible || InputBlocked) return;
            LogOpen = true; WorkUi.Show(logGroup, true); UpdateInput(); LogRequested?.Invoke();
            SoundRequested?.Invoke(CareSfxCue.PanelOpen);
        }
        private void CloseLog()
        {
            if (!LogOpen) return;
            LogOpen = false; WorkUi.Show(logGroup, false); UpdateInput();
            SoundRequested?.Invoke(CareSfxCue.PanelClose);
        }
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
