using UnityEngine;

namespace Wickfall
{
    /// <summary>PDF §2.1 (light radius) and §2.3 (detection formula), plus the look of the player light.</summary>
    [CreateAssetMenu(menuName = "WICKFALL/Config/Light", fileName = "LightConfig")]
    public sealed class LightConfig : ScriptableObject
    {
        [Header("PDF §2.1 - Lumière : rayon de 1,2 à 6 unités selon la cire")]
        public float minRadius = 1.2f;
        public float maxRadius = 6f;

        [Header("PDF §2.3 - Détection des ennemis = 2 + rayon x 1,5")]
        public float detectionBase = 2f;
        public float detectionPerRadius = 1.5f;

        [Header("Look (LightController)")]
        [Tooltip("Inner (full-brightness) radius as a fraction of the outer radius.")]
        [Range(0f, 1f)] public float innerRadiusRatio = 0.25f;
        public float intensityAtFullWax = 1.15f;
        public float intensityAtEmptyWax = 0.75f;
        [Range(0f, 1f)] public float falloff = 0.55f;
        [Range(0f, 1f)] public float shadowIntensity = 0.92f;

        [Header("Flicker (PDF §4: \"avec un léger scintillement\")")]
        [Range(0f, 0.3f)] public float flickerRadius = 0.035f;
        [Range(0f, 0.5f)] public float flickerIntensity = 0.08f;
        public float flickerSpeed = 7f;
        [Tooltip("The flame trembles more as wax runs out.")]
        public float flickerAtEmptyMultiplier = 2.4f;

        [Tooltip("How fast the radius eases to its target, per second (a hit visibly shrinks the light).")]
        public float radiusLerpSpeed = 9f;

        [Header("Temporary light losses (amounts are proposals; durations are the PDF's)")]
        [Tooltip("Soot (Bellows): light multiplier while sooted. PDF gives the 1.5 s duration only (G28).")]
        [Range(0f, 1f)] public float sootLightMultiplier = 0.5f;

        [Tooltip("Light-Eater Bloom: the radius the light is cut down to for the PDF's 3 s (G29).")]
        public float lightCutRadius = 0.55f;
    }
}
