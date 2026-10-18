using UnityEngine;
using Wickfall.Combat;
using Wickfall.World;

namespace Wickfall.Enemies
{
    /// <summary>
    /// Moves a creature: straight at the target when nothing is in the way, along an A* path on the
    /// region's grid otherwise. Applies knockback from hits and the blue flame's slow.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyMotor : MonoBehaviour
    {
        public float acceleration = 30f;
        public float bodyRadius = 0.4f;

        Rigidbody2D rb;
        Damageable body;
        readonly Vector2[] path = new Vector2[48];
        int pathCount;
        int pathIndex;
        Vector2 pathGoal;
        float repathAt;
        Vector2 desired;
        Vector2 knockback;
        static readonly RaycastHit2D[] hits = new RaycastHit2D[2];
        ContactFilter2D wallFilter;

        public Vector2 Facing { get; private set; } = Vector2.down;
        public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;
        public bool Frozen { get; set; }
        public float ExternalSpeedMultiplier { get; set; } = 1f;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            body = GetComponent<Damageable>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            wallFilter.useLayerMask = true;
            wallFilter.layerMask = Layers.WallMask;
            wallFilter.useTriggers = false;
        }

        float SpeedMultiplier => (body != null ? body.SlowMultiplier : 1f) * ExternalSpeedMultiplier;

        public void Stop() => desired = Vector2.zero;

        public void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
                Facing = direction.normalized;
        }

        /// <summary>Heads for a point. Returns the remaining distance.</summary>
        public float MoveTo(Vector2 target, float speed)
        {
            Vector2 position = rb.position;
            Vector2 to = target - position;
            float distance = to.magnitude;
            if (distance < 0.05f)
            {
                desired = Vector2.zero;
                return distance;
            }

            Vector2 direction;
            if (Clear(position, target))
            {
                direction = to / distance;
                pathCount = 0;
            }
            else
            {
                var nav = NavGrid.Active;
                if (nav != null && (pathCount == 0 || Time.time >= repathAt || (pathGoal - target).sqrMagnitude > 1f))
                {
                    pathCount = nav.FindPath(position, target, path, 1500);
                    pathIndex = 0;
                    pathGoal = target;
                    repathAt = Time.time + 0.6f + Random.value * 0.3f;
                }
                while (pathIndex < pathCount && (path[pathIndex] - position).sqrMagnitude < 0.09f)
                    pathIndex++;
                Vector2 waypoint = pathIndex < pathCount ? path[pathIndex] : target;
                // skip ahead when the next-but-one waypoint is directly reachable (smoother paths)
                if (pathIndex + 1 < pathCount && Clear(position, path[pathIndex + 1]))
                    waypoint = path[++pathIndex];
                Vector2 w = waypoint - position;
                direction = w.sqrMagnitude > 0.0001f ? w.normalized : Vector2.zero;
            }

            desired = direction * speed * SpeedMultiplier;
            Face(direction);
            return distance;
        }

        /// <summary>Moves away from a point (fleeing, kiting).</summary>
        public void MoveAway(Vector2 from, float speed)
        {
            Vector2 away = (Vector2)rb.position - from;
            if (away.sqrMagnitude < 0.0001f)
                away = Random.insideUnitCircle;
            Vector2 target = (Vector2)rb.position + away.normalized * 2f;
            if (!Clear(rb.position, target))
                target = (Vector2)rb.position + new Vector2(-away.y, away.x).normalized * 2f;
            MoveTo(target, speed);
        }

        public bool Clear(Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            float length = d.magnitude;
            if (length < 0.01f)
                return true;
            int count = Physics2D.CircleCast(from, bodyRadius * 0.9f, d / length, wallFilter, hits, length);
            for (int i = 0; i < count; i++)
                hits[i] = default;
            return count == 0;
        }

        Vector2 push;
        Vector2 lastPush;
        int pushStep = -1;

        /// <summary>An outside push for this physics step (conveyors, steam), added on top of its own walk.</summary>
        public void AddPush(Vector2 velocity)
        {
            if (pushStep != stepCount)
            {
                push = Vector2.zero;
                pushStep = stepCount;
            }
            push += velocity;
        }

        int stepCount;

        public void Teleport(Vector2 position)
        {
            rb.position = position;
            transform.position = position;
            rb.linearVelocity = Vector2.zero;
            pathCount = 0;
        }

        void FixedUpdate()
        {
            if (body != null && body.PendingKnockback.sqrMagnitude > 0f)
            {
                knockback += body.PendingKnockback;
                body.PendingKnockback = Vector2.zero;
            }
            Vector2 target = Frozen ? Vector2.zero : desired;
            Vector2 outside = pushStep == stepCount ? push : Vector2.zero;
            Vector2 velocity = Vector2.MoveTowards(rb.linearVelocity - knockback - lastPush, target, acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = velocity + knockback + outside;
            lastPush = outside;
            stepCount++;
            knockback = Vector2.MoveTowards(knockback, Vector2.zero, 30f * Time.fixedDeltaTime);
        }
    }
}
