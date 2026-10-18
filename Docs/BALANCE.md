# Balance — what the bots measured

PDF numbers never change (a test checks them). Everything else is a starting value to tune.

| Region | Bot time | Kills | Deaths | Wax left | Score | Rank |
|---|---|---|---|---|---|---|
| R1 Champs de Suif | ~2 min | 10–11 | 0 | 59–88 % | 1700–2100 | Brasier |
| R2 Bois-Mèche | ~2 min 15 | 16–18 | 0 | 96–98 % | ~2200 | Brasier |
| R3 Fonderies de Suie | ~2 min 10 | 13 | 0 | 36–72 % | 1600–2000 | Flamme–Brasier |
| R4 Cathédrale de Verre | ~2 min 30 | 7 | 0–1 | 74–88 % | ~2000 | Brasier |
| R5 Creux Profond | ~2 min 45 | 11 | 3 | ~82 % | ~1760 | Brasier |
| R6 Roi Creux | ~1 min 10 | – | 0 | – | – | – |

**What this says**
- The bot knows the way, so a person will take 5–10× longer per region.
- R2 is easy on wax: there are many drops and pools. R5 is the hardest: the bot dies 3 times.
- Rank thresholds (700 / 1200 / 1700) put the bot at the top rank. A first-time player should land on Braise or Flamme.

**Changed after testing**
- Roi Creux: 260 → 200 health per phase, 2.0 s between attacks, contact damage only on real contact. Before that the bot never won.
- Brightness option added (default 60 %), because players found the regions too dark.
- Data files: `Assets/Data/Config/` (`Balance`, `WaxConfig`, `LightConfig`, `BossConfig`, `ScoreConfig`).
