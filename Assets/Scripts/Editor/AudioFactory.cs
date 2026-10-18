using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Wickfall.Audio;
using Wickfall.Meta;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Fills the AudioLibrary from Assets/Audio: a clip named after a SoundId (e.g. "GeyserWarn.wav",
    /// or "Footstep_1.wav", "Footstep_2.wav" for variations) is mapped to that id automatically.
    /// Music lives in Assets/Audio/Music named after the MusicId, ambience as "&lt;MusicId&gt;_Ambience".
    /// </summary>
    public static class AudioFactory
    {
        public const string SfxFolder = "Assets/Audio/SFX";
        public const string MusicFolder = "Assets/Audio/Music";

        static readonly Dictionary<SoundId, (float volume, float jitter, AudioGroup group, float cooldown)> Tuning =
            new Dictionary<SoundId, (float, float, AudioGroup, float)>
            {
                { SoundId.Footstep, (0.35f, 0.12f, AudioGroup.Sfx, 0.08f) },
                { SoundId.WaxPickup, (0.6f, 0.15f, AudioGroup.Sfx, 0.04f) },
                { SoundId.HitWax, (0.8f, 0.1f, AudioGroup.Sfx, 0.03f) },
                { SoundId.UiMove, (0.5f, 0.05f, AudioGroup.UI, 0.03f) },
                { SoundId.UiConfirm, (0.7f, 0f, AudioGroup.UI, 0f) },
                { SoundId.UiBack, (0.6f, 0f, AudioGroup.UI, 0f) },
                { SoundId.UiOpen, (0.6f, 0f, AudioGroup.UI, 0f) },
                { SoundId.UiError, (0.6f, 0f, AudioGroup.UI, 0.1f) },
                { SoundId.Purchase, (0.7f, 0f, AudioGroup.UI, 0f) },
                { SoundId.ScoreTally, (0.5f, 0.05f, AudioGroup.UI, 0.02f) },
                { SoundId.RankStamp, (0.9f, 0f, AudioGroup.UI, 0f) },
                { SoundId.RelicGet, (0.9f, 0f, AudioGroup.UI, 0f) },
            };

        public static void Fill(AudioLibrary lib)
        {
            var sounds = new List<AudioLibrary.SoundEntry>();
            if (Directory.Exists(SfxFolder))
            {
                var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { SfxFolder })
                    .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(c => c != null).ToList();
                foreach (SoundId id in System.Enum.GetValues(typeof(SoundId)))
                {
                    if (id == SoundId.None)
                        continue;
                    string name = id.ToString();
                    var matches = clips.Where(c => c.name == name || c.name.StartsWith(name + "_")).OrderBy(c => c.name).ToArray();
                    if (matches.Length == 0)
                        continue;
                    var t = Tuning.TryGetValue(id, out var value) ? value : (0.8f, 0.06f, AudioGroup.Sfx, 0.02f);
                    sounds.Add(new AudioLibrary.SoundEntry
                    {
                        id = id, clips = matches, volume = t.Item1, pitchJitter = t.Item2, group = t.Item3, cooldown = t.Item4,
                    });
                }
                lib.flameCrackleLoop = clips.FirstOrDefault(c => c.name == "FlameCrackleLoop");
                lib.heartbeat = clips.FirstOrDefault(c => c.name == "Heartbeat");
            }
            lib.sounds = sounds.ToArray();

            var music = new List<AudioLibrary.MusicEntry>();
            if (Directory.Exists(MusicFolder))
            {
                var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { MusicFolder })
                    .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(c => c != null).ToList();
                foreach (MusicId id in System.Enum.GetValues(typeof(MusicId)))
                {
                    if (id == MusicId.None)
                        continue;
                    var clip = clips.FirstOrDefault(c => c.name == id.ToString());
                    var ambience = clips.FirstOrDefault(c => c.name == id + "_Ambience");
                    if (clip == null && ambience == null)
                        continue;
                    music.Add(new AudioLibrary.MusicEntry { id = id, clip = clip, volume = 0.65f, ambience = ambience, ambienceVolume = 0.55f });
                }
            }
            lib.music = music.ToArray();
            EditorUtility.SetDirty(lib);
        }
    }
}
