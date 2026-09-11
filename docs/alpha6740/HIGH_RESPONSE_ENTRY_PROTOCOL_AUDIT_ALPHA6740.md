# Team Up Alpha 6.7.40 - HIGH Response Preparation / Lower Workings Entry Protocol Audit

## Implemented
- Added a persistent four-stage lower-workings entry-preparation protocol behind completed SURGE HIGH and slot 4 authorization.
- Reuses the exact recorded breach MineShaft from the controlled-breach chapter.
- Requires the full operational formation calculated from online Farmers, unlocked NPC slots, configured party cap, and the hard five-person ceiling.
- Validates staging, withdrawal order, rear anchor, no-pursuit threshold, and abort criteria through a continuous 240-tick hold.
- Persists `LowerWorkingsEntryProtocolReady` only after the readiness drill is reported to Marlon.

## Scope locks
- No boss, monster spawn, custom lower-workings map, combat rewrite, mutation rewrite, capture rewrite, or roster unlock.
- George remains Rank D / Non-Combatant / unrecruitable and unrevealed.
- Evelyn postgame secret remains untouched.
- Reaction catalog remains windows 0..26 with 374 lines per language.

## Live verification still required
- Validate solo full formation and co-op adaptive formation counts.
- Validate the 240-tick hold resets when formation breaks, player warps, or a menu/dialogue interrupts.
- Validate READY persists after save/reload.
