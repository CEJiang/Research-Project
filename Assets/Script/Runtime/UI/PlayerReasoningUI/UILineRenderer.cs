using UnityEngine;
using UnityEngine.UI;

namespace Radishmouse
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UILineRenderer : MaskableGraphic
    {
        [Header("Line")]
        public Vector2[] points;
        public float thickness = 6f;

        [Tooltip("If true, points are interpreted relative to the center of this RectTransform. " +
                 "If false, points are interpreted directly in this RectTransform's local space.")]
        public bool center = false;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (points == null || points.Length < 2)
                return;

            // 目前只畫第一段：points[0] -> points[1]
            Vector2 p0 = points[0];
            Vector2 p1 = points[1];

            if (center)
            {
                Vector2 offset = rectTransform.rect.size * 0.5f;
                p0 -= offset;
                p1 -= offset;
            }

            DrawSimpleLine(vh, p0, p1, thickness, color);
        }

        private void DrawSimpleLine(VertexHelper vh, Vector2 start, Vector2 end, float lineThickness, Color32 lineColor)
        {
            Vector2 direction = end - start;
            float length = direction.magnitude;

            if (length <= 0.001f)
                return;

            direction /= length;

            // 法線方向
            Vector2 normal = new Vector2(-direction.y, direction.x) * (lineThickness * 0.5f);

            // 四個角
            Vector2 v0 = start - normal;
            Vector2 v1 = start + normal;
            Vector2 v2 = end + normal;
            Vector2 v3 = end - normal;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = lineColor;

            vertex.position = v0;
            vh.AddVert(vertex);

            vertex.position = v1;
            vh.AddVert(vertex);

            vertex.position = v2;
            vh.AddVert(vertex);

            vertex.position = v3;
            vh.AddVert(vertex);

            // 兩個三角形組成一個矩形
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }
}