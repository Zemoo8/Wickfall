"""
Creatures (PDF §3 enemy prompts, cards A06-A15). Sizes and the PDF's frame counts are exact;
extra frames the game needs (wind-up, attack, death) are derived from the same body and logged
as "not in PDF" in Docs/ART_PROMPTS.md.
"""
from __future__ import annotations

import math

from pixel import Canvas, Sheet


def blob(cv, cx, cy, rx, ry, material, lumps=(), tone=-1):
    cv.ellipse(cx, cy, rx, ry, material, tone)
    for (lx, ly, lr) in lumps:
        cv.ellipse(cx + lx, cy + ly, lr, lr * 0.85, material, tone)


# ---- Dripling (A06): 24x24, a hunched lump of dull dripping wax, two empty eye holes ----------

def dripling(phase=0, sag=0, stretch=0, lean=0, mouth=0, melt=0.0):
    """
    phase   drip animation phase
    sag     body height change (+ = lower, wider)
    stretch forward stretch (oozing / lunging), facing right
    mouth   0 closed, 1 open (attack)
    melt    0..1 death
    """
    cv = Canvas(24, 24)
    base = 21
    h = 9 - sag - int(melt * 7)
    w = 7 + sag // 2 + int(melt * 4)
    cx = 11.5 + stretch * 0.5 + lean
    cy = base - h
    # the body: a lumpy dome
    for y in range(int(cy - h), base + 1):
        t = (y - (cy - h)) / max(1, (base - (cy - h)))
        half = w * (0.35 + 0.65 * math.sqrt(max(0.0, t)))
        shift = stretch * (1 - t) * 0.8
        for x in range(24):
            if abs(x + 0.5 - (cx + shift)) <= half:
                cv.mset(x, y, "oldwax")
    # lumps on the back and the head
    cv.ellipse(cx - w * 0.45 - stretch * 0.3, cy + 1, 2.5, 2, "oldwax")
    cv.ellipse(cx + w * 0.3 + stretch * 0.5, cy - 1 + sag // 2, 3, 2.5, "oldwax")
    cv.shade()
    if melt < 0.8:
        # empty eye holes (facing right, the near eye bigger), a drooping mouth
        ex = int(round(cx + 2 + stretch * 0.6))
        ey = int(round(cy + 1 + sag * 0.4))
        for (dx, dy) in ((0, 0), (1, 0), (0, 1), (1, 1)):
            cv.set(ex + dx, ey + dy, "k0")
        cv.set(ex - 4, ey + 1, "k0")
        cv.set(ex - 4, ey, "k1")
        my = ey + 3
        if mouth:
            for dx in range(-2, 2):
                cv.set(ex + dx, my, "k0")
                cv.set(ex + dx, my + 1, "k1")
        else:
            cv.set(ex - 1, my, "k1")
            cv.set(ex, my + 1, "k1")
            cv.set(ex + 1, my + 1, "k1")
    # wax strands dripping from the underside
    for i, dx in enumerate((-4, -1, 3)):
        length = 1 + (phase + i) % 3
        x = int(round(cx + dx))
        for k in range(length):
            if base + 1 + k < 24:
                cv.set(x, base + 1 + k, "b1" if k < length - 1 else "b2")
    cv.outline("k0")
    return cv


def dripling_sheet():
    s = Sheet("Enemy_Dripling", 24, 24, (12, 2))
    s.add_row("idle", [dripling(phase=i, sag=(0, 1, 1, 0)[i]) for i in range(4)])              # PDF: 4-frame idle
    s.add_row("move", [dripling(phase=i, stretch=(2, 3, 1, 0)[i], sag=(1, 0, 0, 1)[i]) for i in range(4)])  # PDF: 4-frame move
    s.add_row("windup", [dripling(sag=2, stretch=-1), dripling(sag=3, stretch=-2)])            # not in PDF
    s.add_row("attack", [dripling(stretch=5, mouth=1), dripling(stretch=3, mouth=1)])           # not in PDF
    s.add_row("death", [dripling(melt=t) for t in (0.2, 0.45, 0.7, 0.95)])                     # not in PDF
    s.extra = {"fps": {"idle": 5, "move": 7, "windup": 6, "attack": 10, "death": 6}}
    return s


# ---- Waxfly / Cirelle (A07): 8x8, a tiny harmless glowing insect ------------------------------

def waxfly(wings_up, light):
    cv = Canvas(8, 8)
    # body
    for (x, y, c) in ((3, 4, "b2"), (4, 4, "b2"), (3, 5, "b1"), (4, 5, "b1"), (4, 3, "b3")):
        cv.set(x, y, c)
    # wings (pale, fully opaque)
    wy = 2 if wings_up else 4
    for (x, y) in ((1, wy), (2, wy), (2, wy + 1), (5, wy), (6, wy), (5, wy + 1)):
        cv.set(x, y, "b3")
    # its light
    cv.set(4, 6, "a3" if light else "a2")
    cv.outline("k0")
    return cv


def waxfly_sheet():
    s = Sheet("Enemy_Waxfly", 8, 8, (4, 2))
    s.add_row("idle", [waxfly(i % 2 == 0, i < 2) for i in range(4)])     # PDF: 4-frame idle
    s.add_row("move", [waxfly(i % 2 == 1, i % 2 == 0) for i in range(4)])  # PDF: 4-frame move
    s.extra = {"fps": {"idle": 8, "move": 12}}
    return s


def build(out):
    images = []
    for sheet in (dripling_sheet(), waxfly_sheet()):
        images.append((sheet.name, sheet.save(out)))
    return images
