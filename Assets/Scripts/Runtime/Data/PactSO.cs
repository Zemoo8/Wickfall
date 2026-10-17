using UnityEngine;

namespace Wickfall
{
    public enum PactStat
    {
        None,
        MaxWax,          // added
        WaxLoss,         // multiplier on passive loss
        DamageDealt,     // multiplier
        DetectionRadius, // multiplier
        LightRadius,     // multiplier
        ShardGain,       // multiplier
        HitWaxPenalty,   // extra wax lost per hit taken
        KillWax,         // multiplier on kill rewards
    }

    /// <summary>
    /// PDF §2.4 "Pactes du Roi Creux (optionnels) : puissance contre un coût permanent
    /// (ex. +30 cire maximale, perte x1,5)". The first pact is the PDF's example word for word (G11).
    /// </summary>
    [CreateAssetMenu(menuName = "WICKFALL/Pact", fileName = "Pact")]
    public sealed class PactSO : ScriptableObject
    {
        [Range(0, 7)] public int index;
        public string nameKey = "pact.0";
        public string descriptionKey = "pact.0.desc";
        public bool fromPdf;

        [Header("Gain")]
        public PactStat gainStat = PactStat.MaxWax;
        public float gainValue = 30f;

        [Header("Permanent cost")]
        public PactStat costStat = PactStat.WaxLoss;
        public float costValue = 1.5f;

        [Tooltip("The pact shrine that offers it.")]
        public RegionId offeredIn = RegionId.DeepHollow;
    }
}
