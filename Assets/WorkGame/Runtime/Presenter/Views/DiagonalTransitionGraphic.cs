using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace WorkGame
{
    // Both layers use the same geometry so transparent pattern pixels still cover the scene.
    public sealed class DiagonalTransitionGraphic : MaskableGraphic
    {
        public DiagonalTransitionView Owner { get; set; }
        public Sprite Pattern { get; set; }
        private readonly float[] sliceYs = new float[6];
        public override Texture mainTexture => Pattern != null ? Pattern.texture : base.mainTexture;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Owner == null) return;
            Rect r = rectTransform.rect;
            if (r.width <= 0 || r.height <= 0) return;
            Vector4 uv = Pattern != null ? DataUtility.GetOuterUV(Pattern) : new Vector4(0, 0, 1, 1);
            float shear = Owner.Shear(r);
            float span = r.width + Mathf.Abs(shear);
            float width = span / Owner.StripeCount;
            float start = r.xMin - Mathf.Max(0, shear);
            for (int i = 0; i < Owner.StripeCount; i++)
            {
                float p = Owner.StripeVisibility(i);
                if (p <= 0) continue;
                float left = start + i * width;
                // On reveal, remove the left edge first, continuing the left-to-right sweep.
                float a = Owner.UsesStripeAlpha ? left : Owner.IsRevealing ? left + width * (1 - p) : left;
                float b = Owner.UsesStripeAlpha ? left + width : Owner.IsRevealing ? left + width : left + width * p;
                Color tint = color;
                if (Owner.UsesStripeAlpha) tint.a *= p;
                // Clip each horizontal slice to the screen; UVs stay fixed as the strips grow.
                AddClippedQuad(vh, r, uv, a, b, shear, tint);
            }
        }

        private void AddClippedQuad(VertexHelper vh, Rect r, Vector4 uv, float a, float b, float shear, Color tint)
        {
            // Extra vertices at screen crossings preserve the diagonal edges without a mask.
            float[] ys = sliceYs;
            ys[0] = 0; ys[1] = 1;
            ys[2] = Crossing(r.xMin, a, shear); ys[3] = Crossing(r.xMax, a, shear);
            ys[4] = Crossing(r.xMin, b, shear); ys[5] = Crossing(r.xMax, b, shear);
            System.Array.Sort(ys);
            for (int j = 1; j < ys.Length; j++)
            {
                float y0 = ys[j - 1], y1 = ys[j];
                if (y1 - y0 < 0.00001f) continue;
                float l0 = Mathf.Clamp(a + shear * y0, r.xMin, r.xMax);
                float h0 = Mathf.Clamp(b + shear * y0, r.xMin, r.xMax);
                float l1 = Mathf.Clamp(a + shear * y1, r.xMin, r.xMax);
                float h1 = Mathf.Clamp(b + shear * y1, r.xMin, r.xMax);
                if (h0 <= l0 && h1 <= l1) continue;
                int at = vh.currentVertCount;
                AddVertex(vh, r, uv, l0, y0, tint);
                AddVertex(vh, r, uv, l1, y1, tint);
                AddVertex(vh, r, uv, h1, y1, tint);
                AddVertex(vh, r, uv, h0, y0, tint);
                vh.AddTriangle(at, at + 1, at + 2);
                vh.AddTriangle(at, at + 2, at + 3);
            }
        }

        private static float Crossing(float edge, float x, float shear) =>
            Mathf.Abs(shear) < 0.001f ? 0 : Mathf.Clamp01((edge - x) / shear);

        private void AddVertex(VertexHelper vh, Rect r, Vector4 uv, float x, float y, Color tint)
        {
            vh.AddVert(new Vector3(x, r.yMin + y * r.height), tint,
                new Vector2(Mathf.Lerp(uv.x, uv.z, (x - r.xMin) / r.width), Mathf.Lerp(uv.y, uv.w, y)));
        }
    }
}
