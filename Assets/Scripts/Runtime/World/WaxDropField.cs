using UnityEngine;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §2.3 "Goutte de cire au sol : laissée quand on est touché : sert de fil d'Ariane dans le
    /// Creux Profond." Every hit leaves a faintly glowing drop where the player stood. Drops stay for
    /// the whole region and cannot be picked up (G18); the oldest is recycled past the cap.
    /// </summary>
    public sealed class WaxDropField : MonoBehaviour
    {
        public GameObject dropPrefab;

        GameObject[] drops;
        int next;
        int count;

        public int Count => count;

        void Awake()
        {
            int cap = Mathf.Max(8, Db.Wax.maxGroundDrops);
            drops = new GameObject[cap];
        }

        void OnEnable()
        {
            Events.PlayerHit.Register(OnHit);
            Events.RegionEntered.Register(OnRegion);
        }

        void OnDisable()
        {
            Events.PlayerHit.Unregister(OnHit);
            Events.RegionEntered.Unregister(OnRegion);
        }

        void OnHit(HitReport report)
        {
            if (dropPrefab == null || report.Amount < Db.Wax.dropMinDamage)
                return;
            var drop = drops[next];
            if (drop == null)
            {
                drop = Instantiate(dropPrefab, transform);
                drops[next] = drop;
            }
            Vector2 jitter = new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(-0.15f, 0.15f));
            drop.transform.position = report.Position + jitter;
            drop.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.3f, Mathf.Clamp01(report.Amount / 25f));
            drop.SetActive(true);
            next = (next + 1) % drops.Length;
            count = Mathf.Min(count + 1, drops.Length);
        }

        /// <summary>A new region starts with a clean floor.</summary>
        void OnRegion(RegionId region) => Clear();

        public void Clear()
        {
            for (int i = 0; i < drops.Length; i++)
                if (drops[i] != null)
                    drops[i].SetActive(false);
            count = 0;
        }

        /// <summary>Positions of the drops (oldest first), for the tests and the dev overlay.</summary>
        public Vector2 DropPosition(int i)
        {
            int index = (next - count + i + drops.Length * 2) % drops.Length;
            return drops[index] != null ? (Vector2)drops[index].transform.position : Vector2.zero;
        }
    }
}
