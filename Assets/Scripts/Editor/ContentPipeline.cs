using UnityEditor;
using UnityEngine;
using Wickfall.Audio;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Builds the generated content (art import, prefabs, scenes) in dependency order.
    /// Each step only creates what is missing, except scenes marked as generated, which it rebuilds.
    /// </summary>
    public static class ContentPipeline
    {
        public const string AudioLibraryPath = "Assets/Resources/AudioLibrary.asset";

        public static void EnsureAll()
        {
            if (ArtFactory.EnsureTmpEssentials())
            {
                Debug.Log("[WICKFALL Setup] TMP resources imported: run the setup again to build the content.");
                return;
            }
            ArtFactory.EnsureAll();
            EnsureAudioLibrary();
            PrefabFactory.EnsureAll();
            WorldPrefabs.EnsureAll();
            SceneFactory.EnsureAll();
            RegionFactory.EnsureAll();
            SceneFactory.SetupBuildScenes();
        }

        public static AudioLibrary EnsureAudioLibrary()
        {
            var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(AudioLibraryPath);
            if (lib == null)
            {
                EditorPaths.EnsureFolder(EditorPaths.Resources);
                lib = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(lib, AudioLibraryPath);
            }
            AudioFactory.Fill(lib);
            return lib;
        }
    }
}
