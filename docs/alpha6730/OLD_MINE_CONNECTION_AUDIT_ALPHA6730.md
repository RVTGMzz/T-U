# Team Up Alpha 6.7.30 - Old Mine Connection Audit

## Implemented

- Added a persistent 0..3 Chapter IV bridge after the Alpha 6.7.29 Marlon investigation.
- Guild archive lead points to a municipal safety filing for an old coal mine roughly thirty years ago.
- `ManorHouse` record confirms sealed lower workings, a redacted worker line, and the hooked inspection mark matching current Mutant evidence.
- Returning to the Guild confirms the old-coal-mine connection and unlocks story NPC ally slot 3/4.
- The old miner remains unnamed. George stays observed Rank D / Non-Combatant and unrecruitable.
- No new reaction windows were added in this checkpoint; Alpha 6.7.29 windows 0..6 carry forward unchanged.

## CI acceptance

- Persistent OldMineConnection stage key: PASS
- Marlon Alpha 6.7.29 completion prerequisite: PASS
- Active NPC ally gate at all route steps: PASS
- AdventureGuild -> ManorHouse -> AdventureGuild route: PASS
- Slot 3 additive unlock source `old-mine-connection-confirmed`: PASS
- EN/VI localization key parity: PASS
- George / Last Blaster spoiler guard: PASS
- George pre-reveal recruitment lock: PASS
- Natural Mutant observation from 6.7.29: PASS
- Ten-kill Surge opening trigger: PASS
- Story roster ceiling 4 NPCs: PASS
- Five-person total formation cap carry-forward: PASS
- Pelipper capture ceasefire carry-forward: PASS
- Legacy provider fake-hide writer absent: PASS
- Release compiler under `-warnaserror`: PASS, 0 warnings / 0 errors
- Binary token acceptance: PASS
- Artifact packaging/upload: PASS

## Safety

- This chapter is record/location story logic only. It does not spawn, replace, damage, clone, hide, retarget, or take ownership of monsters or provider-controlled actors.
- Pelipper capture ceasefire and source ownership rules are unchanged.
- Story slot 3 is additive through `UnlockTo` and cannot exceed the 4-NPC story ceiling or the 5-person live formation ceiling.
- George's reveal flag remains untouched.

## Live verification still required

- Guild-to-ManorHouse-to-Guild dialogue pacing.
- Party-follow presence after each warp.
- Real `ManorHouse` gate in the user's full mod stack.
- Multiplayer host/active-ally physical presence behavior.
- Third-slot usability in a real save while respecting online Farmer count.
