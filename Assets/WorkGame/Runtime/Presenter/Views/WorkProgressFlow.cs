using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    /// <summary>Full-size pattern fill. Progress and scrolling are clipped in the shader.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RawImage))]
    public sealed class WorkProgressFlow : MonoBehaviour, IMaterialModifier
    {
        [Header("게이지 색상")]
        [SerializeField] private Color baseColor = new Color(1f, 0.68f, 0.18f, 1f);
        [SerializeField] private Color completedColor = new Color(0.25f, 0.85f, 0.4f, 1f);
        [Tooltip("Fade to the completed color after the gauge finishes filling.")]
        [SerializeField, Min(0f)] private float colorFadeSeconds = 0.35f;
        [Header("흐름 / 채우기")]
        [SerializeField, Min(1f)] private float tileSize = 160f;
        [Tooltip("UI units per second. Negative values reverse the flow.")]
        [SerializeField] private float speed = 24f;
        [SerializeField, Range(0f, 1f)] private float strength = 0.3f;
        [SerializeField] private Color patternColor = Color.white;
        [Tooltip("Seconds to ease into an increased progress value. Zero disables smoothing.")]
        [SerializeField, Min(0f)] private float fillSeconds = 0.35f;
        [Tooltip("Editor preview. WorkHudView supplies the actual progress during play.")]
        [SerializeField, Range(0f, 1f)] private float progress = 0.5f;

        private static readonly int RectId = Shader.PropertyToID("_FlowRect");
        private static readonly int SettingsId = Shader.PropertyToID("_FlowSettings");
        private static readonly int PatternColorId = Shader.PropertyToID("_PatternColor");
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int TransitionId = Shader.PropertyToID("_ProgressTransition");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int CompletedColorId = Shader.PropertyToID("_CompletedColor");
        private static readonly int ColorTransitionId = Shader.PropertyToID("_ColorTransition");
        private RawImage image;
        private Material sourceMaterial;
        private Material effectMaterial;
        private float progressFrom;
        private float progressStartedAt;
        private float progressDuration;
        private bool hasProgress;
        private float colorStartedAt;
        private float colorDuration;

        private void Awake() => image = GetComponent<RawImage>();

        private void OnEnable()
        {
            if (image == null) image = GetComponent<RawImage>();
            image.raycastTarget = false;
            progressFrom = progress;
            progressDuration = 0f;
            colorDuration = 0f;
            hasProgress = false;
            image.SetMaterialDirty();
        }

        public void SetProgress(float value)
        {
            value = Mathf.Clamp01(value);
            bool animate = Application.isPlaying && isActiveAndEnabled && hasProgress
                && value > progress && fillSeconds > 0f;
            bool fadeColor = Application.isPlaying && isActiveAndEnabled && hasProgress
                && value >= 1f && progress < 1f;
            hasProgress = true;
            if (progress == value) return;
            // Retarget from the currently visible value, even during a previous fill.
            float t = progressDuration > 0f
                ? Mathf.Clamp01((Time.time - progressStartedAt) / progressDuration) : 1f;
            float remaining = 1f - t;
            float shown = Mathf.Lerp(progressFrom, progress, 1f - remaining * remaining * remaining);
            progressFrom = animate ? shown : value;
            progressStartedAt = Time.time;
            progressDuration = animate ? fillSeconds : 0f;
            colorStartedAt = progressStartedAt + progressDuration;
            colorDuration = fadeColor ? colorFadeSeconds : 0f;
            progress = value;
            if (image != null) image.SetMaterialDirty();
        }

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled || baseMaterial == null || !baseMaterial.HasProperty(SettingsId))
                return baseMaterial;

            if (image == null) image = GetComponent<RawImage>();

            // Clone the incoming material AFTER the RawImage's stencil modification.
            // Never change shared assets or Unity's cached stencil materials.
            if (effectMaterial == null || sourceMaterial != baseMaterial)
            {
                ReleaseMaterial();
                sourceMaterial = baseMaterial;
                effectMaterial = new Material(baseMaterial)
                {
                    name = baseMaterial.name + " (Progress Flow)",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            effectMaterial.CopyPropertiesFromMaterial(baseMaterial);
            Rect rect = image.rectTransform.rect;
            effectMaterial.SetVector(RectId, new Vector4(rect.xMin, rect.yMin,
                Mathf.Max(rect.width, 0.001f), Mathf.Max(rect.height, 0.001f)));
            effectMaterial.SetVector(SettingsId, new Vector4(Mathf.Max(tileSize, 1f),
                Application.isPlaying ? speed : 0f, strength, 0f));
            effectMaterial.SetColor(PatternColorId, patternColor);
            effectMaterial.SetColor(BaseColorId, baseColor);
            effectMaterial.SetColor(CompletedColorId, completedColor);
            effectMaterial.SetVector(ColorTransitionId,
                new Vector4(colorStartedAt, colorDuration, 0f, 0f));
            effectMaterial.SetFloat(ProgressId, progress);
            effectMaterial.SetVector(TransitionId,
                new Vector4(progressFrom, progressStartedAt, progressDuration, 0f));
            return effectMaterial;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (image != null) image.SetMaterialDirty();
        }

        private void OnDisable()
        {
            hasProgress = false;
            if (image != null) image.SetMaterialDirty();
            ReleaseMaterial();
        }

        private void OnDestroy() => ReleaseMaterial();

        private void ReleaseMaterial()
        {
            if (effectMaterial != null)
            {
                if (Application.isPlaying) Destroy(effectMaterial);
                else DestroyImmediate(effectMaterial);
            }
            effectMaterial = null;
            sourceMaterial = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                progressFrom = progress;
                progressDuration = 0f;
                colorDuration = 0f;
            }
            if (image == null) image = GetComponent<RawImage>();
            if (image != null) image.SetMaterialDirty();
        }
#endif
    }
}
