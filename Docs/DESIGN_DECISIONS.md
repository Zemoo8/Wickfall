# Design decisions

Everything the PDF does not define, with the decision taken. The PDF is never changed by these:
they only fill gaps.

**Status key**
- **Default** — the kickoff prompt's recommended default, taken because the user said on 2026-10-17
  *"dont ask me … always chose recommended"*. Reversible; tell Claude to change any row.
- **Proposed** — a decision made during development that the prompt did not list. Reversible.
- **Approved** / **Rejected** — the user's explicit answer.

Rows marked **ASK** in the prompt (G01, G13, G16, G17) were normally blocking; they took their default
for the same reason and are listed first in the review summary.

## G-table (prompt §4)

| ID | Gap | Decision | Where | Status |
|---|---|---|---|---|
| G01 (ASK) | What "Coût en cire" multiplies | Passive wax loss while that flame is lit, and that flame's art cost. Basic attacks are free. | `WaxSystem.OnFlameSwitched`, `FlameTypeSO.ArtWaxCost` | Default |
| G02 | Flame arts | One art per flame from its PDF role: Amber wide sweep, Blue piercing bolt that slows, Green poison burst, White radiant pulse (hurts Creux, flares light), Crimson low-wax strike. Cost = base 6 × the flame's cost multiplier; none while trimmed. | `PlayerCombat.Execute` | Default |
| G03 | Flame unlocks and slots | Start with Amber; Blue in R1, Green in R2, White in R3, Crimson entering R4. 4 slots, the flame-slot relic makes 5. Loadout chosen at braziers. Puzzles never need a flame you cannot have yet. | `RegionSO.flameFound`, `FlameSwitcher` | Default |
| G04 | The 6 relics | R1 extra dodge, R2 wall grip (climb wick-vines), R3 +20 max wax, R4 flame slot (the PDF's four), R5 **Spare Wick** (once per rest the flame relights with 25 wax instead of going out), R6 **Hearth Ember** (arts cost 25 % less), found in the Chambre du Foyer before the final choice. | `PdfTables.ApplyRelic`, `PlayerModifiers` | Default (R5/R6 Proposed) |
| G05 | Dodge numbers | 1 charge (+1 relic), 2.5 units in 0.18 s, 0.18 s invulnerable, 0.9 s recharge, no wax cost. | `BalanceSO` | Default |
| G06 | Combat numbers | All in data assets, starting values in `Docs/BALANCE.md`. PDF numbers never change (checked by `PdfValuesTests`). | `Assets/Data` | Default |
| G07 | How multipliers combine | base = lerp(1.2, 6, wax/100); radius = base × flame light × temporary effects × (0.35 trimmed); detection = (2 + untrimmed radius × 1.5) × (0.4 trimmed); crimson = lerp(2.2, 0.6, wax/100); wax above 100 adds buffer, not radius. | `LightMath`, `CombatMath` (EditMode-tested) | Default |
| G08 | "Un usage par repos" | Resting saves and refills to max once; that brazier's refill is spent until you die or rest at another. Enemies do not respawn on rest. | `GameSession.Rest` | Default |
| G09 | Score details | Time bonus = max(0, par − time) × 1 point/s; wax % = wax / max wax at the end; an enemy is "avoided" if it noticed you (or you entered its detection range), was never damaged by you and was alive when the region ended. Rank thresholds 700 / 1200 / 1700, tuned from bot runs. | `ScoreCalculator`, `RegionTracker`, `ScoreConfigSO` | Default |
| G10 | "Pour rejouer" | Region Select in the menu once a region is cleared; best score and rank saved. | M7 | Default |
| G11 | Pacts | 4 pacts at shrines voiced by the Roi Creux: the PDF's own (+30 max wax, loss ×1.5) and three proposals (damage ×1.3 / −20 max wax; detection ×0.75 / light ×0.8; shards ×1.5 / +4 wax lost per hit). | `PdfTables.ApplyPact` | Default |
| G12 | Choosing the ending | After phase 3 the Roi Creux falls: extinguish or spare him. At the Foyer: relight (A) or keep the flame (B); pass the wick (C) appears only with 6 relics and the King spared. Endings gallery. | M6 | Default |
| G13 (ASK) | Boss phase 1 removal | Phase 1: the arena's wax pools go dark; phase 2: no trim (PDF); phase 3: max wax 40 (PDF). Removals accumulate. | `ProgressRules`, `BossConfigSO` | Default |
| G14 | Great Bell, Broken Wick | Broken Wick: R1 landmark where the journey starts and where ending C happens. Great Bell at each region's exit gate: ringing opens the way and wakes the region's swarm. | M2+ | Default |
| G15 | "Boss réveillé" | No new boss species; each region climaxes with an elite encounter of its own creatures. | M2+ | Default |
| G16 (ASK) | Story text | Nothing narrative beyond short inscriptions until the user approves an outline. | — | Default |
| G17 (ASK) | "Déplacement 4 directions" | Both modes exist behind a setting: (a) literal 4-way (default) and (b) 8-way input with 4-direction facing. | `PlayerInputReader`, `ControlsSO` | Default |
| G18 | Wax drops | Pooled, capped (48) glowing ground drops left by every hit; stay for the region; cannot be picked up. | `WaxDropField` | Default |
| G19 | Shop | Wax candle (+25 wax), soot salve, two upgrade tiers per flame (damage +15 %, art cost −15 %). | `PdfTables.CreateShop` | Default |
| G20 | Cirelles | Harmless ambient life that glows and scatters; not counted as "avoided". | M2 | Default |
| G21 | 12–15 h | Build all six regions complete first; measure real playtime and report the true number. Never pad. | `Docs/PERFORMANCE.md` / reports | Default |
| G22 | Death penalty | None beyond the return to the brazier. | `GameSession.Respawn` | Default |
| G23 | The Rose | R4 climax: 5 panels, each lit only by its own flame, combined with beam pedestals. | M5 | Default |
| G24 | Map | None. | — | Default |
| G25 | "Sauvegarde chiffrée" | AES-256-CBC + HMAC-SHA256, keys built in code (tamper deterrence, not security), backup copy, corrupted save falls back to the backup. | `SaveCodec`, `SaveStore` | Default |
| G26 | Controls | Keyboard WASD/arrows, J/left click attack, K/right click art, Space dodge, hold Left Shift trim, hold Q wheel / 1–5 / Tab, R / mouse wheel, E interact, Esc pause, backquote dev overlay. Gamepad: stick, X attack, Y art, A dodge, LT trim, LB wheel, RB next flame, B interact, Start pause. Rebindable; hold/toggle trim option. | `GameInput` | Default |
| G27 | Art not covered by the PDF | Written in the PDF's format, logged as "not in PDF" in `Docs/ART_PROMPTS.md`. | `Docs/ART_PROMPTS.md` | Default |

## Decisions made while building (G28+)

| ID | Question | Decision | Where | Status |
|---|---|---|---|---|
| G28 | Soot: how long does "Dégâts ×1,25" last, how much light is lost? | A soot hit removes exactly the PDF's 10 wax, *then* applies "sooted" for the PDF's 1.5 s: hits taken ×1.25 and light radius ×0.5. | `PlayerDamageable`, `LightConfig.sootLightMultiplier` | Proposed |
| G29 | Bloom: "lumière coupée" — to what? | The light radius drops to 0.55 units (a bare ember around the flame) for the PDF's 3 s. | `LightConfig.lightCutRadius` | Proposed |
| G30 | What "Anti-Creux" does | The white flame deals ×1.5 extra to Creux creatures (Rôdeurs, Reflets, Éteints, Roi Creux) on top of its PDF ×1.6. | `FlameTypeSO.hollowBonus` | Proposed |
| G31 | "Poches d'obscurité où se cacher" | Standing in a dark zone (canopy, shadow pocket) halves the distance enemies spot you from (the PDF's ×0.5); walls block sight lines, the canopy does not. | `DarkZone`, `LightController` | Proposed |
| G32 | Wax pools | A pool holds 35 wax, drunk with the interact key, and fills again when you rest at a brazier or die. | `WaxPool` | Proposed |
| G33 | Basic attacks per flame | The basic attack follows the flame's PDF role: Amber melee arc, Blue ranged bolt that slows, Green poison arc that hits groups, White radiant slash, Crimson heavy strike (crimson damage curve). | `PlayerCombat`, `FlameTypeSO.attackStyle` | Proposed |
| G34 | A flame switch at very low wax | Switching (8 wax) and arts are refused if paying would put the flame out: dying is never a menu action. | `WaxModel.TrySpend` | Proposed |
| G35 | Kill rewards per creature | Each creature has its own range inside the PDF's 4–15 (Dripling 4–6 … Slagborn 13–15). | `PdfTables.ApplyEnemy` | Proposed |
| G36 | Art route in practice | Gemini (the PDF's tool) for design references and static art; animated characters are drawn in code on the exact PDF grid from the approved style so every frame stays consistent (prompt §10 allows "frames from an approved key frame with scripted pixel edits"). | `Tools/art/*`, `Docs/ART_PROMPTS.md` | Proposed |
| G37 | Pixel font | "Wickfall Pixel", drawn for the game (CC0), 5×7 glyphs, full French coverage, K/X/H distinct. | `Tools/font/make_font.py` | Proposed |
| G38 | Canopy look | A dark zone is shown with a subtractive "negative" 2D light, the same blend style the Foyer uses. | `SceneFactory.AddDarkZone` | Proposed |
| G39 | Light Lab | A sandbox scene (not part of the campaign) with all five flames, effigies that show the flame-versus-matter rules, geysers and a dark zone. Never saves. | `Assets/Levels/LightLab.txt` | Proposed |
| G40 | Hit flash, hit-stop, popups | Short hit-stop on landed hits, a white hit flash, damage numbers and "BLOQUÉ" when a flame cannot hurt a target. Shake and flash have accessibility sliders. | `FeedbackDirector`, `DamagePopups` | Proposed |
| G41 | Husk ambush ("Carcasses (embuscade)") | A Husk hangs from a branch until the player comes within 2.6 units *while detectable* (a trimmed wick slips past) or strikes it; it drops (0.6 s), then hunts like any creature. | `HuskBrain` | Proposed |
| G42 | "Rôdeurs (bougent si vous brillez)" | A Lurker moves and attacks only while the player shines (wick not trimmed, flame lit); otherwise it freezes in place, eyes glinting. | `LurkerBrain` | Proposed |
| G43 | When a Light-Eater Bloom feeds | When the player's light reaches it (¾ of the light radius + 0.4 u) it opens (its hazard telegraph), then cuts the light for the PDF's 3 s; a trimmed wick lights too little ground to wake it. It can be cut down for a kill reward. | `LightEaterBloom` | Proposed |
| G44 | "Vignes à grimper" | Wick-vines climb R2's root cliff, the only way north; they need R2's relic ("accroche au mur", G04). Without it a short message says the vines slip under the wax. | `Vine`, `R2_Wickwood` | Proposed |
| G45 | Canopy art | One overhead leaf image per region, rendered from the level's canopy cells (ragged rims, leaf clumps lit from the top left); it fades to 30 % while the player is beneath it. | `Tools/art/canopy.py`, `Canopy` | Proposed |
| G46 | Interact timing | Interact is buffered like attack and dodge (0.15 s); when several presses wait, the most recent wins. | `PlayerController` | Proposed |
| G47 | Entering a region without an earlier flame (a skipped shrine, region select, tests) | Entering a region guarantees Amber, every earlier region's flame and, from R4 on, Crimson (G03's order), equipped in a free slot. No puzzle then needs a flame you cannot have (G03). | `ProgressRules.FlamesGuaranteedOnEntering`, `GameSession.OnSceneProfile` | Proposed |
| G48 | Press size and look | A press blocks a 2–3 cell lane (hit radius 1.1); its head hangs 1.2 units over a striped plate, shudders and glows for the 0.9 s warning, slams (35, PDF) and lifts in 0.5 s. | `PdfTables` (Press), `Press` | Proposed |
| G49 | Steam jets | A floor vent hisses for 0.6 s, then vents for 1.1 s along 5 cells: one scald of 6 per vent and a 5 u/s push (G06 values) that can carry you onto a belt. | `SteamJet` | Proposed |
| G50 | Conveyors | A belt carries anyone standing on it at 2.4 u/s (creatures too); a dodge is a jump over it. | `Conveyor` | Proposed |
| G51 | Furnaces | Furnaces are light sources that gutter, sway and sometimes nearly die, so the walls' shadows move; they are solid, and they do not change detection (the PDF ties detection to your own light). | `Furnace` | Proposed |
| G52 | Bellows behaviour | A Bellows keeps its distance (it backs off under 2.4 u) and fires a soot ball every 2.2 s from 6 u; the ball removes the PDF's 10 wax, then soots (G28). | `BellowsBrain`, `PdfTables` | Proposed |
| G53 | A new flame slot with a flame waiting | When a slot opens (R4's relic), an unlocked flame that had no room takes it; R4's relic is placed before the Rose so all five flames fit. | `GameSession.ApplyProgress`, `R4_GlassCathedral` | Proposed |
| G54 | "Faisceaux physiques à orienter" | Crystal lamps cast steady pale beams along cell centres; walls stop them, glass lets them through; black mirrors (fixed, 45°) reflect them; crystal pedestals turn 45° per use and send any beam along their facing; a socket held in a beam for 0.4 s opens its gate for good. Puzzle beams do not burn. | `BeamTracer`, `LightBeam`, `BlackMirror`, `CrystalPedestal`, `BeamSocket` | Proposed |
| G55 | Lasers | An emitter charges for 0.8 s (a flickering thin line and a whine), burns for 1.3 s every 3.2 s (G06), 25 per burn (PDF); black mirrors bounce lasers too. | `LaserEmitter`, `PdfTables` | Proposed |
| G56 | Glass under the crimson flame | Glass panes break only to crimson (they open paths). Glass floors crack in two 0.4 s stages while you stand on them carrying crimson, then give way: you fall (12) and climb back to the last solid ground; the hole mends when you rest or relight. | `GlassBarrier`, `GlassFloor` | Proposed |
| G57 | The Rose | A rose mosaic in the cathedral floor. Its eye must be lit by a beam (aimed with a pedestal); then each of its 5 petals lights only when struck with its own flame (PDF §2.3); all five open the way on. | `RoseWindow`, `RosePanel` | Proposed |
| G58 | How a Reflection copies "your last 3 actions" | Close in, it plays back your last three actions, oldest first, as a mirror image: a swing becomes its swing, an art its burst, a dodge its mirrored dodge, a flame swap its flare. With nothing to copy it strikes once. | `ReflectionBrain`, `ActionRecorder` | Proposed |
| G59 | The Unlit | They drift toward the most attractive light they feel (strength weighed against distance): your flame, a lit brazier, or for 2.5 s any flare (swap, art, kill). Touching one drains 12 wax at most once a second. They have no eyes: they never "alert". | `UnlitBrain`, `LightSources` | Proposed |
| G60 | "Carte visible seulement où vous brillez" | In the Creux Profond the ground is drawn only where light reaches; beyond it everything is black, with a faint pale rim at the edge of your light. | `LightReveal.shader` | Proposed |
| G61 | The Roi Creux's fight | 260 health per phase (780). He wakes when you come near, drifts toward you and attacks every 1.6 s (faster each phase): a slam (1 s warning shown where it will land, 20, radius 3.2), a volley of 6 voids (12 each), from phase 2 an ash wave (10 and slowed ×0.55 for 2 s), in phase 3 a pulse of negative light (your flame is swallowed within 5.5 u for 2.4 s). Each phase change: 1.5 s invulnerable, the crown breaks further, the next support goes (G13). If you go out, the fight starts over. | `BossHollowKing`, `BossConfigSO` | Proposed |
| G62 | How choices are made | Pacts, the King's fate and the Foyer ask through one `Choice` system: a person answers in a paused dialog; bots and tests answer through the same call. A disabled option (C without its conditions) is refused. | `Choice`, `ChoiceView` | Proposed |
| G63 | Ending screens | Each ending shows its PDF line only (plus "Endings found n/3"), then back to the menu; no added story (G16). | `EndingView`, `Ending` scene | Proposed |
| G64 | Where the pacts are | Four pact shrines in R6's gallery, before the King; each can be taken once and never undone; refusing costs nothing. | `PactShrine`, `R6_HearthChamber` | Proposed |
| G65 | The last region's shape | R6 has no bell and no exit: the ash nave, the pact gallery (relic, brazier), the King's arena (two pools, G13) and the Grand Foyer beyond it, which can be used once the King is judged. | `R6_HearthChamber` | Proposed |
| G66 | First-launch language | Always French (the game's language); English is one press away in the menu and the options. | `Loc.SystemDefault` | Proposed |
| G67 | Menus | Main menu: Continue, New game, Region select (once a region is cleared, with best score and rank), Endings (once one is found), How to play (controls and the rule), Options, Language, Credits, Light Lab, Quit. | `MainMenu` | Proposed |
| G68 | Credits | The author line uses the name on the account ("Ahmed Baghouli"); please correct it in `Strings.txt` (`credits.2`) if it is wrong. | `Strings.txt` | **Check** |
