using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wickfall.Audio;
using Wickfall.DevTools;
using Wickfall.Lighting;
using Wickfall.Meta;
using Wickfall.Player;
using Wickfall.Presentation;
using Wickfall.UI;
using Wickfall.World;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Builds the generated scenes. A generated scene carries a <see cref="GeneratedSceneMarker"/>;
    /// a scene without the marker was edited by hand and is never overwritten (prompt §8).
    /// </summary>
    public static class SceneFactory
    {
        public const string Folder = "Assets/Scenes";
        public static string PathOf(string scene, string sub = "") =>
            string.IsNullOrEmpty(sub) ? $"{Folder}/{scene}.unity" : $"{Folder}/{sub}/{scene}.unity";

        public const string UiSortingCanvas = "UI";

        public static void EnsureAll()
        {
            BuildBoot();
            BuildCore();
            BuildMainMenu();
            BuildLightLab();
            BuildEnding();
        }

        static bool CanOverwrite(string path)
        {
            if (!File.Exists(path))
                return true;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool generated = scene.GetRootGameObjects().Any(g => g.GetComponent<GeneratedSceneMarker>() != null);
            if (!generated)
                Debug.LogWarning($"[WICKFALL Setup] {path} was edited by hand (no generated marker): left unchanged.");
            return generated;
        }

        static Scene NewScene(string path)
        {
            EditorPaths.EnsureFolderOf(path);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var marker = new GameObject("[Generated]");
            marker.AddComponent<GeneratedSceneMarker>();
            return scene;
        }

        static void Save(Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("[WICKFALL Setup] Built " + path);
        }

        // ---- Boot -----------------------------------------------------------------------------

        public static void BuildBoot()
        {
            string path = PathOf(SceneNames.Boot);
            if (!CanOverwrite(path))
                return;
            var scene = NewScene(path);
            new GameObject("Boot").AddComponent<Boot>();
            var cam = new GameObject("Boot Camera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x0E, 0x0D, 0x0C, 0xFF);
            cam.orthographic = true;
            cam.depth = -10;
            Save(scene, path);
        }

        // ---- Core -----------------------------------------------------------------------------

        public static void BuildCore()
        {
            string path = PathOf(SceneNames.Core);
            if (!CanOverwrite(path))
                return;
            var scene = NewScene(path);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ArtFactory.FontAssetPath);
            var hudSprites = AssetDatabase.LoadAssetAtPath<HudSprites>(ArtFactory.HudSpritesPath);

            // Player (persistent, like the rest of Core).
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFactory.PlayerPath);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "Player";

            // Camera: 480x270 pixel-perfect, post-processing (bloom on the flames, vignette, grain).
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 270f / 32f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x0E, 0x0D, 0x0C, 0xFF);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            var pixel = camGo.AddComponent<PixelPerfectCamera>();
            pixel.assetsPPU = ArtImportRules.PixelsPerUnit;
            pixel.refResolutionX = CameraRig.ReferenceWidth;
            pixel.refResolutionY = CameraRig.ReferenceHeight;
            pixel.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            pixel.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
            camGo.AddComponent<AudioListener>();
            var rig = camGo.AddComponent<CameraRig>();
            rig.cam = cam;
            rig.pixelPerfect = pixel;
            rig.target = player.transform;

            var volumeGo = new GameObject("Global Volume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = EditorAssets.EnsureVolumeProfile(EditorPaths.GlobalVolume);
            volumeGo.AddComponent<Wickfall.Lighting.ExposureBrightness>();
            volumeGo.AddComponent<PresentationGate>().feature = Feature.PostProcessing;

            // UI
            var ui = BuildUi(font, hudSprites, cam, out var hud, out var fader, out var flash);

            // Systems
            var systems = new GameObject("Systems");
            systems.AddComponent<GameServices>();
            var session = systems.AddComponent<GameSession>();
            session.player = player.GetComponent<PlayerController>();
            session.hud = hud;
            systems.AddComponent<SceneLoader>();
            var audio = systems.AddComponent<AudioManager>();
            audio.library = AssetDatabase.LoadAssetAtPath<AudioLibrary>("Assets/Resources/AudioLibrary.asset");
            var reactive = systems.AddComponent<AudioReactive>();
            reactive.crackle = AudioChild(systems, "Flame Crackle", true, audio.library != null ? audio.library.flameCrackleLoop : null);
            reactive.heartbeat = AudioChild(systems, "Heartbeat", false, audio.library != null ? audio.library.heartbeat : null);
            var feedback = systems.AddComponent<FeedbackDirector>();
            feedback.flashImage = flash;
            var drops = systems.AddComponent<WaxDropField>();
            drops.dropPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFactory.DropPath);
            var popups = systems.AddComponent<DamagePopups>();
            popups.prefab = AssetDatabase.LoadAssetAtPath<TextMeshPro>(PrefabFactory.PopupPath);
            var overlay = systems.AddComponent<DevOverlay>();
            overlay.lineMaterial = EditorAssets.Unlit;

            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>();

            Save(scene, path);
        }

        static AudioSource AudioChild(GameObject parent, string name, bool loop, AudioClip clip)
        {
            var go = EditorAssets.Child(parent, name);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.clip = clip;
            source.spatialBlend = 0f;
            return source;
        }

        // ---- UI helpers -----------------------------------------------------------------------

        public static Canvas MakeCanvas(string name, int order)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            canvas.pixelPerfect = true;
            // one canvas unit per sprite pixel: SetNativeSize on a 16-PPU sprite would otherwise make it 100/16 too big
            // (the scaler only copies it to the canvas on its next update, too late for the images built here)
            go.AddComponent<CanvasScaler>().referencePixelsPerUnit = ArtImportRules.PixelsPerUnit;
            canvas.referencePixelsPerUnit = ArtImportRules.PixelsPerUnit;
            go.AddComponent<PixelCanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Rect(GameObject parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(GameObject parent, string name)
        {
            var rt = Rect(parent, name, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Img(RectTransform rt, Sprite sprite, bool native = true)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            if (native && sprite != null)
                image.SetNativeSize();
            return image;
        }

        public static TextMeshProUGUI Text(RectTransform rt, TMP_FontAsset font, string text, float size = 12f,
            TextAlignmentOptions align = TextAlignmentOptions.Left, Color? color = null)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.fontSize = size;
            t.text = text;
            t.alignment = align;
            t.color = color ?? new Color(0.93f, 0.9f, 0.83f);
            t.enableWordWrapping = false;
            t.raycastTarget = false;
            t.extraPadding = false;
            return t;
        }

        static GameObject BuildUi(TMP_FontAsset font, HudSprites sprites, Camera cam, out GameObject hud, out ScreenFader fader, out Image flash)
        {
            var canvas = MakeCanvas("UI", 10);
            var root = canvas.gameObject;

            hud = Stretch(root, "HUD").gameObject;

            // Candle wax bar, bottom-left.
            var candleRoot = Rect(hud, "Candle", Vector2.zero, new Vector2(0.5f, 0f), new Vector2(18f, 6f), new Vector2(16f, 64f));
            var candleImg = Img(Rect(candleRoot.gameObject, "Wax", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(16, 64)),
                sprites != null ? sprites.candleStates[0] : null);
            var flameImg = Img(Rect(candleRoot.gameObject, "Flame", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(16, 20)),
                sprites != null && sprites.flameAmber != null && sprites.flameAmber.Length > 0 ? sprites.flameAmber[0] : null);
            var iconImg = Img(Rect(hud, "CurrentFlame", Vector2.zero, new Vector2(0f, 0f), new Vector2(30f, 8f), new Vector2(16, 16)),
                sprites != null ? sprites.flameIcons[0] : null);
            var candle = candleRoot.gameObject.AddComponent<HudCandle>();
            candle.sprites = sprites;
            candle.candle = candleImg;
            candle.flame = flameImg;
            candle.currentIcon = iconImg;

            // Counters: dodge charges and status by the candle, shards top-right.
            var counters = hud.AddComponent<HudCounters>();
            counters.sprites = sprites;
            counters.dodgeIcons = new Image[4];
            for (int i = 0; i < 4; i++)
                counters.dodgeIcons[i] = Img(Rect(hud, "Dodge" + i, Vector2.zero, new Vector2(0f, 0f), new Vector2(23f + i * 9f, 20f), new Vector2(24, 24)),
                    sprites != null ? sprites.dodgeFull : null);
            counters.sootIcon = Img(Rect(hud, "Soot", Vector2.zero, Vector2.zero, new Vector2(24f, 38f), new Vector2(24, 24)), sprites != null ? sprites.sootIcon : null);
            counters.lightCutIcon = Img(Rect(hud, "LightCut", Vector2.zero, Vector2.zero, new Vector2(36f, 38f), new Vector2(24, 24)), sprites != null ? sprites.lightCutIcon : null);
            Img(Rect(hud, "ShardIcon", Vector2.one, Vector2.one, new Vector2(-4f, -2f), new Vector2(24, 24)), sprites != null ? sprites.shardIcon : null);
            counters.shardText = Text(Rect(hud, "Shards", Vector2.one, Vector2.one, new Vector2(-28f, -7f), new Vector2(60f, 14f)), font, "0", 12f,
                TextAlignmentOptions.Right);

            // Flame wheel, centre.
            var wheelRt = Rect(hud, "FlameWheel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(96, 96));
            var group = wheelRt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            var wheel = wheelRt.gameObject.AddComponent<HudFlameWheel>();
            wheel.sprites = sprites;
            wheel.group = group;
            wheel.ring = Img(Rect(wheelRt.gameObject, "Ring", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 96)),
                sprites != null ? sprites.wheelRing : null);
            for (int i = 0; i < 5; i++)
            {
                wheel.sockets[i] = Img(Rect(wheelRt.gameObject, "Socket" + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24)),
                    sprites != null ? sprites.wheelSocket : null);
                wheel.icons[i] = Img(Rect(wheelRt.gameObject, "Icon" + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16)),
                    sprites != null ? sprites.flameIcons[i] : null);
            }

            // Interaction prompt (follows the world object).
            var promptRt = Rect(hud, "Prompt", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(140f, 14f));
            promptRt.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            var promptLabel = Text(Stretch(promptRt.gameObject, "Label"), font, "", 12f, TextAlignmentOptions.Center);
            var prompt = hud.AddComponent<InteractPromptView>();
            prompt.root = promptRt;
            prompt.label = promptLabel;
            prompt.worldCamera = cam;
            prompt.canvasRect = (RectTransform)root.transform;

            counters.itemsText = Text(Rect(hud, "Items", Vector2.one, Vector2.one, new Vector2(-8f, -22f), new Vector2(120f, 14f)), font, "", 12f,
                TextAlignmentOptions.Right, new Color(0.64f, 0.6f, 0.55f));

            // Menus and cards above the HUD (built at runtime by UiKit in one style).
            var overlay = Stretch(root, "Overlay").gameObject;
            var kit = overlay.AddComponent<UiKit>();
            kit.font = font;
            kit.sprites = sprites;
            kit.overlay = (RectTransform)overlay.transform;
            overlay.AddComponent<RegionTitleCard>();
            overlay.AddComponent<ItemCard>();
            overlay.AddComponent<ScoreScreen>();
            overlay.AddComponent<BrazierMenuView>();
            overlay.AddComponent<PauseMenuView>();
            overlay.AddComponent<OptionsView>();
            overlay.AddComponent<ChoiceView>();

            flash = Img(Stretch(root, "Flash"), null, false);
            flash.color = new Color(1f, 1f, 1f, 0f);
            flash.enabled = false;

            // Fader on its own canvas, above every menu.
            var top = MakeCanvas("Fader", 100);
            top.transform.SetParent(root.transform.parent, false);
            var black = Img(Stretch(top.gameObject, "Black"), null, false);
            black.color = new Color32(0x0E, 0x0D, 0x0C, 0xFF);
            fader = top.gameObject.AddComponent<ScreenFader>();
            fader.image = black;
            return root;
        }

        // ---- Main menu (first version; the full menus come in M7) -----------------------------

        public static void BuildMainMenu()
        {
            string path = PathOf(SceneNames.MainMenu);
            if (!CanOverwrite(path))
                return;
            var scene = NewScene(path);
            new GameObject("CoreBootstrap").AddComponent<CoreBootstrap>();
            var profile = new GameObject("SceneProfile").AddComponent<SceneProfile>();
            profile.kind = SceneKind.Menu;
            profile.hasRegion = false;
            profile.music = MusicId.Title;
            var global = new GameObject("Global Light");
            EditorAssets.GlobalLight(global, 1f, Color.white);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ArtFactory.FontAssetPath);
            var canvas = MakeCanvas("Menu", 20);
            var root = canvas.gameObject;
            var title = Text(Rect(root, "Title", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(300f, 40f)),
                font, "WICKFALL", 36f, TextAlignmentOptions.Center, new Color(0.93f, 0.9f, 0.83f));
            var menu = root.AddComponent<MainMenu>();
            menu.title = title;
            menu.font = font;
            Save(scene, path);
        }

        // ---- Ending (PDF §1: A, B, C) -----------------------------------------------------------

        public static void BuildEnding()
        {
            string path = PathOf(SceneNames.Ending);
            if (!CanOverwrite(path))
                return;
            var scene = NewScene(path);
            new GameObject("CoreBootstrap").AddComponent<CoreBootstrap>();
            var profile = new GameObject("SceneProfile").AddComponent<SceneProfile>();
            profile.kind = SceneKind.Cinematic;
            profile.hasRegion = false;
            profile.music = MusicId.Ending;
            EditorAssets.GlobalLight(new GameObject("Global Light"), 1f, Color.white);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ArtFactory.FontAssetPath);
            var canvas = MakeCanvas("Ending", 20);
            var root = canvas.gameObject;
            var view = root.AddComponent<EndingView>();
            var artRt = Rect(root, "Art", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(120f, 120f));
            view.art = artRt.gameObject.AddComponent<UnityEngine.UI.Image>();
            view.title = Text(Rect(root, "Title", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(420f, 20f)),
                font, "", 16f, TextAlignmentOptions.Center, new Color(1f, 0.76f, 0.3f));
            view.body = Text(Rect(root, "Body", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -62f), new Vector2(420f, 30f)),
                font, "", 12f, TextAlignmentOptions.Center, new Color(0.93f, 0.9f, 0.83f));
            view.body.enableWordWrapping = true;
            view.tally = Text(Rect(root, "Tally", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -88f), new Vector2(420f, 14f)),
                font, "", 12f, TextAlignmentOptions.Center, new Color(0.64f, 0.6f, 0.55f));
            view.artA = SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_Foyer"), "lit").FirstOrDefault();
            view.artB = SpriteSheets.Clip(ArtFactory.Sheet("Player", "Player_Body"), "idle_down").FirstOrDefault();
            view.artC = SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_BrokenWick"), "wick").FirstOrDefault();
            Save(scene, path);
        }

        // ---- Light Lab (M1 sandbox) -----------------------------------------------------------

        public static void BuildLightLab()
        {
            string path = PathOf(SceneNames.LightLab, "Dev");
            if (!CanOverwrite(path))
                return;
            var scene = NewScene(path);
            new GameObject("CoreBootstrap").AddComponent<CoreBootstrap>();

            var tiles = AssetDatabase.LoadAssetAtPath<RegionTiles>("Assets/Data/Tiles/tallow/RegionTiles_tallow.asset");
            var rows = LevelBuilder.ReadMap(File.ReadAllText("Assets/Levels/LightLab.txt"));
            var level = LevelBuilder.Build(null, "Level", rows, tiles, Vector2Int.zero);

            var region = Db.Region(RegionId.TallowFields);
            var global = new GameObject("Global Light");
            EditorAssets.GlobalLight(global, region.ambientIntensity, region.ambientColor);

            var profile = new GameObject("SceneProfile").AddComponent<SceneProfile>();
            profile.kind = SceneKind.Gameplay;
            profile.region = RegionId.TallowFields;
            profile.sandbox = true;
            profile.music = MusicId.TallowFields;

            var entities = new GameObject("Entities");
            var dark = new List<Vector2>();
            foreach (var marker in level.Markers)
            {
                switch (marker.Symbol)
                {
                    case 'P':
                        var start = new GameObject("PlayerStart");
                        start.transform.SetParent(entities.transform);
                        start.transform.position = marker.Center;
                        profile.playerStart = start.transform;
                        break;
                    case 'B': Place(PrefabFactory.BrazierPath, marker.Center + new Vector2(0f, -0.4f), entities, "lab_brazier"); break;
                    case 'W': Place(PrefabFactory.PoolPath, marker.Center, entities, "lab_pool"); break;
                    case 'G': Place(PrefabFactory.GeyserPath, marker.Center, entities, null, marker.Cell.x * 0.37f); break;
                    case 'x': Place(PrefabFactory.EffigyPath, marker.Center, entities, null); break;
                    case 'X': Place(PrefabFactory.EffigyArmoredPath, marker.Center, entities, null); break;
                    case 'g': Place(PrefabFactory.EffigyGlassPath, marker.Center, entities, null); break;
                    case 'd': dark.Add(marker.Center); break;
                }
            }
            if (dark.Count > 0)
                AddDarkZone(entities, dark);
            Save(scene, path);
        }

        static GameObject Place(string prefabPath, Vector2 position, GameObject parent, string id, float offset = 0f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.scene);
            go.transform.SetParent(parent.transform, true);
            go.transform.position = position;
            if (id != null)
            {
                if (go.TryGetComponent(out Brazier brazier)) brazier.id = id;
                if (go.TryGetComponent(out WaxPool pool)) pool.id = id;
            }
            if (go.TryGetComponent(out WaxGeyser geyser))
                geyser.phaseOffset = offset;
            return go;
        }

        /// <summary>One trigger box over the 'd' cells, plus a dim overhead canopy look (darker shade).</summary>
        static void AddDarkZone(GameObject parent, List<Vector2> cells)
        {
            float minX = cells.Min(c => c.x) - 0.5f, maxX = cells.Max(c => c.x) + 0.5f;
            float minY = cells.Min(c => c.y) - 0.5f, maxY = cells.Max(c => c.y) + 0.5f;
            var go = new GameObject("DarkZone");
            go.transform.SetParent(parent.transform);
            EditorAssets.SetLayer(go, Layers.Zone);
            go.transform.position = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(maxX - minX, maxY - minY);
            go.AddComponent<DarkZone>();
            var shade = EditorAssets.Child(go, "Shade");
            var light = shade.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Freeform;
            light.SetShapePath(new[]
            {
                new Vector3(-box.size.x / 2, -box.size.y / 2), new Vector3(box.size.x / 2, -box.size.y / 2),
                new Vector3(box.size.x / 2, box.size.y / 2), new Vector3(-box.size.x / 2, box.size.y / 2),
            });
            light.blendStyleIndex = WickfallSetup.NegativeBlendStyle; // a canopy subtracts the ambient light
            light.intensity = 0.07f;
            light.color = Color.white;
            light.targetSortingLayers = EditorAssets.AllSortingLayers();
        }

        // ---- build settings -------------------------------------------------------------------

        public static void SetupBuildScenes()
        {
            var order = new List<string>
            {
                PathOf(SceneNames.Boot), PathOf(SceneNames.Core), PathOf(SceneNames.MainMenu),
                PathOf(SceneNames.ConceptShowcase, "Presentation"), PathOf(SceneNames.DesignShowcase, "Presentation"),
                PathOf(SceneNames.LightLab, "Dev"),
            };
            for (int i = 0; i < WickfallCounts.Regions; i++)
            {
                string folder = $"{Folder}/Regions/R{i + 1}";
                if (Directory.Exists(folder))
                    order.AddRange(Directory.GetFiles(folder, "*.unity").Select(p => p.Replace('\\', '/')).OrderBy(p => p));
            }
            order.Add(PathOf(SceneNames.Ending));
            EditorBuildSettings.scenes = order.Where(File.Exists).Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        }
    }
}
