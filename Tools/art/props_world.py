"""
World objects of PDF §3 "Objets et décors" (A29-A33) plus the props Region 1 needs:
  Ember Relics (6 different, glowing, 16x16), Great Bell (hanging, ringing 4 frames, ~48x64),
  the Broken Wick (huge candle stump landmark, 64x160), locked gate door (closed and open, 48x48),
  flame shrine (not in PDF), inscription stone (not in PDF), sleeping swarm mound (not in PDF).
"""
from __future__ import annotations

import math

from pixel import Canvas, Sheet


def relic(index):
    """A29: six glowing ember objects whose shapes hint at their power (G04)."""
    cv = Canvas(16, 16)
    ramp = {0: ("a1", "a2", "a3"), 1: ("g0", "g1", "g2"), 2: ("b2", "w0", "w1"),
            3: ("u0", "u1", "u2"), 4: ("a0", "a2", "a3"), 5: ("r0", "r1", "r2")}[index]
    lo, mid, hi = ramp

    def p(x, y, c):
        cv.set(x, y, c)

    if index == 0:    # Ember Feather: a feather-shaped ember (extra dodge)
        for y in range(2, 14):
            half = 3.2 * math.sin((y - 1) / 13 * math.pi)
            for x in range(16):
                if abs(x + 0.5 - (8 + (y - 8) * 0.25)) <= half:
                    p(x, y, hi if abs(x + 0.5 - (8 + (y - 8) * 0.25)) < half * 0.4 else mid)
        for y in range(3, 15):
            p(int(8 + (y - 8) * 0.25), y, lo)
    elif index == 1:  # Ember Hook: a hook of ember (wall grip)
        for a in range(0, 200, 12):
            r = math.radians(a)
            p(int(8 + math.cos(r) * 4), int(6 - math.sin(r) * 4), mid)
            p(int(8 + math.cos(r) * 3), int(6 - math.sin(r) * 3), hi)
        for y in range(6, 14):
            p(12, y, mid)
            p(11, y, hi if y < 10 else mid)
    elif index == 2:  # Brimming Ember: a full, round bead (more max wax)
        for y in range(16):
            for x in range(16):
                r = math.hypot(x + 0.5 - 8, y + 0.5 - 8.5)
                if r <= 5.5:
                    p(x, y, hi if r < 2.5 else mid if r < 4.2 else lo)
    elif index == 3:  # Five-Socket Ring (flame slot)
        for a in range(0, 360, 8):
            r = math.radians(a)
            p(int(8 + math.cos(r) * 5.5), int(8 + math.sin(r) * 5.5), lo)
        for k in range(5):
            r = math.radians(-90 + k * 72)
            x, y = int(8 + math.cos(r) * 5.5), int(8 + math.sin(r) * 5.5)
            for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1)):
                p(x + dx - 1, y + dy - 1, hi)
    elif index == 4:  # Spare Wick: a coiled wick with an ember tip
        for y in range(4, 14):
            p(7 + (y % 2), y, "k2")
        for (x, y) in ((7, 2), (8, 2), (7, 3), (8, 3), (8, 1)):
            p(x, y, hi if y < 3 else mid)
    else:             # Hearth Ember: a small hearth-shaped ember (cheaper arts)
        for y in range(5, 14):
            half = 2 + (y - 5) * 0.55
            for x in range(16):
                if abs(x + 0.5 - 8) <= half:
                    p(x, y, hi if abs(x + 0.5 - 8) < half * 0.4 and y > 8 else mid)
        for (x, y) in ((8, 2), (7, 3), (8, 3), (9, 4), (7, 4)):
            p(x, y, hi)
    cv.outline("k0")
    return cv


def great_bell(frame):
    """A30: hanging from a beam; still, plus 4 ringing frames (left, centre, right, centre)."""
    cv = Canvas(48, 64)
    # beam and posts
    for x in range(4, 44):
        for y in range(6, 10):
            cv.set(x, y, "o2" if y == 6 else "o1" if y < 9 else "o0")
    for y in range(10, 62):
        for (x, c) in ((6, "o1"), (7, "o0"), (40, "o1"), (41, "o0")):
            cv.set(x, y, c)
    swing = (0, -3, 0, 3, 0)[frame]
    # rope
    for y in range(10, 18):
        cv.set(24 + int(swing * (y - 10) / 26), y, "o2")
    # bell body (bronze-ish ochre)
    top = 18
    for y in range(top, top + 26):
        t = (y - top) / 25
        half = 5 + 10 * (t ** 1.6)
        cxx = 24 + swing * (y - 10) / 26
        for x in range(48):
            d = x + 0.5 - (cxx + 0.5)
            if abs(d) <= half:
                p = (d + half) / (2 * half)
                cv.set(x, y, "o3" if p < 0.22 else "o2" if p < 0.6 else "o1" if p < 0.88 else "o0")
    # lip and clapper
    for x in range(int(9 + swing), int(40 + swing)):
        cv.set(x, top + 26, "o1")
    cv.set(int(24 + swing * 1.4), top + 28, "k2")
    cv.set(int(24 + swing * 1.4), top + 29, "k1")
    cv.outline("k0")
    if frame in (1, 3):  # ochre sound rings
        for a in range(0, 360, 10):
            r = math.radians(a)
            for rad in (20, 23):
                x, y = 24 + math.cos(r) * rad, 32 + math.sin(r) * rad * 0.7
                if cv.get(int(x), int(y)) == 0:
                    cv.set(x, y, "o2")
    return cv


def broken_wick():
    """A31: a huge candle stump landmark (64x160) with a snapped, blackened wick and centuries of drips."""
    cv = Canvas(64, 160)
    base_y = 157
    top = 34
    for y in range(top, base_y + 1):
        t = (y - top) / (base_y - top)
        half = 20 + t * 9
        for x in range(64):
            d = x + 0.5 - 32
            if abs(d) <= half:
                p = (d + half) / (2 * half)
                cv.set(x, y, "b2" if p < 0.1 else "b3" if p < 0.35 else "b2" if p < 0.62 else "b1" if p < 0.86 else "b0")
    # the broken, uneven top
    for x in range(12, 53):
        dip = int(4 + 4 * math.sin(x * 0.45) + (6 if 24 < x < 34 else 0))
        for y in range(top, top + dip):
            cv.set(x, y, 0) if y < top + dip - 2 else cv.set(x, y, "b3")
    # snapped blackened wick
    for y in range(18, 40):
        lean = (40 - y) // 6
        cv.set(31 + lean, y, "k1")
        cv.set(32 + lean, y, "k2")
    cv.set(35, 17, "k0")
    cv.set(36, 16, "k0")
    # centuries of drips
    for i, (x, length) in enumerate([(14, 40), (19, 22), (26, 64), (33, 30), (40, 52), (46, 18), (50, 34)]):
        for k in range(length):
            y = top + 6 + k
            if y < base_y:
                cv.set(x, y, "b3")
                cv.set(x + 1, y, "b1")
        cv.set(x, top + 6 + length, "b3")
    # pooled wax and small offerings at the base
    for x in range(2, 62):
        for y in range(base_y - 3, base_y + 1):
            if abs(x - 32) < 30 - (base_y - y) * 2:
                cv.set(x, y, "b1" if y == base_y else "b2")
    for (x, c) in ((8, "o2"), (9, "o3"), (55, "o2"), (54, "a2"), (12, "a1")):
        cv.set(x, base_y - 4, c)
        cv.set(x, base_y - 5, c)
    cv.outline("k0")
    return cv


def gate(open_):
    """A33: a locked gate door, closed and open: iron bars, a wax seal and an ochre lock plate (48x48)."""
    cv = Canvas(48, 48)
    for x in range(48):
        for y in range(4, 8):
            cv.set(x, y, "k3" if y == 4 else "k2")
    for (x0, x1) in ((0, 4), (43, 47)):
        for x in range(x0, x1 + 1):
            for y in range(4, 48):
                cv.set(x, y, "k3" if x in (x0, x1) else "k2")
    if not open_:
        for x in range(8, 41, 5):
            for y in range(8, 47):
                cv.set(x, y, "k3")
                cv.set(x + 1, y, "k1")
        for y in (16, 34):
            for x in range(5, 43):
                cv.set(x, y, "k3")
        # lock plate and wax seal
        for x in range(20, 28):
            for y in range(22, 30):
                cv.set(x, y, "o2" if (x + y) % 3 else "o1")
        for (x, y) in ((23, 25), (24, 25), (23, 26), (24, 26)):
            cv.set(x, y, "a1")
    else:
        for x in (6, 41):
            for y in range(8, 47):
                cv.set(x, y, "k3")
    cv.outline("k0")
    return cv


def shrine():
    """Not in PDF: the altar where a region's flame is found (a wax altar on a stone base)."""
    cv = Canvas(32, 32)
    for y in range(22, 30):
        for x in range(6, 26):
            cv.set(x, y, "k3" if y == 22 else "k2" if x < 16 else "k1")
    for y in range(12, 22):
        for x in range(10, 22):
            p = (x - 10) / 11
            cv.set(x, y, "b3" if p < 0.35 else "b2" if p < 0.7 else "b1")
    for x in range(9, 23):
        cv.set(x, 11, "b3")
    for (x, length) in ((11, 4), (15, 6), (20, 3)):
        for k in range(length):
            cv.set(x, 12 + k, "b3")
    cv.outline("k0")
    return cv


def stone():
    """Not in PDF: an inscription stone (16x24)."""
    cv = Canvas(16, 24)
    for y in range(4, 22):
        half = 6 - (2 if y < 7 else 0)
        for x in range(16):
            if abs(x + 0.5 - 8) <= half:
                p = (x + 0.5 - 8 + half) / (2 * half)
                cv.set(x, y, "k3" if p < 0.3 else "k2" if p < 0.75 else "k1")
    for (y, x0, x1) in ((9, 5, 11), (12, 5, 10), (15, 5, 11)):
        for x in range(x0, x1):
            cv.set(x, y, "b0" if x % 3 else "k1")
    cv.outline("k0")
    return cv


def nest():
    """Not in PDF: a mound of cold wax where a creature sleeps until the bell (24x16)."""
    cv = Canvas(24, 16)
    cv.ellipse(12, 11, 9, 4, "oldwax")
    cv.ellipse(10, 9, 4, 3, "oldwax")
    cv.shade()
    for x in (8, 12, 15):
        cv.set(x, 12, "k1")
    cv.outline("k0")
    return cv


def build(out):
    rel = Sheet("Prop_Relics", 16, 16, (8, 0))
    rel.add_row("relic", [relic(i) for i in range(6)])
    rel.save(out)
    bell = Sheet("Prop_GreatBell", 48, 64, (24, 2))
    bell.add_row("still", [great_bell(0)])
    bell.add_row("ring", [great_bell(i) for i in (1, 2, 3, 4)])
    bell.save(out)
    wick = Sheet("Prop_BrokenWick", 64, 160, (32, 3))
    wick.add_row("wick", [broken_wick()])
    wick.save(out)
    g = Sheet("Prop_Gate", 48, 48, (24, 0))
    g.add_row("gate", [gate(False), gate(True)])
    g.save(out)
    sh = Sheet("Prop_Shrine", 32, 32, (16, 2))
    sh.add_row("shrine", [shrine()])
    sh.save(out)
    st = Sheet("Prop_Stone", 16, 24, (8, 2))
    st.add_row("stone", [stone()])
    st.save(out)
    ne = Sheet("Prop_Nest", 24, 16, (12, 3))
    ne.add_row("nest", [nest()])
    ne.save(out)
