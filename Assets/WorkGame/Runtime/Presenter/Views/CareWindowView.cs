using UnityEngine;

namespace WorkGame
{
    public sealed class CareWindowView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image windowImage;
        [SerializeField] private Sprite earlySprite;
        [SerializeField] private Sprite onTimeSprite;
        [SerializeField] private Sprite overtimeSprite;

        public void Show(CareLeaveKind kind)
        {
            if (windowImage == null) return;
            var sprite = kind == CareLeaveKind.Overtime ? overtimeSprite
                : kind == CareLeaveKind.OnTime ? onTimeSprite : earlySprite;
            if (sprite != null && windowImage.sprite != sprite) windowImage.sprite = sprite;
        }
    }
}
