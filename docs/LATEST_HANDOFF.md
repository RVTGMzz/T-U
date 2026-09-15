# Team Up - Canonical Latest Handoff

Current restart file: `../CONTINUE_HERE.md`

Current detailed handoff: `ALPHA_6_7_44_23_MUTANT_LEADER_SMOOTHING_HANDOFF.md`

## Current checkpoint

- Version: `0.2.0-alpha.6.7.44.23`
- Branch: `v0.2-alpha6-7-44-23-mutant-leader-smoothing`
- CI source SHA: `2c8f132527cf06aa2d17a82875d7b6f4f46750b4`
- Run: `34996106050`
- Job: `104472621463`
- Artifact ID: `10407443067`
- Wrapper SHA256: `f27e09e980708d06a91c1e74bbd7a5094b92a2637aa405916fd5df8b8a283080`
- Inner ZIP SHA256: `d95ed6f2a48447ec967032b38b08c2b30632547ecc3fc02351278981fec1efac`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

## Current live truth

Source-native Pelipper followers are live-proven: genuine same species, 2-4 contract, Spawn Commands may remain OFF, and no unrelated Slime fallback.

6.7.44.22 live testing proved followers now attack Farmer. Its remaining defect is leader-only: the x2 Mutant attacks only when very close and visibly jitters instead of moving like a normal Pokemon.

## 6.7.44.23 delta

`Alpha674423PelipperMutantLeaderSmoothingService` keeps the working follower pack behavior and replaces only the leader motion policy:

- no pack separation is applied to the leader;
- Pelipper passive movement/velocity is cleared with `source.Halt()` before each leader chase step;
- short direction hysteresis reduces rapid axis flipping;
- the leader holds a stable melee band rather than forcing overlap with Farmer;
- leader attack reach is extended to fit the x2 visual size;
- followers still use attack-ring targets, separation, leader clearance and sidestep recovery;
- genuine source/proxy identity and native capture architecture remain intact.

6.7.44.22 remains in source history but is not instantiated at runtime in 6.7.44.23.

## Next runtime sequence

1. Keep Pelipper Spawn Commands OFF.
2. Force a normal non-Shiny Pelipper Mutation.
3. Do not attack first.
4. Verify followers still pursue and damage Farmer.
5. Verify the x2 leader approaches without the prior jitter.
6. Verify the leader stops near Farmer instead of trying to overlap the player.
7. Verify leader damage lands from noticeably farther away.
8. Run `teamup_mutation status` and inspect `leaderReachHits`, `leaderRangeHolds`, `leaderHaltResets`, `leaderDirectionChanges`, `leaderDirectionLocks`, `leaderMoves`, `minionMoves`, `proxySyncs`, `contactDamageCalls`, `identityMisses`.
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
