namespace Wickfall.Meta
{
    /// <summary>Scene names (WICKFALL > Setup adds them all to the build).</summary>
    public static class SceneNames
    {
        public const string Boot = "Boot";
        public const string Core = "Core";
        public const string MainMenu = "MainMenu";
        public const string ConceptShowcase = "Showcase_Concept";
        public const string DesignShowcase = "Showcase_Design";
        public const string LightLab = "LightLab";
        public const string Ending = "Ending";

        public static string RegionBase(RegionId region) => "R" + ((int)region + 1) + "_" + region;
    }
}
