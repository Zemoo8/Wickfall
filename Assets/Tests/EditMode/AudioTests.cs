using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Wickfall.Audio;
using Wickfall.EditorTools;

namespace Wickfall.Tests
{
    /// <summary>Prompt §11: every sound has a clip; the flame crackle rises as wax drops; a heartbeat exists.</summary>
    public class AudioTests
    {
        static AudioLibrary Library => AssetDatabase.LoadAssetAtPath<AudioLibrary>(ContentPipeline.AudioLibraryPath);

        [Test]
        public void EverySoundId_HasAClip()
        {
            var lib = Library;
            Assert.IsNotNull(lib);
            var missing = new List<string>();
            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
            {
                if (id == SoundId.None)
                    continue;
                var entries = lib.sounds.Where(s => s.id == id).ToList();
                if (entries.Count == 0 || entries[0].clips == null || entries[0].clips.Length == 0 || entries[0].clips.Any(c => c == null))
                    missing.Add(id.ToString());
            }
            Assert.IsEmpty(missing, "sounds without a clip: " + string.Join(", ", missing));
        }

        [Test]
        public void EveryMusicId_HasALoop_AndTheFlameHasItsCrackleAndHeartbeat()
        {
            var lib = Library;
            var missing = Enum.GetValues(typeof(MusicId)).Cast<MusicId>()
                .Where(id => id != MusicId.None && !lib.music.Any(m => m.id == id && m.clip != null))
                .Select(id => id.ToString()).ToList();
            Assert.IsEmpty(missing, "music without a clip: " + string.Join(", ", missing));
            Assert.IsNotNull(lib.flameCrackleLoop, "the flame's crackle loop");
            Assert.IsNotNull(lib.heartbeat, "the Creux Profond's heartbeat");
        }

        [Test]
        public void Crackle_RisesInPitch_AsWaxDrops_AndTheHeartbeatSpeedsUp()
        {
            var config = Db.AudioReactive;
            Assert.Greater(AudioReactive.PitchFor(0.1f, config), AudioReactive.PitchFor(0.9f, config), "prompt §11: pitch rises as wax drops");
            Assert.Greater(AudioReactive.BpmFor(0.1f, config), AudioReactive.BpmFor(0.9f, config), "the heartbeat speeds up at low wax");
        }
    }
}
