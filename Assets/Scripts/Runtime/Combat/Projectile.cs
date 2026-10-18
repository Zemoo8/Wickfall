using UnityEngine;

namespace Wickfall.Combat
{
    /// <summary>
    /// A pooled projectile (blue bolts, Bellows soot, the Roi Creux's voids). It moves itself and
    /// circle-casts each physics step, so it never allocates and never needs collision callbacks.
    /// </summary>
    public sealed class Projectile : MonoBehaviour, IPoolable
    {
        public float radius = 0.18f;
        public bool pierce;
        public SpriteRenderer sprite;
        public bool spin;
        public GameObject impactVfx;

        HitData hit;
        Vector2 velocity;
        float distanceLeft;
        int mask;
        readonly RaycastHit2D[] results = new RaycastHit2D[8];
        readonly IDamageable[] pierced = new IDamageable[8];
        int piercedCount;
        ContactFilter2D filter;
        PooledObject pooled;

        public UnityEngine.Rendering.Universal.Light2D glow;

        public void Launch(in HitData data, Vector2 direction, float speed, float range)
        {
            hit = data;
            velocity = direction.normalized * speed;
            distanceLeft = range;
            mask = HitQuery.TargetsFor(data.Source) | Layers.WallMask;
            piercedCount = 0;
            transform.right = direction;
            if (data.HasFlame)
            {
                var flame = Db.Flame(data.Flame);
                if (sprite != null) sprite.color = flame.bodyColor;
                if (glow != null) glow.color = flame.lightColor;
            }
        }

        public void OnSpawned()
        {
            if (pooled == null)
                pooled = GetComponent<PooledObject>();
        }

        public void OnDespawned()
        {
            for (int i = 0; i < pierced.Length; i++)
                pierced[i] = null;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 position = transform.position;
            Vector2 step = velocity * dt;
            float length = step.magnitude;

            filter.useLayerMask = true;
            filter.layerMask = mask;
            filter.useTriggers = true;
            int count = Physics2D.CircleCast(position, radius, step, filter, results, length);
            for (int i = 0; i < count; i++)
            {
                var other = results[i].collider;
                if (other == null)
                    continue;
                if (other.TryGetComponent(out Hurtbox hurtbox))
                {
                    var target = hurtbox.Owner;
                    if (target == null || !target.IsAlive || target.Team == hit.Source || AlreadyPierced(target))
                        continue;
                    var each = hit;
                    each.Direction = velocity.normalized;
                    target.TakeHit(each);
                    if (pierce && piercedCount < pierced.Length)
                    {
                        pierced[piercedCount++] = target;
                        continue;
                    }
                    Impact(results[i].centroid);
                    return;
                }
                if (((1 << other.gameObject.layer) & Layers.WallMask) != 0 && !other.isTrigger)
                {
                    Impact(results[i].centroid);
                    return;
                }
            }

            transform.position = position + step;
            if (spin)
                transform.Rotate(0f, 0f, 540f * dt);
            distanceLeft -= length;
            if (distanceLeft <= 0f)
                Impact(transform.position);
        }

        bool AlreadyPierced(IDamageable target)
        {
            for (int i = 0; i < piercedCount; i++)
                if (pierced[i] == target)
                    return true;
            return false;
        }

        void Impact(Vector2 at)
        {
            if (impactVfx != null)
                Pools.Spawn(impactVfx, at);
            if (pooled != null) pooled.Release();
            else gameObject.SetActive(false);
        }
    }
}
