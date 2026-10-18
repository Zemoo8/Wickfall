"""
Region tilesets (PDF §3 "Tilesets (16x16, répétables sans coupure)", cards A21-A26).

Every region gets, at 16x16:
  floor_0..3      four floor variants that tile seamlessly (periodic noise)
  top_<mask>      47 wall-top tiles (8-neighbour blob set: edges, outer and inner corners)
  face_<e>_<v>    wall front faces (3/4 view) for each left/right end combination, 2 variants
  shadow          a soft contact shadow placed on the floor under wall faces
  decor_*         region details on a separate layer (grass, roots, cracks...)
plus region specials (geyser vent, vines, conveyor, glass cracks, ash drifts...).
"""
from __future__ import annotations

import math
import random

from pixel import Canvas, Sheet

N, NE, E, SE, S, SW, W, NW = 1, 2, 4, 8, 16, 32, 64, 128


def reduce_mask(m: int) -> int:
    """Corner bits only count when both neighbouring edges are walls (the 47-tile blob rule)."""
    if not (m & N and m & E):
        m &= ~NE
    if not (m & E and m & S):
        m &= ~SE
    if not (m & S and m & W):
        m &= ~SW
    if not (m & W and m & N):
        m &= ~NW
    return m


BLOB_MASKS = sorted({reduce_mask(m) for m in range(256)})
assert len(BLOB_MASKS) == 47


_GRIDS = {}


def _grid(seed, cells):
    key = (seed, cells)
    if key not in _GRIDS:
        rnd = random.Random(seed * 977 + cells)
        _GRIDS[key] = [[rnd.uniform(-1, 1) for _ in range(cells)] for _ in range(cells)]
    return _GRIDS[key]


def _value(x, y, seed, cells):
    """Smooth value noise on a periodic grid: repeats every 16 pixels, no direction."""
    g = _grid(seed, cells)
    fx, fy = x / 16 * cells, y / 16 * cells
    x0, y0 = int(math.floor(fx)) % cells, int(math.floor(fy)) % cells
    x1, y1 = (x0 + 1) % cells, (y0 + 1) % cells
    tx, ty = fx - math.floor(fx), fy - math.floor(fy)
    sx, sy = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
    a = g[y0][x0] + (g[y0][x1] - g[y0][x0]) * sx
    b = g[y1][x0] + (g[y1][x1] - g[y1][x0]) * sx
    return a + (b - a) * sy


def periodic_noise(x, y, seed, octaves=((2, 1.0), (4, 0.5), (8, 0.25))):
    """Tileable noise (sum of periodic value-noise octaves), so tiles join without seams."""
    return sum(amp * _value(x, y, seed + i * 31, cells) for i, (cells, amp) in enumerate(octaves)) * 1.4


class Style:
    """A region's materials. Colours are palette names, dark -> light."""

    def __init__(self, key, floor, floor_accents, top, top_rim, face, outline="k0", seed=1):
        self.key = key
        self.floor = floor              # 3-4 tones for the ground
        self.floor_accents = floor_accents  # (colour, probability)
        self.top = top                  # 3 tones for wall tops
        self.top_rim = top_rim          # (lit rim, shadow rim)
        self.face = face                # 3 tones for wall faces
        self.outline = outline
        self.seed = seed


def floor_tile(style: Style, variant: int) -> Canvas:
    cv = Canvas(16, 16)
    rnd = random.Random(style.seed * 100 + variant)
    tones = style.floor
    for y in range(16):
        for x in range(16):
            # Large soft patches shared by every variant (so variants mix smoothly) + per-variant detail.
            n = periodic_noise(x, y, style.seed * 10, ((2, 1.0), (4, 0.4))) * 0.7 \
                + periodic_noise(x, y, style.seed * 10 + variant + 1, ((4, 0.5), (8, 0.3))) * 0.5
            k = (n + 1.2) / 2.4 * (len(tones) - 1)
            k += 0.18 if (x + y) % 2 == 0 else -0.18  # light ordered dither at tone borders
            idx = max(0, min(len(tones) - 1, int(round(k))))
            cv.set(x, y, tones[idx])
    for colour, prob in style.floor_accents:
        for _ in range(int(prob * 256)):
            cv.set(rnd.randrange(16), rnd.randrange(16), colour)
    return cv


def top_tile(style: Style, mask: int) -> Canvas:
    cv = Canvas(16, 16)
    lo, mid, hi = style.top
    lit, shade = style.top_rim
    for y in range(16):
        for x in range(16):
            n = periodic_noise(x, y, style.seed * 7 + 3, ((4, 0.7), (8, 0.4)))
            cv.set(x, y, hi if n > 0.75 else (mid if n > -0.85 else lo))
    o = style.outline
    if not mask & N:
        for x in range(16):
            cv.set(x, 0, o)
            cv.set(x, 1, lit)
    if not mask & S:
        for x in range(16):
            cv.set(x, 15, o)
            cv.set(x, 14, shade)
    if not mask & W:
        for y in range(16):
            cv.set(0, y, o)
            if cv.get(1, y) != cv.px[0, 0] or True:
                cv.set(1, y, lit if y > (1 if not mask & N else -1) else cv.get(1, y))
    if not mask & E:
        for y in range(16):
            cv.set(15, y, o)
            cv.set(14, y, shade)
    # outer corners stay outlined by the two edges; inner corners get a notch
    if mask & N and mask & W and not mask & NW:
        cv.set(0, 0, o); cv.set(1, 0, o); cv.set(0, 1, o); cv.set(1, 1, lit)
    if mask & N and mask & E and not mask & NE:
        cv.set(15, 0, o); cv.set(14, 0, o); cv.set(15, 1, o); cv.set(14, 1, shade)
    if mask & S and mask & W and not mask & SW:
        cv.set(0, 15, o); cv.set(1, 15, o); cv.set(0, 14, o)
    if mask & S and mask & E and not mask & SE:
        cv.set(15, 15, o); cv.set(14, 15, o); cv.set(15, 14, o)
    return cv


def face_tile(style: Style, west_open: bool, east_open: bool, variant: int, detail=None) -> Canvas:
    """The visible front of a wall (3/4 view): a lip of the top surface, then the darker face."""
    cv = Canvas(16, 16)
    lo, mid, hi = style.face
    tlo, tmid, thi = style.top
    lit, shade = style.top_rim
    rnd = random.Random(style.seed * 31 + variant)
    for y in range(16):
        for x in range(16):
            if y <= 3:
                cv.set(x, y, thi if y == 1 else (tmid if y < 3 else lit))
            else:
                n = periodic_noise(x, y, style.seed * 5 + variant, ((4, 0.8), (8, 0.4)))
                t = (y - 4) / 11
                col = mid if n > t * 1.4 - 0.5 else lo
                if y == 4:
                    col = hi
                cv.set(x, y, col)
    for x in range(16):
        cv.set(x, 0, style.outline)
        cv.set(x, 15, style.outline)
    if detail:
        detail(cv, rnd, variant)
    if west_open:
        for y in range(16):
            cv.set(0, y, style.outline)
    if east_open:
        for y in range(16):
            cv.set(15, y, style.outline)
            if 4 < y < 15:
                cv.set(14, y, lo)
    return cv


def shadow_tile() -> Canvas:
    cv = Canvas(16, 16)
    for y in range(3):
        for x in range(16):
            if y == 0 or (y == 1 and (x % 2 == 0)) or (y == 2 and x % 4 == 0):
                cv.set(x, y, "k0")
    return cv


# ---- Region 1: Champs de Suif (A21) ----------------------------------------------------------

TALLOW = Style(
    "tallow",
    floor=["b0", "b1", "b1", "b2"],
    floor_accents=[("o2", 0.012), ("b2", 0.012)],
    top=["b1", "b2", "b3"],
    top_rim=("b3", "b0"),
    face=["o1", "b0", "b1"],
    seed=1,
)


def tallow_face_detail(cv, rnd, variant):
    """Hardened wax drips running down the face."""
    for _ in range(3 + variant):
        x = rnd.randrange(2, 14)
        length = rnd.randrange(3, 9)
        for k in range(length):
            cv.set(x, 4 + k, "b2" if k < length - 1 else "b3")
            cv.set(x + 1, 4 + k, "b1") if x + 1 < 15 else None


def wax_grass(frame, variant):
    """2-frame swaying wax grass (decor layer)."""
    cv = Canvas(16, 16)
    rnd = random.Random(40 + variant)
    for _ in range(4):
        bx = rnd.randrange(2, 13)
        by = rnd.randrange(10, 15)
        h = rnd.randrange(3, 6)
        for k in range(h):
            sway = (1 if frame == 1 else 0) if k >= h - 2 else 0
            cv.set(bx + sway, by - k, "b3" if k >= h - 2 else "b2")
        cv.set(bx - 1, by, "b1")
    return cv


def wax_cracks(variant):
    cv = Canvas(16, 16)
    rnd = random.Random(70 + variant)
    x, y = rnd.randrange(3, 13), rnd.randrange(3, 13)
    for _ in range(7):
        cv.set(x, y, "b0")
        x += rnd.choice((-1, 0, 1))
        y += rnd.choice((0, 1))
    return cv


def geyser_vent(state, frame):
    """A21 special: a wax geyser vent, dormant / bubbling warning (glows) / erupting."""
    cv = Canvas(16, 16)
    for y in range(16):
        for x in range(16):
            r = math.hypot((x + 0.5 - 8) / 6.5, (y + 0.5 - 9) / 4.5)
            if r <= 1:
                cv.set(x, y, "b2" if r > 0.75 else ("b1" if r > 0.5 else "o1"))
    if state == "dormant":
        for x, y in ((7, 9), (8, 9), (8, 8)):
            cv.set(x, y, "o0")
    elif state == "warning":
        glow = ["o2", "a1"] if frame == 0 else ["a1", "a2"]
        for x, y in ((6, 9), (7, 9), (8, 9), (9, 9), (7, 8), (8, 8), (7, 10), (8, 10)):
            cv.set(x, y, glow[(x + y + frame) % 2])
        cv.set(5 + frame * 4, 7, "b3")
        cv.set(9 - frame * 3, 6, "b3")
    else:
        for x, y in ((6, 9), (7, 9), (8, 9), (9, 9), (7, 8), (8, 8), (7, 10), (8, 10)):
            cv.set(x, y, "b3")
        for k in range(4 + frame * 2):
            cv.set(8 + (k % 3) - 1, 7 - k, "b3" if k % 2 else "b2")
    cv.outline("k0")
    return cv


def wall_mask(grid, x, y):
    """8-neighbour mask of wall cells around (x, y); outside the map counts as wall."""
    h, w = len(grid), len(grid[0])

    def wall(cx, cy):
        return cx < 0 or cy < 0 or cx >= w or cy >= h or grid[cy][cx] == "#"

    m = 0
    for bit, (dx, dy) in ((N, (0, -1)), (NE, (1, -1)), (E, (1, 0)), (SE, (1, 1)), (S, (0, 1)),
                          (SW, (-1, 1)), (W, (-1, 0)), (NW, (-1, -1))):
        if wall(x + dx, y + dy):
            m |= bit
    return m


def render_map(style: Style, grid, face_detail=None, decor=None):
    """Draws an ASCII room with the same autotile rule the Unity level builder uses (preview only)."""
    from PIL import Image
    h, w = len(grid), len(grid[0])
    img = Image.new("RGBA", (w * 16, h * 16), (14, 13, 12, 255))
    floors = [floor_tile(style, v).image() for v in range(4)]
    tops = {m: top_tile(style, m).image() for m in BLOB_MASKS}
    faces = {}
    shadow = shadow_tile().image()
    rnd = random.Random(5)
    for y in range(h):
        for x in range(w):
            ch = grid[y][x]
            if ch != "#":
                img.alpha_composite(floors[(x * 7 + y * 13) % 4], (x * 16, y * 16))
                if y > 0 and grid[y - 1][x] == "#":
                    img.alpha_composite(shadow, (x * 16, y * 16))
                if decor and ch == "," :
                    img.alpha_composite(decor[rnd.randrange(len(decor))].image(), (x * 16, y * 16))
                continue
            m = wall_mask(grid, x, y)
            south_open = y + 1 < h and grid[y + 1][x] != "#"
            if south_open:
                west_open = x > 0 and grid[y][x - 1] != "#" or (x > 0 and y + 1 < h and grid[y + 1][x - 1] == "#" and False)
                key = (x > 0 and grid[y][x - 1] != "#", x + 1 < w and grid[y][x + 1] != "#", (x + y) % 2)
                if key not in faces:
                    faces[key] = face_tile(style, key[0], key[1], key[2], face_detail).image()
                img.alpha_composite(faces[key], (x * 16, y * 16))
            else:
                img.alpha_composite(tops[reduce_mask(m)], (x * 16, y * 16))
    return img


TEST_ROOM = [
    "####################",
    "#..,.....##.....,..#",
    "#........##........#",
    "#..###.......###...#",
    "#..#.#.......#.#.,.#",
    "#..###...,...###...#",
    "#...........,......#",
    "#,.....######......#",
    "#......#....#....,.#",
    "#..........,.......#",
    "#..,...............#",
    "####################",
]


REGIONS = {}


def region_sheets(style: Style, out, face_detail=None, decor=None, specials=None):
    sheet = Sheet("Tiles_" + style.key, 16, 16, (8, 8))
    sheet.add_row("floor", [floor_tile(style, v) for v in range(4)])
    tops = [top_tile(style, m) for m in BLOB_MASKS]
    sheet.add_row("top", tops)
    faces = []
    names = []
    for west in (False, True):
        for east in (False, True):
            for v in range(2):
                faces.append(face_tile(style, west, east, v, face_detail))
    sheet.add_row("face", faces)
    sheet.add_row("shadow", [shadow_tile()])
    if decor:
        for name, frames in decor:
            sheet.add_row("decor_" + name, frames)
    if specials:
        for name, frames in specials:
            sheet.add_row(name, frames)
    sheet.extra = {"blobMasks": {str(i): [m, 0] for i, m in enumerate(BLOB_MASKS)}}
    return sheet.save(out)


def build_tallow(out):
    return region_sheets(
        TALLOW, out, tallow_face_detail,
        decor=[("grass0", [wax_grass(0, 0), wax_grass(1, 0)]), ("grass1", [wax_grass(0, 1), wax_grass(1, 1)]),
               ("crack", [wax_cracks(i) for i in range(3)])],
        specials=[("geyser_dormant", [geyser_vent("dormant", 0)]),
                  ("geyser_warning", [geyser_vent("warning", 0), geyser_vent("warning", 1)]),
                  ("geyser_erupt", [geyser_vent("erupt", 0), geyser_vent("erupt", 1)])],
    )
