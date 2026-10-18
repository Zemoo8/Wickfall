using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Lighting;
using Wickfall.Meta;

namespace Wickfall.Player
{
    /// <summary>
    /// PDF §4 LightController: "Transforme la cire en rayon, intensité et couleur de la lumière 2D,
    /// avec un léger scintillement." It listens to wax, flame, trim and status channels and
    /// publishes the result (radius, detection radius, colour) on the LightChanged channel every
    /// frame. Formulas in <see cref="LightMath"/> (G07).
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class LightController : MonoBehaviour
    {
        public Light2D playerLight;
        [Tooltip("Optional soft glow light that only lights the player's surroundings a little.")]
        public Light2D haloLight;

        float wax = 100f;
        bool trimmed;
        FlameId flame = FlameId.Amber;
        float displayedRadius = -1f;
        float lightMultiplier = 1f;      // pacts
        float detectionMultiplier = 1f;  // pacts
        float noiseSeed;
        int sourceId;
        PlayerStatus status;
        DimController dim;
        LightState state;

        public LightState State => state;
        public float DisplayedRadius => displayedRadius;
        public bool ForceFullLight { get; set; } // dev overlay / showcase

        void Awake()
        {
            status = GetComponent<PlayerStatus>();
            dim = GetComponent<DimController>();
            noiseSeed = Random.value * 100f;
        }

        void OnEnable()
        {
            Events.WaxChanged.Register(OnWax);
            Events.FlameSwitched.Register(OnFlame);
            Events.TrimChanged.Register(OnTrim);
        }

        void OnDisable()
        {
            Events.WaxChanged.Unregister(OnWax);
            Events.FlameSwitched.Unregister(OnFlame);
            Events.TrimChanged.Unregister(OnTrim);
            LightSources.Remove(sourceId);
            sourceId = 0;
        }

        void OnWax(WaxChange change) => wax = change.Current;
        void OnFlame(FlameSwitch change) => flame = change.To;
        void OnTrim(bool value) => trimmed = value;

        public void ApplyModifiers(in PlayerModifiers modifiers)
        {
            lightMultiplier = modifiers.LightMultiplier;
            detectionMultiplier = modifiers.DetectionMultiplier;
        }

        /// <summary>Target radius and the detection radius for this frame (pure numbers, no smoothing).</summary>
        public void Compute(out float untrimmed, out float finalRadius, out float detection)
        {
            var light = Db.Light;
            var flameType = Db.Flame(flame);
            float baseRadius = LightMath.BaseRadius(ForceFullLight ? Db.Wax.maxWax : wax, Db.Wax.maxWax, light.minRadius, light.maxRadius);

            float temporary = lightMultiplier;
            if (status != null && status.Sooted)
                temporary *= light.sootLightMultiplier; // PDF: soot = light loss for 1.5 s

            untrimmed = LightMath.UntrimmedRadius(baseRadius, flameType.lightMultiplier, temporary);
            if (status != null && status.LightCut)
                untrimmed = Mathf.Min(untrimmed, light.lightCutRadius); // PDF: Bloom cuts light for 3 s

            finalRadius = LightMath.FinalRadius(untrimmed, trimmed, Db.Dim.radiusMultiplier);
            detection = LightMath.DetectionRadius(untrimmed, trimmed, light, Db.Dim) * detectionMultiplier;
            if (status != null && status.InDarkness)
                detection *= DarkZone.DetectionMultiplier; // under the canopy: "poches d'obscurité"
        }

        void LateUpdate()
        {
            var light = Db.Light;
            Compute(out float untrimmed, out float target, out float detection);

            // Trimming eases over DimConfig.transitionSeconds; everything else eases at radiusLerpSpeed.
            if (displayedRadius < 0f)
                displayedRadius = target;
            float speed = light.radiusLerpSpeed * Mathf.Max(1f, Mathf.Abs(target - displayedRadius));
            displayedRadius = Mathf.MoveTowards(displayedRadius, target, speed * Time.deltaTime);

            float t = Mathf.Clamp01(wax / Db.Wax.maxWax);
            float shake = Mathf.Lerp(light.flickerAtEmptyMultiplier, 1f, t);
            float time = Time.time * light.flickerSpeed;
            float n1 = Mathf.PerlinNoise(noiseSeed, time) - 0.5f;
            float n2 = Mathf.PerlinNoise(noiseSeed + 17.3f, time * 1.7f) - 0.5f;
            float radius = displayedRadius * (1f + n1 * 2f * light.flickerRadius * shake);
            float intensity = LightMath.Intensity(wax, Db.Wax.maxWax, light) * (1f + n2 * 2f * light.flickerIntensity * shake);
            Color color = Db.Flame(flame).lightColor;

            if (playerLight != null)
            {
                playerLight.pointLightOuterRadius = Mathf.Max(0.05f, radius);
                playerLight.pointLightInnerRadius = Mathf.Max(0f, radius * light.innerRadiusRatio);
                playerLight.intensity = intensity;
                playerLight.color = color;
                playerLight.falloffIntensity = light.falloff;
                playerLight.shadowIntensity = light.shadowIntensity;
            }
            if (haloLight != null)
            {
                haloLight.pointLightOuterRadius = Mathf.Max(0.6f, radius * 0.35f);
                haloLight.color = color;
                haloLight.intensity = 0.35f * intensity;
            }

            Vector2 position = transform.position;
            state = new LightState
            {
                Radius = displayedRadius,
                UntrimmedRadius = untrimmed,
                DetectionRadius = detection,
                Intensity = intensity,
                Color = color,
                Trimmed = trimmed,
                Position = position,
            };
            Events.LightChanged.Raise(state);
            sourceId = LightSources.Set(sourceId, position, displayedRadius, true);
        }
    }
}
