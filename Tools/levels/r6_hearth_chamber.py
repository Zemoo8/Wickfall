"""
Region 6 - Chambre du Foyer (PDF §2.2: an ash cathedral, the Foyer in negative light; ash that slows;
the Roi Creux, a boss in 3 phases). PDF §2.4: the King's pacts (optional). PDF §1: the endings.

Beats: wading through ash; the King's voice offering pacts (G11); the Hearth Ember (the relic found
before the final choice, G04); the King's three phases, each taking a support away (G13 + PDF); his
fall and his fate; the Grand Foyer and the choice of ending (G12).

Route: the ash nave -> the pact gallery (4 shrines, the relic in the west alcove, a brazier in the east)
-> the arena (the Roi Creux) -> the Grand Foyer beyond it.

    python Tools/levels/r6_hearth_chamber.py
"""
from carve import Level

L = Level("R6 Chambre du Foyer", 80, 90, seed=66)
K = {
    "P": "PlayerStart", "B": "Brazier", "W": "WaxPool", "w": "ArenaPool", "R": "Relic", "k": "HollowKing", "O": "Foyer", "a": "Ash",
}

# ---- A: the ash nave -----------------------------------------------------------------------------------------
L.rect(20, 70, 60, 86)
for (x, y) in [(24, 72), (56, 72), (24, 83), (56, 83)]:
    L.pillar(x, y, 1, 1)
L.corridor([(40, 69), (40, 62)], 3)

# ---- B: the pact gallery, the relic alcove (west), the brazier (east) ----------------------------------------
L.rect(14, 48, 66, 61)
L.rect(4, 50, 13, 58)
L.corridor([(40, 47), (40, 38)], 5)

# ---- C: the arena and the Grand Foyer --------------------------------------------------------------------------
L.ellipse(40, 24, 22, 13)
L.rect(30, 4, 50, 12)
for (x0, y0, x1, y1, c) in [(20, 70, 60, 86, 0.03), (4, 48, 66, 61, 0.03), (18, 11, 62, 37, 0.02)]:
    L.decorate(x0, y0, x1, y1, c)

# ---- ash that slows (PDF): bands across the nave, drifts in the gallery and the arena ------------------------
def ash(x0, y0, x1, y1):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if L.grid[y][x] in ".,":
                L.grid[y][x] = "a"
ash(21, 76, 59, 77)
ash(21, 79, 40, 79)
ash(16, 52, 26, 54)
ash(54, 52, 64, 54)
ash(22, 30, 30, 33)
ash(50, 30, 58, 33)
ash(36, 32, 44, 35)
L.legend["a"] = K["a"]

# ---- entities ----------------------------------------------------------------------------------------------------
L.put(40, 84, "P", K["P"])
L.put(34, 82, "B", K["B"])
L.inscription(46, 83, 0)                        # 0: ash slows the step
for i, x in enumerate((20, 32, 48, 60)):
    L.put(x, 49, "pqrs"[i], f"PactShrine{i}")   # G11: four pacts offered by the King's voice
L.inscription(40, 59, 1)                        # 1: the King offers pacts; they cannot be undone
L.put(7, 54, "R", K["R"])
L.inscription(9, 57, 2)                         # 2: an ember of the Foyer waits here
L.put(63, 55, "B", K["B"])
L.put(60, 57, "W", K["W"])
L.inscription(43, 40, 3)                        # 3: the Hollow King waits under his crown
for (x, y) in [(24, 26), (56, 26)]:
    L.put(x, y, "w", K["w"])                    # the arena's pools (G13: phase 1 darkens them)
L.put(40, 22, "k", K["k"])
L.put(40, 8, "O", K["O"])
L.inscription(33, 12, 4)                        # 4: the cold Foyer drinks the light

# ---- room groups and the bots (one route per ending; the level file keeps the A route) ------------------------
L.group("South", 0, 79)

ROUTE = [
    "rest 34 82", "read 46 83", "goto 40 70", "read 40 59", "goto 14 54", "read 9 57", "take 7 54",
    "goto 40 55", "rest 63 55", "goto 40 46", "read 43 40", "boss 40 22",
]
L.bot = ROUTE + ["judge 40 22 extinguish", "ending 40 8 A"]

lost = L.check_connected((40, 84))
if lost:
    raise SystemExit(f"unreachable cells: {lost[:10]}")
print("level:", L.save("R6_HearthChamber.txt", "# Region 6: ash, pacts, the Roi Creux, the Grand Foyer, the endings."))

if __name__ == "__main__":
    import tiles
    colors = {"P": (80, 220, 120), "B": (255, 160, 60), "W": (240, 240, 230), "R": (255, 210, 60), "k": (10, 10, 10),
              "O": (255, 120, 40), "a": (190, 185, 175), "p": (60, 30, 80), "q": (60, 30, 80), "r": (60, 30, 80), "s": (60, 30, 80)}
    print("preview:", L.preview(tiles.HEARTH, tiles.hearth_face_detail, [tiles.cinder(0)], "level_r6.png", colors))
