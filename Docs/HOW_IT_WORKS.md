# How WICKFALL works in Unity — a guide you can follow

This guide explains, in plain words, how the game is built in Unity: what each idea means, and **where
it lives in the project** (file and line) so you can open it and follow along. Paths are clickable in
VS Code / Rider. Line numbers are correct on 2026-10-18; if code moves, search for the name in bold.

Each section ends with **"Say it to the professor"**: a two-line answer in French you can reuse.

---

## 0. Opening and running the project

1. Open **Unity Hub** → *Add* → choose the folder `C:\Users\LENOVO\RPG-DS` → open with **Unity 6000.2.7f2**.
2. In the *Project* window open `Assets/Scenes/Boot.unity` and press **Play**. Boot loads the **Core**
   scene (player, camera, menus, sound) and then the **Main Menu**.
3. You can also open **any** level directly (for example `Assets/Scenes/Regions/R1/R1_TallowFields.unity`)
   and press Play: a small helper, **CoreBootstrap**, loads Core for you.
4. Controls: WASD / arrows move · J or left click attack · K or right click flame art · Space dodge ·
   hold Left Shift trim the wick · Q flame wheel (or 1–5, Tab) · E interact · Esc pause ·
   **`** (backquote) developer overlay. A gamepad works too.

> The editor's top bar has a **WICKFALL** menu. `WICKFALL > Setup > Run Project Setup` rebuilds all
> generated content (data, sprites, prefabs, scenes). You never need to place things by hand.

---

## 1. The five Unity ideas everything else uses

| Unity idea | Plain meaning | Example in WICKFALL |
|---|---|---|
| **Scene** | A level or a screen saved as a file. | `Assets/Scenes/Regions/R2/R2_Wickwood.unity` |
| **GameObject** | A "thing" in a scene (an empty box). | the player, a brazier, the camera |
| **Component** | A piece of behaviour or data attached to a GameObject. A C# script that inherits `MonoBehaviour` is a component. | `PlayerController`, `Rigidbody2D`, `Light2D` |
| **Prefab** | A saved GameObject you can copy into many scenes. | `Assets/Prefabs/Enemies/Husk.prefab` |
| **ScriptableObject** | A data file (numbers, names) that is not in a scene. We keep all tuning there so designers change numbers without touching code. | `Assets/Data/Config/BossConfig.asset`, the flames, the enemies |

**The frame loop.** Unity calls special methods on every component, many times a second:

- `Update()` — once per frame (input, timers, AI decisions).
- `FixedUpdate()` — at a fixed rate (50 times a second) for physics.
- `LateUpdate()` — after all `Update`s (the camera uses it, so it moves after the player moved).

> **Say it to the professor:** « Unity assemble des *GameObjects* avec des *composants*. Chaque script
> hérite de `MonoBehaviour` et Unity appelle `Update` chaque image et `FixedUpdate` pour la physique. »

---

## 2. How the game is organised (scenes)

- **Core** (`Assets/Scenes/Core.unity`) is loaded once and stays: player, camera, HUD, menus, audio,
  the save system. It is never unloaded during play.
- **Content scenes** (main menu, each region, the ending) load *on top of* Core.
- Each region is split into **room groups** (`R1_TallowFields_West.unity`, `..._East.unity`…). The
  **RoomLoader** loads the groups near the player and unloads far ones, so memory stays small.
  → `Assets/Scripts/Runtime/World/RoomLoader.cs` (method **Refresh**, line ~73: load if closer than
  20 units, unload if farther than 30).
- A **SceneProfile** in each content scene tells Core what it is (menu or gameplay, which region, where
  the player starts, which music). → `Assets/Scripts/Runtime/Meta/SceneProfile.cs`

> **Say it to the professor:** « Une scène *Core* persistante contient le joueur et les systèmes ; les
> niveaux sont chargés en plus, par groupes de salles, et déchargés quand on s'éloigne. »

---

## 3. How the player moves (the question from last time)

Movement goes through **four small steps**. Each step is one file.

```
Keyboard / gamepad ──► GameInput ──► PlayerInputReader ──► PlayerController (state machine) ──► Rigidbody2D
    (hardware)         (actions)       (cleans the input)     (decides the speed)              (physics moves it)
```

**Step 1 — Input actions.** `Assets/Scripts/Runtime/Core/GameInput.cs`, line ~58: an action called
**Move** is created and bound to WASD, the arrows and the left stick. We use Unity's *Input System*
package, so keyboard and gamepad give the same `Vector2` (x = left/right, y = down/up, length 0 to 1).

**Step 2 — Reading and cleaning.** `Assets/Scripts/Runtime/Player/PlayerInputReader.cs`, method
**Read** (line ~23). The PDF says "déplacement 4 directions", so **FourWay** (line ~83) keeps only the
strongest axis: pressing W+D gives "up" or "right", never a diagonal. (An option allows 8 directions.)
The result goes into a small struct, **PlayerIntent** ("what the player wants").

**Step 3 — Deciding.** `Assets/Scripts/Runtime/Player/PlayerController.cs`.
- In **Update** (line ~120) the controller reads the intent (`source.Read(ref intent)`, line ~127)
  and runs its **state machine** (see §6).
- The **WalkState** (line ~403) calls **Move** (line ~227):
  `desiredVelocity = intent.Move * speed`, where `speed = Db.Balance.moveSpeed (4.6) × slow effects`.
  Ash, the Bellows' soot or the blue flame's slow multiply this speed.

**Step 4 — Physics moves the body.** Still in `PlayerController.cs`, **FixedUpdate** (line ~156):
the velocity changes smoothly toward `desiredVelocity` (acceleration 60, deceleration 70 units/s²) and is
written into the **Rigidbody2D**: `rb.linearVelocity = velocity`. Unity's physics engine then moves the
player and stops it at walls, because the player has a **CircleCollider2D** at its feet and the walls
have a collider too. Outside pushes (conveyor belts, steam) are added here as well
(`Status.ExternalVelocity`).

Where the numbers live: `Assets/Data/Config/Balance.asset` (class
`Assets/Scripts/Runtime/Data/BalanceSO.cs`: `moveSpeed`, `acceleration`, `deceleration`).

> **Say it to the professor:** « Le *New Input System* donne un vecteur de direction ; un lecteur le
> limite à 4 directions ; la machine à états du joueur calcule une vitesse et le `Rigidbody2D` déplace le
> personnage dans `FixedUpdate`, les collisions sont gérées par la physique 2D. »

---

## 4. How the camera follows the player

File: `Assets/Scripts/Runtime/World/CameraRig.cs`. It sits on the Core camera; its **target** is the
player (set when Core is built, `Assets/Scripts/Editor/SceneFactory.cs` line ~126).

Every frame, in **LateUpdate** (line ~104) — *after* the player has moved:
1. **Look-ahead:** it reads the player's velocity and aims a little in front of where they walk
   (`lookAhead = 0.9` units, line ~110), so you see what is coming.
2. **Goal:** player position + a small upward offset + the look-ahead.
3. **Smoothing:** `Vector3.SmoothDamp` (line ~116) slides the camera toward the goal in about 0.12 s,
   like a spring, instead of snapping. That is why the camera "glides".
4. **Screen shake:** hits and slams send a *shake* event; the rig adds a small random offset for a moment
   (it respects the "screen shake" slider in the options).
5. **Pixel Perfect Camera:** the game is drawn at 480×270 pixels (16 pixels per unit) and scaled up
   cleanly, so pixel art never blurs.

> **Say it to the professor:** « La caméra suit le joueur dans `LateUpdate` avec `SmoothDamp` (effet
> ressort) et un léger décalage dans la direction du mouvement ; une *Pixel Perfect Camera* garde les
> pixels nets en 480×270. »

---

## 5. Wax, light and being seen (the heart of the PDF)

One resource: **wax = life = light = risk**.

- **Wax** goes down by itself: `Assets/Scripts/Runtime/Logic/WaxModel.cs`, **Tick** (line ~48):
  0.5 per second (PDF), multiplied by the region (×1.5 in the Creux Profond), the flame, and trimming.
  The component on the player is `Assets/Scripts/Runtime/Player/WaxSystem.cs`.
- **Light radius** follows the PDF formula `lerp(1.2, 6, wax/100)` →
  `Assets/Scripts/Runtime/Logic/LightMath.cs` (**BaseRadius**, line ~16). The player's
  `Assets/Scripts/Runtime/Player/LightController.cs` (**Compute**, line ~71) combines it with the flame,
  trimming (−65 %) and effects, then sets the real **Light2D**'s radius.
- **Being seen:** detection = `2 + radius × 1.5` (PDF). Each enemy's
  `Assets/Scripts/Runtime/Enemies/EnemyPerception.cs` (line ~107) compares its distance to you with that
  radius, then casts a line (**LineOfSight**, line ~118) to check that no wall is in the way.
  So: shine more → you see more, but you are spotted from farther away.

> **Say it to the professor:** « La cire baisse avec le temps ; le rayon de la lumière 2D dépend de la cire
> (formule du PDF) et les ennemis vous détectent à `2 + rayon × 1,5` s'ils ont une ligne de vue. »

---

## 6. State machines (how the player and enemies "decide")

A **state machine** means: the character is always in exactly one *state* (Idle, Walk, Attack, Dodge…),
and each state decides when to switch to another.

- The tool: `Assets/Scripts/Runtime/Core/StateMachine.cs` (**Change**, line ~50; **Tick**, line ~74).
- The player's states are at the bottom of `PlayerController.cs` (**IdleState**, **WalkState**,
  **DimState**, attack, dodge, hurt, extinguished, interact).
- Enemies: `Assets/Scripts/Runtime/Enemies/EnemyBrain.cs` has shared states — **Wander**, **Alert**,
  **Chase**, **Attack**, **Search**, **Return** — and each creature changes only what is special about it
  (`HuskBrain` hangs and drops, `LurkerBrain` freezes unless you shine, `ReflectionBrain` copies your last
  three actions, `UnlitBrain` drifts toward the strongest light).

> **Say it to the professor:** « Chaque entité a une machine à états : un seul état actif, qui décide
> lui-même quand passer à un autre (patrouille → alerte → poursuite → attaque). »

---

## 7. Events (how systems talk without knowing each other)

When the wax changes, the HUD candle, the sound and the light all need to know. Instead of the player
calling each of them, the player **raises an event** and whoever cares **listens**.

- The event channel class: `Assets/Scripts/Runtime/Core/EventChannel.cs` (**Raise** line ~45,
  **Register** line ~33).
- All events of the game: `Assets/Scripts/Runtime/Core/GameEvents.cs` (`Events.WaxChanged`,
  `Events.PlayerHit`, `Events.BossPhaseChanged`, …).
- Example listener: the HUD candle, `Assets/Scripts/Runtime/UI/HudCandle.cs` line ~30
  (`Events.WaxChanged.Register(OnWax)`).

> **Say it to the professor:** « Les systèmes communiquent par des canaux d'événements
> (*ScriptableObjects*) : celui qui émet ne connaît pas ceux qui écoutent. »

---

## 8. Combat (hits, flames and matter)

- An attack switches on a damage zone: `Assets/Scripts/Runtime/Combat/HitQuery.cs` (**Arc**) or a
  `Hitbox`. Everything that can be hurt has a **Hurtbox** pointing to a **Damageable**.
- The PDF's rules (Scorifiés hurt only by white or green, glass only by crimson, crimson stronger at low
  wax) are pure functions in `Assets/Scripts/Runtime/Logic/CombatMath.cs` (**CanHurt**) — tested in
  `Assets/Tests/EditMode/CombatAndScoreTests.cs`.
- Each flame's numbers are data: `Assets/Data/Flames/*.asset`.

---

## 9. Enemies finding their way

- `Assets/Scripts/Runtime/World/NavGrid.cs`: the level is a grid; **A\*** (FindPath) finds a path around
  walls. Props with a solid body block their cell (BlockSolidProps).
- `Assets/Scripts/Runtime/Enemies/EnemyMotor.cs` (**MoveTo**, line ~57) follows that path with the
  enemy's Rigidbody2D.

---

## 10. How levels are made

Levels are **text maps** (like ASCII art) generated by small Python scripts, then turned into Unity
tilemaps automatically.

1. A script draws the map: `Tools/levels/r2_wickwood.py` (rooms, corridors, where each enemy goes).
   Run `python Tools/levels/r2_wickwood.py` → it writes `Assets/Resources/Levels/R2_Wickwood.txt` and a
   preview image in `ArtSource/Previews/level_r2.png`.
2. In Unity, `WICKFALL > Setup > Run Project Setup` reads the text: walls and floors become **Tilemap** tiles
   (`Assets/Scripts/Editor/LevelBuilder.cs`), each symbol becomes a prefab
   (`Assets/Scripts/Editor/RegionFactory.cs`).

---

## 11. Light and shadows in URP 2D

- The project uses **URP** with the **2D Renderer**: sprites are lit by **Light2D** components.
- Each region has one **Global Light** (the ambient light, very low; zero in the Creux Profond) and
  point lights: the player's flame, braziers, furnaces.
- Walls cast shadows (**ShadowCaster2D** on the wall tilemap).
- "Negative light" (the Foyer, the canopy shade) is a light that *subtracts* brightness.
- In the Creux Profond, the shader `Assets/Shaders/LightReveal.shader` draws the floor only where light
  reaches.

---

## 12. Art and sound (made by code)

- Pixel art is drawn by Python scripts in `Tools/art/` with the PDF's palette, e.g. the player in
  `Tools/art/wickbearer.py`. Run `python Tools/art/make_art.py` → sprites in `Assets/Art/Generated` and
  previews in `ArtSource/Previews`. Unity cuts each sheet into frames automatically
  (`Assets/Scripts/Editor/ArtImport.cs`).
- Animations play with `Assets/Scripts/Runtime/Core/SpriteAnimator.cs` (it swaps sprites at a set speed).
- Every sound and music loop is synthesized: `Tools/audio/make_audio.py` → `Assets/Audio`.

---

## 13. Saving

`Assets/Scripts/Runtime/Meta/SaveData.cs` (what is saved) → `SaveCodec.cs` (encrypted, PDF §4) →
`SaveStore.cs` (the file, with a backup copy). Settings are a separate plain file.

---

## 14. Tests and bots (how we know it works)

- **EditMode tests** check the rules and the PDF numbers without playing:
  `Assets/Tests/EditMode/PdfValuesTests.cs` checks every number of the PDF in the data.
- **PlayMode tests** play the real game. A **bot** plays each region from start to bell, and three bots
  reach endings A, B and C: `Assets/Scripts/Runtime/DevTools/RegionBot.cs` (it presses the same inputs as
  a person). Results are logged in `Logs/bot_runs.txt`.
- Run them (Unity closed): `powershell -File Tools/unity.ps1 -Tests EditMode` and
  `powershell -File Tools/unity.ps1 -Tests PlayMode -Filter Wickfall.Tests.PlayerTests`.

---

## 15. Tutorial: change things yourself (10 small exercises)

Do them in the Unity editor; press Play to see the result. Undo with Ctrl+Z or `git checkout` later.

1. **Walk faster.** Select `Assets/Data/Config/Balance.asset` → *Inspector* → `Move Speed` 4.6 → 7.
   Play: the Wickbearer runs. (Code that reads it: `PlayerController.cs` line ~229.)
2. **Smoother or stiffer camera.** Open `Assets/Scenes/Core.unity`, select *Main Camera* → component
   *Camera Rig* → `Smooth Time` 0.12 → 0.5 (slow, floaty) or 0.02 (stiff). `Look Ahead` 0.9 → 3 to
   see further in front.
3. **See the light formula.** Play a region, press **`** to open the developer overlay: it shows wax,
   radius and detection radius live. Hold Left Shift to trim: the radius drops by 65 %.
4. **Make wax last longer.** `Assets/Data/Config/WaxConfig.asset` → `Passive Loss Per Second` 0.5 → 0.2.
   (This is a PDF number: change it only to experiment, and put it back — a test checks it.)
5. **Change a flame's colour.** `Assets/Data/Flames/Flame_2_Blue.asset` → `Light Color`.
6. **Watch a state machine.** In Play, press **`**: the developer overlay lists every creature near you
   with its current state (Wander → Alert → Chase…) and draws your light and detection circles.
7. **Edit a level.** In `Tools/levels/r1_tallow_fields.py` move the brazier (`L.put(...)`), run the
   script, then `WICKFALL > Setup > Run Project Setup` → the scene is rebuilt with your change.
8. **Translate a text.** `Assets/Resources/Localization/Strings.txt`: one line per text, columns
   `key → French → English` separated by tabs.
9. **Listen to a sound.** `Assets/Audio/SFX/BellRing.wav` — click it in the Project window and press ▶.
10. **Run a bot.** `powershell -File Tools/unity.ps1 -Tests PlayMode -Filter Wickfall.Tests.Region1Tests`
    and read `Logs/bot_runs.txt`: kills, deaths, score and rank of the bot's run.

---

## 16. Where everything is (map)

| What | Where |
|---|---|
| Design source (PDF + transcript) | `Docs/Source/` |
| Decisions where the PDF is silent | `Docs/DESIGN_DECISIONS.md` |
| Bugs found and fixed | `Docs/BUG_LOG.md` |
| Game code (runs in the game) | `Assets/Scripts/Runtime/` — `Core` (events, input, state machine), `Player`, `Enemies`, `Combat`, `World` (levels, hazards, camera), `Meta` (save, scenes), `UI`, `Audio`, `Lighting`, `Logic` (pure rules) |
| Editor code (builds the content) | `Assets/Scripts/Editor/` |
| Tuning data | `Assets/Data/` and `Assets/Resources/GameDatabase.asset` |
| Scenes | `Assets/Scenes/` (Boot, Core, MainMenu, Ending, `Regions/R1…R6`) |
| Prefabs | `Assets/Prefabs/` |
| Art scripts and output | `Tools/art/` → `Assets/Art/Generated/` |
| Level scripts and output | `Tools/levels/` → `Assets/Resources/Levels/` |
| Sound script and output | `Tools/audio/` → `Assets/Audio/` |
| Shaders | `Assets/Shaders/` |
| Tests | `Assets/Tests/EditMode/`, `Assets/Tests/PlayMode/` |
| Screenshots | `Docs/Screenshots/` |

## 17. Small glossary

- **Rigidbody2D** — makes an object obey 2D physics (velocity, collisions).
- **Collider2D** — the invisible shape used for collisions or triggers.
- **Trigger** — a collider that detects overlaps but does not block (an interaction zone).
- **Tilemap** — a grid of tiles (walls, floors) painted from a palette of tiles.
- **Light2D** — a 2D light (point, global, or freeform shape) in URP.
- **ScriptableObject** — a data asset; our tuning lives in them.
- **Prefab** — a reusable GameObject template.
- **Coroutine** — a method that can wait (`yield return`) over several frames.
- **Pool** — objects reused instead of created/destroyed, so the game never stutters (`Pooling.cs`).
- **A\*** — a path-finding algorithm (shortest path around walls).
