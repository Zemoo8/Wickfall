using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wickfall.Enemies;
using Wickfall.Meta;
using Wickfall.World;

namespace Wickfall.Tests
{
    /// <summary>Region 6 - Chambre du Foyer: ash, pacts, the Roi Creux's phases, and the three endings (M6).</summary>
    public class Region6Tests : PlayModeFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.HearthChamber);

        static Vector2 Text(int x, int y) => new Vector2(x + 0.5f, 90 - 1 - y + 0.5f);

        static readonly string[] Route =
        {
            "rest 34 82", "read 46 83", "goto 40 70", "read 40 59", "goto 14 54", "read 9 57", "take 7 54",
            "goto 40 55", "rest 63 55", "goto 40 46", "read 43 40", "boss 40 22",
        };

        [UnityTest]
        public IEnumerator Ash_Slows()
        {
            // walk east along a clean row, then along an ash row
            Player.Teleport(Text(28, 73));
            yield return Wait(0.3f);
            Input.Move = Vector2.right;
            yield return Wait(0.5f);
            Assert.IsFalse(Player.Status.InAsh);
            float free = Player.Velocity.magnitude;
            Player.Teleport(Text(28, 76));
            yield return Wait(0.5f);
            Assert.IsTrue(Player.Status.InAsh, "wading through a drift");
            float slowed = Player.Velocity.magnitude;
            Input.Move = Vector2.zero;
            Assert.AreEqual(0.55f, slowed / free, 0.08f, "PDF: cendre qui ralentit (G06: x0.55)");
        }

        [UnityTest]
        public IEnumerator PactOfWax_IsThePdfsOwnExample()
        {
            float maxBefore = Player.Wax.Max;
            Player.Teleport(Text(20, 51));
            yield return WaitUntil(() => Player.Interactor.Current is PactShrine, 3f);
            Input.PressInteract();
            yield return WaitUntil(() => Choice.IsOpen, 2f);
            Assert.IsTrue(Choice.Resolve(0), "accept");
            yield return null;
            Assert.AreEqual(maxBefore + 30f, Player.Wax.Max, 0.01f, "PDF: +30 cire maximale");
            Player.Wax.SetCurrent(80f);
            float start = Player.Wax.Current;
            yield return Wait(2f);
            Assert.AreEqual(1.5f, start - Player.Wax.Current, 0.15f, "PDF: perte x1,5");
            Assert.IsFalse(Player.Interactor.Current is PactShrine && Player.Interactor.Current.CanInteract(Player), "a pact is taken once");
        }

        [UnityTest]
        public IEnumerator King_EachPhaseTakesASupportAway()
        {
            yield return WaitUntil(() => Object.FindFirstObjectByType<BossHollowKing>() != null, 10f);
            var king = Object.FindFirstObjectByType<BossHollowKing>();
            Assert.IsNotNull(king);
            Player.Teleport(Text(40, 29)); // within his wake radius
            yield return WaitUntil(() => king.Current == BossHollowKing.Mode.Fighting, 3f);
            Assert.AreEqual(1, king.Phase);
            var pool = Object.FindObjectsByType<WaxPool>(FindObjectsSortMode.None).OrderBy(p => Vector2.Distance(p.transform.position, king.transform.position)).First();
            Player.Wax.SetCurrent(50f); // thirsty: only the boss can stop it drinking
            Assert.IsFalse(pool.CanInteract(Player), "G13: the arena's pools go dark in phase 1");

            var hit = new Combat.HitData { Amount = Db.Boss.healthPerPhase + 1f, Source = Team.Player, HollowBonus = 1f };
            king.Body.TakeHit(hit);
            yield return WaitUntil(() => king.Phase == 2, 2f);
            Input.TrimHeld = true;
            yield return Wait(0.2f);
            Assert.IsFalse(Player.Dim.IsTrimmed, "PDF phase 2: pas de mode faible");
            Input.TrimHeld = false;

            yield return Wait(1.6f); // the phase shift's invulnerability
            king.Body.TakeHit(hit);
            yield return WaitUntil(() => king.Phase == 3, 2f);
            yield return null;
            Assert.AreEqual(40f, Player.Wax.Max, 0.01f, "PDF phase 3: cire maximale à 40");

            yield return Wait(1.6f);
            king.Body.TakeHit(hit);
            yield return WaitUntil(() => king.Current == BossHollowKing.Mode.Fallen, 2f);
            Assert.AreEqual(BossHollowKing.Mode.Fallen, king.Current, "he falls after phase 3");
            yield return null;
            Assert.Greater(Player.Wax.Max, 40f, "the supports come back");
        }

        IEnumerator ReachEnding(string label, string judge, string ending)
        {
            var run = new RegionBotRunner.Run();
            var route = Route.Concat(new[] { $"judge 40 22 {judge}", $"ending 40 8 {ending}" }).ToList();
            yield return RegionBotRunner.Play(Player, Session, label, run, 420f, route);
            Assert.IsFalse(run.Bot.Failed, run.Bot.FailReason);
            Assert.IsTrue(run.Bot.Done, run.Report);
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator EndingA_RelightTheHearth()
        {
            yield return ReachEnding("R6-A", "extinguish", "A");
            Assert.AreEqual(EndingId.A, Session.LastEnding);
            Assert.IsTrue(Session.Save.HasEnding(EndingId.A));
            Assert.IsFalse(Session.Save.kingSpared);
            Assert.IsTrue(Session.Save.HasRelic(5), "the Hearth Ember");
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator EndingB_KeepTheFlame()
        {
            yield return ReachEnding("R6-B", "spare", "B");
            Assert.AreEqual(EndingId.B, Session.LastEnding);
            Assert.IsTrue(Session.Save.kingSpared);
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator EndingC_PassTheWick_WithSixRelicsAndTheKingSpared()
        {
            for (int i = 0; i < 5; i++)
                Session.CollectRelic(i); // the first five regions' relics
            yield return ReachEnding("R6-C", "spare", "C");
            Assert.AreEqual(EndingId.C, Session.LastEnding);
            Assert.IsTrue(Session.Save.HasEnding(EndingId.C));
        }

        [UnityTest]
        public IEnumerator EndingC_IsRefused_WithoutTheSixRelics()
        {
            Session.Save.kingDecided = true;
            Session.Save.kingSpared = true;
            Player.Teleport(Text(40, 11));
            yield return WaitUntil(() => Player.Interactor.Current is FoyerAltar, 5f);
            Assert.IsInstanceOf<FoyerAltar>(Player.Interactor.Current, "the Foyer's prompt");
            Input.PressInteract();
            yield return WaitUntil(() => Choice.IsOpen, 2f);
            Assert.IsFalse(Choice.Pending.IsEnabled(2), "PDF: C needs the 6 relics");
            Assert.IsFalse(Choice.Resolve(2));
            Assert.IsTrue(Choice.IsOpen, "still asking");
            Choice.Resolve(1);
        }
    }
}
