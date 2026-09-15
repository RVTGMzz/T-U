# Team Up - Canonical Latest Handoff

Current restart file: `../CONTINUE_HERE.md`

Current detailed handoff: `ALPHA_6_7_44_21_PELIPPER_MUTATION_HOSTILITY_HANDOFF.md`

## Current checkpoint

- Version: `0.2.0-alpha.6.7.44.21`
- Branch: `v0.2-alpha6-7-44-21-pelipper-mutation-hostility`
- CI source SHA: `9bb3a88a98c51a3695ffa9e982f3da482c08632c`
- Run: `34976619122`
- Job: `104405839414`
- Artifact ID: `10398993101`
- Wrapper SHA256: `c27601d8160b76edaad383ed0be7a1dcd47f0b5e4b672101e71b8b38ae41d72e`
- Inner ZIP SHA256: `7e68d423593acf32efcfa1d618bb5d5f90cfa9a0f9679e976b309ee6aaa602e9`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

## Current live truth

Source-native Pelipper followers are live-proven: same species, 2-4 contract, 4/4 observed, Spawn Commands may remain OFF, no pending/failure/safe reject and no Slime fallback.

6.7.44.20 then proved Pelipper wild actors remain passive even when Team Up correctly arms Stardew generic pursuit. Growlithe status showed `leaderArms=8`, `minionArms=67`, `pelipperProxyArms=75`, `pelipperSourceArms=75`, `damageFloors=4`, `identityMisses=0`, but no attacks. Do not treat generic Monster pursuit flags as the Pelipper hostility solution.

## 6.7.44.21 delta

`Alpha674421PelipperMutationHostilityService` gives only Pelipper Mutation leaders/minions a Team Up-owned combat approach:

- resolve the genuine Pelipper visible source + hidden combat proxy pair;
- path the real visible Pokemon toward a passable tile adjacent to Farmer using `PathFindController`;
- repath every 12 ticks or when the target changes;
- synchronize the real combat proxy to the source position;
- when contact occurs, call `Farmer.takeDamage(..., proxy)` with a 45-tick per-proxy cooldown;
- preserve the source/proxy pair instead of replacing or respawning actors;
- leave ordinary non-Mutation Pelipper wilds passive and untouched.

## Frozen Mutation contract

One Mutant leader + 2-4 ordinary hostile source-equivalent followers. No unrelated Slime fallback.

Pelipper followers remain genuine same-species wild encounters. Leader retains HP x3, stat x2, x2 visible cap, aura and global native loot x3. Followers remain ordinary, Mutation-excluded and receive no x3 leader reward.

## Next runtime sequence

1. Keep Pelipper Spawn Commands OFF.
2. Force a normal non-Shiny Pelipper Mutation.
3. Do not attack first.
4. Verify visible leader + followers actually chase Farmer.
5. Verify contact reduces Farmer HP.
6. Run `teamup_mutation status`; expect `pairs>0`, positive leader/minion counts, `pathsBuilt>0`, `pathSteps>0`, usually `proxySyncs>0`, and after contact `contactDamageCalls>0`; ideally `identityMisses=0`.
7. Verify native follower capture with a Poké Ball.
8. Then test all three leader HP phases and final x3 leader loot.
9. Test vanilla/non-Pelipper and a compatible custom source when practical.
10. Complete Lower Workings runtime gate.
11. Only then prepare 6.7.45 unless explicitly waived.

## Carry-forward locks

- Confirmed Shiny remains Mutation-excluded.
- Real Pelipper HP remains `Griff.PelipperTown/WildCurrentHealth` / `WildMaxHealth`, never the proxy's technical sentinel HP.
- Preserve 20Hz encounter discovery and species-pair cache.
- Spawn Commands player setting can remain OFF; internal Team Up access restores it immediately.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Lower Workings remains unchanged and gates 6.7.45 unless explicitly waived.
