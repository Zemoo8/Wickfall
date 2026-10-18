using NUnit.Framework;
using UnityEngine;
using Wickfall.World;

namespace Wickfall.Tests
{
    public class NavGridTests
    {
        const string Map = @"@legend
m = Dripling
@map
##########
#........#
#.######.#
#.#....#.#
#.#.m..#.#
#...####.#
##########
@groups
A 0 0 10 7
";

        [Test]
        public void Level_ParsesLegendGroupsAndCells()
        {
            var level = LevelData.Parse(Map);
            Assert.AreEqual(10, level.Width);
            Assert.AreEqual(7, level.Height);
            Assert.AreEqual("Dripling", level.KindAt(4, 2));
            Assert.IsTrue(level.IsWall(0, 0));
            Assert.IsTrue(level.IsWalkable(1, 1));
            Assert.AreEqual(1, level.Groups.Count);
        }

        [Test]
        public void Path_GoesAroundWalls()
        {
            var level = LevelData.Parse(Map);
            var nav = new NavGrid(level);
            var path = new Vector2[64];
            // from inside the pocket (4,2) out to the top-right corridor (8,5)
            int n = nav.FindPath(new Vector2(4.5f, 2.5f), new Vector2(8.5f, 5.5f), path);
            Assert.Greater(n, 6);
            Assert.AreEqual(new Vector2(8.5f, 5.5f), path[n - 1]);
            for (int i = 0; i < n; i++)
                Assert.IsTrue(nav.IsWalkable(LevelData.CellOf(path[i])), "every step is walkable");
        }

        [Test]
        public void Path_BlockedCellsAreRespected()
        {
            var level = LevelData.Parse(Map);
            var nav = new NavGrid(level);
            var path = new Vector2[64];
            nav.SetBlocked(new Vector2Int(3, 1), true); // close the only way out of the pocket
            Assert.AreEqual(0, nav.FindPath(new Vector2(4.5f, 2.5f), new Vector2(8.5f, 5.5f), path));
            nav.SetBlocked(new Vector2Int(3, 1), false);
            Assert.Greater(nav.FindPath(new Vector2(4.5f, 2.5f), new Vector2(8.5f, 5.5f), path), 0);
        }
    }
}
