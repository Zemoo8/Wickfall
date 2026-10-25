using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wickfall.Meta;
using Wickfall.World;

namespace Wickfall.Tests
{
    /// <summary>A relic taken in one region still works in the next one (the save keeps it, Core stays loaded).</summary>
    public class RelicCarryTests : PlayModeFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.TallowFields);

        [UnityTest]
        public IEnumerator EmberFeather_FromRegion1_GivesTwoDodges_InRegion2()
        {
            Session.CollectRelic(0); // Ember Feather, the Region 1 relic
            Assert.AreEqual(2, Player.Dodger.MaxCharges, "in Region 1 the relic gives one more dodge");
            Session.SaveGame();

            // leave the way the game does: the region loader, not a fresh scene
            SceneLoader.Instance.Load(SceneNames.RegionBase(RegionId.Wickwood));
            float waited = 0f;
            while ((RegionDirector.Instance == null || RegionDirector.Instance.Level == null || RoomLoader.Instance == null
                    || !RoomLoader.Instance.Settled || !GameSession.Instance.player.isActiveAndEnabled) && waited < 30f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;

            Assert.IsNotNull(RegionDirector.Instance?.Level, "region 2 loaded");
            Assert.IsTrue(Session.Save.HasRelic(0), "the save still has the Ember Feather");
            Assert.AreEqual(2, GameSession.Instance.player.Dodger.MaxCharges, "in Region 2 the Ember Feather still gives one more dodge");
            Assert.AreEqual(2, GameSession.Instance.player.Dodger.Charges, "both dodge charges are ready on arrival");
        }
    }
}
