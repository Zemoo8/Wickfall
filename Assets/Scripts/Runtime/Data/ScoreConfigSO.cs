using UnityEngine;

namespace Wickfall
{
    /// <summary>
    /// PDF §2.4 "Score de région (pour rejouer) : cire restante en % x 10, plus bonus de temps, plus 500
    /// par relique, plus 20 par ennemi évité sans combat, moins 150 par mort. Rangs : Cendre, Braise,
    /// Flamme, Brasier. Valeurs à ajuster en test." The PDF allows tuning these after testing.
    /// </summary>
    [CreateAssetMenu(menuName = "WICKFALL/Config/Score", fileName = "ScoreConfig")]
    public sealed class ScoreConfigSO : ScriptableObject
    {
        [Header("PDF §2.4 formula")]
        public float waxPercentFactor = 10f;
        public int pointsPerRelic = 500;
        public int pointsPerAvoidedEnemy = 20;
        public int pointsPerDeath = -150;

        [Header("Time bonus (G09): max(0, par - time) x pointsPerSecondUnderPar")]
        public float pointsPerSecondUnderPar = 1f;

        [Header("Rank thresholds (G09, tuned from bot and playtest spreads)")]
        [Tooltip("Minimum score for Braise, Flamme and Brasier. Anything lower is Cendre.")]
        public int braiseMin = 700;
        public int flammeMin = 1200;
        public int brasierMin = 1700;
    }
}
