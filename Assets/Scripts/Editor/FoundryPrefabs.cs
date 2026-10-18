using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Wickfall.Combat;
using Wickfall.Enemies;
using Wickfall.World;

namespace Wickfall.EditorTools
{
    /// <summary>Region 3 (Fonderies de Suie) prefabs: Slagborn, Bellows and their soot, presses, steam vents, furnaces.</summary>
    public static class FoundryPrefabs
    {
        public const string SootPath = "Assets/Prefabs/Combat/SootBall.prefab";

        public static void EnsureAll()
        {
            WorldPrefabs.EnsureEnemy<SlagbornBrain>("Slagborn", EnemyKind.Slagborn, "Enemy_Slagborn");
            var soot = EnsureSoot();
            var bellows = WorldPrefabs.EnsureEnemy<BellowsBrain>("Bellows", EnemyKind.Bellows, "Enemy_Bellows");
            bellows.GetComponent<BellowsBrain>().sootPrefab = soot;
            PrefabUtility.SavePrefabAsset(bellows);
            EnsurePress();
            EnsureSteamJet();
            EnsureFurnace();
        }

        static Projectile EnsureSoot()
        {
            var frames = SpriteSheets.Clip(ArtFactory.Sheet("VFX", "VFX_SootBall"), "fx");
            var root = new GameObject("SootBall");
            var sr = EditorAssets.Sprite(root, frames.FirstOrDefault(), EditorAssets.Lit, Layers.SortVfx);
            var projectile = root.AddComponent<Projectile>();
            projectile.sprite = sr;
            projectile.radius = 0.2f;
            projectile.spin = true;
            projectile.impactVfx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/VFX_SootSplash.prefab");
            root.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            root.AddComponent<PooledObject>();
            return EditorAssets.SavePrefab(root, SootPath).GetComponent<Projectile>();
        }

        static void EnsurePress()
        {
            string headSheet = ArtFactory.Sheet("Props", "Prop_Press");
            var heads = SpriteSheets.Clip(headSheet, "head");
            var root = new GameObject("Press");
            EditorAssets.Sprite(EditorAssets.Child(root, "Plate"),
                SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_PressPlate"), "plate").FirstOrDefault(), EditorAssets.Lit, Layers.SortGroundDecal, 3);
            var shadow = EditorAssets.Sprite(EditorAssets.Child(root, "Shadow", new Vector3(0f, -0.2f, 0f)),
                SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_ShadowLarge"), "shadow").FirstOrDefault(), EditorAssets.LitDefault, Layers.SortGroundDecal, 5);
            shadow.color = new Color(1f, 1f, 1f, 0.25f);
            var headGo = EditorAssets.Child(root, "Head", new Vector3(0f, 1.2f, 0f));
            // The raised head hangs above everything beneath it.
            var head = EditorAssets.Sprite(headGo, heads.FirstOrDefault(), EditorAssets.Lit, Layers.SortOverhead);
            var warn = EditorAssets.PointLight(EditorAssets.Child(root, "Warning", new Vector3(0f, 0.2f, 0f)), 1.8f, 0.1f, new Color(1f, 0.45f, 0.2f), false);
            var hitbox = EditorAssets.Child(root, "Hitbox").AddComponent<Hitbox>();
            var press = root.AddComponent<Press>();
            press.hazard = AssetDatabase.LoadAssetAtPath<HazardSO>(EditorPaths.Hazards + "/Hazard_Press.asset");
            press.head = headGo.transform;
            press.headSprite = head;
            press.headCold = heads.ElementAtOrDefault(0);
            press.headHot = heads.ElementAtOrDefault(1);
            press.shadow = shadow;
            press.warningLight = warn;
            press.hitbox = hitbox;
            EditorAssets.SavePrefab(root, WorldPrefabs.WorldPath("Press"));
        }

        static void EnsureSteamJet()
        {
            string sheet = ArtFactory.Sheet("Props", "Prop_SteamVent");
            var root = new GameObject("SteamJet");
            var sr = EditorAssets.Sprite(root, SpriteSheets.Clip(sheet, "idle").FirstOrDefault(), EditorAssets.Lit, Layers.SortGroundDecal, 4);
            var jet = root.AddComponent<SteamJet>();
            jet.hazard = AssetDatabase.LoadAssetAtPath<HazardSO>(EditorPaths.Hazards + "/Hazard_SteamJet.asset");
            jet.sprite = sr;
            jet.idle = sr.sprite;
            jet.warning = SpriteSheets.Clip(sheet, "warn");
            jet.venting = SpriteSheets.Clip(sheet, "vent");
            EditorAssets.SavePrefab(root, WorldPrefabs.WorldPath("SteamJet"));
        }

        static void EnsureFurnace()
        {
            var frames = SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_Furnace"), "burn");
            var root = new GameObject("Furnace");
            root.AddComponent<SortingGroup>().sortingLayerName = Layers.SortEntities;
            var solid = EditorAssets.Child(root, "Solid", new Vector3(0f, 0.45f, 0f));
            EditorAssets.SetLayer(solid, Layers.Wall);
            solid.AddComponent<BoxCollider2D>().size = new Vector2(1.6f, 0.9f);
            var sr = EditorAssets.Sprite(EditorAssets.Child(root, "Visual"), frames.FirstOrDefault(), EditorAssets.Lit, Layers.SortEntities);
            // A warm, shadow-casting light: as it gutters and sways, the walls' shadows move.
            var glow = EditorAssets.PointLight(EditorAssets.Child(root, "Glow", new Vector3(0f, 0.7f, 0f)), 5.5f, 1.1f, new Color(1f, 0.62f, 0.3f), true);
            var furnace = root.AddComponent<Furnace>();
            furnace.glow = glow;
            furnace.sprite = sr;
            furnace.frames = frames;
            EditorAssets.SavePrefab(root, WorldPrefabs.WorldPath("Furnace"));
        }
    }
}
