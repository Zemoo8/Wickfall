"""
Region 5 - Creux Profond (PDF §2.2: no ambient light, the map visible only where you shine; wax loss
x1.5; the Unlit, which drift toward the strongest light). PDF §2.3: the wax drops you leave when hit
are an Ariadne's thread here. Prompt §11: a heartbeat plays in the Creux Profond.

Teaching beats (prompt §9): darkness with the ground revealed only by your light (LightReveal); the
x1.5 wax loss; the Unlit drawn to the brightest light (lure them with flares, burn them with white);
wax drops as a thread back; the heartbeat. The relic here is the Spare Wick (G04 proposal).

Route: the first cave (brazier) -> the winding tunnel -> the Unlit field -> north to the relic cave ->
the brazier nook -> the long dark west -> the bell cave and the exit.

    python Tools/levels/r5_deep_hollow.py
"""
from carve import Level

L = Level("R5 Creux Profond", 100, 80, seed=55)
K = {
    "P": "PlayerStart", "B": "Brazier", "W": "WaxPool", "u": "Unlit", "R": "Relic", "L": "GreatBell", "H": "Gate",
    "E": "Exit", "n": "SwarmNestUnlit",
}

# ---- caves and tunnels (organic: rough ellipses and winding corridors) ---------------------------------
L.ellipse(14, 66, 10, 7, rough=0.2)                         # the first cave
L.corridor([(22, 66), (30, 62), (38, 64), (46, 62)], 3)     # the winding tunnel
L.ellipse(57, 62, 12, 8, rough=0.2)                         # the Unlit field
L.corridor([(60, 54), (66, 46), (62, 38), (58, 32)], 3)     # north, into the relic cave
L.ellipse(52, 30, 11, 7, rough=0.2)                         # the relic cave
L.corridor([(62, 31), (72, 35)], 3)
L.ellipse(78, 36, 6, 5, rough=0.15)                         # the brazier nook
L.corridor([(44, 26), (36, 20), (28, 22)], 3)               # the long dark west
L.ellipse(20, 18, 12, 8, rough=0.15)                        # the bell cave
L.wall_rect(14, 8, 26, 9)
L.rect(18, 10, 22, 11)                                      # the cave mouth under the gate
L.corridor([(20, 7), (20, 3)], 3)
for (x0, y0, x1, y1, c) in [(4, 59, 24, 73, 0.03), (45, 54, 69, 70, 0.03), (41, 23, 63, 37, 0.03), (8, 10, 32, 26, 0.03)]:
    L.decorate(x0, y0, x1, y1, c)

# ---- entities -----------------------------------------------------------------------------------------------
L.put(9, 68, "P", K["P"])
L.put(16, 64, "B", K["B"])
L.inscription(12, 63, 0)                       # 0: here only your flame draws the world
L.inscription(30, 64, 1)                       # 1: your wax drops lead you back
for (x, y) in [(54, 58), (63, 65), (58, 62)]:
    L.put(x, y, "u", K["u"])
L.inscription(48, 61, 2)                       # 2: the Unlit go to the strongest light
L.inscription(62, 47, 3)                       # 3: white burns the hollow
L.put(45, 28, "R", K["R"])
for (x, y) in [(56, 26), (50, 34), (58, 32)]:
    L.put(x, y, "u", K["u"])
L.put(78, 36, "B", K["B"])
L.put(75, 37, "W", K["W"])
L.inscription(80, 38, 4)                       # 4: the heart beats faster as the wax runs low
L.put(36, 21, "u", K["u"])
L.put(20, 15, "L", K["L"])
L.inscription(14, 20, 5)                       # 5: the bell rings for the unlit too
for x in (19, 20, 21):
    for y in (8, 9):
        L.grid[y][x] = "H"
L.legend["H"] = K["H"]
for (x, y) in [(12, 15), (29, 14), (12, 22), (27, 22)]:
    L.put(x, y, "n", K["n"])
L.put(20, 3, "E", K["E"])

# ---- room groups and the bot -----------------------------------------------------------------------------
L.group("West", 0, 43)
L.group("East", 44, 99)

L.bot = [
    "rest 16 64", "read 12 63", "read 30 64", "goto 44 62", "read 48 61", "fight 58 62 12",
    "goto 61 50", "read 62 47", "goto 62 38", "fight 52 30 12", "take 45 28",
    "goto 70 35", "rest 78 36", "drink 75 37", "read 80 38", "goto 44 26", "fight 36 21 8",
    "goto 24 21", "read 14 20", "ring 20 15", "fight 20 18 14", "goto 20 10", "exit 20 3",
]

lost = L.check_connected((9, 68))
if lost:
    raise SystemExit(f"unreachable cells: {lost[:10]}")
print("level:", L.save("R5_DeepHollow.txt", "# Region 5: no ambient light, the Unlit, wax x1.5, the heartbeat."))

if __name__ == "__main__":
    import tiles
    colors = {"P": (80, 220, 120), "B": (255, 160, 60), "W": (240, 240, 230), "u": (30, 30, 30), "R": (255, 210, 60),
              "L": (190, 160, 90), "H": (120, 120, 120), "E": (60, 255, 255), "n": (60, 30, 80)}
    print("preview:", L.preview(tiles.HOLLOW, tiles.hollow_face_detail, [tiles.faint_outline(0), tiles.bone_crack(0)], "level_r5.png", colors))
