using UnityEngine;

namespace Wickfall
{
    /// <summary>Physics layers and sorting layers. WICKFALL > Setup creates them in this order.</summary>
    public static class Layers
    {
        // Physics layers (user layers start at 6).
        public const string Wall = "Wall";               // blocks movement, light and sight
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Hazard = "Hazard";
        public const string Pickup = "Pickup";
        public const string Interactable = "Interactable";
        public const string Projectile = "Projectile";
        public const string Occluder = "Occluder";       // canopy: blocks sight and light, not movement
        public const string Breakable = "Breakable";     // glass walls (crimson only)
        public const string Zone = "Zone";               // triggers: dark zones, ash, exits

        public static readonly string[] PhysicsOrder =
        {
            Wall, Player, Enemy, Hazard, Pickup, Interactable, Projectile, Occluder, Breakable, Zone,
        };

        public const int FirstUserLayer = 6;

        // Sorting layers, back to front. Entities are sorted by Y (custom transparency axis).
        public const string SortGround = "Ground";
        public const string SortGroundDecal = "GroundDecal";
        public const string SortEntities = "Entities";
        public const string SortOverhead = "Overhead";
        public const string SortVfx = "VFX";
        public const string SortForeground = "Foreground";

        public static readonly string[] SortingOrder =
        {
            SortGround, SortGroundDecal, "Default", SortEntities, SortOverhead, SortVfx, SortForeground,
        };

        static int wallMask = -1, sightMask = -1, enemyMask = -1, playerMask = -1, hurtableMask = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            wallMask = sightMask = enemyMask = playerMask = hurtableMask = -1;
        }

        /// <summary>What blocks movement.</summary>
        public static int WallMask => wallMask >= 0 ? wallMask : (wallMask = LayerMask.GetMask(Wall, Breakable));

        /// <summary>What blocks sight (walls and the canopy: "poches d'obscurité où se cacher").</summary>
        public static int SightMask => sightMask >= 0 ? sightMask : (sightMask = LayerMask.GetMask(Wall, Occluder, Breakable));

        public static int EnemyMask => enemyMask >= 0 ? enemyMask : (enemyMask = LayerMask.GetMask(Enemy));
        public static int PlayerMask => playerMask >= 0 ? playerMask : (playerMask = LayerMask.GetMask(Player));

        /// <summary>Things a player attack can hit.</summary>
        public static int HurtableMask => hurtableMask >= 0 ? hurtableMask : (hurtableMask = LayerMask.GetMask(Enemy, Breakable, Interactable));
    }
}
