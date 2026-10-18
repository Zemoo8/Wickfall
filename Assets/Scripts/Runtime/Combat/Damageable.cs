using System;
using UnityEngine;

namespace Wickfall.Combat
{
    /// <summary>
    /// Health for enemies and breakable things. The target applies the multipliers of its type
    /// (PDF §4): Scorifiés ignore every flame but white and green, glass only breaks under the
    /// crimson flame, Creux creatures take the white flame's anti-Creux bonus. Also runs the blue
    /// flame's slow and the green flame's poison.
    /// </summary>
    public class Damageable : MonoBehaviour, IDamageable
    {
        public Team team = Team.Enemy;
        public float maxHealth = 20f;
        public DamageTags tags;
        [Range(0f, 1f)] public float knockbackResistance;
        [Tooltip("Optional: shown in hit reports and kill events.")]
        public EnemyKind kind = EnemyKind.Dripling;
        public bool reportsAsEnemy = true;

        public float Health { get; private set; }
        public bool IsAlive => Health > 0f;
        public Team Team => team;
        public DamageTags Tags => tags;
        public Vector2 Position => transform.position;
        public float Normalized => maxHealth > 0f ? Health / maxHealth : 0f;

        // Status effects.
        public float SlowMultiplier => Time.time < slowUntil ? slowFactor : 1f;
        public bool IsPoisoned => Time.time < poisonUntil;
        float slowFactor = 1f;
        float slowUntil;
        float poisonDps;
        float poisonUntil;
        float poisonTick;
        float invulnerableUntil;
        HitData lastHit;

        public Vector2 PendingKnockback { get; set; }

        /// <summary>Raised when a hit lands (hit, damage taken).</summary>
        public event Action<HitData, float> Damaged;
        /// <summary>Raised when a hit is blocked by the type rules (wrong flame).</summary>
        public event Action<HitData> Blocked;
        public event Action<HitData> Died;

        protected virtual void Awake()
        {
            Health = maxHealth;
            foreach (var hurtbox in GetComponentsInChildren<Hurtbox>(true))
                hurtbox.Bind(this);
        }

        public void ResetHealth()
        {
            Health = maxHealth;
            slowUntil = poisonUntil = invulnerableUntil = 0f;
            PendingKnockback = Vector2.zero;
        }

        public void SetInvulnerable(float seconds) => invulnerableUntil = Time.time + seconds;

        public virtual bool TakeHit(in HitData hit)
        {
            if (!IsAlive || Time.time < invulnerableUntil)
                return false;

            float amount = CombatMath.TargetTakes(hit.Amount, tags, hit.HasFlame, hit.Flame, hit.HollowBonus);
            if (amount <= 0f && hit.Amount > 0f)
            {
                Blocked?.Invoke(hit);
                Events.EnemyHit.Raise(new HitReport { Position = Position, Amount = 0f, Blocked = true, Flame = hit.Flame, Victim = team });
                Events.Play(SoundId.HitBlocked, Position);
                return false;
            }

            if (hit.HasFlame && CombatMath.CanHurt(tags, true, hit.Flame))
            {
                if ((hit.Effects & HitEffects.Slow) != 0)
                {
                    slowFactor = Mathf.Min(SlowMultiplier, hit.SlowFactor);
                    slowUntil = Time.time + hit.SlowSeconds;
                }
                if ((hit.Effects & HitEffects.Poison) != 0)
                {
                    poisonDps = Mathf.Max(IsPoisoned ? poisonDps : 0f, hit.PoisonDps);
                    poisonUntil = Time.time + hit.PoisonSeconds;
                }
            }

            PendingKnockback += hit.Direction * hit.Knockback * (1f - knockbackResistance);
            lastHit = hit;
            Apply(amount, hit);
            return true;
        }

        void Apply(float amount, in HitData hit)
        {
            Health = Mathf.Max(0f, Health - amount);
            bool killed = Health <= 0f;
            Damaged?.Invoke(hit, amount);
            Events.EnemyHit.Raise(new HitReport { Position = Position, Amount = amount, Flame = hit.Flame, Victim = team, Killed = killed });
            if (killed)
                Died?.Invoke(hit);
        }

        protected virtual void Update()
        {
            if (!IsAlive || !IsPoisoned)
                return;
            poisonTick += Time.deltaTime;
            if (poisonTick >= 0.5f)
            {
                poisonTick -= 0.5f;
                var tick = lastHit;
                tick.Effects = HitEffects.None;
                tick.Knockback = 0f;
                Apply(poisonDps * 0.5f, tick);
            }
        }
    }
}
