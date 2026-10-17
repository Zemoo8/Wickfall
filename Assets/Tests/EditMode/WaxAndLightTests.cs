using NUnit.Framework;
using UnityEngine;

namespace Wickfall.Tests
{
    /// <summary>PDF §2.1 wax rules and the §2.3 light/detection formulas (G07).</summary>
    public class WaxAndLightTests
    {
        const float Eps = 1e-4f;

        [Test]
        public void Wax_StartsFull_At100()
        {
            var wax = new WaxModel(100f, 0.5f);
            Assert.AreEqual(100f, wax.Current, Eps);
            Assert.AreEqual(100f, wax.Max, Eps);
        }

        [Test]
        public void Wax_PassiveLoss_IsHalfAPerSecond_EvenStandingStill()
        {
            var wax = new WaxModel(100f, 0.5f);
            for (int i = 0; i < 100; i++)
                wax.Tick(0.1f); // 10 seconds
            Assert.AreEqual(95f, wax.Current, 1e-3f);
        }

        [Test]
        public void Wax_DeepHollow_LosesOneAndAHalfTimesFaster()
        {
            var wax = new WaxModel(100f, 0.5f) { LossMultiplier = 1.5f };
            wax.Tick(10f);
            Assert.AreEqual(100f - 7.5f, wax.Current, Eps);
        }

        [Test]
        public void Wax_Trimmed_LosesSeventyFivePercentLess()
        {
            var wax = new WaxModel(100f, 0.5f) { LossMultiplier = 0.25f };
            wax.Tick(10f);
            Assert.AreEqual(100f - 1.25f, wax.Current, Eps);
        }

        [Test]
        public void Wax_DeathHappensAtExactlyZero_AndOnlyOnce()
        {
            var wax = new WaxModel(100f, 0.5f);
            int deaths = 0;
            wax.Extinguished += () => deaths++;
            wax.Damage(99.999f);
            Assert.IsFalse(wax.IsExtinguished);
            wax.Damage(50f);
            Assert.IsTrue(wax.IsExtinguished);
            Assert.AreEqual(0f, wax.Current, Eps);
            wax.Damage(10f);
            wax.Tick(5f);
            Assert.AreEqual(1, deaths);
        }

        [Test]
        public void Wax_GainsNeverExceedMax()
        {
            var wax = new WaxModel(100f, 0.5f);
            wax.Damage(10f);
            Assert.AreEqual(10f, wax.Gain(15f), Eps);
            Assert.AreEqual(100f, wax.Current, Eps);
        }

        [Test]
        public void Wax_CannotSpendIntoDeath()
        {
            var wax = new WaxModel(100f, 0.5f);
            wax.Damage(95f);
            Assert.IsFalse(wax.TrySpend(8f), "a flame switch must not extinguish the player");
            Assert.AreEqual(5f, wax.Current, Eps);
            wax.Gain(20f);
            Assert.IsTrue(wax.TrySpend(8f));
            Assert.AreEqual(17f, wax.Current, Eps);
        }

        [Test]
        public void Wax_RelicAndPactRaiseMax_BossPhase3CapsItAt40()
        {
            var wax = new WaxModel(100f, 0.5f);
            wax.SetBaseMax(150f); // +20 relic, +30 PDF pact
            Assert.AreEqual(150f, wax.Max, Eps);
            wax.Refill();
            wax.SetCap(40f);
            Assert.AreEqual(40f, wax.Max, Eps);
            Assert.AreEqual(40f, wax.Current, Eps);
            wax.SetCap(float.PositiveInfinity);
            Assert.AreEqual(150f, wax.Max, Eps);
        }

        [Test]
        public void Light_Radius_Is1Point2AtNoWax_And6AtFullWax()
        {
            Assert.AreEqual(1.2f, LightMath.BaseRadius(0f, 100f, 1.2f, 6f), Eps);
            Assert.AreEqual(6f, LightMath.BaseRadius(100f, 100f, 1.2f, 6f), Eps);
            Assert.AreEqual(3.6f, LightMath.BaseRadius(50f, 100f, 1.2f, 6f), Eps);
        }

        [Test]
        public void Light_WaxAbove100_AddsBufferNotRadius()
        {
            Assert.AreEqual(6f, LightMath.BaseRadius(130f, 100f, 1.2f, 6f), Eps);
        }

        [Test]
        public void Light_FlameMultipliers_AndTrimMinus65Percent()
        {
            float untrimmed = LightMath.UntrimmedRadius(6f, 1.8f, 1f); // white flame
            Assert.AreEqual(10.8f, untrimmed, Eps);
            Assert.AreEqual(10.8f * 0.35f, LightMath.FinalRadius(untrimmed, true, 0.35f), Eps);
            Assert.AreEqual(10.8f, LightMath.FinalRadius(untrimmed, false, 0.35f), Eps);
        }

        [Test]
        public void Detection_IsTwoPlusRadiusTimesOnePointFive()
        {
            Assert.AreEqual(2f + 6f * 1.5f, LightMath.DetectionRadius(6f, false, 2f, 1.5f, 0.4f), Eps);
            Assert.AreEqual(2f + 1.2f * 1.5f, LightMath.DetectionRadius(1.2f, false, 2f, 1.5f, 0.4f), Eps);
        }

        [Test]
        public void Detection_TrimmedIsSixtyPercentLower_AppliedOnce()
        {
            float untrimmed = 6f;
            float expected = (2f + 6f * 1.5f) * 0.4f;
            Assert.AreEqual(expected, LightMath.DetectionRadius(untrimmed, true, 2f, 1.5f, 0.4f), Eps);
        }

        [Test]
        public void Detection_BrighterMeansSpottedFromFurther()
        {
            float dim = LightMath.DetectionRadius(LightMath.BaseRadius(20f, 100f, 1.2f, 6f), false, 2f, 1.5f, 0.4f);
            float bright = LightMath.DetectionRadius(LightMath.BaseRadius(90f, 100f, 1.2f, 6f), false, 2f, 1.5f, 0.4f);
            Assert.Greater(bright, dim);
        }
    }
}
