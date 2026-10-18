"""
Region 6 - Chambre du Foyer art.

PDF §5: "Boss 128x160 px, a tall crowned figure made of pure black negative space, with a flame-shaped
hole in its chest. Sprite sheet: idle, slam attack, and three phase variants (increasingly broken
crown), 6 frames each." Not in PDF (G27): the Grand Foyer in negative light, the King's fallen pose,
pact shrines, the void projectile.
"""
from __future__ import annotations

import math

from pixel import Canvas, Sheet

W, H = 128, 160


def _robe(cv, t, kneel=False):
    top, bottom = (50, 156) if not kneel else (78, 156)
    for x in range(W):                                   # rounded shoulders
        for y in range(top - 6, top + 10):
            if ((x + 0.5 - 64) / 27) ** 2 + ((y + 0.5 - top - 4) / 9) ** 2 <= 1:
                cv.set(x, y, "k0")
    for y in range(top, bottom + 1):
        f = (y - top) / (bottom - top)
        half = 22 + 24 * f ** 1.4 + 2.0 * math.sin(y * 0.16 + t) * f
        for x in range(W):
            # a tattered hem: the bottom edge rises and falls
            hem = bottom - 6 * (0.5 + 0.5 * math.sin(x * 0.55 + t)) * (1 if abs(x - 64) > 8 else 0.3)
            if abs(x + 0.5 - 64) <= half and y <= hem:
                cv.set(x, y, "k0")


def _head(cv, cy, tilt=0):
    for y in range(cy - 14, cy + 14):
        for x in range(64 - 12, 64 + 12):
            if ((x + 0.5 - 64 - tilt * (y - cy) * 0.1) / 11) ** 2 + ((y + 0.5 - cy) / 13) ** 2 <= 1:
                cv.set(x, y, "k0")
    cv.set(59, cy, "b2")
    cv.set(69, cy, "b2")


def _crown(cv, base_y, phase, dx=0, tilt=0):
    """A black crown with a pale rim, more broken each phase (PDF: increasingly broken crown)."""
    crown = Canvas(W, H)
    spikes = [(-12, 10), (-6, 12), (0, 16), (6, 12), (12, 10)]
    broken = {1: set(), 2: {3, 4}, 3: {0, 3, 4}}[phase]
    for x in range(64 - 14, 64 + 15):
        if phase == 3 and 65 <= x <= 67:
            continue                                   # a crack through the band
        yy = base_y + int(tilt * (x - 64) / 14)
        for y in range(yy, yy + 4):
            crown.set(x + dx, y, "k0")
    for i, (ox, h) in enumerate(spikes):
        h = 3 if i in broken else h
        for k in range(h):
            w = max(0, 2 - k * 2 // max(1, h))
            for x in range(64 + ox - w, 64 + ox + w + 1):
                crown.set(x + dx, base_y - k - 1 + int(tilt * ox / 14), "k0")
    for y in range(H):
        for x in range(W):
            if crown.get(x, y):
                edge = not crown.get(x - 1, y) or not crown.get(x + 1, y) or not crown.get(x, y - 1)
                cv.set(x, y, "b0" if edge else "k0")


def _arm(cv, sx, sy, hx, hy):
    steps = int(max(abs(hx - sx), abs(hy - sy))) + 1
    for i in range(steps + 1):
        f = i / steps
        x, y = sx + (hx - sx) * f, sy + (hy - sy) * f
        for o in (-1, 0, 1):
            cv.set(x + o, y, "k0")
    for oy in range(-3, 4):
        for ox in range(-3, 4):
            if ox * ox + oy * oy <= 9:
                cv.set(hx + ox, hy + oy, "k0")
    for k in range(4):                                   # long claws
        for (cx, cy) in ((-3, 1), (0, 1), (3, 1)):
            cv.set(hx + cx, hy + 3 + k + cy * 0, "k0")


def _hole(cv, cy, breath):
    """The flame-shaped hole in its chest: truly empty (the floor shows through), a pale rim."""
    top, bottom = cy - 16 - breath, cy + 12
    inside = []
    for y in range(top, bottom + 1):
        f = (y - top) / (bottom - top)
        half = (7 + breath) * math.sin(math.pi * min(1, f * 1.1)) * (0.45 + 0.55 * f)
        for x in range(W):
            if abs(x + 0.5 - 64 - math.sin(f * 3) * (1 - f) * 2) <= half:
                inside.append((x, y))
    for (x, y) in inside:
        cv.erase(x, y)
    for (x, y) in inside:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if cv.get(nx, ny):
                cv.set(nx, ny, "b1" if dy <= 0 else "k3")


def _outline(cv):
    out = cv.copy()
    for y in range(H):
        for x in range(W):
            v = cv.get(x, y)
            if v and (not cv.get(x - 1, y) or not cv.get(x + 1, y) or not cv.get(x, y - 1) or not cv.get(x, y + 1)):
                out.set(x, y, "k2")
    return out


def king(pose="idle", frame=0, phase=1):
    cv = Canvas(W, H)
    t = frame / 6 * 2 * math.pi
    breath = int(round(1 + math.sin(t)))
    if pose == "fallen":
        _robe(cv, t, kneel=True)
        _head(cv, 68, tilt=3)
        _arm(cv, 44, 84, 30, 150)
        _arm(cv, 84, 84, 98, 150)
        cv = _outline(cv)
        _hole(cv, 108, 0)
        _crown(cv, 150, 3, dx=36, tilt=0.6)         # the crown has fallen beside him
        return cv
    _robe(cv, t)
    _head(cv, 34)
    hands = {
        "idle": ((16, 122 + int(2 * math.sin(t))), (112, 122 - int(2 * math.sin(t)))),
    }
    if pose == "slam":
        hands = [((40, 60), (88, 60)), ((52, 8), (76, 8)), ((54, 6), (74, 6)),
                 ((56, 146), (72, 146)), ((54, 152), (74, 152)), ((40, 120), (88, 120))][frame]
    else:
        hands = hands["idle"]
    (lx, ly), (rx, ry) = hands
    _arm(cv, 44, 54, lx, ly)
    _arm(cv, 84, 54, rx, ry)
    cv = _outline(cv)
    _hole(cv, 84, breath)
    _crown(cv, 20, phase)
    if pose == "slam" and frame == 4:
        for (x, y) in ((40, 156), (46, 158), (82, 157), (88, 155), (64, 159)):
            cv.set(x, y, "b0")
    return cv


def king_sheet():
    s = Sheet("Boss_HollowKing", W, H, (64, 4))
    for phase in (1, 2, 3):
        s.add_row(f"idle{phase}", [king("idle", i, phase) for i in range(6)])   # PDF: 3 phase variants, 6 frames
    s.add_row("slam", [king("slam", i, 1) for i in range(6)])                  # PDF: slam attack, 6 frames
    s.add_row("fallen", [king("fallen", i) for i in range(2)])                 # G27: fallen / spared pose
    s.extra = {"fps": {"idle1": 5, "idle2": 6, "idle3": 7, "slam": 6, "fallen": 2}}
    return s


# ---- the Grand Foyer (G27): a vast cold hearth in negative light; lit for ending A ---------------------

def foyer(lit, frame=0):
    cv = Canvas(96, 96)
    for y in range(70, 92):                              # the pedestal
        half = 14 + (y - 70) * 0.6
        for x in range(96):
            if abs(x + 0.5 - 48) <= half:
                cv.mset(x, y, "stone")
    for y in range(44, 72):                              # the great bowl
        f = (y - 44) / 28
        half = 40 * (1 - f ** 2) + 10
        for x in range(96):
            if abs(x + 0.5 - 48) <= half:
                cv.mset(x, y, "stone")
    cv.shade()
    for x in range(10, 86):                              # the rim
        cv.set(x, 44, "b0")
    if lit:
        rnd_heights = [(14, 20), (24, 30), (36, 38), (48, 42), (60, 36), (72, 28), (82, 18)]
        for (fx, h) in rnd_heights:
            h = h + (3 if (fx + frame) % 3 == 0 else 0)
            for k in range(h):
                w = max(1, int(5 * (1 - k / h)))
                colour = "a3" if k < h * 0.3 else "a2" if k < h * 0.65 else "a1"
                for x in range(fx - w, fx + w + 1):
                    cv.set(x, 44 - k, colour)
    else:
        for x in range(14, 82, 3):                       # dead black embers
            cv.set(x, 43, "k0")
            cv.set(x + 1, 42, "k1")
    cv.outline("k0")
    return cv


def pact_shrine():
    cv = Canvas(32, 48)
    for y in range(6, 46):                               # a dark obelisk
        half = 6 + (y - 6) * 0.1
        for x in range(32):
            if abs(x + 0.5 - 16) <= half:
                cv.mset(x, y, "char")
    cv.shade()
    for x in range(10, 23):                              # a crown cut into its head
        cv.erase(x, 8)
    for (x, h) in ((11, 4), (16, 6), (21, 4)):
        for k in range(h):
            cv.set(x, 8 - k, "k0")
    for y in range(20, 30):                              # a void where a flame should be
        for x in range(14, 19):
            if (x + 0.5 - 16.5) ** 2 / 6 + (y + 0.5 - 25) ** 2 / 25 <= 1:
                cv.erase(x, y)
    for (x, y) in ((13, 22), (19, 22), (14, 29), (18, 29)):
        cv.set(x, y, "b0")
    cv.outline("k0")
    return cv


def void_ball(frame):
    cv = Canvas(10, 10)
    for y in range(10):
        for x in range(10):
            r = math.hypot(x + 0.5 - 5, y + 0.5 - 5)
            if r <= 3.6:
                cv.set(x, y, "k0")
            elif r <= 4.4:
                a = math.atan2(y - 5, x - 5) + frame * math.pi / 2
                cv.set(x, y, "b1" if math.sin(a * 3) > 0 else "k3")
    return cv


def build(out_enemies, out_props, out_vfx):
    k = king_sheet()
    images = [(k.name, k.save(out_enemies))]
    f = Sheet("Prop_Foyer", 96, 96, (48, 4))
    f.add_row("cold", [foyer(False)])
    f.add_row("lit", [foyer(True, i) for i in range(4)])
    f.save(out_props)
    p = Sheet("Prop_PactShrine", 32, 48, (16, 2))
    p.add_row("shrine", [pact_shrine()])
    p.save(out_props)
    v = Sheet("VFX_VoidBall", 10, 10, (5, 5))
    v.add_row("fx", [void_ball(i) for i in range(4)])
    v.save(out_vfx)
    return images, ["Prop_Foyer", "Prop_PactShrine"]
