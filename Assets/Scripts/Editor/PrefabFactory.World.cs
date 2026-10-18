using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Wickfall.Combat;
using Wickfall.Enemies;
using Wickfall.World;

namespace Wickfall.EditorTools
{
    /// <summary>Region prefabs: creatures and world objects (built from the generated sheets).</summary>
    public static class WorldPrefabs
    {
        public const string Enemies = "Assets/Prefabs/Enemies";
        public const string World = "Assets/Prefabs/World";
        public const string EnemySprites = "Assets/Data/Visuals/Enemies";

        public static string EnemyPath(string kind) => $"{Enemies}/{kind}.prefab";
        public static string WorldPath(string kind) => $"{World}/{kind}.prefab";

        public static void EnsureAll()
        {
            EnsureEnemy<DriplingBrain>("Dripling", EnemyKind.Dripling, "Enemy_Dripling");
            EnsureEnemy<HuskBrain>("Husk", EnemyKind.Husk, "Enemy_Husk");
            EnsureEnemy<LurkerBrain>("Lurker", EnemyKind.Lurker, "Enemy_Lurker");
            EnsureBloom();
            EnsureVine();
            EnsureNest("SwarmNestHusk", "Husk");
            EnsureNest("SwarmNestLurker", "Lurker");
            FoundryPrefabs.EnsureAll();
            EnsureNest("SwarmNestSlagborn", "Slagborn");
            EnsureNest("SwarmNestBellows", "Bellows");
            EnsureCirelle();
            EnsureBrokenWick();
            EnsureBell();
            EnsureGate();
            EnsureRelic();
            EnsureShrine();
            EnsureInscription();
            EnsureNest("SwarmNest", "Dripling");
            EnsureExit();
            AssetDatabase.SaveAssets();
        }

        // ---- creatures ----------------------------------------------------------------------------

        public static EnemySpriteSet EnsureEnemySprites(string sheetName)
        {
            EditorPaths.EnsureFolder(EnemySprites);
            string path = $"{EnemySprites}/{sheetName}.asset";
            var set = AssetDatabase.LoadAssetAtPath<EnemySpriteSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<EnemySpriteSet>();
                AssetDatabase.CreateAsset(set, path);
            }
            string sheet = ArtFactory.Sheet("Enemies", sheetName);
            var meta = SpriteSheets.ReadMeta(sheet);
            SpriteClip C(string clip, bool loop) =>
                new SpriteClip(SpriteSheets.Clip(sheet, clip), SpriteSheets.Fps(meta, clip, 8f), loop);
            set.idle = C("idle", true);
            set.move = C("move", true);
            set.windup = C("windup", true);
            set.attack = C("attack", false);
            set.hurt = C("hurt", false);
            set.death = C("death", false);
            set.special = C("special", true);
            set.special2 = C("special2", true);
            set.alertHeight = meta != null && meta.cell != null ? meta.cell[1] / 16f + 0.4f : 1.6f;
            EditorUtility.SetDirty(set);
            return set;
        }

        public static GameObject EnsureEnemy<TBrain>(string kind, EnemyKind enemyKind, string sheetName, float lightRadius = 0f)
            where TBrain : EnemyBrain
        {
            var sprites = EnsureEnemySprites(sheetName);
            var type = AssetDatabase.LoadAssetAtPath<EnemyTypeSO>($"{EditorPaths.Enemies}/Enemy_{enemyKind}.asset");
            float r = type != null ? type.colliderRadius : 0.45f;

            var root = new GameObject(kind);
            EditorAssets.SetLayer(root, Layers.Enemy);
            root.AddComponent<SortingGroup>().sortingLayerName = Layers.SortEntities;
            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.mass = type != null ? 1f + type.knockbackResistance * 6f : 1f;
            var feet = root.AddComponent<CircleCollider2D>();
            feet.radius = r * 0.75f;
            feet.offset = new Vector2(0f, r * 0.6f);

            var damageable = root.AddComponent<Damageable>();
            damageable.team = Team.Enemy;
            root.AddComponent<EnemyPerception>();
            root.AddComponent<EnemyMotor>();
            var brain = root.AddComponent<TBrain>();
            brain.type = type;
            brain.pickupPrefab = AssetDatabase.LoadAssetAtPath<WaxPickup>(PrefabFactory.PickupPath);

            var hurt = EditorAssets.Child(root, "Hurtbox", new Vector3(0f, r + 0.1f, 0f));
            EditorAssets.SetLayer(hurt, Layers.Enemy);
            var hurtCol = hurt.AddComponent<CircleCollider2D>();
            hurtCol.isTrigger = true;
            hurtCol.radius = r * 1.1f;
            hurt.AddComponent<Hurtbox>();

            var shadow = EditorAssets.Child(root, "Shadow", new Vector3(0f, 0.06f, 0f));
            var shadowSprite = SpriteSheets.Clip(ArtFactory.Sheet("Props", r > 0.6f ? "Prop_ShadowLarge" : "Prop_Shadow"), "shadow").FirstOrDefault();
            EditorAssets.Sprite(shadow, shadowSprite, EditorAssets.LitDefault, Layers.SortEntities, -2).color = new Color(1f, 1f, 1f, 0.4f);

            var visual = EditorAssets.Child(root, "Visual");
            var sr = EditorAssets.Sprite(visual, sprites.idle.IsValid ? sprites.idle.frames[0] : null, EditorAssets.Lit, Layers.SortEntities);
            var anim = visual.AddComponent<SpriteAnimator>();
            anim.target = sr;
            var visuals = root.AddComponent<EnemyVisuals>();
            visuals.sprites = sprites;
            visuals.body = sr;
            visuals.animator = anim;

            var attack = EditorAssets.Child(root, "Attack");
            brain.attackHitbox = attack.AddComponent<Hitbox>();

            if (lightRadius > 0f)
                EditorAssets.PointLight(EditorAssets.Child(root, "Glow", new Vector3(0f, 0.6f, 0f)), lightRadius, 0.5f, Color.white, false);

            return EditorAssets.SavePrefab(root, EnemyPath(kind));
        }

        static void EnsureBloom()
        {
            string sheet = ArtFactory.Sheet("Enemies", "Enemy_Bloom");
            var pulse = SpriteSheets.Clip(sheet, "pulse");
            var root = new GameObject("LightEaterBloom");
            EditorAssets.SetLayer(root, Layers.Enemy);
            root.AddComponent<SortingGroup>().sortingLayerName = Layers.SortEntities;
            var body = root.AddComponent<Damageable>();
            body.team = Team.Enemy;
            body.kind = EnemyKind.LightEaterBloom;
            body.knockbackResistance = 1f;
            var solid = root.AddComponent<CircleCollider2D>();
            solid.radius = 0.35f;
            solid.offset = new Vector2(0f, 0.3f);
            var hurt = EditorAssets.Child(root, "Hurtbox", new Vector3(0f, 0.8f, 0f));
            EditorAssets.SetLayer(hurt, Layers.Enemy);
            var hc = hurt.AddComponent<CircleCollider2D>();
            hc.isTrigger = true;
            hc.radius = 0.6f;
            hurt.AddComponent<Hurtbox>();
            var visual = EditorAssets.Child(root, "Visual");
            var sr = EditorAssets.Sprite(visual, pulse.FirstOrDefault(), EditorAssets.Lit, Layers.SortEntities);
            var anim = visual.AddComponent<SpriteAnimator>();
            anim.target = sr;
            var bloom = root.AddComponent<LightEaterBloom>();
            bloom.sprite = sr;
            bloom.animator = anim;
            bloom.pulse = pulse;
            bloom.feed = SpriteSheets.Clip(sheet, "feed");
            bloom.type = AssetDatabase.LoadAssetAtPath<EnemyTypeSO>($"{EditorPaths.Enemies}/Enemy_LightEaterBloom.asset");
            bloom.hazard = AssetDatabase.LoadAssetAtPath<HazardSO>($"{EditorPaths.Hazards}/Hazard_LightEaterBloom.asset");
            bloom.pickupPrefab = AssetDatabase.LoadAssetAtPath<WaxPickup>(PrefabFactory.PickupPath);
            // the pale core gives off a little cold light: a lure in the dark forest
            bloom.core = EditorAssets.PointLight(EditorAssets.Child(root, "Core", new Vector3(0f, 1f, 0f)), 1.6f, 0.4f, new Color(0.93f, 0.95f, 0.88f), false);
            EditorAssets.SavePrefab(root, EnemyPath("LightEaterBloom"));
        }

        static void EnsureVine()
        {
            var root = new GameObject("Vine");
            EditorAssets.SetLayer(root, Layers.Interactable);
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.4f, 5f);
            root.AddComponent<Vine>().promptOffset = new Vector2(0f, 1.4f);
            EditorAssets.SavePrefab(root, WorldPath("Vine"));
        }

        static void EnsureCirelle()
        {
            string sheet = ArtFactory.Sheet("Enemies", "Enemy_Waxfly");
            var idle = SpriteSheets.Clip(sheet, "idle");
            var fly = SpriteSheets.Clip(sheet, "move");
            var root = new GameObject("Cirelle");
            var sr = EditorAssets.Sprite(root, idle.FirstOrDefault(), ArtFactory.GlowMaterial(1.6f), Layers.SortEntities, 5);
            var anim = root.AddComponent<SpriteAnimator>();
            anim.target = sr;
            var cirelle = root.AddComponent<Cirelle>();
            cirelle.sprite = sr;
            cirelle.animator = anim;
            cirelle.idleFrames = idle;
            cirelle.flyFrames = fly;
            EditorAssets.PointLight(EditorAssets.Child(root, "Light"), 1.3f, 0.5f, new Color(1f, 0.82f, 0.5f), false);
            EditorAssets.SavePrefab(root, WorldPath("Cirelle"));
        }

        // ---- world objects ---------------------------------------------------------------------------

        static Sprite First(string sheet, string clip) => SpriteSheets.Clip(ArtFactory.Sheet("Props", sheet), clip).FirstOrDefault();

        static void EnsureBrokenWick()
        {
            var root = new GameObject("BrokenWick");
            EditorAssets.SetLayer(root, Layers.Wall);
            root.AddComponent<SortingGroup>().sortingLayerName = Layers.SortEntities;
            EditorAssets.Sprite(EditorAssets.Child(root, "Visual"), First("Prop_BrokenWick", "wick"), EditorAssets.Lit, Layers.SortEntities);
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(3.4f, 1.1f);
            box.offset = new Vector2(0f, 0.6f);
            var caster = root.AddComponent<ShadowCaster2D>();
            caster.castingOption = ShadowCaster2D.ShadowCastingOptions.CastShadow;
            // A dying ember at the top: the landmark can be seen from far away.
            EditorAssets.PointLight(EditorAssets.Child(root, "Ember", new Vector3(0.2f, 8.6f, 0f)), 3.5f, 0.55f, new Color(1f, 0.6f, 0.3f), false);
            EditorAssets.SavePrefab(root, WorldPath("BrokenWick"));
        }

        static void EnsureBell()
        {
            string sheet = ArtFactory.Sheet("Props", "Prop_GreatBell");
            var root = new GameObject("GreatBell");
            EditorAssets.SetLayer(root, Layers.Interactable);
            root.AddComponent<SortingGroup>().sortingLayerName = Layers.SortEntities;
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.6f;
            trigger.offset = new Vector2(0f, 0.5f);
            foreach (float x in new[] { -1.1f, 1.1f })
            {
                var post = EditorAssets.Child(root, "Post", new Vector3(x, 0.2f, 0f));
                EditorAssets.SetLayer(post, Layers.Wall);
                post.AddComponent<BoxCollider2D>().size = new Vector2(0.3f, 0.4f);
            }
            var sr = EditorAssets.Sprite(EditorAssets.Child(root, "Visual"), SpriteSheets.Clip(sheet, "still").FirstOrDefault(), EditorAssets.Lit, Layers.SortEntities);
            var bell = root.AddComponent<GreatBell>();
            bell.sprite = sr;
            bell.stillSprite = sr.sprite;
            bell.ringFrames = SpriteSheets.Clip(sheet, "ring");
            bell.promptOffset = new Vector2(0f, 4.2f);
            EditorAssets.SavePrefab(root, WorldPath("GreatBell"));
        }

        static void EnsureGate()
        {
            string sheet = ArtFactory.Sheet("Props", "Prop_Gate");
            var frames = SpriteSheets.Clip(sheet, "gate");
            var root = new GameObject("Gate");
            EditorAssets.SetLayer(root, Layers.Wall);
            root.AddComponent<SortingGroup>().sortingLayerName = Layers.SortEntities;
            var sr = EditorAssets.Sprite(EditorAssets.Child(root, "Visual"), frames.FirstOrDefault(), EditorAssets.Lit, Layers.SortEntities);
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(3f, 1f);
            box.offset = new Vector2(0f, 0.5f);
            var gate = root.AddComponent<Gate>();
            gate.sprite = sr;
            gate.closedSprite = frames.ElementAtOrDefault(0);
            gate.openSprite = frames.ElementAtOrDefault(1);
            gate.blocker = box;
            gate.widthCells = 3;
            EditorAssets.SavePrefab(root, WorldPath("Gate"));
        }

        static void EnsureRelic()
        {
            var root = new GameObject("Relic");
            EditorAssets.SetLayer(root, Layers.Interactable);
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1f;
            var visual = EditorAssets.Child(root, "Visual", new Vector3(0f, 0.6f, 0f));
            var sr = EditorAssets.Sprite(visual, First("Prop_Relics", "relic"), ArtFactory.GlowMaterial(2f), Layers.SortEntities, 3);
            var relic = root.AddComponent<RelicPickup>();
            relic.sprite = sr;
            relic.glow = EditorAssets.PointLight(EditorAssets.Child(root, "Glow", new Vector3(0f, 0.8f, 0f)), 2.8f, 1f, new Color(1f, 0.76f, 0.3f), false);
            relic.promptOffset = new Vector2(0f, 2f);
            EditorAssets.SavePrefab(root, WorldPath("Relic"));
        }

        static void EnsureShrine()
        {
            var root = new GameObject("FlameShrine");
            EditorAssets.SetLayer(root, Layers.Interactable);
            root.AddComponent<SortingGroup>().sortingLayerName = Layers.SortEntities;
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.4f;
            var solid = EditorAssets.Child(root, "Solid");
            EditorAssets.SetLayer(solid, Layers.Wall);
            solid.AddComponent<BoxCollider2D>().size = new Vector2(1.2f, 0.5f);
            EditorAssets.Sprite(EditorAssets.Child(root, "Altar"), First("Prop_Shrine", "shrine"), EditorAssets.Lit, Layers.SortEntities);
            var flameGo = EditorAssets.Child(root, "Flame", new Vector3(0f, 1.3f, 0f));
            var flameSr = EditorAssets.Sprite(flameGo, SpriteSheets.Clip(ArtFactory.Sheet("Player", "Player_Flame"), "flame").FirstOrDefault(),
                ArtFactory.GlowMaterial(2.4f), Layers.SortEntities, 2);
            var anim = flameGo.AddComponent<SpriteAnimator>();
            anim.target = flameSr;
            var shrine = root.AddComponent<FlameShrine>();
            shrine.flameSprite = flameSr;
            shrine.glow = EditorAssets.PointLight(EditorAssets.Child(root, "Glow", new Vector3(0f, 1.5f, 0f)), 4f, 1.2f, Color.white, true);
            shrine.promptOffset = new Vector2(0f, 2.8f);
            EditorAssets.SavePrefab(root, WorldPath("FlameShrine"));
        }

        static void EnsureInscription()
        {
            var root = new GameObject("Inscription");
            EditorAssets.SetLayer(root, Layers.Interactable);
            root.AddComponent<SortingGroup>().sortingLayerName = Layers.SortEntities;
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.2f;
            var solid = EditorAssets.Child(root, "Solid");
            EditorAssets.SetLayer(solid, Layers.Wall);
            solid.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.4f);
            EditorAssets.Sprite(EditorAssets.Child(root, "Visual"), First("Prop_Stone", "stone"), EditorAssets.Lit, Layers.SortEntities);
            root.AddComponent<Inscription>().promptOffset = new Vector2(0f, 1.9f);
            EditorAssets.SavePrefab(root, WorldPath("Inscription"));
        }

        static void EnsureNest(string name, string creature)
        {
            var root = new GameObject(name);
            EditorAssets.Sprite(root, First("Prop_Nest", "nest"), EditorAssets.Lit, Layers.SortGroundDecal, 3);
            var nest = root.AddComponent<SwarmNest>();
            nest.creaturePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath(creature));
            EditorAssets.SavePrefab(root, WorldPath(name));
        }

        static void EnsureExit()
        {
            var root = new GameObject("RegionExit");
            EditorAssets.SetLayer(root, Layers.Zone);
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(3f, 1.2f);
            root.AddComponent<RegionExit>();
            EditorAssets.PointLight(EditorAssets.Child(root, "Glow", new Vector3(0f, 0.5f, 0f)), 3.5f, 0.7f, new Color(0.85f, 0.9f, 1f), false);
            EditorAssets.SavePrefab(root, WorldPath("RegionExit"));
        }
    }
}
