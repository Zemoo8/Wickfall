using System.IO;
using System.Linq;
using UnityEditor;

namespace Wickfall.EditorTools
{
    /// <summary>Where generated assets live, and folder helpers.</summary>
    public static class EditorPaths
    {
        public const string Settings = "Assets/Settings";
        public const string Renderer = "Assets/Settings/Wickfall2DRenderer.asset";
        public const string Pipeline = "Assets/Settings/WickfallURP.asset";
        public const string GlobalVolume = "Assets/Settings/WickfallVolumeProfile.asset";

        public const string Resources = "Assets/Resources";
        public const string Data = "Assets/Data";
        public const string Events = "Assets/Data/Events";
        public const string Config = "Assets/Data/Config";
        public const string Flames = "Assets/Data/Flames";
        public const string Regions = "Assets/Data/Regions";
        public const string Relics = "Assets/Data/Relics";
        public const string Enemies = "Assets/Data/Enemies";
        public const string Hazards = "Assets/Data/Hazards";
        public const string Pacts = "Assets/Data/Pacts";
        public const string Shop = "Assets/Data/Shop";

        public const string Scenes = "Assets/Scenes";
        public const string Prefabs = "Assets/Prefabs";
        public const string Art = "Assets/Art";
        public const string Materials = "Assets/Materials";
        public const string Shaders = "Assets/Shaders";
        public const string Audio = "Assets/Audio";

        /// <summary>Creates every missing folder along an "Assets/..." path.</summary>
        public static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        public static void EnsureFolderOf(string assetPath) => EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));
    }
}
