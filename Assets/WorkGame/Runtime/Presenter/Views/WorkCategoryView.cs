using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class WorkCategoryView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text nameText;

        public void Show(WorkCategorySO category)
        {
            if (background != null) background.color = category.HudColor;
            if (nameText != null) nameText.text = string.IsNullOrWhiteSpace(category.ShortName) ? category.Title : category.ShortName;
        }

        public void RefreshLayout()
        {
            if (nameText != null) nameText.ForceMeshUpdate();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }
    }
}
