using UnityEngine;

namespace Wickfall
{
    /// <summary>PDF §4 AudioReactive: "Le crépitement de la flamme monte en hauteur quand la cire baisse ; battement de coeur dans le Creux Profond."</summary>
    [CreateAssetMenu(menuName = "WICKFALL/Config/Audio Reactive", fileName = "AudioReactive")]
    public sealed class AudioReactiveSO : ScriptableObject
    {
        [Header("Flame crackle: pitch rises as wax drops")]
        public float cracklePitchAtFullWax = 0.85f;
        public float cracklePitchAtEmptyWax = 1.75f;
        public float crackleVolume = 0.55f;
        [Tooltip("Trimmed flame: quieter crackle.")]
        public float trimmedVolumeMultiplier = 0.35f;

        [Header("Heartbeat in the Creux Profond (proposal: faster at low wax)")]
        public float heartbeatBpmAtFullWax = 54f;
        public float heartbeatBpmAtEmptyWax = 132f;
        public float heartbeatVolume = 0.8f;
    }
}
