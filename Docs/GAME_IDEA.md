# WICKFALL: the game idea in plain English

The idea comes from `Wickfall_Idee_Complete.pdf` (sections 1 and 2), the design file the user gave us.
Where the PDF says nothing, the choice is logged in `Docs/DESIGN_DECISIONS.md` (the G-numbers).

## The idea in one sentence
You are a candle that must relight a dead fire, but the light you make is also what lets the
monsters find you.

## Words you need
- **Wickbearer (Porte-mèche):** you, a candle golem.
- **Wax:** your health and your light, in one bar. At 0 you go out.
- **Light:** the glow around you. More wax, bigger light. You see farther, and enemies see you farther.
- **Hollows (Creux):** dark shapes with no light inside. They are the enemies.
- **Great Hearth (Grand Foyer):** the fire you must relight at the end.
- **Hollow King (Roi Creux):** the boss at the end.
- **Flames:** five kinds of fire you can switch between (Amber, Blue, Green, White, Crimson). Each hurts some enemies more, and switching costs wax.
- **Trim the wick (sneak):** hold Shift. Your light gets small, enemies see you much less, but you hit weaker.
- **Brazier:** a checkpoint. Resting there refills your wax, and it is where you come back after going out.
- **Great Bell:** ring it to open a region's exit. Its sound wakes the enemies nearby.
- **Relic:** one permanent power, one per region.
- **Pact:** an optional deal with the Hollow King: a bonus with a permanent cost.

## The golden rule
**You cannot be strong and hidden at the same time.** Bright: you see and hit hard, but everything sees you.
Dim: you hide, but you are weak.

## One loop
Explore in the dark, find a flame, an enemy or a relic, decide to shine or hide, fight, pick up wax,
rest at the next brazier, repeat.

## The six regions
1. Tallow Fields: a wax plain in low light.
2. Wickwood: a black forest; the canopy hides you.
3. Soot Foundries: a factory of presses, steam and conveyors.
4. Glass Cathedral: aim light beams with crystals; mirrors copy your moves.
5. Deep Hollow: no light at all; you only see what you light.
6. Hearth Chamber: the Hollow King, then the endings.

## The three endings
- **A:** relight the Hearth, and you die in it.
- **B:** keep the flame, and stay alone.
- **C (secret):** pass the wick to someone else. Needs all 6 relics and the King spared.

## What is from the PDF and what we added
- **From the PDF:** wax = life = light, the golden rule, the six regions and their enemies, the five
  flames and their roles, the score and ranks, the three endings, the candle look and its glow.
- **Added by us (logged as proposals):** the relic names (the PDF gives only their effects), the boss's
  numbers per phase, how the Rose puzzle works, the shop's items, the extra pact options, the drawing
  of the pixel art (the PDF gives sizes and colours), and the captions in the gameplay videos.

## Still to write
- A full game design document (`Docs/GDD.md`), and the others the docs already point to:
  `ART_PROMPTS.md`, `TRACEABILITY.md`, `LEVEL_DESIGN.md`, `ART_BIBLE.md`, `PROJECT_MAP.md`, `PERFORMANCE.md`.
