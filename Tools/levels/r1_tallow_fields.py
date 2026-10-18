"""
Region 1 - Champs de Suif (PDF §2.2: a wax plain in low ambient light; wax geysers, 10 damage,
announced 1.2 s; Driplings, harmless Cirelles).

Teaching beats (prompt §9): wax loss and "standing still is losing"; the light shrinks when hit;
detection = light, trimming as stealth; geysers telegraphed 1.2 s; Driplings; harmless Cirelles;
the first brazier, the first relic (an extra dodge), the Broken Wick; the Blue flame (G03);
the Great Bell that opens the gate and wakes the swarm (G14).

    python Tools/levels/r1_tallow_fields.py
"""
from carve import Level

L = Level("R1 Champs de Suif", 128, 64, seed=11)
K = {
    "P": "PlayerStart", "B": "Brazier", "W": "WaxPool", "G": "Geyser", "m": "Dripling", "f": "Cirelle",
    "R": "Relic", "F": "FlameShrine", "K": "BrokenWick", "k": "Solid", "L": "GreatBell", "H": "Gate",
    "E": "Exit", "I": "Inscription", "n": "SwarmNest",
}

# 1. Waking meadow by the Broken Wick (west)
L.ellipse(18, 45, 14, 11, rough=0.18)
L.decorate(4, 34, 32, 56, 0.12)

# 2. Corridor east, then the first clearing (Driplings)
L.corridor([(30, 45), (36, 44)], 4)
L.ellipse(45, 44, 10, 8, rough=0.12)
L.decorate(36, 37, 55, 52, 0.06)

# 3. Corridor east to the geyser field; the geyser field with pillars
L.corridor([(54, 44), (63, 45)], 4)
L.rect(63, 37, 92, 55)
for (px, py, w, h) in [(68, 40, 2, 3), (75, 37, 2, 4), (80, 42, 3, 2), (70, 48, 3, 2), (86, 39, 2, 4), (84, 50, 2, 3), (77, 52, 2, 3)]:
    L.pillar(px, py, w, h)
L.decorate(63, 37, 92, 55, 0.05)
# shrine alcove (south) with the Blue flame
L.corridor([(74, 55), (74, 58)], 3)
L.ellipse(74, 60, 6, 2.6)

# 4. North corridor from the clearing up to the sleeping herd plain
L.corridor([(44, 37), (44, 26)], 4)
L.ellipse(52, 15, 23, 10, rough=0.1)
L.decorate(30, 5, 74, 25, 0.05)

# 5. Relic chamber (north-west) through a geyser gauntlet
L.corridor([(30, 12), (24, 12)], 3)
L.rect(10, 5, 24, 16)
L.pillar(16, 9, 1, 3)

# 6. East corridor to the second brazier room, then down to the bell plaza
L.corridor([(74, 14), (88, 14)], 4)
L.rect(88, 7, 102, 21)
L.corridor([(96, 21), (96, 27)], 4)
L.rect(98, 27, 124, 46)
L.corridor([(92, 46), (98, 44)], 4)    # back route from the geyser field
L.corridor([(92, 52), (92, 46)], 3)
# north wall of the plaza with the gate, then the exit corridor beyond it
L.wall_rect(98, 25, 124, 26)
L.corridor([(111, 24), (111, 8)], 3)
L.rect(106, 3, 116, 8)
for (px, py) in [(102, 31), (120, 31), (102, 42), (120, 42)]:
    L.pillar(px, py, 2, 2)
L.decorate(98, 27, 124, 46, 0.04)

# ---- entities ---------------------------------------------------------------------------------
L.put(18, 48, "P", K["P"])
L.put(11, 40, "K", K["K"])
for x in (10, 12, 13):
    L.put(x, 40, "k", K["k"])
for x in (10, 11, 12, 13):
    L.put(x, 41, "k", K["k"])
L.put(23, 46, "I", K["I"])              # 0: wax is life; standing still is losing
L.put(25, 52, "W", K["W"])
for (x, y) in [(12, 50), (21, 54), (26, 40), (8, 46), (15, 37)]:
    L.put(x, y, "f", K["f"])

L.put(38, 40, "I", K["I"])              # 1: every hit shrinks what you can see
L.put(47, 41, "m", K["m"])
L.put(50, 47, "m", K["m"])
L.put(56, 44, "B", K["B"])              # brazier 1

L.put(65, 39, "I", K["I"])              # 2: the ground rumbles before it spits
for (x, y) in [(66, 44), (72, 39), (73, 45), (79, 47), (83, 40), (88, 46), (82, 54), (68, 53), (89, 51)]:
    L.put(x, y, "G", K["G"])
L.put(74, 61, "F", K["F"])
L.put(70, 60, "m", K["m"])
L.put(78, 60, "m", K["m"])

L.put(42, 24, "I", K["I"])              # 3: trim the wick to pass unseen
for (x, y) in [(38, 12), (50, 9), (60, 14), (47, 19), (34, 17), (66, 9), (56, 21)]:
    L.put(x, y, "m", K["m"])
for (x, y) in [(28, 12), (22, 10), (19, 13), (13, 13)]:
    L.put(x, y, "G", K["G"])
L.put(12, 7, "R", K["R"])

L.put(95, 13, "B", K["B"])              # brazier 2
L.put(99, 18, "W", K["W"])
for (x, y) in [(90, 9), (100, 10), (92, 19)]:
    L.put(x, y, "f", K["f"])

L.put(111, 33, "L", K["L"])             # the Great Bell
L.put(108, 36, "I", K["I"])             # 4: the bell opens the way and wakes what sleeps
for x in (110, 111, 112):
    L.put(x, 25, "H", K["H"])           # gate across the plaza's north wall
for y in (25, 26):
    for x in (110, 111, 112):
        L.grid[y][x] = "H"
L.legend["H"] = K["H"]
for (x, y) in [(104, 30), (118, 30), (104, 44), (118, 44), (111, 44), (100, 37), (122, 37)]:
    L.put(x, y, "n", K["n"])
L.put(111, 4, "E", K["E"])

# ---- room groups (RoomLoader) -----------------------------------------------------------------
L.group("West", 0, 33)
L.group("Center", 34, 62)
L.group("Fields", 63, 93)
L.group("East", 94, 127)

# ---- bot route (text coords): the region bot plays these steps in order -----------------------
L.bot = [
    "read 23 46", "drink 25 52", "goto 30 45", "fight 47 44 9", "rest 56 44",
    "goto 63 45", "walk 66 41", "walk 70 42", "walk 72 47", "walk 75 50", "walk 74 55", "fight 74 60 6", "take 74 61",
    "walk 74 55", "walk 78 49", "walk 86 47", "walk 92 47", "goto 92 52", "goto 92 47",
    "goto 98 44", "goto 96 27", "goto 96 21", "rest 95 14",
    "goto 88 14", "trim on", "goto 74 14", "goto 52 12", "goto 32 12", "trim off",
    "walk 26 12", "walk 22 8", "walk 17 7", "take 12 7", "walk 17 7", "walk 26 12", "walk 30 12",
    "trim on", "goto 52 12", "goto 74 14", "trim off", "rest 95 14",
    "goto 96 27", "goto 108 32", "ring 111 33", "fight 111 34 10", "goto 111 24", "goto 111 6", "exit 111 4",
]

missing = L.check_connected((18, 48))
if missing:
    raise SystemExit(f"unreachable cells: {missing[:10]} ({len(missing)})")
print("level:", L.save("R1_TallowFields.txt", "# Region 1: wax plain, low ambient light."))

if __name__ == "__main__":
    import tiles
    colors = {"P": (80, 220, 120), "B": (255, 160, 60), "W": (240, 240, 230), "G": (230, 120, 40), "m": (200, 60, 60),
              "f": (255, 230, 150), "R": (255, 210, 60), "F": (90, 170, 255), "K": (140, 120, 90), "k": (110, 95, 70),
              "L": (190, 160, 90), "H": (120, 120, 120), "E": (60, 255, 255), "I": (180, 180, 255), "n": (140, 30, 30)}
    print("preview:", L.preview(tiles.TALLOW, tiles.tallow_face_detail, [tiles.wax_grass(0, 0), tiles.wax_cracks(0)],
                                "level_r1.png", colors))
