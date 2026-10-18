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
    /// <summary>Region 3 - Fonderies de Suie: presses, belts, steam, Bellows soot, and a bot that clears it (M4).</summary>
    public class Region3Tests : PlayModeFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.SootFoundries);

        static Vector2 Text(int x, int y) => new Vector2(x + 0.5f, 66 - 1 - y + 0.5f);

        [UnityTest]
        public IEnumerator EnteringR3_GivesTheGreenFlame_SlagbornCanBeFought()
        {
            yield return null;
            Assert.IsTrue(Session.Save.HasFlame(FlameId.Green), "G47: R2's flame is guaranteed on entering R3");
            Assert.IsTrue(Player.Flames.IsEquipped(FlameId.Green), "and equipped in a free slot");
        }

        [UnityTest]
        public IEnumerator Press_WarnsFor0Point9Seconds_ThenSlamsFor35()
        {
            yield return WaitUntil(() => Press.All.Count > 0, 10f);
            var press = Press.All.OrderBy(p => Vector2.Distance(p.transform.position, Player.transform.position)).First();
            yield return WaitUntil(() => press.IsWarning, 6f);
            Assert.IsTrue(press.IsWarning);
            float warnedAt = Time.time;
            Player.Wax.SetCurrent(100f);
            Player.Teleport(press.transform.position);
            yield return WaitUntil(() => press.IsSlamming, 2f);
            Assert.AreEqual(0.9f, Time.time - warnedAt, 0.1f, "the warning lasts 0.9 s");
            yield return Wait(0.2f);
            Assert.AreEqual(65f, Player.Wax.Current, 1.5f, "PDF: presses deal 35");
        }

        [UnityTest]
        public IEnumerator Conveyor_CarriesAStandingPlayer()
        {
            Player.Teleport(Text(60, 54));
            yield return WaitUntil(() => Object.FindObjectsByType<Conveyor>(FindObjectsSortMode.None).Length > 0, 10f);
            yield return Wait(0.3f);
            Vector2 start = Player.transform.position;
            yield return Wait(1.0f);
            Vector2 moved = (Vector2)Player.transform.position - start;
            Assert.Less(moved.x, -1.8f, "carried west at about 2.4 u/s");
            Assert.Less(Mathf.Abs(moved.y), 0.3f);
        }

        [UnityTest]
        public IEnumerator SteamJet_PushesAlongItsColumn_AndScalds()
        {
            Player.Teleport(Text(60, 50));
            yield return WaitUntil(() => Object.FindObjectsByType<SteamJet>(FindObjectsSortMode.None).Length > 0, 10f);
            var jet = Object.FindObjectsByType<SteamJet>(FindObjectsSortMode.None)
                .Where(j => j.direction == Vector2.down)
                .OrderBy(j => Vector2.Distance(j.transform.position, Player.transform.position)).First();
            yield return WaitUntil(() => jet.Current == SteamJet.Phase.Warning, 6f);
            Player.Wax.SetCurrent(100f);
            Player.Teleport((Vector2)jet.transform.position + Vector2.down * 2f);
            yield return WaitUntil(() => jet.IsVenting, 2f);
            Vector2 start = Player.transform.position;
            yield return Wait(0.6f);
            Assert.Less(((Vector2)Player.transform.position - start).y, -1.5f, "pushed down the column");
            Assert.AreEqual(94f, Player.Wax.Current, 1.5f, "scalded once (6)");
        }

        [UnityTest]
        public IEnumerator Bellows_SootRemoves10Wax_ThenSootsThePlayer()
        {
            Player.Teleport(Text(96, 48));
            yield return WaitUntil(() => EnemyBrain.All.Any(e => e is BellowsBrain), 10f);
            Player.Wax.SetCurrent(100f);
            yield return WaitUntil(() => Player.Status.Sooted, 12f);
            Assert.IsTrue(Player.Status.Sooted, "hit by a soot ball");
            Assert.Less(Player.Wax.Current, 92f, "-10 wax");
            Assert.Greater(Player.Wax.Current, 86f, "only the soot hit (and a little passive loss)");
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator Region3_BotClearsTheRegion()
        {
            var run = new RegionBotRunner.Run();
            yield return RegionBotRunner.Play(Player, Session, "R3", run);
            Assert.IsFalse(run.Bot.Failed, run.Bot.FailReason);
            Assert.IsTrue(run.Completed, run.Report);
            Assert.IsTrue(Session.Save.HasRelic(2), "the max-wax relic");
            Assert.IsTrue(Session.Save.HasFlame(FlameId.White), "the White flame");
        }
    }
}
