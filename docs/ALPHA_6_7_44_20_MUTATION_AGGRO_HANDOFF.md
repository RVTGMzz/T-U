# Team Up 0.2.0-alpha.6.7.44.20 Mutation Aggro Handoff

## Checkpoint

- Branch: `v0.2-alpha6-7-44-20-mutation-aggro`
- Version: `0.2.0-alpha.6.7.44.20`
- CI source SHA: `712bc6a1bacb74fa0bd6273246f9f42b08927353`
- Run: `34974189152`
- Job: `104397637635`
- Artifact ID: `10398603342`
- Artifact: `team-up-alpha6-7-44-20-mutation-native-aggro`
- Wrapper SHA256: `db583c3f1fec2560bc38d522c82b652c690b625638b09cee91d5f4cf4c17b544`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.20_MUTATION_NATIVE_AGGRO_TEST.zip`
- Inner ZIP SHA256: `e18b146d28d539f0f6c87634ccb03dbe9305f151b1205d8742d2d4a74bb79efd`
- Build: PASS, 0 warnings / 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

## Live proof from 6.7.44.19

Rockruff at BusStop proved the native source-equivalent follower architecture live:

- forced Mutation succeeded;
- baseHP 78 -> mutantHP 234;
- x2 visible leader scale;
- requested 4 followers;
- Pelipper Spawn Commands gate was resolved while the player's option stayed disabled;
- four genuine wild Rockruff Lv.5 were created;
- `spawnedNow=4`, `pending=0`, `failed=0`, `safeRejected=0`;
- each follower had `capture=native` identity;
- no Slime fallback.

The remaining regression was hostility: the Mutant leader and all native followers stopped attacking the player.

## 6.7.44.20 delta

`Alpha674420MutationAggroService` restores native aggression without replacing provider AI.

For every active Mutation leader/minion in the current location it periodically re-arms Stardew's native pursuit state:

- `Monster.focusedOnFarmers = true`;
- `NPC.moveTowardPlayer(999)`;
- Team Up combat target opt-in remains true;
- zero-damage edge cases receive a minimum `DamageToFarmer = 1`.

For Pelipper wild encounters, both the hidden combat proxy and paired visible Pokemon source are armed. Team Up does not teleport actors and does not null/replace provider controllers.

Scan cadence is every 3 ticks; a given actor is refreshed at most once per 60 ticks.

Telemetry from `teamup_mutation status` now includes:

- `leaderArms`
- `minionArms`
- `pelipperProxyArms`
- `pelipperSourceArms`
- `damageFloors`
- `identityMisses`

## Current Mutation contract

One encounter is **1 Mutant leader + 2-4 ordinary hostile source-equivalent followers**.

Leader keeps HP x3, stat x2, visible Pelipper scale cap x2, aura and global native loot x3.

Followers match the pre-Mutation source creature, stay ordinary, have no Mutation bonus/aura/x3 leader reward, are Mutation-excluded, and remain natively catchable when the source provider supports capture.

No unrelated Slime fallback is allowed.

## Immediate runtime gate

Keep Pelipper Spawn Commands OFF.

1. Find a normal non-Shiny wild Pokemon.
2. Run `teamup_mutation force`.
3. Confirm 2-4 same-species followers still spawn.
4. Stand nearby without attacking and confirm leader + followers actively move toward / attack the Farmer.
5. Run `teamup_mutation status`.
6. Expect `leaderArms>0`, `minionArms>0`, `pelipperProxyArms>0`, `pelipperSourceArms>0`, ideally `identityMisses=0`.
7. Confirm one follower remains catchable with a Poké Ball.

If actors are visibly armed in telemetry but still do not move, Pelipper is overriding NPC movement after Team Up's native flags. The next fix should inspect/provider-hook its movement controller rather than introducing teleport movement.

After hostility passes, continue three leader HP phases, final x3 leader loot, vanilla/non-Pelipper Mutation regression, compatible custom source regression, and Lower Workings.

## Frozen locks

- Confirmed Shiny remains Mutation-excluded.
- No Slime fallback.
- Real Pelipper HP is `Griff.PelipperTown/WildCurrentHealth` / `WildMaxHealth`, never the proxy 1,000,000 sentinel.
- Preserve 20Hz encounter discovery and pair cache.
- Player-facing Pelipper Spawn Commands setting may remain OFF; Team Up's internal gate restores it immediately.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Lower Workings still gates 6.7.45 unless explicitly waived.
