using System.Collections.Generic;
using UnityEngine;
using Wickfall.Combat;
using Wickfall.Presentation;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §2.2 Cathédrale de Verre: "Lasers (25 dégâts)". An emitter charges (a thin flickering line and
    /// a rising whine: every telegraph is audible), then fires a burning beam that black mirrors bounce.
    /// Each firing burns each creature or player once.
    /// </summary>
    public sealed class LaserEmitter : MonoBehaviour
    {
        public HazardSO hazard;
        public Vector2 direction = Vector2.right;
        public BeamLine line;
        public float startOffset = 0.55f;
        public float phaseOffset;

        public enum Phase { Idle, Charging, Firing }
        public Phase Current { get; private set; }
        public bool IsFiring => Current == Phase.Firing;
        public bool IsCharging => Current == Phase.Charging;

        float timer;
        readonly Vector2[] points = new Vector2[BeamTracer.MaxPoints];
        int pointCount;
        static readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        readonly IDamageable[] burned = new IDamageable[16];
        int burnedCount;

        float IdleSeconds => Mathf.Max(0.4f, hazard.period - hazard.telegraphSeconds - hazard.activeSeconds);

        static readonly List<LaserEmitter> all = new List<LaserEmitter>(16);
        public static IReadOnlyList<LaserEmitter> All => all;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        /// <summary>Seconds until the beam next burns (0 while it does).</summary>
        public float SecondsUntilFire
        {
            get
            {
                if (hazard == null) return float.MaxValue;
                switch (Current)
                {
                    case Phase.Idle: return Mathf.Max(0f, IdleSeconds - timer) + hazard.telegraphSeconds;
                    case Phase.Charging: return Mathf.Max(0f, hazard.telegraphSeconds - timer);
                    default: return 0f;
                }
            }
        }

        public int PointCount => pointCount;
        public Vector2 Point(int i) => points[i];

        /// <summary>Distance from a point to the beam's path (its last trace).</summary>
        public float DistanceToPath(Vector2 p)
        {
            float best = float.MaxValue;
            for (int s = 0; s + 1 < pointCount; s++)
            {
                Vector2 a = points[s], b = points[s + 1], ab = b - a;
                float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }

        void Start()
        {
            if (hazard == null)
                hazard = Db.Hazard(HazardKind.Laser);
            timer = -phaseOffset;
            Enter(Phase.Idle);
        }

        void Update()
        {
            if (!PresentationMode.IsEnabled(Feature.Hazards))
            {
                line?.Hide();
                return;
            }
            timer += Time.deltaTime;
            switch (Current)
            {
                case Phase.Idle:
                    if (timer >= IdleSeconds) Enter(Phase.Charging);
                    break;
                case Phase.Charging:
                    if (timer >= hazard.telegraphSeconds) Enter(Phase.Firing); // PDF: 25 dégâts, after a warning
                    break;
                case Phase.Firing:
                    if (timer >= hazard.activeSeconds) Enter(Phase.Idle);
                    break;
            }
        }

        void LateUpdate()
        {
            Vector2 dir = direction.normalized;
            pointCount = BeamTracer.Trace(BeamKind.Laser, (Vector2)transform.position + dir * startOffset, dir, points);
            if (Current == Phase.Idle || line == null)
                return;
            if (Current == Phase.Charging)
            {
                float flicker = Random.value < 0.75f ? 1f : 0f;
                line.Show(points, pointCount, 0.25f, 0.35f + 0.4f * timer / hazard.telegraphSeconds * flicker);
            }
            else
            {
                line.Show(points, pointCount, 1f, 1f);
            }
        }

        void FixedUpdate()
        {
            if (Current != Phase.Firing || pointCount < 2)
                return;
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = Layers.PlayerMask | Layers.EnemyMask };
            for (int s = 0; s + 1 < pointCount; s++)
            {
                Vector2 a = points[s], b = points[s + 1];
                Vector2 d = b - a;
                float length = d.magnitude;
                if (length < 0.01f)
                    continue;
                int count = Physics2D.CircleCast(a, hazard.radius, d / length, filter, hits, length);
                for (int i = 0; i < count; i++)
                {
                    var c = hits[i].collider;
                    hits[i] = default;
                    if (c == null || !c.TryGetComponent(out Hurtbox hurtbox))
                        continue;
                    Burn(hurtbox.Owner, d / length);
                }
            }
        }

        void Burn(IDamageable target, Vector2 along)
        {
            if (target == null || !target.IsAlive)
                return;
            for (int i = 0; i < burnedCount; i++)
                if (burned[i] == target)
                    return;
            if (burnedCount < burned.Length)
                burned[burnedCount++] = target;
            var hit = HitData.FromHazard(hazard.damage, target.Position - along, 4f);
            hit.Direction = new Vector2(-along.y, along.x);
            target.TakeHit(hit);
            Vfx.Play(VfxId.LaserImpact, target.Position + new Vector2(0f, 0.5f));
        }

        void Enter(Phase next)
        {
            Current = next;
            timer = Current == Phase.Idle && timer < 0f ? timer : 0f;
            switch (next)
            {
                case Phase.Idle:
                    line?.Hide();
                    break;
                case Phase.Charging:
                    Events.Play(SoundId.LaserCharge, transform.position);
                    break;
                case Phase.Firing:
                    for (int i = 0; i < burnedCount; i++)
                        burned[i] = null;
                    burnedCount = 0;
                    Events.Play(SoundId.LaserFire, transform.position);
                    Events.Shake(0.05f, 0.15f);
                    break;
            }
        }
    }
}
