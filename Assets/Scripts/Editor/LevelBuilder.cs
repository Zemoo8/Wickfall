using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Wickfall.World;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Turns an ASCII room map (prompt §8: "level layouts are data") into tilemaps:
    /// Ground (floor), Decor, Shadows and Walls (with collisions and 2D shadow casting).
    /// '#' wall, '.' floor, ',' floor with decor, ' ' nothing; any other character is floor plus an
    /// entity marker the caller places.
    /// Cell (x, row) of the map becomes world cell (origin.x + x, origin.y + rows - 1 - row): north is up.
    /// </summary>
    public static class LevelBuilder
    {
        public struct Marker
        {
            public char Symbol;
            public Vector2Int Cell;
            public Vector2 Center;
        }

        public sealed class Result
        {
            public GameObject Root;
            public Tilemap Ground, Decor, Shadows, Walls;
            public readonly List<Marker> Markers = new List<Marker>();
            public int Width, Height;
            public Vector2Int Origin;
        }

        public static bool IsWall(string[] rows, int x, int y)
        {
            if (y < 0 || y >= rows.Length || x < 0 || x >= rows[y].Length)
                return true;
            return rows[y][x] == '#';
        }

        public static bool IsVoid(string[] rows, int x, int y) =>
            y >= 0 && y < rows.Length && x >= 0 && x < rows[y].Length && rows[y][x] == ' ';

        public static int Mask(string[] rows, int x, int y)
        {
            int m = 0;
            if (IsWall(rows, x, y - 1)) m |= RegionTiles.N;
            if (IsWall(rows, x + 1, y - 1)) m |= RegionTiles.NE;
            if (IsWall(rows, x + 1, y)) m |= RegionTiles.E;
            if (IsWall(rows, x + 1, y + 1)) m |= RegionTiles.SE;
            if (IsWall(rows, x, y + 1)) m |= RegionTiles.S;
            if (IsWall(rows, x - 1, y + 1)) m |= RegionTiles.SW;
            if (IsWall(rows, x - 1, y)) m |= RegionTiles.W;
            if (IsWall(rows, x - 1, y - 1)) m |= RegionTiles.NW;
            return m;
        }

        public static Result Build(Transform parent, string name, string[] rows, RegionTiles tiles, Vector2Int origin,
            Material groundMaterial = null, int seed = 1)
        {
            int height = rows.Length;
            int width = 0;
            foreach (var r in rows)
                width = Mathf.Max(width, r.Length);
            for (int i = 0; i < rows.Length; i++)
                rows[i] = rows[i].PadRight(width, ' ');

            var result = new Result { Width = width, Height = height, Origin = origin };
            var root = new GameObject(name);
            if (parent != null)
                root.transform.SetParent(parent, false);
            root.AddComponent<Grid>();
            result.Root = root;

            result.Ground = Map(root, "Ground", Layers.SortGround, 0, groundMaterial);
            result.Decor = Map(root, "Decor", Layers.SortGroundDecal, 0, null);
            result.Shadows = Map(root, "Shadows", Layers.SortGroundDecal, 1, null);
            result.Shadows.color = new Color(1f, 1f, 1f, 0.55f);
            result.Walls = Map(root, "Walls", Layers.SortEntities, 0, null);
            var wallRenderer = result.Walls.GetComponent<TilemapRenderer>();
            wallRenderer.mode = TilemapRenderer.Mode.Individual; // Y-sorted with the characters

            var rnd = new System.Random(seed);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char ch = rows[y][x];
                    var cell = new Vector3Int(origin.x + x, origin.y + height - 1 - y, 0);
                    if (ch == ' ')
                        continue;
                    if (ch == '#')
                    {
                        if (IsVoid(rows, x, y + 1) || y + 1 >= height)
                        {
                            result.Walls.SetTile(cell, tiles.Top(Mask(rows, x, y)));
                            continue;
                        }
                        bool southOpen = !IsWall(rows, x, y + 1);
                        if (southOpen)
                        {
                            bool westOpen = !IsWall(rows, x - 1, y);
                            bool eastOpen = !IsWall(rows, x + 1, y);
                            result.Walls.SetTile(cell, tiles.Face(westOpen, eastOpen, (x + y) & 1));
                            result.Shadows.SetTile(cell + Vector3Int.down, tiles.shadow);
                        }
                        else
                        {
                            result.Walls.SetTile(cell, tiles.Top(Mask(rows, x, y)));
                        }
                        continue;
                    }

                    // floor (variants mixed by a hash so the pattern never repeats visibly)
                    int variant = (int)(((uint)(x * 73856093) ^ (uint)(y * 19349663)) % (uint)tiles.floors.Length);
                    result.Ground.SetTile(cell, tiles.floors[variant]);
                    if (ch == ',' && tiles.decor.Length > 0)
                        result.Decor.SetTile(cell, tiles.decor[rnd.Next(tiles.decor.Length)]);
                    else if (ch == '.' && tiles.decor.Length > 0 && rnd.NextDouble() < 0.025)
                        result.Decor.SetTile(cell, tiles.decor[rnd.Next(tiles.decor.Length)]);

                    if (ch != '.' && ch != ',')
                    {
                        result.Markers.Add(new Marker
                        {
                            Symbol = ch,
                            Cell = new Vector2Int(cell.x, cell.y),
                            Center = new Vector2(cell.x + 0.5f, cell.y + 0.5f),
                        });
                    }
                }
            }

            SetupWallPhysics(result.Walls.gameObject);
            return result;
        }

        static Tilemap Map(GameObject root, string name, string sortingLayer, int order, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var map = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            renderer.sharedMaterial = material != null ? material : EditorAssets.LitDefault;
            return map;
        }

        /// <summary>Walls collide (merged into one composite shape) and cast 2D shadows from that shape.</summary>
        public static void SetupWallPhysics(GameObject walls)
        {
            EditorAssets.SetLayer(walls, Layers.Wall);
            var body = walls.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            // The composite must come before the tilemap collider: the shadow caster uses the first collider it finds.
            var composite = walls.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
            var tilemapCollider = walls.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            composite.GenerateGeometry();
            var caster = walls.AddComponent<ShadowCaster2D>();
            caster.castingOption = ShadowCaster2D.ShadowCastingOptions.CastShadow;
        }

        /// <summary>Reads a level file: lines after "@map" until the next "@" section.</summary>
        public static string[] ReadMap(string text, string section = "map")
        {
            var rows = new List<string>();
            bool inside = false;
            foreach (var raw in text.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (line.StartsWith("@"))
                {
                    inside = line.Trim() == "@" + section;
                    continue;
                }
                if (inside)
                    rows.Add(line);
            }
            while (rows.Count > 0 && rows[rows.Count - 1].Trim().Length == 0)
                rows.RemoveAt(rows.Count - 1);
            return rows.ToArray();
        }
    }
}
