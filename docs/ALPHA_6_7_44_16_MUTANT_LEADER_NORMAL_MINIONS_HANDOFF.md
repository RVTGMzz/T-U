# Team Up handoff: 0.2.0-alpha.6.7.44.16

## Checkpoint
- Branch: `v0.2-alpha6-7-44-16-mutant-leader-minion-hostility`
- CI SHA: `8ab6f7105e505ec47a63f6dd93f77251e7c4fdc5`
- Run: `34901111167`
- Job: `104167024063`
- Artifact ID: `10370307939`
- Artifact wrapper SHA256: `bf313a16b2496c56c5ef2c820e506b9b6439eb8017585993558148cf030c24c4`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.16_MUTANT_LEADER_NORMAL_MINIONS_TEST.zip`
- Inner ZIP SHA256: `5074ee09ee439a0b94ec30bc1ae95d997c6da9236cbcf820eaac6941b3e46dbc`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

## User design lock
Mutation encounter is one Mutant leader plus 2-4 ordinary hostile followers.

Leader:
- remains hostile;
- HP x3 / stat x2 / visual x2 for Pelipper source presentation;
- receives global x3 native loot after final defeat;
- only the leader carries the Mutant/loot multiplier state.

Followers:
- remain ordinary hostile monsters, not Mutants;
- are immediately Mutation-excluded so they cannot recursively mutate;
- receive no Mutant aura/scale/stat/HP multiplier;
- receive no x3 Mutant loot bonus;
- keep the existing minion-loot policy;
- remain valid Team Up combat targets.

`MonsterMutationMinionFactory` still prefers the same runtime monster type when it can safely reconstruct it; otherwise it uses the existing ordinary GreenSlime fallback. This is a safe priority rule, not a promise that every third-party/Pelipper species can be reconstructed as the exact same species yet.

## 6.7.44.16 implementation
New `Alpha674416MutationLeaderMinionPolicyService` Harmony-patches successful Mutation conversion and minion-wave completion. It marks one leader, normalizes newly spawned minions, strips accidental Mutant/x3-loot state from minions, sets Mutation exclusion, and preserves combat targeting.

New status line:
`Mutation leader/minion policy: leader=Mutant | minions=normal-hostile | leaderLoot=x3 | minionLootBonus=none | leadersMarked=... | minionsNormalized=... | hostileReady=... | mutationGuards=... | lootMarkersStripped=... | sameType=... | fallback=... | last=...`

Carry-forward:
- 2-4 minion wide-ring spawn fallback remains enabled, including Pelipper visible-source anchoring;
- global x3 loot remains scoped to Mutant leaders of any origin, not Pelipper-only;
- Pelipper visible Mutation scale remains x2;
- Shiny behavior remains frozen as currently accepted and Shiny remains Mutation-excluded;
- active Following/Waiting teammates cannot receive held-item vanilla gifts;
- pair cache and 20Hz encounter discovery remain enabled;
- hidden Green Slime sentinel HP is never treated as Pokemon HP;
- Lower Workings still gates 6.7.45;
- Mutants are not yet proven non-catchable through Pelipper's real ball-capture path.

## Next live test
Force a normal non-Shiny Mutation. Confirm the leader and all spawned followers attack the player/party. Visually verify followers remain ordinary size/state. Run `teamup_mutation status` and expect `leadersMarked>=1`, `minionsNormalized` matching the spawned wave, `hostileReady` matching living followers, and no follower x3-loot marker. Kill the leader through all phases and verify x3 leader reward telemetry. Test both Pelipper and one non-Pelipper Mutant.
