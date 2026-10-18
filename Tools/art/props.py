"""
Objects and props (PDF §3 "Objets et décors", cards A27-A33) used by the Light Lab and Region 1:
brazier (unlit and lit, 4-frame fire), wax pool (full and empty), the training effigy,
wax pickups and the glowing wax drops left on the ground when hit.
"""
from __future__ import annotations

import math

from pixel import Canvas, Sheet


def brazier_bowl(lit: bool) -> Canvas:
    """A27: an iron bowl on three legs, ochre metal, a bone-white ash rim (32x32)."""
    cv = Canvas(32, 32)
    # legs
    for lx, lean in ((9, -1), (16, 0), (22, 1)):
        for k in range(9):
            cv.set(lx + (lean * k) // 4, 22 + k, "o1" if k % 3 else "o0")
            cv.set(lx + 1 + (lean * k) // 4, 22 + k, "o0")
    # bowl
    for y in range(14, 23):
        t = (y - 14) / 8
        half = 11 - t * t * 6
        for x in range(32):
            d = x + 0.5 - 16
            if abs(d) <= half:
                p = (d + half) / (2 * half)
                cv.set(x, y, "o3" if p < 0.25 else "o2" if p < 0.6 else "o1" if p < 0.85 else "o0")
    # rim and ash
    for x in range(5, 27):
        cv.set(x, 13, "b2" if lit else "b1")
        cv.set(x, 14, "o3" if x < 16 else "o2")
    for x in range(8, 24):
        cv.set(x, 12, "b3" if lit and 11 < x < 21 else "b1")
    # rivets
    for x in (9, 16, 23):
        cv.set(x, 17, "b2")
    if lit:
        for x in range(9, 23):
            cv.set(x, 13, "a1" if x % 3 else "a2")
    cv.outline("k0")
    return cv


def brazier_fire(frame: int) -> Canvas:
    """Lit brazier fire, 4 frames (amber: a light source)."""
    cv = Canvas(32, 32)
    for k, (ox, h) in enumerate(((-5, 7), (0, 11), (5, 8), (-2, 6), (3, 6))):
        h2 = h + [0, 2, 1, -1][(frame + k) % 4]
        for row in range(h2):
            y = 12 - row
            t = row / max(1, h2 - 1)
            half = max(0.5, 2.6 * (1 - t) ** 0.8)
            sway = int(round(math.sin((frame + k) * 1.7) * t * 1.5))
            for x in range(32):
                d = abs(x + 0.5 - (16 + ox + sway))
                if d <= half:
                    col = "a3" if d < half * 0.4 and t < 0.6 else "a2" if d < half * 0.75 else "a1"
                    if cv.get(x, y) == 0 or col == "a3":
                        cv.set(x, y, col)
    for x, y in ((12 + frame, 3), (19 - frame, 1), (16, frame)):
        cv.set(x, y, "a2")
    return cv


def wax_pool(full: bool) -> Canvas:
    """A28: full (glowing liquid wax with a soft surface highlight) and empty (a dry, cracked basin)."""
    cv = Canvas(32, 32)
    for y in range(32):
        for x in range(32):
            r = math.hypot((x + 0.5 - 16) / 13, (y + 0.5 - 18) / 8)
            if r <= 1:
                if r > 0.8:
                    cv.set(x, y, "b1" if y < 18 else "b0")
                elif full:
                    cv.set(x, y, "b3" if (r < 0.35 and y < 18) else "b2" if r < 0.6 else "b1")
                else:
                    cv.set(x, y, "k3" if r < 0.6 else "k2")
    if full:
        for x in range(11, 17):
            cv.set(x, 14, "w0")
        cv.set(18, 15, "w0")
        cv.set(20, 21, "a3")
    else:
        for x, y in ((12, 17), (13, 18), (14, 18), (15, 19), (19, 16), (20, 17), (18, 20), (17, 21)):
            cv.set(x, y, "k1")
    cv.outline("k0")
    return cv


def effigy() -> Canvas:
    """Light Lab training effigy (not in the PDF, a sandbox prop): a lump of old wax on a stake."""
    cv = Canvas(24, 32)
    for y in range(18, 31):
        cv.set(11, y, "o1")
        cv.set(12, y, "o0")
    cv.ellipse(11.5, 13, 6.5, 8, "oldwax")
    cv.shade()
    for x, y in ((9, 11), (14, 11), (9, 12), (14, 12)):
        cv.set(x, y, "k1")
    for x in range(8, 16):
        cv.set(x, 16, "o1" if x % 2 else "o0")
    cv.outline("k0")
    return cv


def wax_pickup(frame: int) -> Canvas:
    cv = Canvas(8, 8)
    for y in range(8):
        for x in range(8):
            r = math.hypot(x + 0.5 - 4, (y + 0.5 - 4.5) * 1.2)
            if r <= 2.6:
                cv.set(x, y, "b3" if r < 1.2 else "b2")
    cv.set(3 + frame, 3, "w1")
    cv.outline("k0")
    return cv


def wax_drop() -> Canvas:
    """G18: a faint glowing drop left on the ground (8x6)."""
    cv = Canvas(8, 6)
    for y in range(6):
        for x in range(8):
            r = math.hypot((x + 0.5 - 4) / 3.4, (y + 0.5 - 3) / 2.2)
            if r <= 1:
                cv.set(x, y, "b3" if r < 0.45 else "b2" if r < 0.8 else "b1")
    return cv


def blob_shadow(w=16, h=6) -> Canvas:
    cv = Canvas(w, h)
    for y in range(h):
        for x in range(w):
            if ((x + 0.5 - w / 2) / (w / 2)) ** 2 + ((y + 0.5 - h / 2) / (h / 2)) ** 2 <= 1:
                cv.set(x, y, "k0")
    return cv


def build(out):
    sh = Sheet("Prop_Shadow", 16, 6, (8, 3))
    sh.add_row("shadow", [blob_shadow()])
    sh.save(out)
    shb = Sheet("Prop_ShadowLarge", 32, 10, (16, 5))
    shb.add_row("shadow", [blob_shadow(32, 10)])
    shb.save(out)

    br = Sheet("Prop_Brazier", 32, 32, (16, 2))
    br.add_row("bowl", [brazier_bowl(False), brazier_bowl(True)])
    br.add_row("fire", [brazier_fire(i) for i in range(4)])
    br.save(out)

    pool = Sheet("Prop_WaxPool", 32, 32, (16, 10))
    pool.add_row("pool", [wax_pool(True), wax_pool(False)])
    pool.save(out)

    ef = Sheet("Prop_Effigy", 24, 32, (12, 1))
    ef.add_row("effigy", [effigy()])
    ef.save(out)

    small = Sheet("Prop_Pickups", 8, 8, (4, 4))
    small.add_row("pickup", [wax_pickup(0), wax_pickup(1)])
    small.save(out)

    drop = Sheet("Prop_WaxDrop", 8, 6, (4, 3))
    drop.add_row("drop", [wax_drop()])
    drop.save(out)
