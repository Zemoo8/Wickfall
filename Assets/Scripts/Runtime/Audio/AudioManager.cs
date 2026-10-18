using UnityEngine;
using Wickfall.Meta;
using Wickfall.Presentation;

namespace Wickfall.Audio
{
    /// <summary>
    /// Plays every sound requested on the Sound channel from a fixed pool of AudioSources (zero
    /// allocation), music and ambience with crossfades on the Music channel. Group volumes (music,
    /// SFX, ambience, UI) come from the settings file and update live.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public AudioLibrary library;
        public int voices = 24;
        public float crossfadeSeconds = 1.6f;
        [Tooltip("Sounds further than this from the listener are skipped.")]
        public float hearingRange = 18f;

        AudioSource[] pool;
        int next;
        AudioSource musicA, musicB, ambienceA, ambienceB;
        bool musicOnA = true;
        float fade = 1f;
        float musicVolume = 1f, ambienceVolume = 1f;
        float[] lastPlayed;
        MusicId currentMusic = MusicId.None;
        Transform listener;

        public static AudioManager Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            if (library == null)
                library = Resources.Load<AudioLibrary>(AudioLibrary.ResourcePath);
            pool = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
                pool[i] = MakeSource("Voice " + i, false);
            musicA = MakeSource("Music A", true);
            musicB = MakeSource("Music B", true);
            ambienceA = MakeSource("Ambience A", true);
            ambienceB = MakeSource("Ambience B", true);
            lastPlayed = new float[System.Enum.GetValues(typeof(SoundId)).Length];
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        AudioSource MakeSource(string label, bool loop)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        void OnEnable()
        {
            Events.Sound.Register(OnSound);
            Events.Music.Register(OnMusic);
        }

        void OnDisable()
        {
            Events.Sound.Unregister(OnSound);
            Events.Music.Unregister(OnMusic);
        }

        static float GroupVolume(AudioGroup group) =>
            GameServices.Instance != null ? GameServices.Instance.Volume(group) : 0.8f;

        void OnSound(SoundRequest request)
        {
            if (library == null || !PresentationMode.IsEnabled(Feature.Audio) && request.Positional)
                return;
            if (!library.TryGet(request.Id, out var entry))
                return;
            int id = (int)request.Id;
            if (entry.cooldown > 0f && Time.unscaledTime - lastPlayed[id] < entry.cooldown)
                return;

            float pan = 0f;
            float attenuation = 1f;
            if (request.Positional)
            {
                if (listener == null && Camera.main != null)
                    listener = Camera.main.transform;
                if (listener != null)
                {
                    Vector2 delta = request.Position - (Vector2)listener.position;
                    float distance = delta.magnitude;
                    if (distance > hearingRange)
                        return;
                    attenuation = 1f - Mathf.Clamp01((distance - 4f) / (hearingRange - 4f));
                    pan = Mathf.Clamp(delta.x / 12f, -0.8f, 0.8f);
                }
            }

            lastPlayed[id] = Time.unscaledTime;
            var source = pool[next];
            next = (next + 1) % pool.Length;
            source.clip = entry.clips[entry.clips.Length > 1 ? Random.Range(0, entry.clips.Length) : 0];
            float volume = (entry.volume > 0f ? entry.volume : 1f) * request.Volume * attenuation * GroupVolume(entry.group);
            source.volume = volume;
            source.pitch = request.Pitch * (1f + Random.Range(-entry.pitchJitter, entry.pitchJitter));
            source.panStereo = pan;
            source.ignoreListenerPause = entry.group == AudioGroup.UI;
            source.Play();
        }

        void OnMusic(MusicId id)
        {
            if (id == currentMusic || library == null)
                return;
            currentMusic = id;
            library.TryGetMusic(id, out var entry);
            bool allowed = PresentationMode.IsEnabled(Feature.Music);
            var nextMusic = musicOnA ? musicB : musicA;
            var nextAmbience = musicOnA ? ambienceB : ambienceA;
            nextMusic.clip = allowed ? entry.clip : null;
            nextAmbience.clip = entry.ambience;
            musicVolume = entry.volume > 0f ? entry.volume : 0.7f;
            ambienceVolume = entry.ambienceVolume > 0f ? entry.ambienceVolume : 0.6f;
            if (nextMusic.clip != null) nextMusic.Play();
            if (nextAmbience.clip != null) nextAmbience.Play();
            musicOnA = !musicOnA;
            fade = 0f;
        }

        void Update()
        {
            fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / crossfadeSeconds);
            var current = musicOnA ? musicA : musicB;
            var previous = musicOnA ? musicB : musicA;
            var currentAmb = musicOnA ? ambienceA : ambienceB;
            var previousAmb = musicOnA ? ambienceB : ambienceA;
            float mv = GroupVolume(AudioGroup.Music) * musicVolume;
            float av = GroupVolume(AudioGroup.Ambience) * ambienceVolume;
            current.volume = mv * fade;
            previous.volume = mv * (1f - fade);
            currentAmb.volume = av * fade;
            previousAmb.volume = av * (1f - fade);
            if (fade >= 1f)
            {
                if (previous.isPlaying) previous.Stop();
                if (previousAmb.isPlaying) previousAmb.Stop();
            }
        }
    }
}
