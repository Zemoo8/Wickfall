using UnityEngine;

namespace Wickfall
{
    public enum FlameAttackStyle
    {
        MeleeArc,     // Ambre: "Équilibrée, arc de mêlée"
        Bolt,         // Bleue: "À distance, ralentit"
        PoisonCloud,  // Verte: "Poison, touche les groupes"
        RadiantSlash, // Blanche: "Anti-Creux, énorme lumière"
        HeavyStrike,  // Cramoisie: "Plus de dégâts à faible cire"
    }

    /// <summary>
    /// One row of the PDF §2.1 flame table ("Chaque flamme est une donnée réglable", PDF §4).
    /// Cost, damage and light multipliers are the PDF's; the attack tuning is a proposal (G02, G06).
    /// </summary>
    [CreateAssetMenu(menuName = "WICKFALL/Flame Type", fileName = "Flame")]
    public sealed class FlameTypeSO : ScriptableObject
    {
        public FlameId id;
        [Tooltip("Localization keys.")]
        public string nameKey = "flame.amber";
        public string roleKey = "flame.amber.role";

        [Header("PDF §2.1 table")]
        [Tooltip("\"Coût en cire\". G01: multiplies passive wax loss while lit, and this flame's art cost.")]
        public float waxCostMultiplier = 1f;

        [Tooltip("\"Dégâts\". For the crimson flame see the low-wax curve below.")]
        public float damageMultiplier = 1f;

        [Tooltip("Cramoisie: damage follows wax (x0.6 at full wax up to x2.2 at none).")]
        public bool damageScalesWithLowWax;
        public float damageAtEmptyWax = 2.2f;
        public float damageAtFullWax = 0.6f;

        [Tooltip("\"Lumière\".")]
        public float lightMultiplier = 1f;

        [Header("Colors (the only saturated hues in the game)")]
        public Color coreColor = new Color(1f, 0.914f, 0.659f);
        public Color bodyColor = new Color(1f, 0.757f, 0.302f);
        public Color edgeColor = new Color(0.949f, 0.541f, 0.114f);
        public Color lightColor = new Color(1f, 0.78f, 0.45f);

        [Header("Basic attack (G02, G06 - proposals)")]
        public FlameAttackStyle attackStyle = FlameAttackStyle.MeleeArc;
        public float attackRange = 1.35f;
        [Range(10f, 360f)] public float attackArcDegrees = 120f;
        public float attackDuration = 0.28f;
        public float attackCooldown = 0.12f;
        public float projectileSpeed = 11f;
        public float projectileRange = 7f;

        [Header("Effects")]
        [Range(0f, 1f)] public float slowFactor = 0.5f;
        public float slowSeconds = 2f;
        public float poisonDamagePerSecond = 0f;
        public float poisonSeconds = 0f;
        [Tooltip("Extra multiplier against Creux creatures (the white flame is \"anti-Creux\", G30).")]
        public float hollowBonus = 1f;

        [Header("Flame art (G02)")]
        public string artNameKey = "art.amber";
        [Tooltip("Base wax cost of the art; multiplied by waxCostMultiplier (G01).")]
        public float artBaseWaxCost = 6f;
        public float artDamageMultiplier = 2.2f;
        public float artRadius = 2.2f;
        public float artCooldown = 0.9f;

        [Header("Unlock (G03)")]
        public RegionId unlockedIn = RegionId.TallowFields;
        public bool unlockedAtStart;

        /// <summary>The art's wax cost (G01: base cost x the flame's "Coût en cire").</summary>
        public float ArtWaxCost => artBaseWaxCost * waxCostMultiplier;

        /// <summary>PDF damage multiplier at this much wax (only the crimson flame depends on wax).</summary>
        public float DamageMultiplierAt(float wax, float pdfMaxWax) =>
            damageScalesWithLowWax
                ? CombatMath.CrimsonMultiplier(wax, pdfMaxWax, damageAtEmptyWax, damageAtFullWax)
                : damageMultiplier;
    }
}
