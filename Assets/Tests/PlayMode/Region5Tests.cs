using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Wickfall.Audio;
using Wickfall.Enemies;
using Wickfall.Meta;
using Wickfall.World;

namespace Wickfall.Tests
{
    /// <summary>Region 5 - Creux Profond: darkness, x1.5 wax loss, the Unlit, the heartbeat, and a bot (M5).</summary>
    public class Region5Tests : PlayModeFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.DeepHollow);

        static Vector2 Text(int x, int y) => new Vector2(x + 0.5f, 80 - 1 - y + 0.5f);

        [UnityTest]
        public IEnumerator NoAmbientLight_TheGroundUsesLightReveal()
        {
            yield return WaitUntil(() => RoomLoader.Instance != null && RoomLoader.Instance.Settled, 10f);
            yield return Wait(0.3f);
            var global = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None).FirstOrDefault(l => l.lightType == Light2D.LightType.Global);
            Assert.IsNotNull(global);
            Assert.AreEqual(0f, global.intensity, 0.0001f, "PDF: aucune lumière ambiante");
            var ground = Object.FindObjectsByType<UnityEngine.Tilemaps.TilemapRenderer>(FindObjectsSortMode.None).First(r => r.name == "Ground");
            Assert.AreEqual("Wickfall/LightReveal", ground.sharedMaterial.shader.name, "visible only where you shine");
        }

        [UnityTest]
        public IEnumerator WaxLoss_IsOnePointFiveTimes()
        {
            yield return Wait(0.3f);
            Player.Wax.SetCurrent(80f);
            float start = Player.Wax.Current;
            yield return Wait(2f);
            Assert.AreEqual(1.5f, start - Player.Wax.Current, 0.15f, "0.5/s x 1.5 for 2 s");
        }

        [UnityTest]
        public IEnumerator Heartbeat_PlaysInTheCreuxProfond()
        {
            yield return Wait(0.3f);
            var audio = Object.FindFirstObjectByType<AudioReactive>();
            Assert.IsNotNull(audio);
            Assert.IsTrue(audio.HeartbeatActive, "PDF/prompt: a heartbeat in the Creux Profond");
            Assert.Greater(AudioReactive.BpmFor(0.1f, Db.AudioReactive), AudioReactive.BpmFor(1f, Db.AudioReactive), "faster at low wax");
        }

        [UnityTest]
        public IEnumerator Unlit_DriftsTowardTheLight_AndAFlareLuresIt()
        {
            Player.Teleport(Text(46, 62));
            yield return WaitUntil(() => EnemyBrain.All.Any(e => e is UnlitBrain && Vector2.Distance(e.transform.position, Player.transform.position) < 15f), 10f);
            var unlit = EnemyBrain.All.OfType<UnlitBrain>().OrderBy(u => Vector2.Distance(u.transform.position, Player.transform.position)).First();
            Player.Wax.SetCurrent(100f);
            float before = Vector2.Distance(unlit.transform.position, Player.transform.position);
            yield return Wait(2f);
            Assert.Less(Vector2.Distance(unlit.transform.position, Player.transform.position), before - 0.8f, "drawn to the player's flame");

            Vector2 lure = (Vector2)unlit.transform.position + new Vector2(0f, -4f);
            Events.Flare.Raise(new FlareInfo(lure, 6f, FlareKind.FlameSwitch, Color.white));
            yield return null;
            Assert.IsTrue(unlit.IsLured);
            Assert.Less(Vector2.Distance(unlit.Target, lure), 0.01f, "a flare draws it away");
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator Region5_BotClearsTheRegion()
        {
            var run = new RegionBotRunner.Run();
            yield return RegionBotRunner.Play(Player, Session, "R5", run);
            Assert.IsFalse(run.Bot.Failed, run.Bot.FailReason);
            Assert.IsTrue(run.Completed, run.Report);
            Assert.IsTrue(Session.Save.HasRelic(4), "the Spare Wick");
        }
    }
}
