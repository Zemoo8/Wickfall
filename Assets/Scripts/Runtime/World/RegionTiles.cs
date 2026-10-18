using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Wickfall.World
{
    /// <summary>
    /// A region's tileset (PDF §3 "One tileset per region, seamless 16x16 tiles... wall tiles and
    /// corner tiles"), wired by WICKFALL > Setup from the generated sheet. The level builder picks
    /// tiles from here; swapping art means regenerating the sheet, never touching code.
    /// </summary>
    [CreateAssetMenu(menuName = "WICKFALL/Visuals/Region Tiles", fileName = "RegionTiles")]
    public sealed class RegionTiles : ScriptableObject
    {
        [Serializable]
        public struct MaskTile
        {
            public int mask;
            public TileBase tile;
        }

        public string key;
        public TileBase[] floors = new TileBase[0];
        public MaskTile[] tops = new MaskTile[0];
        [Tooltip("Index = (westOpen ? 4 : 0) + (eastOpen ? 2 : 0) + variant.")]
        public TileBase[] faces = new TileBase[8];
        public TileBase shadow;
        public TileBase[] decor = new TileBase[0];
        [Tooltip("Named extra tiles (geyser vents, vines, conveyors...).")]
        public NamedTile[] specials = new NamedTile[0];

        [Serializable]
        public struct NamedTile
        {
            public string name;
            public TileBase tile;
            public Sprite[] frames;
        }

        // 8-neighbour bits, the same as Tools/art/tiles.py.
        public const int N = 1, NE = 2, E = 4, SE = 8, S = 16, SW = 32, W = 64, NW = 128;

        public static int Reduce(int m)
        {
            if ((m & N) == 0 || (m & E) == 0) m &= ~NE;
            if ((m & E) == 0 || (m & S) == 0) m &= ~SE;
            if ((m & S) == 0 || (m & W) == 0) m &= ~SW;
            if ((m & W) == 0 || (m & N) == 0) m &= ~NW;
            return m;
        }

        public TileBase Top(int mask)
        {
            int reduced = Reduce(mask);
            for (int i = 0; i < tops.Length; i++)
                if (tops[i].mask == reduced)
                    return tops[i].tile;
            return tops.Length > 0 ? tops[0].tile : null;
        }

        public TileBase Face(bool westOpen, bool eastOpen, int variant) =>
            faces[(westOpen ? 4 : 0) + (eastOpen ? 2 : 0) + (variant & 1)];

        public NamedTile Special(string name)
        {
            for (int i = 0; i < specials.Length; i++)
                if (specials[i].name == name)
                    return specials[i];
            return default;
        }
    }
}
