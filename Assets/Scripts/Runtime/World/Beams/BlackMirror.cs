using UnityEngine;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §2.2 Cathédrale de Verre: "Miroirs noirs" — a fixed black mirror set at 45°. It reflects
    /// beams and lasers off its face; it does not turn (the crystal pedestals do).
    /// </summary>
    public sealed class BlackMirror : MonoBehaviour, IBeamReceiver
    {
        [Tooltip("True for '/' (bottom-left to top-right), false for '\\'.")]
        public bool slash = true;

        public bool Receive(BeamKind kind, Vector2 point, Vector2 direction, Vector2 normal, out Vector2 outOrigin, out Vector2 outDirection)
        {
            // Reflect off the 45° face (exact, so axis beams stay on the grid).
            Vector2 face = slash ? new Vector2(1f, -1f).normalized : new Vector2(1f, 1f).normalized;
            outDirection = Vector2.Reflect(direction, face).normalized;
            outDirection = new Vector2(Mathf.Round(outDirection.x * 1000f) / 1000f, Mathf.Round(outDirection.y * 1000f) / 1000f).normalized;
            outOrigin = (Vector2)transform.position + outDirection * 0.05f;
            return true;
        }
    }
}
