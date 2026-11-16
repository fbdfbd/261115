using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WorkGame
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class WorkFeedbackBubbleView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text messageText;
        [SerializeField, Min(0.01f)] private float clickCloseSeconds = 0.1f;
        private Tween lifeTween;
        private Tween exitTween;
        private Tween moveTween;
        private Vector3 restScale;
        private float restX;
        private float rowY;
        private float riseY;
        private float risePixels;
        private float riseSeconds;
        private bool ready;

        public bool IsShowing { get; private set; }
        public RectTransform Rect => (RectTransform)transform;
        public float Height => Rect.rect.height;
        public event Action<WorkFeedbackBubbleView> Finished;

        public void Prepare()
        {
            if (ready) return;
            ready = true;
            if (group == null) group = GetComponent<CanvasGroup>();
            if (background == null) background = GetComponent<Image>();
            restScale = Rect.localScale;
            restX = Rect.anchoredPosition.x;
            rowY = Rect.anchoredPosition.y;
            group.interactable = false;
            group.blocksRaycasts = false;
            foreach (var graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            if (background != null) background.raycastTarget = true;
            ResetView();
        }

        public void Show(string text, Color color, float startY, float staySeconds,
            float flySeconds, float flyPixels, float appearSeconds)
        {
            Prepare();
            ResetView();
            IsShowing = true;
            group.interactable = true;
            group.blocksRaycasts = true;
            riseSeconds = Mathf.Max(0.01f, flySeconds);
            risePixels = Mathf.Max(0, flyPixels);
            rowY = startY;
            ApplyPosition();
            if (background != null) background.color = color;
            if (messageText != null)
            {
                messageText.text = text;
                messageText.ForceMeshUpdate();
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(Rect);
            Rect.localScale = restScale * 0.96f;
            float appear = Mathf.Max(0.01f, appearSeconds);
            lifeTween = DOTween.Sequence().SetUpdate(true)
                .Append(FadeTo(1, appear))
                .Join(Rect.DOScale(restScale, appear).SetEase(Ease.OutBack))
                .AppendInterval(Mathf.Max(0, staySeconds))
                .OnComplete(() => StartExit(riseSeconds));
        }

        public void MoveTo(float y, float seconds)
        {
            moveTween?.Kill();
            moveTween = DOTween.To(() => rowY, value => { rowY = value; ApplyPosition(); },
                y, Mathf.Max(0.01f, seconds)).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        public void OnPointerClick(PointerEventData data)
        {
            if (!IsShowing || data.button != PointerEventData.InputButton.Left) return;
            group.interactable = false;
            group.blocksRaycasts = false;
            FinishSoon(clickCloseSeconds);
        }

        // Keep the exit animation; only bring its finish time forward.
        public void FinishSoon(float seconds)
        {
            if (!IsShowing) return;
            float duration = Mathf.Max(0.01f, seconds);
            if (exitTween != null && exitTween.IsActive())
            {
                float remaining = exitTween.Duration(false) - exitTween.Elapsed(false);
                exitTween.timeScale = Mathf.Max(exitTween.timeScale, remaining / duration);
            }
            else StartExit(duration);
        }

        private void StartExit(float seconds)
        {
            lifeTween?.Kill();
            lifeTween = null;
            Rect.localScale = restScale;
            exitTween = DOTween.Sequence().SetUpdate(true)
                .Append(DOTween.To(() => riseY, value => { riseY = value; ApplyPosition(); },
                    risePixels, seconds).SetEase(Ease.OutSine))
                .Join(FadeTo(0, seconds).SetEase(Ease.InSine))
                .OnComplete(Finish);
        }

        private void Finish()
        {
            ResetView();
            Finished?.Invoke(this);
        }

        public void ResetView()
        {
            lifeTween?.Kill();
            exitTween?.Kill();
            moveTween?.Kill();
            lifeTween = exitTween = moveTween = null;
            IsShowing = false;
            riseY = 0;
            if (!ready) return;
            group.alpha = 0;
            group.interactable = false;
            group.blocksRaycasts = false;
            Rect.localScale = restScale;
            ApplyPosition();
        }

        private void ApplyPosition() => Rect.anchoredPosition = new Vector2(restX, rowY + riseY);
        private Tween FadeTo(float alpha, float seconds) =>
            DOTween.To(() => group.alpha, value => group.alpha = value, alpha, seconds);
        private void OnDisable() => ResetView();
    }
}
