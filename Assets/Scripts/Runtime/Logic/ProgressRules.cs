using Wickfall.Meta;

namespace Wickfall
{
    /// <summary>Endings of PDF §1 and the Roi Creux's phases of PDF §4, as pure rules.</summary>
    public static class ProgressRules
    {
        /// <summary>PDF §1: C (secret) needs "les 6 reliques et le Roi Creux épargné".</summary>
        public static bool CanPassTheWick(SaveData save) =>
            save != null && save.RelicCount >= WickfallCounts.Relics && save.kingDecided && save.kingSpared;

        public static bool CanPassTheWick(int relicCount, bool kingSpared) =>
            relicCount >= WickfallCounts.Relics && kingSpared;

        /// <summary>G13 phase 1: the arena's wax pools go dark (from phase 1 on).</summary>
        public static bool WaxPoolsDisabled(int bossPhase, BossConfigSO config) =>
            config.phase1DisablesWaxPools && bossPhase >= 1;

        /// <summary>PDF: "pas de mode faible" from phase 2 on.</summary>
        public static bool TrimDisabled(int bossPhase, BossConfigSO config) =>
            config.phase2DisablesTrim && bossPhase >= 2;

        /// <summary>PDF: "cire maximale à 40" in phase 3. Returns +Infinity when there is no cap.</summary>
        public static float MaxWaxCap(int bossPhase, BossConfigSO config) =>
            bossPhase >= 3 ? config.phase3MaxWax : float.PositiveInfinity;

        /// <summary>G03: the flame each region hands out, in PDF order (amber is the starting flame).</summary>
        public static bool IsFlameAvailable(SaveData save, FlameId flame) =>
            save != null && save.HasFlame(flame);
    }
}
