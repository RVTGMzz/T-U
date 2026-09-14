# Team Up handoff: 0.2.0-alpha.6.7.44.15

Canonical latest: `docs/LATEST_HANDOFF.md`
Detailed notes: `docs/ALPHA_6_7_44_15_MUTANT_MINIONS_GLOBAL_LOOT_X3_HANDOFF.md`

- Branch: `v0.2-alpha6-7-44-15-mutant-minions-global-loot-x3`
- CI SHA: `61fe5b59811ea6cfe3cd95da3f3cf201665f1b9b`
- Run: `34863995611`
- Job: `104043116358`
- Artifact ID: `10356535462`
- Artifact wrapper SHA256: `00f38c9f0cd199568ae64508e0fef18686ea2fc284c33160566ed3f1997e768b`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.15_MUTANT_MINIONS_GLOBAL_LOOT_X3_TEST.zip`
- ZIP SHA256: `a44958d0bd1053d83cea9fd0b6e07ac0db9694870d20c43f50db886013c805ad`
- Build: PASS, 0 warnings / 0 errors
- main not merged
- 6.7.45 not started

## Live truth carried forward
Pelipper Mutation core, proxy-modData HP binding, natural Mutation rolls and visible source scaling are working. x3 visible scale was too large, so visible Pelipper Mutants are capped at x2.

6.7.44.13 live still showed missing minions (`spawned=0/N`, all safe candidates rejected). User clarified this is a bug to fix, NOT permission to remove the 2-4 minion mechanic.

## 6.7.44.15 design lock
- Every Mutant keeps the intended 2-4 minion wave.
- Pelipper minion suppression from 6.7.44.14 is removed.
- The fallback minion search anchors Pelipper waves on the visible Pokemon source, searches radius 2..8, permits harmless terrain like grass, and keeps passability/object/occupancy safety gates.
- Every main Mutant gets x3 native loot after final defeat, regardless of vanilla/custom/Pelipper origin.
- x3 reward means the native `monsterDrop` path is run three total times with recursion protection, not a literal clone of one selected item.
- Mutation minions remain loot-suppressed by default.
- Pelipper visible Mutation scale remains x2.

## Next live test
Force a normal non-Shiny Pelipper Mutation. Confirm x2 size, verify 2-4 minions appear, and run `teamup_mutation status`. Expected minion fallback if needed: `resolved>0`, and Pelipper may show `pelipperSourceAnchors>0`. Fight through all three HP phases, kill the main Mutant, then expect `Mutant reward: ... dropCalls>=1 | extraDropPasses=2 | errors=0`. Also test one non-Pelipper Mutant to prove x3 loot scope is global.

Carry-forward locks:
- current Shiny behavior remains frozen as accepted unless a concrete regression appears;
- Shiny remains Mutation-excluded;
- active Following/Waiting teammates cannot receive held-item vanilla gifts;
- pair cache/performance safeguards remain enabled;
- never use hidden Green Slime 1,000,000 sentinel HP as Pokemon HP;
- Pelipper retains controller/render/ownership authority;
- Lower Workings remains unchanged and still gates 6.7.45;
- do not claim Mutants fully non-catchable until the actual Pelipper ball-capture path is intercepted and live-tested.
