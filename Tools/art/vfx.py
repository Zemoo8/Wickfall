"""
Visual effects. The six PDF sheets (8 frames each, PDF §3 "Effets visuels"):
  ember trail 16x16, wax drip hitting the ground 16x16, extinguish burst (radial) 64x64,
  flare burst upward on flame swap 32x64, mirror laser beam segment 16x16, soot hit splash 32x32
plus the effects of the flame attacks and arts (not in the PDF, logged as G27).
Tintable effects are drawn in bone-white levels; the engine multiplies them by the flame colour.
"""
from __future__ import annotations

import math
import random

from pixel import Canvas, Sheet

BRIGHT, MID, DIM = "b3", "b2", "b1"


def ring(cv, cx, cy, r0, r1, col, a0=0.0, a1=360.0, dither=0.0, rnd=None):
    for y in range(cv.h):
        for x in range(cv.w):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            r = math.hypot(dx, dy)
            if r0 <= r <= r1:
                a = math.degrees(math.atan2(-dy, dx)) % 360
                lo, hi = a0 % 360, a1 % 360
                inside = (lo <= a <= hi) if lo <= hi else (a >= lo or a <= hi)
                if a1 - a0 >= 360 or inside:
                    if dither and rnd is not None and rnd.random() < dither:
                        continue
                    cv.set(x, y, col)


def disc(cv, cx, cy, r, col, dither=0.0, rnd=None):
    ring(cv, cx, cy, 0, r, col, dither=dither, rnd=rnd)


def slash(frame):
    """A crescent of fire sweeping in front (pointing right): build up, full, breaking into embers."""
    cv = Canvas(48, 48)
    rnd = random.Random(frame * 7 + 1)
    span = [50, 95, 120, 120, 120, 120][frame]
    start = -60
    dither = [0, 0, 0, 0.35, 0.65, 0.85][frame]
    ring(cv, 20, 24, 15, 21, DIM, start, start + span, dither, rnd)
    ring(cv, 20, 24, 17, 20, MID, start + 10, start + span, dither, rnd)
    if frame <= 3:
        ring(cv, 20, 24, 18, 19.4, BRIGHT, start + span * 0.4, start + span, dither * 0.5, rnd)
    if frame >= 3:
        for _ in range(6 + frame * 2):
            a = math.radians(rnd.uniform(-60, 60))
            r = rnd.uniform(16, 24 + frame)
            cv.set(20 + math.cos(a) * r, 24 - math.sin(a) * r, BRIGHT if rnd.random() < 0.4 else MID)
    return cv


def wide_sweep(frame):
    cv = Canvas(64, 64)
    rnd = random.Random(frame + 11)
    r = 8 + frame * 3.2
    d = max(0.0, (frame - 4) * 0.2)
    ring(cv, 32, 32, r - 3, r, DIM, dither=d, rnd=rnd)
    ring(cv, 32, 32, r - 2, r - 0.5, MID, dither=d, rnd=rnd)
    if frame < 6:
        ring(cv, 32, 32, r - 1.2, r - 0.4, BRIGHT, dither=d, rnd=rnd)
    return cv


def radiant_pulse(frame):
    cv = Canvas(64, 64)
    rnd = random.Random(frame + 21)
    r = 6 + frame * 3.4
    d = max(0.0, (frame - 3) * 0.18)
    ring(cv, 32, 32, r - 2, r, MID, dither=d, rnd=rnd)
    ring(cv, 32, 32, r - 1, r, BRIGHT, dither=d, rnd=rnd)
    for k in range(8):
        a = math.radians(k * 45 + frame * 4)
        for t in range(int(r * 0.4), int(r + 4)):
            if rnd.random() > d:
                cv.set(32 + math.cos(a) * t, 32 + math.sin(a) * t, BRIGHT if t > r - 2 else MID)
    if frame < 3:
        disc(cv, 32, 32, 6 - frame * 2, BRIGHT)
    return cv


def poison_cloud(frame):
    cv = Canvas(48, 48)
    rnd = random.Random(5)
    grow = 0.5 + frame * 0.12
    fade = max(0.0, (frame - 4) * 0.22)
    blobs = [(24, 24, 9), (16, 22, 6), (32, 26, 7), (22, 31, 6), (28, 17, 5), (14, 29, 4), (34, 32, 4)]
    for bx, by, br in blobs:
        disc(cv, 24 + (bx - 24) * grow, 24 + (by - 24) * grow, br * grow, DIM, fade, random.Random(bx + frame))
    for bx, by, br in blobs[:4]:
        disc(cv, 24 + (bx - 24) * grow, 24 + (by - 24) * grow - 1, br * grow * 0.6, MID, fade, random.Random(by + frame))
    for _ in range(8):
        x, y = rnd.uniform(8, 40), rnd.uniform(8, 40)
        if rnd.random() > fade:
            cv.set(x, y - frame, BRIGHT)
    return cv


def crimson_strike(frame):
    cv = Canvas(48, 48)
    rnd = random.Random(frame + 31)
    length = [10, 20, 24, 24, 24, 24][frame]
    d = [0, 0, 0, 0.3, 0.6, 0.85][frame]
    for off, slope in ((0, 0.55), (3, -0.55)):
        for t in range(-length, length):
            x = 24 + t
            y = 24 + t * slope + off - 1.5
            for w in range(-1, 2):
                if rnd.random() > d:
                    cv.set(x, y + w, BRIGHT if w == 0 else MID)
    if frame >= 2:
        for _ in range(10):
            a = rnd.uniform(0, math.tau)
            r = rnd.uniform(4, 10 + frame * 2)
            if rnd.random() > d:
                cv.set(24 + math.cos(a) * r, 24 + math.sin(a) * r, MID)
    return cv


def ember_trail(frame):
    """PDF: ember trail, 16x16, 8 frames."""
    cv = Canvas(16, 16)
    rnd = random.Random(3)
    for i in range(6):
        x = 8 + rnd.uniform(-4, 4)
        y = 12 - frame * 1.2 - i * 0.6 + rnd.uniform(-1, 1)
        if i < 8 - frame:
            cv.set(x, y, BRIGHT if i % 3 == 0 else MID)
    return cv


def wax_drip(frame):
    """PDF: wax drip hitting the ground, 16x16, 8 frames (in wax colours, not tinted)."""
    cv = Canvas(16, 16)
    if frame < 3:
        y = 2 + frame * 4
        cv.set(8, y, "b3")
        cv.set(8, y + 1, "b2")
        cv.set(7, y + 1, "b2") if frame == 2 else None
    else:
        spread = frame - 2
        for x in range(8 - spread, 9 + spread):
            cv.set(x, 13, "b2" if abs(x - 8) < spread else "b1")
        if frame < 6:
            cv.set(8 - spread - 1, 11 - (frame - 3), "b3")
            cv.set(8 + spread + 1, 11 - (frame - 3), "b3")
        cv.set(8, 12, "b3") if frame < 5 else None
    return cv


def extinguish_burst(frame):
    """PDF: extinguish burst (radial), 64x64, 8 frames: embers fly out, smoke rings spread."""
    cv = Canvas(64, 64)
    rnd = random.Random(9)
    r = 4 + frame * 3.5
    d = max(0.0, (frame - 3) * 0.2)
    ring(cv, 32, 32, r - 2, r, "k3", dither=0.3 + d, rnd=random.Random(frame))
    ring(cv, 32, 32, r - 4, r - 2, "b0", dither=0.6 + d * 0.5, rnd=random.Random(frame + 5))
    for k in range(14):
        a = k / 14 * math.tau + rnd.uniform(-0.2, 0.2)
        dist = r * rnd.uniform(0.8, 1.25)
        if rnd.random() > d:
            cv.set(32 + math.cos(a) * dist, 32 + math.sin(a) * dist, "a2" if frame < 4 else "a1")
    if frame < 2:
        disc(cv, 32, 32, 4 - frame * 2, "a3")
    return cv


def flare_burst(frame):
    """PDF: flare burst upward on flame swap, 32x64, 8 frames (amber ramp in the art; tinted in engine)."""
    cv = Canvas(32, 64)
    rnd = random.Random(13)
    h = min(60, 12 + frame * 9)
    fade = max(0.0, (frame - 4) * 0.25)
    for y in range(64 - h, 64):
        t = (y - (64 - h)) / max(1, h)
        half = 1 + 7 * t * (1 - fade)
        for x in range(32):
            d = abs(x + 0.5 - 16)
            if d <= half and rnd.random() > fade:
                cv.set(x, y, BRIGHT if d < half * 0.35 else MID if d < half * 0.7 else DIM)
    for _ in range(10):
        x, y = rnd.uniform(6, 26), rnd.uniform(64 - h - 6, 60)
        if rnd.random() > fade:
            cv.set(x, y, BRIGHT)
    return cv


def laser_segment(frame):
    """PDF: mirror laser beam segment, 16x16, tileable along the beam (horizontal), shimmering."""
    cv = Canvas(16, 16)
    for x in range(16):
        wob = 1 if (x + frame) % 8 < 4 else 0
        for y in range(6, 10):
            cv.set(x, y, BRIGHT if 7 <= y <= 8 else MID)
        cv.set(x, 5 + wob, DIM)
        cv.set(x, 10 - wob, DIM)
    for x in range(frame * 2 % 16, 16, 8):
        cv.set(x, 7, "w1")
    return cv


def soot_splash(frame):
    """PDF: soot hit splash, 32x32, 8 frames (charcoal, not tinted)."""
    cv = Canvas(32, 32)
    rnd = random.Random(17)
    r = 2 + frame * 1.8
    d = max(0.0, (frame - 3) * 0.2)
    for k in range(10):
        a = k / 10 * math.tau + rnd.uniform(-0.3, 0.3)
        dist = r * rnd.uniform(0.7, 1.2)
        disc(cv, 16 + math.cos(a) * dist, 16 + math.sin(a) * dist, max(0.8, 2.5 - frame * 0.25), "k2", d, random.Random(k + frame))
    disc(cv, 16, 16, max(0.0, 4 - frame), "k1")
    for k in range(5):
        a = k / 5 * math.tau
        if rnd.random() > d:
            cv.set(16 + math.cos(a) * (r + 2), 16 + math.sin(a) * (r + 2), "k3")
    return cv


def hit_spark(frame):
    cv = Canvas(16, 16)
    size = [3, 6, 5, 3][frame]
    for k in range(4):
        a = k * math.pi / 2 + math.pi / 4 * (frame % 2)
        for t in range(size):
            cv.set(8 + math.cos(a) * t, 8 + math.sin(a) * t, BRIGHT if t < size - 1 else MID)
    return cv


def death_puff(frame):
    cv = Canvas(32, 32)
    d = max(0.0, (frame - 3) * 0.2)
    for i, (ox, oy, r) in enumerate([(16, 18, 6), (11, 15, 4), (21, 14, 4), (16, 11, 3)]):
        rr = r * (0.5 + frame * 0.12)
        disc(cv, ox, oy - frame * 0.8, rr, "b0" if i % 2 else "k3", d, random.Random(i + frame))
        disc(cv, ox - 1, oy - frame * 0.8 - 1, rr * 0.5, "b1", d + 0.2, random.Random(i * 3 + frame))
    return cv


def dust_puff(frame):
    cv = Canvas(16, 16)
    d = max(0.0, (frame - 2) * 0.25)
    for i, (ox, oy) in enumerate([(5, 11), (9, 12), (12, 10)]):
        disc(cv, ox - frame * 0.5, oy - frame * 0.4, 1 + frame * 0.4, "b1" if i % 2 else "b0", d, random.Random(i + frame))
    return cv


def geyser_burst(frame):
    """The wax geyser's eruption column (32x48)."""
    cv = Canvas(32, 48)
    rnd = random.Random(23)
    h = [10, 26, 40, 46, 44, 36, 24, 12][frame]
    for y in range(48 - h, 48):
        t = (y - (48 - h)) / max(1, h)
        half = 2 + 5 * t
        wob = math.sin(y * 0.6 + frame) * 1.2
        for x in range(32):
            d = abs(x + 0.5 - 16 - wob)
            if d <= half:
                cv.set(x, y, "b3" if d < half * 0.4 else "b2" if d < half * 0.75 else "b1")
    for _ in range(8 if frame > 1 else 0):
        a = rnd.uniform(-math.pi * 0.9, -math.pi * 0.1)
        dist = rnd.uniform(4, 10 + frame)
        cv.set(16 + math.cos(a) * dist * 1.3, 48 - h + 4 + math.sin(a) * dist, "b3")
    cv.outline("k0")
    return cv


def relight_burst(frame):
    cv = Canvas(32, 32)
    r = 2 + frame * 2
    d = max(0.0, (frame - 4) * 0.25)
    ring(cv, 16, 16, r - 1.5, r, MID, dither=d, rnd=random.Random(frame))
    for k in range(6):
        a = k / 6 * math.tau + frame * 0.2
        cv.set(16 + math.cos(a) * (r + 1), 16 + math.sin(a) * (r + 1), BRIGHT)
    return cv


def pickup_glint(frame):
    cv = Canvas(8, 8)
    s = [1, 3, 2, 1][frame]
    for t in range(-s, s + 1):
        cv.set(4 + t, 4, BRIGHT)
        cv.set(4, 4 + t, BRIGHT)
    return cv


def bolt(frame):
    """The blue flame's bolt (16x8): a bright teardrop pointing right with a flickering tail."""
    cv = Canvas(16, 8)
    for x in range(16):
        t = x / 15
        half = 2.6 * math.sin(t * math.pi * 0.9) if t > 0.2 else 0.8 * t * 5 * (0.6 + 0.4 * ((x + frame) % 2))
        for y in range(8):
            d = abs(y + 0.5 - 4)
            if d <= half:
                cv.set(x, y, BRIGHT if d < half * 0.45 and t > 0.4 else MID)
    return cv


def glass_shards(frame):
    cv = Canvas(32, 32)
    rnd = random.Random(29)
    for k in range(12):
        a = rnd.uniform(0, math.tau)
        dist = 2 + frame * rnd.uniform(1.2, 2.2)
        x, y = 16 + math.cos(a) * dist, 16 + math.sin(a) * dist + frame * 0.6
        if frame < 7:
            cv.set(x, y, "b3")
            cv.set(x + 1, y, "k2")
    return cv


def steam_burst(frame):
    cv = Canvas(32, 48)
    d = max(0.0, (frame - 4) * 0.2)
    for i in range(5):
        r = 3 + frame * 0.6 + i * 0.3
        disc(cv, 16 + math.sin(i * 1.7) * 3, 44 - i * 7 - frame * 1.5, r, "b2" if i % 2 else "b1", d + i * 0.08,
             random.Random(i * 5 + frame))
    return cv


def alert_mark(frame):
    cv = Canvas(8, 12)
    for y in range(1, 8):
        cv.set(3, y, "a2")
        cv.set(4, y, "a1")
    cv.set(3, 10, "a2")
    cv.set(4, 10, "a1")
    if frame == 1:
        cv = cv.shifted(0, -1)
    cv.outline("k0")
    return cv


def void_burst(frame):
    cv = Canvas(32, 32)
    r = 3 + frame * 1.8
    d = max(0.0, (frame - 4) * 0.25)
    disc(cv, 16, 16, r, "k0", d, random.Random(frame))
    ring(cv, 16, 16, r, r + 1, "b2", dither=0.3 + d, rnd=random.Random(frame + 3))
    return cv


def ash_wave(frame):
    cv = Canvas(64, 32)
    rnd = random.Random(37)
    for i in range(40):
        x = rnd.uniform(0, 64)
        y = 28 - abs(math.sin((x / 64) * math.pi)) * (6 + frame * 2) + rnd.uniform(-2, 2)
        if rnd.random() > max(0.0, (frame - 4) * 0.2):
            cv.set(x, y, "b1" if i % 2 else "b0")
    return cv


SHEETS = [
    # name, size, pivot, frames, generator, fps
    ("VFX_Slash", (48, 48), (20, 24), 6, slash, 20),
    ("VFX_WideSweep", (64, 64), (32, 32), 8, wide_sweep, 20),
    ("VFX_RadiantPulse", (64, 64), (32, 32), 8, radiant_pulse, 18),
    ("VFX_PoisonCloud", (48, 48), (24, 24), 8, poison_cloud, 12),
    ("VFX_CrimsonStrike", (48, 48), (24, 24), 6, crimson_strike, 18),
    ("VFX_EmberTrail", (16, 16), (8, 8), 8, ember_trail, 16),
    ("VFX_WaxDrip", (16, 16), (8, 3), 8, wax_drip, 16),
    ("VFX_ExtinguishBurst", (64, 64), (32, 32), 8, extinguish_burst, 12),
    ("VFX_Flare", (32, 64), (16, 0), 8, flare_burst, 16),
    ("VFX_LaserSegment", (16, 16), (0, 8), 8, laser_segment, 16),
    ("VFX_SootSplash", (32, 32), (16, 16), 8, soot_splash, 16),
    ("VFX_HitSpark", (16, 16), (8, 8), 4, hit_spark, 24),
    ("VFX_DeathPuff", (32, 32), (16, 10), 8, death_puff, 14),
    ("VFX_DustPuff", (16, 16), (8, 4), 6, dust_puff, 14),
    ("VFX_GeyserBurst", (32, 48), (16, 0), 8, geyser_burst, 16),
    ("VFX_RelightBurst", (32, 32), (16, 16), 8, relight_burst, 16),
    ("VFX_PickupGlint", (8, 8), (4, 4), 4, pickup_glint, 16),
    ("VFX_Bolt", (16, 8), (12, 4), 4, bolt, 16),
    ("VFX_GlassShards", (32, 32), (16, 16), 8, glass_shards, 14),
    ("VFX_SteamBurst", (32, 48), (16, 0), 8, steam_burst, 12),
    ("VFX_AlertMark", (8, 12), (4, 0), 2, alert_mark, 6),
    ("VFX_VoidBurst", (32, 32), (16, 16), 8, void_burst, 14),
    ("VFX_AshWave", (64, 32), (32, 4), 8, ash_wave, 12),
]


def build(out):
    images = []
    for name, (w, h), pivot, count, gen, fps in SHEETS:
        sheet = Sheet(name, w, h, pivot)
        sheet.add_row("fx", [gen(i) for i in range(count)])
        sheet.extra = {"fps": {"fx": fps}}
        images.append((name, sheet.save(out)))
    return images
