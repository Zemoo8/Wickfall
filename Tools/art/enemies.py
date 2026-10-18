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


# ---- Husk / Carcasse (A08): 32x32, a hollow split trunk with long thin limbs, hanging in ambush --

def husk(pose="hang", frame=0):
    """pose: hang (barely swaying), drop (drop, land, rise, ready), lunge (raking claws)."""
    cv = Canvas(32, 32)
    sway = (0, 1, 0, -1)[frame % 4] if pose == "hang" else 0
    if pose == "hang":
        top, bottom = 2, 22          # hangs high, head down
    elif pose == "drop":
        top, bottom = [6, 10, 9, 8][frame], [26, 30, 30, 30][frame]
    else:
        top, bottom = 8, 30
    cx = 16 + sway
    # the trunk body (bark)
    for y in range(top, bottom):
        t = (y - top) / max(1, bottom - top)
        half = 4.5 + 1.5 * math.sin(t * math.pi)
        for x in range(32):
            if abs(x + 0.5 - cx) <= half:
                cv.mset(x, y, "rust")
    cv.shade()
    # the empty cavity in its chest
    cy = (top + bottom) // 2
    for y in range(cy - 3, cy + 3):
        for x in range(cx - 2, cx + 2):
            cv.set(x, y, "k0" if abs(x + 0.5 - cx) < 1.5 else "k1")
    # bark splinters (bone-white)
    for (dx, dy) in ((-4, 3), (3, 6), (-3, -4)):
        cv.set(cx + dx, cy + dy, "b2")
    # limbs: long and thin; hanging = draped up to the branch, lunge = raking forward
    if pose == "hang":
        for x in range(4, 28):
            cv.set(x, 1, "o0")  # the branch
        for k in range(6):
            cv.set(cx - 5 - k // 2, top + k, "o1")
            cv.set(cx + 5 + k // 2, top + k, "o1")
            cv.set(cx - 7 + sway, top + 8 + k, "o1")
            cv.set(cx + 7 + sway, top + 8 + k, "o1")
    elif pose == "drop":
        spread = [2, 6, 5, 4][frame]
        for k in range(7):
            cv.set(cx - 5 - k * spread // 6, cy + k, "o1")
            cv.set(cx + 5 + k * spread // 6, cy + k, "o1")
    else:
        reach = [2, 6, 9, 4][frame]
        for k in range(reach):
            cv.set(cx + 5 + k, cy - 2 + k // 3, "o2")
            cv.set(cx + 5 + k, cy + 1 + k // 3, "o2")
        for (dx, dy) in ((reach + 5, -2 + reach // 3), (reach + 5, 1 + reach // 3)):
            cv.set(cx + dx, cy + dy, "b3")
            cv.set(cx + dx + 1, cy + dy - 1, "b3")
        for k in range(5):
            cv.set(cx - 5, cy + k, "o1")
    cv.outline("k0")
    return cv


def husk_sheet():
    s = Sheet("Enemy_Husk", 32, 32, (16, 2))
    s.add_row("special", [husk("hang", i) for i in range(4)])     # PDF/A08: hanging in ambush
    s.add_row("windup", [husk("drop", i) for i in range(4)])      # A08: drop, land, rise, ready
    s.add_row("attack", [husk("lunge", i) for i in range(4)])     # A08: lunging attack, raking claws
    s.add_row("idle", [husk("drop", 3), husk("drop", 3).shifted(0, 1)])
    s.add_row("move", [husk("drop", 3).shifted(0, (0, 1, 0, -1)[i]) for i in range(4)])
    s.add_row("death", [husk("drop", 1).shifted(0, i) for i in range(4)])
    s.extra = {"fps": {"special": 3, "windup": 7, "attack": 12, "idle": 3, "move": 8, "death": 6}}
    return s


# ---- Lurker / Rôdeur (A09): 24x24, low crouching shadow creature, frozen pose and slow crawl ------

def lurker(frame=0, crawl=False, glint=False):
    cv = Canvas(24, 24)
    step = (0, 1, 0, -1)[frame % 4] if crawl else 0
    # long folded limbs (spider crossed with a cat), body low
    cv.ellipse(12, 15, 7, 3.2, "char")
    cv.ellipse(16, 13, 3, 2.5, "char")          # head, facing right
    for (lx, ly, dx, dy) in ((6, 15, -3, 4), (9, 16, -1, 5), (14, 16, 2, 5), (17, 15, 4, 4)):
        lift = step if lx in (6, 14) else -step
        cv.mline(lx, ly, lx + dx, ly + dy - abs(lift), "char")
        cv.mline(lx + dx, ly + dy - abs(lift), lx + dx + (1 if dx > 0 else -1), 21, "char")
    cv.shade()
    # a faint bone-white rim where light would touch it (top-left edges)
    for y in range(24):
        for x in range(24):
            if cv.get(x, y) and (cv.get(x, y - 1) == 0 or cv.get(x - 1, y) == 0):
                cv.set(x, y, "b0")
    # two pinprick pale eyes
    cv.set(17, 12, "b3")
    cv.set(19, 12, "b3" if not glint else "w1")
    cv.outline("k0")
    return cv


def lurker_sheet():
    s = Sheet("Enemy_Lurker", 24, 24, (12, 2))
    s.add_row("special", [lurker(0, False, i == 2) for i in range(4)])  # PDF: frozen pose (1-px eye glint)
    s.add_row("move", [lurker(i, True) for i in range(4)])               # PDF: slow crawl
    s.add_row("idle", [lurker(0, False), lurker(0, False, True)])
    s.add_row("windup", [lurker(0, False).shifted(-1, 1), lurker(0, False).shifted(-2, 1)])
    s.add_row("attack", [lurker(1, True).shifted(2, 0), lurker(2, True).shifted(3, -1)])
    s.add_row("death", [lurker(0, False).shifted(0, i) for i in range(4)])
    s.extra = {"fps": {"special": 2, "move": 6, "idle": 2, "windup": 8, "attack": 12, "death": 6}}
    return s


# ---- Light-Eater Bloom (A10): 32x32 dark flower, pale glowing core, 4-frame pulse -----------------

def bloom(frame=0, mode="pulse"):
    cv = Canvas(32, 32)
    # stem and roots in black soil
    for y in range(20, 30):
        cv.set(16, y, "o0")
        cv.set(15, y, "k2")
    for (dx, dy) in ((-3, 0), (3, 1), (-5, 1), (5, 0)):
        cv.set(16 + dx, 29 + dy // 2, "o0")
    if mode == "pulse":
        openness = (0.0, 0.15, 0.25, 0.15)[frame]
    else:  # feed: snap wide, flare, gulp shut
        openness = (0.9, 1.0, 0.4, 0.0)[frame]
    # thick charcoal petals with ochre veins
    for k in range(6):
        a = math.radians(-90 + (k - 2.5) * (22 + 30 * openness))
        length = 9 + openness * 2
        for t in range(int(length)):
            px = 16 + math.cos(a) * t
            py = 16 + math.sin(a) * t * 0.9
            w = 2.4 * math.sin((t + 1) / (length + 1) * math.pi)
            for o in range(-int(w), int(w) + 1):
                cv.mset(px - math.sin(a) * o, py + math.cos(a) * o, "char")
    cv.shade()
    for k in range(6):
        a = math.radians(-90 + (k - 2.5) * (22 + 30 * openness))
        for t in range(2, 8):
            cv.set(16 + math.cos(a) * t, 16 + math.sin(a) * t * 0.9, "o1")
    # the pale glowing core
    core = 2 + int(openness * 2) + (1 if mode == "pulse" and frame == 2 else 0)
    for y in range(32):
        for x in range(32):
            r = math.hypot(x + 0.5 - 16, y + 0.5 - 17)
            if r <= core:
                cv.set(x, y, "w0" if r < core * 0.5 else "b3")
    cv.outline("k0")
    return cv


def bloom_sheet():
    s = Sheet("Enemy_Bloom", 32, 32, (16, 2))
    s.add_row("pulse", [bloom(i, "pulse") for i in range(4)])   # PDF: 4-frame pulse
    s.add_row("feed", [bloom(i, "feed") for i in range(4)])     # A10: snap, flare, gulp shut
    s.extra = {"fps": {"pulse": 4, "feed": 10}}
    return s


def build(out):
    images = []
    for sheet in (dripling_sheet(), waxfly_sheet(), husk_sheet(), lurker_sheet(), bloom_sheet()):
        images.append((sheet.name, sheet.save(out)))
    return images
