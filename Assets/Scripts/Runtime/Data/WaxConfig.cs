using UnityEngine;

namespace Wickfall
{
    /// <summary>PDF §2.1 and §2.3: the one resource. Every default here is the PDF's number.</summary>
    [CreateAssetMenu(menuName = "WICKFALL/Config/Wax", fileName = "WaxConfig")]
    public sealed class WaxConfig : ScriptableObject
    {
        [Header("PDF §2.1 - Cire")]
        [Tooltip("PDF: \"Cire : 100 au maximum\". Also the reference for the light radius (G07).")]
        public float maxWax = 100f;

        [Tooltip("PDF: \"Perte passive de 0,5 par seconde\".")]
        public float passiveLossPerSecond = 0.5f;

        [Header("PDF §2.1 - Changer de flamme")]
        [Tooltip("PDF: \"Changer de flamme coûte 8 de cire\".")]
        public float flameSwitchCost = 8f;

        [Header("PDF §2.3 - Tuer un ennemi")]
        [Tooltip("PDF: \"Gain de 4 à 15 de cire\" (lowest reward of any enemy).")]
        public int killRewardMin = 4;

        [Tooltip("PDF: \"Gain de 4 à 15 de cire\" (highest reward of any enemy).")]
        public int killRewardMax = 15;

        [Header("Proposals (G18: wax drops left when hit)")]
        [Tooltip("Most wax drops kept on the ground at once in a region; the oldest is recycled.")]
        public int maxGroundDrops = 48;

        [Tooltip("A drop is left for every hit of at least this much wax.")]
        public float dropMinDamage = 1f;
    }
}
