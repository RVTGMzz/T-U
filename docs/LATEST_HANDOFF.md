# Team Up - Canonical Latest Handoff

Current detailed handoff: `docs/ALPHA_6_7_44_14_PELIPPER_X2_LOOT_X3_HANDOFF.md`.

## Current checkpoint
- Version: `0.2.0-alpha.6.7.44.14`
- Branch: `v0.2-alpha6-7-44-14-pelipper-x2-loot-x3`
- CI SHA: `96811b01800b2eeaf1a7a3247899818d74a791b1`
- Run: `34862316701`
- Job: `104037413329`
- Artifact ID: `10355521041`
- Artifact wrapper SHA256: `9c0985f806bce5fc87ab98104c44b785064650a01530b73c92e09827332c9c96`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.14_PELIPPER_X2_LOOT_X3_TEST.zip`
- Inner ZIP SHA256: `8f81c20f34305dc3ceaa5f2823b7fd046ae04a05004a51170b25a7d06bf08b8b`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

## Live truth
6.7.44.12/13 live-confirmed the Pelipper Mutation core, authoritative proxy-modData HP binding, natural-roll path, and visible PokemonNpc scaling. Live Fidough showed the x3 source scale is functional but visually too large. Pelipper minions still failed 0/4 even with the relaxed fallback.

## 6.7.44.14
- Visible Pelipper Mutation scale is hard-capped at x2; new configs also default to x2.
- Pelipper Mutants temporarily disable their unreliable 2-4 minion wave. Generic/non-Pelipper Mutation minion behavior remains unchanged.
- Pelipper Mutants receive temporary x3 loot compensation.
- The current x3 implementation repeats Stardew's native `GameLocation.monsterDrop` pass two extra times for the same marked Pelipper Mutant, protected against recursive Harmony re-entry.
- This reward is not accepted until live final-death telemetry proves `dropCalls>=1`, `extraDropPasses=2`, `errors=0` and visible additional drops. `dropCalls=0` means Pelipper bypasses Stardew's native drop path and requires a Pelipper-specific reward hook.

## Next live test
Run `teamup_mutation force` on a normal non-Shiny Pelipper Pokemon. Confirm x2 visual size and no 2-4 Pelipper minions. Fight through all three HP phases, then after final death run `teamup_mutation status`. Verify `phaseGuards` advanced on the first two bars and `Pelipper Mutant reward` reports a successful x3 native-drop path.

## Carry-forward locks
- Current Shiny behavior stays frozen as accepted unless a concrete regression appears.
- Shiny remains Mutation-excluded.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Encounter discovery remains 20Hz and pair cache stays enabled.
- Never use hidden Green Slime 1,000,000 sentinel HP as Pokemon HP.
- Pelipper remains controller/render/ownership authority.
- Lower Workings remains unchanged and still gates 6.7.45.
- Mutants are NOT yet proven non-catchable through Pelipper's real ball-capture path.
