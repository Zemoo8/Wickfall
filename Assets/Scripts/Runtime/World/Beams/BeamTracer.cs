using UnityEngine;

namespace Wickfall.World
{
    public enum BeamKind { Light, Laser }

    /// <summary>Something a beam can touch: mirrors and crystal pedestals pass it on, sockets keep it.</summary>
    public interface IBeamReceiver
    {
        /// <summary>
        /// A beam travelling along `direction` touches this at `point` (surface `normal`). Return true with
        /// the outgoing ray to pass it on, false to stop it here.
        /// </summary>
        bool Receive(BeamKind kind, Vector2 point, Vector2 direction, Vector2 normal, out Vector2 outOrigin, out Vector2 outDirection);
    }

    /// <summary>
    /// PDF §2.2 Cathédrale de Verre: "faisceaux physiques à orienter". Follows a beam from its emitter
    /// through mirrors and pedestals until a wall or a receiver stops it. Walls stop beams; glass lets
    /// them through. Never allocates.
    /// </summary>
    public static class BeamTracer
    {
        public const int MaxPoints = 16;
        static readonly RaycastHit2D[] hits = new RaycastHit2D[8];

        public static int Trace(BeamKind kind, Vector2 origin, Vector2 direction, Vector2[] points, float maxLength = 48f)
        {
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = LayerMask.GetMask(Layers.Wall) };
            int n = 0;
            points[n++] = origin;
            IBeamReceiver last = null;
            direction = direction.normalized;
            while (n < points.Length)
            {
                int count = Physics2D.Raycast(origin, direction, filter, hits, maxLength);
                int best = -1;
                float bestDistance = float.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    var h = hits[i];
                    if (h.collider == null || (last != null && h.collider.GetComponentInParent<IBeamReceiver>() == last))
                        continue; // the receiver the beam just left
                    if (h.distance < bestDistance)
                    {
                        bestDistance = h.distance;
                        best = i;
                    }
                }
                if (best < 0)
                {
                    points[n++] = origin + direction * maxLength;
                    break;
                }
                var hit = hits[best];
                points[n++] = hit.point;
                var receiver = hit.collider.GetComponentInParent<IBeamReceiver>();
                if (receiver == null || !receiver.Receive(kind, hit.point, direction, hit.normal, out origin, out direction))
                    break;
                last = receiver;
                if (n < points.Length && (origin - hit.point).sqrMagnitude > 0.0001f)
                    points[n++] = origin; // the beam leaves from the receiver's own exit point
            }
            for (int i = 0; i < hits.Length; i++)
                hits[i] = default;
            return n;
        }
    }
}
