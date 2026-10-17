using UnityEngine;

namespace Wickfall
{
    /// <summary>One row of PDF §2.2 plus what the engine needs to build and run it.</summary>
    [CreateAssetMenu(menuName = "WICKFALL/Region", fileName = "Region")]
    public sealed class RegionSO : ScriptableObject
    {
        public RegionId id;
        public string nameKey = "region.tallow";
        public string environmentKey = "region.tallow.env";

        [Header("PDF §2.2 environment")]
        [Tooltip("Global 2D light. Champs de Suif: \"lumière ambiante faible\"; Creux Profond: none.")]
        [Range(0f, 1f)] public float ambientIntensity = 0.12f;
        public Color ambientColor = new Color(0.62f, 0.58f, 0.52f);
        [Tooltip("Creux Profond: \"Perte de cire x1,5\".")]
        public float waxLossMultiplier = 1f;
        [Tooltip("Creux Profond: \"carte visible seulement où vous brillez\" (LightReveal shader).")]
        public bool lightRevealGround;
        [Tooltip("Chambre du Foyer: \"Foyer en lumière négative\".")]
        public bool negativeLight;

        [Header("Progression (G03, G04)")]
        public FlameId flameFound = FlameId.Blue;
        public bool grantsFlame = true;
        public int relicIndex;

        [Header("Score (G09)")]
        [Tooltip("Par time in seconds for the time bonus.")]
        public float parTimeSeconds = 900f;

        [Header("Audio")]
        public MusicId music = MusicId.TallowFields;
        [Tooltip("Creux Profond: \"battement de coeur\" (PDF §4 AudioReactive).")]
        public bool heartbeat;

        [Header("Scenes (built by WICKFALL > Setup)")]
        public string baseScene;
        public string[] roomGroupScenes = new string[0];
    }
}
