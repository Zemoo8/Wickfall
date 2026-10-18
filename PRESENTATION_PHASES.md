# Presentation phases — what to show at each review

In Unity: **`WICKFALL > Presentation > Phase N`** switches on only that stage's features. Nothing is deleted. Use **Show Everything** to get the whole game back.

| Phase | Show | Ready? |
|---|---|---|
| 1 Conception | The idea: wax = life = light = risk; 6 regions; 3 endings (`Docs/Source/` PDF) | Idea yes; slide deck not made yet |
| 2 Diagrams | How the code is organised (`Docs/HOW_IT_WORKS.md`) | Text yes; diagram images not made yet |
| 3 Design | The art: `ArtSource/Previews/` and `Docs/Screenshots/` | Yes; art deck not made yet |
| 4 Development | Regions 1–2 playable: movement, wax, light, combat, enemies | Yes |
| 5 2D Animation | Regions 1–4: animations, VFX, camera shake, post-processing | Yes |
| 6 Testing | All 6 regions and the Roi Creux; the tests and the bots (`Logs/bot_runs.txt`) | Yes |
| 7 Final Product | Everything polished, the menus, the endings | Mostly (polish still in progress) |
| 8 Release (PC) | The Windows `.exe` | A working build exists; the final package is not done |

**Videos** in `Builds/Videos/` (a bot plays at 1.25× speed; a caption on top explains what is on screen, one idea at a time):

| Stage | Clips |
|---|---|
| 4 Development | `Stage4_R1_TallowFields.mp4` (1:51), `Stage4_R2_Wickwood.mp4` (1:35) |
| 5 2D Animation | `Stage5_R3_SootFoundries.mp4` (1:24), `Stage5_R4_GlassCathedral.mp4` (1:28) |
| 6 Testing | `Stage6_R5_DeepHollow.mp4` (1:13), `Stage6_R6_Boss_and_EndingA.mp4` (1:08) |
| 7–8 | show the stage 6 clips and run the `.exe` |

The bot plays like a careful player: it sneaks past creatures with the wick trimmed, fights only what catches it (or guards a puzzle), and dodges. All six clips: regions cleared, 0 deaths.

Make them again (Unity closed, one at a time, about 4–8 minutes each):
`powershell -File Tools/unity.ps1 -Tests PlayMode -Filter Wickfall.Tests.ZzRecordR1` (R1 … R6), then
`sh Tools/shrink_video.sh Builds/Videos/<clip>.mp4` if a clip is over 29 MB.

**What to say at each phase:** the one-line answers in `Docs/HOW_IT_WORKS.md`.
