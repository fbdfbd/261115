using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera)), DefaultExecutionOrder(-1000)]
    public sealed class GameAspectView : MonoBehaviour
    {
        private Camera target;
        private Rect originalRect;
        private GameObject barsRoot;
        private readonly RectTransform[] bars = new RectTransform[4];
        private int width = -1, height = -1;

        private void OnEnable()
        {
            target = GetComponent<Camera>();
            originalRect = target.rect;
            if (barsRoot == null) CreateBars();
            barsRoot.SetActive(true);
            width = height = -1;
            Refresh();
        }
        private void LateUpdate() => Refresh();
        private void CreateBars()
        {
            barsRoot = new GameObject("Game 16x9 Letterbox", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = barsRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            canvas.targetDisplay = target.targetDisplay;
            string[] names = { "Left", "Right", "Bottom", "Top" };
            for (int i = 0; i < bars.Length; i++)
            {
                var bar = new GameObject(names[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bar.transform.SetParent(barsRoot.transform, false);
                var image = bar.GetComponent<Image>();
                image.color = Color.black; image.raycastTarget = true;
                bars[i] = (RectTransform)bar.transform;
            }
        }
        private void Refresh()
        {
            if (target == null || (width == Screen.width && height == Screen.height)) return;
            width = Screen.width; height = Screen.height;
            var fit = GameDisplayPolicy.GetViewport(width, height);
            target.rect = new Rect(fit.X, fit.Y, fit.Width, fit.Height);
            SetBar(0, 0, 0, fit.X, 1);
            SetBar(1, fit.X + fit.Width, 0, 1, 1);
            SetBar(2, fit.X, 0, fit.X + fit.Width, fit.Y);
            SetBar(3, fit.X, fit.Y + fit.Height, fit.X + fit.Width, 1);
        }
        private void SetBar(int index, float left, float bottom, float right, float top)
        {
            var bar = bars[index];
            bar.gameObject.SetActive(right - left > 0.000001f && top - bottom > 0.000001f);
            bar.anchorMin = new Vector2(left, bottom); bar.anchorMax = new Vector2(right, top);
            bar.offsetMin = bar.offsetMax = Vector2.zero;
        }
        private void OnDisable()
        {
            if (target != null) target.rect = originalRect;
            if (barsRoot != null) barsRoot.SetActive(false);
        }
        private void OnDestroy() { if (barsRoot != null) Destroy(barsRoot); }
    }
}
