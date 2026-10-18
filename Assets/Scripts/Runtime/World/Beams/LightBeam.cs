using UnityEngine;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §2.2 Cathédrale de Verre: "faisceaux physiques à orienter". A crystal lamp in the wall casts a
    /// steady pale beam; mirrors and pedestals carry it to sockets and to the Rose. It does not burn.
    /// </summary>
    public sealed class LightBeam : MonoBehaviour
    {
        public Vector2 direction = Vector2.right;
        public BeamLine line;
        public float startOffset = 0.55f;

        readonly Vector2[] points = new Vector2[BeamTracer.MaxPoints];
        public int PointCount { get; private set; }
        public Vector2 End => PointCount > 0 ? points[PointCount - 1] : (Vector2)transform.position;

        void LateUpdate() => Refresh();

        /// <summary>Traces and draws the beam now (also used by edit-mode captures).</summary>
        public void Refresh()
        {
            Vector2 dir = direction.normalized;
            PointCount = BeamTracer.Trace(BeamKind.Light, (Vector2)transform.position + dir * startOffset, dir, points);
            if (line != null)
                line.Show(points, PointCount);
        }
    }
}
