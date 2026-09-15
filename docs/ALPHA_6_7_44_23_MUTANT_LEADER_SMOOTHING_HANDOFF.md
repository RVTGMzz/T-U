# Team Up 0.2.0-alpha.6.7.44.23 - Mutant Leader Smoothing Handoff

## Verified build

- Branch: `v0.2-alpha6-7-44-23-mutant-leader-smoothing`
- Version: `0.2.0-alpha.6.7.44.23`
- CI source SHA: `2c8f132527cf06aa2d17a82875d7b6f4f46750b4`
- CI run: `34996106050`
- CI job: `104472621463`
- Artifact ID: `10407443067`
- Wrapper SHA256: `f27e09e980708d06a91c1e74bbd7a5094b92a2637aa405916fd5df8b8a283080`
- Inner ZIP SHA256: `d95ed6f2a48447ec967032b38b08c2b30632547ecc3fc02351278981fec1efac`
- Build: PASS, 0 warnings / 0 errors

## Live truth before this checkpoint

6.7.44.19 live-proved genuine same-species Pelipper followers with Spawn Commands OFF and no Slime fallback.

6.7.44.20 proved generic Monster pursuit flags do not make Pelipper wild Pokemon hostile.

6.7.44.21 proved Team Up-owned chase can make genuine Pelipper Pokemon pursue Farmer, but tile pathing was visually bad and could jam the x2 leader.

6.7.44.22 live testing improved the pack substantially: followers now attack Farmer. Remaining defects are specifically on the Mutant leader:

- the leader only damages Farmer at very close range;
- the leader visibly jitters / looks unlike a normal moving Pokemon;
- follower behavior is already usable and should not be regressed.

## 6.7.44.23 delta

`Alpha674423PelipperMutantLeaderSmoothingService` supersedes the 6.7.44.22 runtime steering instance while retaining its successful follower pack behavior.

Leader-specific changes:

- leader ignores follower separation completely and gets hard right-of-way;
- before each leader chase step, `source.Halt()` clears Pelipper passive movement/velocity state;
- leader uses short cardinal direction hysteresis (`LeaderDirectionLockTicks=8`, `LeaderAxisSwitchBias=24`) to reduce rapid axis flipping;
- leader stops in a stable melee band around Farmer (`LeaderHoldCenterDistance=92` pixels) instead of trying to overlap the player;
- leader can deal melee damage out to `LeaderAttackCenterDistance=112` pixels, matching the visual presence of the x2 Mutant better;
- followers keep attack-ring, separation, leader clearance and sidestep behavior from 6.7.44.22;
- genuine source/proxy identity, proxy-based damage and native capture architecture remain unchanged.

6.7.44.22 remains in source history but is NOT instantiated at runtime in 6.7.44.23.

## Immediate live test

Keep Pelipper Spawn Commands OFF and run:

```text
teamup_mutation force
```

Do not attack first. Check:

1. followers still attack normally;
2. Mutant leader visibly approaches without the previous rapid jitter;
3. leader stops near Farmer instead of pushing into the same pixels;
4. leader can damage Farmer from visibly farther away than 6.7.44.22;
5. capture/source-native behavior remains intact.

Then run:

```text
teamup_mutation status
```

Inspect the new line `Pelipper Mutation steering: leader-smooth-reach` and especially:

- `leaderMoves>0`
- `minionMoves>0`
- `leaderReachHits>0` after ranged-melee contact
- `leaderRangeHolds>0` while the leader sits in its melee band
- `leaderHaltResets>0`
- `leaderDirectionLocks>0`
- `leaderDirectionChanges` should not explode every tick
- `identityMisses=0` ideally

## Mutation contract unchanged

One Mutant leader + 2-4 ordinary hostile source-equivalent followers. No unrelated Slime fallback.

Leader keeps HP x3, stat x2, visible Pelipper x2 cap, aura and global native loot x3. Followers remain ordinary, Mutation-excluded, no x3 leader reward and genuine Pelipper wild encounters when Pelipper is the source.

## Remaining gates before 6.7.45

- 6.7.44.23 leader smoothing/reach live-pass;
- follower native capture recheck;
- all three Pelipper HP phases;
- final global x3 leader loot;
- vanilla/non-Pelipper Mutation regression;
- compatible custom-source regression when practical;
- Lower Workings runtime gate.

Do not start 6.7.45 unless remaining gates pass or the user explicitly waives them.
