# WICKFALL — standing rules for Claude sessions

A 2D top-down action-RPG in pixel art, Unity 6000.2.7f2, URP 2D, Windows 64-bit (Mono). One resource:
**wax = life = light = risk**. Golden rule: *on ne peut pas être fort et caché en même temps*.

## Source of truth
1. `Wickfall_Idee_Complete.pdf` (copy + transcript in `Docs/Source/`) is the ONLY design reference.
   Order of authority: the PDF, then the user's instructions, then Claude's proposals.
2. Never change a PDF number, name, region order, flame, enemy, hazard, ending or art size. If one
   plays badly: implement it, measure, propose with evidence, wait for the user. `PdfValuesTests`
   checks every PDF number in the shipped assets.
3. Gaps are filled with the smallest proposal and logged in `Docs/DESIGN_DECISIONS.md` (G-IDs).
4. Every PDF statement is traced in `Docs/TRACEABILITY.md` (where, proof, status).
5. Public sources are for HOW to build, never WHAT the game is.
6. The full build brief is `WICKFALL_START_PROMPT.md` (milestones M0–M9, phases, art cards).

## The user
First-year student; the professor reviews in 8 stages and works in French. Short, plain English
replies (status, done, needs, next). Code and docs in English. What to say to the teacher: English, short (user, 2026-10-18); `Docs/HOW_IT_WORKS.md` stays one short page.
The user said (2026-10-17): don't ask, take the recommended option, they review afterwards.
Never spend money/credits, never push, never delete their work without asking.

## How to build and test (the Unity editor must be closed; one Unity process at a time)
```
powershell -File Tools/unity.ps1 -Method Wickfall.EditorTools.WickfallSetup.Run   # settings, data, art, prefabs, scenes
powershell -File Tools/unity.ps1 -Tests EditMode
powershell -File Tools/unity.ps1 -Tests PlayMode [-Filter Wickfall.Tests.PlayerTests]
powershell -File Tools/unity.ps1 -Method Wickfall.EditorTools.WickfallCapture.CaptureLightLab
powershell -File Tools/unity.ps1 -Method Wickfall.EditorTools.WickfallBuild.BuildWindows
python Tools/art/make_art.py [player|ui|vfx|tiles|props|...]   # regenerates sprites + previews
python Tools/font/make_font.py                                 # the pixel font
```
- Run foreground with the max timeout. Exit code 1073807364 = killed run (not a compile error): rerun.
- `-runTests` never with `-quit`; split long PlayMode suites with `-testFilter` (< 10 min each).
- Tests mute audio (`AudioListener.volume = 0`).
- After a killed build: `git status`, restore damaged `ProjectSettings/*.asset`.
- Look at every capture (`Docs/Screenshots`, `ArtSource/Previews`) before calling a visual done.

## Architecture (PDF §4)
- All tuning in ScriptableObjects (`Assets/Data`, `Resources/GameDatabase`, access via `Db`).
- Systems talk only through event channels (`Events.*`, `Resources/GameEvents`).
- Every entity has a `StateMachine<T>` (allocation-free).
- Pure logic in plain C# (`Assets/Scripts/Runtime/Logic`, `Meta/SaveCodec`) proven by EditMode tests.
- Persistent `Core` scene (player, camera, systems, UI, audio); content scenes load additively with a
  `SceneProfile`; `CoreBootstrap` lets any scene be played directly.
- Everything is built by idempotent editor code (`WICKFALL > Setup`); levels are ASCII maps in
  `Assets/Levels`. Generated scenes carry `GeneratedSceneMarker`; never overwrite a scene without it.
- Zero allocation in gameplay frames: pools (`Pools`), NonAlloc physics, no LINQ/closures per frame.

## Hard-won facts (keep)
- Unity 6.2: `Physics2D.queriesHitTriggers` must stay **true** or triggers vanish from every query.
- `Light2D` builds its mesh in `LateUpdate`: edit-mode captures call `WickfallCapture.PrepareLights`.
- Batch-mode `AssetDatabase.ImportPackage` never runs before `-quit`: use `Tools/import_unitypackage.py`.
- `Start-Process -Wait` hangs on Unity's helper processes: `Tools/unity.ps1` waits on Unity only.
- Unity can't serialize 64-bit enums: `Feature` flags are stored as `long`.
- Disable the Pixel Perfect Camera before rendering into a RenderTexture.
- `GC.GetAllocatedBytesForCurrentThread` always reads 0 on Unity's Mono: measure with the profiler counter
  "GC Allocated In Frame" (`ProfilerRecorder`), against a floor taken with the game's scripts off.
- Physics2D "queries start in colliders" is off: a cast ignores what it starts inside (projectiles also
  check their first-step overlap).
- A MonoBehaviour must live in a file of its own name, or prefabs save it as a missing script.
- Level inscriptions carry their number (`Inscription3`); map scan order is not the intended order.

## Presentation commands
- `SHOW PHASE N` / `SHOW EVERYTHING` / `PREPARE FOR NEXT SESSION` — see `PRESENTATION_PHASES.md`.
  Phases only switch features at runtime; nothing is ever removed.

## Docs
`README.md`, `PRESENTATION_PHASES.md`, `ASSET_LICENSES.md`, `Docs/` (GDD, TRACEABILITY,
DESIGN_DECISIONS, ART_BIBLE, ART_PROMPTS, LEVEL_DESIGN, BALANCE, TEST_PLAN, BUG_LOG, PERFORMANCE,
PROJECT_MAP, Diagrams/, Presentation/).

## Git
Commit at verified points with messages that say what was verified. Tag only verified milestones.
Never fake history. Push only when asked.
