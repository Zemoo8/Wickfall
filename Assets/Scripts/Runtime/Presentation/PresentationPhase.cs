using System;

namespace Wickfall.Presentation
{
    /// <summary>The professor's review stages (kickoff prompt §13, same format as ECHO).</summary>
    public enum PresentationPhase
    {
        Everything = 0,
        Conception = 1,
        Diagrams = 2,
        Design = 3,
        Development = 4,
        Animation2D = 5,
        Testing = 6,
        FinalProduct = 7,
        Release = 8,
    }

    /// <summary>What a phase can switch on or off at runtime. Nothing is ever removed from the project.</summary>
    [Flags]
    public enum Feature : long
    {
        None = 0,
        // Decks
        ConceptDeck = 1L << 0,
        DiagramSlides = 1L << 1,
        DesignDeck = 1L << 2,
        // Shell
        MainMenu = 1L << 3,
        Hud = 1L << 4,
        PauseMenu = 1L << 5,
        SaveSystem = 1L << 6,
        Options = 1L << 7,
        // Core gameplay (PDF §2 and §4)
        Combat = 1L << 8,
        FlameArts = 1L << 9,
        FlameSwitching = 1L << 10,
        Trim = 1L << 11,
        Dodge = 1L << 12,
        Enemies = 1L << 13,
        Hazards = 1L << 14,
        Braziers = 1L << 15,
        Shop = 1L << 16,
        Relics = 1L << 17,
        Score = 1L << 18,
        LightPuzzles = 1L << 19,
        // Content
        Region1 = 1L << 20,
        Region2 = 1L << 21,
        Region3 = 1L << 22,
        Region4 = 1L << 23,
        Region5 = 1L << 24,
        Region6 = 1L << 25,
        Boss = 1L << 26,
        Endings = 1L << 27,
        Pacts = 1L << 28,
        RegionSelect = 1L << 29,
        EndingsGallery = 1L << 30,
        // Polish
        Animation = 1L << 31,
        VFX = 1L << 32,
        CameraFeedback = 1L << 33,
        Audio = 1L << 34,
        Music = 1L << 35,
        PostProcessing = 1L << 36,
        // Tools
        DevOverlay = 1L << 37,
        DebugCheats = 1L << 38,
    }
}
