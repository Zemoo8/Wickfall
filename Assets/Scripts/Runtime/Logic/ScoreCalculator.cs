using UnityEngine;

namespace Wickfall
{
    /// <summary>What a region run produced, for the PDF §2.4 score.</summary>
    public struct RegionRunStats
    {
        public float WaxPercent;     // 0..100: wax / max wax when the region ends (G09)
        public float TimeSeconds;
        public int Relics;
        public int EnemiesAvoided;
        public int Deaths;
    }

    /// <summary>PDF §2.4: wax % x 10 + time bonus + 500 per relic + 20 per enemy avoided - 150 per death.</summary>
    public static class ScoreCalculator
    {
        public static int TimeBonus(float timeSeconds, float parSeconds, float pointsPerSecond) =>
            Mathf.RoundToInt(Mathf.Max(0f, parSeconds - timeSeconds) * pointsPerSecond);

        public static int Score(in RegionRunStats run, float parSeconds, ScoreConfigSO config)
        {
            float score = Mathf.Clamp(run.WaxPercent, 0f, 100f) * config.waxPercentFactor
                          + TimeBonus(run.TimeSeconds, parSeconds, config.pointsPerSecondUnderPar)
                          + run.Relics * config.pointsPerRelic
                          + run.EnemiesAvoided * config.pointsPerAvoidedEnemy
                          + run.Deaths * config.pointsPerDeath;
            return Mathf.RoundToInt(score);
        }

        /// <summary>Ranks, lowest first: Cendre, Braise, Flamme, Brasier.</summary>
        public static Rank RankFor(int score, ScoreConfigSO config)
        {
            if (score >= config.brasierMin) return Rank.Brasier;
            if (score >= config.flammeMin) return Rank.Flamme;
            if (score >= config.braiseMin) return Rank.Braise;
            return Rank.Cendre;
        }
    }
}
