using NUnit.Framework;
using UnityEngine;

namespace Wickfall.Tests
{
    /// <summary>PDF §2.1 flames, §2.3 interactions, §2.4 score.</summary>
    public class CombatAndScoreTests
    {
        const float Eps = 1e-4f;

        [Test]
        public void Crimson_DealsTwoPointTwoAtNoWax_AndZeroPointSixAtFull()
        {
            Assert.AreEqual(2.2f, CombatMath.CrimsonMultiplier(0f, 100f), Eps);
            Assert.AreEqual(0.6f, CombatMath.CrimsonMultiplier(100f, 100f), Eps);
            Assert.AreEqual(1.4f, CombatMath.CrimsonMultiplier(50f, 100f), Eps);
            Assert.Greater(CombatMath.CrimsonMultiplier(10f, 100f), CombatMath.CrimsonMultiplier(90f, 100f));
        }

        [Test]
        public void Trimmed_DealsHalfDamage()
        {
            var amber = PdfTables.CreateFlame(FlameId.Amber);
            Assert.AreEqual(10f, CombatMath.PlayerOutgoing(10f, amber, 80f, 100f, false, 0.5f, 1f), Eps);
            Assert.AreEqual(5f, CombatMath.PlayerOutgoing(10f, amber, 80f, 100f, true, 0.5f, 1f), Eps);
        }

        [Test]
        public void FlameDamageMultipliers_FollowThePdfTable()
        {
            Assert.AreEqual(7f, CombatMath.PlayerOutgoing(10f, PdfTables.CreateFlame(FlameId.Blue), 80f, 100f, false, 0.5f, 1f), Eps);
            Assert.AreEqual(9f, CombatMath.PlayerOutgoing(10f, PdfTables.CreateFlame(FlameId.Green), 80f, 100f, false, 0.5f, 1f), Eps);
            Assert.AreEqual(16f, CombatMath.PlayerOutgoing(10f, PdfTables.CreateFlame(FlameId.White), 80f, 100f, false, 0.5f, 1f), Eps);
        }

        [TestCase(FlameId.Amber, false)]
        [TestCase(FlameId.Blue, false)]
        [TestCase(FlameId.Green, true)]
        [TestCase(FlameId.White, true)]
        [TestCase(FlameId.Crimson, false)]
        public void Slagborn_HurtOnlyByWhiteOrGreen(FlameId flame, bool hurts)
        {
            Assert.AreEqual(hurts, CombatMath.CanHurt(DamageTags.Armored, true, flame));
        }

        [TestCase(FlameId.Amber, false)]
        [TestCase(FlameId.Blue, false)]
        [TestCase(FlameId.Green, false)]
        [TestCase(FlameId.White, false)]
        [TestCase(FlameId.Crimson, true)]
        public void Glass_BrokenOnlyByCrimson(FlameId flame, bool breaks)
        {
            Assert.AreEqual(breaks, CombatMath.CanHurt(DamageTags.Glass, true, flame));
        }

        [Test]
        public void HitsWithoutAFlame_AreNeverFiltered()
        {
            Assert.IsTrue(CombatMath.CanHurt(DamageTags.Armored | DamageTags.Glass, false, FlameId.Amber));
        }

        [Test]
        public void Soot_MultipliesIncomingDamageBy1Point25()
        {
            Assert.AreEqual(10f, CombatMath.PlayerIncoming(10f, false, 1.25f, 0f), Eps);
            Assert.AreEqual(12.5f, CombatMath.PlayerIncoming(10f, true, 1.25f, 0f), Eps);
        }

        [Test]
        public void KillReward_StaysInsideTheTypeRange()
        {
            var type = PdfTables.CreateEnemy(EnemyKind.Slagborn);
            for (float roll = 0f; roll < 1f; roll += 0.05f)
            {
                int reward = CombatMath.KillReward(type, roll);
                Assert.GreaterOrEqual(reward, type.killWaxMin);
                Assert.LessOrEqual(reward, type.killWaxMax);
            }
            Assert.AreEqual(type.killWaxMax, CombatMath.KillReward(type, 0.9999f));
        }

        [Test]
        public void Score_FollowsThePdfFormula()
        {
            var config = ScriptableObject.CreateInstance<ScoreConfigSO>();
            var run = new RegionRunStats { WaxPercent = 60f, TimeSeconds = 500f, Relics = 1, EnemiesAvoided = 4, Deaths = 2 };
            // 60 x 10 + (600 - 500) x 1 + 500 + 4 x 20 - 2 x 150
            Assert.AreEqual(600 + 100 + 500 + 80 - 300, ScoreCalculator.Score(run, 600f, config));
        }

        [Test]
        public void Score_TimeBonusNeverNegative()
        {
            Assert.AreEqual(0, ScoreCalculator.TimeBonus(900f, 600f, 1f));
        }

        [Test]
        public void Ranks_GoCendreBraiseFlammeBrasier()
        {
            var config = ScriptableObject.CreateInstance<ScoreConfigSO>();
            Assert.AreEqual(Rank.Cendre, ScoreCalculator.RankFor(config.braiseMin - 1, config));
            Assert.AreEqual(Rank.Braise, ScoreCalculator.RankFor(config.braiseMin, config));
            Assert.AreEqual(Rank.Flamme, ScoreCalculator.RankFor(config.flammeMin, config));
            Assert.AreEqual(Rank.Brasier, ScoreCalculator.RankFor(config.brasierMin + 500, config));
        }

        [Test]
        public void Arc_HitsInFrontOnly()
        {
            Assert.IsTrue(CombatMath.InArc(Vector2.zero, Vector2.right, new Vector2(1f, 0.2f), 1.5f, 120f));
            Assert.IsFalse(CombatMath.InArc(Vector2.zero, Vector2.right, new Vector2(-1f, 0f), 1.5f, 120f));
            Assert.IsFalse(CombatMath.InArc(Vector2.zero, Vector2.right, new Vector2(3f, 0f), 1.5f, 120f));
        }

        [Test]
        public void Reflection_RemembersExactlyTheLastThreeActions()
        {
            var recorder = new ActionRecorder();
            recorder.Record(new PlayerActionInfo { Kind = PlayerActionKind.Attack });
            recorder.Record(new PlayerActionInfo { Kind = PlayerActionKind.Dodge });
            recorder.Record(new PlayerActionInfo { Kind = PlayerActionKind.Art });
            recorder.Record(new PlayerActionInfo { Kind = PlayerActionKind.SwitchFlame });
            Assert.AreEqual(3, recorder.Count);
            Assert.AreEqual(PlayerActionKind.Dodge, recorder.Get(0).Kind);
            Assert.AreEqual(PlayerActionKind.Art, recorder.Get(1).Kind);
            Assert.AreEqual(PlayerActionKind.SwitchFlame, recorder.Get(2).Kind);
        }
    }
}
