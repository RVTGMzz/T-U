# Team Up - Canonical Latest Handoff

Current detailed handoff: `docs/ALPHA_6_7_44_13_PELIPPER_VISIBLE_SCALE_MINION_SPAWN_HANDOFF.md`.

## Current checkpoint
- Version: `0.2.0-alpha.6.7.44.13`
- Branch: `v0.2-alpha6-7-44-13-pelipper-visible-scale-minion-spawn`
- CI SHA: `c989c6407ac70ad799ffc5b06cbaac90c9717272`
- Run: `34778989370`
- Job: `103782389584`
- Artifact ID: `10324089476`
- Artifact wrapper SHA256: `ac0369fb12e2d249d918eeae10f5252a9d1705157674b551b955b354d8e49562`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.13_PELIPPER_VISIBLE_SCALE_MINION_SPAWN_TEST.zip`
- Inner ZIP SHA256: `9e3c2fb4975c2944f915d38fb39e5002311178b31dfc3e66b40b30779cdfbd64`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

## Live truth
6.7.44.12 live-confirmed the Pelipper Mutation core and authoritative proxy-modData HP binding. Natural lethal events now reach Mutation rolls.

The remaining live failures were visual/source scale and minion placement: Tauros stayed normal-sized despite `scaleApplied=True`, proving generic scale affected only the hidden proxy; minion waves spawned 0/N with every candidate `safeRejected` on Farm/custom map.

## 6.7.44.13
- Mirrors Mutation scale onto the paired visible `PelipperTown.PokemonNpc`, preferring `_visualScaleMultiplier` and restoring the original value when the wild Mutant encounter ends or changes role.
- Reapplies visible scale at the existing 20Hz runtime pulse if Pelipper refreshes presentation state.
- Keeps strict minion safe-spawn first, then adds a conservative fallback that drops only the over-broad `CollisionMask.All` gate while still requiring passable/on-map tiles, no placed object/terrain feature, and safe distance from Farmer/all characters.
- Adds visible-scale and minion-fallback telemetry to `teamup_mutation status`.

## Next live test
Run `teamup_mutation force` on a normal non-Shiny Pelipper wild Pokemon. Confirm the actual Pokemon sprite becomes roughly x3 larger, verify minions can spawn, then run `teamup_mutation status` and return the new visible Mutation/minion fallback lines plus source Mutation/HP binding and final MutationTelemetry. Continue the HPx3 phase test by depleting one real HP bar and checking `phaseGuards` + HP-binding writes.

## Carry-forward locks
- Current Shiny behavior stays frozen as accepted unless a concrete regression appears.
- Shiny remains Mutation-excluded.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Encounter discovery remains 20Hz and pair cache stays enabled.
- Never use hidden Green Slime 1,000,000 sentinel HP as Pokemon HP.
- Pelipper remains controller/render/ownership authority.
- Lower Workings remains unchanged and still gates 6.7.45.
- Mutants are NOT yet proven non-catchable through Pelipper's real ball-capture path.
