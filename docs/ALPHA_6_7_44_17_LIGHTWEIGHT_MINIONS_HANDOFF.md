# Team Up handoff: 0.2.0-alpha.6.7.44.17

Branch: `v0.2-alpha6-7-44-17-lightweight-minions`

## CI verified checkpoint
- Verified source SHA: `c1df68ef8f6f8d0e7bd73d1876b32c28c914c00e`
- Run: `34904471245`
- Job: `104177799252`
- Artifact ID: `10371887676`
- Artifact name: `team-up-alpha6-7-44-17-lightweight-minions`
- Artifact wrapper SHA256: `ca2c4e2426c2c195c1f201030164d064244f6b639e87a4c349b05e766cbfa8f9`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.17_LIGHTWEIGHT_MUTATION_MINIONS_TEST.zip`
- Inner ZIP SHA256: `dc19dd525401583892f2c056b4448dcb1aeff01531b410f073ec73469f4c680d`
- Build: PASS, 0 warnings / 0 errors
- ZIP content audit: PASS
- main: NOT merged
- 6.7.45: NOT started

## Design lock
6.7.44.17 prioritizes runtime performance over capture semantics for Mutation followers.

For a Pelipper Mutation leader, Team Up does NOT ask Pelipper Town to spawn 2-4 complete new native wild encounters. A native Pelipper wild encounter carries a visible PokemonNpc, hidden combat proxy, WildEncounterId, HP modData, pairing/cache work, and Pelipper runtime bookkeeping. Repeating that 2-4 times for temporary followers would be heavier than necessary.

Instead, Pelipper Mutation followers use Team Up's lightweight temporary monster path:
- one temporary Team Up combat actor per minion;
- no new Pelipper PokemonNpc/source actor;
- no new Pelipper WildEncounterId;
- no Pelipper source/proxy pairing for minions;
- no capture requirement or capture-lock requirement;
- minions remain ordinary hostile, MutationExcluded, and never receive leader x3 loot;
- natural Pelipper wild Pokemon keep native Pelipper capture behavior.

Non-Pelipper Mutation leaders still prefer same-runtime-type ordinary minions where safe.

## Telemetry added
`teamup_mutation status` now exposes the lightweight policy through the existing leader/minion line:
- `pelipperLightweight=...`
- `nativePelipperSpawnsAvoided=...`

A Pelipper wave should increase both counters for each lightweight follower created. The follower also keeps the leader species as metadata for diagnostics, but it is not promoted into a full Pelipper encounter.

## CI gates passed
- PELIPPER MODDATA HP + PAIR CACHE CARRY-FORWARD
- PELIPPER VISIBLE MUTATION X2 CAP + RESTORE
- MUTATION 2-4 MINION WIDE-SPAWN CARRY-FORWARD
- MUTANT LEADER + NORMAL HOSTILE MINION POLICY
- PELIPPER LIGHTWEIGHT ONE-ACTOR MINION POLICY
- GLOBAL LEADER-ONLY MUTANT LOOT-X3
- C# BUILD: PASS, 0 warnings / 0 errors
- ZIP CONTENT AUDIT: PASS

## Carry-forward rules
- Mutant leader: HPx3, stat x2, Pelipper visible x2, aura, global x3 native loot.
- 2-4 followers: ordinary hostile, no Mutation bonuses, no x3 loot.
- Shiny remains Mutation-excluded.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Encounter discovery remains 20Hz and Pelipper pair cache remains enabled.
- Never use the hidden 1,000,000 HP Green Slime sentinel as Pokemon combat HP.
- Pelipper retains controller/render/ownership authority for the real wild leader encounter.
- Lower Workings remains unchanged and still gates 6.7.45.

## Next live gate
Force a normal non-Shiny Pelipper Mutation and verify:
1. 2-4 followers appear;
2. followers attack normally;
3. no noticeable hitch/lag when the wave appears;
4. `teamup_mutation status` reports `pelipperLightweight>0` and `nativePelipperSpawnsAvoided>0`;
5. leader still has x2 visible size and three HP phases;
6. final leader defeat still gives x3 native loot.

Also test one non-Pelipper Mutant to make sure same-runtime-type followers and global x3 leader loot still behave normally.
