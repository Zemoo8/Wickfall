using System.Collections;
using System.IO;
using UnityEngine;
using Wickfall.DevTools;
using Wickfall.Meta;
using Wickfall.Player;
using Wickfall.UI;
using Wickfall.World;

namespace Wickfall.Tests
{
    /// <summary>Runs a region bot at 4x speed and records the run in Logs/bot_runs.txt.</summary>
    public static class RegionBotRunner
    {
        public sealed class Run
        {
            public RegionBot Bot;
            public RegionResult Result;
            public string Report;
            public bool Completed;
        }

        public static IEnumerator Play(PlayerController player, GameSession session, string label, Run run, float realSeconds = 420f,
            System.Collections.Generic.IList<string> route = null, float speed = 4f, System.Action perFrame = null)
        {
            float waited = 0f;
            while ((RegionDirector.Instance == null || RoomLoader.Instance == null || !RoomLoader.Instance.Settled) && waited < 10f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            ScoreScreen.AutoContinueSeconds = 0.5f;
            player.SetInputSource(null);
            var bot = player.gameObject.AddComponent<RegionBot>();
            bot.Begin(player, RegionDirector.Instance.Level, route);
            run.Bot = bot;
            Time.timeScale = speed;
            float real = 0f;
            float nextLog = 15f;
            var director = RegionDirector.Instance;
            while (!bot.Done && !bot.Failed && !director.Completed && real < realSeconds)
            {
                real += Time.unscaledDeltaTime;
                perFrame?.Invoke();
                if (real >= nextLog)
                {
                    nextLog += 15f;
                    var cell = LevelData.CellOf(player.transform.position);
                    Debug.Log($"[WICKFALL Bot] {label} t={real:0}s step {bot.Step}/{bot.StepCount} '{bot.CurrentStep}' at text {cell.x} {director.Level.Height - 1 - cell.y} " +
                              $"state={player.StateName} wax={player.Wax.Current:0} deaths={bot.Deaths} kills={bot.Kills} flame={player.Flames.Current} " +
                              $"loadout={string.Join(",", player.Flames.Loadout)} {bot.Note}");
                }
                yield return null;
            }
            Time.timeScale = 1f;
            run.Completed = director.Completed;
            run.Result = director.LastResult;
            run.Report = $"{label} bot: completed={director.Completed} failed={bot.Failed} reason={bot.FailReason} step={bot.Step}/{bot.StepCount} " +
                         $"kills={bot.Kills} deaths={bot.Deaths} gameTime={session.Tracker.Time:0}s score={run.Result.Score} rank={run.Result.Rank} " +
                         $"wax%={run.Result.WaxPercent:0} avoided={run.Result.EnemiesAvoided} realTime={real:0}s";
            Debug.Log("[WICKFALL Bot] " + run.Report);
            Directory.CreateDirectory("Logs");
            File.AppendAllText("Logs/bot_runs.txt", System.DateTime.Now.ToString("s") + " " + run.Report + "\n");
        }
    }
}
