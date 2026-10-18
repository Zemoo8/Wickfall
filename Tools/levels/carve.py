"""
Level carving helpers: start from solid wall, carve rooms and corridors, place entities, then
write the ASCII level file the Unity builder reads (Assets/Resources/Levels/*.txt) and a preview.
Coordinates here are text coordinates: x to the right, y DOWN (row 0 is the north edge).
"""
from __future__ import annotations

import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "Tools", "art"))


class Level:
    def __init__(self, name, width, height, seed=1):
        self.name = name
        self.w, self.h = width, height
        self.grid = [["#"] * width for _ in range(height)]
        self.canopy = [[" "] * width for _ in range(height)]
        self.legend = {}
        self.groups = []
        self.bot = []
        self.properties = {}
        self.rnd = random.Random(seed)

    # ---- carving -------------------------------------------------------------------------------
    def inside(self, x, y):
        return 1 <= x < self.w - 1 and 1 <= y < self.h - 1

    def floor(self, x, y, ch="."):
        if self.inside(x, y) and self.grid[y][x] in "#.,":
            self.grid[y][x] = ch

    def rect(self, x0, y0, x1, y1, ch="."):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.floor(x, y, ch)

    def wall_rect(self, x0, y0, x1, y1):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if self.inside(x, y):
                    self.grid[y][x] = "#"

    def ellipse(self, cx, cy, rx, ry, rough=0.0, ch="."):
        for y in range(int(cy - ry - 2), int(cy + ry + 3)):
            for x in range(int(cx - rx - 2), int(cx + rx + 3)):
                a = math.atan2(y - cy, x - cx)
                wobble = 1 + rough * (math.sin(a * 3 + cx) * 0.5 + math.sin(a * 5 + cy) * 0.5)
                if ((x - cx) / (rx * wobble)) ** 2 + ((y - cy) / (ry * wobble)) ** 2 <= 1:
                    self.floor(x, y, ch)

    def corridor(self, points, width=3):
        """A path through points (text coords), `width` cells wide."""
        half = width // 2
        for (x0, y0), (x1, y1) in zip(points, points[1:]):
            steps = max(abs(x1 - x0), abs(y1 - y0))
            for i in range(steps + 1):
                t = i / max(1, steps)
                x = round(x0 + (x1 - x0) * t)
                y = round(y0 + (y1 - y0) * t)
                for dy in range(-half, width - half):
                    for dx in range(-half, width - half):
                        self.floor(x + dx, y + dy)

    def pillar(self, x, y, w=1, h=1):
        self.wall_rect(x, y, x + w - 1, y + h - 1)

    def decorate(self, x0, y0, x1, y1, chance=0.08):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if self.inside(x, y) and self.grid[y][x] == "." and self.rnd.random() < chance:
                    self.grid[y][x] = ","

    def canopy_ellipse(self, cx, cy, rx, ry, rough=0.25):
        """Marks overhead canopy over floor cells (the level file's @canopy layer)."""
        for y in range(int(cy - ry - 2), int(cy + ry + 3)):
            for x in range(int(cx - rx - 2), int(cx + rx + 3)):
                if not (0 <= x < self.w and 0 <= y < self.h):
                    continue
                a = math.atan2(y - cy, x - cx)
                wobble = 1 + rough * (math.sin(a * 4 + cx) * 0.5 + math.sin(a * 7 + cy) * 0.5)
                if ((x - cx) / (rx * wobble)) ** 2 + ((y - cy) / (ry * wobble)) ** 2 <= 1:
                    self.canopy[y][x] = "c"

    def put(self, x, y, symbol, kind=None):
        """Places an entity symbol on a floor cell (carving it if needed)."""
        if not self.inside(x, y):
            raise ValueError(f"{self.name}: {symbol} at {x},{y} is outside")
        self.grid[y][x] = symbol
        if kind:
            self.legend[symbol] = kind

    def inscription(self, x, y, n):
        """Inscription number n (its text is inscription.rR.n): the digit is the symbol, so the order is explicit."""
        self.put(x, y, str(n), f"Inscription{n}")

    def group(self, name, x0, x1):
        """A room group spanning columns x0..x1 (all rows). Stored in world cells (y up)."""
        self.groups.append((name, x0, 0, x1 - x0 + 1, self.h))

    # ---- checks and output ---------------------------------------------------------------------
    def check_connected(self, start):
        """Every non-wall cell must be reachable from the start (gates count as walkable)."""
        sx, sy = start
        seen = {(sx, sy)}
        stack = [(sx, sy)]
        while stack:
            x, y = stack.pop()
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < self.w and 0 <= ny < self.h and (nx, ny) not in seen and self.grid[ny][nx] not in "# K":
                    seen.add((nx, ny))
                    stack.append((nx, ny))
        lost = [(x, y) for y in range(self.h) for x in range(self.w)
                if self.grid[y][x] not in "# K" and (x, y) not in seen]
        return lost

    def check_connected_with(self, start, passable=""):
        """Like check_connected, but some symbols (vines) count as passable."""
        sx, sy = start
        seen = {(sx, sy)}
        stack = [(sx, sy)]
        blocked = set("# K") - set(passable)
        while stack:
            x, y = stack.pop()
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < self.w and 0 <= ny < self.h and (nx, ny) not in seen and self.grid[ny][nx] not in blocked:
                    seen.add((nx, ny))
                    stack.append((nx, ny))
        return [(x, y) for y in range(self.h) for x in range(self.w)
                if self.grid[y][x] not in "# K" and (x, y) not in seen]

    def text(self, header=""):
        lines = [f"# WICKFALL level: {self.name}", "# Generated by Tools/levels; edit the script, not this file.", header, "@legend"]
        for sym, kind in sorted(self.legend.items()):
            lines.append(f"{sym} = {kind}")
        if self.properties:
            lines.append("@properties")
            for k, v in self.properties.items():
                lines.append(f"{k} = {v}")
        lines.append("@map")
        lines += ["".join(row) for row in self.grid]
        cells = self.canopy_cells()
        if cells:
            lines.append("@canopy")
            lines += cells
        lines.append("@groups")
        for name, x, y, w, h in self.groups:
            lines.append(f"{name} {x} {y} {w} {h}")
        if self.bot:
            lines.append("@bot")
            lines += self.bot
        return "\n".join(lines) + "\n"

    def save(self, filename, header=""):
        out = os.path.join(ROOT, "Assets", "Resources", "Levels", filename)
        os.makedirs(os.path.dirname(out), exist_ok=True)
        with open(out, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(self.text(header))
        cells = self.canopy_cells()
        if cells:
            import canopy
            stem = os.path.splitext(filename)[0]
            canopy.render(cells, os.path.join(ROOT, "Assets", "Art", "Levels", stem + "_Canopy.png"), width=self.w)
        return out

    def canopy_cells(self):
        """The @canopy rows (leaves also hang over tree trunks and wall edges), or [] if there is none."""
        rows = ["".join(("c" if self.canopy[y][x] == "c" else " ") for x in range(self.w))
                for y in range(self.h)]
        return [r.rstrip() for r in rows] if any("c" in r for r in rows) else []

    def preview(self, style, face_detail, decor_tiles, filename, colors):
        """Renders the map with the region's tiles plus coloured entity markers."""
        import tiles
        from PIL import Image, ImageDraw
        grid = ["".join(c if c in "#., " else "." for c in row) for row in self.grid]
        img = tiles.render_map(style, grid, face_detail, decor_tiles)
        draw = ImageDraw.Draw(img, "RGBA")
        for y, row in enumerate(self.canopy):
            for x, c in enumerate(row):
                if c == "c" and self.grid[y][x] != "#":
                    draw.rectangle([x * 16, y * 16, x * 16 + 15, y * 16 + 15], fill=(10, 30, 10, 120))
        for y, row in enumerate(self.grid):
            for x, c in enumerate(row):
                if c in colors or c.isdigit():
                    col = colors.get(c, (180, 180, 255))  # digits are numbered inscriptions
                    draw.rectangle([x * 16 + 3, y * 16 + 3, x * 16 + 12, y * 16 + 12], fill=col, outline=(14, 13, 12))
        small = img.resize((img.width // 2, img.height // 2), Image.NEAREST)
        path = os.path.join(ROOT, "ArtSource", "Previews", filename)
        small.save(path)
        return path
