using UnityEngine;
using Wickfall.Combat;

namespace Wickfall.Enemies
{
    /// <summary>
    /// PDF §2.2 Fonderies de Suie: "Soufflets (tirent de la suie, -10 cire)". A ranged machine that keeps
    /// its distance and blows soot balls: a hit removes 10 wax, then "sooted" for 1.5 s (damage ×1.25,
    /// light loss; PDF §2.3, G28).
    /// </summary>
    public sealed class BellowsBrain : EnemyBrain
    {
        public Projectile sootPrefab;
        public float retreatDistance = 2.4f;

        protected override void Start()
        {
            base.Start();
            if (sootPrefab != null)
                Pools.Prewarm(sootPrefab.gameObject, 4);
        }

        protected override void Update()
        {
            base.Update();
            // Back away from a player who closes in (only while chasing; it never flees off its leash).
            if (IsDead || !Fsm.Is(ChaseState) || !Perception.CanSeePlayer)
                return;
            Vector2 position = transform.position;
            Vector2 away = position - Perception.PlayerPosition;
            if (away.magnitude < retreatDistance)
                Motor.MoveTo(position + away.normalized * 1.5f, type.chaseSpeed);
        }

        protected override void PerformAttack(Vector2 direction)
        {
            var soot = Db.Hazard(HazardKind.Soot);
            var hit = HitData.FromEnemy(soot != null ? soot.damage : 10f, transform.position, direction, 2f); // PDF: -10 cire
            hit.Effects = HitEffects.Soot;
            Vector2 nozzle = (Vector2)transform.position + new Vector2(0f, 0.55f) + direction * 0.6f;
            Events.Play(SoundId.BellowsPuff, nozzle);
            Vfx.Play(VfxId.SootSplash, nozzle, direction);
            if (sootPrefab == null)
                return;
            var ball = Pools.Spawn(sootPrefab, nozzle, Quaternion.identity);
            ball.Launch(hit, direction, type.projectileSpeed, type.attackRange + 2f);
        }
    }
}
