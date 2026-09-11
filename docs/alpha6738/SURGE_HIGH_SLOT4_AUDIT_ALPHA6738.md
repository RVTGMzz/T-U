# Team Up Alpha 6.7.38 - Surge HIGH Escalation / Story Slot 4 Audit

## Implemented
- Added persistent Surge HIGH chapter service gated behind completed Controlled Breach / First Entry.
- Reuses the exact recorded breach MineShaft face from Alpha 6.7.36.
- Requires a valid three-person field team with at least one active Team Up NPC ally.
- Requires a continuous 180-tick stable reading at the breach face.
- Persists the confirmed HIGH state before the team returns to Marlon.
- Unlocks story NPC slot 4 only on the Guild report after HIGH is confirmed.
- Five-person total formation cap remains authoritative in multiplayer.

## Safety / scope
- George remains Rank D / Non-Combatant / unrecruitable and unrevealed.
- Evelyn postgame secret remains untouched.
- No final boss, custom lower-workings dungeon map, mutation rewrite, capture rewrite, or combat ownership change.
- Reaction catalog remains windows 0..22 with 318 lines per language.

## Live verification still required
- Validate real 180-tick hold and reset behavior.
- Validate HIGH flag persists after save/reload.
- Validate stage 3 still shows roster 3/4 and stage 4 report changes it to 4/4.
- Validate multiplayer effective NPC capacity still respects five total people.
