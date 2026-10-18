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
    /// <summary>Region 6 prefabs: the Roi Creux and his voids, the Grand Foyer, the pact shrines.</summary>
    public static class HearthPrefabs
    {
        public const string VoidPath = "Assets/Prefabs/Combat/VoidBall.prefab";

        public static void EnsureAll()
        {
            var voids = EnsureVoid();
            EnsureKing(voids);
            EnsureFoyer();
            EnsurePactShrine();
        }

        static Projectile EnsureVoid()
        {
            var frames = SpriteSheets.Clip(ArtFactory.Sheet("VFX", "VFX_VoidBall"), "fx");
            var root = new GameObject("VoidBall");
            var sr = EditorAssets.Sprite(root, frames.FirstOrDefault(), EditorAssets.Lit, Layers.SortVfx);
            var projectile = root.AddComponent<Projectile>();
            projectile.sprite = sr;
            projectile.radius = 0.24f;
            projectile.spin = true;
            projectile.impactVfx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/VFX_VoidBurst.prefab");
            root.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
            root.AddComponent<PooledObject>();
            return EditorAssets.SavePrefab(root, VoidPath).GetComponent<Projectile>();
        }

        static Light2D NegativeLight(GameObject go, float radius, float intensity)
        {
            var light = EditorAssets.PointLight(go, radius, intensity, Color.white, false, WickfallSetup.NegativeBlendStyle);
            light.falloffIntensity = 0.6f;
            return light;
        }

        static void EnsureKing(Projectile voids)
        {
            string sheet = ArtFactory.Sheet("Enemies", "Boss_HollowKing");
            var type = AssetDatabase.LoadAssetAtPath<EnemyTypeSO>($"{EditorPaths.Enemies}/Enemy_HollowKing.asset");
            var root = new GameObject("HollowKing");
            EditorAssets.SetLayer(root, Layers.Enemy);
            root.AddComponent<SortingGroup>().sortingLayerName = Layers.SortEntities;
            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.mass = 50f;
            var feet = root.AddComponent<CircleCollider2D>();
            feet.radius = 1.1f;
            feet.offset = new Vector2(0f, 0.8f);
            var body = root.AddComponent<Damageable>();
            body.team = Team.Enemy;
            body.tags = DamageTags.Hollow;
            body.kind = EnemyKind.HollowKing;
            body.knockbackResistance = 1f;
            root.AddComponent<EnemyMotor>();
            var hurt = EditorAssets.Child(root, "Hurtbox", new Vector3(0f, 3.5f, 0f));
            EditorAssets.SetLayer(hurt, Layers.Enemy);
            var hc = hurt.AddComponent<CapsuleCollider2D>();
            hc.isTrigger = true;
            hc.size = new Vector2(3.4f, 7.5f);
            hurt.AddComponent<Hurtbox>();
            var visual = EditorAssets.Child(root, "Visual");
            var sr = EditorAssets.Sprite(visual, SpriteSheets.Clip(sheet, "idle1").FirstOrDefault(), EditorAssets.Lit, Layers.SortEntities);
            var anim = visual.AddComponent<SpriteAnimator>();
            anim.target = sr;
            var king = root.AddComponent<BossHollowKing>();
            king.type = type;
            king.config = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Resources/GameDatabase.asset")?.boss;
            king.voidPrefab = voids;
            king.sprite = sr;
            king.animator = anim;
            king.idle1 = SpriteSheets.Clip(sheet, "idle1");
            king.idle2 = SpriteSheets.Clip(sheet, "idle2");
            king.idle3 = SpriteSheets.Clip(sheet, "idle3");
            king.slam = SpriteSheets.Clip(sheet, "slam");
            king.fallen = SpriteSheets.Clip(sheet, "fallen");
            king.negative = NegativeLight(EditorAssets.Child(root, "NegativePulse", new Vector3(0f, 1f, 0f)), 5.5f, 0f);
            var slamGo = new GameObject("SlamHitbox");
            slamGo.transform.SetParent(root.transform, false);
            king.slamHitbox = slamGo.AddComponent<Hitbox>();
            // the fallen King can be judged
            var judge = EditorAssets.Child(root, "Judgement", new Vector3(0f, 0.6f, 0f));
            EditorAssets.SetLayer(judge, Layers.Interactable);
            var jc = judge.AddComponent<CircleCollider2D>();
            jc.isTrigger = true;
            jc.radius = 2.4f;
            var j = judge.AddComponent<KingJudgement>();
            j.king = king;
            j.promptOffset = new Vector2(0f, 4f);
            judge.SetActive(false);
            EditorAssets.SavePrefab(root, WorldPrefabs.EnemyPath("HollowKing"));
        }

        static void EnsureFoyer()
        {
            string sheet = ArtFactory.Sheet("Props", "Prop_Foyer");
            var root = new GameObject("GrandFoyer");
            EditorAssets.SetLayer(root, Layers.Interactable);
            root.AddComponent<SortingGroup>().sortingLayerName = Layers.SortEntities;
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 3.6f;
            trigger.offset = new Vector2(0f, 1f);
            var solid = EditorAssets.Child(root, "Solid", new Vector3(0f, 1f, 0f));
            EditorAssets.SetLayer(solid, Layers.Wall);
            solid.AddComponent<BoxCollider2D>().size = new Vector2(3.4f, 1.6f);
            var sr = EditorAssets.Sprite(EditorAssets.Child(root, "Visual"), SpriteSheets.Clip(sheet, "cold").FirstOrDefault(), EditorAssets.Lit, Layers.SortEntities);
            var foyer = root.AddComponent<FoyerAltar>();
            foyer.sprite = sr;
            foyer.promptOffset = new Vector2(0f, 6f);
            // "Foyer en lumière négative": a darkness that drinks the light around it
            foyer.negative = NegativeLight(EditorAssets.Child(root, "Negative", new Vector3(0f, 2.5f, 0f)), 9f, 0.55f);
            foyer.relit = EditorAssets.PointLight(EditorAssets.Child(root, "Relit", new Vector3(0f, 4f, 0f)), 14f, 1.6f, new Color(1f, 0.72f, 0.35f), true);
            EditorAssets.SavePrefab(root, WorldPrefabs.WorldPath("GrandFoyer"));
        }

        static void EnsurePactShrine()
        {
            var root = new GameObject("PactShrine");
            EditorAssets.SetLayer(root, Layers.Interactable);
            root.AddComponent<SortingGroup>().sortingLayerName = Layers.SortEntities;
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.2f;
            var solid = EditorAssets.Child(root, "Solid", new Vector3(0f, 0.3f, 0f));
            EditorAssets.SetLayer(solid, Layers.Wall);
            solid.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.6f);
            var sr = EditorAssets.Sprite(EditorAssets.Child(root, "Visual"),
                SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_PactShrine"), "shrine").FirstOrDefault(), EditorAssets.Lit, Layers.SortEntities);
            var shrine = root.AddComponent<PactShrine>();
            shrine.sprite = sr;
            shrine.promptOffset = new Vector2(0f, 3.2f);
            shrine.shade = NegativeLight(EditorAssets.Child(root, "Shade", new Vector3(0f, 1.6f, 0f)), 2.6f, 0.12f);
            EditorAssets.SavePrefab(root, WorldPrefabs.WorldPath("PactShrine"));
        }
    }
}
