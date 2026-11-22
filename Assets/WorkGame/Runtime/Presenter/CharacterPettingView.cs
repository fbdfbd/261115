using UnityEngine;
using UnityEngine.EventSystems;

namespace WorkGame
{
    // Presentation only: petting never changes friendship, story progress or saves.
    [DisallowMultipleComponent]
    public sealed class CharacterPettingView : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        [SerializeField] private WorkRoot workRoot;
        [SerializeField] private CharacterExpressionAnimator character;
        [SerializeField] private SpriteRenderer blush;
        [SerializeField] private SpriteRenderer[] hitAreas;
        [SerializeField] private SpriteRenderer[] hearts;
        [SerializeField, Min(0.5f)] private float reactionSeconds = 1.4f;
        [SerializeField, Min(0.1f)] private float heartSeconds = 0.85f;
        [SerializeField, Min(0f)] private float heartInterval = 0.18f;
        [SerializeField, Min(0f)] private float heartRise = 1.5f;

        [Header("쓰다듬 동작")]
        [SerializeField] private Transform leftEarPivot;
        [SerializeField] private Transform rightEarPivot;
        [SerializeField] private Transform tailPivot;
        [SerializeField, Range(0f, 15f)] private float earFlutterAngle = 5f;
        [SerializeField, Range(1f, 12f)] private float earFlutterSpeed = 6.5f;
        [SerializeField, Range(0f, 20f)] private float tailSwayAngle = 9f;
        [SerializeField, Range(0.1f, 4f)] private float tailSwaySpeed = 1.4f;

        private Vector3[] heartPositions, heartScales;
        private Transform[] motionPivots;
        private Quaternion[] animatedRotations, appliedRotations;
        private bool motionApplied;
        private CharacterExpression previousExpression, reactionExpression;
        private bool reacting;
        private float elapsed;
        private int pressedPointer = int.MinValue;
        public event System.Action Petted;
        public bool IsPettable => isActiveAndEnabled && CanPet;

        private bool CanPet => workRoot != null && workRoot.CareFlow != null
            && workRoot.CareFlow.Phase == CareFlowPhase.Free && !workRoot.CareFlow.IsPaused;

        private void Awake()
        {
            motionPivots = new[] { leftEarPivot, rightEarPivot, tailPivot };
            animatedRotations = new Quaternion[3];
            appliedRotations = new Quaternion[3];
            // Match the source sprites, so hit areas follow the animated face/body.
            foreach (var area in hitAreas)
            {
                if (area == null || area.sprite == null) continue;
                var box = area.GetComponent<BoxCollider2D>();
                if (box == null) continue;
                box.offset = area.sprite.bounds.center;
                box.size = area.sprite.bounds.size;
            }
            heartPositions = new Vector3[hearts.Length];
            heartScales = new Vector3[hearts.Length];
            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] == null) continue;
                heartPositions[i] = hearts[i].transform.localPosition;
                heartScales[i] = hearts[i].transform.localScale;
                hearts[i].enabled = false;
            }
        }

        public void OnPointerDown(PointerEventData data)
        {
            pressedPointer = data.button == PointerEventData.InputButton.Left && CanPet
                ? data.pointerId : int.MinValue;
        }

        public void OnPointerClick(PointerEventData data)
        {
            bool accepted = data.button == PointerEventData.InputButton.Left
                && data.pointerId == pressedPointer && CanPet;
            pressedPointer = int.MinValue;
            if (!accepted || character == null) return;
            if (!reacting) previousExpression = character.expression;
            reactionExpression = Random.value < 0.5f ? CharacterExpression.Happy : CharacterExpression.Normal;
            character.SetExpression(reactionExpression);
            reacting = true;
            // Rapid clicks refresh one reaction rather than stacking effects.
            elapsed = 0f;
            Petted?.Invoke();
        }

        private void LateUpdate()
        {
            if (!CanPet) pressedPointer = int.MinValue;
            if (!reacting) return;
            if (!CanPet || character.expression != reactionExpression)
            {
                Clear();
                return;
            }
            elapsed += Time.unscaledDeltaTime;
            float duration = Mathf.Max(reactionSeconds, heartSeconds + heartInterval * (hearts.Length - 1));
            if (elapsed >= duration)
            {
                Clear();
                return;
            }
            // Animator owns normal/happy features. Only overlay the existing blush
            // after animation evaluation, including on the round-eyed normal face.
            if (blush != null) blush.enabled = true;
            ApplyMotion(duration);
            for (int i = 0; i < hearts.Length; i++)
            {
                var heart = hearts[i];
                if (heart == null) continue;
                float t = (elapsed - i * heartInterval) / heartSeconds;
                heart.enabled = t >= 0f && t < 1f;
                if (!heart.enabled) continue;
                heart.transform.localPosition = heartPositions[i] + Vector3.up * (heartRise * t);
                float pop = Mathf.Sin(Mathf.Clamp01(t / 0.3f) * Mathf.PI * 0.5f);
                heart.transform.localScale = heartScales[i] * (pop * (1f + 0.12f * Mathf.Sin(t * Mathf.PI)));
                var color = heart.color;
                color.a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, t));
                heart.color = color;
            }
        }

        private void ApplyMotion(float duration)
        {
            float weight = Mathf.SmoothStep(0f, 1f, elapsed / 0.1f)
                * Mathf.SmoothStep(0f, 1f, (duration - elapsed) / 0.25f);
            for (int i = 0; i < motionPivots.Length; i++)
            {
                var pivot = motionPivots[i];
                if (pivot == null) continue;
                // The Animator supplies a fresh base pose each frame. Blend toward
                // a petting pose without accumulating rotation or changing its clips.
                animatedRotations[i] = pivot.localRotation;
                float angle;
                if (i == 2)
                    angle = tailSwayAngle * Mathf.Sin(elapsed * tailSwaySpeed * Mathf.PI * 2f);
                else
                {
                    // Cry's mirrored, rapid ear tremble, with a perked happy pose
                    // and a slight delay between ears instead of the sad droop.
                    float phase = elapsed * earFlutterSpeed * Mathf.PI * 2f - i * 0.6f;
                    angle = (i == 0 ? -1f : 1f) * (3f + earFlutterAngle * Mathf.Sin(phase));
                }
                appliedRotations[i] = Quaternion.Slerp(animatedRotations[i], Quaternion.Euler(0f, 0f, angle), weight);
                pivot.localRotation = appliedRotations[i];
            }
            motionApplied = true;
        }

        private void RestoreMotion()
        {
            if (!motionApplied) return;
            for (int i = 0; i < motionPivots.Length; i++)
            {
                var pivot = motionPivots[i];
                // Preserve any pose the Animator/dialogue has already replaced.
                if (pivot != null && Quaternion.Angle(pivot.localRotation, appliedRotations[i]) < 0.001f)
                    pivot.localRotation = animatedRotations[i];
            }
            motionApplied = false;
        }

        private void OnDisable() => Clear();

        private void Clear()
        {
            RestoreMotion();
            pressedPointer = int.MinValue;
            // A newly started dialogue owns the expression immediately.
            if (reacting && character != null && character.expression == reactionExpression
                && (workRoot == null || workRoot.CareFlow == null || !workRoot.CareFlow.Dialogue.IsOpen))
                character.SetExpression(previousExpression);
            if (reacting && blush != null)
                blush.enabled = character != null && character.expression == CharacterExpression.Happy;
            reacting = false;
            foreach (var heart in hearts) if (heart != null) heart.enabled = false;
        }
    }
}
