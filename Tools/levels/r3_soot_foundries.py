"""
Region 3 - Fonderies de Suie (PDF §2.2: factory, conveyors, flickering furnaces; presses (35 damage),
steam jets, conveyors; Slagborn (armored), Bellows (shoot soot, -10 wax)).

Teaching beats (prompt §9): presses in rhythm; steam that pushes and belts that carry; Bellows soot
(-10 wax, damage x1.25, light loss 1.5 s); Slagborn hurt only by white or green; furnaces as moving
light and shadow. The White flame is found here (G03); the relic is max wax +20 (G04).

Route: the press corridor -> the belt hall (north lane: steam jets; south lane: presses; the belt
itself pushes you back) -> the Bellows gallery (brazier) -> the Slagborn foundry (the White flame in
its side chapel) -> the west gauntlet -> the relic hall -> the bell plaza and the exit.

    python Tools/levels/r3_soot_foundries.py
"""
from carve import Level

L = Level("R3 Fonderies de Suie", 124, 66, seed=33)
K = {
    "P": "PlayerStart", "B": "Brazier", "W": "WaxPool", "s": "Slagborn", "w": "Bellows", "R": "Relic",
    "F": "FlameShrine", "L": "GreatBell", "H": "Gate", "E": "Exit", "p": "Press", "o": "Furnace",
    "<": "ConveyorW", ">": "ConveyorE", "j": "SteamJetS", "J": "SteamJetN", "n": "SwarmNestBellows", "m": "SwarmNestSlagborn",
}

# ---- A: the entry hall and the press corridor ---------------------------------------------------
L.rect(4, 50, 24, 61)
L.pillar(10, 52, 2, 2)
L.decorate(4, 50, 24, 61, 0.05)
L.corridor([(24, 55), (46, 55)], 3)

# ---- B: the belt hall -----------------------------------------------------------------------------
L.rect(46, 46, 80, 60)
L.wall_rect(50, 57, 77, 58)                      # the south lane (rows 59-60) is walled off from the belt
L.wall_rect(46, 61, 80, 62)
L.decorate(46, 46, 80, 60, 0.04)

# ---- C: the Bellows gallery -----------------------------------------------------------------------
L.rect(81, 40, 108, 62)
for (x, y) in [(88, 46), (100, 46), (88, 56), (100, 56)]:
    L.pillar(x, y, 2, 2)                     # low walls the Bellows shoot over and you hide behind
L.decorate(81, 40, 108, 62, 0.04)

# ---- D: the Slagborn foundry (north) and the White flame's chapel -------------------------------
L.corridor([(94, 40), (94, 30)], 4)
L.rect(68, 12, 110, 30)
for (x, y) in [(76, 16), (86, 26), (98, 16), (104, 26)]:
    L.pillar(x, y, 2, 2)
L.decorate(68, 12, 110, 30, 0.04)
L.corridor([(110, 20), (114, 20)], 3)
L.ellipse(117, 20, 4, 4)

# ---- the west gauntlet: presses over a belt that pushes you back ---------------------------------
L.corridor([(68, 21), (50, 21)], 3)

# ---- F: the relic hall ------------------------------------------------------------------------------
L.rect(36, 14, 50, 28)
L.pillar(42, 24, 2, 2)
L.decorate(36, 14, 50, 28, 0.05)
L.corridor([(36, 21), (33, 21)], 3)

# ---- G: the bell plaza and the exit ---------------------------------------------------------------
L.rect(8, 10, 33, 30)
for (x, y) in [(12, 14), (28, 14), (12, 26), (28, 26)]:
    L.pillar(x, y, 2, 2)
L.wall_rect(8, 8, 33, 9)
L.corridor([(20, 7), (20, 2)], 3)
L.decorate(8, 10, 33, 30, 0.04)

# ---- hazards ---------------------------------------------------------------------------------------
for x in (30, 36, 42):                         # press rhythm in the corridor
    L.put(x, 55, "p", K["p"])
for y in (53, 54, 55):                          # the belt pushes back toward the entrance
    for x in range(50, 78):
        if L.grid[y][x] == "." or L.grid[y][x] == ",":
            L.grid[y][x] = "<"
for x in (56, 62, 68):                          # north lane: steam from the wall pushes you onto the belt
    L.put(x, 46, "j", K["j"])
for x in (58, 66):                              # south lane (rows 59-60): presses block it in rhythm
    L.put(x, 59, "p", K["p"])
for x in range(52, 67):                         # the gauntlet: a belt pushing east, back toward the foundry
    L.grid[21][x] = ">"
for x in (55, 61):
    L.put(x, 21, "p", K["p"])
L.put(58, 22, "J", K["J"])                       # a jet from the south side of the gauntlet
L.legend["<"] = K["<"]
L.legend[">"] = K[">"]
for (x, y) in [(84, 42), (106, 42), (84, 60), (106, 60), (72, 14), (108, 14), (72, 29), (90, 14)]:
    L.put(x, y, "o", K["o"])                     # furnaces: moving light and shadow
L.put(14, 60, "o", K["o"])
L.put(40, 15, "o", K["o"])
L.put(10, 11, "o", K["o"])
L.put(31, 11, "o", K["o"])

# ---- entities --------------------------------------------------------------------------------------
L.put(7, 56, "P", K["P"])
L.put(16, 53, "B", K["B"])
L.inscription(20, 58, 0)                         # 0: the presses strike in rhythm
L.inscription(48, 58, 1)                         # 1: steam pushes, the belt carries
for (x, y) in [(96, 43), (104, 51), (92, 59)]:
    L.put(x, y, "w", K["w"])
L.inscription(84, 50, 2)                         # 2: soot sticks to the wick
L.put(94, 51, "B", K["B"])
L.put(91, 51, "W", K["W"])
L.inscription(92, 32, 3)                         # 3: only white and green bite the metal
for (x, y) in [(80, 20), (92, 18), (100, 22)]:
    L.put(x, y, "s", K["s"])
L.put(82, 26, "w", K["w"])
L.put(118, 20, "F", K["F"])
L.put(40, 19, "R", K["R"])
L.put(46, 26, "w", K["w"])
L.put(44, 16, "s", K["s"])
L.put(22, 16, "L", K["L"])
L.inscription(16, 20, 4)                         # 4: the bell wakes the forge
for x in (19, 20, 21):
    for y in (8, 9):
        L.grid[y][x] = "H"
L.legend["H"] = K["H"]
for (x, y) in [(10, 18), (31, 18), (20, 28)]:
    L.put(x, y, "n", K["n"])
L.put(10, 28, "m", K["m"])
L.put(20, 3, "E", K["E"])

# ---- room groups and the bot ----------------------------------------------------------------------
L.group("West", 0, 47)
L.group("Center", 48, 81)
L.group("East", 82, 123)

L.bot = [
    "rest 16 53", "read 20 58", "goto 26 55", "walk 46 55", "read 48 58",
    "goto 49 59", "walk 79 59", "goto 82 52", "fight 96 51 12", "rest 94 51",
    "read 92 32", "goto 94 31", "fight 90 21 14", "goto 112 20", "take 118 20",
    "goto 70 21", "walk 51 21", "goto 46 22", "fight 43 20 8", "take 40 19",
    "goto 34 21", "goto 20 20", "ring 22 16", "fight 20 19 14", "goto 20 9", "exit 20 3",
]

lost = L.check_connected((7, 56))
if lost:
    raise SystemExit(f"unreachable cells: {lost[:10]}")
print("level:", L.save("R3_SootFoundries.txt", "# Region 3: factory, belts, presses, steam, furnaces."))

if __name__ == "__main__":
    import tiles
    colors = {"P": (80, 220, 120), "B": (255, 160, 60), "W": (240, 240, 230), "s": (150, 90, 50), "w": (120, 100, 70),
              "R": (255, 210, 60), "F": (250, 250, 250), "L": (190, 160, 90), "H": (120, 120, 120), "E": (60, 255, 255),
              "p": (220, 60, 40), "o": (255, 120, 30), "<": (90, 90, 140), ">": (90, 90, 140), "j": (200, 220, 255),
              "J": (200, 220, 255), "n": (60, 30, 80), "m": (100, 40, 40)}
    print("preview:", L.preview(tiles.FOUNDRY, tiles.foundry_face_detail, [tiles.grating(0), tiles.oil_stain(0)], "level_r3.png", colors))
