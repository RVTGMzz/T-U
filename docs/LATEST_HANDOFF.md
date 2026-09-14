# Team Up - Canonical Latest Handoff

Current detailed handoff: `docs/ALPHA_6_7_44_15_MUTANT_MINIONS_GLOBAL_LOOT_X3_HANDOFF.md`.

## Current checkpoint
- Version: `0.2.0-alpha.6.7.44.15`
- Branch: `v0.2-alpha6-7-44-15-mutant-minions-global-loot-x3`
- CI SHA: `61fe5b59811ea6cfe3cd95da3f3cf201665f1b9b`
- Run: `34863995611`
- Job: `104043116358`
- Artifact ID: `10356535462`
- Artifact wrapper SHA256: `00f38c9f0cd199568ae64508e0fef18686ea2fc284c33160566ed3f1997e768b`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.15_MUTANT_MINIONS_GLOBAL_LOOT_X3_TEST.zip`
- Inner ZIP SHA256: `a44958d0bd1053d83cea9fd0b6e07ac0db9694870d20c43f50db886013c805ad`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

## Live truth
Pelipper Mutation core, real HP binding and natural Mutation rolls are working. Visible Pelipper Mutant scaling works, but x3 was too large; x2 is the current visual lock.

The 2-4 minion wave is part of the Mutation design. A previous 6.7.44.14 interpretation temporarily suppressed Pelipper minions after live `spawned=0/N`; that was incorrect and is reverted in 6.7.44.15.

## 6.7.44.15
- Restores 2-4 minions for every Mutant, including Pelipper.
- Wider minion fallback uses the visible Pokemon source as Pelipper anchor, searches radius 2 through 8, permits harmless terrain such as grass, and retains map/passability/object/occupancy gates.
- Globalizes x3 loot to every main Mutant, not only Pelipper.
- x3 loot repeats the native Stardew `monsterDrop` pass two extra times with recursion protection.
- Mutation minions stay loot-suppressed by default.
- Pelipper visible scale remains capped at x2.

CI gates passed:
- PELIPPER MODDATA HP + PAIR CACHE CARRY-FORWARD
- PELIPPER VISIBLE MUTATION X2 CAP + RESTORE
- MUTATION 2-4 MINION RESTORE + WIDE-SPAWN
- GLOBAL MUTANT LOOT-X3
- C# BUILD, 0 warnings / 0 errors
- ZIP CONTENT AUDIT

## Next live test
Force a normal non-Shiny Pelipper Mutant. Confirm x2 visible size and verify 2-4 minions actually appear. Then fight through all three HP phases, kill the main Mutant, and run `teamup_mutation status`. Expected reward telemetry: `dropCalls>=1`, `extraDropPasses=2`, `errors=0`. Also test one vanilla/custom non-Pelipper Mutant to confirm the same x3 reward applies globally.

## Carry-forward locks
- Current Shiny behavior stays frozen as accepted unless a concrete regression appears.
- Shiny remains Mutation-excluded.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Encounter discovery remains 20Hz and pair cache stays enabled.
- Never use hidden Green Slime 1,000,000 sentinel HP as Pokemon HP.
- Pelipper remains controller/render/ownership authority.
- Lower Workings remains unchanged and still gates 6.7.45.
- Mutants are NOT yet proven non-catchable through Pelipper's real ball-capture path.
