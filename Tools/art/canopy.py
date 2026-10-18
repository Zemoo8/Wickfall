"""
Canopy art (A22, PDF §2.2 "canopée qui bloque la lumière"): renders a region's whole overhead leaf
layer as one image from the level's @canopy cells, so patches are organic (ragged rims, overlapping
leaf clumps lit from the top left, a soft drop shadow) instead of a repeating tile.

Used by the level scripts:  canopy.render(level.canopy_cells(), "Assets/Art/Levels/R2_Wickwood_Canopy.png")
"""
from __future__ import annotations

import os
import random

import numpy as np
from PIL import Image

from pixel import PALETTE_HEX

CELL = 16


def _rgb(name):
    h = PALETTE_HEX[name].lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], dtype=np.uint8)


def _box_blur(a, r):
    """Mean over a (2r+1)^2 window, via an integral image (edges clamp)."""
    p = np.pad(a, r + 1, mode="edge").astype(np.float64)
    s = p.cumsum(0).cumsum(1)
    k = 2 * r + 1
    out = s[k:, k:] - s[:-k, k:] - s[k:, :-k] + s[:-k, :-k]
    return (out / (k * k))[: a.shape[0], : a.shape[1]]


def _value_noise(h, w, cell, rnd):
    """Smooth value noise in [0, 1] with features about `cell` pixels wide."""
    gh, gw = h // cell + 2, w // cell + 2
    grid = np.array([[rnd.random() for _ in range(gw)] for _ in range(gh)])
    ys = np.arange(h) / cell
    xs = np.arange(w) / cell
    y0 = ys.astype(int)
    x0 = xs.astype(int)
    ty = (ys - y0)[:, None]
    tx = (xs - x0)[None, :]
    ty = ty * ty * (3 - 2 * ty)
    tx = tx * tx * (3 - 2 * tx)
    a = grid[y0][:, x0]
    b = grid[y0][:, x0 + 1]
    c = grid[y0 + 1][:, x0]
    d = grid[y0 + 1][:, x0 + 1]
    return (a * (1 - tx) + b * tx) * (1 - ty) + (c * (1 - tx) + d * tx) * ty


def render(cells, out_path, width=None, seed=7):
    """cells: list of strings (text rows, y down), 'c' = canopy above that cell; width = level width in cells."""
    rnd = random.Random(seed)
    rows = len(cells)
    cols = width or max((len(r) for r in cells), default=0)
    H, W = rows * CELL, cols * CELL
    cell_mask = np.zeros((rows, cols), dtype=np.float64)
    for y, row in enumerate(cells):
        for x, ch in enumerate(row):
            if ch == "c":
                cell_mask[y, x] = 1.0
    mask = np.kron(cell_mask, np.ones((CELL, CELL)))

    # organic rim: blur the blocky cell mask, then erode/extend it with noise
    soft = _box_blur(mask, 6)
    noise = 0.65 * _value_noise(H, W, 14, rnd) + 0.35 * _value_noise(H, W, 5, rnd)
    inside = soft + 0.42 * (noise - 0.5) > 0.5

    # leaf clumps: a heightfield of overlapping domes
    height = np.zeros((H, W))
    ys, xs = np.nonzero(soft > 0.25)
    count = len(ys) // 40
    for _ in range(count):
        i = rnd.randrange(len(ys))
        cy, cx = ys[i], xs[i]
        r = rnd.uniform(5.0, 10.0)
        amp = rnd.uniform(0.75, 1.15)
        y0, y1 = max(0, int(cy - r)), min(H, int(cy + r) + 1)
        x0, x1 = max(0, int(cx - r)), min(W, int(cx + r) + 1)
        yy, xx = np.mgrid[y0:y1, x0:x1]
        dome = amp * (1 - ((yy - cy) ** 2 + (xx - cx) ** 2) / (r * r))
        np.maximum(height[y0:y1, x0:x1], dome, out=height[y0:y1, x0:x1])

    # light from the top left: lit where the surface rises from its top-left neighbour
    shifted = np.zeros_like(height)
    shifted[1:, 1:] = height[:-1, :-1]
    slope = height - shifted

    img = np.zeros((H, W, 4), dtype=np.uint8)
    colour = np.full((H, W), 1, dtype=np.int8)        # 0 k0, 1 k1, 2 k2, 3 o0, 4 o1
    colour[slope > 0.07] = 2
    colour[slope > 0.17] = 3
    colour[(slope < -0.10) | (height < 0.10)] = 0
    sprinkle = np.random.default_rng(seed).random((H, W))
    colour[(slope > 0.18) & (sprinkle < 0.025)] = 4

    # dark outline where the canopy meets open sky
    outside = ~inside
    edge = inside & (np.roll(outside, 1, 0) | np.roll(outside, -1, 0) | np.roll(outside, 1, 1) | np.roll(outside, -1, 1))
    colour[edge] = 0

    palette = [_rgb("k0"), _rgb("k1"), _rgb("k2"), _rgb("o0"), _rgb("o1")]
    for k, rgb in enumerate(palette):
        sel = inside & (colour == k)
        img[sel, :3] = rgb
        img[sel, 3] = 255

    # soft drop shadow on the floor (down-right), under the leaves
    shadow = np.zeros_like(inside)
    shadow[4:, 3:] = inside[:-4, :-3]
    shadow &= outside
    img[shadow, :3] = _rgb("k0")
    img[shadow, 3] = 120

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    Image.fromarray(img, "RGBA").save(out_path)
    return out_path
