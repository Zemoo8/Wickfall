using UnityEngine;

namespace Wickfall.Combat
{
    /// <summary>
    /// Allocation-free damage queries (PDF §4: "les attaques activent une zone de dégâts").
    /// Every attack in the game goes through these: an arc or a circle hits each target once.
    /// </summary>
    public static class HitQuery
    {
        static readonly Collider2D[] buffer = new Collider2D[48];
        static readonly IDamageable[] seen = new IDamageable[48];
        static ContactFilter2D filter;

        /// <summary>Hits everything of the other teams inside an arc. Returns how many hits landed.</summary>
        public static int Arc(Vector2 origin, Vector2 facing, float range, float arcDegrees, int layerMask, in HitData hit,
            IDamageable ignore = null)
        {
            filter.useLayerMask = true;
            filter.layerMask = layerMask;
            filter.useTriggers = true;
            int count = Physics2D.OverlapCircle(origin, range, filter, buffer);
            int landed = 0;
            int seenCount = 0;
            for (int i = 0; i < count; i++)
            {
                var collider = buffer[i];
                buffer[i] = null;
                if (collider == null || !collider.TryGetComponent(out Hurtbox hurtbox))
                    continue;
                var target = hurtbox.Owner;
                if (target == null || target == ignore || !target.IsAlive || target.Team == hit.Source)
                    continue;
                if (Contains(seen, seenCount, target))
                    continue;
                seen[seenCount++] = target;

                Vector2 point = collider.ClosestPoint(origin);
                if (!CombatMath.InArc(origin, facing, point, range, arcDegrees))
                    continue;

                var each = hit;
                each.Direction = (target.Position - origin).sqrMagnitude > 0.0001f ? (target.Position - origin).normalized : facing;
                if (target.TakeHit(each))
                    landed++;
            }
            for (int i = 0; i < seenCount; i++)
                seen[i] = null;
            return landed;
        }

        public static int Circle(Vector2 origin, float radius, int layerMask, in HitData hit, IDamageable ignore = null) =>
            Arc(origin, Vector2.right, radius, 360f, layerMask, hit, ignore);

        static bool Contains(IDamageable[] list, int count, IDamageable item)
        {
            for (int i = 0; i < count; i++)
                if (list[i] == item)
                    return true;
            return false;
        }

        /// <summary>The mask of whatever the given team can hurt.</summary>
        public static int TargetsFor(Team source)
        {
            switch (source)
            {
                case Team.Player: return Layers.HurtableMask;
                case Team.Enemy: return Layers.PlayerMask;
                default: return Layers.PlayerMask | Layers.EnemyMask;
            }
        }
    }
}
