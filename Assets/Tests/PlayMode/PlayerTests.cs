using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wickfall.Combat;
using Wickfall.World;

namespace Wickfall.Tests
{
    /// <summary>The player's PDF rules, played for real in the Light Lab (PDF §2.1, §2.3, §4).</summary>
    public class PlayerTests : PlayModeFixture
    {
        [UnityTest]
        public IEnumerator Wax_DrainsHalfAPerSecond_StandingStill()
        {
            float before = Player.Wax.Current;
            yield return Wait(2f);
            float lost = before - Player.Wax.Current;
            Assert.AreEqual(1f, lost, 0.15f, "0.5 wax per second with the amber flame");
            Assert.AreEqual("Idle", Player.StateName);
        }

        [UnityTest]
        public IEnumerator Walk_MovesThePlayer_AndFacesTheDirection()
        {
            Vector2 start = Player.transform.position;
            Input.Move = Vector2.right;
            yield return Wait(0.6f);
            Input.Move = Vector2.zero;
            Assert.Greater(Player.transform.position.x - start.x, 1.5f);
            Assert.AreEqual(Facing.Right, Player.Facing);
        }

        [UnityTest]
        public IEnumerator Trim_CutsRadius65_Loss75_Detection60()
        {
            yield return Wait(0.3f);
            var normal = Player.Light.State;
            float normalLoss = Player.Wax.LossPerSecond;
            Input.TrimHeld = true;
            yield return Wait(0.6f);
            var trimmed = Player.Light.State;
            Assert.IsTrue(Player.Dim.IsTrimmed);
            Assert.AreEqual("Dim", Player.StateName);
            Assert.AreEqual(normal.UntrimmedRadius * 0.35f, trimmed.Radius, 0.08f, "rayon -65 %");
            Assert.AreEqual(normalLoss * 0.25f, Player.Wax.LossPerSecond, 0.01f, "perte -75 %");
            float expected = (2f + trimmed.UntrimmedRadius * 1.5f) * 0.4f;
            Assert.AreEqual(expected, trimmed.DetectionRadius, 0.05f, "détection -60 %");
            Input.TrimHeld = false;
            yield return Wait(0.4f);
            Assert.IsFalse(Player.Dim.IsTrimmed);
        }

        [UnityTest]
        public IEnumerator Light_FollowsWax_AndThePdfDetectionFormula()
        {
            Player.Wax.SetCurrent(100f);
            yield return Wait(0.8f);
            var full = Player.Light.State;
            Assert.AreEqual(6f, full.UntrimmedRadius, 0.1f);
            Assert.AreEqual(2f + 6f * 1.5f, full.DetectionRadius, 0.2f);
            Player.Wax.SetCurrent(10f);
            yield return Wait(1.2f);
            var low = Player.Light.State;
            Assert.AreEqual(Mathf.Lerp(1.2f, 6f, 0.1f), low.UntrimmedRadius, 0.1f);
            Assert.Less(low.Radius, full.Radius, "each hit shrinks what you can see");
        }

        [UnityTest]
        public IEnumerator FlameSwitch_Costs8Wax_AndFlares()
        {
            Player.Wax.SetCurrent(80f);
            int flares = 0;
            System.Action<FlareInfo> onFlare = _ => flares++;
            Events.Flare.Register(onFlare);
            yield return null;
            float before = Player.Wax.Current;
            Input.SelectSlot(1); // blue
            yield return null;
            yield return null;
            Events.Flare.Unregister(onFlare);
            Assert.AreEqual(FlameId.Blue, Player.Flames.Current);
            Assert.AreEqual(8f, before - Player.Wax.Current, 0.1f, "Changer de flamme coûte 8 de cire");
            Assert.AreEqual(1, flares, "provoque un éclat qui attire les ennemis");
            Assert.AreEqual(Db.Flame(FlameId.Blue).lightColor, Player.Light.State.Color);
        }

        [UnityTest]
        public IEnumerator Dodge_UsesACharge_AndGivesInvulnerability()
        {
            Assert.AreEqual(1, Player.Dodger.Charges);
            Input.Move = Vector2.left;
            yield return null;
            Input.PressDodge();
            yield return null;
            yield return null;
            Assert.AreEqual("Dodge", Player.StateName);
            Assert.AreEqual(0, Player.Dodger.Charges);
            Assert.IsTrue(Player.IsDodgeInvulnerable);
            bool hurt = Player.Body.TakeHit(HitData.FromEnemy(10f, Player.transform.position, Vector2.up));
            Assert.IsFalse(hurt, "invulnerable while dodging");
            Input.Move = Vector2.zero;
            yield return Wait(1.1f);
            Assert.AreEqual(1, Player.Dodger.Charges, "recharges");
        }

        [UnityTest]
        public IEnumerator Attack_HitsTheEffigy_ArmoredBlocksAmber_WhiteHurtsIt()
        {
            var effigies = Object.FindObjectsByType<TrainingDummy>(FindObjectsSortMode.None);
            var plain = effigies.Select(e => e.GetComponent<Damageable>()).First(d => d.tags == DamageTags.None);
            var armored = effigies.Select(e => e.GetComponent<Damageable>()).First(d => (d.tags & DamageTags.Armored) != 0);

            Player.Teleport(plain.Position + new Vector2(0f, -1.2f));
            yield return null;
            Input.Move = Vector2.up; // a short step to face the effigy
            yield return Wait(0.08f);
            Input.Move = Vector2.zero;
            yield return Wait(0.15f);
            Assert.AreEqual(Facing.Up, Player.Facing);
            Player.Teleport(plain.Position + new Vector2(0f, -1.2f));
            Input.PressAttack();
            yield return Wait(0.5f);
            Assert.Less(plain.Health, plain.maxHealth, "the amber arc hits");

            Player.Teleport(armored.Position + new Vector2(0f, -1.2f));
            yield return null;
            Input.PressAttack();
            yield return Wait(0.5f);
            Assert.AreEqual(armored.maxHealth, armored.Health, 0.01f, "Scorifiés: only white or green");

            Input.SelectSlot(3); // white
            yield return Wait(0.2f);
            Assert.AreEqual(FlameId.White, Player.Flames.Current);
            Input.PressAttack();
            yield return Wait(0.5f);
            Assert.Less(armored.Health, armored.maxHealth, "the white flame hurts it");
        }

        [UnityTest]
        public IEnumerator Hit_RemovesWax_LeavesADrop_AndHurts()
        {
            yield return Wait(0.2f);
            var field = Object.FindFirstObjectByType<WaxDropField>();
            int drops = field.Count;
            float before = Player.Wax.Current;
            Assert.IsTrue(Player.Body.TakeHit(HitData.FromEnemy(10f, (Vector2)Player.transform.position + Vector2.right, Vector2.left)));
            Assert.AreEqual(10f, before - Player.Wax.Current, 0.05f);
            yield return null;
            Assert.AreEqual("Hurt", Player.StateName);
            Assert.AreEqual(drops + 1, field.Count, "a wax drop is left where the hit landed");
        }

        [UnityTest]
        public IEnumerator Soot_TakesTen_ThenMakesHitsHurt125Percent_For1Point5Seconds()
        {
            Player.Wax.SetCurrent(90f);
            var soot = HitData.FromEnemy(10f, Player.transform.position, Vector2.up);
            soot.Effects = HitEffects.Soot;
            Player.Body.TakeHit(soot);
            Assert.AreEqual(80f, Player.Wax.Current, 0.05f, "Soufflets: -10 cire");
            Assert.IsTrue(Player.Status.Sooted);
            yield return Wait(Db.Balance.invulnerableAfterHurt + 0.05f);
            float before = Player.Wax.Current;
            Player.Body.TakeHit(HitData.FromEnemy(8f, Player.transform.position, Vector2.up));
            Assert.AreEqual(10f, before - Player.Wax.Current, 0.05f, "Dégâts x1,25 while sooted");
            yield return Wait(1.6f);
            Assert.IsFalse(Player.Status.Sooted, "soot lasts 1.5 s");
        }

        [UnityTest]
        public IEnumerator Extinguished_AtZeroWax_ThenBackToTheBrazierWithFullWax()
        {
            Player.Wax.Damage(Player.Wax.Current + 1f);
            yield return null;
            Assert.AreEqual("Extinguished", Player.StateName);
            yield return WaitUntil(() => Player.StateName != "Extinguished", 6f);
            Assert.AreEqual("Idle", Player.StateName);
            Assert.AreEqual(Player.Wax.Max, Player.Wax.Current, 0.5f, "relit full at the brazier");
        }

        [UnityTest]
        public IEnumerator Brazier_Rest_RefillsOncePerRest()
        {
            var brazier = Object.FindFirstObjectByType<Brazier>();
            Player.Wax.SetCurrent(40f);
            Player.Teleport(brazier.RespawnPosition);
            yield return Wait(0.2f);
            Assert.AreSame(brazier, Player.Interactor.Current, "the interactor finds the brazier");
            Assert.AreEqual("Idle", Player.StateName);
            Input.PressInteract();
            yield return Wait(0.2f);
            Assert.Greater(Player.Wax.Current, 99f, "remplit toute la cire");
            Player.Wax.SetCurrent(40f);
            yield return Wait(0.2f);
            Input.PressInteract();
            yield return Wait(0.2f);
            Assert.Less(Player.Wax.Current, 41f, "un usage par repos (G08)");
        }

        [UnityTest]
        public IEnumerator Interact_PressedAtTheEndOfASwing_IsBuffered()
        {
            var brazier = Object.FindFirstObjectByType<Brazier>();
            Player.Wax.SetCurrent(40f);
            Player.Teleport(brazier.RespawnPosition);
            yield return Wait(0.2f);
            Input.PressAttack();
            yield return WaitUntil(() => Player.AttackProgress >= 0.7f, 1f);
            Assert.AreEqual("Attack", Player.StateName, "still swinging");
            Input.PressInteract();
            yield return Wait(0.4f);
            Assert.Greater(Player.Wax.Current, 99f, "the rest happens once the swing ends");
        }

        [UnityTest]
        public IEnumerator Geyser_Telegraphs1Point2Seconds_ThenHitsFor10()
        {
            var geyser = Object.FindObjectsByType<WaxGeyser>(FindObjectsSortMode.None).OrderBy(g => g.phaseOffset).First();
            yield return WaitUntil(() => geyser.IsWarning, 12f);
            Assert.IsTrue(geyser.IsWarning);
            Player.Teleport(geyser.transform.position);
            Player.Wax.SetCurrent(90f);
            float warnedAt = Time.time;
            yield return WaitUntil(() => geyser.IsErupting, 3f);
            Assert.AreEqual(1.2f, Time.time - warnedAt, 0.15f, "annoncés 1,2 s");
            float drained = (Time.time - warnedAt + 0.1f) * Db.Wax.passiveLossPerSecond;
            yield return Wait(0.1f);
            Assert.AreEqual(90f - 10f - drained, Player.Wax.Current, 0.15f, "10 dégâts");
        }
    }
}
