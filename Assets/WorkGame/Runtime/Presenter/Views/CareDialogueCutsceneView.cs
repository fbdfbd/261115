using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace WorkGame
{
    public sealed class CareDialogueCutsceneView : CareDialogueCueExtension
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private UnityEngine.UI.Image picture;
        [SerializeField] private DiagonalTransitionView returnTransition;
        [SerializeField, Min(0)] private float fadeSeconds = 0.35f;
        private Coroutine exitRoutine;
        private UnityAction coveredHandler;
        private bool paused, ownsTransition;
        public bool IsVisible { get; private set; }
        public bool IsClosing { get; private set; }

        public void ConfigureDialogueCanvas(Canvas dialogueCanvas)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || dialogueCanvas == null || canvas == dialogueCanvas) return;
            canvas.overrideSorting = true;
            canvas.sortingLayerID = dialogueCanvas.sortingLayerID;
            canvas.sortingOrder = dialogueCanvas.sortingOrder - 1;
        }

        private void Awake() => Hide();
        private void OnDisable() => Cancel();
        public override bool CanPlay(string key) => key == "story_nameplate" || key == "intro_timepass";
        public override void SetPaused(bool value) => paused = value;

        public override IEnumerator Play(CareDialogueCue cue)
        {
            if (cue.Key == "intro_timepass")
            {
                FinishDialogue(null, null);
                while (IsClosing) yield return null;
                yield break;
            }
            if (!Show()) yield break;
            float elapsed = 0;
            group.alpha = 0;
            while (elapsed < fadeSeconds)
            {
                if (!paused) elapsed += Time.unscaledDeltaTime;
                group.alpha = fadeSeconds <= 0 ? 1 : Mathf.Clamp01(elapsed / fadeSeconds);
                yield return null;
            }
            group.alpha = 1;
        }

        public override void ApplyFinal(CareDialogueCue cue)
        {
            if (cue.Key == "intro_timepass") return;
            if (Show()) group.alpha = 1;
        }

        private bool Show()
        {
            if (group == null || picture == null || picture.sprite == null) return false;
            IsVisible = true;
            group.interactable = false;
            group.blocksRaycasts = false;
            return true;
        }

        public void FinishDialogue(Action finish, Action completed)
        {
            if (IsClosing) return;
            IsClosing = true;
            exitRoutine = StartCoroutine(Exit(finish, completed));
        }

        private IEnumerator Exit(Action finish, Action completed)
        {
            // Yield before callbacks so completion cannot leave a stale coroutine handle.
            yield return null;
            bool finished = false;
            Action finishCovered = () =>
            {
                if (finished) return;
                finished = true;
                Hide();
                finish?.Invoke();
            };
            if (returnTransition != null && returnTransition.isActiveAndEnabled)
            {
                while (returnTransition != null && returnTransition.isActiveAndEnabled && returnTransition.IsPlaying)
                    yield return null;
                if (returnTransition != null && returnTransition.isActiveAndEnabled)
                {
                    coveredHandler = () => finishCovered();
                    returnTransition.Covered.AddListener(coveredHandler);
                    ownsTransition = true;
                    returnTransition.Play();
                    yield return null;
                    while (returnTransition != null && returnTransition.isActiveAndEnabled && returnTransition.IsPlaying)
                        yield return null;
                }
            }
            RemoveCoveredHandler();
            ownsTransition = false;
            finishCovered(); // Also recover if the transition was disabled before reaching cover.
            exitRoutine = null;
            IsClosing = false;
            completed?.Invoke();
        }

        private void Hide()
        {
            IsVisible = false;
            if (group != null) { group.alpha = 0; group.blocksRaycasts = false; group.interactable = false; }
        }

        private void RemoveCoveredHandler()
        {
            if (returnTransition != null && coveredHandler != null)
                returnTransition.Covered.RemoveListener(coveredHandler);
            coveredHandler = null;
        }

        public override void Cancel()
        {
            if (exitRoutine != null) StopCoroutine(exitRoutine);
            exitRoutine = null;
            RemoveCoveredHandler();
            if (ownsTransition && returnTransition != null) returnTransition.HideImmediate();
            ownsTransition = false;
            IsClosing = false;
            Hide();
        }
    }
}
