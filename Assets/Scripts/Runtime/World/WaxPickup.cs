using UnityEngine;
using Wickfall.Meta;
using Wickfall.Player;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §4 WaxPickup: "Récompense de cire à la mort d'un ennemi". Droplets burst out of a dead
    /// creature, then fly to the player when close (PDF §2.3: 4 to 15 wax per kill).
    /// </summary>
    public sealed class WaxPickup : MonoBehaviour, IPoolable
    {
        public SpriteRenderer sprite;
        public float wax = 3f;
        public int shards;

        Vector2 velocity;
        float age;
        PooledObject pooled;

        public void Launch(float waxAmount, int shardAmount, Vector2 burst)
        {
            wax = waxAmount;
            shards = shardAmount;
            velocity = burst;
            age = 0f;
        }

        public void OnSpawned()
        {
            if (pooled == null)
                pooled = GetComponent<PooledObject>();
            age = 0f;
        }

        public void OnDespawned() { }

        void Update()
        {
            age += Time.deltaTime;
            var session = GameSession.Instance;
            var player = session != null ? session.player : null;
            Vector2 position = transform.position;

            if (age > 0.35f && player != null && player.isActiveAndEnabled && !player.IsExtinguished)
            {
                Vector2 target = (Vector2)player.transform.position + new Vector2(0f, 0.6f);
                Vector2 to = target - position;
                float distance = to.magnitude;
                var balance = Db.Balance;
                if (distance < balance.pickupMagnetRadius || age > 4f)
                {
                    velocity = Vector2.MoveTowards(velocity, to.normalized * balance.pickupMagnetSpeed * (age > 4f ? 2f : 1f),
                        60f * Time.deltaTime);
                    if (distance < 0.35f)
                    {
                        Collect(player);
                        return;
                    }
                }
                else
                {
                    velocity = Vector2.MoveTowards(velocity, Vector2.zero, 8f * Time.deltaTime);
                }
            }
            else
            {
                velocity = Vector2.MoveTowards(velocity, Vector2.zero, 6f * Time.deltaTime);
            }
            transform.position = position + velocity * Time.deltaTime;
            if (sprite != null)
                sprite.transform.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(age * 6f)) * 0.08f, 0f);
        }

        void Collect(PlayerController player)
        {
            player.Wax.Gain(wax * GameSession.Instance.Modifiers.KillWaxMultiplier);
            if (shards > 0)
                GameSession.Instance.AddShards(shards);
            Events.Play(SoundId.WaxPickup, transform.position, 0.6f, 0.9f + Random.value * 0.3f);
            Combat.Vfx.Play(Combat.VfxId.PickupGlint, transform.position);
            if (pooled != null) pooled.Release();
            else gameObject.SetActive(false);
        }

        /// <summary>Spreads a kill reward over a few droplets.</summary>
        public static void Burst(WaxPickup prefab, Vector2 at, int totalWax, int totalShards)
        {
            if (prefab == null || totalWax <= 0 && totalShards <= 0)
                return;
            int drops = Mathf.Clamp(totalWax / 3, 1, 5);
            for (int i = 0; i < drops; i++)
            {
                float angle = (i / (float)drops) * Mathf.PI * 2f + Random.value;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var drop = Pools.Spawn(prefab, at, Quaternion.identity);
                int shardsHere = i == 0 ? totalShards : 0;
                drop.Launch(totalWax / (float)drops, shardsHere, dir * Random.Range(2f, 3.5f));
            }
        }
    }
}
