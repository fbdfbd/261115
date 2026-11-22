using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WorkGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
    public sealed class DiagonalTransitionView : MonoBehaviour
    {
        [Header("색상 / 패턴")]
        [SerializeField] private Color backgroundColor = new Color(0.12f, 0.16f, 0.24f, 1);
        [SerializeField] private Sprite patternSprite;
        [SerializeField] private Color patternColor = new Color(1, 1, 1, 0.18f);
        [Header("대각선 띠")]
        [SerializeField, Range(2, 40)] private int stripeCount = 14;
        [Tooltip("각 띠의 크기를 고정하고, 왼쪽부터 짧은 알파 전환으로 나타내고 숨깁니다.")]
        [SerializeField] private bool popStripes = true;
        [SerializeField, Range(-45, 45)] private float angle = 16;
        [Tooltip("덮을 때 각 띠가 채워지는 시간입니다.")]
        [SerializeField, Min(0.01f)] private float coverStripeSeconds = 0.14f;
        [Tooltip("걷힐 때 각 띠가 사라지는 시간입니다.")]
        [SerializeField, Min(0.01f)] private float stripeSeconds = 0.32f;
        [SerializeField, Min(0)] private float stripeDelay = 0.045f;
        [SerializeField, Min(0)] private float holdSeconds = 0.43f;
        [Header("도트 아이콘 — 비어 있으면 표시 안 함")]
        [SerializeField] private Sprite dotSprite;
        [SerializeField] private Color dotColor = new Color(1, 1, 1, 0.7f);
        [SerializeField, Min(16)] private float dotSpacing = 170;
        [SerializeField, Min(1)] private float dotSize = 42;
        [SerializeField, Min(0.01f)] private float dotSeconds = 0.24f;
        [SerializeField, Min(0)] private float dotRise = 24;
        [Header("재생")]
        [SerializeField] private bool playOnEnable;
        [SerializeField] private bool blockInput = true;
        [Header("연결 — 완전히 가렸을 때 / 완전히 걷혔을 때")]
        [SerializeField] private UnityEvent onCovered = new UnityEvent();
        [SerializeField] private UnityEvent onCompleted = new UnityEvent();

        public bool IsPlaying { get; private set; }
        public bool IsCovered { get; private set; }
        public UnityEvent Covered => onCovered;
        public UnityEvent Completed => onCompleted;
        public int StripeCount => Mathf.Clamp(stripeCount, 2, 40);
        public bool IsRevealing { get; private set; }
        public bool UsesStripeAlpha => popStripes;
        private float CurrentStripeSeconds => Mathf.Max(0.01f, IsRevealing ? stripeSeconds : coverStripeSeconds);
        private CanvasGroup group;
        private RectTransform root;
        private DiagonalTransitionGraphic background, pattern;
        private readonly List<UnityEngine.UI.Image> dots = new List<UnityEngine.UI.Image>();
        private readonly List<Vector2> dotPositions = new List<Vector2>();
        private Coroutine routine;
        private float elapsed;
        private int version;
        private Vector2 layoutSize;
        private float layoutSpacing;
        private Sprite layoutSprite;
        private bool visualsDirty;

        private void Awake() => Initialize();
        private void OnEnable() { if (playOnEnable) Play(); }
        private void OnDisable() => HideImmediate();

        private void Initialize()
        {
            if (group != null) return;
            group = GetComponent<CanvasGroup>();
            root = (RectTransform)transform;
            background = CreateLayer("Background");
            pattern = CreateLayer("Pattern");
            group.alpha = 0;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private DiagonalTransitionGraphic CreateLayer(string layerName)
        {
            var go = new GameObject(layerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(DiagonalTransitionGraphic));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            var graphic = go.GetComponent<DiagonalTransitionGraphic>();
            graphic.Owner = this;
            graphic.raycastTarget = false;
            return graphic;
        }

        /// <summary>Cover, hold, then reveal. Uses real time even while the game is paused.</summary>
        public void Play() => Begin(true, false);
        /// <summary>Cover and keep the screen covered until Reveal or HideImmediate is called.</summary>
        public void Cover() => Begin(false, false);
        /// <summary>Reveal from a fully covered screen.</summary>
        public void Reveal() => Begin(false, true);

        private void Begin(bool full, bool reveal)
        {
            if (!isActiveAndEnabled) return;
            Initialize();
            Cancel();
            Canvas.ForceUpdateCanvases();
            IsPlaying = true;
            IsCovered = reveal;
            IsRevealing = reveal;
            elapsed = 0;
            group.alpha = 1;
            group.blocksRaycasts = blockInput;
            // The solid layer catches input across the entire screen, including strip gaps.
            background.raycastTarget = blockInput;
            RefreshVisuals();
            routine = StartCoroutine(Animate(full, reveal, version));
        }

        private IEnumerator Animate(bool full, bool reveal, int token)
        {
            // Ensure the routine handle is assigned before any external event is invoked.
            yield return null;
            if (!reveal)
            {
                yield return Sweep(false);
                IsCovered = true;
                IsPlaying = full;
                onCovered.Invoke();
                if (token != version || !isActiveAndEnabled) yield break;
                if (!full) { routine = null; yield break; }
                float held = 0;
                while (held < Mathf.Max(0, holdSeconds))
                {
                    held += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            yield return Sweep(true);
            if (token != version) yield break;
            routine = null;
            IsPlaying = false;
            IsCovered = false;
            group.alpha = 0;
            group.blocksRaycasts = false;
            onCompleted.Invoke();
        }

        private IEnumerator Sweep(bool reveal)
        {
            IsRevealing = reveal;
            elapsed = 0;
            float duration = (StripeCount - 1) * Mathf.Max(0, stripeDelay)
                + CurrentStripeSeconds + Mathf.Max(0.01f, dotSeconds);
            while (elapsed < duration)
            {
                visualsDirty = true;
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            elapsed = duration;
            visualsDirty = true;
            if (reveal) IsCovered = false;
        }

        public void HideImmediate()
        {
            Cancel();
            IsPlaying = IsCovered = false;
            if (group == null) return;
            group.alpha = 0;
            group.blocksRaycasts = false;
        }

        private void Cancel()
        {
            version++;
            if (routine != null) StopCoroutine(routine);
            routine = null;
        }

        public float Shear(Rect r) => r.height * Mathf.Tan(angle * Mathf.Deg2Rad);

        public float StripeVisibility(int index) => SmoothStripeVisibility(index);

        private float SmoothStripeVisibility(int index)
        {
            float p = Mathf.Clamp01((elapsed - index * Mathf.Max(0, stripeDelay)) / CurrentStripeSeconds);
            p = p * p * (3 - 2 * p);
            return IsRevealing ? 1 - p : p;
        }

        private void LateUpdate()
        {
            // Animation only marks changes; apply them once, after coroutine updates.
            // Fully covered holds keep their existing mesh and dot transforms.
            if (visualsDirty && group != null && group.alpha > 0) RefreshVisuals();
        }

        private void OnRectTransformDimensionsChange() => visualsDirty = true;

        private void RefreshVisuals()
        {
            visualsDirty = false;
            Color opaque = backgroundColor;
            opaque.a = 1; // Covered must hide the old screen even if the pattern has alpha.
            background.color = opaque;
            pattern.color = patternColor;
            if (pattern.Pattern != patternSprite)
            {
                pattern.Pattern = patternSprite;
                pattern.SetMaterialDirty();
            }
            pattern.enabled = patternSprite != null;
            background.SetVerticesDirty();
            if (pattern.enabled) pattern.SetVerticesDirty();
            LayoutDots();
            Rect r = root.rect;
            float shear = Shear(r);
            float width = (r.width + Mathf.Abs(shear)) / StripeCount;
            for (int i = 0; i < dotPositions.Count; i++)
            {
                Vector2 home = dotPositions[i];
                float baseX = home.x - shear * ((home.y - r.yMin) / Mathf.Max(1, r.height));
                int stripe = Mathf.Clamp(Mathf.FloorToInt((baseX - r.xMin + Mathf.Max(0, shear)) / Mathf.Max(1, width)), 0, StripeCount - 1);
                float delay = stripe * Mathf.Max(0, stripeDelay);
                // Icons appear after their strip is filled and vanish before it starts opening.
                float t = Mathf.Clamp01((elapsed - delay - (IsRevealing ? 0 : CurrentStripeSeconds)) / Mathf.Max(0.01f, dotSeconds));
                float visible = IsRevealing ? 1 - t : t;
                if (IsRevealing) visible = Mathf.Min(visible, SmoothStripeVisibility(stripe));
                var image = dots[i];
                Color tint = dotColor;
                tint.a *= visible;
                image.color = tint;
                float pop = visible + Mathf.Sin(visible * Mathf.PI) * 0.18f;
                var rect = image.rectTransform;
                Vector3 scale = Vector3.one * pop;
                Vector2 position = home - Vector2.up * (dotRise * (1 - visible));
                Vector2 size = Vector2.one * Mathf.Max(1, dotSize);
                if (rect.localScale != scale) rect.localScale = scale;
                if (rect.anchoredPosition != position) rect.anchoredPosition = position;
                if (rect.sizeDelta != size) rect.sizeDelta = size;
            }
        }

        private void LayoutDots()
        {
            Vector2 size = root.rect.size;
            float spacing = Mathf.Max(16, dotSpacing);
            if (layoutSize == size && layoutSpacing == spacing && layoutSprite == dotSprite) return;
            layoutSize = size; layoutSpacing = spacing; layoutSprite = dotSprite;
            dotPositions.Clear();
            if (dotSprite != null)
            {
                Rect r = root.rect;
                int rows = Mathf.Max(1, Mathf.FloorToInt(size.y / spacing));
                int cols = Mathf.Max(1, Mathf.FloorToInt(size.x / spacing));
                // Keep a bounded pool even on extreme aspect ratios or small spacing values.
                rows = Mathf.Min(rows, 32); cols = Mathf.Min(cols, 48);
                for (int y = 0; y < rows; y++)
                    for (int x = 0; x < cols; x++)
                    {
                        float px = (x + 0.5f + (y % 2) * 0.25f) / cols;
                        dotPositions.Add(new Vector2(r.xMin + px * size.x, r.yMin + (y + 0.5f) / rows * size.y));
                    }
            }
            while (dots.Count < dotPositions.Count)
            {
                var go = new GameObject("Dot", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                var image = go.GetComponent<UnityEngine.UI.Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
                dots.Add(image);
            }
            for (int i = 0; i < dots.Count; i++)
            {
                dots[i].gameObject.SetActive(i < dotPositions.Count);
                dots[i].sprite = dotSprite;
            }
            // Iterate only active dots; retain spare images for subsequent layouts.
        }
    }
}
