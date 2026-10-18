using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wickfall.World
{
    /// <summary>
    /// A region's layout, read from its text file (prompt §8: "level layouts are data"). The same
    /// file builds the scenes in the editor and the navigation grid at runtime.
    /// Sections: @legend (symbol = entity), @map (ASCII rows), @groups (room groups: name x y w h),
    /// @bot (waypoints for the region bot), @text (inscription keys by symbol position order).
    /// Map row 0 is the northernmost row; world cell = (x, height - 1 - row).
    /// </summary>
    public sealed class LevelData
    {
        public struct Group
        {
            public string Name;
            public RectInt Cells; // in world cells
        }

        public string[] Rows = new string[0];
        public int Width;
        public int Height;
        public readonly Dictionary<char, string> Legend = new Dictionary<char, string>();
        public readonly List<Group> Groups = new List<Group>();
        public readonly List<string> BotScript = new List<string>();
        public readonly Dictionary<string, string> Properties = new Dictionary<string, string>();

        public static LevelData Parse(string text)
        {
            var data = new LevelData();
            var rows = new List<string>();
            string section = "";
            foreach (var raw in text.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (line.StartsWith("@"))
                {
                    section = line.Trim().Substring(1);
                    continue;
                }
                if (section != "map" && (line.TrimStart().StartsWith("#") || line.Trim().Length == 0))
                    continue;
                switch (section)
                {
                    case "map":
                        rows.Add(line);
                        break;
                    case "legend":
                    {
                        int eq = line.IndexOf('=');
                        if (eq > 0)
                        {
                            string key = line.Substring(0, eq).Trim();
                            if (key.Length == 1)
                                data.Legend[key[0]] = line.Substring(eq + 1).Trim();
                        }
                        break;
                    }
                    case "groups":
                    {
                        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5)
                            data.Groups.Add(new Group
                            {
                                Name = parts[0],
                                Cells = new RectInt(int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]), int.Parse(parts[4])),
                            });
                        break;
                    }
                    case "bot":
                        data.BotScript.Add(line.Trim());
                        break;
                    case "properties":
                    {
                        int eq = line.IndexOf('=');
                        if (eq > 0)
                            data.Properties[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                        break;
                    }
                }
            }
            while (rows.Count > 0 && rows[rows.Count - 1].Trim().Length == 0)
                rows.RemoveAt(rows.Count - 1);
            data.Height = rows.Count;
            foreach (var r in rows)
                data.Width = Mathf.Max(data.Width, r.Length);
            for (int i = 0; i < rows.Count; i++)
                rows[i] = rows[i].PadRight(data.Width, ' ');
            data.Rows = rows.ToArray();
            return data;
        }

        /// <summary>Character at a world cell (' ' outside the map).</summary>
        public char At(int cellX, int cellY)
        {
            int row = Height - 1 - cellY;
            if (row < 0 || row >= Height || cellX < 0 || cellX >= Width)
                return ' ';
            return Rows[row][cellX];
        }

        public bool IsWall(int cellX, int cellY) => At(cellX, cellY) == '#';
        public bool IsVoid(int cellX, int cellY) => At(cellX, cellY) == ' ';

        /// <summary>Walkable for navigation: not a wall, not the void, not a solid prop symbol.</summary>
        public bool IsWalkable(int cellX, int cellY)
        {
            char c = At(cellX, cellY);
            if (c == '#' || c == ' ')
                return false;
            return !Legend.TryGetValue(c, out string kind) || !SolidKinds.Contains(kind);
        }

        /// <summary>Entity kinds that block movement (navigation treats their cell as a wall).</summary>
        public static readonly HashSet<string> SolidKinds = new HashSet<string> { "BrokenWick", "Pillar", "Pedestal", "Statue" };

        public string KindAt(int cellX, int cellY) => Legend.TryGetValue(At(cellX, cellY), out string kind) ? kind : null;

        /// <summary>Every cell whose symbol maps to an entity kind, in reading order (north to south, west to east).</summary>
        public IEnumerable<(Vector2Int cell, string kind, char symbol)> Entities()
        {
            for (int row = 0; row < Height; row++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c = Rows[row][x];
                    if (Legend.TryGetValue(c, out string kind))
                        yield return (new Vector2Int(x, Height - 1 - row), kind, c);
                }
            }
        }

        public string GroupAt(Vector2Int cell)
        {
            foreach (var g in Groups)
                if (g.Cells.Contains(cell))
                    return g.Name;
            return Groups.Count > 0 ? Groups[0].Name : "";
        }

        public static Vector2 CellCenter(Vector2Int cell) => new Vector2(cell.x + 0.5f, cell.y + 0.5f);
        public static Vector2Int CellOf(Vector2 world) => new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
    }
}
