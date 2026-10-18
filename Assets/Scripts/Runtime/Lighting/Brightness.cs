using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Wickfall.Meta;

namespace Wickfall.Lighting
{
    /// <summary>
    /// The Brightness option (G69): lifts a dim region's ambient light and the picture's exposure so the
    /// world stays readable on any screen. It never adds ambient light where the PDF says there is none
    /// (the Creux Profond keeps 0), and it never changes the player's light or the detection rules.
    /// </summary>
    public static class Brightness
    {
        /// <summary>Ambient added at full brightness to a dim region (base below 0.5).</summary>
        public const float MaxAmbientBoost = 0.35f;
        /// <summary>Post-exposure (stops) at brightness 0 and 1.</summary>
        public const float MinExposure = -0.2f, MaxExposure = 0.9f;

        public static float Setting => GameServices.Instance != null ? Mathf.Clamp01(GameServices.Instance.Settings.brightness) : 0.6f;

        public static float Ambient(float baseIntensity, float brightness) =>
            baseIntensity <= 0f || baseIntensity >= 0.5f ? baseIntensity : baseIntensity + MaxAmbientBoost * Mathf.Clamp01(brightness);

        public static float Exposure(float brightness) => Mathf.Lerp(MinExposure, MaxExposure, Mathf.Clamp01(brightness));

        /// <summary>Film grain on or off (gameplay recordings turn it off: video encoders cannot compress noise).</summary>
        public static void SetFilmGrain(bool on)
        {
            foreach (var volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
                if (volume.profile != null && volume.profile.TryGet(out FilmGrain grain))
                    grain.active = on;
        }
    }
}
