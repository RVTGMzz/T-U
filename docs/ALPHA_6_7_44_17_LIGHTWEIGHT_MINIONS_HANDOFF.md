# Team Up handoff: 0.2.0-alpha.6.7.44.17

Branch: `v0.2-alpha6-7-44-17-lightweight-minions`

## Design lock
6.7.44.17 prioritizes runtime performance over capture semantics for Mutation followers.

For a Pelipper Mutation leader, Team Up must NOT ask Pelipper Town to spawn 2-4 complete new native wild encounters. A native Pelipper wild encounter carries a visible PokemonNpc, hidden combat proxy, WildEncounterId, HP modData, pairing/cache work, and Pelipper runtime bookkeeping. Repeating that 2-4 times is heavier than necessary.

Instead, Pelipper Mutation followers use Team Up's lightweight temporary monster path:
- one temporary Team Up combat actor per minion;
- no new Pelipper PokemonNpc/source actor;
- no new Pelipper WildEncounterId;
- no Pelipper source/proxy pairing for minions;
- no capture requirement or capture lock requirement;
- minions remain normal hostile, MutationExcluded, and never receive leader x3 loot;
- natural Pelipper wild Pokemon keep native capture behavior.

Non-Pelipper Mutation leaders still prefer same-runtime-type normal minions where safe.

## Carry-forward rules
- Mutant leader: HPx3, stat x2, Pelipper visible x2, aura, global x3 native loot.
- 2-4 followers: ordinary hostile, no Mutation bonuses, no x3 loot.
- Shiny remains Mutation-excluded.
- Lower Workings unchanged and still gates 6.7.45.
- main remains untouched.

## Next live gate
Force a normal non-Shiny Pelipper Mutation and verify:
1. 2-4 minions appear;
2. minions attack normally;
3. no noticeable hitch when the wave appears;
4. status reports Pelipper lightweight minion creation.
