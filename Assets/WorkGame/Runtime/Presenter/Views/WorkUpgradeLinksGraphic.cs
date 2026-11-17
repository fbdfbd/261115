using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    // One UI graphic per branch box. No per-frame layout or line GameObjects.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WorkUpgradeLinksGraphic : MaskableGraphic
    {
        [Serializable]
        public sealed class Link
        {
            public RectTransform From;
            public RectTransform To;
        }

        public List<Link> Links = new List<Link>();
        [Min(1)] public float Thickness = 2;

        protected override void OnEnable()
        {
            // Older generated pages predate the required component.
            if (GetComponent<CanvasRenderer>() == null) gameObject.AddComponent<CanvasRenderer>();
            base.OnEnable();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            foreach (var link in Links)
            {
                if (link.From == null || link.To == null) continue;
                Vector2 from = rectTransform.InverseTransformPoint(link.From.TransformPoint(
                    new Vector3(link.From.rect.center.x, link.From.rect.yMin)));
                Vector2 to = rectTransform.InverseTransformPoint(link.To.TransformPoint(
                    new Vector3(link.To.rect.center.x, link.To.rect.yMax)));
                float middle = (from.y + to.y) * 0.5f;
                var turnA = new Vector2(from.x, middle);
                var turnB = new Vector2(to.x, middle);
                AddLine(mesh, from, turnA);
                AddLine(mesh, turnA, turnB);
                AddLine(mesh, turnB, to);
            }
        }

        private void AddLine(VertexHelper mesh, Vector2 from, Vector2 to)
        {
            Vector2 direction = to - from;
            if (direction.sqrMagnitude < 0.01f) return;
            Vector2 side = new Vector2(-direction.y, direction.x).normalized * Mathf.Max(1, Thickness) * 0.5f;
            int start = mesh.currentVertCount;
            mesh.AddVert(from - side, color, Vector2.zero);
            mesh.AddVert(from + side, color, Vector2.zero);
            mesh.AddVert(to + side, color, Vector2.zero);
            mesh.AddVert(to - side, color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
