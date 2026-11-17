using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class CareSquareGrid : MonoBehaviour
    {
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField, Min(32)] private float nodeSize = 100;
        [SerializeField, Min(1)] private int maxColumns = 8;
        [SerializeField] private List<GridLayoutGroup> groups = new List<GridLayoutGroup>();
        [SerializeField, Min(0)] private float headerHeight = 32;
        [SerializeField, Min(0)] private float groupPadding = 8;
        private void OnEnable() => Resize();
        private void OnRectTransformDimensionsChange() => Resize();
        private void OnValidate() => Resize();
        private void Resize()
        {
            if (grid == null) return;
            float width = ((RectTransform)transform).rect.width - grid.padding.horizontal;
            if (width <= 0) return;
            float size = Mathf.Min(nodeSize, width);
            if (groups.Count > 0)
            {
                float gap = groups[0] != null ? groups[0].spacing.x : 8;
                size = Mathf.Min(nodeSize, Mathf.Max(1, (width - groupPadding * 2 - gap) / 2));
                float groupWidth = size * 2 + gap + groupPadding * 2;
                int count = Mathf.Clamp(Mathf.FloorToInt((width + grid.spacing.x) / (groupWidth + grid.spacing.x)), 1, Mathf.Max(1, maxColumns / 2));
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = count;
                int rows = 1;
                foreach (var group in groups)
                    if (group != null) rows = Mathf.Max(rows, Mathf.CeilToInt(group.transform.childCount / 2f));
                float rowGap = groups[0] != null ? groups[0].spacing.y : 8;
                grid.cellSize = new Vector2(groupWidth, size * rows + rowGap * (rows - 1) + headerHeight + groupPadding * 2);
                foreach (var group in groups) if (group != null) group.cellSize = Vector2.one * size;
                return;
            }
            int columns = Mathf.Clamp(Mathf.FloorToInt((width + grid.spacing.x) / (size + grid.spacing.x)), 1, maxColumns);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = Vector2.one * size;
        }
    }
}
