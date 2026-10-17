using NUnit.Framework;
using UnityEditor;
using Wickfall.EditorTools;

namespace Wickfall.Tests
{
    /// <summary>
    /// Every number the PDF gives, checked against the SHIPPED data assets, so no value can drift
    /// away from Wickfall_Idee_Complete.pdf unnoticed. The expected numbers are typed here again on
    /// purpose (not read from PdfTables) so a wrong table is caught too.
    /// </summary>
    public class PdfValuesTests
    {
        const float Eps = 1e-4f;
        GameDatabase db;

        [OneTimeSetUp]
        public void Load()
        {
            db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DataFactory.DatabasePath);
            Assert.IsNotNull(db, "Run WICKFALL > Setup first: " + DataFactory.DatabasePath + " is missing.");
        }

        // PDF §2.1 Cire : 100 au maximum. Perte passive de 0,5 par seconde.
        [Test]
        public void Wax_Max100_Loss0Point5PerSecond()
        {
            Assert.AreEqual(100f, db.wax.maxWax, Eps);
            Assert.AreEqual(0.5f, db.wax.passiveLossPerSecond, Eps);
        }

        // PDF §2.1 Lumière : rayon de 1,2 à 6 unités. §2.3 Détection = 2 + rayon x 1,5.
        [Test]
        public void Light_Radius1Point2To6_Detection2PlusRadiusTimes1Point5()
        {
            Assert.AreEqual(1.2f, db.light.minRadius, Eps);
            Assert.AreEqual(6f, db.light.maxRadius, Eps);
            Assert.AreEqual(2f, db.light.detectionBase, Eps);
            Assert.AreEqual(1.5f, db.light.detectionPerRadius, Eps);
        }

        // PDF §2.1 Tailler la mèche : rayon -65 %, perte -75 %, détection -60 %, dégâts -50 %, plus d'arts.
        [Test]
        public void Trim_Minus65Minus75Minus60Minus50_NoArts()
        {
            Assert.AreEqual(0.35f, db.dim.radiusMultiplier, Eps);
            Assert.AreEqual(0.25f, db.dim.waxLossMultiplier, Eps);
            Assert.AreEqual(0.4f, db.dim.detectionMultiplier, Eps);
            Assert.AreEqual(0.5f, db.dim.damageMultiplier, Eps);
            Assert.IsFalse(db.dim.flameArtsAllowed);
        }

        // PDF §2.1 flame table.
        [TestCase(FlameId.Amber, 1.0f, 1.0f, 1.0f)]
        [TestCase(FlameId.Blue, 0.9f, 0.7f, 0.85f)]
        [TestCase(FlameId.Green, 1.4f, 0.9f, 1.1f)]
        [TestCase(FlameId.White, 1.2f, 1.6f, 1.8f)]
        public void Flame_CostDamageLight(FlameId id, float cost, float damage, float light)
        {
            var flame = db.Flame(id);
            Assert.AreEqual(id, flame.id);
            Assert.AreEqual(cost, flame.waxCostMultiplier, Eps, "Coût en cire");
            Assert.AreEqual(damage, flame.damageMultiplier, Eps, "Dégâts");
            Assert.IsFalse(flame.damageScalesWithLowWax);
            Assert.AreEqual(light, flame.lightMultiplier, Eps, "Lumière");
        }

        [Test]
        public void Flame_Crimson_Cost1Point6_Damage0Point6To2Point2_Light1Point3()
        {
            var crimson = db.Flame(FlameId.Crimson);
            Assert.AreEqual(1.6f, crimson.waxCostMultiplier, Eps);
            Assert.IsTrue(crimson.damageScalesWithLowWax);
            Assert.AreEqual(0.6f, crimson.damageAtFullWax, Eps);
            Assert.AreEqual(2.2f, crimson.damageAtEmptyWax, Eps);
            Assert.AreEqual(1.3f, crimson.lightMultiplier, Eps);
        }

        [Test]
        public void Flame_RolesMatchThePdf()
        {
            Assert.AreEqual(FlameAttackStyle.MeleeArc, db.Flame(FlameId.Amber).attackStyle, "arc de mêlée");
            Assert.AreEqual(FlameAttackStyle.Bolt, db.Flame(FlameId.Blue).attackStyle, "à distance");
            Assert.Less(db.Flame(FlameId.Blue).slowFactor, 1f, "ralentit");
            Assert.AreEqual(FlameAttackStyle.PoisonCloud, db.Flame(FlameId.Green).attackStyle, "poison");
            Assert.Greater(db.Flame(FlameId.Green).poisonDamagePerSecond, 0f, "poison");
            Assert.Greater(db.Flame(FlameId.White).hollowBonus, 1f, "anti-Creux");
        }

        // PDF §2.1 Changer de flamme coûte 8 de cire.
        [Test]
        public void FlameSwitch_Costs8()
        {
            Assert.AreEqual(8f, db.wax.flameSwitchCost, Eps);
        }

        // PDF §2.3 Tuer un ennemi : gain de 4 à 15 de cire.
        [Test]
        public void KillRewards_Between4And15()
        {
            Assert.AreEqual(4, db.wax.killRewardMin);
            Assert.AreEqual(15, db.wax.killRewardMax);
            foreach (var enemy in db.enemies)
            {
                if (enemy.kind == EnemyKind.Waxfly || enemy.kind == EnemyKind.HollowKing)
                    continue; // harmless ambient life and the boss (spared or extinguished, never "rewarded")
                Assert.GreaterOrEqual(enemy.killWaxMin, 4, enemy.name);
                Assert.LessOrEqual(enemy.killWaxMax, 15, enemy.name);
                Assert.LessOrEqual(enemy.killWaxMin, enemy.killWaxMax, enemy.name);
            }
        }

        // PDF §2.2 obstacles and §2.3 soot.
        [Test]
        public void Hazards_PdfNumbers()
        {
            Assert.AreEqual(10f, db.Hazard(HazardKind.WaxGeyser).damage, Eps, "Geysers de cire (10 dégâts)");
            Assert.AreEqual(1.2f, db.Hazard(HazardKind.WaxGeyser).telegraphSeconds, Eps, "annoncés 1,2 s");
            Assert.AreEqual(3f, db.Hazard(HazardKind.LightEaterBloom).lightEffectSeconds, Eps, "lumière coupée 3 s");
            Assert.AreEqual(35f, db.Hazard(HazardKind.Press).damage, Eps, "Presses (35 dégâts)");
            Assert.AreEqual(25f, db.Hazard(HazardKind.Laser).damage, Eps, "Lasers (25 dégâts)");
            Assert.AreEqual(10f, db.Hazard(HazardKind.Soot).damage, Eps, "Soufflets (-10 cire)");
            Assert.AreEqual(1.25f, db.Hazard(HazardKind.Soot).damageTakenMultiplier, Eps, "Dégâts x1,25");
            Assert.AreEqual(1.5f, db.Hazard(HazardKind.Soot).lightEffectSeconds, Eps, "perte de lumière pendant 1,5 s");
            Assert.Less(db.Hazard(HazardKind.Ash).moveMultiplier, 1f, "Cendre qui ralentit");
        }

        // PDF §2.2 region order and environments.
        [Test]
        public void Regions_InPdfOrder_WithTheirEnvironmentRules()
        {
            string[] keys = { "region.tallow", "region.wickwood", "region.foundries", "region.cathedral", "region.hollow", "region.hearth" };
            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual((RegionId)i, db.regions[i].id);
                Assert.AreEqual(keys[i], db.regions[i].nameKey);
            }
            Assert.AreEqual(1.5f, db.Region(RegionId.DeepHollow).waxLossMultiplier, Eps, "Perte de cire x1,5");
            Assert.AreEqual(0f, db.Region(RegionId.DeepHollow).ambientIntensity, Eps, "Aucune lumière ambiante");
            Assert.IsTrue(db.Region(RegionId.DeepHollow).lightRevealGround, "carte visible seulement où vous brillez");
            Assert.IsTrue(db.Region(RegionId.DeepHollow).heartbeat, "battement de coeur dans le Creux Profond");
            Assert.IsTrue(db.Region(RegionId.HearthChamber).negativeLight, "Foyer en lumière négative");
            Assert.Less(db.Region(RegionId.TallowFields).ambientIntensity, 0.3f, "lumière ambiante faible");
            for (int i = 0; i < 4; i++)
                Assert.AreEqual(1f, db.regions[i].waxLossMultiplier, Eps);
        }

        // PDF §2.2 creature behaviours that live in data.
        [Test]
        public void Enemies_TagsFromThePdf()
        {
            Assert.IsTrue((db.Enemy(EnemyKind.Slagborn).tags & DamageTags.Armored) != 0, "Scorifiés (armurés)");
            Assert.AreEqual(0f, db.Enemy(EnemyKind.Waxfly).attackDamage, Eps, "Cirelles (inoffensives)");
            Assert.AreEqual(0f, db.Enemy(EnemyKind.Waxfly).contactDamage, Eps, "Cirelles (inoffensives)");
            Assert.IsFalse(db.Enemy(EnemyKind.Waxfly).countsForAvoided);
        }

        // PDF §2.4 relics, pacts, brazier and score.
        [Test]
        public void Progression_SixRelics_PdfPactExample_Score()
        {
            Assert.AreEqual(6, db.relics.Length, "6 Reliques de Braise (une par région)");
            for (int i = 0; i < 6; i++)
                Assert.AreEqual((RegionId)i, db.relics[i].region, "une par région");
            Assert.AreEqual(RelicEffect.ExtraDodge, db.relics[0].effect, "esquive en plus");
            Assert.AreEqual(RelicEffect.WallGrip, db.relics[1].effect, "accroche au mur");
            Assert.AreEqual(RelicEffect.MaxWax, db.relics[2].effect, "cire maximale");
            Assert.AreEqual(RelicEffect.FlameSlot, db.relics[3].effect, "emplacement de flamme en plus");

            var pdfPact = db.pacts[0];
            Assert.IsTrue(pdfPact.fromPdf);
            Assert.AreEqual(PactStat.MaxWax, pdfPact.gainStat);
            Assert.AreEqual(30f, pdfPact.gainValue, Eps, "+30 cire maximale");
            Assert.AreEqual(PactStat.WaxLoss, pdfPact.costStat);
            Assert.AreEqual(1.5f, pdfPact.costValue, Eps, "perte x1,5");

            Assert.AreEqual(10f, db.score.waxPercentFactor, Eps, "cire restante en % x 10");
            Assert.AreEqual(500, db.score.pointsPerRelic, "500 par relique");
            Assert.AreEqual(20, db.score.pointsPerAvoidedEnemy, "20 par ennemi évité");
            Assert.AreEqual(-150, db.score.pointsPerDeath, "moins 150 par mort");
            Assert.Less(db.score.braiseMin, db.score.flammeMin);
            Assert.Less(db.score.flammeMin, db.score.brasierMin);
        }

        // PDF §4 BossHollowKing: pas de mode faible, puis cire maximale à 40.
        [Test]
        public void Boss_Phase2NoTrim_Phase3MaxWax40()
        {
            Assert.IsTrue(db.boss.phase2DisablesTrim);
            Assert.AreEqual(40f, db.boss.phase3MaxWax, Eps);
        }
    }
}
