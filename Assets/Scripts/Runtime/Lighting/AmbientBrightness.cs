using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Wickfall.Meta;

namespace Wickfall.Lighting
{
    /// <summary>On a scene's Global Light: applies the Brightness option to its ambient intensity.</summary>
    [RequireComponent(typeof(Light2D))]
    public sealed class AmbientBrightness : MonoBehaviour
    {
        public float baseIntensity = -1f;
        Light2D ambient;

        void Awake()
        {
            ambient = GetComponent<Light2D>();
            if (baseIntensity < 0f)
                baseIntensity = ambient.intensity;
        }

        void OnEnable()
        {
            Events.SettingsChanged.Register(Apply);
            Apply();
        }

        void OnDisable() => Events.SettingsChanged.Unregister(Apply);

        void Apply()
        {
            if (ambient != null)
                ambient.intensity = Brightness.Ambient(baseIntensity, Brightness.Setting);
        }
    }
}
