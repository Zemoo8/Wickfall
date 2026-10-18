"""
Region 2 - Bois-Mèche (PDF §2.2: a black forest, a canopy that blocks light, vines to climb;
Light-Eater Blooms cut the light for 3 s; Husks ambush; Lurkers move if you shine).

Teaching beats (prompt §9): hiding in the darkness under the canopy; Light-Eater Blooms; Husk
ambushes from branches; Lurkers that move only while you shine (red light / green light);
climbable wick-vines (they need this region's relic, "accroche au mur", G04); the Green flame (G03).

Layout: the south forest holds the relic; a root cliff splits the map and the only ways north are
wick-vines. North: the Green flame's grove and the bell plaza with the exit.

    python Tools/levels/r2_wickwood.py
"""
from carve import Level

L = Level("R2 Bois-Mèche", 124, 72, seed=22)
K = {
    "P": "PlayerStart", "B": "Brazier", "W": "WaxPool", "h": "Husk", "l": "Lurker", "b": "LightEaterBloom",
    "R": "Relic", "F": "FlameShrine", "L": "GreatBell", "H": "Gate", "E": "Exit", "I": "Inscription",
    "v": "Vine", "n": "SwarmNestLurker", "u": "SwarmNestHusk", "f": "Cirelle",
}

# ---- south forest -------------------------------------------------------------------------------
# entry path under the canopy (south-west)
L.ellipse(20, 62, 16, 7, rough=0.2)
L.canopy_ellipse(20, 62, 14, 6)
for (x, y) in [(10, 58), (16, 66), (27, 59), (31, 65), (22, 62)]:
    L.pillar(x, y, 2, 2)                      # tree trunks
L.decorate(4, 54, 38, 70, 0.08)
# the bloom alley: a corridor with side alcoves where the blooms grow
L.corridor([(35, 61), (74, 60)], 3)
for x in (42, 50, 58, 64):
    L.rect(x, 58, x + 1, 59)                  # alcove north of the path
L.decorate(36, 58, 68, 64, 0.05)
# the husk grove (east)
L.ellipse(84, 59, 15, 9, rough=0.15)
L.canopy_ellipse(86, 58, 12, 7)
for (x, y) in [(76, 55), (82, 62), (90, 54), (94, 63), (86, 57), (78, 64)]:
    L.pillar(x, y, 2, 2)
L.decorate(70, 50, 99, 68, 0.06)
# the lurker clearing (open sky, no canopy), reached from the alley
L.corridor([(54, 60), (54, 52)], 3)
L.ellipse(54, 46, 14, 7, rough=0.12)
L.decorate(40, 39, 68, 53, 0.04)
# the relic hollow (west of the clearing)
L.corridor([(41, 46), (30, 46)], 3)
L.ellipse(19, 45, 11, 6, rough=0.15)
L.canopy_ellipse(17, 44, 8, 5)
L.pillar(19, 43, 2, 2)
# path from the grove to the main vine, brazier 2 at the cliff foot
L.corridor([(84, 50), (84, 42)], 4)
L.ellipse(84, 41, 7, 3)

# ---- north forest ----------------------------------------------------------------------------------
L.ellipse(88, 26, 18, 8, rough=0.18)
L.canopy_ellipse(94, 24, 13, 6)
for (x, y) in [(80, 22), (96, 28), (102, 21), (88, 30), (92, 20)]:
    L.pillar(x, y, 2, 2)
L.decorate(70, 17, 106, 33, 0.07)
L.corridor([(84, 33), (84, 30)], 3)          # vine top to the forest
L.ellipse(110, 22, 7, 5)                     # the shrine grove
L.corridor([(104, 24), (110, 22)], 3)
# west to the bell plaza
L.corridor([(72, 24), (54, 18)], 4)
L.rect(26, 8, 56, 30)
L.canopy_ellipse(32, 26, 6, 4)
for (x, y) in [(30, 12), (50, 12), (30, 24), (50, 24)]:
    L.pillar(x, y, 2, 2)
L.corridor([(20, 33), (26, 28)], 3)          # shortcut vine top joins the plaza
L.wall_rect(26, 6, 56, 7)                    # plaza north wall with the gate
L.corridor([(41, 5), (41, 2)], 3)
L.decorate(26, 8, 56, 30, 0.05)

# ---- the root cliff (rows 34..37): solid; only the wick-vines cross it ---------------------------
L.wall_rect(1, 34, L.w - 2, 37)
for x in (84, 20):
    for y in range(34, 38):
        L.grid[y][x] = "v"
    L.rect(x - 1, 31, x + 1, 33)             # landing at the top
    L.rect(x - 1, 38, x + 1, 40)             # landing at the foot (3 wide: the body fits)
L.corridor([(20, 40), (19, 43)], 3)          # the west vine rises from the relic hollow
L.legend["v"] = K["v"]

# ---- entities -----------------------------------------------------------------------------------------
L.put(8, 64, "P", K["P"])
L.put(14, 61, "B", K["B"])
L.inscription(12, 63, 0)                   # 0: under the canopy, darkness hides you
for (x, y) in [(18, 66), (26, 64), (6, 60)]:
    L.put(x, y, "f", K["f"])
L.inscription(38, 61, 1)                   # 1: the flowers drink the light
for x in (42, 50, 58, 64):
    L.put(x, 58, "b", K["b"])
for (x, y) in [(78, 54), (88, 61), (92, 56), (81, 58)]:
    L.put(x, y, "h", K["h"])
L.inscription(46, 49, 2)                   # 2: what prowls only moves when you shine
for (x, y) in [(50, 44), (58, 47), (62, 42)]:
    L.put(x, y, "l", K["l"])
L.put(12, 44, "R", K["R"])
L.put(24, 42, "b", K["b"])
L.put(16, 48, "b", K["b"])
L.put(25, 46, "h", K["h"])
L.put(87, 41, "B", K["B"])
L.put(80, 41, "W", K["W"])
L.inscription(81, 40, 3)                   # 3: only gripping hands hold the wick-vines
for (x, y) in [(78, 26), (95, 25), (100, 30)]:
    L.put(x, y, "l", K["l"])
for (x, y) in [(86, 22), (99, 24)]:
    L.put(x, y, "h", K["h"])
L.put(106, 24, "b", K["b"])
L.put(111, 21, "F", K["F"])
L.put(43, 16, "L", K["L"])
L.inscription(38, 19, 4)                   # 4: above the roots, the bell waits
for x in (40, 41, 42):
    for y in (6, 7):
        L.grid[y][x] = "H"
L.legend["H"] = K["H"]
for (x, y) in [(28, 10), (54, 10), (28, 28), (54, 28)]:
    L.put(x, y, "n", K["n"])
for (x, y) in [(34, 16), (48, 22)]:
    L.put(x, y, "u", K["u"])
L.put(41, 3, "E", K["E"])

# ---- room groups and the bot ------------------------------------------------------------------------
L.group("SouthWest", 0, 39)
L.group("SouthEast", 40, 71)
L.group("Grove", 72, 123)

L.bot = [
    "rest 14 61", "read 12 63", "goto 34 61", "trim on", "walk 46 61", "walk 54 61", "trim off",
    "goto 54 54", "fight 56 45 10", "goto 41 46", "fight 19 45 8", "take 12 44", "goto 41 46", "goto 54 54",
    "goto 54 61", "trim on", "walk 66 61", "trim off", "fight 84 58 12", "goto 84 44", "rest 87 41",
    "climb 84 38", "goto 84 30", "fight 88 25 12", "goto 104 24", "take 111 21",
    "goto 72 24", "goto 54 18", "ring 43 16", "fight 41 18 14", "goto 41 6", "exit 41 3",
]

cut = L.check_connected_with((8, 64), passable="")
# Without climbing, the north must be unreachable (the vines are the only way up).
blocked_view = [row[:] for row in L.grid]
for y in range(L.h):
    for x in range(L.w):
        if L.grid[y][x] == "v":
            L.grid[y][x] = "#"
north_cut = L.check_connected((8, 64))
L.grid = blocked_view
if (111, 21) not in north_cut:
    raise SystemExit("the north (shrine) is reachable without the vines")
if (12, 44) in north_cut:
    raise SystemExit("the relic needs the vines: it must be reachable from the south")
if cut:
    raise SystemExit(f"unreachable cells even with vines: {cut[:10]}")
print("level:", L.save("R2_Wickwood.txt", "# Region 2: black forest, canopy, wick-vines."))

if __name__ == "__main__":
    import tiles
    colors = {"P": (80, 220, 120), "B": (255, 160, 60), "W": (240, 240, 230), "h": (150, 110, 60), "l": (60, 60, 70),
              "b": (230, 230, 220), "R": (255, 210, 60), "F": (90, 230, 90), "L": (190, 160, 90), "H": (120, 120, 120),
              "E": (60, 255, 255), "I": (180, 180, 255), "v": (200, 150, 60), "n": (60, 30, 80), "u": (120, 70, 30), "f": (255, 230, 150)}
    print("preview:", L.preview(tiles.WICKWOOD, tiles.wickwood_face_detail, [tiles.roots(0), tiles.mushrooms(0)], "level_r2.png", colors))
