using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Wickfall.Meta;
using Wickfall.Presentation;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// WICKFALL > Presentation: one click per review stage (prompt §13). The click sets the phase and
    /// plays from the Boot scene; the active phase has a checkmark. Nothing is ever removed.
    /// Headless: -executeMethod Wickfall.EditorTools.PresentationMenu.SetPhaseFromCommandLine -phase 4
    /// </summary>
    public static class PresentationMenu
    {
        const string Root = "WICKFALL/Presentation/";

        static PresentationConfig Config => AssetDatabase.LoadAssetAtPath<PresentationConfig>(WickfallSetup.PresentationConfigPath);

        public static void SetPhase(PresentationPhase phase, bool play)
        {
            var config = Config;
            if (config == null)
            {
                Debug.LogError("[WICKFALL] PresentationConfig missing: run WICKFALL > Setup first.");
                return;
            }
            config.SetPhase(phase);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[WICKFALL] Presentation phase: " + PresentationConfig.DisplayName(phase));
            if (play && !Application.isBatchMode)
                PlayFromStart();
        }

        static bool Is(PresentationPhase phase) => Config != null && Config.Phase == phase;

        [MenuItem(Root + "Phase 1 - Conception (pitch deck)", priority = 1)] static void P1() => SetPhase(PresentationPhase.Conception, true);
        [MenuItem(Root + "Phase 1 - Conception (pitch deck)", true)] static bool P1v() { Menu.SetChecked(Root + "Phase 1 - Conception (pitch deck)", Is(PresentationPhase.Conception)); return true; }
        [MenuItem(Root + "Phase 2 - Diagrams (deck + diagrams)", priority = 2)] static void P2() => SetPhase(PresentationPhase.Diagrams, true);
        [MenuItem(Root + "Phase 2 - Diagrams (deck + diagrams)", true)] static bool P2v() { Menu.SetChecked(Root + "Phase 2 - Diagrams (deck + diagrams)", Is(PresentationPhase.Diagrams)); return true; }
        [MenuItem(Root + "Phase 3 - Design (art deck)", priority = 3)] static void P3() => SetPhase(PresentationPhase.Design, true);
        [MenuItem(Root + "Phase 3 - Design (art deck)", true)] static bool P3v() { Menu.SetChecked(Root + "Phase 3 - Design (art deck)", Is(PresentationPhase.Design)); return true; }
        [MenuItem(Root + "Phase 4 - Development (Regions 1-2)", priority = 4)] static void P4() => SetPhase(PresentationPhase.Development, true);
        [MenuItem(Root + "Phase 4 - Development (Regions 1-2)", true)] static bool P4v() { Menu.SetChecked(Root + "Phase 4 - Development (Regions 1-2)", Is(PresentationPhase.Development)); return true; }
        [MenuItem(Root + "Phase 5 - 2D Animation (Regions 1-4)", priority = 5)] static void P5() => SetPhase(PresentationPhase.Animation2D, true);
        [MenuItem(Root + "Phase 5 - 2D Animation (Regions 1-4)", true)] static bool P5v() { Menu.SetChecked(Root + "Phase 5 - 2D Animation (Regions 1-4)", Is(PresentationPhase.Animation2D)); return true; }
        [MenuItem(Root + "Phase 6 - Testing (all regions, Roi Creux)", priority = 6)] static void P6() => SetPhase(PresentationPhase.Testing, true);
        [MenuItem(Root + "Phase 6 - Testing (all regions, Roi Creux)", true)] static bool P6v() { Menu.SetChecked(Root + "Phase 6 - Testing (all regions, Roi Creux)", Is(PresentationPhase.Testing)); return true; }
        [MenuItem(Root + "Phase 7 - Final Product", priority = 7)] static void P7() => SetPhase(PresentationPhase.FinalProduct, true);
        [MenuItem(Root + "Phase 7 - Final Product", true)] static bool P7v() { Menu.SetChecked(Root + "Phase 7 - Final Product", Is(PresentationPhase.FinalProduct)); return true; }
        [MenuItem(Root + "Phase 8 - Release (PC)", priority = 8)] static void P8() => SetPhase(PresentationPhase.Release, true);
        [MenuItem(Root + "Phase 8 - Release (PC)", true)] static bool P8v() { Menu.SetChecked(Root + "Phase 8 - Release (PC)", Is(PresentationPhase.Release)); return true; }
        [MenuItem(Root + "Show Everything - Entire Game", priority = 20)] static void All() => SetPhase(PresentationPhase.Everything, true);
        [MenuItem(Root + "Show Everything - Entire Game", true)] static bool Allv() { Menu.SetChecked(Root + "Show Everything - Entire Game", Is(PresentationPhase.Everything)); return true; }

        [MenuItem(Root + "Play From Start", priority = 40)]
        public static void PlayFromStart()
        {
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(SceneFactory.PathOf(SceneNames.Boot), OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        /// <summary>Batch mode: -phase N (0 = everything).</summary>
        public static void SetPhaseFromCommandLine()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-phase" && int.TryParse(args[i + 1], out int n))
                {
                    SetPhase((PresentationPhase)n, false);
                    return;
                }
            }
            Debug.LogError("[WICKFALL] Pass -phase N (0..8).");
        }
    }
}
