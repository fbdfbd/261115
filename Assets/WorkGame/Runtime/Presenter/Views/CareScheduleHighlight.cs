using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareScheduleHighlight : MonoBehaviour
    {
        [SerializeField] private Outline outline;
        [SerializeField, Min(0.1f)] private float pulseSeconds = 0.6f;
        private Tween tween;
        private bool shown;
        private Color lastColor;
        public void Show(bool visible, Color color)
        {
            if (outline == null) return;
            if (shown == visible && (!visible || color == lastColor)) return;
            Clear();
            if (!visible) return;
            shown = true; lastColor = color; outline.enabled = true;
            var start = color; start.a *= 0.3f; outline.effectColor = start;
            tween = DOTween.To(() => outline.effectColor, value => outline.effectColor = value, color, pulseSeconds)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }
        public void Clear()
        {
            tween?.Kill(); tween = null; shown = false;
            if (outline != null) outline.enabled = false;
        }
        private void OnDisable() => Clear();
    }
}
