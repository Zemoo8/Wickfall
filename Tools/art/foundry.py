"""
Region 3 - Fonderies de Suie art (PDF §5 prompts + A11-A13, A23; props "not in PDF" per G27):
Slagborn 48x48 (slow walk 6 frames, overhead smash), Bellows 32x32 (attack 6 frames), soot ball 8x8
(4 frames), the press, the steam vent and the flickering furnace.
"""
from __future__ import annotations

import math

from pixel import Canvas, Sheet


# ---- Slagborn / Scorifié (A11): heavy hunched golem of rusted plates and cooled slag ------------

def slagborn(pose="walk", frame=0):
    cv = Canvas(48, 48)
    lift_l = lift_r = 0
    sway = 0
    bob = 0
    fists = "low"
    if pose == "walk":
        lift_l = (0, 2, 3, 1, 0, 0)[frame]
        lift_r = (1, 0, 0, 0, 2, 3)[frame]
        sway = (0, -1, -1, 0, 1, 1)[frame]
        bob = (0, 1, 1, 0, 1, 1)[frame]
    elif pose == "windup":
        fists = ("mid", "high")[frame]
        bob = -1 if frame else 0
    elif pose == "attack":
        fists = ("slam", "impact", "recover", "low")[frame]
        bob = (2, 3, 1, 0)[frame]
    elif pose == "idle":
        bob = frame
    cx = 24 + sway
    # thick slag legs
    for (lx, lift) in ((cx - 6, lift_l), (cx + 6, lift_r)):
        for y in range(35, 46 - lift):
            for x in range(lx - 3, lx + 4):
                cv.mset(x, y + 0, "char")
        for x in range(lx - 4, lx + 5):
            cv.mset(x, 45 - lift, "rust")
            cv.mset(x, 44 - lift, "rust")
    # a barrel body of cooled slag, rusted plates riveted over the chest and the huge shoulders
    cv.ellipse(cx, 28 + bob, 13, 10, "char")
    cv.ellipse(cx, 24 + bob, 10, 5.5, "rust")
    cv.ellipse(cx - 11, 21 + bob, 6, 5, "rust")
    cv.ellipse(cx + 11, 21 + bob, 6, 5, "rust")
    # the small head sunk between the shoulders
    cv.ellipse(cx, 17 + bob, 4.5, 3.5, "char")
    # arms and fists
    fist_points = {
        "low": ((cx - 15, 33 + bob), (cx + 15, 33 + bob)),
        "mid": ((cx - 10, 14), (cx + 10, 14)),
        "high": ((cx - 5, 6), (cx + 5, 6)),
        "slam": ((cx - 6, 40), (cx + 6, 40)),
        "impact": ((cx - 7, 42), (cx + 7, 42)),
        "recover": ((cx - 12, 36), (cx + 12, 36)),
    }[fists]
    for (fx, fy), (sx, sy) in zip(fist_points, ((cx - 11, 22 + bob), (cx + 11, 22 + bob))):
        cv.mline(sx, sy, fx, fy, "rust")
        cv.mline(sx + (1 if fx > sx else -1), sy, fx + (1 if fx > sx else -1), fy, "rust")
        cv.ellipse(fx, fy, 3.5, 3, "char")
    cv.shade()
    # plate seams and rivets
    for x in range(cx - 9, cx + 10):
        cv.set(x, 29 + bob, "k0")
    for y in range(20, 26):
        cv.set(cx - 6, y + bob, "k0")
        cv.set(cx + 6, y + bob, "k0")
    for (rx, ry) in ((cx - 13, 20), (cx - 9, 18), (cx + 9, 18), (cx + 13, 20), (cx - 8, 27), (cx + 8, 27), (cx - 3, 21), (cx + 3, 21)):
        cv.set(rx, ry + bob, "b1")
    # eyes: two dull ochre points
    cv.set(cx - 2, 17 + bob, "o3")
    cv.set(cx + 1, 17 + bob, "o3")
    # cracks in the belly leaking a dull ochre glow (cooling metal, not a flame)
    for (x0, y0, x1, y1) in ((cx - 5, 31, cx - 1, 36), (cx + 2, 31, cx + 6, 35), (cx - 1, 36, cx + 2, 37)):
        cv.line(x0, y0 + bob, x1, y1 + bob, "o2")
    cv.set(cx, 34 + bob, "o3")
    cv.set(cx - 3, 33 + bob, "o3")
    if fists == "impact":
        for (dx, dy) in ((-12, 44), (-10, 46), (11, 45), (13, 43), (0, 46)):
            cv.set(cx + dx, dy, "k3")
    cv.outline("k0")
    return cv


def slagborn_sheet():
    s = Sheet("Enemy_Slagborn", 48, 48, (24, 2))
    s.add_row("move", [slagborn("walk", i) for i in range(6)])          # PDF: slow walk, 6 frames
    s.add_row("windup", [slagborn("windup", i) for i in range(2)])      # A11 smash: raise, hold
    s.add_row("attack", [slagborn("attack", i) for i in range(4)])      # slam, impact, recover, settle
    s.add_row("idle", [slagborn("idle", 0), slagborn("idle", 1)])
    s.add_row("death", [_crumble(slagborn("walk", 0), i) for i in range(5)])
    s.extra = {"fps": {"move": 6, "windup": 3, "attack": 9, "idle": 2, "death": 7}}
    return s


def _crumble(cv, step):
    """Death: the plates fall away from the top down and the body sinks."""
    out = cv.shifted(0, step * 2)
    for y in range(48):
        for x in range(48):
            if out.get(x, y) and ((x * 7 + y * 13) % 9) < step * 2 and y < 20 + step * 6:
                out.erase(x, y)
    return out


# ---- Bellows / Soufflet (A12): accordion body on short iron legs, long dented nozzle ------------

def bellows(expand=0.0, puff=False, recoil=0, step=0):
    cv = Canvas(32, 32)
    ox = -recoil
    # short iron legs
    for (lx, lift) in ((10, step % 2), (20, (step + 1) % 2)):
        cv.mline(lx + ox, 23, lx + ox, 29 - lift, "char")
        cv.mline(lx + 1 + ox, 23, lx + 1 + ox, 29 - lift, "char")
    # pleated leather body between two iron plates
    half = 6 + expand * 1.5
    top, bottom = 16 - half, 16 + half
    for y in range(int(top), int(bottom) + 1):
        band = int((y - top) / max(1.0, (bottom - top) / 5)) % 2
        w = 7 - abs(y - 16) * 0.15
        for x in range(int(15 - w), int(15 + w) + 1):
            cv.mset(x + ox, y, "ochre" if band else "rust")
    for x in range(7, 24):
        cv.mset(x + ox, int(top) - 1, "char")
        cv.mset(x + ox, int(bottom) + 1, "char")
    # the long dented nozzle, pointing right, flared at the tip
    for x in range(21, 30):
        dent = 1 if x == 25 else 0
        for y in range(14 + dent, 18):
            cv.mset(x + ox, y, "rust")
    for y in range(13, 19):
        cv.mset(29 + ox, y, "rust")
    cv.shade()
    for (rx, ry) in ((8, int(top) - 1), (22, int(top) - 1), (8, int(bottom) + 1), (22, int(bottom) + 1)):
        cv.set(rx + ox, ry, "b1")
    if puff:
        for (dx, dy, col) in ((31, 15, "k0"), (30, 14, "k1"), (30, 17, "k1"), (29, 13, "k3")):
            cv.set(dx + ox, dy, col)
    cv.outline("k0")
    return cv


def bellows_sheet():
    s = Sheet("Enemy_Bellows", 32, 32, (16, 2))
    breathe = (0.0, 0.4, 0.8, 1.0, 0.6, 0.2)
    s.add_row("idle", [bellows(e) for e in breathe])                                  # A12 row 1: breathing
    s.add_row("windup", [bellows(1.6), bellows(1.8)])                                 # A12 row 2: expand fully
    s.add_row("attack", [bellows(-1.2), bellows(-1.2, True), bellows(-0.6, False, 1), bellows(0.2)])  # compress, puff, recoil, settle
    s.add_row("move", [bellows(breathe[i], step=i) for i in range(4)])
    s.add_row("death", [bellows(-0.8 - i * 0.3).shifted(0, i) for i in range(4)])
    s.extra = {"fps": {"idle": 5, "windup": 4, "attack": 10, "move": 6, "death": 6}}
    return s


# ---- Soot projectile (A13): 8x8 churning black cloud, 4 frames --------------------------------------

def soot_ball(frame):
    cv = Canvas(8, 8)
    for y in range(8):
        for x in range(8):
            r = math.hypot(x + 0.5 - 4, y + 0.5 - 4)
            if r <= 3.2:
                cv.set(x, y, "k0" if r < 1.8 else "k1")
    # a pale grey rim on the upper left so it reads over dark floors (no glow: PDF)
    for (x, y) in ((2, 2), (3, 1), (4, 1), (1, 3), (1, 4)):
        cv.set(x, y, "k3")
    a = frame * math.pi / 2
    for k in range(3):
        t = a + k * 2.1
        cv.set(4 + round(math.cos(t) * 3.4), 4 + round(math.sin(t) * 3.4), "b0" if k == 0 else "k3")
    cv.outline("k0")
    return cv


def soot_sheet():
    s = Sheet("VFX_SootBall", 8, 8, (4, 4))
    s.add_row("fx", [soot_ball(i) for i in range(4)])
    return s


# ---- Press (not in PDF, G27): a heavy press head over a base plate with ochre warning stripes ----

def press_plate():
    cv = Canvas(32, 32)
    for y in range(6, 28):
        for x in range(2, 30):
            edge = x < 4 or x > 27 or y < 8 or y > 25
            if edge:
                cv.set(x, y, "o2" if ((x + y) // 2) % 2 == 0 else "k1")   # warning stripes
            else:
                cv.set(x, y, "k2" if (x + y) % 5 else "k3")
    for (rx, ry) in ((5, 9), (26, 9), (5, 24), (26, 24)):
        cv.set(rx, ry, "b0")
    cv.outline("k0")
    return cv


def press_head(heat=0):
    """The press head seen from above-front: a riveted iron block on a piston."""
    cv = Canvas(32, 48)
    for y in range(0, 22):
        for x in range(13, 19):
            cv.mset(x, y, "char")          # the piston
    for y in range(22, 44):
        for x in range(3, 29):
            cv.mset(x, y, "rust")          # the block
    cv.shade()
    for x in range(3, 29):
        cv.set(x, 43, "k1")
        cv.set(x, 42, "o2" if (x // 2) % 2 == 0 else "k1")
    for (rx, ry) in ((6, 25), (25, 25), (6, 38), (25, 38), (15, 31)):
        cv.set(rx, ry, "b1")
    if heat:
        for x in range(8, 24):
            cv.set(x, 41, "o3")
    cv.outline("k0")
    return cv


# ---- Steam vent (A23): a floor grate, idle and venting -------------------------------------------------

def steam_vent(state=0):
    cv = Canvas(16, 16)
    for y in range(3, 13):
        for x in range(3, 13):
            cv.set(x, y, "k1" if (x % 2 == 0) else "k2")
    for x in range(2, 14):
        cv.set(x, 2, "k3")
        cv.set(x, 13, "k0")
    if state >= 1:          # warning: wisps rising from the slats
        for (x, y) in ((5, 6), (9, 4), (11, 8)):
            cv.set(x, y, "b0" if state == 1 else "b2")
    if state == 2:
        for (x, y) in ((4, 4), (7, 3), (10, 5), (12, 3), (6, 9), (8, 7)):
            cv.set(x, y, "b3")
    cv.outline("k0")
    return cv


# ---- Furnace (A23 "fours qui vacillent"): an iron furnace with a flickering mouth ---------------------

def furnace(frame=0):
    cv = Canvas(32, 40)
    for y in range(4, 38):
        w = 13 if y > 10 else 9 + (y - 4) * 0.6
        for x in range(32):
            if abs(x + 0.5 - 16) <= w:
                cv.mset(x, y, "char")
    for x in range(12, 21):
        for y in range(0, 6):
            cv.mset(x, y, "rust")          # the chimney stub
    cv.shade()
    # the mouth: a glowing arch whose brightness flickers
    glow = ("a1", "a2", "a1", "a0")[frame % 4]
    core = ("a2", "a3", "a2", "a1")[frame % 4]
    for y in range(22, 34):
        for x in range(9, 23):
            if (x + 0.5 - 16) ** 2 / 49 + (y - 34) ** 2 / 144 <= 1:
                cv.set(x, y, core if abs(x + 0.5 - 16) < 3 and y > 27 else glow, lock=False)
    for x in range(8, 24):
        cv.set(x, 34, "k0")
    for (rx, ry) in ((6, 14), (25, 14), (6, 30), (25, 30)):
        cv.set(rx, ry, "b0")
    cv.outline("k0")
    return cv


def build_enemies(out):
    images = []
    for sheet in (slagborn_sheet(), bellows_sheet()):
        images.append((sheet.name, sheet.save(out)))
    return images


def build_props(out):
    p = Sheet("Prop_Press", 32, 48, (16, 4))
    p.add_row("head", [press_head(0), press_head(1)])
    p.save(out)
    pl = Sheet("Prop_PressPlate", 32, 32, (16, 16))
    pl.add_row("plate", [press_plate()])
    pl.save(out)
    v = Sheet("Prop_SteamVent", 16, 16, (8, 8))
    v.add_row("idle", [steam_vent(0)])
    v.add_row("warn", [steam_vent(1), steam_vent(0)])
    v.add_row("vent", [steam_vent(2), steam_vent(1)])
    v.save(out)
    f = Sheet("Prop_Furnace", 32, 40, (16, 2))
    f.add_row("burn", [furnace(i) for i in range(4)])
    f.save(out)
    return ["Prop_Press", "Prop_PressPlate", "Prop_SteamVent", "Prop_Furnace"]


def build_vfx(out):
    s = soot_sheet()
    return s.name, s.save(out)
