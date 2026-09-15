# Team Up - Canonical Latest Handoff

Current restart file: `../CONTINUE_HERE.md`

Current detailed handoff: `ALPHA_6_7_44_20_MUTATION_AGGRO_HANDOFF.md`

## Current checkpoint

- Version: `0.2.0-alpha.6.7.44.20`
- Branch: `v0.2-alpha6-7-44-20-mutation-aggro`
- CI source SHA: `712bc6a1bacb74fa0bd6273246f9f42b08927353`
- Run: `34974189152`
- Job: `104397637635`
- Artifact ID: `10398603342`
- Wrapper SHA256: `db583c3f1fec2560bc38d522c82b652c690b625638b09cee91d5f4cf4c17b544`
- Inner ZIP SHA256: `e18b146d28d539f0f6c87634ccb03dbe9305f151b1205d8742d2d4a74bb79efd`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

## Current live truth

6.7.44.19 live Rockruff test proved the source-native follower pipeline: leader HP 78 -> 234, x2 scale, 4 requested, four genuine Rockruff Lv.5 created, `spawnedNow=4`, `pending=0`, `failed=0`, `safeRejected=0`, native capture identity and no Slime fallback.

The remaining regression is hostility: leader and native followers do not actively attack the player.

## 6.7.44.20 delta

`Alpha674420MutationAggroService` re-arms Stardew native pursuit for Mutation leaders/minions. It sets the Monster proxy to focus Farmers and walk toward the player, and also arms the paired visible Pelipper source. Zero-damage edge cases get a minimum contact damage of 1.

The service scans at the existing 3-tick cadence but refreshes each actor at most every 60 ticks. It does not teleport and does not null/replace provider controllers.

## Frozen Mutation contract

One Mutant leader + 2-4 ordinary hostile source-equivalent followers. No unrelated Slime fallback.

Pelipper followers remain genuine same-species wild encounters with native capture authority. Leader retains HP x3, stat x2, x2 visible cap, aura and global x3 native loot. Followers remain ordinary, Mutation-excluded, and receive no x3 leader reward.

## Next runtime sequence

1. Keep Pelipper Spawn Commands OFF.
2. Force a normal non-Shiny Pelipper Mutation.
3. Confirm 2-4 same-species followers still spawn.
4. Stand nearby and verify leader + followers pursue and damage Farmer without first being hit.
5. Run `teamup_mutation status`; expect positive `leaderArms`, `minionArms`, `pelipperProxyArms`, `pelipperSourceArms`, ideally `identityMisses=0`.
6. Recheck Poké Ball capture on one follower.
7. Then test 3 HP phases, final x3 leader loot, vanilla/custom regression and Lower Workings.
8. Only then prepare 6.7.45 unless explicitly waived.

## Carry-forward locks

- Confirmed Shiny remains Mutation-excluded.
- Real Pelipper HP stays on `WildCurrentHealth` / `WildMaxHealth`, never proxy sentinel HP.
- Preserve 20Hz encounter discovery and pair cache.
- Spawn Commands player setting may remain OFF; Team Up's internal gate restores it immediately.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Lower Workings still gates 6.7.45 unless explicitly waived.
