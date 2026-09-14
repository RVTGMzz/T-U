# Team Up handoff: 0.2.0-alpha.6.7.44.14

Canonical latest: `docs/LATEST_HANDOFF.md`

## Source of truth
- Branch: `v0.2-alpha6-7-44-14-pelipper-x2-loot-x3`
- Version: `0.2.0-alpha.6.7.44.14`
- CI input SHA: `96811b01800b2eeaf1a7a3247899818d74a791b1`
- Successful CI run: `34862316701`
- Successful CI job: `104037413329`
- Artifact ID: `10355521041`
- Artifact wrapper SHA256: `9c0985f806bce5fc87ab98104c44b785064650a01530b73c92e09827332c9c96`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.14_PELIPPER_X2_LOOT_X3_TEST.zip`
- Inner ZIP SHA256: `8f81c20f34305dc3ceaa5f2823b7fd046ae04a05004a51170b25a7d06bf08b8b`
- Build: PASS, 0 warnings / 0 errors
- main: NOT merged
- 6.7.45: NOT started

## Live truth carried from 6.7.44.13
Pelipper Mutation itself is working. The real visible Pokemon source can be scaled, HP binding resolves through proxy modData, forced Mutation succeeds, and natural lethal events reach Mutation rolls.

Live Fidough test proved x3 visible scale is too large even though the source scaling path is correct:
- `Pelipper visible Mutation: tracked=1 | applied=1 ... last=scaled source=Fidough member=_visualScaleMultiplier 1->3 force=True`
- `Mutation minion spawn fallback: attempts=4 | resolved=0 | rejected=4`
- `spawned=0/4 ... safeRejected=4`

User decision for the next pass:
- visible Pelipper Mutant scale should be x2;
- temporarily stop relying on the broken 2-4 Pelipper minion wave;
- compensate Pelipper Mutants with x3 loot.

## 6.7.44.14
### Visible scale x2
`Alpha674413PelipperVisibleMutationService` now hard-caps visible Pelipper Mutation scale at x2 even when an existing user config still says x3. New configs default `MutationVisualScaleMultiplier=2f`.

The existing source-actor restore behavior remains: the original PokemonNpc scale is restored when the encounter ends or the actor stops being a wild Mutant.

### Pelipper Mutant reward policy
New `Alpha674414PelipperMutantRewardService` marks successful Pelipper Mutants with:
`Ronvotri.TeamUp/PelipperMutantLootMultiplier=3`

For marked Pelipper Mutants only:
- the unreliable queued 2-4 minion wave is temporarily suppressed;
- generic/non-Pelipper Mutation minions remain unchanged;
- when Stardew's native `GameLocation.monsterDrop` path handles the final death, Team Up repeats that same native drop pass two additional times, for three native drop passes total;
- a ThreadStatic re-entry guard prevents recursion;
- Team Up does not fabricate replacement item IDs.

This is intentionally a runtime-test feature. Do NOT claim x3 loot fully accepted until final Pelipper Mutant death produces `dropCalls>=1`, `extraDropPasses=2`, `errors=0`, and the player sees the additional loot. If `dropCalls=0`, Pelipper bypasses `GameLocation.monsterDrop` and the reward hook must move to Pelipper's actual drop path.

New status line:
`Pelipper Mutant reward: lootX3 | minions=off | marked=... | minionWavesSuppressed=... | dropHooks=... | dropCalls=... | extraDropPasses=... | errors=... | last=...`

## CI acceptance
Run `34862316701` passed:
- `PELIPPER MODDATA HP + PAIR CACHE CARRY-FORWARD: PASS`
- `PELIPPER VISIBLE MUTATION X2 CAP + RESTORE AUDIT: PASS`
- `GENERIC MUTATION MINION FALLBACK CARRY-FORWARD: PASS`
- `PELIPPER MINIONS-OFF + LOOT-X3 REWARD AUDIT: PASS`
- `C# BUILD: PASS`
- `ZIP CONTENT AUDIT: PASS`
- 0 warnings / 0 errors

## Next live test
Use a normal non-Shiny Pelipper wild Pokemon:
1. run `teamup_mutation force`;
2. visually confirm the actual Pokemon source is about x2, not x3;
3. wait long enough that the old minion wave would have spawned; expected Pelipper minions remain off;
4. run `teamup_mutation status` and verify `Pelipper Mutant reward: ... marked>=1` and eventually `minionWavesSuppressed>=1`;
5. fight through the HPx3 phases; the first two lethal bars should increment `phaseGuards`, then the third/final bar should complete the encounter;
6. after final death, run status again. Expected reward telemetry: `dropCalls>=1 | extraDropPasses=2 | errors=0` and visibly increased native loot.

## Carry-forward locks
- Current Shiny behavior remains frozen as accepted unless a concrete regression appears.
- Shiny remains Mutation-excluded.
- Active Following/Waiting Team Up members cannot receive vanilla held-item gifts.
- Encounter discovery remains 20Hz; pair cache/performance safeguards remain enabled.
- Pelipper authoritative HP remains `WildCurrentHealth/WildMaxHealth`; never use hidden proxy 1,000,000 HP as Pokemon HP.
- Pelipper keeps controller/render/ownership authority.
- Mutants are NOT yet proven non-catchable through Pelipper's real ball-capture path.
- Lower Workings remains unchanged and still gates 6.7.45.
