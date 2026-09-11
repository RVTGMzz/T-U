# Team Up Alpha 6.7.44 - Dedicated Lower Workings Map / Interior Survey Audit

## Implemented
- Added a real save-backed Stardew 1.6 Lower Workings GameLocation through Data/Locations.
- Added assets/LowerWorkings.tmx using the vanilla Mines/mine.png tilesheet.
- Bound ingress to the recorded controlled-breach MineShaft and a persistent tile anchor.
- New saves inherit the anchor from the 6.7.42 threshold crossing; legacy 6.7.43 saves calibrate once at the real breach.
- Added a secured return path from the Lower Workings entry; early emergency withdrawal preserves survey progress.
- Added three 120-tick full-formation survey zones: directed cribbing, newer Mutation-linked residue over old blast scoring, and a deeper sealed-pressure edge.
- Added bundled reaction windows 36 through 41, 14 NPCs each, bringing the exact reaction catalog to 584 lines per language.

## Story boundary
- Evidence now links present Mutation activity to the older sealed system without identifying the source/entity behind it.
- Historical worker identity remains unknown. George stays observed Rank D / Non-Combatant / unrecruitable.
- Evelyn postgame secret remains untouched. No Sector 17 identifier is introduced.
- No boss or containment encounter is introduced; that escalation remains 6.7.45 scope.

## Safety / compatibility
- Host remains authoritative for story progression; farmhands may use the shared location route without writing host flags.
- Five-PEOPLE total cap, story roster 4/4 ceiling, Entry Protocol READY, and SURGE HIGH prerequisites remain intact.
- First guaranteed Mutation remains the 10th eligible natural normal-monster defeat.
- Pelipper capture ceasefire / protected-target safety remains intact.

## Live verification still required
- Validate the TMX renders correctly in-game with no void tiles or collision traps.
- Validate ingress/egress on a 6.7.43 legacy save and on a fresh 6.7.44 progression.
- Validate host/farmhand entry, formation counting, save/reload, and emergency withdrawal.
- Validate one-shot reactions 36..41 and stale-window skipping.
