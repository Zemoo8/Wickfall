using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Wickfall.Meta;

namespace Wickfall.Lighting
{
    /// <summary>On the Core's global Volume: applies the Brightness option as post-exposure.</summary>
    [RequireComponent(typeof(Volume))]
    public sealed class ExposureBrightness : MonoBehaviour
    {
        ColorAdjustments adjustments;

        void Start()
        {
            var volume = GetComponent<Volume>();
            if (volume.profile != null && volume.profile.TryGet(out adjustments))
                adjustments.postExposure.overrideState = true;
            Apply();
        }

        void OnEnable() => Events.SettingsChanged.Register(Apply);
        void OnDisable() => Events.SettingsChanged.Unregister(Apply);

        void Apply()
        {
            if (adjustments != null)
                adjustments.postExposure.value = Brightness.Exposure(Brightness.Setting);
        }
    }
}
