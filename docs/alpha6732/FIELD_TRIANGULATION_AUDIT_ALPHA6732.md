# Team Up Alpha 6.7.32 - Field Triangulation Audit

## CI result

**PASS**

- Run: `34484485052`
- Job: `102895196797`
- CI-verified source: `bece1c7cfc5dd83bf4d79dc815b879d869dc4868`
- Build: `0 Warning(s) / 0 Error(s)`
- Binary acceptance: PASS
- Inner ZIP SHA256: `325ad7586c682b12aea83f31dd2ba915cfb29b58613b33949712ebeac0be8982`

## Verified behavior

- Field Triangulation persistent route: `0..4`.
- Requires Old Mine Connection completion.
- Requires at least 3 people in the same location.
- Requires at least 1 active Team Up NPC ally.
- Same-location online Farmers contribute to field headcount.
- Active NPC allies are deduplicated by character name.
- Bearing A persists its MineShaft `NameOrUniqueName`.
- Bearing B must come from a different MineShaft location.
- Completion narrows the likely sealed-workings corridor.
- Story slot 4 is not unlocked.
- Reaction windows 0..9 carry forward unchanged with 136 reaction lines per language.
- George / Last Blaster secrecy remains intact.
- Evelyn postgame secrecy remains intact.
- Story/roster/capture/provider safety through Alpha 6.7.31 carries forward.

## Live checks still required

- Same-location Farmer counting in real multiplayer.
- NPC physical-presence timing immediately after warp.
- Actual MineShaft `NameOrUniqueName` behavior across floors in the user's mod stack.
- Dialogue and HUD pacing across the complete route.
