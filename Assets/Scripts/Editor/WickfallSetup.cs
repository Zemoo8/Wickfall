using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Wickfall.Presentation;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Re-runnable project setup: player settings, the URP 2D renderer (with a subtractive
    /// "Negative" light blend style for the Foyer's negative light), layers, physics, every data
    /// asset with the PDF's values, and the scenes. It only creates what is missing and re-applies
    /// settings, so it is safe to run any time.
    /// Menu: WICKFALL > Setup > Run Project Setup.
    /// Headless: -batchmode -quit -executeMethod Wickfall.EditorTools.WickfallSetup.Run
    /// </summary>
    public static class WickfallSetup
    {
        public const string CompanyName = "Zemoo8";
        public const string ProductName = "WICKFALL";
        public const string Version = "0.1.0";

        /// <summary>Index of the light blend style used for negative light (PDF §2.2, Chambre du Foyer).</summary>
        public const int NegativeBlendStyle = 3;

        [MenuItem("WICKFALL/Setup/Run Project Setup", priority = 0)]
        public static void Run()
        {
            SetupProjectSettings();
            SetupRenderer();
            SetupTagsAndLayers();
            SetupPhysics();
            DataFactory.EnsureAll();
            EnsurePresentationConfig();
            ContentPipeline.EnsureAll();
            AssetDatabase.SaveAssets();
            Debug.Log("[WICKFALL Setup] Project setup complete.");
        }

        /// <summary>Settings and data only (no scenes): the fast path used by tests and the first boot.</summary>
        [MenuItem("WICKFALL/Setup/Settings and Data Only", priority = 1)]
        public static void RunSettingsAndData()
        {
            SetupProjectSettings();
            SetupRenderer();
            SetupTagsAndLayers();
            SetupPhysics();
            DataFactory.EnsureAll();
            EnsurePresentationConfig();
            AssetDatabase.SaveAssets();
            Debug.Log("[WICKFALL Setup] Settings and data ready.");
        }

        static void SetupProjectSettings()
        {
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
            EditorSettings.enterPlayModeOptionsEnabled = false;

            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false; // the game pauses when it loses focus (Appendix A)
            PlayerSettings.visibleInBackground = false;
            PlayerSettings.forceSingleInstance = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SplashScreen.show = true; // Unity's own splash cannot be removed on a Personal licence
            PlayerSettings.SplashScreen.showUnityLogo = true;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            PlayerSettings.SplashScreen.backgroundColor = new Color32(0x0E, 0x0D, 0x0C, 0xFF);
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Static;

            // Input System only (Project Settings > Player > Active Input Handling).
            var player = Resources.FindObjectsOfTypeAll<PlayerSettings>().FirstOrDefault();
            if (player != null)
            {
                var so = new SerializedObject(player);
                var handler = so.FindProperty("activeInputHandler");
                if (handler != null && handler.intValue != 1)
                {
                    handler.intValue = 1;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // 60 Hz physics: one step per frame at 60 fps.
            Time.fixedDeltaTime = 1f / 60f;
            EditorUtility.SetDirty(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset")[0]);

            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        }

        static void SetupRenderer()
        {
            EditorPaths.EnsureFolder(EditorPaths.Settings);

            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(EditorPaths.Renderer);
            if (renderer == null)
            {
                // Mirrors URP's own Create > Rendering > URP 2D Renderer menu.
                renderer = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(renderer, EditorPaths.Renderer);
                ResourceReloader.ReloadAllNullIn(renderer, UniversalRenderPipelineAsset.packagePath);
                Debug.Log("[WICKFALL Setup] Created " + EditorPaths.Renderer);
            }

            var data = new SerializedObject(renderer);
            var postProcess = data.FindProperty("m_PostProcessData");
            if (postProcess.objectReferenceValue == null)
                postProcess.objectReferenceValue = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                    "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");

            // Top-down: sprites further up the screen draw behind (sort by Y).
            data.FindProperty("m_TransparencySortMode").intValue = (int)TransparencySortMode.CustomAxis;
            data.FindProperty("m_TransparencySortAxis").vector3Value = new Vector3(0f, 1f, 0f);
            data.FindProperty("m_LightRenderTextureScale").floatValue = 1f; // crisp light edges on pixel art

            var styles = data.FindProperty("m_LightBlendStyles");
            if (styles.arraySize >= 4)
            {
                var negative = styles.GetArrayElementAtIndex(NegativeBlendStyle);
                negative.FindPropertyRelative("name").stringValue = "Negative";
                negative.FindPropertyRelative("blendMode").intValue = 2;          // Subtractive
                negative.FindPropertyRelative("maskTextureChannel").intValue = 0; // no mask
            }
            data.ApplyModifiedPropertiesWithoutUndo();

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(EditorPaths.Pipeline);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, EditorPaths.Pipeline);
                Debug.Log("[WICKFALL Setup] Created " + EditorPaths.Pipeline);
            }

            var p = new SerializedObject(pipeline);
            p.FindProperty("m_MSAA").intValue = 1;            // off: sprites do not need it
            p.FindProperty("m_RenderScale").floatValue = 1f;  // full resolution keeps pixels crisp
            p.FindProperty("m_SupportsHDR").boolValue = true;  // bloom on the flames
            p.FindProperty("m_RequireDepthTexture").boolValue = false;
            p.FindProperty("m_RequireOpaqueTexture").boolValue = false;
            p.ApplyModifiedPropertiesWithoutUndo();

            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            EditorUtility.SetDirty(pipeline);
            EditorUtility.SetDirty(renderer);
        }

        static void SetupTagsAndLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            var layers = tagManager.FindProperty("layers");
            for (int i = 0; i < Layers.PhysicsOrder.Length; i++)
            {
                var layer = layers.GetArrayElementAtIndex(Layers.FirstUserLayer + i);
                if (string.IsNullOrEmpty(layer.stringValue))
                    layer.stringValue = Layers.PhysicsOrder[i];
                else if (layer.stringValue != Layers.PhysicsOrder[i])
                    Debug.LogWarning($"[WICKFALL Setup] Layer {Layers.FirstUserLayer + i} is '{layer.stringValue}', expected '{Layers.PhysicsOrder[i]}'.");
            }

            var tags = tagManager.FindProperty("tags");
            foreach (string tag in new[] { "Player" })
            {
                bool found = false;
                for (int i = 0; i < tags.arraySize; i++)
                    found |= tags.GetArrayElementAtIndex(i).stringValue == tag;
                if (!found && tag != "Player") // "Player" is built in
                {
                    tags.InsertArrayElementAtIndex(tags.arraySize);
                    tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
                }
            }

            // Sorting layers in WICKFALL order, keeping the IDs of layers that already exist.
            var sorting = tagManager.FindProperty("m_SortingLayers");
            var existing = new List<(string name, uint id, bool locked)>();
            for (int i = 0; i < sorting.arraySize; i++)
            {
                var e = sorting.GetArrayElementAtIndex(i);
                existing.Add((e.FindPropertyRelative("name").stringValue, e.FindPropertyRelative("uniqueID").uintValue,
                    e.FindPropertyRelative("locked").boolValue));
            }

            var ordered = new List<(string name, uint id, bool locked)>();
            foreach (string name in Layers.SortingOrder)
            {
                int found = existing.FindIndex(e => e.name == name);
                ordered.Add(found >= 0 ? existing[found] : (name, StableId(name), false));
            }
            ordered.AddRange(existing.Where(e => !Layers.SortingOrder.Contains(e.name)));

            sorting.arraySize = ordered.Count;
            for (int i = 0; i < ordered.Count; i++)
            {
                var e = sorting.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("name").stringValue = ordered[i].name;
                e.FindPropertyRelative("uniqueID").uintValue = ordered[i].id;
                e.FindPropertyRelative("locked").boolValue = ordered[i].locked;
            }

            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        static uint StableId(string name)
        {
            uint id = (uint)Animator.StringToHash("WickfallSortingLayer_" + name);
            return id == 0 ? 1u : id;
        }

        static void SetupPhysics()
        {
            Physics2D.gravity = Vector2.zero;      // top-down
            Physics2D.queriesHitTriggers = false;  // sight lines and attacks ignore trigger zones
            Physics2D.queriesStartInColliders = false;
            Physics2D.autoSyncTransforms = false;
            Physics2D.velocityIterations = 8;
            Physics2D.positionIterations = 3;

            var physics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset")[0]);
            var matrix = physics.FindProperty("m_LayerCollisionMatrix");

            int wall = LayerMask.NameToLayer(Layers.Wall);
            int player = LayerMask.NameToLayer(Layers.Player);
            int enemy = LayerMask.NameToLayer(Layers.Enemy);
            int hazard = LayerMask.NameToLayer(Layers.Hazard);
            int pickup = LayerMask.NameToLayer(Layers.Pickup);
            int interactable = LayerMask.NameToLayer(Layers.Interactable);
            int projectile = LayerMask.NameToLayer(Layers.Projectile);
            int occluder = LayerMask.NameToLayer(Layers.Occluder);
            int breakable = LayerMask.NameToLayer(Layers.Breakable);
            int zone = LayerMask.NameToLayer(Layers.Zone);

            for (int layer = 0; layer < 32; layer++)
            {
                // Pickups, hazards and zones only report the player (zones also see enemies, e.g. ash).
                SetCollision(matrix, pickup, layer, layer == player);
                SetCollision(matrix, hazard, layer, layer == player || layer == enemy);
                SetCollision(matrix, zone, layer, layer == player || layer == enemy);
                // The canopy blocks sight and light only; nothing collides with it.
                SetCollision(matrix, occluder, layer, false);
                // Projectiles hit walls, glass, the player and enemies (team decides who is hurt).
                SetCollision(matrix, projectile, layer, layer == wall || layer == breakable || layer == player || layer == enemy);
            }
            SetCollision(matrix, enemy, enemy, true);
            SetCollision(matrix, player, enemy, true);
            SetCollision(matrix, interactable, player, true);
            SetCollision(matrix, interactable, enemy, true);
            physics.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetCollision(SerializedProperty matrix, int a, int b, bool collide)
        {
            if (a < 0 || b < 0)
                return;
            SetBit(matrix.GetArrayElementAtIndex(a), b, collide);
            SetBit(matrix.GetArrayElementAtIndex(b), a, collide);
        }

        static void SetBit(SerializedProperty row, int bit, bool on)
        {
            uint mask = 1u << bit;
            row.uintValue = on ? row.uintValue | mask : row.uintValue & ~mask;
        }

        public const string PresentationConfigPath = "Assets/Resources/PresentationConfig.asset";

        static void EnsurePresentationConfig()
        {
            EditorPaths.EnsureFolder(EditorPaths.Resources);
            if (AssetDatabase.LoadAssetAtPath<PresentationConfig>(PresentationConfigPath) != null)
                return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<PresentationConfig>(), PresentationConfigPath);
            Debug.Log("[WICKFALL Setup] Created " + PresentationConfigPath);
        }
    }
}
