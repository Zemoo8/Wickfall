"""
Store art for the web release (GamePix asks for an icon and a cover that show the game's title):
square icons and landscape covers in Builds/StoreArt, drawn from a gameplay screenshot, the
Wickbearer sprite and the pixel font.

    python Tools/video/make_store_art.py
"""
import os

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

import make_presentation as mp

OUT = os.path.join(mp.ROOT, "Builds", "StoreArt")
SCENE = os.path.join(mp.ROOT, "Docs", "Screenshots", "r1_clearing.png")


def glow_title(img, text, size, y):
    w, h = img.size
    halo = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(halo)
    f = mp.font(size)
    x = (w - d.textlength(text, font=f)) / 2
    d.text((x, y), text, font=f, fill=220)
    halo = halo.filter(ImageFilter.GaussianBlur(max(4, size // 8)))
    img.paste(Image.new("RGB", (w, h), mp.AMBER), (0, 0), halo)
    ImageDraw.Draw(img).text((x, y), text, font=f, fill=mp.INK)


def vignette(size, strength):
    w, h = size
    mask = Image.new("L", (w, h), 255)
    ImageDraw.Draw(mask).ellipse((-w * 0.1, -h * 0.25, w * 1.1, h * 1.25), fill=0)
    return mask.filter(ImageFilter.GaussianBlur(min(w, h) // 5)).point(lambda v: int(v * strength))


def scene_background(w, h):
    """The Tallow Fields clearing, cropped to fill, a little darker, with dark edges."""
    scene = Image.open(SCENE).convert("RGB")
    scale = max(w / scene.width, h / scene.height)
    scene = scene.resize((int(scene.width * scale + 0.5), int(scene.height * scale + 0.5)), Image.NEAREST)
    left, top = (scene.width - w) // 2, (scene.height - h) // 2
    scene = scene.crop((left, top, left + w, top + h))
    scene = ImageEnhance.Brightness(scene).enhance(0.55)
    dark = Image.new("RGB", (w, h), (11, 10, 9))
    return Image.composite(dark, scene, vignette((w, h), 0.95))


def cover(w, h):
    img = scene_background(w, h)
    size = int(h * 0.17)
    glow_title(img, "WICKFALL", size, int(h * 0.12))
    d = ImageDraw.Draw(img)
    f = mp.font(int(h * 0.045))
    line = "Wax = life = light = risk"
    d.text(((w - d.textlength(line, font=f)) / 2, int(h * 0.33)), line, font=f, fill=mp.AMBER)
    return img


def icon(s):
    img = mp.background().resize((s, s))
    sprite = mp.player_sprite(1.0)
    k = (s * 0.58) / sprite.height
    sprite = sprite.resize((int(sprite.width * k), int(sprite.height * k)), Image.NEAREST)
    img.paste(sprite, ((s - sprite.width) // 2, int(s * 0.06)), sprite)
    glow_title(img, "WICKFALL", int(s * 0.15), int(s * 0.72))
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    for s in (512, 1024):
        icon(s).save(os.path.join(OUT, f"icon_{s}.png"))
    for w, h in ((1280, 720), (1920, 1080), (1200, 630)):
        cover(w, h).save(os.path.join(OUT, f"cover_{w}x{h}.png"))
    print(OUT)


if __name__ == "__main__":
    main()
