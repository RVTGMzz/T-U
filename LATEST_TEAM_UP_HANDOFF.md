# Team Up handoff: 0.2.0-alpha.6.7.44.12

Canonical latest: `docs/LATEST_HANDOFF.md`
Detailed notes: `docs/ALPHA_6_7_44_12_PELIPPER_MODDATA_HP_BINDING_HANDOFF.md`

- Branch: `v0.2-alpha6-7-44-12-pelipper-moddata-hp-binding`
- CI SHA: `c79916d0718c567975fd028f9b0f17f674cda015`
- Run: `34753394198`
- Job: `103713602860`
- Artifact ID: `10316283846`
- ZIP SHA256: `83f80afa3acd28c33aa50287b0ba454ad6be5ba77715e6e69be26b9484927912`
- Build: PASS, 0 warnings / 0 errors
- main not merged
- 6.7.45 not started

## Live acceptance on 6.7.44.12
Forced Pelipper Mutation now succeeds on a normal wild Seadra.

Observed live telemetry:
- `Mutation: rolls=2 | mutations=1 | active=1`
- `Pelipper SOURCE mutation: sourceDamageCalls=16 | hpResolved=16 | hpUnresolved=0 | sourceLethalCandidates=2 | mutationAttempts=2 | transformBlocked=0 | forceTransforms=1`
- `Pelipper modData HP binding: resolved=19 | writes=1 | invalid=0 | fallbacks=0`
- last source mutation: `mutated source=Seadra hp=160/160 logicalHP=480 bars=3 extraLives=2 via=PelipperProxyModData.WildCurrentHealth/PelipperProxyModData.WildMaxHealth force=True`

This proves:
- Pelipper authoritative wild HP is correctly bound from `Griff.PelipperTown/WildCurrentHealth` + `WildMaxHealth` on the combat proxy modData;
- the hidden `Monster.Health/MaxHealth=1000000` sentinel is no longer used as Pokemon HP;
- forced Mutation transforms the real Pelipper encounter successfully;
- natural lethal events now reach the Mutation roll path. `rolls=2` is natural-roll telemetry because forced transforms do not increment `_rolls`; both natural rolls simply missed 5% in this test;
- old source HP reflection probes stayed cold after the correct binding was active.

## Remaining runtime gates before declaring the Pelipper Mutation path complete
1. HPx3 phase behavior still needs live verification. Current mutant has `bars=3`, `extraLives=2`, but `phaseGuards=0` because no mutant phase was depleted during the captured status.
2. Minion spawning is not yet accepted. The forced Seadra requested 3 minions but logged `spawned=0/3`, `safeRejected=3`. Diagnose safe-spawn rejection on custom locations before calling minions complete.
3. Confirm the visible Pokemon source actually shows the intended Mutation presentation (aura and desired visual scale). `auraDraws` is active; generic `scaleApplied=True` may refer to the hidden combat proxy and should not be assumed to scale the visible PokemonNpc until observed in-game.
4. Performance remains a live gate, but current pairing telemetry is healthy: `resolved=4`, `cacheHits=901`, `cacheInvalidated=0`. High cache hits are expected reuse, not repeated full-map pairing scans.

Carry-forward locks:
- current Shiny behavior remains frozen as accepted unless a concrete regression appears;
- Shiny remains Mutation-excluded;
- active Following/Waiting teammates cannot receive held-item vanilla gifts;
- pair cache/performance safeguards remain enabled;
- NPC base damage progression remains moderate with slow level growth;
- Lower Workings remains unchanged and still gates 6.7.45.

Next live test: attack the active Seadra mutant until one HP bar is depleted, then run `teamup_mutation status`. Expected: `phaseGuards` increments and `Pelipper modData HP binding writes` increases as `WildCurrentHealth` is restored. Also report whether the visible Seadra is actually enlarged and whether combat feels lag-free. Minion `safeRejected=3` will be handled as a separate runtime fix if it reproduces.
