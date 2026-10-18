using UnityEngine;
using Wickfall.Combat;

namespace Wickfall.Enemies
{
    /// <summary>
    /// PDF §2.2 Bois-Mèche: "Carcasses (embuscade)" — Husks hang from the branches like empty bark,
    /// drop when you pass close (or when struck) and lunge with raking claws.
    /// </summary>
    public sealed class HuskBrain : EnemyBrain
    {
        public float dropRadius = 2.6f;
        public float dropSeconds = 0.6f;

        State<EnemyBrain> hanging, dropping;

        protected override void CreateStates()
        {
            base.CreateStates();
            hanging = Fsm.Add(new Hanging());
            dropping = Fsm.Add(new Dropping());
        }

        protected override State<EnemyBrain> InitialState => hanging;

        public bool IsHanging => Fsm.Is(hanging);

        protected override void OnDamaged(HitData hit, float amount)
        {
            base.OnDamaged(hit, amount);
            if (!IsDead && Fsm.Is(hanging))
                Fsm.Change(dropping);
        }

        protected override void PerformAttack(Vector2 direction)
        {
            base.PerformAttack(direction);
            Motor.Teleport((Vector2)transform.position + direction * 0.5f); // the lunge
        }

        sealed class Hanging : State<EnemyBrain>
        {
            public override string Name => "Hanging";
            public override void Enter()
            {
                Owner.Motor.Stop();
                ((HuskBrain)Owner).SetHang();
            }

            public override void Tick(float dt)
            {
                var o = (HuskBrain)Owner;
                var p = o.Perception;
                // It drops when the player passes close enough to be felt (and is not hidden by a trimmed wick).
                if (p.InDetectionRange && p.DistanceToPlayer <= o.dropRadius)
                    o.Fsm.Change(o.dropping);
            }
        }

        sealed class Dropping : State<EnemyBrain>
        {
            public override string Name => "Dropping";
            public override void Enter()
            {
                var o = (HuskBrain)Owner;
                o.SetDrop();
                Events.Play(SoundId.HuskDrop, o.transform.position);
            }

            public override void Tick(float dt)
            {
                var o = (HuskBrain)Owner;
                if (o.Fsm.TimeInState >= o.dropSeconds)
                {
                    o.Perception.Reveal(o.Perception.PlayerPosition);
                    o.Fsm.Change(o.ChaseState);
                }
            }
        }

        void SetHang() => SetAnim(EnemyAnim.Special);
        void SetDrop() => SetAnim(EnemyAnim.Windup);
    }
}
