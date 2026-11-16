using UnityEngine;

namespace WorkGame
{
    internal static class WorkUi
    {
        public static void Show(CanvasGroup group, bool show)
        {
            if (group == null) return;
            group.alpha = show ? 1 : 0;
            group.interactable = show;
            group.blocksRaycasts = show;
        }

        public static void Show(GameObject target, bool show)
        {
            if (target != null && target.activeSelf != show) target.SetActive(show);
        }
    }
}
