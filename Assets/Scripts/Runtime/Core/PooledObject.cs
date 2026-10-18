using UnityEngine;

namespace Wickfall
{
    /// <summary>Added to every pooled instance so it can send itself back.</summary>
    public sealed class PooledObject : MonoBehaviour
    {
        internal PrefabPool Pool;
        internal bool InPool;
        IPoolable[] hooks;

        internal void CacheHooks() => hooks = GetComponents<IPoolable>();

        internal void NotifySpawned()
        {
            for (int i = 0; i < hooks.Length; i++)
                hooks[i].OnSpawned();
        }

        internal void NotifyDespawned()
        {
            for (int i = 0; i < hooks.Length; i++)
                hooks[i].OnDespawned();
        }

        /// <summary>Returns this object to its pool (or destroys it if it was never pooled).</summary>
        public void Release()
        {
            if (Pool != null)
                Pool.Release(this);
            else
                Destroy(gameObject);
        }
    }
}
