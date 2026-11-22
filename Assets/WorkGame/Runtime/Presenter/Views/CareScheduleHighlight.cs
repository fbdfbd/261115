using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareScheduleHighlight : MonoBehaviour
    {
        [SerializeField] private Outline outline;
        [SerializeField] private Image starBadge;
        private Tween badgePop;
        private Tween badgeShimmer;
        private bool shown;
        private Color lastColor;
        public void Show(bool visible, Color color)
        {
            if (outline == null) return;
            if (shown == visible && (!visible || color == lastColor)) return;
            Clear();
            if (!visible) return;
            shown = true; lastColor = color;
            color.a = 1;
            outline.useGraphicAlpha = false;
            outline.effectColor = color;
            outline.enabled = true;
            if (starBadge == null) return;
            starBadge.gameObject.SetActive(true);
            starBadge.color = Color.white;
            var rect = starBadge.rectTransform;
            rect.localScale = Vector3.one * 0.3f;
            rect.localRotation = Quaternion.Euler(0, 0, -18);
            badgePop = DOTween.Sequence().SetUpdate(true).SetLink(starBadge.gameObject)
                .Append(rect.DOScale(1.2f, 0.18f).SetEase(Ease.OutBack))
                .Join(rect.DOLocalRotate(new Vector3(0, 0, 12), 0.18f))
                .Append(rect.DOScale(1, 0.16f).SetEase(Ease.OutSine))
                .Join(rect.DOLocalRotate(Vector3.zero, 0.16f));
            var badge = starBadge;
            badgeShimmer = DOTween.To(() => badge != null ? badge.color.a : 0f, alpha =>
                {
                    if (badge == null) return;
                    var tint = badge.color;
                    tint.a = alpha;
                    badge.color = tint;
                }, 0.7f, 0.75f).SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(badge.gameObject);
        }
        public void Clear()
        {
            shown = false;
            KillTweens();
            if (outline != null) outline.enabled = false;
            if (starBadge != null)
            {
                starBadge.gameObject.SetActive(false);
                starBadge.rectTransform.localScale = Vector3.one;
                starBadge.rectTransform.localRotation = Quaternion.identity;
                starBadge.color = Color.white;
            }
        }
        private void KillTweens()
        {
            badgePop?.Kill(); badgePop = null;
            badgeShimmer?.Kill(); badgeShimmer = null;
        }
        private void OnDisable() => Clear();
        private void OnDestroy() => KillTweens();
    }
}
