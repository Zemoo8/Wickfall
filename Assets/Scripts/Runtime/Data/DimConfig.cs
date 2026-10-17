using UnityEngine;

namespace Wickfall
{
    /// <summary>
    /// PDF §2.1 "Tailler la mèche (maintenir une touche) : rayon -65 %, perte -75 %, détection -60 %,
    /// dégâts infligés -50 %, plus d'arts de flamme. C'est un mode « pauvreté », pas une invisibilité."
    /// The fields store the resulting multipliers.
    /// </summary>
    [CreateAssetMenu(menuName = "WICKFALL/Config/Dim (trim the wick)", fileName = "DimConfig")]
    public sealed class DimConfig : ScriptableObject
    {
        [Tooltip("rayon -65 %  ->  x0.35")]
        public float radiusMultiplier = 0.35f;

        [Tooltip("perte -75 %  ->  x0.25")]
        public float waxLossMultiplier = 0.25f;

        [Tooltip("détection -60 %  ->  x0.4")]
        public float detectionMultiplier = 0.4f;

        [Tooltip("dégâts infligés -50 %  ->  x0.5")]
        public float damageMultiplier = 0.5f;

        [Tooltip("plus d'arts de flamme")]
        public bool flameArtsAllowed = false;

        [Header("Feel (proposal)")]
        [Tooltip("Seconds for the light to shrink or grow when trimming starts or stops.")]
        public float transitionSeconds = 0.18f;
    }
}
