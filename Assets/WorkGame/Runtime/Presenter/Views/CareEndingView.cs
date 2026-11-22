using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareEndingView : MonoBehaviour, ICareSfxSource
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private CareDialogueClickArea clickArea;
        [SerializeField] private Image picture;
        [SerializeField] private CanvasGroup pictureGroup;
        [SerializeField, Min(0)] private float imageSeconds = 0.3f;
        [Header("엔딩 대화창 등장")]
        [SerializeField, Min(0)] private float dialogueDelaySeconds = 0.18f;
        [SerializeField, Min(0)] private float dialogueFadeSeconds = 0.22f;
        [SerializeField, Min(0)] private float lineDelaySeconds = 0.08f;
        [Header("마지막 직업 이미지 - 끄면 대화 종료 후 바로 타이틀로 전환")]
        [SerializeField] private bool showFinalCareerImage = true;
        [SerializeField, Min(0)] private float titleFadeSeconds = 0.6f;
        [SerializeField] private UnityEvent onCompleted = new UnityEvent();
        [SerializeField] private UnityEvent onTitleRequested = new UnityEvent();
        private Tween imageTween, titleTween;
        private Coroutine dialogueReveal;
        private CareDialogueView revealingDialogue;
        private Image fadeOverlay;
        private Canvas imageCanvas;
        private Sprite careerImage;
        private bool paused, complete, returning, prepared;
        public bool IsOpen { get; private set; }
        public bool RoomVisible { get; private set; }
        public bool CharacterVisible { get; private set; }
        public bool InputBlocked { get; private set; }
        public bool IsTransitioning => imageTween != null || dialogueReveal != null || returning;
        public event Action SceneChanged;
        public event Action Completed;
        public event Action TitleRequested;
        public event Action<CareSfxCue> SoundRequested;

        private void Awake() { PrepareLayout(); Hide(); }
        private void OnEnable() { if (clickArea != null) clickArea.Clicked += Next; }
        private void OnDisable() { if (clickArea != null) clickArea.Clicked -= Next; Hide(); }
        private void PrepareLayout()
        {
            if (prepared) return;
            prepared = true;
            imageCanvas = GetComponent<Canvas>();
            if (imageCanvas == null) imageCanvas = gameObject.AddComponent<Canvas>();
            imageCanvas.overrideSorting = true;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
            // Existing letter prefab references remain valid. Only its presentation changes.
            if (picture != null) picture.transform.SetParent(transform, false);
            foreach (Transform child in transform)
                if (picture == null || child != picture.transform) child.gameObject.SetActive(false);
            var matte = GetComponent<Image>();
            if (matte != null) { matte.color = Color.black; matte.raycastTarget = true; }
            if (picture != null)
            {
                picture.gameObject.SetActive(true); picture.raycastTarget = false; picture.preserveAspect = false;
            }
            var fade = new GameObject("EndingTitleFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fade.transform.SetParent(transform, false); fade.layer = gameObject.layer;
            fadeOverlay = fade.GetComponent<Image>(); fadeOverlay.color = Color.clear; fadeOverlay.raycastTarget = false;
            var rect = (RectTransform)fade.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        public void ConfigureDialogue(CareDialogueView dialogue)
        {
            PrepareLayout();
            var canvas = dialogue != null ? dialogue.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return;
            imageCanvas.sortingLayerID = canvas.sortingLayerID;
            imageCanvas.sortingOrder = canvas.sortingOrder - 1;
        }
        public void Open(string title, Sprite image = null)
        {
            CancelDialogueReveal();
            PrepareLayout(); IsOpen = true; complete = false; returning = false;
            careerImage = image;
            var matte = GetComponent<Image>();
            if (matte != null) matte.color = Color.black;
            fadeOverlay.color = Color.clear;
        }
        public void PresentDialogue(CareDialogueView dialogue, Action show, bool cutsceneChanged)
        {
            CancelDialogueReveal();
            revealingDialogue = dialogue;
            dialogue.SetPresentationPlaying(true);
            if (cutsceneChanged) dialogue.SetOpacity(0);
            dialogueReveal = StartCoroutine(RevealDialogue(show, cutsceneChanged));
        }
        private IEnumerator RevealDialogue(Action show, bool cutsceneChanged)
        {
            yield return null;
            while (imageTween != null) yield return null;
            yield return DialogueDelay(cutsceneChanged ? dialogueDelaySeconds : lineDelaySeconds);
            show();
            if (cutsceneChanged) revealingDialogue.SetOpacity(0);
            float duration = cutsceneChanged ? dialogueFadeSeconds : 0;
            float elapsed = 0;
            while (elapsed < duration)
            {
                if (!paused && !InputBlocked && !revealingDialogue.InputBlocked) elapsed += Time.unscaledDeltaTime;
                revealingDialogue.SetOpacity(Mathf.SmoothStep(0, 1, elapsed / duration));
                yield return null;
            }
            revealingDialogue.SetOpacity(1);
            revealingDialogue.SetPresentationPlaying(false);
            revealingDialogue = null;
            dialogueReveal = null;
        }
        private IEnumerator DialogueDelay(float seconds)
        {
            float elapsed = 0;
            while (elapsed < seconds)
            {
                if (!paused && !InputBlocked && !revealingDialogue.InputBlocked) elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        private void CancelDialogueReveal()
        {
            if (dialogueReveal != null) StopCoroutine(dialogueReveal);
            dialogueReveal = null;
            if (revealingDialogue != null) revealingDialogue.SetPresentationPlaying(false);
            revealingDialogue = null;
        }
        public void ShowRoom(bool character)
        {
            imageTween?.Kill(); imageTween = null;
            RoomVisible = true; CharacterVisible = character;
            WorkUi.Show(panelGroup, false); SceneChanged?.Invoke();
        }
        public void ShowCareer(Sprite sprite)
        {
            bool changed = RoomVisible || careerImage != sprite;
            careerImage = sprite; RoomVisible = false; CharacterVisible = false;
            WorkUi.Show(panelGroup, true);
            if (picture != null) { picture.sprite = sprite; picture.enabled = sprite != null; FitPicture(); }
            SceneChanged?.Invoke();
            if (!changed || pictureGroup == null || sprite == null) return;
            imageTween?.Kill(); pictureGroup.alpha = 0;
            imageTween = DOTween.To(() => pictureGroup.alpha, value => pictureGroup.alpha = value, 1, imageSeconds)
                .SetUpdate(true).OnComplete(() => imageTween = null);
            if (paused) imageTween.Pause();
            SoundRequested?.Invoke(CareSfxCue.EndingPage);
        }
        private void OnRectTransformDimensionsChange() { if (prepared) FitPicture(); }
        private void FitPicture()
        {
            if (picture == null || picture.sprite == null) return;
            var bounds = ((RectTransform)transform).rect.size;
            var size = picture.sprite.rect.size;
            float scale = Mathf.Max(bounds.x / Mathf.Max(1, size.x), bounds.y / Mathf.Max(1, size.y));
            var rect = picture.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero; rect.localScale = Vector3.one; rect.sizeDelta = size * scale;
        }
        public void ShowComplete(bool notify = true)
        {
            CancelDialogueReveal();
            bool first = !complete; complete = true;
            if (notify && first) { Completed?.Invoke(); onCompleted.Invoke(); SoundRequested?.Invoke(CareSfxCue.EndingComplete); }
            if (showFinalCareerImage && careerImage != null) ShowCareer(careerImage);
            else ReturnToTitle();
        }
        private void Next()
        {
            if (IsOpen && complete && !paused && !InputBlocked && !IsTransitioning) ReturnToTitle();
        }
        private void ReturnToTitle()
        {
            if (returning) return;
            if (RoomVisible)
            {
                var matte = GetComponent<Image>();
                if (matte != null) matte.color = Color.clear;
                if (picture != null) picture.enabled = false;
            }
            returning = true; WorkUi.Show(panelGroup, true);
            titleTween = DOTween.To(() => fadeOverlay.color, value => fadeOverlay.color = value, Color.black, titleFadeSeconds)
                .SetUpdate(true).OnComplete(() =>
            {
                titleTween = null; TitleRequested?.Invoke(); onTitleRequested.Invoke();
            });
            if (paused) titleTween.Pause();
        }
        public void SetInputBlocked(bool blocked) => InputBlocked = blocked;
        public void SetPaused(bool value)
        {
            paused = value;
            if (value) { imageTween?.Pause(); titleTween?.Pause(); }
            else { imageTween?.Play(); titleTween?.Play(); }
        }
        public void Hide()
        {
            CancelDialogueReveal();
            imageTween?.Kill(); titleTween?.Kill(); imageTween = titleTween = null;
            IsOpen = RoomVisible = CharacterVisible = complete = returning = false; careerImage = null;
            if (picture != null) { picture.sprite = null; picture.enabled = false; }
            if (pictureGroup != null) pictureGroup.alpha = 1;
            if (fadeOverlay != null) fadeOverlay.color = Color.clear;
            WorkUi.Show(panelGroup, false); SceneChanged?.Invoke();
        }
    }
}
