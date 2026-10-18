using UnityEngine;

namespace Wickfall.World
{
    /// <summary>Draws a traced beam with a LineRenderer (A38e: a shimmering, glowing segment).</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class BeamLine : MonoBehaviour
    {
        public float width = 0.22f;
        public Color color = new Color(0.93f, 0.9f, 0.83f, 1f);
        public float shimmer = 0.15f;

        LineRenderer line;
        readonly Vector3[] positions = new Vector3[BeamTracer.MaxPoints];

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 0;
        }

        public void Show(Vector2[] points, int count, float widthScale = 1f, float alpha = 1f)
        {
            if (line == null)
                line = GetComponent<LineRenderer>();
            for (int i = 0; i < count; i++)
                positions[i] = new Vector3(points[i].x, points[i].y, 0f);
            line.positionCount = count;
            line.SetPositions(positions);
            float w = width * widthScale * (1f + shimmer * Mathf.Sin(Time.time * 31f));
            line.startWidth = line.endWidth = w;
            var c = color;
            c.a *= alpha;
            line.startColor = line.endColor = c;
            line.enabled = count > 1;
        }

        public void Hide()
        {
            if (line == null)
                line = GetComponent<LineRenderer>();
            line.positionCount = 0;
            line.enabled = false;
        }
    }
}
