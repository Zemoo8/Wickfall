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
    /// <summary>Region 2 - Bois-Mèche: Lurkers, Blooms, Husks, vines, and a bot that clears it (M4).</summary>
    public class Region2Tests : PlayModeFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.Wickwood);

        /// <summary>World position of a cell given in level-text coordinates (y down).</summary>
        static Vector2 Text(int x, int y) => new Vector2(x + 0.5f, 72 - 1 - y + 0.5f);

        [UnityTest]
        public IEnumerator Lurker_MovesOnlyWhileThePlayerShines()
        {
            // the Lurker clearing is in the SouthEast room group: go there so it streams in
            Player.Teleport(Text(44, 44));
            yield return WaitUntil(() => EnemyBrain.All.Any(e => e is LurkerBrain), 15f);
            var lurker = EnemyBrain.All.OfType<LurkerBrain>().First();
            Player.Wax.SetCurrent(100f);
            Player.Teleport((Vector2)lurker.transform.position + new Vector2(-4f, 0f));
            yield return Wait(1.5f); // seen, alerted, chasing
            Assert.IsTrue(lurker.Perception.HasLastKnown, "the Lurker noticed the shining player");
            Input.TrimHeld = true;
            yield return Wait(0.6f);
            Vector2 frozenAt = lurker.transform.position;
            yield return Wait(1.0f);
            Assert.Less(Vector2.Distance(frozenAt, lurker.transform.position), 0.15f, "frozen while the wick is trimmed");
            Assert.AreEqual(EnemyAnim.Special, lurker.Anim, "frozen pose");
        }

        [UnityTest]
        public IEnumerator Bloom_CutsTheLightFor3Seconds()
        {
            yield return WaitUntil(() => Object.FindObjectsByType<LightEaterBloom>(FindObjectsSortMode.None).Length > 0, 15f);
            var bloom = Object.FindObjectsByType<LightEaterBloom>(FindObjectsSortMode.None).First();
            Player.Wax.SetCurrent(100f);
            Player.Teleport((Vector2)bloom.transform.position + new Vector2(0f, -2.5f));
            yield return WaitUntil(() => Player.Status.LightCut, 3f);
            Assert.IsTrue(Player.Status.LightCut, "lumière coupée");
            float cutAt = Time.time;
            yield return Wait(0.5f);
            Assert.Less(Player.Light.State.Radius, 1f, "the light is down to an ember");
            Player.Teleport((Vector2)bloom.transform.position + new Vector2(0f, -12f));
            yield return WaitUntil(() => !Player.Status.LightCut, 5f);
            Assert.AreEqual(3f, Time.time - cutAt, 0.2f, "3 s");
        }

        [UnityTest]
        public IEnumerator Vine_RefusesWithoutTheRelic_ThenClimbs()
        {
            yield return WaitUntil(() => Object.FindObjectsByType<Vine>(FindObjectsSortMode.None).Length > 0, 15f);
            var vine = Object.FindObjectsByType<Vine>(FindObjectsSortMode.None).OrderBy(v => -v.bottom.x).First();
            Player.Teleport(vine.bottom);
            yield return Wait(0.3f);
            Input.PressInteract();
            yield return Wait(1f);
            Assert.Less(Vector2.Distance(Player.transform.position, vine.bottom), 1f, "no grip: the player stays below");
            Session.CollectRelic(1);
            Assert.IsTrue(Session.Modifiers.WallGrip);
            Player.Teleport(vine.bottom);
            yield return Wait(0.3f);
            Input.PressInteract();
            yield return Wait(3f);
            Assert.Less(Vector2.Distance(Player.transform.position, vine.top), 1f, "climbed to the top");
        }

        [UnityTest]
        public IEnumerator Nav_BlocksTheCellsOfSolidProps_LikeTheBellPosts()
        {
            Player.Teleport(Text(41, 19));
            yield return WaitUntil(() => Object.FindObjectsByType<GreatBell>(FindObjectsSortMode.None).Length > 0
                                         && RoomLoader.Instance.Settled, 15f);
            yield return Wait(0.2f);
            var bell = Object.FindObjectsByType<GreatBell>(FindObjectsSortMode.None).First();
            var cell = LevelData.CellOf(bell.transform.position);
            var nav = RegionDirector.Instance.Nav;
            Assert.IsFalse(nav.IsWalkable(cell + Vector2Int.left), "left post");
            Assert.IsFalse(nav.IsWalkable(cell + Vector2Int.right), "right post");
            Assert.IsTrue(nav.IsWalkable(cell + Vector2Int.down * 2), "open floor in front of the bell");
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator Region2_BotClearsTheRegion()
        {
            var run = new RegionBotRunner.Run();
            yield return RegionBotRunner.Play(Player, Session, "R2", run);
            Assert.IsFalse(run.Bot.Failed, run.Bot.FailReason);
            Assert.IsTrue(run.Completed, run.Report);
            Assert.IsTrue(Session.Save.HasRelic(1), "the wall-grip relic");
            Assert.IsTrue(Session.Save.HasFlame(FlameId.Green), "the Green flame");
        }
    }
}
