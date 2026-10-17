using UnityEngine;

namespace Wickfall
{
    /// <summary>
    /// PDF §2.1 / §2.3 light and detection formulas, combined as decided in G07:
    ///   base radius   = lerp(1.2, 6, clamp01(wax / 100))
    ///   final radius  = base x flame light x (0.35 while trimmed) x temporary effects
    ///   detection     = (2 + untrimmed radius x 1.5) x (0.4 while trimmed)
    /// The trim multipliers are applied to the untrimmed values so the -65 % radius and the
    /// -60 % detection never stack twice.
    /// </summary>
    public static class LightMath
    {
        /// <summary>Radius from wax alone. Wax above the PDF's 100 adds buffer, not radius (G07).</summary>
        public static float BaseRadius(float wax, float pdfMaxWax, float minRadius, float maxRadius)
        {
            float t = pdfMaxWax > 0f ? Mathf.Clamp01(wax / pdfMaxWax) : 0f;
            return Mathf.Lerp(minRadius, maxRadius, t);
        }

        public static float BaseRadius(float wax, WaxConfig waxConfig, LightConfig light) =>
            BaseRadius(wax, waxConfig.maxWax, light.minRadius, light.maxRadius);

        /// <summary>Radius before trimming: base x flame light x temporary multipliers (soot, pacts).</summary>
        public static float UntrimmedRadius(float baseRadius, float flameLightMultiplier, float temporaryMultiplier) =>
            baseRadius * flameLightMultiplier * temporaryMultiplier;

        /// <summary>What the player sees.</summary>
        public static float FinalRadius(float untrimmedRadius, bool trimmed, float trimRadiusMultiplier) =>
            trimmed ? untrimmedRadius * trimRadiusMultiplier : untrimmedRadius;

        /// <summary>PDF §2.3: "Détection des ennemis = 2 + rayon x 1,5", then -60 % while trimmed.</summary>
        public static float DetectionRadius(float untrimmedRadius, bool trimmed, float detectionBase, float detectionPerRadius,
            float trimDetectionMultiplier)
        {
            float detection = detectionBase + untrimmedRadius * detectionPerRadius;
            return trimmed ? detection * trimDetectionMultiplier : detection;
        }

        public static float DetectionRadius(float untrimmedRadius, bool trimmed, LightConfig light, DimConfig dim) =>
            DetectionRadius(untrimmedRadius, trimmed, light.detectionBase, light.detectionPerRadius, dim.detectionMultiplier);

        /// <summary>Light intensity eases from dim to bright as wax rises.</summary>
        public static float Intensity(float wax, float pdfMaxWax, LightConfig light)
        {
            float t = pdfMaxWax > 0f ? Mathf.Clamp01(wax / pdfMaxWax) : 0f;
            return Mathf.Lerp(light.intensityAtEmptyWax, light.intensityAtFullWax, t);
        }
    }
}
