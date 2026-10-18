using UnityEngine;

namespace Wickfall.Combat
{
    /// <summary>Everything one hit carries. The target applies its own type rules (PDF §4 Hitbox and Damageable).</summary>
    public struct HitData
    {
        public float Amount;
        public Team Source;
        public bool HasFlame;
        public FlameId Flame;
        public Vector2 Origin;
        public Vector2 Direction;
        public float Knockback;
        public HitEffects Effects;
        public float SlowFactor;
        public float SlowSeconds;
        public float PoisonDps;
        public float PoisonSeconds;
        public float HollowBonus;
        public float StatusSeconds;
        public bool IsArt;
        public bool IsHazard;
        public SoundId ImpactSound;

        public static HitData FromEnemy(float amount, Vector2 origin, Vector2 direction, float knockback = 4f)
        {
            return new HitData
            {
                Amount = amount,
                Source = Team.Enemy,
                Origin = origin,
                Direction = direction,
                Knockback = knockback,
                HollowBonus = 1f,
                ImpactSound = SoundId.HitWax,
            };
        }

        public static HitData FromHazard(float amount, Vector2 origin, float knockback = 3f)
        {
            return new HitData
            {
                Amount = amount,
                Source = Team.Neutral,
                Origin = origin,
                Knockback = knockback,
                HollowBonus = 1f,
                IsHazard = true,
                ImpactSound = SoundId.HitWax,
            };
        }
    }

    public interface IDamageable
    {
        Team Team { get; }
        bool IsAlive { get; }
        DamageTags Tags { get; }
        Vector2 Position { get; }
        /// <summary>Returns true when the hit landed (not blocked, not invulnerable).</summary>
        bool TakeHit(in HitData hit);
    }
}
