# Team Up handoff: 0.2.0-alpha.6.7.44.13

Canonical latest: `docs/LATEST_HANDOFF.md`
Detailed notes: `docs/ALPHA_6_7_44_13_PELIPPER_VISIBLE_SCALE_MINION_SPAWN_HANDOFF.md`

- Branch: `v0.2-alpha6-7-44-13-pelipper-visible-scale-minion-spawn`
- CI SHA: `c989c6407ac70ad799ffc5b06cbaac90c9717272`
- Run: `34778989370`
- Job: `103782389584`
- Artifact ID: `10324089476`
- Artifact wrapper SHA256: `ac0369fb12e2d249d918eeae10f5252a9d1705157674b551b955b354d8e49562`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.13_PELIPPER_VISIBLE_SCALE_MINION_SPAWN_TEST.zip`
- ZIP SHA256: `9e3c2fb4975c2944f915d38fb39e5002311178b31dfc3e66b40b30779cdfbd64`
- Build: PASS, 0 warnings / 0 errors
- main not merged
- 6.7.45 not started

## Live truth from 6.7.44.12
Pelipper Mutation core is live-confirmed: forced Mutation succeeds, `WildCurrentHealth/WildMaxHealth` binding resolves and writes correctly, source HP resolution has no failures, and natural lethal events now increment Mutation rolls.

Two remaining live failures were reproduced:
- visible Pokemon source did NOT become x3 larger even though generic telemetry said `scaleApplied=True`; that scale was only on the hidden combat proxy;
- minion waves requested 3-4 minions but spawned 0 with all candidates `safeRejected`, both on a custom map and on Farm.

## 6.7.44.13
New `Alpha674413PelipperVisibleMutationService` applies the configured Mutation scale to the paired visible `PelipperTown.PokemonNpc`, preferring `_visualScaleMultiplier`, and restores the original value when the wild Mutant encounter ends or changes role. It reapplies at the existing 20Hz runtime pulse if Pelipper overwrites presentation state.

New `Alpha674413MutationMinionSpawnService` preserves the original strict safe-spawn pass and only adds a fallback when that pass fails. The fallback still requires an on-map/passable tile, no placed object or terrain feature, and safe distance from Farmer/all characters, but drops the over-broad `CollisionMask.All` rejection.

New status lines:
- `Pelipper visible Mutation: tracked=... | applied=... | reapplied=... | restored=... | failed=... | last=...`
- `Mutation minion spawn fallback: attempts=... | resolved=... | rejected=... | last=...`

## Next live test
On a normal non-Shiny Pelipper wild Pokemon:
1. `teamup_mutation force`
2. visually confirm the actual Pokemon sprite becomes ~x3 larger;
3. verify at least one minion can spawn on Farm/custom map;
4. run `teamup_mutation status` and return the new visible-scale/minion-fallback lines plus source Mutation/HP binding and final MutationTelemetry;
5. continue the HPx3 phase test by depleting one real HP bar and verify `phaseGuards` + HP-binding writes increase.

Carry-forward locks:
- current Shiny behavior remains frozen as accepted unless a concrete regression appears;
- Shiny remains Mutation-excluded;
- active Following/Waiting teammates cannot receive held-item vanilla gifts;
- pair cache/performance safeguards remain enabled;
- never use hidden Green Slime 1,000,000 sentinel HP as Pokemon HP;
- Pelipper retains controller/render/ownership authority;
- Lower Workings remains unchanged and still gates 6.7.45;
- do not claim Mutants fully non-catchable until the actual Pelipper ball-capture path is intercepted and live-tested.
