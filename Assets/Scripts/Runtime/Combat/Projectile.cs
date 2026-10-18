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
        bool firstStep;
        readonly Collider2D[] overlaps = new Collider2D[8];
        ContactFilter2D filter;
        PooledObject pooled;

        public UnityEngine.Rendering.Universal.Light2D glow;

        static readonly System.Collections.Generic.List<Projectile> active = new System.Collections.Generic.List<Projectile>(32);
        /// <summary>Projectiles in flight (the region bot reads them to dodge).</summary>
        public static System.Collections.Generic.IReadOnlyList<Projectile> Active => active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();

        public Vector2 Velocity => velocity;
        public bool FromEnemy => hit.Source == Team.Enemy;

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        public void Launch(in HitData data, Vector2 direction, float speed, float range)
        {
            hit = data;
            velocity = direction.normalized * speed;
            distanceLeft = range;
            mask = HitQuery.TargetsFor(data.Source) | Layers.WallMask;
            piercedCount = 0;
            firstStep = true;
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

            // Casts ignore colliders they start inside (Physics2D "queries start in colliders" is off),
            // so on its first step a projectile also takes whatever it was spawned inside: a bolt fired
            // point-blank must still hit.
            if (firstStep)
            {
                firstStep = false;
                int inside = Physics2D.OverlapCircle(position, radius, filter, overlaps);
                for (int i = 0; i < inside; i++)
                {
                    var other = overlaps[i];
                    overlaps[i] = null;
                    if (other != null && Touch(other, position))
                    {
                        for (int k = i + 1; k < inside; k++)
                            overlaps[k] = null;
                        return;
                    }
                }
            }

            int count = Physics2D.CircleCast(position, radius, step, filter, results, length);
            for (int i = 0; i < count; i++)
            {
                var other = results[i].collider;
                if (other != null && Touch(other, results[i].centroid))
                    return;
            }

            transform.position = position + step;
            if (spin)
                transform.Rotate(0f, 0f, 540f * dt);
            distanceLeft -= length;
            if (distanceLeft <= 0f)
                Impact(transform.position);
        }

        /// <summary>Hits a target or stops on a wall. Returns true when the projectile is spent.</summary>
        bool Touch(Collider2D other, Vector2 at)
        {
            if (other.TryGetComponent(out Hurtbox hurtbox))
            {
                var target = hurtbox.Owner;
                if (target == null || !target.IsAlive || target.Team == hit.Source || AlreadyPierced(target))
                    return false;
                var each = hit;
                each.Direction = velocity.normalized;
                target.TakeHit(each);
                if (pierce && piercedCount < pierced.Length)
                {
                    pierced[piercedCount++] = target;
                    return false;
                }
                Impact(at);
                return true;
            }
            if (((1 << other.gameObject.layer) & Layers.WallMask) != 0 && !other.isTrigger)
            {
                Impact(at);
                return true;
            }
            return false;
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
