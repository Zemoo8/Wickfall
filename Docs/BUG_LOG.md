# Bug log

Real bugs found while building and testing, with cause, fix and the proof that it is fixed.

| # | Date | Found by | Symptom | Cause | Fix | Proof | Status |
|---|---|---|---|---|---|---|---|
| 1 | 2026-10-17 | Headless run | `Tools/unity.ps1` never returned although Unity had exited. | `Start-Process -Wait` also waits for helper processes Unity leaves running (licensing client). | Wait on the Unity process only (`WaitForExit`). | Every later headless run returns. | Fixed |
| 2 | 2026-10-17 | Setup | TMP text could not render (no TMP shaders/settings). | `AssetDatabase.ImportPackage` is deferred in batch mode and never runs before `-quit`. | `Tools/import_unitypackage.py` extracts TMP Essential Resources with their GUIDs. | Font asset built; texts render. | Fixed |
| 3 | 2026-10-17 | Capture | Edit-mode screenshots showed no point lights (only the global light). | `Light2D` builds its mesh and bounds in `LateUpdate`, which never runs for edit-mode objects in batch mode. | `WickfallCapture.PrepareLights` builds them before rendering. | `Docs/Screenshots/lightlab_start.png` shows the player's light. | Fixed (capture tool only; the game was never affected) |
| 4 | 2026-10-17 | PlayMode tests | Interactors, hazard hitboxes and hurtboxes found nothing (brazier unusable, geysers harmless). | Unity 6.2: with `Physics2D.queriesHitTriggers = false`, triggers are excluded even from queries whose `ContactFilter2D.useTriggers` is true. Proven by `PhysicsDiagnostics`. | Project setting on; every query keeps explicit layer masks. | `PhysicsDiagnostics.TriggerQueries_SeeInteractables`, `PlayerTests.Brazier_*`, `PlayerTests.Geyser_*` pass. | Fixed |
| 5 | 2026-10-17 | PlayMode tests | In the Light Lab the flame wheel only had Amber. | Script order: when Core loads, `SceneProfile.Start` can run before `GameSession.Start`, which re-applied the save's loadout over the lab's. | `ApplyProgress` itself respects sandbox mode, whatever the order. | `PlayerTests.FlameSwitch_Costs8Wax_AndFlares` passes. | Fixed |
| 6 | 2026-10-17 | PlayMode tests | Error on loading Core: "Unsupported enum type Feature". | Unity cannot serialize a 64-bit enum field (`PresentationGate.feature`). | Stored as a raw `long` with a typed property. | No error in later runs. | Fixed |
