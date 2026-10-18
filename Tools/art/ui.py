"""
Interface art (PDF §3 "Interface" + upgraded cards A34-A37):
  candle wax bar, 8 fill states (16x64, no flame on top: the engine draws the live flame)
  UI flames pre-tinted per flame (16x20, 6 frames)
  five 16x16 flame icons with five silhouettes readable in greyscale (A05)
  radial flame wheel ring (96x96) and sockets, wax shard icon 16x16, dodge-charge icons 8x8
  pause/panel frame 48x48 nine-slice, bone white on charcoal
"""
from __future__ import annotations

import math

from pixel import Canvas, Sheet, c

FLAME_RAMPS = {
    "amber": ("a1", "a2", "a3"),
    "blue": ("u0", "u1", "u2"),
    "green": ("g0", "g1", "g2"),
    "white": ("b2", "w0", "w1"),
    "crimson": ("r0", "r1", "r2"),
}
FLAME_ORDER = ["amber", "blue", "green", "white", "crimson"]


def candle_states():
    """16x64 cells; the candle shrinks from full to a short stub over 8 states, drips pile at the base."""
    frames, tops = [], []
    for s in range(8):
        cv = Canvas(16, 64)
        base = 61
        height = int(round(52 - s * 6.2))
        top = base - height
        # holder: an ochre dish
        for x in range(1, 15):
            cv.set(x, 62, "o1")
            cv.set(x, 63 if 2 <= x <= 13 else 62, "o0")
        for x in range(2, 14):
            cv.set(x, 61, "o2" if x < 8 else "o1")
        # wax body with cylinder shading
        for y in range(top, base):
            for x in range(4, 12):
                p = (x - 4) / 7
                t = "b2" if p < 0.12 else "b3" if p < 0.4 else "b2" if p < 0.7 else "b1" if p < 0.9 else "b0"
                cv.set(x, y, t)
        # melted top: slightly concave, lighter rim, small wick
        for x in range(4, 12):
            cv.set(x, top, "b3" if x in (4, 11) else "b2")
        cv.set(7, top - 1, "k2")
        cv.set(7, top - 2, "k2")
        cv.set(8, top - 1, "k1")
        # drips down the side; more as it melts, and they pile up at the base
        for i, (dx, length) in enumerate([(4, 3 + s), (10, 2 + s // 2), (6, 1 + s // 3)]):
            for k in range(min(length, height - 2)):
                cv.set(dx, top + 1 + k, "b3")
                if dx + 1 < 12:
                    cv.set(dx + 1, top + 1 + k, "b1")
        for k in range(s):
            x = 3 + (k * 5) % 10
            cv.set(x, 60, "b2")
            cv.set(x + 1, 60, "b1")
            if k % 2 == 0:
                cv.set(x, 59, "b3")
        cv.outline("k0")
        frames.append(cv)
        tops.append(64 - (top - 2))  # pixels from the bottom of the sprite to the wick top
    return frames, tops


def ui_flame(ramp, frame):
    cv = Canvas(16, 20)
    shapes = [(11, 0), (12, 1), (13, 1), (12, 0), (11, -1), (12, -1)]
    height, tip = shapes[frame]
    edge, body, core = ramp
    for row in range(height):
        y = 19 - row
        t = row / max(1, height - 1)
        half = max(0.5, 3.6 * (1 - abs(t - 0.32) / 0.68) ** 0.7 if t < 0.98 else 0.4)
        fx = 7.5 + tip * t * t * 1.6
        for x in range(16):
            d = abs(x + 0.5 - (fx + 0.5))
            if d <= half:
                cv.set(x, y, core if d <= half * 0.42 and t < 0.55 else body if d <= half * 0.78 and t < 0.85 else edge)
    return cv


def flame_icon(kind, ramp, locked=False):
    """A05: amber round, blue teardrop, green leaf, white star, crimson jagged."""
    cv = Canvas(16, 16)
    edge, body, core = ramp if not locked else ("k2", "k3", "b0")
    cx, cy = 7.5, 8.5

    def put(x, y, col):
        if 0 <= x < 16 and 0 <= y < 16:
            cv.set(x, y, col)

    for y in range(16):
        for x in range(16):
            px, py = x + 0.5 - (cx + 0.5), y + 0.5 - (cy + 0.5)
            r = math.hypot(px, py)
            a = math.atan2(py, px)
            inside = False
            depth = 0.0
            if kind == "amber":      # round, plump
                inside = (px / 5.6) ** 2 + ((py - 0.6) / 6.2) ** 2 <= 1 and not (py < -4 and abs(px) > 6.5 + py)
                depth = 1 - math.hypot(px / 5.6, (py - 1.5) / 6.2)
            elif kind == "blue":     # teardrop with a sharp tip
                if py >= -1:
                    inside = (px / 4.8) ** 2 + ((py - 1.6) / 4.8) ** 2 <= 1
                else:
                    inside = abs(px) <= 4.6 * (py + 7.5) / 6.5
                depth = 1 - math.hypot(px / 4.8, (py - 2) / 6)
            elif kind == "green":    # a leaf with a central vein
                inside = abs(px) <= 5.2 * math.sin(max(0.0, min(math.pi, (py + 7.2) / 14.4 * math.pi))) and -7.2 <= py <= 7
                depth = 1 - abs(px) / 5.2
            elif kind == "white":    # a star-shaped flame (5 points)
                k = 5
                spike = 0.5 + 0.5 * math.cos(k * (a + math.pi / 2))
                inside = r <= 3.2 + 3.8 * spike ** 2
                depth = 1 - r / 7
            elif kind == "crimson":  # jagged, forked, aggressive
                base = (px / 5.4) ** 2 + ((py - 2) / 5.0) ** 2 <= 1
                prongs = py < -1 and any(abs(px - ox) <= (py + 8.5 - oy) * 0.32 for ox, oy in ((-3.5, 2.5), (0.5, 0), (3.8, 3)))
                inside = (base or prongs) and py <= 7
                depth = 1 - math.hypot(px / 5.4, (py - 2.5) / 5)
            if inside:
                put(x, y, core if depth > 0.62 else body if depth > 0.28 else edge)
    if kind == "green" and not locked:
        for y in range(3, 15):
            put(7, y, edge if y < 6 else body)
    cv.outline("k0")
    return cv


def wheel_ring():
    cv = Canvas(96, 96)
    for y in range(96):
        for x in range(96):
            r = math.hypot(x + 0.5 - 48, y + 0.5 - 48)
            if 40 <= r <= 43:
                cv.set(x, y, "b2" if r < 41.5 else "b1")
            elif 43 < r <= 44:
                cv.set(x, y, "k0")
            elif 39 <= r < 40:
                cv.set(x, y, "k0")
            elif r < 39 and r > 20 and (x + y) % 2 == 0:
                cv.set(x, y, "k1")
    # small ornaments between sockets
    for i in range(5):
        a = math.radians(90 - i * 72 - 36)
        ox, oy = 48 + math.cos(a) * 41.5, 48 - math.sin(a) * 41.5
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
            cv.set(int(ox) + dx, int(oy) + dy, "b3")
    return cv


def socket(selected):
    cv = Canvas(24, 24)
    for y in range(24):
        for x in range(24):
            r = math.hypot(x + 0.5 - 12, y + 0.5 - 12)
            if r <= 10:
                cv.set(x, y, "k1" if r < 9 else ("a2" if selected else "b1"))
            if selected and 10 < r <= 11:
                cv.set(x, y, "a1")
    cv.outline("k0")
    return cv


def shard_icon():
    """A36: a faceted chip of glowing wax."""
    cv = Canvas(16, 16)
    pts = [(8, 1), (13, 6), (11, 14), (5, 14), (3, 6)]
    cv.poly(pts, "wax")
    cv.shade()
    for x, y in ((8, 3), (8, 4), (7, 5), (9, 5)):
        cv.set(x, y, "a3")
    for x, y in ((6, 9), (7, 10), (9, 8)):
        cv.set(x, y, "a2")
    cv.line(8, 2, 8, 13, "b1")
    cv.outline("k0")
    return cv


def dodge_icon(full):
    cv = Canvas(8, 8)
    for y in range(8):
        for x in range(8):
            r = math.hypot(x + 0.5 - 4, y + 0.5 - 4)
            if r <= 3:
                cv.set(x, y, ("b3" if r < 1.5 else "b2") if full else "k2")
    if full:
        cv.set(2, 2, "b3")
    cv.outline("k0")
    return cv


def status_icon(kind):
    cv = Canvas(10, 10)
    if kind == "soot":
        for x, y, col in ((3, 3, "k3"), (4, 3, "k3"), (5, 4, "k2"), (2, 4, "k2"), (3, 5, "k3"), (6, 5, "k3"),
                          (4, 6, "k2"), (5, 6, "k3"), (4, 5, "b0"), (6, 3, "k3"), (7, 4, "k2")):
            cv.set(x, y, col)
    else:  # light cut: a small flame struck through
        for y in range(2, 8):
            for x in range(3, 7):
                if abs(x + 0.5 - 5) <= (y - 1) * 0.5:
                    cv.set(x, y, "a2" if y > 5 else "a1")
        cv.line(1, 8, 8, 1, "b2")
    cv.outline("k0")
    return cv


def panel_frame():
    """A37: 48x48 nine-slice, 16 px corners, bone-white ornament on charcoal, candle drips on top."""
    cv = Canvas(48, 48)
    for y in range(48):
        for x in range(48):
            cv.set(x, y, "k1")
    for i in range(48):
        for t, col in ((0, "k0"), (1, "b1"), (2, "b2"), (3, "k0")):
            cv.set(i, t, col)
            cv.set(i, 47 - t, col)
            cv.set(t, i, col)
            cv.set(47 - t, i, col)
    # corner ornaments
    for ox, oy in ((5, 5), (42, 5), (5, 42), (42, 42)):
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
            cv.set(ox + dx, oy + dy, "b3" if (dx, dy) == (0, 0) else "b1")
    # drips hanging from the top edge
    for x, length in ((9, 4), (14, 2), (22, 5), (31, 3), (38, 4)):
        for k in range(length):
            cv.set(x, 4 + k, "b2" if k < length - 1 else "b3")
    return cv


def key_cap():
    cv = Canvas(12, 12)
    for y in range(1, 11):
        for x in range(1, 11):
            cv.set(x, y, "b2" if y < 9 else "b0")
    cv.outline("k0")
    return cv


def build(out):
    sheet = Sheet("HUD_Candle", 16, 64, (8, 0))
    frames, tops = candle_states()
    sheet.add_row("candle", frames)
    sheet.extra = {"candleTops": {str(i): [0, t] for i, t in enumerate(tops)}}
    img = sheet.save(out)

    fl = Sheet("HUD_Flames", 16, 20, (8, 0))
    for name in FLAME_ORDER:
        fl.add_row("flame_" + name, [ui_flame(FLAME_RAMPS[name], f) for f in range(6)])
    fl.save(out)

    icons = Sheet("HUD_FlameIcons", 16, 16, (8, 8))
    icons.add_row("icon", [flame_icon(n, FLAME_RAMPS[n]) for n in FLAME_ORDER] + [flame_icon("amber", None, locked=True)])
    icons.save(out)

    small = Sheet("HUD_Small", 24, 24, (12, 12))
    small.add_row("socket", [socket(False), socket(True)])
    misc = []
    for cvs in (shard_icon(), dodge_icon(True), dodge_icon(False), status_icon("soot"), status_icon("cut"), key_cap()):
        big = Canvas(24, 24)
        big.paste(cvs, (24 - cvs.w) // 2, (24 - cvs.h) // 2)
        misc.append(big)
    small.add_row("misc", misc)
    small.save(out)

    ring = Sheet("HUD_Wheel", 96, 96, (48, 48))
    ring.add_row("ring", [wheel_ring()])
    ring.save(out)

    panel = Sheet("UI_Panel", 48, 48, (24, 24))
    panel.add_row("panel", [panel_frame()])
    panel.extra = {"border": {"all": [16, 16]}}
    panel.save(out)
    return img
