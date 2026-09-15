# Team Up latest handoff: 0.2.0-alpha.6.7.44.20

Start here: `CONTINUE_HERE.md`

Detailed handoff: `docs/ALPHA_6_7_44_20_MUTATION_AGGRO_HANDOFF.md`

## Current checkpoint

- Branch: `v0.2-alpha6-7-44-20-mutation-aggro`
- Version: `0.2.0-alpha.6.7.44.20`
- CI source SHA: `712bc6a1bacb74fa0bd6273246f9f42b08927353`
- Run: `34974189152`
- Job: `104397637635`
- Artifact ID: `10398603342`
- Wrapper SHA256: `db583c3f1fec2560bc38d522c82b652c690b625638b09cee91d5f4cf4c17b544`
- Inner ZIP SHA256: `e18b146d28d539f0f6c87634ccb03dbe9305f151b1205d8742d2d4a74bb79efd`
- Build: PASS, 0 warnings / 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

## Live result from 6.7.44.19

Rockruff proved source-native Pelipper followers spawn correctly while Spawn Commands stays OFF: 4/4 genuine same-species wild followers, no pending/failure/safe reject, native capture identity, no Slime fallback.

Remaining regression: leader and followers no longer attacked Farmer.

## 6.7.44.20

Adds `Alpha674420MutationAggroService`. Mutation leaders/minions are re-armed with Stardew native pursuit controls; Pelipper hidden proxy + visible source are armed together. No teleport or provider-controller replacement.

Immediate gate: force a Pelipper Mutation, verify same-species spawn remains good, then verify leader + followers actively pursue and damage Farmer. `teamup_mutation status` should show positive `leaderArms`, `minionArms`, `pelipperProxyArms`, `pelipperSourceArms`, ideally zero `identityMisses`.

Recheck follower capture after aggro. Then continue 3 HP phases, x3 final loot, vanilla/custom regression and Lower Workings.

Do not begin 6.7.45 unless remaining runtime gates are passed or explicitly waived.
