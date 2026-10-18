using UnityEngine;

namespace Wickfall.World
{
    /// <summary>
    /// A* on the region's tile grid, for creatures and bots. All arrays are allocated once per region,
    /// so a path search never allocates (PDF §4: zero allocation). 8-way moves without cutting corners.
    /// Closed gates and broken glass mark cells blocked or open at runtime.
    /// </summary>
    public sealed class NavGrid
    {
        public static NavGrid Active { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Active = null;

        readonly int width, height;
        readonly bool[] walkable;
        readonly bool[] blocked;
        readonly float[] g;
        readonly int[] parent;
        readonly int[] stamp;
        readonly int[] heapIndex;
        readonly int[] heap;
        readonly float[] f;
        int search;
        int heapCount;

        public int Width => width;
        public int Height => height;

        public NavGrid(LevelData level)
        {
            width = level.Width;
            height = level.Height;
            int n = width * height;
            walkable = new bool[n];
            blocked = new bool[n];
            g = new float[n];
            f = new float[n];
            parent = new int[n];
            stamp = new int[n];
            heapIndex = new int[n];
            heap = new int[n];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    walkable[y * width + x] = level.IsWalkable(x, y);
        }

        public void MakeActive() => Active = this;

        public void Clear()
        {
            if (Active == this)
                Active = null;
        }

        bool Inside(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

        public bool IsWalkable(int x, int y) => Inside(x, y) && walkable[y * width + x] && !blocked[y * width + x];
        public bool IsWalkable(Vector2Int c) => IsWalkable(c.x, c.y);

        public void SetBlocked(Vector2Int cell, bool value)
        {
            if (Inside(cell.x, cell.y))
                blocked[cell.y * width + cell.x] = value;
        }

        public void SetWalkable(Vector2Int cell, bool value)
        {
            if (Inside(cell.x, cell.y))
                walkable[cell.y * width + cell.x] = value;
        }

        static readonly Collider2D[] PropBuffer = new Collider2D[8];

        /// <summary>
        /// Marks walkable cells in `cells` unwalkable where a solid (non-trigger) collider on `mask` stands
        /// in the cell's middle. Gates are skipped: they open and close their cells themselves.
        /// </summary>
        public int BlockSolidProps(RectInt cells, int mask)
        {
            int blockedCount = 0;
            var size = new Vector2(0.6f, 0.6f);
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = mask };
            for (int y = Mathf.Max(0, cells.yMin); y < Mathf.Min(height, cells.yMax); y++)
            {
                for (int x = Mathf.Max(0, cells.xMin); x < Mathf.Min(width, cells.xMax); x++)
                {
                    if (!walkable[y * width + x])
                        continue;
                    int count = Physics2D.OverlapBox(new Vector2(x + 0.5f, y + 0.5f), size, 0f, filter, PropBuffer);
                    for (int i = 0; i < count; i++)
                    {
                        var c = PropBuffer[i];
                        PropBuffer[i] = null;
                        if (c == null || c.isTrigger || c.GetComponentInParent<Gate>() != null || c.GetComponent<CompositeCollider2D>() != null)
                            continue;
                        walkable[y * width + x] = false;
                        blockedCount++;
                        break;
                    }
                }
            }
            return blockedCount;
        }

        /// <summary>The walkable cell nearest to a point (searching outwards a few rings).</summary>
        public bool TryNearestWalkable(Vector2 world, out Vector2Int cell, int maxRing = 4)
        {
            cell = LevelData.CellOf(world);
            if (IsWalkable(cell))
                return true;
            for (int ring = 1; ring <= maxRing; ring++)
            {
                for (int dy = -ring; dy <= ring; dy++)
                {
                    for (int dx = -ring; dx <= ring; dx++)
                    {
                        if (Mathf.Abs(dx) != ring && Mathf.Abs(dy) != ring)
                            continue;
                        var c = new Vector2Int(cell.x + dx, cell.y + dy);
                        if (IsWalkable(c))
                        {
                            cell = c;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Finds a path and writes world points (cell centres) into <paramref name="output"/>, start excluded.
        /// Returns the number of points (0 = no path). Searches at most <paramref name="maxNodes"/> cells.
        /// </summary>
        public int FindPath(Vector2 from, Vector2 to, Vector2[] output, int maxNodes = 4000)
        {
            if (!TryNearestWalkable(from, out var start) || !TryNearestWalkable(to, out var goal))
                return 0;
            int startIndex = start.y * width + start.x;
            int goalIndex = goal.y * width + goal.x;
            if (startIndex == goalIndex)
            {
                if (output.Length > 0) output[0] = LevelData.CellCenter(goal);
                return 1;
            }

            search++;
            heapCount = 0;
            Open(startIndex, 0f, Heuristic(start, goal), -1);
            int expanded = 0;
            int found = -1;
            while (heapCount > 0 && expanded < maxNodes)
            {
                int current = Pop();
                expanded++;
                if (current == goalIndex)
                {
                    found = current;
                    break;
                }
                int cx = current % width, cy = current / width;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;
                        int nx = cx + dx, ny = cy + dy;
                        if (!IsWalkable(nx, ny))
                            continue;
                        if (dx != 0 && dy != 0 && (!IsWalkable(cx + dx, cy) || !IsWalkable(cx, cy + dy)))
                            continue; // no corner cutting
                        int next = ny * width + nx;
                        float cost = g[current] + (dx != 0 && dy != 0 ? 1.4142f : 1f);
                        if (stamp[next] == search && cost >= g[next])
                            continue;
                        Open(next, cost, cost + Heuristic(new Vector2Int(nx, ny), goal), current);
                    }
                }
            }
            if (found < 0)
                return 0;

            // Count the steps, then fill the output from the end (start excluded). A short buffer keeps
            // the first steps, which is all a mover needs before it searches again.
            int length = 0;
            for (int i = found; i != startIndex && i >= 0; i = parent[i])
                length++;
            int index = length - 1;
            for (int i = found; i != startIndex && i >= 0; i = parent[i])
            {
                if (index < output.Length)
                    output[index] = new Vector2(i % width + 0.5f, i / width + 0.5f);
                index--;
            }
            return Mathf.Min(length, output.Length);
        }

        static float Heuristic(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
            return (dx + dy) + (1.4142f - 2f) * Mathf.Min(dx, dy);
        }

        // ---- binary heap on f -----------------------------------------------------------------

        void Open(int node, float cost, float total, int from)
        {
            bool known = stamp[node] == search;
            stamp[node] = search;
            g[node] = cost;
            f[node] = total;
            parent[node] = from;
            if (known && heapIndex[node] >= 0)
            {
                Up(heapIndex[node]);
                return;
            }
            heap[heapCount] = node;
            heapIndex[node] = heapCount;
            Up(heapCount);
            heapCount++;
        }

        int Pop()
        {
            int top = heap[0];
            heapIndex[top] = -1;
            heapCount--;
            if (heapCount > 0)
            {
                heap[0] = heap[heapCount];
                heapIndex[heap[0]] = 0;
                Down(0);
            }
            return top;
        }

        void Up(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (f[heap[p]] <= f[heap[i]])
                    break;
                Swap(i, p);
                i = p;
            }
        }

        void Down(int i)
        {
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, best = i;
                if (l < heapCount && f[heap[l]] < f[heap[best]]) best = l;
                if (r < heapCount && f[heap[r]] < f[heap[best]]) best = r;
                if (best == i)
                    return;
                Swap(i, best);
                i = best;
            }
        }

        void Swap(int a, int b)
        {
            int t = heap[a];
            heap[a] = heap[b];
            heap[b] = t;
            heapIndex[heap[a]] = a;
            heapIndex[heap[b]] = b;
        }
    }
}
