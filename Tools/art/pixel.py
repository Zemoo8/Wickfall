"""
WICKFALL pixel-art toolkit (prompt §10: the Python/Pillow pipeline that replaces Aseprite).

Every sprite is drawn on an exact low-resolution grid with the locked 27-colour palette
(STYLE LOCK, prompt §10.5). Drawing works on *material* maps: shapes are filled with a material
(wax, ochre, charcoal...), then `shade()` turns each material into its 3-4 tone ramp with light
from the top-left, and `outline()` adds the clean dark 1-pixel outline.
"""
from __future__ import annotations

import json
import math
import os
from dataclasses import dataclass, field

import numpy as np
from PIL import Image

# ---------------------------------------------------------------------------------------------
# The locked palette (STYLE LOCK). Index 0 is transparent.
# ---------------------------------------------------------------------------------------------

def _hex(h: str) -> tuple[int, int, int, int]:
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


PALETTE_HEX = {
    # charcoal blacks
    "k0": "#0E0D0C", "k1": "#1B1917", "k2": "#2A2724", "k3": "#3C3833",
    # bone whites (darkest -> lightest)
    "b0": "#7A7266", "b1": "#A39A8B", "b2": "#CEC5B2", "b3": "#EDE6D4",
    # desaturated ochres
    "o0": "#4A3D2A", "o1": "#6F5D40", "o2": "#978059", "o3": "#BFA87C",
    # amber (the only saturated colours: light sources)
    "a3": "#FFE9A8", "a2": "#FFC14D", "a1": "#F28A1D", "a0": "#B8500F",
    # blue, green, white, crimson flames
    "u2": "#E0F7FF", "u1": "#8EDBFF", "u0": "#3A9BE8",
    "g2": "#E6FFD0", "g1": "#9BEA6A", "g0": "#46A53E",
    "w1": "#FFFFFF", "w0": "#F4F0DE",
    "r2": "#FFD0C8", "r1": "#FF5A4A", "r0": "#C21A33",
}

NAMES = ["__"] + list(PALETTE_HEX.keys())
INDEX = {name: i for i, name in enumerate(NAMES)}
RGBA = [(0, 0, 0, 0)] + [_hex(PALETTE_HEX[n]) for n in NAMES[1:]]
# Grey levels for flame layers (recoloured in the engine by Wickfall/FlameSprite).
FLAME_GREYS = {"fe": (77, 77, 77, 255), "fb": (166, 166, 166, 255), "fc": (255, 255, 255, 255)}
for _n, _c in FLAME_GREYS.items():
    INDEX[_n] = len(NAMES)
    NAMES.append(_n)
    RGBA.append(_c)

T = 0  # transparent


def c(name: str) -> int:
    return INDEX[name]


# Material ramps, dark -> light. shade() picks a tone per pixel.
RAMPS = {
    "wax": ["b0", "b1", "b2", "b3"],          # the Wickbearer's pale cream wax
    "oldwax": ["o1", "b0", "b1", "b2"],       # dull dirty wax (Driplings)
    "ochre": ["o0", "o1", "o2", "o3"],
    "rust": ["k2", "o0", "o1", "o2"],
    "char": ["k0", "k1", "k2", "k3"],
    "stone": ["k1", "k2", "k3", "b0"],
    "bone": ["b0", "b1", "b2", "b3"],
    "ash": ["k3", "b0", "b1", "b2"],
    "glass": ["k0", "k1", "k2", "b0"],
    "soil": ["k1", "k2", "o0", "o1"],
    "amber": ["a0", "a1", "a2", "a3"],
    "blue": ["u0", "u0", "u1", "u2"],
    "green": ["g0", "g0", "g1", "g2"],
    "white": ["w0", "w0", "w1", "w1"],
    "crimson": ["r0", "r0", "r1", "r2"],
}

MATERIALS = {name: i + 1 for i, name in enumerate(RAMPS.keys())}
MATERIAL_NAMES = {v: k for k, v in MATERIALS.items()}


# ---------------------------------------------------------------------------------------------
# Canvas
# ---------------------------------------------------------------------------------------------

class Canvas:
    """A small indexed image: `px` holds palette indices, `mat` holds material ids (for shading)."""

    def __init__(self, w: int, h: int):
        self.w, self.h = w, h
        self.px = np.zeros((h, w), dtype=np.uint8)
        self.mat = np.zeros((h, w), dtype=np.uint8)
        self.tone = np.full((h, w), -1, dtype=np.int8)  # forced tone offsets (-1 = auto)
        self.lock = np.zeros((h, w), dtype=bool)        # pixels shade()/outline() must not touch

    # -- low level --------------------------------------------------------------------------
    def inside(self, x: int, y: int) -> bool:
        return 0 <= x < self.w and 0 <= y < self.h

    def set(self, x: int, y: int, colour: str | int, lock: bool = True):
        x, y = int(round(x)), int(round(y))
        if self.inside(x, y):
            self.px[y, x] = colour if isinstance(colour, (int, np.integer)) else c(colour)
            self.mat[y, x] = 0
            self.lock[y, x] = lock

    def get(self, x: int, y: int) -> int:
        return int(self.px[y, x]) if self.inside(x, y) else T

    def mset(self, x: int, y: int, material: str, tone: int = -1):
        x, y = int(round(x)), int(round(y))
        if self.inside(x, y) and not self.lock[y, x]:
            self.mat[y, x] = MATERIALS[material]
            self.tone[y, x] = tone
            self.px[y, x] = c(RAMPS[material][2])  # placeholder until shade()

    def erase(self, x: int, y: int):
        x, y = int(round(x)), int(round(y))
        if self.inside(x, y):
            self.px[y, x] = T
            self.mat[y, x] = 0
            self.tone[y, x] = -1
            self.lock[y, x] = False

    # -- shapes (material fills) --------------------------------------------------------------
    def rect(self, x0, y0, x1, y1, material, tone=-1):
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                self.mset(x, y, material, tone)

    def ellipse(self, cx, cy, rx, ry, material, tone=-1):
        for y in range(int(math.floor(cy - ry)), int(math.ceil(cy + ry)) + 1):
            for x in range(int(math.floor(cx - rx)), int(math.ceil(cx + rx)) + 1):
                dx = (x - cx) / max(rx, 0.01)
                dy = (y - cy) / max(ry, 0.01)
                if dx * dx + dy * dy <= 1.0:
                    self.mset(x, y, material, tone)

    def poly(self, points, material, tone=-1):
        xs = [p[0] for p in points]
        ys = [p[1] for p in points]
        for y in range(int(min(ys)), int(max(ys)) + 1):
            for x in range(int(min(xs)), int(max(xs)) + 1):
                if _point_in_poly(x + 0.5, y + 0.5, points):
                    self.mset(x, y, material, tone)

    def line(self, x0, y0, x1, y1, colour, lock=True):
        for x, y in _bresenham(int(round(x0)), int(round(y0)), int(round(x1)), int(round(y1))):
            self.set(x, y, colour, lock)

    def mline(self, x0, y0, x1, y1, material, tone=-1):
        for x, y in _bresenham(int(round(x0)), int(round(y0)), int(round(x1)), int(round(y1))):
            self.mset(x, y, material, tone)

    # -- finishing ----------------------------------------------------------------------------
    def shade(self, light=(-1, -1), depth=2):
        """Bevel shading per material: the side facing the light gets lighter tones, the far side darker."""
        lx, ly = light
        filled = self.mat > 0
        h, w = self.h, self.w
        for y in range(h):
            for x in range(w):
                m = self.mat[y, x]
                if m == 0 or self.lock[y, x]:
                    continue
                ramp = RAMPS[MATERIAL_NAMES[m]]
                forced = self.tone[y, x]
                if forced >= 0:
                    self.px[y, x] = c(ramp[min(forced, len(ramp) - 1)])
                    continue
                tone = 2
                # Toward the light: an edge within `depth` pixels means a highlight.
                for d in range(1, depth + 1):
                    sx, sy = x + lx * d, y + ly * d
                    if not (0 <= sx < w and 0 <= sy < h) or self.mat[sy, sx] != m:
                        tone = 3 if d == 1 else max(tone, 3 if (x + y) % 2 == 0 else 2)
                        break
                # Away from the light: shadow.
                for d in range(1, depth + 1):
                    sx, sy = x - lx * d, y - ly * d
                    if not (0 <= sx < w and 0 <= sy < h) or self.mat[sy, sx] != m:
                        tone = 0 if d == 1 else min(tone, 1)
                        break
                self.px[y, x] = c(ramp[tone])

    def outline(self, colour="k0", diagonal=False, skip=None):
        """Adds a 1-pixel outline outside the silhouette."""
        src = self.px.copy()
        h, w = self.h, self.w
        offsets = [(1, 0), (-1, 0), (0, 1), (0, -1)]
        if diagonal:
            offsets += [(1, 1), (1, -1), (-1, 1), (-1, -1)]
        skip_idx = {c(s) for s in (skip or [])}
        for y in range(h):
            for x in range(w):
                if src[y, x] != T:
                    continue
                for dx, dy in offsets:
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h and src[ny, nx] != T and src[ny, nx] not in skip_idx:
                        self.px[y, x] = c(colour)
                        self.lock[y, x] = True
                        break

    def recolour(self, mapping: dict[str, str]):
        for a, b in mapping.items():
            self.px[self.px == c(a)] = c(b)

    def mirrored(self) -> "Canvas":
        out = Canvas(self.w, self.h)
        out.px = self.px[:, ::-1].copy()
        out.mat = self.mat[:, ::-1].copy()
        out.tone = self.tone[:, ::-1].copy()
        out.lock = self.lock[:, ::-1].copy()
        return out

    def shifted(self, dx: int, dy: int) -> "Canvas":
        out = Canvas(self.w, self.h)
        for y in range(self.h):
            for x in range(self.w):
                if self.px[y, x] != T:
                    nx, ny = x + dx, y + dy
                    if out.inside(nx, ny):
                        out.px[ny, nx] = self.px[y, x]
        return out

    def paste(self, other: "Canvas", ox: int, oy: int):
        for y in range(other.h):
            for x in range(other.w):
                v = other.px[y, x]
                if v != T and self.inside(x + ox, y + oy):
                    self.px[y + oy, x + ox] = v
                    self.lock[y + oy, x + ox] = True

    def copy(self) -> "Canvas":
        out = Canvas(self.w, self.h)
        out.px, out.mat, out.tone, out.lock = self.px.copy(), self.mat.copy(), self.tone.copy(), self.lock.copy()
        return out

    def image(self) -> Image.Image:
        rgba = np.array(RGBA, dtype=np.uint8)[self.px]
        return Image.fromarray(rgba, "RGBA")

    def bbox(self):
        ys, xs = np.nonzero(self.px)
        if len(xs) == 0:
            return None
        return int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())


def _bresenham(x0, y0, x1, y1):
    dx, dy = abs(x1 - x0), -abs(y1 - y0)
    sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
    err = dx + dy
    while True:
        yield x0, y0
        if x0 == x1 and y0 == y1:
            return
        e2 = 2 * err
        if e2 >= dy:
            err += dy
            x0 += sx
        if e2 <= dx:
            err += dx
            y0 += sy


def _point_in_poly(x, y, pts):
    inside = False
    j = len(pts) - 1
    for i in range(len(pts)):
        xi, yi = pts[i]
        xj, yj = pts[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / ((yj - yi) or 1e-9) + xi:
            inside = not inside
        j = i
    return inside


# ---------------------------------------------------------------------------------------------
# Sheets
# ---------------------------------------------------------------------------------------------

@dataclass
class Sheet:
    """A grid sprite sheet with JSON metadata the Unity importer reads (prompt §10 step 6)."""
    name: str
    cell_w: int
    cell_h: int
    pivot: tuple[float, float]  # pixels from the cell's bottom-left
    rows: list[tuple[str, list[Canvas]]] = field(default_factory=list)
    extra: dict = field(default_factory=dict)
    gap: int = 0

    def add_row(self, clip: str, frames: list[Canvas]):
        for f in frames:
            assert f.w == self.cell_w and f.h == self.cell_h, f"{self.name}/{clip}: frame {f.w}x{f.h} != cell {self.cell_w}x{self.cell_h}"
        self.rows.append((clip, frames))

    def save(self, folder: str):
        os.makedirs(folder, exist_ok=True)
        cols = max(len(fr) for _, fr in self.rows)
        W = cols * (self.cell_w + self.gap)
        H = len(self.rows) * (self.cell_h + self.gap)
        img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        frames_meta = []
        clips = {}
        for r, (clip, frames) in enumerate(self.rows):
            names = []
            for i, f in enumerate(frames):
                x = i * (self.cell_w + self.gap)
                y = r * (self.cell_h + self.gap)
                img.paste(f.image(), (x, y))
                name = f"{clip}_{i}"
                names.append(name)
                # Unity rects are measured from the bottom-left of the texture.
                frames_meta.append({"name": name, "x": x, "y": H - y - self.cell_h, "w": self.cell_w, "h": self.cell_h})
            clips[clip] = names
        img.save(os.path.join(folder, self.name + ".png"))
        meta = {
            "sheet": self.name, "cell": [self.cell_w, self.cell_h], "pivot": list(self.pivot),
            "frames": frames_meta, "clips": clips, **self.extra,
        }
        # Unity's JsonUtility cannot read dictionaries: also write list forms.
        meta["clipList"] = [{"name": k, "frames": v} for k, v in clips.items()]
        for key, value in list(self.extra.items()):
            if isinstance(value, dict):
                meta[key + "List"] = [
                    {"name": k, "x": (v[0] if isinstance(v, (list, tuple)) else float(v)),
                     "y": (v[1] if isinstance(v, (list, tuple)) else 0.0)}
                    for k, v in value.items()
                ]
        with open(os.path.join(folder, self.name + ".json"), "w", encoding="utf-8") as fh:
            json.dump(meta, fh, indent=1)
        return img


def save_single(canvas: Canvas, folder: str, name: str, pivot=None, extra=None):
    os.makedirs(folder, exist_ok=True)
    canvas.image().save(os.path.join(folder, name + ".png"))
    meta = {"sheet": name, "cell": [canvas.w, canvas.h], "pivot": list(pivot or (canvas.w / 2, 0)),
            "frames": [{"name": name, "x": 0, "y": 0, "w": canvas.w, "h": canvas.h}], "clips": {name: [name]},
            "clipList": [{"name": name, "frames": [name]}]}
    if extra:
        meta.update(extra)
    with open(os.path.join(folder, name + ".json"), "w", encoding="utf-8") as fh:
        json.dump(meta, fh, indent=1)


def preview(img: Image.Image, scale: int, background=(14, 13, 12, 255)) -> Image.Image:
    """Nearest-neighbour enlargement on charcoal, for contact sheets (prompt §10 step 7)."""
    big = img.resize((img.width * scale, img.height * scale), Image.NEAREST)
    bg = Image.new("RGBA", big.size, background)
    bg.alpha_composite(big)
    return bg


def tint_flame(img: Image.Image, ramp=("#F28A1D", "#FFC14D", "#FFE9A8")) -> Image.Image:
    """Recolours a grey-level flame layer like the engine does (for previews and the design deck)."""
    arr = np.array(img).copy()
    g = arr[..., :3].max(axis=2)
    a = arr[..., 3] > 0
    cols = [np.array(_hex(h)[:3]) for h in ramp]
    for mask, col in ((g < 115, cols[0]), ((g >= 115) & (g < 204), cols[1]), (g >= 204, cols[2])):
        sel = mask & a
        arr[sel, :3] = col
    return Image.fromarray(arr, "RGBA")
