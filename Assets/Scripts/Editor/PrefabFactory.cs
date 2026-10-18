using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Wickfall.Combat;
using Wickfall.Player;
using Wickfall.UI;
using Wickfall.World;

namespace Wickfall.EditorTools
{
    /// <summary>Builds the prefabs from the generated art and the data assets (re-runnable).</summary>
    public static class PrefabFactory
    {
        public const string Folder = "Assets/Prefabs";
        public const string PlayerPath = Folder + "/Player.prefab";
        public const string BoltPath = Folder + "/Combat/Bolt.prefab";
        public const string ArtBoltPath = Folder + "/Combat/ArtBolt.prefab";
        public const string PickupPath = Folder + "/World/WaxPickup.prefab";
        public const string DropPath = Folder + "/World/WaxDrop.prefab";
        public const string PopupPath = Folder + "/UI/DamagePopup.prefab";
        public const string BrazierPath = Folder + "/World/Brazier.prefab";
        public const string PoolPath = Folder + "/World/WaxPool.prefab";
        public const string GeyserPath = Folder + "/World/WaxGeyser.prefab";
        public const string EffigyPath = Folder + "/World/Effigy.prefab";
        public const string EffigyArmoredPath = Folder + "/World/Effigy_Armored.prefab";
        public const string EffigyGlassPath = Folder + "/World/Effigy_Glass.prefab";

        static Sprite First(string folder, string sheet, string clip) =>
            SpriteSheets.Clip(ArtFactory.Sheet(folder, sheet), clip).FirstOrDefault();

        public static void EnsureAll()
        {
            var bolt = EnsureBolt(BoltPath, false);
            var artBolt = EnsureBolt(ArtBoltPath, true);
            EnsurePickup();
            EnsureDrop();
            EnsurePopup();
            EnsurePlayer(bolt, artBolt);
            EnsureBrazier();
            EnsurePool();
            EnsureGeyser();
            EnsureEffigy(EffigyPath, DamageTags.None, Color.white, "lab.effigy");
            EnsureEffigy(EffigyArmoredPath, DamageTags.Armored, new Color(0.62f, 0.5f, 0.38f), "lab.effigy.armored");
            EnsureEffigy(EffigyGlassPath, DamageTags.Glass, new Color(0.85f, 0.9f, 0.95f, 0.8f), "lab.effigy.glass");
            AssetDatabase.SaveAssets();
        }

        // ---- player -------------------------------------------------------------------------------

        public static GameObject EnsurePlayer(Projectile bolt, Projectile artBolt)
        {
            var sprites = AssetDatabase.LoadAssetAtPath<PlayerSpriteSet>(ArtFactory.PlayerSpritesPath);
            var root = new GameObject("Player");
            EditorAssets.SetLayer(root, Layers.Player);
            root.tag = "Player";
            var group = root.AddComponent<SortingGroup>();
            group.sortingLayerName = Layers.SortEntities;

            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var feet = root.AddComponent<CircleCollider2D>();
            feet.radius = 0.3f;
            feet.offset = new Vector2(0f, 0.22f);

            root.AddComponent<WaxSystem>();
            root.AddComponent<PlayerStatus>();
            root.AddComponent<DimController>();
            root.AddComponent<FlameSwitcher>();
            var light = root.AddComponent<LightController>();
            var combat = root.AddComponent<PlayerCombat>();
            combat.boltPrefab = bolt;
            combat.artBoltPrefab = artBolt;
            root.AddComponent<PlayerDodge>();
            root.AddComponent<PlayerInteractor>();
            root.AddComponent<PlayerDamageable>();
            root.AddComponent<PlayerController>();

            var hurt = EditorAssets.Child(root, "Hurtbox", new Vector3(0f, 0.65f, 0f));
            EditorAssets.SetLayer(hurt, Layers.Player);
            var hurtCollider = hurt.AddComponent<CircleCollider2D>();
            hurtCollider.isTrigger = true;
            hurtCollider.radius = 0.45f;
            hurt.AddComponent<Hurtbox>();

            var shadow = EditorAssets.Child(root, "Shadow", new Vector3(0f, 0.06f, 0f));
            var shadowSr = EditorAssets.Sprite(shadow, First("Props", "Prop_Shadow", "shadow"), EditorAssets.LitDefault, Layers.SortEntities, -2);
            shadowSr.color = new Color(1f, 1f, 1f, 0.45f);

            var visual = EditorAssets.Child(root, "Visual");
            var body = EditorAssets.Child(visual, "Body");
            var bodySr = EditorAssets.Sprite(body, sprites != null && sprites.idle[0].IsValid ? sprites.idle[0].frames[0] : null,
                EditorAssets.Lit, Layers.SortEntities, 0);
            var bodyAnim = body.AddComponent<SpriteAnimator>();
            bodyAnim.target = bodySr;

            var flame = EditorAssets.Child(visual, "Flame", new Vector3(0f, 31f / 16f, 0f));
            var flameSr = EditorAssets.Sprite(flame, sprites != null && sprites.flame.IsValid ? sprites.flame.frames[0] : null,
                EditorAssets.Flame, Layers.SortEntities, 2);
            var flameAnim = flame.AddComponent<SpriteAnimator>();
            flameAnim.target = flameSr;

            var visuals = root.AddComponent<PlayerVisuals>();
            visuals.sprites = sprites;
            visuals.body = bodySr;
            visuals.bodyAnimator = bodyAnim;
            visuals.flame = flameSr;
            visuals.flameAnimator = flameAnim;
            visuals.shadow = shadowSr;
            visuals.squashRoot = visual.transform;

            // The light the PDF is about: radius 1.2 to 6 units from wax (LightController drives it).
            var lightGo = EditorAssets.Child(root, "Light", new Vector3(0f, 1.25f, 0f));
            light.playerLight = EditorAssets.PointLight(lightGo, 6f, 1.15f, new Color(1f, 0.78f, 0.45f), true);
            var haloGo = EditorAssets.Child(root, "Halo", new Vector3(0f, 2.05f, 0f));
            light.haloLight = EditorAssets.PointLight(haloGo, 1.4f, 0.35f, new Color(1f, 0.78f, 0.45f), false, 1);

            return EditorAssets.SavePrefab(root, PlayerPath);
        }

        // ---- combat --------------------------------------------------------------------------------

        static Projectile EnsureBolt(string path, bool art)
        {
            var frames = SpriteSheets.Clip(ArtFactory.Sheet("VFX", "VFX_Bolt"), "fx");
            var root = new GameObject(art ? "ArtBolt" : "Bolt");
            var sr = EditorAssets.Sprite(root, frames.FirstOrDefault(), ArtFactory.GlowMaterial(2.4f), Layers.SortVfx);
            var anim = root.AddComponent<SpriteAnimator>();
            anim.target = sr;
            var projectile = root.AddComponent<Projectile>();
            projectile.sprite = sr;
            projectile.radius = art ? 0.3f : 0.18f;
            projectile.pierce = art;
            var impact = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/VFX_BoltImpact.prefab");
            projectile.impactVfx = impact;
            var glowGo = EditorAssets.Child(root, "Glow");
            projectile.glow = EditorAssets.PointLight(glowGo, art ? 2.4f : 1.6f, 0.9f, new Color(0.56f, 0.86f, 1f), false);
            if (art)
                root.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            root.AddComponent<PooledObject>();
            var prefab = EditorAssets.SavePrefab(root, path);
            // the animator plays its frames from the prefab's frames at spawn
            var p = prefab.GetComponent<Projectile>();
            var a = prefab.GetComponent<SpriteAnimator>();
            return p;
        }

        static void EnsurePickup()
        {
            var frames = SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_Pickups"), "pickup");
            var root = new GameObject("WaxPickup");
            var visual = EditorAssets.Child(root, "Visual");
            var sr = EditorAssets.Sprite(visual, frames.FirstOrDefault(), ArtFactory.GlowMaterial(1.8f), Layers.SortVfx);
            var pickup = root.AddComponent<WaxPickup>();
            pickup.sprite = sr;
            root.AddComponent<PooledObject>();
            EditorAssets.SavePrefab(root, PickupPath);
        }

        static void EnsureDrop()
        {
            var root = new GameObject("WaxDrop");
            var sr = EditorAssets.Sprite(root, First("Props", "Prop_WaxDrop", "drop"), ArtFactory.GlowMaterial(0.9f), Layers.SortGroundDecal, 2);
            sr.color = new Color(1f, 1f, 1f, 0.85f);
            EditorAssets.SavePrefab(root, DropPath);
        }

        static void EnsurePopup()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ArtFactory.FontAssetPath);
            var root = new GameObject("DamagePopup");
            var text = root.AddComponent<TextMeshPro>();
            text.font = font;
            text.fontSize = 6f;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.text = "0";
            text.rectTransform.sizeDelta = new Vector2(4f, 1f);
            text.GetComponent<MeshRenderer>().sortingLayerName = Layers.SortForeground;
            EditorAssets.SavePrefab(root, PopupPath);
        }

        // ---- world ---------------------------------------------------------------------------------

        static void EnsureBrazier()
        {
            var bowl = SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_Brazier"), "bowl");
            var fire = SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_Brazier"), "fire");
            var root = new GameObject("Brazier");
            EditorAssets.SetLayer(root, Layers.Interactable);
            var group = root.AddComponent<SortingGroup>();
            group.sortingLayerName = Layers.SortEntities;
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.1f;
            trigger.offset = new Vector2(0f, 0.4f);

            var solid = EditorAssets.Child(root, "Solid");
            EditorAssets.SetLayer(solid, Layers.Wall);
            var box = solid.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.3f, 0.5f);
            box.offset = new Vector2(0f, 0.35f);

            var bowlGo = EditorAssets.Child(root, "Bowl");
            var bowlSr = EditorAssets.Sprite(bowlGo, bowl.ElementAtOrDefault(1), EditorAssets.Lit, Layers.SortEntities, 0);
            var fireGo = EditorAssets.Child(root, "Fire", new Vector3(0f, 0.75f, 0f));
            var fireSr = EditorAssets.Sprite(fireGo, fire.FirstOrDefault(), ArtFactory.GlowMaterial(2.2f), Layers.SortEntities, 1);
            var fireAnim = fireGo.AddComponent<SpriteAnimator>();
            fireAnim.target = fireSr;
            var lightGo = EditorAssets.Child(root, "Light", new Vector3(0f, 1.1f, 0f));
            var light = EditorAssets.PointLight(lightGo, 5.5f, 1.1f, new Color(1f, 0.7f, 0.38f), true);
            var spawn = EditorAssets.Child(root, "Respawn", new Vector3(0f, -1.3f, 0f));

            var brazier = root.AddComponent<Brazier>();
            brazier.sprite = bowlSr;
            brazier.fire = fireAnim;
            brazier.fireLight = light;
            brazier.unlitSprite = bowl.FirstOrDefault();
            brazier.litFrames = fire;
            brazier.respawnPoint = spawn.transform;
            brazier.promptKey = "prompt.rest";
            brazier.promptOffset = new Vector2(0f, 2.4f);
            EditorAssets.SavePrefab(root, BrazierPath);
        }

        static void EnsurePool()
        {
            var frames = SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_WaxPool"), "pool");
            var root = new GameObject("WaxPool");
            EditorAssets.SetLayer(root, Layers.Interactable);
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.2f;
            var sr = EditorAssets.Sprite(root, frames.FirstOrDefault(), ArtFactory.GlowMaterial(1.1f), Layers.SortGroundDecal, 3);
            var glowGo = EditorAssets.Child(root, "Glow", new Vector3(0f, 0.3f, 0f));
            var glow = EditorAssets.PointLight(glowGo, 2.4f, 0.7f, new Color(1f, 0.93f, 0.78f), false);
            var pool = root.AddComponent<WaxPool>();
            pool.sprite = sr;
            pool.fullSprite = frames.ElementAtOrDefault(0);
            pool.emptySprite = frames.ElementAtOrDefault(1);
            pool.glow = glow;
            pool.promptOffset = new Vector2(0f, 1.6f);
            EditorAssets.SavePrefab(root, PoolPath);
        }

        static void EnsureGeyser()
        {
            var tiles = AssetDatabase.LoadAssetAtPath<RegionTiles>("Assets/Data/Tiles/tallow/RegionTiles_tallow.asset");
            var root = new GameObject("WaxGeyser");
            var sr = EditorAssets.Sprite(root, null, EditorAssets.Lit, Layers.SortGroundDecal, 4);
            var lightGo = EditorAssets.Child(root, "Warning", new Vector3(0f, 0.2f, 0f));
            var warn = EditorAssets.PointLight(lightGo, 1.7f, 0.2f, new Color(1f, 0.6f, 0.25f), false);
            var hitGo = EditorAssets.Child(root, "Hitbox");
            var hitbox = hitGo.AddComponent<Hitbox>();
            var geyser = root.AddComponent<WaxGeyser>();
            geyser.sprite = sr;
            geyser.warningLight = warn;
            geyser.hitbox = hitbox;
            geyser.hazard = AssetDatabase.LoadAssetAtPath<HazardSO>(EditorPaths.Hazards + "/Hazard_WaxGeyser.asset");
            if (tiles != null)
            {
                geyser.dormant = tiles.Special("geyser_dormant").frames?.FirstOrDefault();
                geyser.warning = tiles.Special("geyser_warning").frames;
                geyser.eruption = tiles.Special("geyser_erupt").frames;
                sr.sprite = geyser.dormant;
            }
            EditorAssets.SavePrefab(root, GeyserPath);
        }

        static void EnsureEffigy(string path, DamageTags tags, Color tint, string labelKey)
        {
            var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
            EditorAssets.SetLayer(root, Layers.Enemy);
            var group = root.AddComponent<SortingGroup>();
            group.sortingLayerName = Layers.SortEntities;
            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;
            collider.offset = new Vector2(0f, 0.35f);
            root.AddComponent<Hurtbox>();
            var visual = EditorAssets.Child(root, "Visual");
            var sr = EditorAssets.Sprite(visual, First("Props", "Prop_Effigy", "effigy"), EditorAssets.Lit, Layers.SortEntities);
            sr.color = tint;
            var body = root.AddComponent<Damageable>();
            body.team = Team.Enemy;
            body.maxHealth = 60f;
            body.tags = tags;
            body.knockbackResistance = 1f;
            var dummy = root.AddComponent<TrainingDummy>();
            dummy.sprite = sr;

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ArtFactory.FontAssetPath);
            var labelGo = EditorAssets.Child(root, "Label", new Vector3(0f, 2.4f, 0f));
            var label = labelGo.AddComponent<TextMeshPro>();
            label.font = font;
            label.fontSize = 5f;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.color = new Color(0.81f, 0.77f, 0.7f);
            label.rectTransform.sizeDelta = new Vector2(8f, 1f);
            label.GetComponent<MeshRenderer>().sortingLayerName = Layers.SortForeground;
            labelGo.AddComponent<LocalizedText>().key = labelKey;
            EditorAssets.SavePrefab(root, path);
        }
    }
}
