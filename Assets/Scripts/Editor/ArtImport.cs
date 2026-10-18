using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Import rules for every texture under Assets/Art (prompt §10 step 6): point filter, no
    /// mipmaps, lossless, 16 pixels per unit. A sheet with a JSON file next to it is sliced into
    /// sprites with the exact cells and pivots the art pipeline wrote.
    /// </summary>
    public sealed class ArtImportRules : AssetPostprocessor
    {
        public const string ArtRoot = "Assets/Art/";
        public const int PixelsPerUnit = 16;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot, StringComparison.Ordinal))
                return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = assetPath.Contains("/Readable/");
            importer.sRGBTexture = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // crisp quads, no tight mesh jitter
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            // A JSON beside the image means a sliced sheet; anything else is one sprite (never let the
            // importer auto-slice a large image into islands).
            importer.spriteImportMode = File.Exists(Path.ChangeExtension(assetPath, ".json"))
                ? SpriteImportMode.Multiple
                : SpriteImportMode.Single;
        }
    }

    /// <summary>Slices the generated sheets using their JSON metadata.</summary>
    public static class SpriteSheets
    {
        [Serializable]
        public sealed class FrameMeta
        {
            public string name;
            public int x, y, w, h;
        }

        [Serializable]
        public sealed class ClipMeta
        {
            public string name;
            public string[] frames;
        }

        [Serializable]
        public sealed class NamedVector
        {
            public string name;
            public float x, y;
        }

        [Serializable]
        public sealed class SheetMeta
        {
            public string sheet;
            public int[] cell;
            public float[] pivot;
            public FrameMeta[] frames;
            public ClipMeta[] clipList;
            public NamedVector[] fpsList;
            public NamedVector[] flameAnchorList;
            public float[] dimFlameAnchor;
            public float[] walkBob;
            public NamedVector[] anchorsList;
        }

        public static SheetMeta ReadMeta(string pngPath)
        {
            string json = Path.ChangeExtension(pngPath, ".json");
            return File.Exists(json) ? JsonUtility.FromJson<SheetMeta>(File.ReadAllText(json)) : null;
        }

        [MenuItem("WICKFALL/Art/Reimport And Slice Generated Art", priority = 0)]
        public static void SliceAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (File.Exists(Path.ChangeExtension(path, ".json")))
                    Slice(path);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Slices one sheet. Sprite ids are derived from the names, so references survive re-slicing.</summary>
        public static void Slice(string pngPath)
        {
            var meta = ReadMeta(pngPath);
            var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (meta == null || importer == null || meta.frames == null)
                return;

            importer.spriteImportMode = SpriteImportMode.Multiple;
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var rects = new List<SpriteRect>(meta.frames.Length);
            foreach (var f in meta.frames)
            {
                rects.Add(new SpriteRect
                {
                    name = f.name,
                    rect = new Rect(f.x, f.y, f.w, f.h),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(meta.pivot[0] / f.w, meta.pivot[1] / f.h),
                    border = Vector4.zero,
                    spriteID = StableGuid(pngPath + "/" + f.name),
                });
            }

            // Skip the reimport when nothing changed (keeps setup runs fast).
            var existing = provider.GetSpriteRects();
            if (existing != null && existing.Length == rects.Count && existing.Zip(rects, Same).All(x => x))
                return;

            provider.SetSpriteRects(rects.ToArray());
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
            provider.Apply();
            importer.SaveAndReimport();
        }

        static bool Same(SpriteRect a, SpriteRect b) =>
            a.name == b.name && a.rect == b.rect && Vector2.Distance(a.pivot, b.pivot) < 0.0001f && a.spriteID == b.spriteID;

        static GUID StableGuid(string text)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
                return new GUID(BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant());
            }
        }

        /// <summary>Every sprite of a sheet by name.</summary>
        public static Dictionary<string, Sprite> Load(string pngPath)
        {
            var map = new Dictionary<string, Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(pngPath))
                if (asset is Sprite sprite)
                    map[sprite.name] = sprite;
            return map;
        }

        public static Sprite[] Clip(string pngPath, string clip)
        {
            var meta = ReadMeta(pngPath);
            var sprites = Load(pngPath);
            var entry = meta?.clipList?.FirstOrDefault(c => c.name == clip);
            if (entry == null)
                return new Sprite[0];
            return entry.frames.Select(n => sprites.TryGetValue(n, out var s) ? s : null).Where(s => s != null).ToArray();
        }

        public static float Fps(SheetMeta meta, string key, float fallback)
        {
            var entry = meta?.fpsList?.FirstOrDefault(e => e.name == key);
            return entry != null ? entry.x : fallback;
        }
    }
}
