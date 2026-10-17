using UnityEngine;

namespace Wickfall.Meta
{
    /// <summary>
    /// Everything relics (PDF §2.4), pacts (PDF §2.4) and flame upgrades (G19) change on the player,
    /// computed from the save as one plain struct so the systems and the tests read the same numbers.
    /// </summary>
    public struct PlayerModifiers
    {
        public float MaxWax;
        public float WaxLossMultiplier;
        public float DamageMultiplier;
        public float DetectionMultiplier;
        public float LightMultiplier;
        public float ShardMultiplier;
        public float KillWaxMultiplier;
        public float ExtraWaxPerHit;
        public int DodgeCharges;
        public int FlameSlots;
        public bool WallGrip;
        public float SpareWickWax; // 0 = no Spare Wick relic
        public float ArtCostMultiplier;

        public static PlayerModifiers Compute(SaveData save, GameDatabase db)
        {
            var m = new PlayerModifiers
            {
                MaxWax = db.wax.maxWax,
                WaxLossMultiplier = 1f,
                DamageMultiplier = 1f,
                DetectionMultiplier = 1f,
                LightMultiplier = 1f,
                ShardMultiplier = 1f,
                KillWaxMultiplier = 1f,
                ExtraWaxPerHit = 0f,
                DodgeCharges = db.balance.dodgeCharges,
                FlameSlots = 4, // G03: 4 slots, the flame-slot relic adds the 5th
                ArtCostMultiplier = 1f,
            };

            for (int i = 0; i < db.relics.Length; i++)
            {
                var relic = db.relics[i];
                if (relic == null || save == null || !save.HasRelic(relic.index))
                    continue;
                switch (relic.effect)
                {
                    case RelicEffect.ExtraDodge: m.DodgeCharges += Mathf.RoundToInt(relic.value); break;
                    case RelicEffect.WallGrip: m.WallGrip = true; break;
                    case RelicEffect.MaxWax: m.MaxWax += relic.value; break;
                    case RelicEffect.FlameSlot: m.FlameSlots += Mathf.RoundToInt(relic.value); break;
                    case RelicEffect.SpareWick: m.SpareWickWax = relic.value; break;
                    case RelicEffect.HearthEmber: m.ArtCostMultiplier *= 1f - relic.value; break;
                }
            }

            for (int i = 0; i < db.pacts.Length; i++)
            {
                var pact = db.pacts[i];
                if (pact == null || save == null || !save.HasPact(pact.index))
                    continue;
                Apply(ref m, pact.gainStat, pact.gainValue);
                Apply(ref m, pact.costStat, pact.costValue);
            }

            m.FlameSlots = Mathf.Clamp(m.FlameSlots, 1, WickfallCounts.Flames);
            m.MaxWax = Mathf.Max(10f, m.MaxWax);
            return m;
        }

        static void Apply(ref PlayerModifiers m, PactStat stat, float value)
        {
            switch (stat)
            {
                case PactStat.MaxWax: m.MaxWax += value; break;
                case PactStat.WaxLoss: m.WaxLossMultiplier *= value; break;
                case PactStat.DamageDealt: m.DamageMultiplier *= value; break;
                case PactStat.DetectionRadius: m.DetectionMultiplier *= value; break;
                case PactStat.LightRadius: m.LightMultiplier *= value; break;
                case PactStat.ShardGain: m.ShardMultiplier *= value; break;
                case PactStat.HitWaxPenalty: m.ExtraWaxPerHit += value; break;
                case PactStat.KillWax: m.KillWaxMultiplier *= value; break;
            }
        }

        /// <summary>Damage upgrade multiplier for a flame (G19: two tiers).</summary>
        public static float FlameDamageUpgrade(SaveData save, FlameId flame, float perTier) =>
            1f + perTier * (save != null ? save.flameDamageTier[(int)flame] : 0);

        /// <summary>Art cost upgrade multiplier for a flame (G19: two tiers).</summary>
        public static float FlameArtCostUpgrade(SaveData save, FlameId flame, float perTier) =>
            Mathf.Max(0.2f, 1f - perTier * (save != null ? save.flameArtTier[(int)flame] : 0));
    }
}
