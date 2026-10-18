using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wickfall.World;

namespace Wickfall.Tests
{
    /// <summary>
    /// Guard for a Unity 6.2 behaviour found while building M1: with Physics2D.queriesHitTriggers off,
    /// trigger colliders are invisible even to queries that ask for them, which silently broke
    /// interaction, hazards and hurtboxes. The project setting must stay on.
    /// </summary>
    public class PhysicsDiagnostics : PlayModeFixture
    {
        [UnityTest]
        public IEnumerator TriggerQueries_SeeInteractables()
        {
            Assert.IsTrue(Physics2D.queriesHitTriggers, "Physics2D 'Queries Hit Triggers' must stay on (see WickfallSetup.SetupPhysics)");
            var brazier = Object.FindFirstObjectByType<Brazier>();
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = LayerMask.GetMask(Layers.Interactable), useTriggers = true };
            var results = new Collider2D[8];
            yield return null;
            Assert.Greater(Physics2D.OverlapCircle(brazier.RespawnPosition, 1.1f, filter, results), 0);
        }
    }
}
