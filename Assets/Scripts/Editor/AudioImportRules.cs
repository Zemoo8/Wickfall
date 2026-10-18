using System;
using UnityEditor;
using UnityEngine;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Import settings for Assets/Audio (prompt §11: Vorbis compression). Music streams from disk; short
    /// effects are decompressed when loaded so they start instantly; all are mono.
    /// </summary>
    public sealed class AudioImportRules : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Audio/", StringComparison.Ordinal))
                return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            bool music = assetPath.StartsWith(AudioFactory.MusicFolder + "/", StringComparison.Ordinal);
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music ? 0.45f : 0.7f;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = music;
        }
    }
}
