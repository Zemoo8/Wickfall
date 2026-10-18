using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wickfall.Combat;
using Wickfall.Enemies;
using Wickfall.Meta;
using Wickfall.World;

namespace Wickfall.Tests
{
    /// <summary>Region 4 - Cathédrale de Verre: beams, lasers, crimson glass, the Rose, Reflections, and a bot (M5).</summary>
    public class Region4Tests : PlayModeFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.GlassCathedral);

        static Vector2 Text(int x, int y) => new Vector2(x + 0.5f, 90 - 1 - y + 0.5f);

        static T Nearest<T>(Vector2 point) where T : Component =>
            Object.FindObjectsByType<T>(FindObjectsSortMode.None).OrderBy(c => Vector2.Distance(c.transform.position, point)).FirstOrDefault();

        static HitData PlayerHit(FlameId flame) => new HitData
        {
            Amount = 10f, Source = Team.Player, HasFlame = true, Flame = flame, HollowBonus = 1f,
        };

        [UnityTest]
        public IEnumerator Pedestal_TurnedNorth_WakesTheSocket_AndOpensGateA()
        {
            yield return WaitUntil(() => Object.FindObjectsByType<CrystalPedestal>(FindObjectsSortMode.None).Length > 0, 10f);
            var pedestal = Nearest<CrystalPedestal>(Text(20, 81));
            var gate = Gate.Find("cathedral/A");
            Assert.IsNotNull(gate, "gate A exists");
            yield return Wait(0.5f);
            Assert.IsFalse(gate.IsOpen, "closed while the beam runs east into the wall");
            pedestal.SetFacing(2);
            yield return WaitUntil(() => gate.IsOpen, 2f);
            Assert.IsTrue(Nearest<BeamSocket>(Text(20, 78)).Solved, "the socket woke");
            Assert.IsTrue(gate.IsOpen, "gate A opened");
        }

        [UnityTest]
        public IEnumerator Laser_ChargesThenBurnsFor25()
        {
            Player.Teleport(Text(30, 67));
            yield return WaitUntil(() => LaserEmitter.All.Count > 0, 10f);
            var laser = LaserEmitter.All.OrderBy(l => Mathf.Abs(l.transform.position.y - Text(0, 66).y)).First();
            yield return WaitUntil(() => laser.IsCharging, 6f);
            Player.Wax.SetCurrent(100f);
            Player.Teleport(Text(36, 66));
            yield return WaitUntil(() => laser.IsFiring, 2f);
            yield return Wait(0.3f);
            Assert.AreEqual(75f, Player.Wax.Current, 1.5f, "PDF: lasers deal 25");
        }

        [UnityTest]
        public IEnumerator Glass_OnlyCrimsonBreaksIt()
        {
            Player.Teleport(Text(80, 51));
            yield return WaitUntil(() => Object.FindObjectsByType<GlassBarrier>(FindObjectsSortMode.None).Length > 0, 10f);
            var glass = Nearest<GlassBarrier>(Text(80, 48));
            var body = glass.GetComponent<Damageable>();
            Assert.IsFalse(body.TakeHit(PlayerHit(FlameId.Amber)), "amber glances off");
            Assert.IsFalse(body.TakeHit(PlayerHit(FlameId.White)), "white too");
            Assert.IsTrue(body.TakeHit(PlayerHit(FlameId.Crimson)), "crimson breaks it");
            yield return null;
            Assert.IsTrue(glass.Broken);
            Assert.IsTrue(RegionDirector.Instance.Nav.IsWalkable(LevelData.CellOf(glass.transform.position)), "the path opens");
        }

        [UnityTest]
        public IEnumerator GlassFloor_HoldsOtherFlames_BreaksUnderCrimson()
        {
            Session.CollectRelic(3); // the extra slot: crimson joins the loadout (G53)
            Assert.IsTrue(Player.Flames.IsEquipped(FlameId.Crimson));
            Player.Teleport(Text(80, 40));
            yield return WaitUntil(() => Object.FindObjectsByType<GlassFloor>(FindObjectsSortMode.None).Length > 0, 10f);
            var floor = Nearest<GlassFloor>(Text(80, 38));
            Player.Teleport(floor.transform.position);
            yield return Wait(2f);
            Assert.IsFalse(floor.Broken, "amber does not crack the glass");
            Player.Teleport(Text(80, 40));
            yield return Wait(0.3f);
            Input.SelectSlot(Player.Flames.SlotOf(FlameId.Crimson));
            yield return WaitUntil(() => Player.Flames.Current == FlameId.Crimson, 2f);
            Player.Wax.SetCurrent(100f);
            Player.Teleport(floor.transform.position);
            yield return WaitUntil(() => floor.Broken, 3f);
            Assert.IsTrue(floor.Broken, "crimson breaks the floor");
            yield return null;
            Assert.Less(Player.Wax.Current, 90f, "the fall hurts (12)");
            Assert.Greater(Vector2.Distance(Player.transform.position, floor.transform.position), 0.6f, "back on solid ground");
        }

        [UnityTest]
        public IEnumerator Rose_AsleepWithoutItsBeam_ThenEachPanelTakesOnlyItsFlame()
        {
            Player.Teleport(Text(36, 30));
            yield return WaitUntil(() => Object.FindObjectsByType<RoseWindow>(FindObjectsSortMode.None).Length > 0, 10f);
            foreach (var e in EnemyBrain.All.ToList())
                e.gameObject.SetActive(false); // no Reflections in this rules test
            var rose = Nearest<RoseWindow>(Text(42, 32));
            var amber = rose.panels.First(p => p.flame == FlameId.Amber);
            Assert.IsFalse(amber.TakeHit(PlayerHit(FlameId.Amber)), "asleep: no beam on its eye yet");
            Nearest<CrystalPedestal>(Text(42, 24)).SetFacing(6);
            yield return WaitUntil(() => rose.Powered, 2f);
            Assert.IsTrue(rose.Powered, "the beam reaches the eye");
            Assert.IsFalse(amber.TakeHit(PlayerHit(FlameId.Blue)), "the wrong flame");
            Assert.IsTrue(amber.TakeHit(PlayerHit(FlameId.Amber)), "its own flame");
            foreach (var p in rose.panels)
                p.TakeHit(PlayerHit(p.flame));
            Assert.IsTrue(rose.Complete);
            yield return null;
            Assert.IsTrue(Gate.Find("cathedral/C").IsOpen, "gate C opens");
        }

        [UnityTest]
        public IEnumerator Reflections_RememberTheLast3Actions()
        {
            Player.Teleport(Text(36, 30));
            yield return WaitUntil(() => EnemyBrain.All.Any(e => e is ReflectionBrain), 10f);
            Input.PressAttack();
            yield return Wait(0.6f);
            Input.Move = Vector2.right;
            Input.PressDodge();
            yield return Wait(0.6f);
            Input.Move = Vector2.zero;
            Input.PressAttack();
            yield return Wait(0.6f);
            var memory = ReflectionBrain.Memory;
            Assert.AreEqual(3, memory.Count);
            Assert.AreEqual(PlayerActionKind.Attack, memory.Get(0).Kind);
            Assert.AreEqual(PlayerActionKind.Dodge, memory.Get(1).Kind);
            Assert.AreEqual(PlayerActionKind.Attack, memory.Get(2).Kind);
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator Region4_BotClearsTheRegion()
        {
            var run = new RegionBotRunner.Run();
            yield return RegionBotRunner.Play(Player, Session, "R4", run);
            Assert.IsFalse(run.Bot.Failed, run.Bot.FailReason);
            Assert.IsTrue(run.Completed, run.Report);
            Assert.IsTrue(Session.Save.HasRelic(3), "the flame-slot relic");
            Assert.IsTrue(Session.Save.HasFlame(FlameId.Crimson), "crimson");
        }
    }
}
