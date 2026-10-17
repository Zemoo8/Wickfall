using UnityEngine;

namespace Wickfall
{
    public enum HazardKind
    {
        WaxGeyser,       // R1: 10 dégâts, annoncés 1,2 s
        LightEaterBloom, // R2: lumière coupée 3 s
        Press,           // R3: 35 dégâts
        SteamJet,        // R3
        Conveyor,        // R3
        Soot,            // R3 Soufflets: -10 cire, dégâts x1,25 et perte de lumière 1,5 s
        Laser,           // R4: 25 dégâts
        GlassFloor,      // R4: casse sous la flamme cramoisie
        Ash,             // R6: cendre qui ralentit
        // R5's "Perte de cire x1,5" is RegionSO.waxLossMultiplier on the Creux Profond.
    }

    /// <summary>One obstacle of PDF §2.2 / §2.3. Fields left at 0 do not apply to that hazard.</summary>
    [CreateAssetMenu(menuName = "WICKFALL/Hazard", fileName = "Hazard")]
    public sealed class HazardSO : ScriptableObject
    {
        public HazardKind kind;
        public string nameKey = "hazard.geyser";

        [Header("PDF values")]
        [Tooltip("Wax removed per hit (geyser 10, press 35, laser 25, soot 10).")]
        public float damage;
        [Tooltip("Warning time before the hit (geyser 1.2 s).")]
        public float telegraphSeconds;
        [Tooltip("Light cut (Bloom 3 s) or light loss (soot 1.5 s).")]
        public float lightEffectSeconds;
        [Tooltip("Soot: damage taken x1.25 while sooted.")]
        public float damageTakenMultiplier = 1f;

        [Header("Proposals (G06)")]
        [Tooltip("Seconds between activations for cyclic hazards.")]
        public float period = 3f;
        [Tooltip("How long the hazard stays dangerous once it fires.")]
        public float activeSeconds = 0.35f;
        [Tooltip("Movement multiplier inside (ash, conveyor push speed for conveyors).")]
        public float moveMultiplier = 1f;
        public float pushSpeed;
        public float radius = 0.8f;
        [Tooltip("Status duration for effects whose length the PDF does not give.")]
        public float statusSeconds;
    }
}
