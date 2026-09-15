# Team Up - Canonical Latest Handoff

Current restart file: `../CONTINUE_HERE.md`

Current detailed handoff: `ALPHA_6_7_44_22_PELIPPER_PACK_STEERING_HANDOFF.md`

## Current checkpoint

- Version: `0.2.0-alpha.6.7.44.22`
- Branch: `v0.2-alpha6-7-44-22-pelipper-pack-steering`
- CI source SHA: `1921deff08d397cca6c867fcce35f5b184c68c1c`
- Run: `34991407821`
- Job: `104456710100`
- Artifact ID: `10405981878`
- Wrapper SHA256: `60b24fdb084fc687da548569e390546fed627330219ab2cc0b55a13f6f90529b`
- Inner ZIP SHA256: `51c5422cb6c456e9946290c9c65090f4b4438aae9dfbd9fa76cb2d2a260e6432`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

## Current live truth

Source-native Pelipper followers are already live-proven: genuine same species, 2-4 contract, Spawn Commands may remain OFF, no unrelated Slime fallback.

6.7.44.21 proved that Team Up can make the real Pelipper Pokemon chase Farmer, but the tile PathFindController approach is superseded because small followers can block the x2 leader and passive/gentle species move unnaturally.

## 6.7.44.22 delta

`Alpha674422PelipperMutationSteeringService` uses low-level NPC movement instead of tile pathing. Followers approach a small ring around Farmer, pack members separate, followers strongly yield to the leader, the leader gets right-of-way, hidden proxies do not physically block their source Pokemon, and blocked actors perform short alternating sidesteps. Contact damage still uses the genuine combat proxy and native capture identity is preserved.

The 6.7.44.21 hostility service is not instantiated at runtime in this checkpoint.

## Next runtime sequence

1. Keep Pelipper Spawn Commands OFF.
2. Force a normal non-Shiny Pelipper Mutation.
3. Prefer testing both a small species and a large/gentle species.
4. Do not attack first. Verify leader and followers pursue Farmer with better spacing.
5. Verify the x2 leader is not trapped behind followers.
6. Verify passive species no longer move with the obvious grid-NPC behavior seen in 6.7.44.21.
7. Verify contact reduces Farmer HP.
8. Run `teamup_mutation status`; inspect `leaderMoves`, `minionMoves`, `separation`, `leaderClearance`, `sidesteps`, `blockedFrames`, `proxySyncs`, `contactDamageCalls`, `identityMisses`.
9. Recheck a follower with a Poke Ball.
10. Then test all three leader HP phases and final x3 leader loot.
11. Test vanilla/non-Pelipper and compatible custom source when practical.
12. Complete Lower Workings runtime gate.
13. Only then prepare 6.7.45 unless explicitly waived.

## Carry-forward locks

- Confirmed Shiny remains Mutation-excluded.
- Real Pelipper HP remains `WildCurrentHealth` / `WildMaxHealth`, never proxy sentinel HP.
- Preserve 20Hz encounter discovery and species-pair cache.
- Spawn Commands setting can remain OFF; Team Up restores the internal gate immediately.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Lower Workings remains unchanged and gates 6.7.45 unless explicitly waived.
