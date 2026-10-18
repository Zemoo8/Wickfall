using UnityEngine;
using Wickfall.Meta;
using Wickfall.Presentation;

namespace Wickfall.Audio
{
    /// <summary>
    /// PDF §4 AudioReactive: "Le crépitement de la flamme monte en hauteur quand la cire baisse ;
    /// battement de coeur dans le Creux Profond." Listens to the WaxChanged, TrimChanged and
    /// RegionEntered channels.
    /// </summary>
    public sealed class AudioReactive : MonoBehaviour
    {
        public AudioSource crackle;
        public AudioSource heartbeat;

        float wax = 100f;
        float max = 100f;
        bool trimmed;
        bool extinguished;
        bool heartbeatRegion;
        float nextBeat;

        /// <summary>Crackle pitch for a wax amount (exposed for the tests).</summary>
        public static float PitchFor(float waxNormalized, AudioReactiveSO config) =>
            Mathf.Lerp(config.cracklePitchAtEmptyWax, config.cracklePitchAtFullWax, Mathf.Clamp01(waxNormalized));

        public static float BpmFor(float waxNormalized, AudioReactiveSO config) =>
            Mathf.Lerp(config.heartbeatBpmAtEmptyWax, config.heartbeatBpmAtFullWax, Mathf.Clamp01(waxNormalized));

        public float CurrentPitch => crackle != null ? crackle.pitch : PitchFor(max > 0 ? wax / max : 0, Db.AudioReactive);
        public bool HeartbeatActive => heartbeatRegion && !extinguished;

        void OnEnable()
        {
            Events.WaxChanged.Register(OnWax);
            Events.TrimChanged.Register(OnTrim);
            Events.RegionEntered.Register(OnRegion);
            if (Events.RegionEntered.HasLast) OnRegion(Events.RegionEntered.Last);
        }

        void OnDisable()
        {
            Events.WaxChanged.Unregister(OnWax);
            Events.TrimChanged.Unregister(OnTrim);
            Events.RegionEntered.Unregister(OnRegion);
        }

        void OnWax(WaxChange change)
        {
            wax = change.Current;
            max = Mathf.Max(1f, change.Max);
            extinguished = change.Current <= 0f;
        }

        void OnTrim(bool value) => trimmed = value;

        void OnRegion(RegionId region)
        {
            var data = Db.Region(region);
            heartbeatRegion = data != null && data.heartbeat;
        }

        void Update()
        {
            var config = Db.AudioReactive;
            float n = wax / max;
            float sfx = GameServices.Instance != null ? GameServices.Instance.Volume(AudioGroup.Sfx) : 0.8f;
            bool audible = PresentationMode.IsEnabled(Feature.Audio) && !extinguished && Time.timeScale > 0f;

            if (crackle != null)
            {
                crackle.pitch = PitchFor(n, config);
                crackle.volume = audible ? config.crackleVolume * sfx * (trimmed ? config.trimmedVolumeMultiplier : 1f) : 0f;
                if (crackle.clip != null && !crackle.isPlaying && audible)
                    crackle.Play();
            }

            if (heartbeat != null && heartbeat.clip != null && HeartbeatActive && audible && Time.time >= nextBeat)
            {
                heartbeat.volume = config.heartbeatVolume * sfx;
                heartbeat.Play();
                nextBeat = Time.time + 60f / BpmFor(n, config);
            }
        }
    }
}
