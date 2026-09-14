# Team Up handoff: 0.2.0-alpha.6.7.44.17

Branch: `v0.2-alpha6-7-44-17-mutant-capture-lock`

## Design change from live feedback
Captureability is no longer a design gate for Mutation minions. The priority is runtime cost.

For Pelipper Mutation leaders, Team Up should NOT ask Pelipper Town to spawn 2-4 complete new wild encounters. A native Pelipper wild encounter carries visible PokemonNpc + combat proxy + WildEncounterId/pairing/modData/runtime work. Multiplying that by 2-4 followers would be heavier than necessary.

Therefore 6.7.44.17 uses the lightweight Team Up minion path:
- one temporary Team Up combat actor per minion;
- no new Pelipper WildEncounterId;
- no Pelipper source/proxy pair for the minion;
- no capture requirement or capture lock requirement;
- minions are ordinary hostile, MutationExcluded and leader-loot bonus does not apply;
- Pelipper Mutant leader remains the real source-aware Pelipper encounter;
- non-Pelipper Mutation still prefers same-runtime-type minions where safe.

## Carry-forward
- Mutant leader: HPx3, stat x2, Pelipper visible x2, aura, global x3 native loot.
- 2-4 minions: normal hostile, no Mutation bonus, no x3 loot.
- Shiny remains Mutation-excluded.
- Natural Pelipper wild Pokemon retain native Pelipper capture behavior.
- Lower Workings unchanged; 6.7.45 not started.

## Next live gate
Force a Pelipper Mutation and verify:
1. 2-4 minions spawn;
2. minions attack normally;
3. no noticeable hitch/lag when the wave appears;
4. `teamup_mutation status` reports Pelipper lightweight minions instead of native Pelipper encounter spawning.
