# Test plan

Run with Unity closed: `powershell -File Tools/unity.ps1 -Tests EditMode` and `-Tests PlayMode -Filter <class>`.
Split PlayMode into batches (each run must stay under 10 minutes).

## EditMode (77 tests, rules without playing)
| File | Checks |
|---|---|
| `PdfValuesTests` | Every number of the PDF in the shipped data |
| `WaxAndLightTests` | Wax loss, the light formula, trimming, detection |
| `CombatAndScoreTests` | Flame rules (Slagborn, glass, crimson curve), kill rewards, score and ranks |
| `SaveAndRulesTests` | Encrypted save round trip, tampering, backup; endings rules; flames guaranteed per region |
| `NavGridTests` | Path finding |
| `LevelFileTests` | Each level has its essentials; inscriptions numbered and translated |
| `LocalizationTests` | Every text in French and English; French is the default |
| `AudioTests` | Every sound and music has a clip; crackle and heartbeat |
| `ProjectHealthTests` | No missing scripts in prefabs or scenes |

## PlayMode (the real game)
| File | Checks |
|---|---|
| `PlayerTests` | Movement, wax, trim, light, flame switch, dodge, combat, soot, braziers, buffered input |
| `Region1Tests` … `Region6Tests` | Each region's creatures and hazards, plus **a bot that clears it** |
| `Region6Tests` | Pacts, the King's 3 phases, **bots reaching endings A, B and C** |
| `MenuTests` | Region select, endings gallery, credits, how to play, options |
| `AllocationTests` | No memory allocated per frame while playing |
| `PhysicsDiagnostics` | Trigger queries still work |

Tests never touch the player's real save: they use their own temporary folder.

Bot results go to `Logs/bot_runs.txt`. Gameplay videos come from `ZzRecordR1` … `ZzRecordR6` (on demand): the same bot in showcase mode (no optional fights, sneaks, dodges, narrates).
