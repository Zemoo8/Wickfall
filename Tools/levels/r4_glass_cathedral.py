"""
Region 4 - Cathédrale de Verre (PDF §2.2: black mirrors, physical beams to aim; lasers (25 damage),
glass floors that break under the crimson flame; Reflections (copy your last 3 actions)).

Teaching beats (prompt §9): black mirrors and beams aimed with crystal pedestals; lasers; glass broken
by crimson (a hazard and a way to open paths); Reflections; the Rose, where each panel needs a
different flame (G23). Crimson is given on entering (G03/G47); the relic here is the extra flame slot
(G04), found before the Rose so all five flames fit (G53).

Route: the nave (turn the pedestal: the beam wakes socket A, gate A opens) -> the laser hall -> the
relic chapel -> the glass pane only crimson breaks -> the glass bridge (cross it without crimson) ->
the Reflection hall and the Rose (aim its beam, then light each petal with its flame: gate C) -> the
bell plaza and the exit.

    python Tools/levels/r4_glass_cathedral.py
"""
from carve import Level

L = Level("R4 Cathédrale de Verre", 106, 90, seed=44)
K = {
    "P": "PlayerStart", "B": "Brazier", "W": "WaxPool", "r": "Reflection", "R": "Relic", "L": "GreatBell", "H": "Gate",
    "E": "Exit", "a": "GateA", "c": "GateC", "e": "BeamLampE", "q": "Pedestal0", "s": "SocketA", "o": "RoseC",
    "z": "LaserE", "m": "MirrorSlash", "x": "GlassBarrier", "g": "GlassFloor", "n": "SwarmNestReflection",
}

# ---- A: the nave and its first beam -----------------------------------------------------------------
L.rect(6, 78, 34, 88)
L.decorate(6, 78, 34, 88, 0.03)
L.corridor([(29, 77), (29, 68)], 3)

# ---- B: the laser hall --------------------------------------------------------------------------------
L.rect(20, 56, 60, 68)
L.decorate(20, 56, 60, 68, 0.03)
L.corridor([(60, 61), (70, 61)], 3)

# ---- C: the relic chapel ------------------------------------------------------------------------------
L.rect(70, 54, 90, 68)
L.pillar(82, 57, 1, 1)
L.pillar(82, 65, 1, 1)
L.decorate(70, 54, 90, 68, 0.03)
L.corridor([(80, 53), (80, 43)], 3)

# ---- D: the glass bridge over the dark ------------------------------------------------------------------
L.rect(66, 28, 100, 43)
L.wall_rect(66, 34, 100, 38)                    # the chasm
L.decorate(66, 28, 100, 43, 0.03)

# ---- E: the Reflection hall and the Rose --------------------------------------------------------------
L.corridor([(66, 31), (56, 31)], 3)
L.rect(28, 20, 56, 40)
for (x, y) in [(32, 23), (52, 23), (32, 37), (52, 37)]:
    L.pillar(x, y, 1, 1)
L.decorate(28, 20, 56, 40, 0.03)
L.corridor([(42, 19), (42, 14)], 3)

# ---- F: the bell plaza ----------------------------------------------------------------------------------
L.rect(26, 6, 58, 13)
L.wall_rect(26, 4, 58, 5)
L.corridor([(42, 3), (42, 2)], 3)
L.decorate(26, 6, 58, 13, 0.03)

# ---- the light puzzles (beams run through cell centres) -------------------------------------------------
L.put(6, 81, "e", K["e"])                       # lamp shooting east along row 81
L.put(20, 81, "q", K["q"])                      # pedestal: turn it north (step 2)
L.put(20, 78, "s", K["s"])                      # socket A on the nave's north edge
for x in (28, 29, 30):
    L.grid[76][x] = "a"                         # gate A in the corridor
L.legend["a"] = K["a"]
for y in (58, 64, 66):                          # lasers across the hall (none on the exit rows 60-62)
    L.put(20, y, "z", K["z"])
L.put(46, 58, "m", K["m"])                      # a black mirror bounces the north laser up the wall
for x in (79, 80, 81):
    L.put(x, 48, "x", K["x"])                   # the pane only crimson breaks
for y in range(34, 39):                         # the glass bridge
    for x in (79, 80, 81):
        L.put(x, y, "g", K["g"])
L.put(28, 24, "e", K["e"])                      # the Rose's lamp, east along row 24
L.put(42, 24, "q", K["q"])                      # turn it south (step 6) onto the Rose's eye
L.put(42, 32, "o", K["o"])                      # the Rose (its eye at the centre), opens gate C
for x in (41, 42, 43):
    L.grid[17][x] = "c"
L.legend["c"] = K["c"]

# ---- entities -------------------------------------------------------------------------------------------
L.put(10, 86, "P", K["P"])
L.put(14, 84, "B", K["B"])
L.inscription(12, 83, 0)                        # 0: black glass sends the light back
L.inscription(24, 67, 1)                        # 1: the rays burn: wait until they fall silent
L.put(86, 61, "R", K["R"])
L.put(74, 64, "B", K["B"])
L.put(72, 64, "W", K["W"])
L.inscription(76, 56, 2)                        # 2: only crimson breaks black glass
L.inscription(76, 41, 3)                        # 3: glass carries whoever does not burn red
for (x, y) in [(34, 36), (50, 36), (50, 27)]:
    L.put(x, y, "r", K["r"])
L.inscription(31, 38, 4)                        # 4: each petal wants its own flame
L.put(47, 38, "W", K["W"])                       # a pool in the Rose hall: the swaps cost wax
L.put(42, 9, "L", K["L"])
L.inscription(32, 11, 5)                        # 5: the bell answers the Rose
for x in (41, 42, 43):
    for y in (4, 5):
        L.grid[y][x] = "H"
L.legend["H"] = K["H"]
for (x, y) in [(28, 8), (56, 8), (28, 12), (56, 12)]:
    L.put(x, y, "n", K["n"])
L.put(42, 2, "E", K["E"])

# ---- room groups and the bot --------------------------------------------------------------------------
L.group("West", 0, 61)
L.group("East", 62, 105)

L.bot = [
    "rest 14 84", "read 12 83", "aim 20 81 2", "goto 29 74", "goto 29 69", "read 24 67",
    "goto 59 61", "goto 75 61", "take 86 61", "rest 74 64", "read 76 56",
    "smash 80 48", "flame 0", "read 76 41", "goto 80 40", "walk 80 32", "goto 60 31",
    "fight 42 30 13", "aim 42 24 6", "read 31 38", "rose 42 32", "drink 47 38", "goto 42 16",
    "read 32 11", "ring 42 9", "fight 42 10 15", "goto 42 6", "exit 42 2",
]

lost = L.check_connected((10, 86))
if lost:
    raise SystemExit(f"unreachable cells: {lost[:10]}")
print("level:", L.save("R4_GlassCathedral.txt", "# Region 4: black mirrors, beams, lasers, crimson glass, the Rose."))

if __name__ == "__main__":
    import tiles
    colors = {"P": (80, 220, 120), "B": (255, 160, 60), "W": (240, 240, 230), "r": (20, 20, 20), "R": (255, 210, 60),
              "L": (190, 160, 90), "H": (120, 120, 120), "E": (60, 255, 255), "a": (140, 140, 140), "c": (140, 140, 140),
              "e": (240, 240, 220), "q": (200, 200, 255), "s": (250, 250, 200), "o": (255, 120, 200), "z": (255, 180, 80),
              "m": (90, 90, 120), "x": (60, 60, 90), "g": (40, 40, 70), "n": (60, 30, 80)}
    print("preview:", L.preview(tiles.CATHEDRAL, tiles.cathedral_face_detail, [tiles.crystal_edge(0), tiles.glass_glint(0)], "level_r4.png", colors))
