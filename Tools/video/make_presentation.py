"""
Builds the short presentation video from the narrated region clips in Builds/Videos:
a title and a pitch card, one or two captioned moments per region, the Hollow King and Ending A,
then what is in the game and how it is built.

    python Tools/video/make_presentation.py        # -> Builds/Videos/WICKFALL_Presentation.mp4

Needs ffmpeg on PATH and Pillow. The clips' own captions do the narration; cuts start where a
caption starts, so every caption is read whole.
"""
import os
import shutil
import subprocess
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
VIDEOS = os.path.join(ROOT, "Builds", "Videos")
OUT = os.path.join(VIDEOS, "WICKFALL_Presentation.mp4")
WORK = os.path.join(VIDEOS, "_presentation_parts")
FONT = os.path.join(ROOT, "Assets", "Art", "Fonts", "WickfallPixel.ttf")
PLAYER = os.path.join(ROOT, "ArtSource", "Previews", "player_zoom.png")
TITLE_MUSIC = os.path.join(ROOT, "Assets", "Audio", "Music", "Title.wav")

W, H, FPS = 1280, 720, 30
INK = (240, 232, 214)
AMBER = (255, 190, 92)
DIM = (170, 160, 142)
GITHUB = "github.com/Zemoo8/Wickfall"

# (clip, start, end, region label or None). Times are caption starts in the source clip.
# The last clip ends on the Ending A screen, which is held a few seconds so it can be read.
HOLD_LAST = 3.5
SEGMENTS = [
    ("Stage4_R1_TallowFields.mp4", 28.0, 45.7, "REGION 1  -  TALLOW FIELDS"),
    ("Stage4_R2_Wickwood.mp4", 15.0, 23.9, "REGION 2  -  WICKWOOD"),
    ("Stage4_R2_Wickwood.mp4", 51.6, 58.7, None),
    ("Stage5_R3_SootFoundries.mp4", 15.0, 23.5, "REGION 3  -  SOOT FOUNDRIES"),
    ("Stage5_R3_SootFoundries.mp4", 40.7, 49.7, None),
    ("Stage5_R4_GlassCathedral.mp4", 15.0, 22.5, "REGION 4  -  GLASS CATHEDRAL"),
    ("Stage5_R4_GlassCathedral.mp4", 65.9, 74.8, None),
    ("Stage6_R5_DeepHollow.mp4", 15.0, 32.6, "REGION 5  -  DEEP HOLLOW"),
    ("Stage6_R6_Boss_and_EndingA.mp4", 43.3, 59.2, "REGION 6  -  THE HOLLOW KING"),
    ("Stage6_R6_Boss_and_EndingA.mp4", 99.2, 115.4, None),
]

# (name, seconds, lines). A line is (text, size, colour); "" is a gap.
INTRO = [
    ("title", 5.0, None),
    ("pitch", 8.0, [
        ("You are the last Wickbearer,", 30, INK),
        ("a candle golem carrying a shard of the dead Great Hearth.", 30, INK),
        ("", 22, INK),
        ("WAX = LIFE = LIGHT = RISK", 56, AMBER),
        ("", 22, INK),
        ("Shine to see and fight. Trim your wick to hide.", 30, INK),
        ("You can't be strong and hidden at the same time.", 30, DIM),
    ]),
]
OUTRO = [
    ("content", 7.0, [
        ("WHAT IS IN IT", 44, AMBER),
        ("", 18, INK),
        ("6 regions  -  5 flames  -  6 relics  -  4 pacts", 30, INK),
        ("10 kinds of creatures and the Hollow King in 3 phases", 30, INK),
        ("Stealth with light, light-beam puzzles, hazards, scores and ranks", 30, INK),
        ("3 endings: A, B and a secret C", 30, INK),
        ("English and French  -  keyboard and gamepad", 30, INK),
    ]),
    ("tech", 7.0, [
        ("HOW IT IS BUILT", 44, AMBER),
        ("", 18, INK),
        ("Unity 6, C#, URP 2D lights and post-processing", 30, INK),
        ("All tuning in ScriptableObjects; systems talk through event channels", 30, INK),
        ("Every creature runs on a state machine; zero allocations per frame", 30, INK),
        ("Levels written as text maps; pixel art and sound made by scripts", 30, INK),
        ("77 unit tests, plus play tests where a bot finishes every region", 30, INK),
    ]),
    ("end", 6.0, None),
]


def run(args):
    subprocess.run(args, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)


def font(size):
    return ImageFont.truetype(FONT, size)


def background():
    """Near-black with a warm candle glow in the middle, like the main menu."""
    img = Image.new("RGB", (W, H), (11, 10, 9))
    glow = Image.new("L", (W, H), 0)
    ImageDraw.Draw(glow).ellipse((W * 0.18, H * 0.05, W * 0.82, H * 0.95), fill=60)
    glow = glow.filter(ImageFilter.GaussianBlur(160))
    warm = Image.new("RGB", (W, H), (120, 72, 26))
    return Image.composite(warm, img, glow)


def centred(draw, y, text, size, colour):
    f = font(size)
    w = draw.textlength(text, font=f)
    draw.text(((W - w) / 2, y), text, font=f, fill=colour)


def glow_text(img, y, text, size, colour):
    """Text with a soft halo, for the big WICKFALL titles."""
    halo = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(halo)
    f = font(size)
    x = (W - d.textlength(text, font=f)) / 2
    d.text((x, y), text, font=f, fill=200)
    halo = halo.filter(ImageFilter.GaussianBlur(14))
    img.paste(Image.new("RGB", (W, H), AMBER), (0, 0), halo)
    ImageDraw.Draw(img).text((x, y), text, font=f, fill=colour)


def player_sprite(scale):
    """The first idle frame of the Wickbearer from the art preview, background keyed out."""
    sheet = Image.open(PLAYER).convert("RGBA")
    cell = sheet.crop((0, 0, sheet.width // 12, sheet.height))
    bg = cell.getpixel((2, 2))
    px = cell.load()
    for yy in range(cell.height):
        for xx in range(cell.width):
            r, g, b, a = px[xx, yy]
            if abs(r - bg[0]) + abs(g - bg[1]) + abs(b - bg[2]) < 12:
                px[xx, yy] = (0, 0, 0, 0)
    cell = cell.crop(cell.getbbox())
    return cell.resize((int(cell.width * scale), int(cell.height * scale)), Image.NEAREST)


def card(name, lines):
    img = background()
    draw = ImageDraw.Draw(img)
    if name in ("title", "end"):
        sprite = player_sprite(0.55 if name == "title" else 0.4)
        sy = 125 if name == "title" else 115
        img.paste(sprite, ((W - sprite.width) // 2, sy), sprite)
        ty = sy + sprite.height + 30
        glow_text(img, ty, "WICKFALL", 110, INK)
        draw = ImageDraw.Draw(img)
        if name == "title":
            centred(draw, ty + 150, "A 2D top-down pixel-art action RPG", 32, INK)
            centred(draw, ty + 196, "Unity 6  -  Windows  -  by Ahmed Baghouli", 26, DIM)
        else:
            centred(draw, ty + 150, "A game by Ahmed Baghouli", 32, INK)
            centred(draw, ty + 200, GITHUB, 28, AMBER)
            centred(draw, ty + 250, "Thanks for watching", 26, DIM)
    else:
        heights = [size * 1.45 for _, size, _ in lines]
        y = (H - sum(heights)) / 2
        for (text, size, colour), h in zip(lines, heights):
            if text:
                centred(draw, y, text, size, colour)
            y += h
    path = os.path.join(WORK, f"card_{name}.png")
    img.save(path)
    return path


ENCODE = ["-c:v", "libx264", "-preset", "slow", "-crf", "23", "-pix_fmt", "yuv420p", "-r", str(FPS),
          "-c:a", "aac", "-b:a", "128k", "-ar", "44100", "-ac", "2"]


def card_clip(png, seconds, music_from, out):
    fade = f"fade=t=in:st=0:d=0.6,fade=t=out:st={seconds - 0.6}:d=0.6"
    run(["ffmpeg", "-y", "-loop", "1", "-framerate", str(FPS), "-t", str(seconds), "-i", png,
         "-ss", str(music_from), "-t", str(seconds), "-i", TITLE_MUSIC,
         "-vf", f"format=yuv420p,{fade}",
         "-af", f"afade=t=in:st=0:d=0.6,afade=t=out:st={seconds - 0.8}:d=0.8,volume=0.8",
         "-shortest", *ENCODE, out])


def label_filter(text, dur):
    """A region name in the bottom-right corner for the first seconds of a region."""
    f = FONT.replace("\\", "/").replace(":", "\\:")
    alpha = "if(lt(t,0.4),t/0.4,if(lt(t,3.6),1,if(lt(t,4.2),(4.2-t)/0.6,0)))"
    return (f",drawtext=fontfile='{f}':text='{text}':fontsize=30:fontcolor=0xF0E8D6:"
            f"x=w-tw-36:y=h-th-34:box=1:boxcolor=0x0B0A09@0.75:boxborderw=12:alpha='{alpha}'")


def segment_clip(clip, start, end, label, out, hold=0.0):
    dur = end - start
    total = dur + hold
    vf = f"scale={W}:{H}:flags=lanczos"
    if hold > 0:
        vf += f",tpad=stop_mode=clone:stop_duration={hold}"
    vf += f",fade=t=in:st=0:d=0.3,fade=t=out:st={total - 0.5}:d=0.5"
    if label:
        vf += label_filter(label, dur)
    af = f"apad=whole_dur={total},afade=t=in:st=0:d=0.3,afade=t=out:st={total - 1.2}:d=1.2"
    run(["ffmpeg", "-y", "-ss", str(start), "-t", str(dur), "-i", os.path.join(VIDEOS, clip),
         "-vf", vf, "-af", af, "-t", str(total), *ENCODE, out])


def main():
    if shutil.which("ffmpeg") is None:
        sys.exit("ffmpeg is not on PATH")
    os.makedirs(WORK, exist_ok=True)
    parts = []
    music = 0.0
    for name, seconds, lines in INTRO:
        out = os.path.join(WORK, f"a_{name}.mp4")
        card_clip(card(name, lines), seconds, music, out)
        music += seconds
        parts.append(out)
    for i, (clip, start, end, label) in enumerate(SEGMENTS):
        out = os.path.join(WORK, f"b_{i:02d}.mp4")
        segment_clip(clip, start, end, label, out, HOLD_LAST if i == len(SEGMENTS) - 1 else 0.0)
        parts.append(out)
    for name, seconds, lines in OUTRO:
        out = os.path.join(WORK, f"c_{name}.mp4")
        card_clip(card(name, lines), seconds, music, out)
        music += seconds
        parts.append(out)

    listing = os.path.join(WORK, "parts.txt")
    with open(listing, "w", encoding="utf-8") as fh:
        for p in parts:
            fh.write("file '" + p.replace("\\", "/") + "'\n")
    run(["ffmpeg", "-y", "-f", "concat", "-safe", "0", "-i", listing, "-c", "copy",
         "-movflags", "+faststart", OUT])
    size = os.path.getsize(OUT) / (1024 * 1024)
    print(f"{OUT}  ({size:.1f} MB)")


if __name__ == "__main__":
    main()
