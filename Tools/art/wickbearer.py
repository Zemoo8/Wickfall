"""
The Wickbearer (PDF §3 "Joueur"): a small candle golem, 32x40 px, pale cream wax, a small glowing
flame on its head, melancholic and determined. Built from layered shapes (legs, body, arms, each
shaded and outlined on its own) so every frame of every animation is the same character.

Sheets produced (frame counts are the PDF's):
  walk     4 directions x 6 frames
  dim      idle, 4 frames (facing down)
  attack   4 directions x 5 frames   (the fire arc is the flame-tinted Slash VFX)
  dash     4 directions x 3 frames
  hurt     2 frames
  death    12 frames
  idle     4 directions x 2 frames (breathing; the standing poses of design sheet A01)
  flame    6-frame flicker loop + 4-frame dim bead (grey levels, recoloured in the engine)
"""
from __future__ import annotations

import math

from pixel import Canvas, Sheet, c

W, H = 32, 40
FEET = 37                       # bottom row of the feet
PIVOT = (16, H - (FEET + 1))    # pixels from the bottom-left: centre of the feet
CX = 15.5                       # body centre


def cylinder_tone(x, xl, xr):
    """Tone of a vertical wax cylinder lit from the top-left (0 darkest .. 3 lightest)."""
    if xr <= xl:
        return 2
    p = (x - xl) / (xr - xl)
    if p < 0.1:
        return 2
    if p < 0.38:
        return 3
    if p < 0.68:
        return 2
    if p < 0.9:
        return 1
    return 0


WAX = ["b0", "b1", "b2", "b3"]


def paint_cylinder(cv: Canvas, rows: dict[int, tuple[int, int]], darker=0):
    for y, (xl, xr) in rows.items():
        for x in range(xl, xr + 1):
            t = max(0, cylinder_tone(x, xl, xr) - darker)
            cv.set(x, y, WAX[t])


def limb(cv: Canvas, x0, y0, w, h, darker=0, round_end=True):
    rows = {}
    for y in range(y0, y0 + h):
        rows[y] = (x0, x0 + w - 1)
    if round_end and h > 2:
        rows[y0 + h - 1] = (x0 + 1, x0 + w - 2) if w > 2 else (x0, x0 + w - 1)
    paint_cylinder(cv, rows, darker)


def layer() -> Canvas:
    return Canvas(W, H)


def composite(layers) -> Canvas:
    out = Canvas(W, H)
    for i, lay in enumerate(layers):
        if i == 0:
            out.paste(lay, 0, 0)
        else:
            paste_soft(out, lay)
    return out


def paste_soft(base: Canvas, top: Canvas):
    """Pastes a limb over the body: its black outline turns into dark wax where it lies on wax,
    so overlapping forms stay separate without hard black lines across the body."""
    k0 = c("k0")
    for y in range(H):
        for x in range(W):
            v = top.px[y, x]
            if v == 0:
                continue
            under = base.px[y, x]
            if v == k0 and under != 0 and under != k0:
                base.px[y, x] = c("b0")
            else:
                base.px[y, x] = v


def body(view: str, *, dy=0, squash=0, lean=0, legs=(0, 0), arms=(0, 0), arm_fwd=(0, 0), hunch=0,
         eyes="open", drip_phase=0, crack=True, drop=False):
    """
    One pose. Returns (canvas, wick_top_xy).
      view     "down" | "up" | "right"   (left = mirrored right)
      dy       whole-body vertical offset (+ = down)
      squash   body rows removed (+) or added (-)
      lean     shear of the top of the body in pixels (+ = right)
      legs     front/back: lift per leg (left, right); side: stride (back, front)
      arms     vertical offset per arm (left, right)
      arm_fwd  reach per arm (left, right)
      hunch    shoulders curl in and the body sinks (dim state)
    """
    side = view == "right"
    half = 5 if side else 6
    top = 12 + dy + squash + hunch
    bottom = 30 + dy
    x0 = int(round(CX - half)) + (1 if side else 0)
    x1 = int(round(CX + half)) - 1 + (1 if side else 0)

    def shear(y):
        if lean == 0 or bottom == top:
            return 0
        t = (bottom - y) / (bottom - top)
        return int(round(lean * max(0.0, min(1.0, t))))

    # ---- legs ------------------------------------------------------------------------------
    lg = layer()
    if side:
        back, front = legs
        for lx, off, dark in ((13 + back, back, 1), (16 + front, front, 0)):
            lift = 1 if off != 0 and abs(off) >= 2 else 0
            foot = FEET + dy - lift
            limb(lg, lx, bottom, 3, foot - bottom + 1, dark, round_end=False)
            for x in range(lx, lx + 4):
                lg.set(x, foot, WAX[max(0, 1 - dark)])
    else:
        for i, (lx, fx0, fx1) in enumerate(((11, 10, 14), (17, 17, 21))):
            foot = FEET + dy - legs[i]
            limb(lg, lx, bottom, 4, foot - bottom + 1, 1, round_end=False)
            for x in range(fx0, fx1 + 1):
                lg.set(x, foot, "b1" if x < fx1 else "b0")
    lg.outline()

    # ---- body ------------------------------------------------------------------------------
    bd = layer()
    rows = {}
    for y in range(top, bottom + 1):
        s = shear(y)
        xl, xr = x0 + s, x1 + s
        if y >= bottom - 1:          # the base flares where wax pooled
            xl, xr = xl - 1, xr + 1
        rows[y] = (xl, xr)
    paint_cylinder(bd, rows)
    # darker base band
    for x in range(rows[bottom][0], rows[bottom][1] + 1):
        bd.set(x, bottom, "b1" if x < rows[bottom][1] else "b0")

    # top surface (seen from above at ~45 degrees): light ellipse with a shallow melt pool
    s_top = shear(top)
    tcx = CX + s_top + (1 if side else 0)
    rx = half + 0.5
    for y in range(top - 3, top + 1):
        for x in range(x0 - 1 + s_top, x1 + 2 + s_top):
            dx = (x + 0.5 - (tcx + 0.5)) / rx
            dyy = (y + 0.5 - (top - 1)) / 2.0
            if dx * dx + dyy * dyy <= 1.0:
                inner = dx * dx * 2.2 + dyy * dyy * 2.2 <= 1.0
                bd.set(x, y, "b2" if inner else "b3")
    # the rim lip hangs over the body by one pixel on each side
    for x in (rows[top][0] - 1, rows[top][1] + 1):
        bd.set(x, top, "b2")
        bd.set(x, top + 1, "b1" if x > tcx else "b2")

    # drips running down the front (lighter streaks with a shadow on their right)
    if True:
        # Long drips at the edges, short ones in the middle (they must not run through the eyes).
        drip_cols = [(-6, 5), (-2, 2), (1, 3), (5, 6)] if not side else [(-5, 5), (-1, 2), (3, 6)]
        for i, (off, length) in enumerate(drip_cols):
            dx_ = int(round(tcx + off))
            L = length + (1 if (drip_phase + i) % 3 == 0 else 0)
            for k in range(L):
                y = top + 1 + k
                x = dx_ + shear(y) - s_top
                bd.set(x, y, "b3")
                bd.set(x + 1, y, "b1" if k < L - 1 else "b2")
            bd.set(dx_ + shear(top + 1 + L) - s_top, top + 1 + L, "b2")

    # wick
    wick_x = int(round(tcx))
    for y in range(top - 5, top - 1):
        bd.set(wick_x, y, "k2")
        if not side:
            bd.set(wick_x - 1, y, "k1") if y > top - 4 else None
    wick_top = (wick_x + 0.5, top - 5)

    # face
    if view in ("down", "right"):
        ey = top + 6 + max(0, -squash)
        if view == "down":
            spots = [(int(CX) - 4 + shear(ey), -1), (int(CX) + 3 + shear(ey), 1)]
        else:
            spots = [(x1 - 3 + shear(ey), 1)]
        for ex, outward in spots:
            if eyes == "closed":
                bd.set(ex, ey + 1, "k1")
                bd.set(ex + 1, ey + 1, "k1")
                continue
            # Hollow eyes, 2x3, darkest at the bottom.
            for py in range(3):
                for px in range(2):
                    bd.set(ex + px, ey + py, "k0" if py == 2 else "k1")
            # A sad, determined brow: one darker pixel above the inner corner, sloping out.
            inner = ex + (1 if outward < 0 else 0)
            outer = ex + (0 if outward < 0 else 1)
            bd.set(inner, ey - 1, "b0")
            bd.set(outer, ey - 1, "b1")

    # the chest crack, glowing ochre wick-core
    if crack and view in ("down", "right"):
        cy0 = top + 10
        if view == "down":
            path = [(0, 0), (1, 1), (1, 2), (0, 3), (1, 4), (2, 5)]
            bx = int(CX) - 2
        else:
            path = [(0, 0), (0, 1), (-1, 2), (0, 3), (0, 4)]
            bx = x1 - 2
        for i, (px, py) in enumerate(path):
            y = cy0 + py
            bd.set(bx + px + shear(y), y, "o2" if 0 < i < len(path) - 1 else "o1")
        bd.set(bx + path[2][0] + 1 + shear(cy0 + 2), cy0 + 2, "o3")

    # back view: an old seam of wax down the back
    if view == "up":
        for y in range(top + 3, bottom - 1):
            if (y - top) % 4 != 0:
                bd.set(int(CX) + shear(y), y, "b1")
    bd.outline()

    # ---- arms ------------------------------------------------------------------------------
    am = layer()
    back_arm = layer()
    if side:
        ay = top + 8 + arms[1]
        ax = int(CX) + arm_fwd[1] - 1
        limb(am, ax, ay, 3, 6, 0)
    else:
        for i, base in enumerate((rows[top][0] - 3, rows[top][1] + 1)):
            ay = top + 7 + arms[i]
            reach = arm_fwd[i] * (-1 if i == 0 else 1)
            ax = base + (hunch if i == 0 else -hunch) + reach
            target = am if view == "down" else back_arm
            limb(target, ax, ay, 3, 6, 0 if i == 0 else 1)
    am.outline()
    back_arm.outline()

    if view == "up":
        out = composite([lg, back_arm, bd])
    else:
        out = composite([lg, bd, am])

    if drop:
        out.set(26, 13, "b3")
        out.set(27, 14, "b2")
        out.set(26, 14, "b2")
        out.set(27, 12, "k0")

    return out, wick_top


def mirror(frames):
    return [f.mirrored() for f in frames]


# ---------------------------------------------------------------------------------------------
# Animations
# ---------------------------------------------------------------------------------------------

WALK = [  # contact, down, passing, up, contact (other foot), passing
    dict(dy=0, legs=(0, 2), arms=(1, -1)),
    dict(dy=1, legs=(0, 1), arms=(1, 0)),
    dict(dy=0, legs=(0, 0), arms=(0, 0)),
    dict(dy=-1, legs=(2, 0), arms=(-1, 1)),
    dict(dy=0, legs=(1, 0), arms=(0, 1)),
    dict(dy=0, legs=(0, 0), arms=(0, 0)),
]

WALK_SIDE = [  # stride offsets (back leg, front leg)
    dict(dy=0, legs=(-2, 2), arm_fwd=(0, 1)),
    dict(dy=1, legs=(-1, 1), arm_fwd=(0, 1)),
    dict(dy=0, legs=(0, 0), arm_fwd=(0, 0)),
    dict(dy=-1, legs=(2, -2), arm_fwd=(0, -1)),
    dict(dy=0, legs=(1, -1), arm_fwd=(0, -1)),
    dict(dy=0, legs=(0, 0), arm_fwd=(0, 0)),
]


def walk(view):
    poses = WALK_SIDE if view == "right" else WALK
    frames, bob = [], []
    for i, p in enumerate(poses):
        cv, _ = body(view, drip_phase=i, **p)
        frames.append(cv)
        bob.append(-p["dy"])
    return frames, bob


def idle(view):
    a, wick = body(view, drip_phase=0)
    b, _ = body(view, squash=1, drip_phase=1)  # breathing: the body settles by one pixel
    return [a, b], wick


def dim():
    """PDF: "tiny tight flame, hunched posture, wax melting and dripping. Idle animation, 4 frames"."""
    frames = []
    wick = None
    for i in range(4):
        cv, w = body("down", hunch=3, squash=1 if i in (1, 2) else 0, drip_phase=i, arms=(2, 2))
        # one drip slowly sliding down across the 4 frames
        cv.set(20, 21 + i * 2, "b3")
        cv.set(21, 21 + i * 2, "b1")
        frames.append(cv)
        wick = wick or w
    return frames, wick


def attack(view):
    # anticipation, swing start, strike, follow-through, recovery
    if view == "right":
        poses = [dict(lean=-2, arm_fwd=(0, -2), arms=(0, -3)), dict(lean=0, arm_fwd=(0, 1), arms=(0, -2)),
                 dict(lean=2, arm_fwd=(0, 3), arms=(0, -1)), dict(lean=1, arm_fwd=(0, 2), arms=(0, 1)),
                 dict(lean=0)]
    elif view == "down":
        poses = [dict(dy=-1, arms=(-3, -3), arm_fwd=(1, 1)), dict(dy=0, arms=(-2, 0), arm_fwd=(0, 1)),
                 dict(dy=1, arms=(1, 2), arm_fwd=(-1, -1)), dict(dy=1, arms=(1, 1)), dict(dy=0)]
    else:
        poses = [dict(dy=1, arms=(1, 1)), dict(dy=0, arms=(-1, -1), arm_fwd=(1, 1)),
                 dict(dy=-1, arms=(-3, -3), arm_fwd=(-1, -1)), dict(dy=-1, arms=(-1, -1)), dict(dy=0)]
    return [body(view, drip_phase=i, **p)[0] for i, p in enumerate(poses)]


def dash(view):
    # squash (wide and low), stretch (long and leaning), recover
    if view == "right":
        poses = [dict(squash=3, legs=(-1, 1)), dict(squash=-2, lean=3, legs=(-2, 2), arm_fwd=(0, -2)), dict(squash=1)]
    else:
        poses = [dict(squash=3), dict(squash=-2, legs=(1, 1)), dict(squash=1)]
    return [body(view, drip_phase=i, **p)[0] for i, p in enumerate(poses)]


def hurt():
    a, _ = body("down", lean=-2, dy=-1, eyes="closed", arms=(-3, -3))
    b, _ = body("down", lean=-1, squash=1, eyes="closed", arms=(-1, -1), drop=True)
    return [a, b]


def puddle(width, height, lump, smoke=0, eyes=False):
    """The collapsed wax puddle of the death animation."""
    cv = Canvas(W, H)
    cx, base = CX, FEET
    rows = {}
    for y in range(base - height, base + 1):
        t = (y - (base - height)) / max(1, height)
        half = width * (0.55 + 0.45 * math.sqrt(t))
        if y < base - height + lump:
            half *= 0.6
        rows[y] = (int(round(cx - half)), int(round(cx + half)) - 1)
    paint_cylinder(cv, rows)
    top_y = base - height
    for x in range(rows[top_y][0], rows[top_y][1] + 1):
        cv.set(x, top_y, "b3")
    if eyes:
        cv.set(int(cx) - 3, top_y + 2, "k1")
        cv.set(int(cx) + 2, top_y + 2, "k1")
    # the snapped wick lying in the wax
    cv.set(int(cx), top_y - 1, "k2")
    cv.set(int(cx) + 1, top_y - 1, "k1")
    cv.outline()
    for k in range(smoke):
        sx = int(cx) + (1 if k % 2 else 0) - (k // 3)
        cv.set(sx, top_y - 3 - k * 2, "k3" if k % 2 else "b0")
    return cv


def death():
    """12 frames: the flame gutters (flame layer), the body sags, melts, collapses and spreads into a
    flat wax puddle, with one last wisp of smoke."""
    frames = []
    frames.append(body("down", eyes="open")[0])
    frames.append(body("down", hunch=1, eyes="open", drip_phase=1)[0])
    frames.append(body("down", hunch=2, squash=1, eyes="closed", drip_phase=2)[0])
    frames.append(body("down", hunch=3, squash=3, eyes="closed", drip_phase=0, arms=(2, 2))[0])
    frames.append(body("down", hunch=4, squash=5, eyes="closed", drip_phase=1, arms=(3, 3), lean=-1)[0])
    frames.append(puddle(9, 13, 6, eyes=True))
    frames.append(puddle(10, 10, 4, eyes=True))
    frames.append(puddle(11, 7, 3, eyes=False))
    frames.append(puddle(12, 5, 2))
    frames.append(puddle(13, 4, 1, smoke=2))
    frames.append(puddle(13, 3, 0, smoke=4))
    frames.append(puddle(14, 3, 0, smoke=5))
    return frames


def flame_frames():
    """Grey-level flame (16x20 cell, pivot at the bottom centre = the wick tip)."""
    frames = []
    shapes = [(11, 0), (12, 1), (13, 1), (12, 0), (11, -1), (12, -1)]
    for height, tip in shapes:
        cv = Canvas(16, 20)
        base_y = 19
        for row in range(height):
            y = base_y - row
            t = row / max(1, height - 1)
            half = 3.6 * (1 - abs(t - 0.32) / 0.68) ** 0.7 if t < 0.98 else 0.4
            half = max(0.5, half)
            fx = 7.5 + tip * t * t * 1.6
            for x in range(16):
                d = abs(x + 0.5 - (fx + 0.5))
                if d <= half:
                    core = d <= half * 0.42 and t < 0.55
                    mid = d <= half * 0.78 and t < 0.85
                    cv.set(x, y, "fc" if core else ("fb" if mid else "fe"))
        frames.append(cv)
    return frames


def flame_dim_frames():
    frames = []
    for i in range(4):
        cv = Canvas(16, 20)
        h = 4 + (i % 2)
        for row in range(h):
            y = 19 - row
            half = 1.5 if row < h - 2 else 0.6
            for x in range(16):
                if abs(x + 0.5 - 8) <= half:
                    cv.set(x, y, "fc" if row < 2 and abs(x + 0.5 - 8) < 1 else "fb")
        frames.append(cv)
    return frames


def build(out_folder: str):
    """Writes Player_Body.png/.json and Player_Flame.png/.json."""
    sheet = Sheet("Player_Body", W, H, PIVOT)
    anchors = {}
    bobs = {}
    for view in ("down", "up", "right"):
        frames, bob = walk(view)
        sheet.add_row(f"walk_{view}", frames)
        bobs[view] = bob
    frames, _ = walk("right")
    sheet.add_row("walk_left", mirror(frames))

    for view in ("down", "up", "right"):
        frames, wick = idle(view)
        sheet.add_row(f"idle_{view}", frames)
        anchors[view] = wick
    frames, _ = idle("right")
    sheet.add_row("idle_left", mirror(frames))
    anchors["left"] = (W - anchors["right"][0], anchors["right"][1])

    frames, dim_wick = dim()
    sheet.add_row("dim", frames)

    for view in ("down", "up", "right"):
        sheet.add_row(f"attack_{view}", attack(view))
    sheet.add_row("attack_left", mirror(attack("right")))
    for view in ("down", "up", "right"):
        sheet.add_row(f"dash_{view}", dash(view))
    sheet.add_row("dash_left", mirror(dash("right")))
    sheet.add_row("hurt", hurt())
    sheet.add_row("death", death())

    def rel(p):  # wick top relative to the pivot, in pixels (x right, y up)
        return [round(p[0] - PIVOT[0], 2), round((H - p[1]) - PIVOT[1], 2)]

    sheet.extra = {
        "flameAnchor": {k: rel(v) for k, v in anchors.items()},
        "dimFlameAnchor": rel(dim_wick),
        "walkBob": bobs["down"],
        "fps": {"walk": 10, "idle": 2, "dim": 4, "attack": 18, "dash": 16, "hurt": 10, "death": 6},
    }
    img = sheet.save(out_folder)

    fl = Sheet("Player_Flame", 16, 20, (8, 0))
    fl.add_row("flame", flame_frames())
    fl.add_row("flame_dim", flame_dim_frames())
    fl.extra = {"fps": {"flame": 12, "flame_dim": 8}}
    fl.save(out_folder)
    return img


def design_sheet(flame_img=None) -> Canvas:
    """A01: the four standing poses side by side (for the design deck)."""
    out = Canvas(W * 4 + 12, H)
    for i, view in enumerate(("down", "up", "right")):
        cv, _ = body(view)
        out.paste(cv, i * (W + 4), 0)
    cv, _ = body("right")
    out.paste(cv.mirrored(), 3 * (W + 4), 0)
    return out
