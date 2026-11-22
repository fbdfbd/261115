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
        [Header("즉시완료 — 색상 변경 없이 탄력 등장 / 광택 1회")]
        [Tooltip("Bubble 프리팹의 광택 Image. 중앙 Anchor/Pivot, Raycast Target 해제.")]
        [SerializeField] private RectTransform instantShine;
        [SerializeField, Min(0.01f)] private float instantAppearSeconds = 0.26f;
        [SerializeField, Range(0.1f, 1f)] private float instantStartScale = 0.78f;
        [SerializeField, Min(0.01f)] private float instantShineSeconds = 0.42f;
        [Header("즉시완료 — 모서리 별 장식")]
        [SerializeField] private Image topLeftStar;
        [SerializeField] private Image bottomRightStar;
        [SerializeField, Min(0.01f)] private float starSeconds = 0.62f;
        [SerializeField, Min(0)] private float starStaggerSeconds = 0.12f;
        [SerializeField] private float starSpinDegrees = 140f;
        [Header("포인트 배수 — 배경 무지개 / 흰빛 반짝임 2회")]
        [SerializeField] private Image rainbowBackground;
        [SerializeField, Min(0.01f)] private float rainbowSeconds = 0.7f;
        [SerializeField, Range(0, 1)] private float rainbowSaturation = 0.38f;
        [Tooltip("원본 Bubble 이미지의 테두리 두께(스프라이트 픽셀).")]
        [SerializeField, Min(0)] private float rainbowInsetPixels = 10f;
        private Tween rainbowTween;
        private Tween topLeftStarTween;
        private Tween bottomRightStarTween;
        private StarPose topLeftStarPose;
        private StarPose bottomRightStarPose;
        private struct StarPose
        {
            public Vector3 Scale;
            public Quaternion Rotation;
            public Color Tint;
            public StarPose(Image star)
            {
                Scale = star != null ? star.rectTransform.localScale : Vector3.one;
                Rotation = star != null ? star.rectTransform.localRotation : Quaternion.identity;
                Tint = star != null ? star.color : Color.white;
            }
        }
        private Tween shineTween;
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
            topLeftStarPose = new StarPose(topLeftStar);
            bottomRightStarPose = new StarPose(bottomRightStar);
            group.interactable = false;
            group.blocksRaycasts = false;
            foreach (var graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            if (background != null) background.raycastTarget = true;
            ResetView();
        }

        public void Show(string text, Color color, float startY, float staySeconds,
            float flySeconds, float flyPixels, float appearSeconds, bool instant = false, bool pointBoosted = false)
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
            Rect.localScale = restScale * (instant ? instantStartScale : 0.96f);
            float appear = Mathf.Max(0.01f, instant ? instantAppearSeconds : appearSeconds);
            lifeTween = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
                .Append(FadeTo(1, appear))
                .Join(Rect.DOScale(restScale, appear).SetEase(Ease.OutBack))
                .AppendInterval(Mathf.Max(0, staySeconds))
                .OnComplete(() => StartExit(riseSeconds));
            if (instant && instantShine != null)
            {
                Vector2 travel = new Vector2((Rect.rect.width + instantShine.rect.size.magnitude) * 0.5f,
                    Rect.rect.height * 0.5f);
                instantShine.anchoredPosition = -travel;
                shineTween = DOTween.To(() => instantShine.anchoredPosition,
                        value => instantShine.anchoredPosition = value, travel, Mathf.Max(0.01f, instantShineSeconds))
                    .SetDelay(appear * 0.5f).SetEase(Ease.Linear).SetUpdate(true)
                    .SetLink(instantShine.gameObject)
                    .OnStart(() => { if (instantShine != null) instantShine.gameObject.SetActive(true); })
                    .OnComplete(StopShine);
            }
            if (instant)
            {
                topLeftStarTween = PlayStar(topLeftStar, topLeftStarPose, appear * 0.5f, -1f);
                bottomRightStarTween = PlayStar(bottomRightStar, bottomRightStarPose,
                    appear * 0.5f + Mathf.Max(0, starStaggerSeconds), 1f);
            }
            if (pointBoosted) PlayRainbow(color);
        }

        public void MoveTo(float y, float seconds)
        {
            moveTween?.Kill();
            moveTween = DOTween.To(() => rowY, value => { rowY = value; ApplyPosition(); },
                y, Mathf.Max(0.01f, seconds)).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
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
            StopShine();
            lifeTween?.Kill();
            lifeTween = null;
            Rect.localScale = restScale;
            exitTween = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
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
            // Stop every callback before touching UI that may already be tearing down.
            KillTweens();
            StopShine();
            StopRainbow();
            ResetStar(topLeftStar, topLeftStarPose);
            ResetStar(bottomRightStar, bottomRightStarPose);
            IsShowing = false;
            riseY = 0;
            if (!ready || this == null) return;
            if (group != null)
            {
                group.alpha = 0;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            Rect.localScale = restScale;
            ApplyPosition();
        }

        private void KillTweens()
        {
            lifeTween?.Kill();
            exitTween?.Kill();
            moveTween?.Kill();
            shineTween?.Kill();
            rainbowTween?.Kill();
            topLeftStarTween?.Kill();
            bottomRightStarTween?.Kill();
            lifeTween = exitTween = moveTween = shineTween = rainbowTween = null;
            topLeftStarTween = bottomRightStarTween = null;
        }

        private void ApplyPosition() => Rect.anchoredPosition = new Vector2(restX, rowY + riseY);
        private void PlayRainbow(Color normalColor)
        {
            if (rainbowBackground == null) return;
            if (background != null && background.sprite != null)
            {
                // 9-slice의 작은 말풍선에서 압축된 테두리 두께에도 맞춥니다.
                Vector4 border = background.sprite.border;
                float ppu = Mathf.Max(0.001f, background.pixelsPerUnit * background.pixelsPerUnitMultiplier);
                Vector2 inset = new Vector2(
                    Mathf.Min(1f, Rect.rect.width * ppu / Mathf.Max(1f, border.x + border.z)),
                    Mathf.Min(1f, Rect.rect.height * ppu / Mathf.Max(1f, border.y + border.w))) * (rainbowInsetPixels / ppu);
                rainbowBackground.rectTransform.offsetMin = inset;
                rainbowBackground.rectTransform.offsetMax = -inset;
            }
            rainbowBackground.color = normalColor;
            rainbowBackground.gameObject.SetActive(true);
            rainbowTween = DOTween.To(() => 0f, progress =>
                {
                    if (rainbowBackground == null) return;
                    Color rainbow = Color.HSVToRGB(Mathf.Repeat(0.95f + progress, 1f), rainbowSaturation, 1f);
                    rainbow.a = normalColor.a;
                    float pulse = Mathf.Pow(Mathf.Sin(progress * Mathf.PI * 2f), 8f);
                    float envelope = Mathf.SmoothStep(0, 1, Mathf.Min(progress, 1f - progress) / 0.12f);
                    rainbowBackground.color = Color.Lerp(normalColor, rainbow, envelope * (1f - 0.85f * pulse));
                }, 1f, Mathf.Max(0.01f, rainbowSeconds))
                .SetEase(Ease.Linear).SetUpdate(true).SetLink(rainbowBackground.gameObject)
                .OnComplete(StopRainbow);
        }

        private void StopRainbow()
        {
            rainbowTween?.Kill();
            rainbowTween = null;
            if (rainbowBackground == null) return;
            rainbowBackground.gameObject.SetActive(false);
            rainbowBackground.color = Color.white;
        }

        private Tween PlayStar(Image star, StarPose pose, float delay, float direction)
        {
            if (star == null) return null;
            star.raycastTarget = false;
            return DOTween.To(() => 0f, progress =>
                {
                    if (star == null) return;
                    // 두 번만 반짝이고, 양쪽 별은 서로 반대 방향으로 회전합니다.
                    float sparkle = Mathf.Abs(Mathf.Sin(progress * Mathf.PI * 2f));
                    star.rectTransform.localScale = pose.Scale * Mathf.Lerp(0.65f, 1.15f, sparkle);
                    star.rectTransform.localRotation = pose.Rotation
                        * Quaternion.Euler(0, 0, direction * starSpinDegrees * progress);
                    Color color = pose.Tint;
                    color.a *= sparkle;
                    star.color = color;
                }, 1f, Mathf.Max(0.01f, starSeconds))
                .SetDelay(delay).SetEase(Ease.Linear).SetUpdate(true).SetLink(star.gameObject)
                .OnStart(() =>
                {
                    if (star == null) return;
                    Color color = pose.Tint;
                    color.a = 0;
                    star.color = color;
                    star.gameObject.SetActive(true);
                })
                .OnComplete(() => ResetStar(star, pose));
        }

        private void ResetStar(Image star, StarPose pose)
        {
            if (star == null) return;
            star.gameObject.SetActive(false);
            if (!ready) return;
            star.rectTransform.localScale = pose.Scale;
            star.rectTransform.localRotation = pose.Rotation;
            star.color = pose.Tint;
        }

        private void StopShine()
        {
            shineTween?.Kill();
            shineTween = null;
            if (instantShine != null) instantShine.gameObject.SetActive(false);
        }
        private Tween FadeTo(float alpha, float seconds) =>
            DOTween.To(() => group.alpha, value => group.alpha = value, alpha, seconds);
        private void OnDisable() => ResetView();
        private void OnDestroy() => KillTweens();
    }
}
