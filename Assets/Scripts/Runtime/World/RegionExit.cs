using UnityEngine;
using Wickfall.Player;

namespace Wickfall.World
{
    /// <summary>The way out of a region: stepping in completes it (score screen, then the next region).</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class RegionExit : MonoBehaviour
    {
        bool used;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (used || other.attachedRigidbody == null || !other.attachedRigidbody.TryGetComponent(out PlayerController player))
                return;
            if (player.IsExtinguished)
                return;
            used = true;
            RegionDirector.Instance?.Complete();
        }

        public void ResetExit() => used = false;
    }
}
