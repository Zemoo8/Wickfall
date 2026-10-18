"""
Builds every generated WICKFALL sprite into Assets/Art/Generated and contact sheets into
ArtSource/Previews (prompt §10: "Look at every result yourself").

    python Tools/art/make_art.py            # everything
    python Tools/art/make_art.py player     # one group
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, HERE)

from PIL import Image  # noqa: E402

import pixel  # noqa: E402

OUT = os.path.join(ROOT, "Assets", "Art", "Generated")
PREVIEWS = os.path.join(ROOT, "ArtSource", "Previews")


def contact(name, img, scale=6, flame=None):
    os.makedirs(PREVIEWS, exist_ok=True)
    big = pixel.preview(img, scale)
    big.save(os.path.join(PREVIEWS, name + ".png"))


def player():
    import json
    import wickbearer
    folder = os.path.join(OUT, "Player")
    img = wickbearer.build(folder)
    contact("player_body", img, 5)
    flame = pixel.tint_flame(Image.open(os.path.join(folder, "Player_Flame.png")))
    contact("player_flame_amber", flame, 8)

    # Key frames with the amber flame on its wick, on charcoal, 8x (what the player looks like in game).
    meta = json.load(open(os.path.join(folder, "Player_Body.json")))
    rows = {name: i for i, name in enumerate(meta["clips"].keys())}
    W, H = meta["cell"]
    picks = [("idle_down", 0, "down"), ("idle_up", 0, "up"), ("idle_right", 0, "right"), ("idle_left", 0, "left"),
             ("dim", 0, "dim"), ("walk_down", 0, "down"), ("walk_right", 3, "right"), ("attack_right", 2, "right"),
             ("dash_right", 1, None), ("hurt", 1, None), ("death", 5, None), ("death", 11, None)]
    out = Image.new("RGBA", (len(picks) * W, H), (42, 39, 36, 255))
    for i, (clip, f, anchor) in enumerate(picks):
        r = rows[clip]
        out.alpha_composite(img.crop((f * W, r * H, f * W + W, r * H + H)), (i * W, 0))
        if anchor:
            ax, ay = meta["dimFlameAnchor"] if anchor == "dim" else meta["flameAnchor"][anchor]
            cell = (16, 20, 32, 40) if anchor == "dim" else (16, 0, 32, 20)
            px = int(i * W + meta["pivot"][0] + ax - 8)
            py = int(H - meta["pivot"][1] - ay - 20)
            out.alpha_composite(flame.crop(cell), (px, py))
    contact("player_keyframes", out, 8)


def ui():
    import ui as ui_art
    folder = os.path.join(OUT, "UI")
    ui_art.build(folder)
    for name in ("HUD_Candle", "HUD_Flames", "HUD_FlameIcons", "HUD_Small", "HUD_Wheel", "UI_Panel"):
        contact("ui_" + name, Image.open(os.path.join(folder, name + ".png")), 6)


def vfx():
    import vfx as vfx_art
    folder = os.path.join(OUT, "VFX")
    images = vfx_art.build(folder)
    # one contact sheet with every effect tinted amber on charcoal
    rows = [img for _, img in images]
    width = max(r.width for r in rows)
    height = sum(r.height for r in rows) + 2 * len(rows)
    sheet = Image.new("RGBA", (width, height), (14, 13, 12, 255))
    y = 0
    for r in rows:
        sheet.alpha_composite(r, (0, y))
        y += r.height + 2
    contact("vfx_all", sheet, 3)


def tiles():
    import tiles as tile_art
    folder = os.path.join(OUT, "Tiles")
    img = tile_art.build_tallow(folder)
    contact("tiles_tallow", img, 4)
    contact("tiles_wickwood", tile_art.build_wickwood(folder), 4)
    contact("tiles_foundry", tile_art.build_foundry(folder), 4)
    room = tile_art.render_map(tile_art.TALLOW, tile_art.TEST_ROOM, tile_art.tallow_face_detail,
                               [tile_art.wax_grass(0, 0), tile_art.wax_grass(0, 1), tile_art.wax_cracks(0)])
    contact("tiles_tallow_room", room, 3)


def props():
    import props as prop_art
    folder = os.path.join(OUT, "Props")
    prop_art.build(folder)
    for name in ("Prop_Brazier", "Prop_WaxPool", "Prop_Effigy"):
        contact("props_" + name, Image.open(os.path.join(folder, name + ".png")), 6)


def enemies():
    import enemies as enemy_art
    folder = os.path.join(OUT, "Enemies")
    for name, img in enemy_art.build(folder):
        contact("enemy_" + name, img, 8)
    import foundry
    for name, img in foundry.build_enemies(folder):
        contact("enemy_" + name, img, 6)
    name, img = foundry.build_vfx(os.path.join(OUT, "VFX"))
    contact("vfx_" + name, img, 12)


def world():
    import props_world
    folder = os.path.join(OUT, "Props")
    props_world.build(folder)
    import foundry
    for name in foundry.build_props(folder):
        contact("props_" + name, Image.open(os.path.join(folder, name + ".png")), 4)
    for name in ("Prop_Relics", "Prop_GreatBell", "Prop_BrokenWick", "Prop_Gate", "Prop_Shrine", "Prop_Stone", "Prop_Nest"):
        contact("props_" + name, Image.open(os.path.join(folder, name + ".png")), 4)


GROUPS = {"player": player, "ui": ui, "vfx": vfx, "tiles": tiles, "props": props, "enemies": enemies, "world": world}


def main(args):
    names = args or list(GROUPS.keys())
    for name in names:
        print("building", name)
        GROUPS[name]()


if __name__ == "__main__":
    main(sys.argv[1:])
