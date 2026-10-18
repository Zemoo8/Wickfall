using System;
using UnityEngine;
using Wickfall.Meta;

namespace Wickfall.Audio
{
    /// <summary>Maps every <see cref="SoundId"/> and <see cref="MusicId"/> to clips (swap audio here, never in code).</summary>
    [CreateAssetMenu(menuName = "WICKFALL/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
        public const string ResourcePath = "AudioLibrary";

        [Serializable]
        public struct SoundEntry
        {
            public SoundId id;
            public AudioClip[] clips;
            [Range(0f, 1.5f)] public float volume;
            [Range(0f, 0.5f)] public float pitchJitter;
            public AudioGroup group;
            [Tooltip("Minimum seconds between two plays of this sound (stops stacking).")]
            public float cooldown;
        }

        [Serializable]
        public struct MusicEntry
        {
            public MusicId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
            public AudioClip ambience;
            [Range(0f, 1f)] public float ambienceVolume;
        }

        public SoundEntry[] sounds = new SoundEntry[0];
        public MusicEntry[] music = new MusicEntry[0];
        public AudioClip flameCrackleLoop;
        public AudioClip heartbeat;

        SoundEntry[] lookup;
        bool[] has;

        public bool TryGet(SoundId id, out SoundEntry entry)
        {
            if (lookup == null)
                Build();
            int i = (int)id;
            if (i >= 0 && i < lookup.Length && has[i])
            {
                entry = lookup[i];
                return true;
            }
            entry = default;
            return false;
        }

        public bool TryGetMusic(MusicId id, out MusicEntry entry)
        {
            for (int i = 0; i < music.Length; i++)
            {
                if (music[i].id == id)
                {
                    entry = music[i];
                    return true;
                }
            }
            entry = default;
            return false;
        }

        void Build()
        {
            int size = Enum.GetValues(typeof(SoundId)).Length;
            lookup = new SoundEntry[size];
            has = new bool[size];
            foreach (var s in sounds)
            {
                int i = (int)s.id;
                if (i >= 0 && i < size && s.clips != null && s.clips.Length > 0)
                {
                    lookup[i] = s;
                    has[i] = true;
                }
            }
        }

        void OnValidate() => lookup = null;
    }
}
