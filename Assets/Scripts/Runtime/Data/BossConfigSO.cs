using UnityEngine;

namespace Wickfall
{
    /// <summary>
    /// PDF §4 BossHollowKing: "Trois phases qui retirent chacune un appui (pas de mode faible, puis cire
    /// maximale à 40)". Phase 1's removal is not named by the PDF (G13: the arena's wax pools go dark).
    /// </summary>
    [CreateAssetMenu(menuName = "WICKFALL/Config/Boss (Roi Creux)", fileName = "BossConfig")]
    public sealed class BossConfigSO : ScriptableObject
    {
        [Header("Supports removed per phase")]
        [Tooltip("G13 proposal: phase 1 turns the arena's wax pools off.")]
        public bool phase1DisablesWaxPools = true;
        [Tooltip("PDF: \"pas de mode faible\" (phase 2).")]
        public bool phase2DisablesTrim = true;
        [Tooltip("PDF: \"cire maximale à 40\" (phase 3).")]
        public float phase3MaxWax = 40f;

        [Header("Health (G06 proposal)")]
        public float healthPerPhase = 200f;

        [Header("Attacks (G06 proposals, from the PDF's themes: slam, ash, negative light, voids)")]
        public float slamDamage = 20f;
        public float slamRadius = 3.2f;
        public float slamWindup = 1.0f;
        public float voidDamage = 12f;
        public float voidSpeed = 4.5f;
        public int voidsPerVolley = 6;
        public float ashWaveDamage = 10f;
        public float ashSlowMultiplier = 0.55f;
        public float negativePulseRadius = 5.5f;
        public float negativePulseSeconds = 2.4f;
        public float timeBetweenAttacks = 2f;
        [Tooltip("Each phase attacks this much faster than the previous one.")]
        public float phaseSpeedup = 0.85f;
        public float moveSpeed = 1.4f;
        public float contactDamage = 10f;
    }
}
