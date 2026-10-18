using UnityEngine;

namespace Wickfall.Combat
{
    /// <summary>
    /// A damage zone switched on for a short window (PDF §4 "Hitbox"). While active it hits each
    /// target once. Used by enemy melee, the boss slam and timed hazards.
    /// </summary>
    public sealed class Hitbox : MonoBehaviour
    {
        public float radius = 0.8f;
        [Range(10f, 360f)] public float arcDegrees = 360f;
        public Vector2 offset;

        HitData hit;
        Vector2 facing = Vector2.down;
        float activeUntil = -1f;
        int mask;
        readonly IDamageable[] alreadyHit = new IDamageable[16];
        int hitCount;
        static readonly Collider2D[] buffer = new Collider2D[24];
        ContactFilter2D filter;
        IDamageable ignore;

        public bool IsActive => Time.time < activeUntil;

        public void Activate(in HitData data, Vector2 direction, float seconds, IDamageable self = null)
        {
            hit = data;
            facing = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;
            activeUntil = Time.time + seconds;
            mask = HitQuery.TargetsFor(data.Source);
            ignore = self;
            for (int i = 0; i < hitCount; i++)
                alreadyHit[i] = null;
            hitCount = 0;
            Sweep();
        }

        public void Deactivate() => activeUntil = -1f;

        void FixedUpdate()
        {
            if (IsActive)
                Sweep();
        }

        void Sweep()
        {
            Vector2 origin = (Vector2)transform.position + offset;
            filter.useLayerMask = true;
            filter.layerMask = mask;
            filter.useTriggers = true;
            int count = Physics2D.OverlapCircle(origin, radius, filter, buffer);
            for (int i = 0; i < count; i++)
            {
                var collider = buffer[i];
                buffer[i] = null;
                if (collider == null || !collider.TryGetComponent(out Hurtbox hurtbox))
                    continue;
                var target = hurtbox.Owner;
                if (target == null || target == ignore || !target.IsAlive || target.Team == hit.Source || WasHit(target))
                    continue;
                if (!CombatMath.InArc(origin, facing, collider.ClosestPoint(origin), radius, arcDegrees))
                    continue;
                if (hitCount < alreadyHit.Length)
                    alreadyHit[hitCount++] = target;
                var each = hit;
                Vector2 to = target.Position - origin;
                each.Direction = to.sqrMagnitude > 0.0001f ? to.normalized : facing;
                target.TakeHit(each);
            }
        }

        bool WasHit(IDamageable target)
        {
            for (int i = 0; i < hitCount; i++)
                if (alreadyHit[i] == target)
                    return true;
            return false;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere((Vector2)transform.position + offset, radius);
        }
    }
}
