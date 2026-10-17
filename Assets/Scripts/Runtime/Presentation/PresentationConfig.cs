using UnityEngine;

namespace Wickfall.Presentation
{
    /// <summary>
    /// Chooses which review stage the game runs in (Resources/PresentationConfig). Switch phases from
    /// the WICKFALL > Presentation menu. A phase only changes what is switched on at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "WICKFALL/Presentation Config", fileName = "PresentationConfig")]
    public sealed class PresentationConfig : ScriptableObject
    {
        public const string ResourcePath = "PresentationConfig";

        public const string ConceptScene = "Showcase_Concept";
        public const string DesignScene = "Showcase_Design";
        public const string MainMenuScene = "MainMenu";

        const Feature Shell = Feature.MainMenu | Feature.Hud | Feature.PauseMenu | Feature.SaveSystem | Feature.Options;
        const Feature Core = Feature.Combat | Feature.FlameArts | Feature.FlameSwitching | Feature.Trim | Feature.Dodge
                             | Feature.Enemies | Feature.Hazards | Feature.Braziers | Feature.Shop | Feature.Relics
                             | Feature.Score | Feature.LightPuzzles;
        const Feature Polish = Feature.Animation | Feature.VFX | Feature.CameraFeedback | Feature.Music | Feature.PostProcessing;
        const Feature Campaign = Feature.Region1 | Feature.Region2 | Feature.Region3 | Feature.Region4 | Feature.Region5
                                 | Feature.Region6 | Feature.Boss | Feature.Endings | Feature.Pacts | Feature.RegionSelect
                                 | Feature.EndingsGallery;

        [Tooltip("Which professor review stage to demonstrate. Everything = the complete game.")]
        [SerializeField] PresentationPhase phase = PresentationPhase.Everything;

        // Stored as raw longs: the Feature flags use more than 32 bits. Edit them through
        // ForceEnabled / ForceDisabled (the WICKFALL > Presentation menu shows the names).
        [Tooltip("Feature bits to force on for a demo, on top of the phase.")]
        [SerializeField] long forceEnabled;

        [Tooltip("Feature bits to force off for a demo, even if the phase includes them.")]
        [SerializeField] long forceDisabled;

        public PresentationPhase Phase => phase;
        public Feature ForceEnabled { get => (Feature)forceEnabled; set => forceEnabled = (long)value; }
        public Feature ForceDisabled { get => (Feature)forceDisabled; set => forceDisabled = (long)value; }
        public Feature EnabledFeatures => (FeaturesFor(phase) | ForceEnabled) & ~ForceDisabled;

        public void SetPhase(PresentationPhase value) => phase = value;

        public static PresentationConfig ForPhase(PresentationPhase value)
        {
            var config = CreateInstance<PresentationConfig>();
            config.phase = value;
            return config;
        }

        /// <summary>Each phase builds on the previous one (prompt §13).</summary>
        public static Feature FeaturesFor(PresentationPhase phase)
        {
            const Feature development = Shell | Core | Feature.Region1 | Feature.Region2 | Feature.Audio;
            const Feature animation = development | Feature.Animation | Feature.VFX | Feature.CameraFeedback
                                      | Feature.PostProcessing | Feature.Region3 | Feature.Region4;
            const Feature testing = animation | Feature.Region5 | Feature.Region6 | Feature.Boss | Feature.Endings
                                    | Feature.Music | Feature.DevOverlay | Feature.DebugCheats;
            const Feature final = Shell | Core | Polish | Campaign | Feature.Audio;

            switch (phase)
            {
                case PresentationPhase.Conception:
                    return Feature.ConceptDeck;
                case PresentationPhase.Diagrams:
                    return Feature.ConceptDeck | Feature.DiagramSlides;
                case PresentationPhase.Design:
                    return Feature.ConceptDeck | Feature.DiagramSlides | Feature.DesignDeck | Feature.MainMenu;
                case PresentationPhase.Development:
                    return development;
                case PresentationPhase.Animation2D:
                    return animation;
                case PresentationPhase.Testing:
                    return testing;
                case PresentationPhase.FinalProduct:
                case PresentationPhase.Release:
                    return final;
                default:
                    return (Feature)~0L;
            }
        }

        public static string StartSceneFor(PresentationPhase phase)
        {
            switch (phase)
            {
                case PresentationPhase.Conception:
                case PresentationPhase.Diagrams:
                    return ConceptScene;
                case PresentationPhase.Design:
                    return DesignScene;
                default:
                    return MainMenuScene;
            }
        }

        public static string DisplayName(PresentationPhase phase)
        {
            switch (phase)
            {
                case PresentationPhase.Conception: return "Phase 1 - Conception";
                case PresentationPhase.Diagrams: return "Phase 2 - Diagrams";
                case PresentationPhase.Design: return "Phase 3 - Design";
                case PresentationPhase.Development: return "Phase 4 - Development (Regions 1-2)";
                case PresentationPhase.Animation2D: return "Phase 5 - 2D Animation (Regions 1-4)";
                case PresentationPhase.Testing: return "Phase 6 - Testing (all regions, Roi Creux)";
                case PresentationPhase.FinalProduct: return "Phase 7 - Final Product";
                case PresentationPhase.Release: return "Phase 8 - Release (PC)";
                default: return "Everything";
            }
        }
    }

    /// <summary>Runtime entry point: <c>PresentationMode.IsEnabled(Feature.Trim)</c>.</summary>
    public static class PresentationMode
    {
        static PresentationConfig config;

        public static PresentationConfig Config
        {
            get
            {
                if (config == null)
                {
                    config = Resources.Load<PresentationConfig>(PresentationConfig.ResourcePath);
                    if (config == null)
                        config = ScriptableObject.CreateInstance<PresentationConfig>(); // Everything
                }
                return config;
            }
        }

        public static PresentationPhase Phase => Config.Phase;
        public static string StartScene => PresentationConfig.StartSceneFor(Phase);

        public static bool IsEnabled(Feature feature) => (Config.EnabledFeatures & feature) == feature;

        public static Feature RegionFeature(RegionId region) => (Feature)((long)Feature.Region1 << (int)region);

        public static bool IsRegionEnabled(RegionId region) => IsEnabled(RegionFeature(region));

        /// <summary>Use another config (tests and tools).</summary>
        public static void Override(PresentationConfig value) => config = value;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => config = null;
    }
}
