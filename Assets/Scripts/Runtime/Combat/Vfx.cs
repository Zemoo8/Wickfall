using UnityEngine;
using Wickfall.Presentation;

namespace Wickfall.Combat
{
    public enum VfxId
    {
        None = 0,
        Slash, Bolt, BoltImpact, PoisonCloud, RadiantPulse, CrimsonStrike, WideSweep,
        Flare, EmberTrail, WaxDrip, ExtinguishBurst, SootSplash, HitSpark, BlockedSpark, DeathPuff,
        DustPuff, GeyserBurst, SteamBurst, GlassShards, AlertMark, RelightBurst, PickupGlint, LaserImpact,
        AshWave, VoidBurst,
    }

    /// <summary>
    /// Plays pooled effects by id. Prefabs come from the VfxLibrary asset (Resources/VfxLibrary),
    /// so swapping art never touches gameplay code. Disabled entirely when the active presentation
    /// phase hides VFX (Phase 4 shows placeholder polish).
    /// </summary>
    public static class Vfx
    {
        static VfxLibrary library;
        static bool searched;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            library = null;
            searched = false;
        }

        public static VfxLibrary Library
        {
            get
            {
                if (library == null && !searched)
                {
                    searched = true;
                    library = Resources.Load<VfxLibrary>(VfxLibrary.ResourcePath);
                }
                return library;
            }
        }

        public static void Use(VfxLibrary value)
        {
            library = value;
            searched = true;
        }

        public static GameObject Play(VfxId id, Vector2 position, Vector2 direction = default, Color tint = default, float scale = 1f)
        {
            if (id == VfxId.None || !PresentationMode.IsEnabled(Feature.VFX))
                return null;
            var lib = Library;
            if (lib == null)
                return null;
            var prefab = lib.Get(id);
            if (prefab == null)
                return null;
            var go = Pools.Spawn(prefab, position);
            if (direction.sqrMagnitude > 0.0001f)
                go.transform.right = direction;
            else
                go.transform.rotation = Quaternion.identity;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            if (tint.a > 0f && go.TryGetComponent(out VfxInstance instance))
                instance.SetTint(tint);
            return go;
        }
    }
}
