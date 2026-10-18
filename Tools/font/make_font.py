"""
WICKFALL's pixel font ("Wickfall Pixel"), drawn for this game and released as CC0.

Prompt §12 asks for a pixel font with full French coverage (é è ê ë à â ç î ï ô û ù œ « » ’) and
warns that ECHO's font drew K and X almost like H, so K, X and H are drawn to be unmistakable.
Every glyph is a 5x7 (or narrower) bitmap; the script turns the pixels into a TrueType font
with fontTools. 1 pixel = 100 font units, em = 12 pixels.

    python Tools/font/make_font.py   ->  Assets/Art/Fonts/WickfallPixel.ttf + preview PNG
"""
import os

from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Art", "Fonts")
UNIT = 100

# Each glyph: 7 rows (row 0 = top of the ascender/cap zone, row 6 = sits on the baseline),
# optional 2 descender rows, optional 2 rows above the caps (accents on capitals).
G = {}


def g(ch, rows, desc=None, top=None):
    G[ch] = (rows.split(), desc.split() if desc else None, top.split() if top else None)


# ---- capitals -------------------------------------------------------------------------------
g("A", ".###. #...# #...# ##### #...# #...# #...#")
g("B", "####. #...# #...# ####. #...# #...# ####.")
g("C", ".###. #...# #.... #.... #.... #...# .###.")
g("D", "####. #...# #...# #...# #...# #...# ####.")
g("E", "##### #.... #.... ####. #.... #.... #####")
g("F", "##### #.... #.... ####. #.... #.... #....")
g("G", ".###. #...# #.... #.### #...# #...# .####")
g("H", "#...# #...# #...# ##### #...# #...# #...#")
g("I", "### .#. .#. .#. .#. .#. ###")
g("J", "..### ...#. ...#. ...#. #..#. #..#. .##..")
g("K", "#...# #..#. #.#.. ##... #.#.. #..#. #...#")
g("L", "#.... #.... #.... #.... #.... #.... #####")
g("M", "#...# ##.## #.#.# #.#.# #...# #...# #...#")
g("N", "#...# ##..# #.#.# #..## #...# #...# #...#")
g("O", ".###. #...# #...# #...# #...# #...# .###.")
g("P", "####. #...# #...# ####. #.... #.... #....")
g("Q", ".###. #...# #...# #...# #.#.# #..#. .##.#")
g("R", "####. #...# #...# ####. #.#.. #..#. #...#")
g("S", ".#### #.... #.... .###. ....# ....# ####.")
g("T", "##### ..#.. ..#.. ..#.. ..#.. ..#.. ..#..")
g("U", "#...# #...# #...# #...# #...# #...# .###.")
g("V", "#...# #...# #...# #...# .#.#. .#.#. ..#..")
g("W", "#...# #...# #...# #.#.# #.#.# ##.## #...#")
g("X", "#...# #...# .#.#. ..#.. .#.#. #...# #...#")
g("Y", "#...# #...# .#.#. ..#.. ..#.. ..#.. ..#..")
g("Z", "##### ....# ...#. ..#.. .#... #.... #####")
g("Œ", ".#### #..#. #..#. #..## #..#. #..#. .####")
g("Æ", ".#### #.#.. #.#.. ##### #.#.. #.#.. #.###")

# ---- lowercase ------------------------------------------------------------------------------
g("a", "..... ..... .###. ....# .#### #...# .####")
g("b", "#.... #.... ####. #...# #...# #...# ####.")
g("c", "..... ..... .###. #.... #.... #.... .###.")
g("d", "....# ....# .#### #...# #...# #...# .####")
g("e", "..... ..... .###. #...# ##### #.... .###.")
g("f", "..## .#.. #### .#.. .#.. .#.. .#..")
g("g", "..... ..... .#### #...# #...# #...# .####", "....# .###.")
g("h", "#.... #.... ####. #...# #...# #...# #...#")
g("i", "# . # # # # #")
g("j", "..# ... ..# ..# ..# ..# ..#", "#.# .#.")
g("k", "#... #... #..# #.#. ##.. #.#. #..#")
g("l", "#. #. #. #. #. #. .#")
g("m", "..... ..... ##.#. #.#.# #.#.# #.#.# #...#")
g("n", "..... ..... ####. #...# #...# #...# #...#")
g("o", "..... ..... .###. #...# #...# #...# .###.")
g("p", "..... ..... ####. #...# #...# #...# ####.", "#.... #....")
g("q", "..... ..... .#### #...# #...# #...# .####", "....# ....#")
g("r", ".... .... #.## ##.. #... #... #...")
g("s", "..... ..... .#### #.... .###. ....# ####.")
g("t", ".#.. .#.. #### .#.. .#.. .#.. ..##")
g("u", "..... ..... #...# #...# #...# #...# .####")
g("v", "..... ..... #...# #...# #...# .#.#. ..#..")
g("w", "..... ..... #...# #...# #.#.# #.#.# .#.#.")
g("x", "..... ..... #...# .#.#. ..#.. .#.#. #...#")
g("y", "..... ..... #...# #...# #...# #...# .####", "....# .###.")
g("z", "..... ..... ##### ...#. ..#.. .#... #####")
g("œ", "....... ....... .##.##. #..#..# #..#### #..#... .##.###")
g("æ", "....... ....... .##.##. ...#..# .###### #..#... .##.###")
g("ı", ". . # # # # #")  # dotless i, base of î and ï

# ---- digits ---------------------------------------------------------------------------------
g("0", ".###. #...# #..## #.#.# ##..# #...# .###.")
g("1", ".#. ##. .#. .#. .#. .#. ###")
g("2", ".###. #...# ....# ...#. ..#.. .#... #####")
g("3", "####. ....# ....# .###. ....# ....# ####.")
g("4", "...#. ..##. .#.#. #..#. ##### ...#. ...#.")
g("5", "##### #.... ####. ....# ....# #...# .###.")
g("6", ".###. #.... #.... ####. #...# #...# .###.")
g("7", "##### ....# ...#. ..#.. .#... .#... .#...")
g("8", ".###. #...# #...# .###. #...# #...# .###.")
g("9", ".###. #...# #...# .#### ....# ....# .###.")

# ---- punctuation and symbols ----------------------------------------------------------------
g(".", ". . . . . . #")
g(",", ".. .. .. .. .. .. .#", "#.")
g(":", ". . # . . . #")
g(";", ".. .. .# .. .. .. .#", "#.")
g("!", "# # # # # . #")
g("?", ".###. #...# ....# ...#. ..#.. ..... ..#..")
g("'", "# # . . . . .")
g("’", ".# .# #. .. .. .. ..")
g("‘", ".# #. #. .. .. .. ..")
g('"', "#.# #.# ... ... ... ... ...")
g("«", "..... ..... ..#.# .#.#. #.#.. .#.#. ..#.#")
g("»", "..... ..... #.#.. .#.#. ..#.# .#.#. #.#..")
g("(", ".# #. #. #. #. #. .#")
g(")", "#. .# .# .# .# .# #.")
g("[", "## #. #. #. #. #. ##")
g("]", "## .# .# .# .# .# ##")
g("-", "... ... ... ### ... ... ...")
g("–", ".... .... .... #### .... .... ....")
g("—", "...... ...... ...... ###### ...... ...... ......")
g("+", "..... ..#.. ..#.. ##### ..#.. ..#.. .....")
g("=", "..... ..... ##### ..... ##### ..... .....")
g("/", "....# ...#. ...#. ..#.. .#... .#... #....")
g("\\", "#.... .#... .#... ..#.. ...#. ...#. ....#")
g("%", "##..# ##.#. ...#. ..#.. .#... .#.## #..##")
g("&", ".##.. #..#. #.#.. .#... #.#.# #..#. .##.#")
g("*", "..... #.#.# .###. ##### .###. #.#.# .....")
g("#", ".#.#. ##### .#.#. .#.#. ##### .#.#. .....")
g("@", ".###. #...# #.### #.#.# #.### #.... .###.")
g("_", "..... ..... ..... ..... ..... ..... .....", "##### .....")
g("<", "...#. ..#.. .#... #.... .#... ..#.. ...#.")
g(">", ".#... ..#.. ...#. ....# ...#. ..#.. .#...")
g("…", "..... ..... ..... ..... ..... ..... #.#.#")
g("×", "..... #...# .#.#. ..#.. .#.#. #...# .....")
g("°", "### #.# ### ... ... ... ...")
g("·", ". . . # . . .")
g("|", "# # # # # # #", "# #")
g("→", "..... ..#.. ...#. ##### ...#. ..#.. .....")
g("←", "..... ..#.. .#... ##### .#... ..#.. .....")
g("↑", "..#.. .###. #.#.# ..#.. ..#.. ..#.. .....")
g("↓", "..... ..#.. ..#.. ..#.. #.#.# .###. ..#..")
g("■", "..... ##### ##### ##### ##### ##### .....")
g("♦", "..... ..#.. .###. ##### .###. ..#.. .....")
g("©", ".###. #...# #.#.# ##..# #.#.# #...# .###.")

# ---- accents --------------------------------------------------------------------------------
ACUTE = ("...#.", "..#..")
GRAVE = (".#...", "..#..")
CIRC = ("..#..", ".#.#.")
DIAER = (".....", ".#.#.")


def centred(mark, width):
    out = []
    for row in mark:
        pad = (width - len(row)) // 2
        r = ("." * pad + row + "." * width)[:width]
        out.append(r)
    return out


def accent_lower(base, mark, name):
    rows, desc, _ = G[base]
    w = max(len(rows[0]), len(mark[0]))
    body = [r.center(w, ".") if len(r) < w else r for r in rows]
    m = centred(mark, w)
    G[name] = ([m[0], m[1]] + body[2:], desc, None)


def accent_upper(base, mark, name):
    rows, desc, _ = G[base]
    G[name] = (rows, desc, centred(mark, len(rows[0])))


for base, marks in {
    "a": {"à": GRAVE, "â": CIRC, "ä": DIAER, "á": ACUTE},
    "e": {"é": ACUTE, "è": GRAVE, "ê": CIRC, "ë": DIAER},
    "o": {"ô": CIRC, "ö": DIAER, "ó": ACUTE},
    "u": {"ù": GRAVE, "û": CIRC, "ü": DIAER, "ú": ACUTE},
    "ı": {"î": ("#", "#") if False else (".#.", "#.#"), "ï": ("...", "#.#"), "í": (".#", "#.")},
    "y": {"ÿ": DIAER},
}.items():
    for name, mark in marks.items():
        accent_lower(base, mark, name)

for base, marks in {
    "A": {"À": GRAVE, "Â": CIRC, "Ä": DIAER, "Á": ACUTE},
    "E": {"É": ACUTE, "È": GRAVE, "Ê": CIRC, "Ë": DIAER},
    "I": {"Î": (".#.", "#.#"), "Ï": ("...", "#.#")},
    "O": {"Ô": CIRC, "Ö": DIAER},
    "U": {"Ù": GRAVE, "Û": CIRC, "Ü": DIAER},
    "Y": {"Ÿ": DIAER},
}.items():
    for name, mark in marks.items():
        accent_upper(base, mark, name)

G["ç"] = (G["c"][0], ["..#..", ".##.."], None)
G["Ç"] = (G["C"][0], ["..#..", ".##.."], None)
G["ñ"] = ([".#.#.", "#.#.."] + G["n"][0][2:], None, None)


# ---------------------------------------------------------------------------------------------

def glyph_rows(ch):
    """All 11 rows (2 accent rows, 7 main rows, 2 descender rows) of a glyph."""
    rows, desc, top = G[ch]
    w = len(rows[0])
    blank = "." * w
    top = top or [blank, blank]
    desc = desc or [blank, blank]
    return top + rows + desc, w


def build():
    os.makedirs(OUT, exist_ok=True)
    chars = sorted(G.keys(), key=ord)
    order = [".notdef", "space"] + [f"uni{ord(ch):04X}" for ch in chars]
    fb = FontBuilder(unitsPerEm=12 * UNIT, isTTF=True)
    fb.setupGlyphOrder(order)
    cmap = {32: "space", 0xA0: "space"}
    cmap.update({ord(ch): f"uni{ord(ch):04X}" for ch in chars})
    fb.setupCharacterMap(cmap)

    glyphs, metrics = {}, {}

    def rect_glyph(rects, advance):
        pen = TTGlyphPen(None)
        for (x0, y0, x1, y1) in rects:
            # clockwise outer contour (TrueType)
            pen.moveTo((x0, y0))
            pen.lineTo((x0, y1))
            pen.lineTo((x1, y1))
            pen.lineTo((x1, y0))
            pen.closePath()
        return pen.glyph(), (advance, rects[0][0] if rects else 0)

    # .notdef: a hollow box
    nd, ndm = rect_glyph([(0, 0, 100, 700), (400, 0, 500, 700), (0, 0, 500, 100), (0, 600, 500, 700)], 600)
    glyphs[".notdef"], metrics[".notdef"] = nd, ndm
    sp, spm = rect_glyph([], 400)
    glyphs["space"], metrics["space"] = sp, (400, 0)

    for ch in chars:
        rows, w = glyph_rows(ch)
        rects = []
        for r, row in enumerate(rows):
            # row 0..1 accents, 2..8 main (row 8 sits on the baseline), 9..10 descenders
            y0 = (8 - r) * UNIT
            x = 0
            while x < len(row):
                if row[x] == "#":
                    start = x
                    while x < len(row) and row[x] == "#":
                        x += 1
                    rects.append((start * UNIT, y0, x * UNIT, y0 + UNIT))
                else:
                    x += 1
        glyph, m = rect_glyph(rects, (w + 1) * UNIT)
        name = f"uni{ord(ch):04X}"
        glyphs[name] = glyph
        metrics[name] = (m[0], min((r[0] for r in rects), default=0))

    fb.setupGlyf(glyphs)
    fb.setupHorizontalMetrics(metrics)
    fb.setupHorizontalHeader(ascent=10 * UNIT, descent=-2 * UNIT)
    fb.setupNameTable({
        "familyName": "Wickfall Pixel", "styleName": "Regular",
        "copyright": "CC0 1.0 - drawn for WICKFALL",
        "uniqueFontIdentifier": "WickfallPixel-Regular", "fullName": "Wickfall Pixel Regular",
        "psName": "WickfallPixel-Regular", "version": "Version 1.0",
    })
    fb.setupOS2(sTypoAscender=10 * UNIT, sTypoDescender=-2 * UNIT, sTypoLineGap=0,
                usWinAscent=10 * UNIT, usWinDescent=2 * UNIT, sxHeight=5 * UNIT, sCapHeight=7 * UNIT,
                achVendID="WKFL", fsType=0)
    fb.setupPost()
    fb.setupHead(unitsPerEm=12 * UNIT)
    path = os.path.join(OUT, "WickfallPixel.ttf")
    fb.save(path)
    return path, chars


def preview(chars):
    """Renders the glyph set and a French test sentence from the bitmaps (prompt §12: look at it)."""
    from PIL import Image
    lines = [
        "WICKFALL - Le Porte-mèche · Champs de Suif",
        "« On ne peut pas être fort et caché en même temps. »",
        "Bois-Mèche, Fonderies de Suie, Cathédrale de Verre",
        "Creux Profond, Chambre du Foyer — Roi Creux épargné",
        "é è ê ë à â ç î ï ô û ù œ Œ « » ’  KXH kxh 0123456789",
        "ÉÈÊËÀÂÇÎÏÔÛÙ  Cire 100%  Reliques 6/6  Éclats ×12",
        "Wax = life = light = risk. Ember Relics, Hollow King.",
    ]
    scale = 4
    pad = 1
    width = max(sum(glyph_rows(ch)[1] + 1 if ch in G else 4 for ch in line) for line in lines) + 2
    img = Image.new("RGB", (width * scale, (len(lines) * 12 + 2) * scale), (14, 13, 12))
    px = img.load()
    for li, line in enumerate(lines):
        x = pad
        for ch in line:
            if ch == " " or ch not in G:
                x += 4
                continue
            rows, w = glyph_rows(ch)
            for r, row in enumerate(rows):
                for cx, v in enumerate(row):
                    if v == "#":
                        for sy in range(scale):
                            for sx in range(scale):
                                px[(x + cx) * scale + sx, (li * 12 + r + 1) * scale + sy] = (237, 230, 212)
            x += w + 1
    out = os.path.join(ROOT, "ArtSource", "Previews", "font_preview.png")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    img.save(out)
    return out


if __name__ == "__main__":
    p, chars = build()
    print("font:", p, len(chars), "glyphs")
    print("preview:", preview(chars))
