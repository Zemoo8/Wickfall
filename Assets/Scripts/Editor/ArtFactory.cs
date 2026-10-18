using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Tilemaps;
using Wickfall.Combat;
using Wickfall.Player;
using Wickfall.UI;
using Wickfall.World;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Turns the generated art (Tools/art -> Assets/Art/Generated) into game data: sliced sprites,
    /// materials, the player's sprite set, the HUD sprite set, VFX prefabs and their library, the
    /// pixel font asset and the region tilesets.
    /// </summary>
    public static class ArtFactory
    {
        public const string Generated = "Assets/Art/Generated";
        public const string VisualsFolder = "Assets/Data/Visuals";
        public const string PlayerSpritesPath = VisualsFolder + "/PlayerSprites.asset";
        public const string HudSpritesPath = VisualsFolder + "/HudSprites.asset";
        public const string VfxLibraryPath = "Assets/Resources/VfxLibrary.asset";
        public const string FontPath = "Assets/Art/Fonts/WickfallPixel.ttf";
        public const string FontAssetPath = "Assets/Art/Fonts/WickfallPixel.asset";
        public const string TmpEssentials = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        public static string Sheet(string folder, string name) => $"{Generated}/{folder}/{name}.png";

        /// <summary>Imports TMP's essential resources (shaders and settings) once. Returns true if it had to.</summary>
        public static bool EnsureTmpEssentials()
        {
            if (File.Exists(TmpEssentials))
                return false;
            string package = Directory.GetDirectories("Library/PackageCache", "com.unity.ugui@*").FirstOrDefault();
            if (package == null)
                return false;
            string path = Path.Combine(package, "Package Resources", "TMP Essential Resources.unitypackage");
            Debug.Log("[WICKFALL Setup] Importing TMP Essential Resources from " + path);
            AssetDatabase.ImportPackage(path, false);
            AssetDatabase.Refresh();
            return true;
        }

        public static void EnsureAll()
        {
            SpriteSheets.SliceAll();
            EnsurePlayerSprites();
            EnsureHudSprites();
            EnsureVfx();
            EnsureFont();
            EnsureTiles("tallow");
            EnsureTiles("wickwood");
            EnsureTiles("foundry");
            AssetDatabase.SaveAssets();
        }

        // ---- player ---------------------------------------------------------------------------------

        static SpriteClip Clip(string sheet, Dictionary<string, Sprite> sprites, SpriteSheets.SheetMeta meta, string clip, string fpsKey, bool loop)
        {
            var entry = meta.clipList.FirstOrDefault(c => c.name == clip);
            var frames = entry == null ? new Sprite[0] : entry.frames.Select(n => sprites.TryGetValue(n, out var s) ? s : null).Where(s => s != null).ToArray();
            return new SpriteClip(frames, SpriteSheets.Fps(meta, fpsKey, 10f), loop);
        }

        public static PlayerSpriteSet EnsurePlayerSprites()
        {
            EditorPaths.EnsureFolder(VisualsFolder);
            var set = AssetDatabase.LoadAssetAtPath<PlayerSpriteSet>(PlayerSpritesPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<PlayerSpriteSet>();
                AssetDatabase.CreateAsset(set, PlayerSpritesPath);
            }

            string body = Sheet("Player", "Player_Body");
            var meta = SpriteSheets.ReadMeta(body);
            var sprites = SpriteSheets.Load(body);
            string[] dirs = { "down", "up", "left", "right" };
            set.idle = dirs.Select(d => Clip(body, sprites, meta, "idle_" + d, "idle", true)).ToArray();
            set.walk = dirs.Select(d => Clip(body, sprites, meta, "walk_" + d, "walk", true)).ToArray();
            set.attack = dirs.Select(d => Clip(body, sprites, meta, "attack_" + d, "attack", false)).ToArray();
            set.dash = dirs.Select(d => Clip(body, sprites, meta, "dash_" + d, "dash", false)).ToArray();
            set.dim = Clip(body, sprites, meta, "dim", "dim", true);
            set.hurt = Clip(body, sprites, meta, "hurt", "hurt", false);
            set.death = Clip(body, sprites, meta, "death", "death", false);
            set.flameAnchor = dirs.Select(d =>
            {
                var a = meta.flameAnchorList.FirstOrDefault(x => x.name == d);
                return a != null ? new Vector2(a.x, a.y) : new Vector2(0, 31);
            }).ToArray();
            if (meta.dimFlameAnchor != null && meta.dimFlameAnchor.Length == 2)
                set.dimFlameAnchor = new Vector2(meta.dimFlameAnchor[0], meta.dimFlameAnchor[1]);
            if (meta.walkBob != null)
                set.walkBob = meta.walkBob;

            string flame = Sheet("Player", "Player_Flame");
            var fmeta = SpriteSheets.ReadMeta(flame);
            var fsprites = SpriteSheets.Load(flame);
            set.flame = Clip(flame, fsprites, fmeta, "flame", "flame", true);
            set.flameDim = Clip(flame, fsprites, fmeta, "flame_dim", "flame_dim", true);
            set.pixelsPerUnit = ArtImportRules.PixelsPerUnit;
            EditorUtility.SetDirty(set);
            return set;
        }

        // ---- HUD ------------------------------------------------------------------------------------

        public static HudSprites EnsureHudSprites()
        {
            EditorPaths.EnsureFolder(VisualsFolder);
            var hud = AssetDatabase.LoadAssetAtPath<HudSprites>(HudSpritesPath);
            if (hud == null)
            {
                hud = ScriptableObject.CreateInstance<HudSprites>();
                AssetDatabase.CreateAsset(hud, HudSpritesPath);
            }

            string candle = Sheet("UI", "HUD_Candle");
            hud.candleStates = SpriteSheets.Clip(candle, "candle");
            var cmeta = SpriteSheets.ReadMeta(candle);
            var tops = new float[8];
            if (cmeta.anchorsList == null)
            {
                // candleTops is stored as a dictionary {"0": [0, top]} -> read via the generic list key
                string json = File.ReadAllText(Path.ChangeExtension(candle, ".json"));
                var parsed = JsonUtility.FromJson<CandleTops>(json);
                if (parsed.candleTopsList != null)
                    foreach (var e in parsed.candleTopsList)
                        if (int.TryParse(e.name, out int i) && i >= 0 && i < 8)
                            tops[i] = e.y;
            }
            hud.candleTops = tops;

            string flames = Sheet("UI", "HUD_Flames");
            hud.flameAmber = SpriteSheets.Clip(flames, "flame_amber");
            hud.flameBlue = SpriteSheets.Clip(flames, "flame_blue");
            hud.flameGreen = SpriteSheets.Clip(flames, "flame_green");
            hud.flameWhite = SpriteSheets.Clip(flames, "flame_white");
            hud.flameCrimson = SpriteSheets.Clip(flames, "flame_crimson");

            var icons = SpriteSheets.Clip(Sheet("UI", "HUD_FlameIcons"), "icon");
            hud.flameIcons = icons.Take(5).ToArray();
            hud.flameIconLocked = icons.Length > 5 ? icons[5] : null;

            var sockets = SpriteSheets.Clip(Sheet("UI", "HUD_Small"), "socket");
            hud.wheelSocket = sockets[0];
            hud.wheelSocketSelected = sockets[1];
            var misc = SpriteSheets.Clip(Sheet("UI", "HUD_Small"), "misc");
            hud.shardIcon = misc[0];
            hud.dodgeFull = misc[1];
            hud.dodgeEmpty = misc[2];
            hud.sootIcon = misc[3];
            hud.lightCutIcon = misc[4];
            hud.keyCap = misc[5];
            hud.wheelRing = SpriteSheets.Clip(Sheet("UI", "HUD_Wheel"), "ring").FirstOrDefault();

            // The panel is a nine-slice: give it 16 px borders.
            string panelPath = Sheet("UI", "UI_Panel");
            SetBorder(panelPath, new Vector4(16, 16, 16, 16));
            hud.panelFrame = SpriteSheets.Clip(panelPath, "panel").FirstOrDefault();
            EditorUtility.SetDirty(hud);
            return hud;
        }

        [System.Serializable]
        sealed class CandleTops
        {
            public SpriteSheets.NamedVector[] candleTopsList;
        }

        static void SetBorder(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;
            var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var rects = provider.GetSpriteRects();
            bool changed = false;
            foreach (var r in rects)
            {
                if (r.border != border)
                {
                    r.border = border;
                    changed = true;
                }
            }
            if (!changed)
                return;
            provider.SetSpriteRects(rects);
            provider.Apply();
            importer.SaveAndReimport();
        }

        // ---- VFX ------------------------------------------------------------------------------------

        struct VfxSpec
        {
            public VfxId id;
            public string sheet;
            public bool lit;
            public float light;
            public float intensity;
            public Color tint;

            public VfxSpec(VfxId id, string sheet, bool lit = false, float light = 0f, float intensity = 1.6f, Color tint = default)
            {
                this.id = id;
                this.sheet = sheet;
                this.lit = lit;
                this.light = light;
                this.intensity = intensity;
                this.tint = tint;
            }
        }

        static readonly VfxSpec[] VfxSpecs =
        {
            new VfxSpec(VfxId.Slash, "VFX_Slash", light: 2.4f, intensity: 2.2f),
            new VfxSpec(VfxId.WideSweep, "VFX_WideSweep", light: 3.2f, intensity: 2.2f),
            new VfxSpec(VfxId.RadiantPulse, "VFX_RadiantPulse", light: 5f, intensity: 2.6f),
            new VfxSpec(VfxId.PoisonCloud, "VFX_PoisonCloud", light: 2f, intensity: 1.4f),
            new VfxSpec(VfxId.CrimsonStrike, "VFX_CrimsonStrike", light: 2.5f, intensity: 2.4f),
            new VfxSpec(VfxId.EmberTrail, "VFX_EmberTrail", intensity: 2f),
            new VfxSpec(VfxId.WaxDrip, "VFX_WaxDrip", lit: true),
            new VfxSpec(VfxId.ExtinguishBurst, "VFX_ExtinguishBurst", light: 3f, intensity: 1.2f),
            new VfxSpec(VfxId.Flare, "VFX_Flare", light: 6f, intensity: 2.4f),
            new VfxSpec(VfxId.SootSplash, "VFX_SootSplash", lit: true),
            new VfxSpec(VfxId.HitSpark, "VFX_HitSpark", intensity: 2.2f),
            new VfxSpec(VfxId.BlockedSpark, "VFX_HitSpark", intensity: 1f, tint: new Color(0.64f, 0.6f, 0.55f)),
            new VfxSpec(VfxId.BoltImpact, "VFX_HitSpark", light: 1.2f, intensity: 2.2f),
            new VfxSpec(VfxId.LaserImpact, "VFX_HitSpark", light: 1.2f, intensity: 2.2f),
            new VfxSpec(VfxId.DeathPuff, "VFX_DeathPuff", lit: true),
            new VfxSpec(VfxId.DustPuff, "VFX_DustPuff", lit: true),
            new VfxSpec(VfxId.GeyserBurst, "VFX_GeyserBurst", lit: true),
            new VfxSpec(VfxId.SteamBurst, "VFX_SteamBurst", lit: true),
            new VfxSpec(VfxId.GlassShards, "VFX_GlassShards", intensity: 1.2f),
            new VfxSpec(VfxId.AlertMark, "VFX_AlertMark", intensity: 1.6f),
            new VfxSpec(VfxId.RelightBurst, "VFX_RelightBurst", light: 3f, intensity: 2.2f, tint: new Color(1f, 0.76f, 0.3f)),
            new VfxSpec(VfxId.PickupGlint, "VFX_PickupGlint", intensity: 2f),
            new VfxSpec(VfxId.VoidBurst, "VFX_VoidBurst", lit: false, intensity: 1f),
            new VfxSpec(VfxId.AshWave, "VFX_AshWave", lit: true),
        };

        public static VfxLibrary EnsureVfx()
        {
            var lib = AssetDatabase.LoadAssetAtPath<VfxLibrary>(VfxLibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<VfxLibrary>();
                AssetDatabase.CreateAsset(lib, VfxLibraryPath);
            }
            var entries = new List<VfxLibrary.Entry>();
            foreach (var spec in VfxSpecs)
            {
                string sheet = Sheet("VFX", spec.sheet);
                var frames = SpriteSheets.Clip(sheet, "fx");
                if (frames.Length == 0)
                    continue;
                var meta = SpriteSheets.ReadMeta(sheet);
                var root = new GameObject("VFX_" + spec.id);
                var sr = EditorAssets.Sprite(root, frames[0], spec.lit ? EditorAssets.Lit : GlowMaterial(spec.intensity), Layers.SortVfx);
                var vfx = root.AddComponent<VfxInstance>();
                vfx.sprite = sr;
                vfx.frames = frames;
                vfx.fps = SpriteSheets.Fps(meta, "fx", 14f);
                if (spec.light > 0f)
                {
                    var lightGo = EditorAssets.Child(root, "Flash");
                    var light = EditorAssets.PointLight(lightGo, spec.light, 1.2f, Color.white, false);
                    vfx.flashLight = light;
                    vfx.flashIntensity = 1.1f;
                }
                if (spec.tint.a > 0f)
                    sr.color = spec.tint;
                root.AddComponent<PooledObject>();
                var prefab = EditorAssets.SavePrefab(root, $"Assets/Prefabs/VFX/VFX_{spec.id}.prefab");
                entries.Add(new VfxLibrary.Entry { id = spec.id, prefab = prefab });
            }
            lib.entries = entries.ToArray();
            EditorUtility.SetDirty(lib);
            return lib;
        }

        static readonly Dictionary<float, Material> glowCache = new Dictionary<float, Material>();

        /// <summary>One glow material per intensity (keeps the SRP batcher happy).</summary>
        public static Material GlowMaterial(float intensity)
        {
            string name = "Mat_Glow_" + Mathf.RoundToInt(intensity * 10);
            var mat = EditorAssets.Material(name, "Wickfall/SpriteGlow");
            if (mat != null && !Mathf.Approximately(mat.GetFloat("_Intensity"), intensity))
            {
                mat.SetFloat("_Intensity", intensity);
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        // ---- font -----------------------------------------------------------------------------------

        public const string Charset =
            " !\"#%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]_abcdefghijklmnopqrstuvwxyz|" +
            "«»°·×ÀÁÂÄÆÇÈÉÊËÎÏÔÖÙÛÜàáâäæçèéêëíîïñóôöùúûüÿŒœŸ–—‘’…←↑→↓■♦©";

        public static TMP_FontAsset EnsureFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null)
                return existing;
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                Debug.LogError("[WICKFALL Setup] " + FontPath + " is missing: run python Tools/font/make_font.py");
                return null;
            }
            // Raster glyphs at the font's native 12 px em: crisp when the canvas scales by whole numbers.
            var asset = TMP_FontAsset.CreateFontAsset(font, 12, 1, GlyphRenderMode.RASTER, 256, 256, AtlasPopulationMode.Dynamic, false);
            asset.name = "WickfallPixel";
            AssetDatabase.CreateAsset(asset, FontAssetPath);
            var atlas = asset.atlasTextures[0];
            atlas.name = "WickfallPixel Atlas";
            atlas.filterMode = FilterMode.Point;
            AssetDatabase.AddObjectToAsset(atlas, asset);
            asset.material.name = "WickfallPixel Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            asset.TryAddCharacters(Charset, out string missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning("[WICKFALL Setup] Font is missing: " + missing);
            asset.atlasTextures[0].filterMode = FilterMode.Point;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        // ---- tiles ----------------------------------------------------------------------------------

        public static RegionTiles EnsureTiles(string key)
        {
            string sheet = Sheet("Tiles", "Tiles_" + key);
            var meta = SpriteSheets.ReadMeta(sheet);
            if (meta == null)
                return null;
            var sprites = SpriteSheets.Load(sheet);
            string folder = $"Assets/Data/Tiles/{key}";
            EditorPaths.EnsureFolder(folder);

            Tile TileFor(string spriteName, Tile.ColliderType collider = Tile.ColliderType.None)
            {
                if (!sprites.TryGetValue(spriteName, out var sprite))
                    return null;
                string path = $"{folder}/{spriteName}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, path);
                }
                tile.sprite = sprite;
                tile.colliderType = collider;
                EditorUtility.SetDirty(tile);
                return tile;
            }

            string setPath = $"{folder}/RegionTiles_{key}.asset";
            var set = AssetDatabase.LoadAssetAtPath<RegionTiles>(setPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<RegionTiles>();
                AssetDatabase.CreateAsset(set, setPath);
            }
            set.key = key;
            set.floors = Enumerable.Range(0, 4).Select(i => (TileBase)TileFor("floor_" + i)).ToArray();

            var tops = new List<RegionTiles.MaskTile>();
            string json = File.ReadAllText(Path.ChangeExtension(sheet, ".json"));
            var masks = JsonUtility.FromJson<BlobMasks>(json);
            foreach (var m in masks.blobMasksList)
            {
                int index = int.Parse(m.name);
                tops.Add(new RegionTiles.MaskTile { mask = Mathf.RoundToInt(m.x), tile = TileFor("top_" + index, Tile.ColliderType.Grid) });
            }
            set.tops = tops.ToArray();

            // face order from tiles.py: west (F,T) x east (F,T) x variant (0,1)
            set.faces = Enumerable.Range(0, 8).Select(i => (TileBase)TileFor("face_" + i, Tile.ColliderType.Grid)).ToArray();
            set.shadow = TileFor("shadow_0");

            var decor = new List<TileBase>();
            var specials = new List<RegionTiles.NamedTile>();
            foreach (var clip in meta.clipList)
            {
                if (clip.name.StartsWith("decor_"))
                {
                    var tile = AnimatedOrStatic(folder, clip, sprites);
                    if (tile != null)
                        decor.Add(tile);
                }
                else if (clip.name != "floor" && clip.name != "top" && clip.name != "face" && clip.name != "shadow")
                {
                    var frames = clip.frames.Select(n => sprites.TryGetValue(n, out var s) ? s : null).Where(s => s != null).ToArray();
                    specials.Add(new RegionTiles.NamedTile { name = clip.name, tile = AnimatedOrStatic(folder, clip, sprites), frames = frames });
                }
            }
            set.decor = decor.ToArray();
            set.specials = specials.ToArray();
            EditorUtility.SetDirty(set);
            return set;
        }

        [System.Serializable]
        sealed class BlobMasks
        {
            public SpriteSheets.NamedVector[] blobMasksList;
        }

        static TileBase AnimatedOrStatic(string folder, SpriteSheets.ClipMeta clip, Dictionary<string, Sprite> sprites)
        {
            var frames = clip.frames.Select(n => sprites.TryGetValue(n, out var s) ? s : null).Where(s => s != null).ToArray();
            if (frames.Length == 0)
                return null;
            if (frames.Length == 1 || clip.name.StartsWith("decor_crack"))
            {
                // static decor: one tile per frame variant, return the first; others are separate decor
                string path = $"{folder}/{clip.name}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, path);
                }
                tile.sprite = frames[0];
                tile.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                return tile;
            }
            string apath = $"{folder}/{clip.name}_anim.asset";
            var anim = AssetDatabase.LoadAssetAtPath<AnimatedTile>(apath);
            if (anim == null)
            {
                anim = ScriptableObject.CreateInstance<AnimatedTile>();
                AssetDatabase.CreateAsset(anim, apath);
            }
            anim.frames = frames;
            anim.fps = 2f;
            EditorUtility.SetDirty(anim);
            return anim;
        }
    }
}
