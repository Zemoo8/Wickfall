using UnityEngine;
using Wickfall.Player;

namespace Wickfall.Lighting
{
    /// <summary>
    /// A pocket of darkness under the canopy or in deep shadow (PDF §2.3: "Canopée, ombres, murs :
    /// bloquent la lumière : poches d'obscurité où se cacher"). Standing inside halves the distance
    /// from which enemies can spot the player (G31).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class DarkZone : MonoBehaviour
    {
        public const float DetectionMultiplier = 0.5f;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent(out PlayerStatus status))
                status.EnterDark();
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent(out PlayerStatus status))
                status.ExitDark();
        }
    }
}
