using System.Collections.Generic;
using UnityEngine;
using Wickfall.Enemies;
using Wickfall.Meta;

namespace Wickfall.World
{
    /// <summary>
    /// A creature asleep in the wax, woken by its region's Great Bell (G14 / PDF §2.5 "essaim").
    /// It rises where it slept and hunts toward the bell.
    /// </summary>
    public sealed class SwarmNest : MonoBehaviour
    {
        public string gateId = "gate";
        public string id = "nest";
        public GameObject creaturePrefab;

        static readonly List<SwarmNest> all = new List<SwarmNest>();
        public static IReadOnlyList<SwarmNest> All => all;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Start()
        {
            // Bell already rung earlier in this run: the swarm is already out (and maybe dead).
            if (GameSession.Instance != null && GameSession.Instance.HasFlag("bell/" + gateId))
                Wake();
        }

        bool woken;

        public void Wake()
        {
            // A flag, not enabled = false: the bell is iterating over All when it wakes the nests.
            if (creaturePrefab == null || woken)
                return;
            woken = true;
            var go = Instantiate(creaturePrefab, transform.position, Quaternion.identity, transform.parent);
            if (go.TryGetComponent(out EnemyBrain brain))
                brain.entityId = id;
            Combat.Vfx.Play(Combat.VfxId.DustPuff, transform.position);
        }
    }
}
