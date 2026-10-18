"""
Regions 4-5 art: Cathédrale de Verre and Creux Profond.

PDF / prompt cards: A14 Reflection (32x40, mirror-black copy of the Wickbearer, glitchy idle), A15 the
Unlit (32x32, 6-frame drift), A24 / A25 tiles, A32 crystal pedestal (8 rotations), A38e beam.
Not in PDF (G27): black mirrors, beam sockets, light lamp and laser emitters, the Rose and its panels,
glass barriers, glass floor (A24's cracked variant in 2 stages + the broken hole).
"""
from __future__ import annotations

import json
import math
import os
import random

import numpy as np
from PIL import Image

from pixel import Canvas, Sheet, NAMES, RGBA

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
_LOOKUP = {tuple(c): i for i, c in enumerate(RGBA)}


def _canvas_from(img):
    cv = Canvas(img.width, img.height)
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            c = px[x, y]
            if c[3] > 0:
                cv.set(x, y, _LOOKUP.get(tuple(c), _LOOKUP.get((c[0], c[1], c[2], 255), 1)), lock=False)
    return cv


def _player_frames(clip):
    folder = os.path.join(ROOT, "Assets", "Art", "Generated", "Player")
    meta = json.load(open(os.path.join(folder, "Player_Body.json")))
    sheet = Image.open(os.path.join(folder, "Player_Body.png")).convert("RGBA")
    frames = {f["name"]: f for f in meta["frames"]}
    out = []
    for name in meta["clips"][clip]:
        f = frames[name]
        top = sheet.height - f["y"] - f["h"]
        out.append(_canvas_from(sheet.crop((f["x"], top, f["x"] + f["w"], top + f["h"]))))
    return out, meta


# ---- A14 Reflection: the Wickbearer in polished black mirror glass -----------------------------------

def mirror_black(src: Canvas, glitch=0, seed=0, anchor=(16, 31)):
    cv = Canvas(src.w, src.h)
    for y in range(src.h):
        for x in range(src.w):
            if src.get(x, y):
                cv.set(x, y, "k0")
    # pale bone-white highlights tracing the edges (light from the top left), the rest charcoal
    for y in range(src.h):
        for x in range(src.w):
            if not cv.get(x, y):
                continue
            if not cv.get(x - 1, y) or not cv.get(x, y - 1):
                cv.set(x, y, "b2")
            elif not cv.get(x + 1, y) or not cv.get(x, y + 1):
                cv.set(x, y, "k2")
    # two empty white-outlined eyes where the face is
    ys = [y for y in range(src.h) for x in range(src.w) if src.get(x, y)]
    if ys:
        face_y = min(ys) + 7
        for ex in (13, 18):
            cv.set(ex, face_y, "w0")
            cv.set(ex, face_y + 1, "k0")
    # a black flame with a pale rim on its head
    ax, ay = anchor
    fx, fy = int(16 + ax), src.h - 2 - int(ay)
    for k, (dx0, dx1) in enumerate(((0, 0), (-1, 1), (-1, 1), (-2, 1), (-1, 1))):
        for dx in range(dx0, dx1 + 1):
            edge = dx in (dx0, dx1) or k == 0
            cv.set(fx + dx, fy + k, "b1" if edge else "k0")
    if glitch:
        rnd = random.Random(seed)
        for _ in range(2):
            y0 = rnd.randrange(8, src.h - 6)
            shift = rnd.choice((-2, -1, 1, 2))
            band = [[cv.px[y, x] for x in range(src.w)] for y in range(y0, y0 + 2)]
            for i, y in enumerate(range(y0, y0 + 2)):
                for x in range(src.w):
                    v = band[i][x - shift] if 0 <= x - shift < src.w else 0
                    cv.px[y, x] = v
    return cv


def reflection_sheet():
    idle, meta = _player_frames("idle_down")
    walk, _ = _player_frames("walk_down")
    attack, _ = _player_frames("attack_down")
    hurt, _ = _player_frames("hurt")
    anchor = tuple(meta["flameAnchor"]["down"])
    s = Sheet("Enemy_Reflection", 32, 40, (16, 2))
    s.add_row("idle", [mirror_black(idle[i % 2], glitch=i in (1, 3), seed=i, anchor=anchor) for i in range(4)])  # A14: glitchy idle
    s.add_row("move", [mirror_black(f, anchor=anchor) for f in walk])
    s.add_row("windup", [mirror_black(attack[0], glitch=1, seed=7, anchor=anchor), mirror_black(attack[1], anchor=anchor)])
    s.add_row("attack", [mirror_black(f, anchor=anchor) for f in attack[2:]])
    s.add_row("hurt", [mirror_black(f, glitch=1, seed=11 + i, anchor=anchor) for i, f in enumerate(hurt)])
    s.add_row("death", [_shatter(mirror_black(idle[0], anchor=anchor), i) for i in range(5)])
    s.extra = {"fps": {"idle": 5, "move": 10, "windup": 8, "attack": 14, "hurt": 10, "death": 10}}
    return s


def _shatter(cv, step):
    out = cv.copy()
    rnd = random.Random(40 + step)
    for y in range(out.h):
        for x in range(out.w):
            if out.get(x, y) and rnd.random() < step * 0.2:
                out.erase(x, y)
    return out.shifted(0, step)


# ---- A15 the Unlit: a hole cut out of the world, a faint bone outline -------------------------------

def unlit(frame):
    """A hunched figure that once carried a lantern, now only a hole with a faint outline."""
    cv = Canvas(32, 32)
    t = frame / 6 * 2 * math.pi
    lean = math.sin(t) * 2.0
    for y in range(32):
        for x in range(32):
            sway = lean * (1 - y / 32)
            u = (x + 0.5 - 14 - sway) / 6.5
            v = (y + 0.5 - 20) / 10
            body = u * u + v * v < 1 + 0.1 * math.sin(t + y * 0.5)
            head = (x + 0.5 - 12 - sway * 1.3) ** 2 + (y + 0.5 - 8) ** 2 < 3.6 ** 2
            if body or head:
                cv.set(x, y, "k0")
    # a drooping arm and the lantern it still holds (dark: nothing burns in it)
    sx, sy = 19 + lean * 0.6, 13
    ex, ey = 24 + lean * 0.4, 19 + int(round(math.sin(t)))
    for k in range(8):
        f = k / 7
        cv.set(sx + (ex - sx) * f, sy + (ey - sy) * f, "k0")
        cv.set(sx + (ex - sx) * f + 1, sy + (ey - sy) * f, "k0")
    for y in range(int(ey) + 1, int(ey) + 6):
        for x in range(int(ex) - 2, int(ex) + 2):
            cv.set(x, y, "k0")
    out = cv.copy()
    for y in range(32):
        for x in range(32):
            if cv.get(x, y) and (not cv.get(x - 1, y) or not cv.get(x + 1, y) or not cv.get(x, y - 1) or not cv.get(x, y + 1)):
                out.set(x, y, "b0")
    # wisps of the outline peeling away on the trailing side
    rnd = random.Random(frame)
    for k in range(3):
        wx = int(7 - lean * 2 - k * 2 + rnd.randrange(-1, 2))
        wy = int(10 + k * 6 + math.sin(t + k) * 2)
        out.set(wx, wy, "b0")
        out.set(wx - 1, wy - 1, "k3")
    return out


def unlit_sheet():
    s = Sheet("Enemy_Unlit", 32, 32, (16, 2))
    frames = [unlit(i) for i in range(6)]
    s.add_row("move", frames)                         # PDF: drifting, 6 frames
    s.add_row("idle", frames[::2])
    s.add_row("death", [_shatter(frames[0], i) for i in range(5)])
    s.extra = {"fps": {"move": 6, "idle": 3, "death": 8}}
    return s


# ---- A32 crystal pedestal (8 rotations), mirrors, sockets, emitters -------------------------------------

def pedestal(step):
    cv = Canvas(32, 32)
    for y in range(20, 30):
        for x in range(9, 23):
            cv.set(x, y, "k3" if y == 20 else "k2" if x < 16 else "k1")
    cv.ellipse(16, 14, 5.5, 6.5, "ash")
    cv.shade()
    a = step * math.pi / 4
    dx, dy = math.cos(a), -math.sin(a)
    for k in range(2, 9):                              # the bright facet that points the way
        cv.set(16 + dx * k, 14 + dy * k * 0.8, "b3" if k > 5 else "b2")
    cv.set(16, 14, "w0")
    cv.outline("k0")
    return cv


def black_mirror(slash):
    cv = Canvas(32, 32)
    for i in range(-11, 12):
        for w in range(-2, 3):
            x = 16 + i + (w if slash else -w)
            y = 16 - i if slash else 16 + i
            y += w * 0
            cv.set(x, y, "k0" if abs(w) < 2 else "k2")
    for i in range(-10, 11, 3):                        # pale glints on the face
        cv.set(16 + i, (16 - i) if slash else (16 + i), "b2")
    for x in range(12, 21):                            # a small iron foot
        cv.set(x, 29, "k2")
        cv.set(x, 30, "k1")
    cv.outline("k0")
    return cv


def socket(lit):
    cv = Canvas(16, 16)
    for y in range(2, 15):
        for x in range(2, 14):
            cv.set(x, y, "k3" if y < 4 else "k2")
    cv.ellipse(8, 9, 3.5, 3.5, "ash")
    cv.shade()
    if lit:
        cv.ellipse(8, 9, 2.5, 2.5, "white")
        cv.set(8, 9, "w1")
    cv.outline("k0")
    return cv


def lamp(laser):
    cv = Canvas(16, 16)
    for y in range(3, 15):
        for x in range(3, 13):
            cv.set(x, y, "k2" if x < 8 else "k1")
    cv.ellipse(8, 9, 3, 3, "amber" if laser else "bone")
    cv.set(8, 9, "a3" if laser else "w0")
    cv.outline("k0")
    return cv


# ---- the Rose (G23) -----------------------------------------------------------------------------------------

def rose_frame():
    cv = Canvas(80, 80)
    for y in range(80):
        for x in range(80):
            r = math.hypot(x + 0.5 - 40, y + 0.5 - 40)
            if 34 <= r <= 38:
                cv.set(x, y, "k3" if r < 35 else "k2")
            elif r < 9:
                cv.set(x, y, "k2" if r > 7 else "k1")   # the eye's setting
    for k in range(5):                                    # stone mullions between the petals
        a = -math.pi / 2 + (k + 0.5) * 2 * math.pi / 5
        for r in range(9, 35):
            cv.set(40 + math.cos(a) * r, 40 + math.sin(a) * r, "k3")
    cv.outline("k0")
    return cv


def rose_panel():
    """One petal of glass, drawn pale so the engine can tint it with its flame's colour."""
    cv = Canvas(24, 24)
    for y in range(24):
        for x in range(24):
            u, v = (x + 0.5 - 12) / 9, (y + 0.5 - 13) / 11
            if u * u + v * v < 1 and y > 2:
                cv.set(x, y, "w0" if (x + y) % 5 else "b3")
    for y in range(4, 22, 4):
        for x in range(4, 20):
            if cv.get(x, y):
                cv.set(x, y, "b1")                        # leading between the glass pieces
    cv.outline("k0")
    return cv


# ---- glass: barrier and floor (A24) -----------------------------------------------------------------------

def glass_barrier():
    cv = Canvas(16, 32)
    for y in range(2, 31):
        for x in range(2, 14):
            cv.set(x, y, "k0" if (x + y) % 7 else "k1")
    for i in range(3, 13):
        cv.set(2 + i // 2, 31 - i * 2, "b1")              # a pale diagonal glint
    for y in range(2, 31):
        cv.set(2, y, "b0")
        cv.set(13, y, "k2")
    for x in range(2, 14):
        cv.set(x, 2, "b2")
    cv.outline("k0")
    return cv


def glass_floor(stage):
    cv = Canvas(16, 16)
    if stage == 3:                                         # broken: a pit into the dark below
        for y in range(16):
            for x in range(16):
                edge = min(x, y, 15 - x, 15 - y)
                if edge > (1 + ((x * 7 + y * 3) % 3)):
                    cv.set(x, y, "k0")
                elif edge > 0:
                    cv.set(x, y, "k2")
        return cv
    for y in range(16):
        for x in range(16):
            cv.set(x, y, "k1" if (x // 4 + y // 4) % 2 else "k0")
            if x in (0, 15) or y in (0, 15):
                cv.set(x, y, "k2")
    for i in range(2, 14, 4):
        cv.set(i, 15 - i, "b0")
    rnd = random.Random(90)
    if stage >= 1:
        x, y = 8, 8
        for _ in range(9 if stage == 1 else 18):
            cv.set(x, y, "b1" if stage == 1 else "b2")
            x = max(1, min(14, x + rnd.choice((-1, 0, 1))))
            y = max(1, min(14, y + rnd.choice((-1, 0, 1))))
    return cv


def beam_core(path):
    """A38e beam cross-section: stretched along the beam by the LineRenderer (pale core, soft edges)."""
    alpha = [0, 50, 140, 230, 255, 255, 230, 140, 50, 0]
    img = Image.new("RGBA", (4, len(alpha)))
    for y, a in enumerate(alpha):
        for x in range(4):
            img.putpixel((x, y), (255, 255, 255, a))
    img.save(path)
    return path


# ---- tiles (A24, A25) are in tiles.py; sheets ------------------------------------------------------------

def build_enemies(out):
    images = []
    for sheet in (reflection_sheet(), unlit_sheet()):
        images.append((sheet.name, sheet.save(out)))
    return images


def build_props(out):
    names = []
    p = Sheet("Prop_Pedestal", 32, 32, (16, 4))
    p.add_row("rot", [pedestal(i) for i in range(8)])
    names.append(p.name); p.save(out)
    m = Sheet("Prop_Mirror", 32, 32, (16, 2))
    m.add_row("mirror", [black_mirror(True), black_mirror(False)])
    names.append(m.name); m.save(out)
    s = Sheet("Prop_Socket", 16, 16, (8, 8))
    s.add_row("socket", [socket(False), socket(True)])
    names.append(s.name); s.save(out)
    l = Sheet("Prop_Lamp", 16, 16, (8, 8))
    l.add_row("lamp", [lamp(False), lamp(True)])
    names.append(l.name); l.save(out)
    r = Sheet("Prop_Rose", 80, 80, (40, 40))
    r.add_row("frame", [rose_frame()])
    names.append(r.name); r.save(out)
    rp = Sheet("Prop_RosePanel", 24, 24, (12, 12))
    rp.add_row("panel", [rose_panel()])
    names.append(rp.name); rp.save(out)
    g = Sheet("Prop_GlassBarrier", 16, 32, (8, 2))
    g.add_row("glass", [glass_barrier()])
    names.append(g.name); g.save(out)
    gf = Sheet("Prop_GlassFloor", 16, 16, (8, 8))
    gf.add_row("floor", [glass_floor(i) for i in range(4)])
    names.append(gf.name); gf.save(out)
    return names


def build_vfx(out):
    os.makedirs(out, exist_ok=True)
    return beam_core(os.path.join(out, "Beam_Core.png"))
