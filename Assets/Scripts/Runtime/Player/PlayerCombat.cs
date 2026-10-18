using UnityEngine;
using Wickfall.Combat;
using Wickfall.Meta;
using Wickfall.Presentation;

namespace Wickfall.Player
{
    /// <summary>
    /// The player's attacks. The basic attack follows the flame's PDF role (amber melee arc, blue
    /// ranged bolt that slows, green poison that hits groups, white anti-Creux radiance, crimson
    /// low-wax strike); each flame also has one art (G02) that costs wax x the flame's
    /// "Coût en cire" (G01) and is unavailable while trimmed (PDF §2.1). Basic attacks are free.
    /// </summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        public Projectile boltPrefab;
        public Projectile artBoltPrefab;

        readonly float[] damageUpgrade = { 1f, 1f, 1f, 1f, 1f };
        readonly float[] artCostUpgrade = { 1f, 1f, 1f, 1f, 1f };
        float damageMultiplier = 1f;
        float artCostMultiplier = 1f;
        float nextAttack;
        float nextArt;

        WaxSystem wax;
        DimController dim;
        FlameSwitcher flames;

        void Awake()
        {
            wax = GetComponent<WaxSystem>();
            dim = GetComponent<DimController>();
            flames = GetComponent<FlameSwitcher>();
        }

        void Start()
        {
            if (boltPrefab != null) Pools.Prewarm(boltPrefab.gameObject, 6);
            if (artBoltPrefab != null) Pools.Prewarm(artBoltPrefab.gameObject, 2);
        }

        public void ApplyModifiers(in PlayerModifiers modifiers, SaveData save)
        {
            damageMultiplier = modifiers.DamageMultiplier;
            artCostMultiplier = modifiers.ArtCostMultiplier;
            for (int i = 0; i < WickfallCounts.Flames; i++)
            {
                damageUpgrade[i] = PlayerModifiers.FlameDamageUpgrade(save, (FlameId)i, 0.15f);
                artCostUpgrade[i] = PlayerModifiers.FlameArtCostUpgrade(save, (FlameId)i, 0.15f);
            }
        }

        public bool CanAttack => Time.time >= nextAttack && PresentationMode.IsEnabled(Feature.Combat);

        public float ArtCost(FlameId flame) => Db.Flame(flame).ArtWaxCost * artCostUpgrade[(int)flame] * artCostMultiplier;

        /// <summary>Why an art cannot be used right now (null when it can).</summary>
        public string ArtBlockedReason()
        {
            if (!PresentationMode.IsEnabled(Feature.FlameArts)) return "hidden";
            if (!dim.ArtsAllowed) return "trimmed";      // PDF: "plus d'arts de flamme"
            if (Time.time < nextArt) return "cooldown";
            if (wax.Current - ArtCost(flames.Current) <= 0f) return "wax";
            return null;
        }

        public bool CanArt => ArtBlockedReason() == null;

        /// <summary>Total length of the attack animation, in seconds.</summary>
        public float AttackDuration(bool art) => art ? 0.42f : flames.CurrentType.attackDuration;

        /// <summary>When, inside the animation, the hit happens.</summary>
        public float HitMoment(bool art) => AttackDuration(art) * (art ? 0.45f : 0.4f);

        /// <summary>Starts an attack: pays the art's wax and the cooldowns. Returns false if it can't.</summary>
        public bool Begin(bool art)
        {
            var flame = flames.CurrentType;
            if (art)
            {
                if (!CanArt || !wax.TrySpend(ArtCost(flame.id)))
                {
                    Events.PlayUi(SoundId.UiError);
                    return false;
                }
                nextArt = Time.time + flame.artCooldown;
                nextAttack = Time.time + AttackDuration(true);
                Events.Play(SoundId.ArtCast, transform.position);
            }
            else
            {
                if (!CanAttack)
                    return false;
                nextAttack = Time.time + flame.attackDuration + flame.attackCooldown;
                Events.Play(SoundId.AttackSwing, transform.position, 0.8f, 0.9f + 0.2f * Random.value);
            }
            return true;
        }

        public float Damage(FlameTypeSO flame, bool art)
        {
            float damage = CombatMath.PlayerOutgoing(Db.Balance.baseDamage, flame, wax.Current, Db.Wax.maxWax, dim.IsTrimmed,
                Db.Dim.damageMultiplier, damageMultiplier * damageUpgrade[(int)flame.id]);
            return art ? damage * flame.artDamageMultiplier : damage;
        }

        HitData MakeHit(FlameTypeSO flame, bool art, Vector2 direction)
        {
            var hit = new HitData
            {
                Amount = Damage(flame, art),
                Source = Team.Player,
                HasFlame = true,
                Flame = flame.id,
                Origin = transform.position,
                Direction = direction,
                Knockback = Db.Balance.knockback * (art ? 1.5f : 1f),
                HollowBonus = flame.hollowBonus,
                IsArt = art,
                ImpactSound = SoundId.HitWax,
            };
            if (flame.slowSeconds > 0f)
            {
                hit.Effects |= HitEffects.Slow;
                hit.SlowFactor = flame.slowFactor;
                hit.SlowSeconds = flame.slowSeconds * (art ? 1.5f : 1f);
            }
            if (flame.poisonSeconds > 0f)
            {
                hit.Effects |= HitEffects.Poison;
                hit.PoisonDps = flame.poisonDamagePerSecond * (art ? 1.5f : 1f);
                hit.PoisonSeconds = flame.poisonSeconds;
            }
            return hit;
        }

        /// <summary>The hit itself (called by the attack state at the hit moment).</summary>
        public void Execute(bool art, Vector2 direction)
        {
            var flame = flames.CurrentType;
            Vector2 position = transform.position + new Vector3(0f, 0.45f, 0f); // chest height
            var hit = MakeHit(flame, art, direction);
            int landed = 0;

            Events.PlayerActed.Raise(new PlayerActionInfo
            {
                Kind = art ? PlayerActionKind.Art : PlayerActionKind.Attack,
                Direction = direction, Flame = flame.id, Position = position, Time = Time.time,
            });

            if (!art)
            {
                switch (flame.attackStyle)
                {
                    case FlameAttackStyle.Bolt:
                        Fire(boltPrefab, hit, position, direction, flame.projectileSpeed, flame.projectileRange);
                        Vfx.Play(VfxId.Slash, position + direction * 0.4f, direction, flame.bodyColor, 0.6f);
                        break;
                    default:
                        Vector2 center = position + direction * 0.35f;
                        landed = HitQuery.Arc(center, direction, flame.attackRange, flame.attackArcDegrees, Layers.HurtableMask, hit);
                        Vfx.Play(flame.attackStyle == FlameAttackStyle.PoisonCloud ? VfxId.PoisonCloud : VfxId.Slash,
                            position + direction * 0.7f, direction, flame.bodyColor,
                            flame.attackStyle == FlameAttackStyle.PoisonCloud ? 1.1f : 1f);
                        break;
                }
            }
            else
            {
                switch (flame.id)
                {
                    case FlameId.Amber: // a wide arc sweep
                        landed = HitQuery.Circle(position, flame.artRadius, Layers.HurtableMask, hit);
                        Vfx.Play(VfxId.WideSweep, position, direction, flame.bodyColor, flame.artRadius / 2f);
                        break;
                    case FlameId.Blue: // a piercing bolt that slows
                        Fire(artBoltPrefab != null ? artBoltPrefab : boltPrefab, hit, position, direction, flame.projectileSpeed * 1.2f,
                            flame.projectileRange * 1.4f);
                        break;
                    case FlameId.Green: // a poison burst that hits groups
                        landed = HitQuery.Circle(position, flame.artRadius, Layers.HurtableMask, hit);
                        Vfx.Play(VfxId.PoisonCloud, position, direction, flame.bodyColor, flame.artRadius / 1.2f);
                        break;
                    case FlameId.White: // a radiant pulse that hurts Creux, and a lot of light
                        landed = HitQuery.Circle(position, flame.artRadius, Layers.HurtableMask, hit);
                        Vfx.Play(VfxId.RadiantPulse, position, direction, flame.coreColor, flame.artRadius / 1.5f);
                        Events.Flare.Raise(new FlareInfo(position, flame.artRadius * 2f, FlareKind.Art, flame.lightColor));
                        break;
                    default: // crimson: a strike whose power grows as wax runs low
                        landed = HitQuery.Arc(position + direction * 0.4f, direction, flame.artRadius, 120f, Layers.HurtableMask, hit);
                        Vfx.Play(VfxId.CrimsonStrike, position + direction * 0.9f, direction, flame.bodyColor, 1.2f);
                        break;
                }
            }

            if (landed > 0 && PresentationMode.IsEnabled(Feature.CameraFeedback))
            {
                Events.HitStop(Db.Balance.hitStopSeconds * (art ? 1.6f : 1f));
                Events.Shake(art ? 0.18f : 0.08f, 0.12f);
            }
        }

        void Fire(Projectile prefab, in HitData hit, Vector2 position, Vector2 direction, float speed, float range)
        {
            if (prefab == null)
                return;
            var projectile = Pools.Spawn(prefab, position + direction * 0.5f, Quaternion.identity);
            projectile.Launch(hit, direction, speed, range);
            Events.Play(SoundId.BoltFire, position, 0.8f);
        }
    }
}
