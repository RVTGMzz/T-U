# Team Up latest handoff: 0.2.0-alpha.6.7.44.23

Start here: `CONTINUE_HERE.md`

Detailed handoff: `docs/ALPHA_6_7_44_23_MUTANT_LEADER_SMOOTHING_HANDOFF.md`

## Current checkpoint

- Branch: `v0.2-alpha6-7-44-23-mutant-leader-smoothing`
- Version: `0.2.0-alpha.6.7.44.23`
- CI source SHA: `2c8f132527cf06aa2d17a82875d7b6f4f46750b4`
- Run: `34996106050`
- Job: `104472621463`
- Artifact ID: `10407443067`
- Wrapper SHA256: `f27e09e980708d06a91c1e74bbd7a5094b92a2637aa405916fd5df8b8a283080`
- Inner ZIP SHA256: `d95ed6f2a48447ec967032b38b08c2b30632547ecc3fc02351278981fec1efac`
- Build: PASS, 0 warnings / 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

## Current live truth

Native same-species Pelipper followers already work with Spawn Commands OFF and no Slime fallback.

6.7.44.22 live test confirmed followers now pursue/attack Farmer. Remaining defect was specifically the x2 Mutant leader: attack range felt too short and movement visibly jittered.

## 6.7.44.23

`Alpha674423PelipperMutantLeaderSmoothingService` supersedes the 6.7.44.22 runtime steering instance. Followers retain the working pack steering. The leader now ignores follower separation, clears passive Pelipper movement/velocity with `Halt()` before chase steps, uses short direction hysteresis, holds a stable melee band, and can damage Farmer from an extended range suited to its x2 visual scale.

The real visible Pokemon, genuine hidden combat proxy, proxy-based damage and native capture identity remain unchanged.

Immediate live gate: force a Pelipper Mutation, do not attack first, verify followers still attack, leader movement is visibly smoother, leader stops near Farmer instead of overlapping, and leader damage lands from farther away. `teamup_mutation status` should show the new `leader-smooth-reach` telemetry with positive `leaderReachHits`, `leaderRangeHolds`, `leaderHaltResets` and `leaderDirectionLocks` during a real encounter, ideally `identityMisses=0`.

After this passes, recheck follower capture, then continue 3 HP phases, x3 final loot, vanilla/custom regression and Lower Workings. Do not begin 6.7.45 unless remaining runtime gates are passed or explicitly waived.
