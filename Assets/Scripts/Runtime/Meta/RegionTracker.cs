using System.Collections.Generic;
using UnityEngine;

namespace Wickfall.Meta
{
    /// <summary>
    /// What ScoreSystem needs from a region run (PDF §2.4): time, deaths, relics taken, and enemies
    /// avoided without fighting. G09: an enemy counts as avoided when it noticed the player (or
    /// the player came within its detection range), was never damaged by the player, and was still
    /// alive when the region ended.
    /// </summary>
    public sealed class RegionTracker
    {
        public RegionId Region { get; private set; }
        public float Time { get; private set; }
        public int Deaths { get; private set; }
        public int Relics { get; private set; }
        public bool Running { get; private set; }

        readonly HashSet<int> noticed = new HashSet<int>();
        readonly HashSet<int> fought = new HashSet<int>();
        readonly HashSet<int> killed = new HashSet<int>();

        public void Begin(RegionId region)
        {
            if (Running && region == Region)
                return;
            Region = region;
            Time = 0f;
            Deaths = 0;
            Relics = 0;
            noticed.Clear();
            fought.Clear();
            killed.Clear();
            Running = true;
        }

        public void Tick(float dt)
        {
            if (Running)
                Time += dt;
        }

        public void Died() => Deaths++;
        public void RelicTaken() => Relics++;

        public void Noticed(int entityId) => noticed.Add(entityId);
        public void Fought(int entityId) => fought.Add(entityId);
        public void Killed(int entityId) => killed.Add(entityId);

        public int EnemiesAvoided
        {
            get
            {
                int n = 0;
                foreach (int id in noticed)
                    if (!fought.Contains(id) && !killed.Contains(id))
                        n++;
                return n;
            }
        }

        public RegionResult Finish(float waxPercent, float parSeconds, ScoreConfigSO config)
        {
            var stats = new RegionRunStats
            {
                WaxPercent = waxPercent,
                TimeSeconds = Time,
                Relics = Relics,
                EnemiesAvoided = EnemiesAvoided,
                Deaths = Deaths,
            };
            int score = ScoreCalculator.Score(stats, parSeconds, config);
            Running = false;
            return new RegionResult
            {
                Region = Region,
                Score = score,
                Rank = ScoreCalculator.RankFor(score, config),
                WaxPercent = waxPercent,
                TimeSeconds = Time,
                Relics = Relics,
                EnemiesAvoided = stats.EnemiesAvoided,
                Deaths = Deaths,
            };
        }
    }
}
