using System.Collections.Generic;
using UnityEngine;

namespace Wickfall
{
    /// <summary>Optional hooks for pooled objects.</summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }

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

    /// <summary>A pool of copies of one prefab. Spawning and releasing never allocate once warmed up.</summary>
    public sealed class PrefabPool
    {
        readonly GameObject prefab;
        readonly Transform root;
        readonly Stack<PooledObject> free;
        int created;

        public int Created => created;
        public int Available => free.Count;
        public GameObject Prefab => prefab;

        public PrefabPool(GameObject prefab, Transform root, int prewarm)
        {
            this.prefab = prefab;
            this.root = root;
            free = new Stack<PooledObject>(Mathf.Max(4, prewarm * 2));
            for (int i = 0; i < prewarm; i++)
                free.Push(Create());
        }

        PooledObject Create()
        {
            var instance = Object.Instantiate(prefab, root);
            instance.SetActive(false);
            var pooled = instance.GetComponent<PooledObject>();
            if (pooled == null)
                pooled = instance.AddComponent<PooledObject>();
            pooled.Pool = this;
            pooled.InPool = true;
            pooled.CacheHooks();
            created++;
            return pooled;
        }

        public PooledObject Spawn(Vector3 position, Quaternion rotation)
        {
            var pooled = free.Count > 0 ? free.Pop() : Create();
            if (pooled == null) // destroyed while pooled (scene teardown): make a fresh one
                pooled = Create();
            pooled.InPool = false;
            pooled.transform.SetPositionAndRotation(position, rotation);
            pooled.gameObject.SetActive(true);
            pooled.NotifySpawned();
            return pooled;
        }

        public void Release(PooledObject pooled)
        {
            if (pooled == null || pooled.InPool)
                return;
            pooled.NotifyDespawned();
            pooled.InPool = true;
            pooled.gameObject.SetActive(false);
            if (pooled.transform.parent != root)
                pooled.transform.SetParent(root, false);
            free.Push(pooled);
        }
    }

    /// <summary>
    /// PDF §4 "Pooling": one pool per prefab, under a persistent root. Projectiles, effects, wax
    /// drops, popups and audio sources all come from here, so gameplay frames allocate nothing.
    /// </summary>
    public static class Pools
    {
        static readonly Dictionary<int, PrefabPool> pools = new Dictionary<int, PrefabPool>(64);
        static Transform root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            pools.Clear();
            root = null;
        }

        static Transform Root
        {
            get
            {
                if (root == null)
                {
                    var go = new GameObject("[Pools]");
                    if (Application.isPlaying)
                        Object.DontDestroyOnLoad(go);
                    root = go.transform;
                    pools.Clear(); // the old root and its objects are gone
                }
                return root;
            }
        }

        public static PrefabPool For(GameObject prefab, int prewarm = 4)
        {
            var parent = Root;
            int key = prefab.GetInstanceID();
            if (!pools.TryGetValue(key, out var pool))
            {
                pool = new PrefabPool(prefab, parent, prewarm);
                pools.Add(key, pool);
            }
            return pool;
        }

        public static void Prewarm(GameObject prefab, int count)
        {
            if (prefab != null)
                For(prefab, count);
        }

        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            var pooled = For(prefab.gameObject).Spawn(position, rotation);
            return pooled.GetComponent<T>();
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position)
        {
            return For(prefab).Spawn(position, Quaternion.identity).gameObject;
        }

        public static void Release(Component component)
        {
            if (component == null)
                return;
            var pooled = component.GetComponent<PooledObject>();
            if (pooled != null)
                pooled.Release();
            else
                Object.Destroy(component.gameObject);
        }

        /// <summary>Every pool, for the dev overlay.</summary>
        public static Dictionary<int, PrefabPool>.ValueCollection All => pools.Values;
    }
}
