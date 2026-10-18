using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wickfall.DevTools;
using Wickfall.Enemies;
using Wickfall.Meta;
using Wickfall.UI;
using Wickfall.World;

namespace Wickfall.Tests
{
    /// <summary>Region 1 - Champs de Suif, played for real (M2 exit criterion: "a bot clears R1").</summary>
    public class Region1Tests : PlayModeFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.TallowFields);

        [UnityTest]
        public IEnumerator Region1_LoadsRoomGroups_AndTheNavGrid()
        {
            yield return WaitUntil(() => RoomLoader.Instance != null && RoomLoader.Instance.LoadedCount > 0 && RoomLoader.Instance.Settled, 10f);
            Assert.IsNotNull(NavGrid.Active, "navigation grid");
            Assert.Greater(RoomLoader.Instance.LoadedCount, 0, "room groups near the start are loaded");
            Assert.Less(RoomLoader.Instance.LoadedCount, RoomLoader.Instance.groups.Length, "far groups stay unloaded");
            Assert.AreEqual(RegionId.TallowFields, Session.Region);
            Assert.IsTrue(NavGrid.Active.IsWalkable(LevelData.CellOf(Player.transform.position)), "the player starts on walkable ground");
        }

        [UnityTest]
        public IEnumerator Region1_Driplings_NoticeABrightPlayer_NotATrimmedOne()
        {
            yield return WaitUntil(() => EnemyBrain.All.Count > 0, 10f);
            // The two Driplings of the first clearing.
            EnemyBrain dripling = null;
            foreach (var e in EnemyBrain.All)
                if (dripling == null || e.transform.position.x < dripling.transform.position.x && e.type.kind == EnemyKind.Dripling)
                    dripling = e;
            Assert.IsNotNull(dripling);
            Vector2 home = dripling.transform.position;

            // Trimmed at 6 units: detection (2 + 6 x 1.5) x 0.4 = 4.4 < 6, unseen.
            Player.Teleport(home + new Vector2(-6f, 0f));
            Input.TrimHeld = true;
            yield return Wait(1.2f);
            Assert.IsFalse(dripling.Perception.CanSeePlayer, "trimmed: not spotted at 6 units");
            // Bright at 6 units: detection 2 + 6 x 1.5 = 11 > 6, spotted (wax refilled so the radius is 6).
            Player.Wax.SetCurrent(100f);
            Input.TrimHeld = false;
            yield return Wait(1.2f);
            Assert.IsTrue(dripling.Perception.InDetectionRange, "shining: spotted from 6 units");
        }

        [UnityTest]
        public IEnumerator Region1_BotClearsTheRegion()
        {
            yield return WaitUntil(() => RegionDirector.Instance != null && RoomLoader.Instance != null && RoomLoader.Instance.Settled, 10f);
            ScoreScreen.AutoContinueSeconds = 0.5f;
            Player.SetInputSource(null);
            var bot = Player.gameObject.AddComponent<RegionBot>();
            bot.Begin(Player, RegionDirector.Instance.Level);
            Time.timeScale = 4f;
            float real = 0f;
            while (!bot.Done && !bot.Failed && !RegionDirector.Instance.Completed && real < 420f)
            {
                real += Time.unscaledDeltaTime;
                yield return null;
            }
            Time.timeScale = 1f;
            var result = RegionDirector.Instance.LastResult;
            string report = $"R1 bot: done={bot.Done} failed={bot.Failed} reason={bot.FailReason} step={bot.Step}/{bot.StepCount} " +
                            $"kills={bot.Kills} deaths={bot.Deaths} gameTime={Session.Tracker.Time:0}s score={result.Score} rank={result.Rank} " +
                            $"wax%={result.WaxPercent:0} avoided={result.EnemiesAvoided}";
            Debug.Log("[WICKFALL Bot] " + report);
            Directory.CreateDirectory("Logs");
            File.AppendAllText("Logs/bot_runs.txt", System.DateTime.Now.ToString("s") + " " + report + "\n");
            Assert.IsFalse(bot.Failed, bot.FailReason);
            Assert.IsTrue(RegionDirector.Instance.Completed, "the region was completed");
            Assert.IsTrue(Session.Save.HasRelic(0), "the Ember Feather was taken");
            Assert.IsTrue(Session.Save.HasFlame(FlameId.Blue), "the Blue flame was found");
            Assert.AreEqual(2, Player.Dodger.MaxCharges, "the relic gives one more dodge");
            Assert.IsTrue(Session.Save.regions[0].cleared);
        }
    }
}
